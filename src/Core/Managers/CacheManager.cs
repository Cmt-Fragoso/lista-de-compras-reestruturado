using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;

namespace ListaCompras.Core.Managers
{
    /// <summary>
    /// Manager responsável pelo gerenciamento de cache
    /// </summary>
    public class CacheManager : IManager
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheManager> _logger;
        private readonly string _cacheDirectory;
        private readonly ConcurrentDictionary<string, object> _locks;
        private bool _initialized;

        private const int DEFAULT_MEMORY_EXPIRATION_MINUTES = 30;
        private const int DEFAULT_DISK_EXPIRATION_DAYS = 7;

        public CacheManager(
            IMemoryCache memoryCache,
            ILogger<CacheManager> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");
            _locks = new ConcurrentDictionary<string, object>();
        }

        public bool IsInitialized => _initialized;

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            _logger.LogInformation("Inicializando CacheManager");

            try
            {
                // Garante que o diretório de cache existe
                if (!Directory.Exists(_cacheDirectory))
                    Directory.CreateDirectory(_cacheDirectory);

                // Limpa cache expirado em disco
                await CleanExpiredDiskCacheAsync();

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
                // Persiste cache de memória importante em disco
                await PersistMemoryCacheToDiskAsync();

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
        /// Obtém item do cache (memória ou disco)
        /// </summary>
        public async Task<T> GetAsync<T>(string key, bool useMemoryOnly = false)
        {
            EnsureInitialized();

            // Tenta memória primeiro
            if (_memoryCache.TryGetValue<T>(key, out var value))
                return value;

            if (useMemoryOnly)
                return default;

            // Se não encontrou e pode usar disco, tenta disco
            return await GetFromDiskAsync<T>(key);
        }

        /// <summary>
        /// Define item no cache
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? memoryExpiration = null, bool persistToDisk = false)
        {
            EnsureInitialized();

            var memoryCacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(memoryExpiration ?? TimeSpan.FromMinutes(DEFAULT_MEMORY_EXPIRATION_MINUTES));

            _memoryCache.Set(key, value, memoryCacheOptions);

            if (persistToDisk)
                await SaveToDiskAsync(key, value);
        }

        /// <summary>
        /// Remove item do cache
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            EnsureInitialized();

            _memoryCache.Remove(key);
            await RemoveFromDiskAsync(key);
        }

        /// <summary>
        /// Limpa todo o cache
        /// </summary>
        public async Task ClearAsync()
        {
            EnsureInitialized();

            var diskCacheFiles = Directory.GetFiles(_cacheDirectory, "*.cache");
            foreach (var file in diskCacheFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Erro ao deletar arquivo de cache: {file}");
                }
            }
        }

        #region Métodos Privados

        private void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("CacheManager não está inicializado");
        }

        private async Task<T> GetFromDiskAsync<T>(string key)
        {
            var filePath = GetCacheFilePath(key);
            if (!File.Exists(filePath))
                return default;

            var lockObj = _locks.GetOrAdd(key, _ => new object());
            lock (lockObj)
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var cacheEntry = JsonSerializer.Deserialize<DiskCacheEntry<T>>(json);

                    if (cacheEntry.ExpirationDate < DateTime.Now)
                    {
                        File.Delete(filePath);
                        return default;
                    }

                    // Coloca em memória também
                    _memoryCache.Set(key, cacheEntry.Value, TimeSpan.FromMinutes(DEFAULT_MEMORY_EXPIRATION_MINUTES));

                    return cacheEntry.Value;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao ler cache do disco: {key}");
                    return default;
                }
            }
        }

        private async Task SaveToDiskAsync<T>(string key, T value)
        {
            var filePath = GetCacheFilePath(key);
            var lockObj = _locks.GetOrAdd(key, _ => new object());
            
            var cacheEntry = new DiskCacheEntry<T>
            {
                Key = key,
                Value = value,
                CreationDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddDays(DEFAULT_DISK_EXPIRATION_DAYS)
            };

            lock (lockObj)
            {
                try
                {
                    var json = JsonSerializer.Serialize(cacheEntry);
                    File.WriteAllText(filePath, json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao salvar cache em disco: {key}");
                }
            }
        }

        private async Task RemoveFromDiskAsync(string key)
        {
            var filePath = GetCacheFilePath(key);
            if (File.Exists(filePath))
            {
                var lockObj = _locks.GetOrAdd(key, _ => new object());
                lock (lockObj)
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Erro ao remover cache do disco: {key}");
                    }
                }
            }
        }

        private async Task CleanExpiredDiskCacheAsync()
        {
            var files = Directory.GetFiles(_cacheDirectory, "*.cache");
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var expirationDate = JsonDocument.Parse(json)
                        .RootElement
                        .GetProperty("ExpirationDate")
                        .GetDateTime();

                    if (expirationDate < DateTime.Now)
                        File.Delete(file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Erro ao limpar cache expirado: {file}");
                }
            }
        }

        private async Task PersistMemoryCacheToDiskAsync()
        {
            // Este método seria implementado para persistir itens importantes
            // da memória para o disco durante o shutdown
            // Por enquanto está vazio pois precisamos definir quais itens são importantes
        }

        private string GetCacheFilePath(string key)
        {
            return Path.Combine(_cacheDirectory, $"{key}.cache");
        }

        #endregion

        private class DiskCacheEntry<T>
        {
            public string Key { get; set; }
            public T Value { get; set; }
            public DateTime CreationDate { get; set; }
            public DateTime ExpirationDate { get; set; }
        }
    }
}