using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ListaCompras.Core.Data
{
    /// <summary>
    /// Cache de consultas para otimização de performance
    /// </summary>
    public class QueryCache<TKey, TValue>
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger _logger;
        private readonly string _region;
        private readonly TimeSpan _defaultExpiration;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;

        public QueryCache(
            IMemoryCache cache,
            ILogger logger,
            string region,
            TimeSpan? defaultExpiration = null)
        {
            _cache = cache;
            _logger = logger;
            _region = region;
            _defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(5);
            _locks = new ConcurrentDictionary<string, SemaphoreSlim>();
        }

        /// <summary>
        /// Obtém ou adiciona item ao cache
        /// </summary>
        public async Task<TValue> GetOrAddAsync(
            TKey key,
            Func<Task<TValue>> factory,
            TimeSpan? expiration = null)
        {
            var cacheKey = GetCacheKey(key);
            var lockKey = $"lock_{cacheKey}";
            var lockObj = _locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

            try
            {
                // Tenta obter do cache primeiro
                if (_cache.TryGetValue<TValue>(cacheKey, out var cachedValue))
                {
                    _logger.LogTrace($"Cache hit: {cacheKey}");
                    return cachedValue;
                }

                // Se não encontrou, aguarda lock para evitar múltiplas chamadas
                await lockObj.WaitAsync();

                // Tenta novamente após obter o lock (double-check)
                if (_cache.TryGetValue<TValue>(cacheKey, out cachedValue))
                {
                    _logger.LogTrace($"Cache hit after lock: {cacheKey}");
                    return cachedValue;
                }

                // Se ainda não encontrou, busca o valor
                var value = await factory();

                // Armazena no cache
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(expiration ?? _defaultExpiration)
                    .RegisterPostEvictionCallback((key, value, reason, state) =>
                    {
                        _logger.LogTrace($"Cache evicted: {key}, Reason: {reason}");
                    });

                _cache.Set(cacheKey, value, cacheOptions);
                _logger.LogTrace($"Cache set: {cacheKey}");

                return value;
            }
            finally
            {
                lockObj.Release();
            }
        }

        /// <summary>
        /// Remove item do cache
        /// </summary>
        public void Remove(TKey key)
        {
            var cacheKey = GetCacheKey(key);
            _cache.Remove(cacheKey);
            _logger.LogTrace($"Cache removed: {cacheKey}");
        }

        /// <summary>
        /// Limpa todos os itens da região
        /// </summary>
        public void Clear()
        {
            if (_cache is MemoryCache memoryCache)
            {
                var field = typeof(MemoryCache).GetProperty("EntriesCollection", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var collection = field?.GetValue(memoryCache) as dynamic;
                var items = new List<string>();

                if (collection != null)
                {
                    foreach (var item in collection)
                    {
                        var key = item?.Key?.ToString();
                        if (key?.StartsWith($"{_region}:") == true)
                        {
                            items.Add(key);
                        }
                    }
                }

                foreach (var key in items)
                {
                    _cache.Remove(key);
                    _logger.LogTrace($"Cache cleared: {key}");
                }
            }
        }

        private string GetCacheKey(TKey key)
        {
            return $"{_region}:{key}";
        }
    }
}