using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using StackExchange.Redis;

namespace ListaCompras.Core.Managers
{
    /// <summary>
    /// Gerenciador de cache avançado com suporte a distribuição, compressão e priorização
    /// </summary>
    public class CacheManager : IManager
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<CacheManager> _logger;
        private readonly IConnectionMultiplexer _redis;
        private readonly CacheSettings _settings;
        private readonly ConcurrentDictionary<string, CachePriority> _priorities;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;
        private bool _initialized;

        public CacheManager(
            IMemoryCache memoryCache,
            IDistributedCache distributedCache,
            IConnectionMultiplexer redis,
            ILogger<CacheManager> logger,
            CacheSettings settings = null)
        {
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _redis = redis;
            _logger = logger;
            _settings = settings ?? new CacheSettings();
            _priorities = new ConcurrentDictionary<string, CachePriority>();
            _locks = new ConcurrentDictionary<string, SemaphoreSlim>();
        }

        public bool IsInitialized => _initialized;

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            _logger.LogInformation("Inicializando CacheManager");

            try
            {
                // Limpa cache expirado
                await CleanExpiredCacheAsync();

                // Inicializa prioridades
                await LoadPrioritiesAsync();

                _initialized = true;
                _logger.LogInformation("CacheManager inicializado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar CacheManager");
                throw;
            }
        }

        public async Task ShutdownAsync()
        {
            if (!_initialized)
                return;

            _logger.LogInformation("Finalizando CacheManager");

            try
            {
                // Persiste itens importantes
                await PersistHighPriorityItemsAsync();

                _initialized = false;
                _logger.LogInformation("CacheManager finalizado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao finalizar CacheManager");
                throw;
            }
        }

        /// <summary>
        /// Obtém um item do cache, com fallback para cache distribuído
        /// </summary>
        public async Task<T> GetAsync<T>(string key, CacheOptions options = null)
        {
            EnsureInitialized();
            options ??= new CacheOptions();

            try
            {
                // Tenta memória local primeiro
                if (_memoryCache.TryGetValue(key, out T value))
                {
                    _logger.LogTrace("Cache hit (memória) para {Key}", key);
                    return value;
                }

                // Tenta cache distribuído
                if (!options.LocalOnly)
                {
                    var lockObj = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
                    await lockObj.WaitAsync();

                    try
                    {
                        var bytes = await _distributedCache.GetAsync(key);
                        if (bytes != null)
                        {
                            _logger.LogTrace("Cache hit (distribuído) para {Key}", key);
                            
                            // Descomprime e deserializa
                            var decompressed = await DecompressAsync(bytes);
                            var item = JsonSerializer.Deserialize<CacheItem<T>>(decompressed);

                            // Atualiza cache local
                            var cacheOptions = GetMemoryCacheOptions(options);
                            _memoryCache.Set(key, item.Value, cacheOptions);

                            return item.Value;
                        }
                    }
                    finally
                    {
                        lockObj.Release();
                    }
                }

                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter cache para {Key}", key);
                return default;
            }
        }

        /// <summary>
        /// Define um item no cache com suporte a compressão
        /// </summary>
        public async Task SetAsync<T>(
            string key, 
            T value, 
            CacheOptions options = null)
        {
            EnsureInitialized();
            options ??= new CacheOptions();

            try
            {
                var item = new CacheItem<T>
                {
                    Value = value,
                    CreatedAt = DateTime.UtcNow,
                    Priority = options.Priority
                };

                // Atualiza cache local
                var cacheOptions = GetMemoryCacheOptions(options);
                _memoryCache.Set(key, value, cacheOptions);

                // Atualiza cache distribuído
                if (!options.LocalOnly)
                {
                    var lockObj = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
                    await lockObj.WaitAsync();

                    try
                    {
                        // Serializa e comprime
                        var json = JsonSerializer.Serialize(item);
                        var compressed = await CompressAsync(json);

                        // Define no cache distribuído
                        var distributedOptions = GetDistributedCacheOptions(options);
                        await _distributedCache.SetAsync(key, compressed, distributedOptions);

                        // Atualiza prioridade
                        _priorities[key] = options.Priority;
                    }
                    finally
                    {
                        lockObj.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao definir cache para {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Remove um item do cache local e distribuído
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            EnsureInitialized();

            try
            {
                _memoryCache.Remove(key);
                await _distributedCache.RemoveAsync(key);
                _priorities.TryRemove(key, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover cache para {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Limpa todo o cache
        /// </summary>
        public async Task ClearAsync()
        {
            EnsureInitialized();

            try
            {
                // Limpa cache local
                var field = typeof(MemoryCache).GetField("_entries", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                
                var entries = field?.GetValue(_memoryCache) as IDictionary<object, object>;
                entries?.Clear();

                // Limpa cache distribuído
                await _redis.GetDatabase().KeyDeleteAsync(
                    _redis.GetServer(_redis.GetEndPoints().First())
                        .Keys(pattern: "*")
                        .ToArray());

                _priorities.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar cache");
                throw;
            }
        }

        private async Task CleanExpiredCacheAsync()
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(pattern: "*");

                foreach (var key in keys)
                {
                    var value = await _redis.GetDatabase().StringGetAsync(key);
                    if (!value.HasValue)
                        continue;

                    try
                    {
                        var decompressed = await DecompressAsync(value);
                        var item = JsonSerializer.Deserialize<CacheItem<object>>(decompressed);

                        if (item.IsExpired(_settings.DefaultExpirationMinutes))
                        {
                            await _redis.GetDatabase().KeyDeleteAsync(key);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao verificar expiração de {Key}", key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar cache expirado");
            }
        }

        private async Task LoadPrioritiesAsync()
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(pattern: "*");

                foreach (var key in keys)
                {
                    var value = await _redis.GetDatabase().StringGetAsync(key);
                    if (!value.HasValue)
                        continue;

                    try
                    {
                        var decompressed = await DecompressAsync(value);
                        var item = JsonSerializer.Deserialize<CacheItem<object>>(decompressed);
                        _priorities[key.ToString()] = item.Priority;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao carregar prioridade de {Key}", key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar prioridades");
            }
        }

        private async Task PersistHighPriorityItemsAsync()
        {
            var highPriorityKeys = _priorities
                .Where(p => p.Value == CachePriority.High)
                .Select(p => p.Key)
                .ToList();

            foreach (var key in highPriorityKeys)
            {
                try
                {
                    var value = await _distributedCache.GetAsync(key);
                    if (value == null)
                        continue;

                    var persistPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, 
                        "Cache", 
                        $"{key}.cache");

                    Directory.CreateDirectory(Path.GetDirectoryName(persistPath));
                    await File.WriteAllBytesAsync(persistPath, value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao persistir cache para {Key}", key);
                }
            }
        }

        private async Task<byte[]> CompressAsync(string data)
        {
            using var outputStream = new MemoryStream();
            await using var gzip = new GZipStream(outputStream, CompressionLevel.Optimal);
            await using var writer = new StreamWriter(gzip);
            await writer.WriteAsync(data);
            await writer.FlushAsync();
            return outputStream.ToArray();
        }

        private async Task<string> DecompressAsync(byte[] data)
        {
            using var inputStream = new MemoryStream(data);
            using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            return await reader.ReadToEndAsync();
        }

        private MemoryCacheEntryOptions GetMemoryCacheOptions(CacheOptions options)
        {
            var entryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(
                    options.ExpirationMinutes ?? _settings.DefaultExpirationMinutes));

            if (_settings.EnableSlidingExpiration)
            {
                entryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(
                    options.SlidingExpirationMinutes ?? _settings.DefaultSlidingMinutes));
            }

            return entryOptions;
        }

        private DistributedCacheEntryOptions GetDistributedCacheOptions(CacheOptions options)
        {
            var entryOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(
                    options.ExpirationMinutes ?? _settings.DefaultExpirationMinutes));

            if (_settings.EnableSlidingExpiration)
            {
                entryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(
                    options.SlidingExpirationMinutes ?? _settings.DefaultSlidingMinutes));
            }

            return entryOptions;
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("CacheManager não está inicializado");
        }
    }

    public class CacheSettings
    {
        public int DefaultExpirationMinutes { get; set; } = 30;
        public int DefaultSlidingMinutes { get; set; } = 10;
        public bool EnableSlidingExpiration { get; set; } = true;
        public long MaxMemoryMB { get; set; } = 1024;
        public bool CompressData { get; set; } = true;
    }

    public class CacheOptions
    {
        public int? ExpirationMinutes { get; set; }
        public int? SlidingExpirationMinutes { get; set; }
        public bool LocalOnly { get; set; }
        public CachePriority Priority { get; set; } = CachePriority.Normal;
    }

    public class CacheItem<T>
    {
        public T Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public CachePriority Priority { get; set; }

        public bool IsExpired(int minutes) =>
            DateTime.UtcNow > CreatedAt.AddMinutes(minutes);
    }

    public enum CachePriority
    {
        Low,
        Normal,
        High
    }
}