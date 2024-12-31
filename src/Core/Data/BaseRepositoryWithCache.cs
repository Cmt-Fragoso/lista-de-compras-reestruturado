using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ListaCompras.Core.Data
{
    /// <summary>
    /// Implementação base para repositórios com suporte a cache
    /// </summary>
    public abstract class BaseRepositoryWithCache<T> : BaseRepository<T> where T : class
    {
        protected readonly QueryCache<int, T> _singleItemCache;
        protected readonly QueryCache<string, IEnumerable<T>> _collectionCache;
        private readonly TimeSpan _defaultSingleItemExpiration = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _defaultCollectionExpiration = TimeSpan.FromMinutes(2);

        protected BaseRepositoryWithCache(
            AppDbContext context,
            IMemoryCache cache,
            ILogger logger) : base(context)
        {
            var typeName = typeof(T).Name;
            _singleItemCache = new QueryCache<int, T>(
                cache, 
                logger, 
                $"{typeName}_Single",
                _defaultSingleItemExpiration);

            _collectionCache = new QueryCache<string, IEnumerable<T>>(
                cache, 
                logger, 
                $"{typeName}_Collection",
                _defaultCollectionExpiration);
        }

        public override async Task<T> GetByIdAsync(int id)
        {
            return await _singleItemCache.GetOrAddAsync(
                id,
                async () => await base.GetByIdAsync(id));
        }

        public override async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _collectionCache.GetOrAddAsync(
                "all",
                async () => await base.GetAllAsync());
        }

        public override async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            // Cache somente se o predicado for simples (um filtro por Id ou chave)
            if (IsSimplePredicate(predicate))
            {
                var cacheKey = GetPredicateCacheKey(predicate);
                return await _collectionCache.GetOrAddAsync(
                    cacheKey,
                    async () => await base.FindAsync(predicate));
            }

            // Se for uma query complexa, não usa cache
            return await base.FindAsync(predicate);
        }

        public override async Task<T> AddAsync(T entity)
        {
            var result = await base.AddAsync(entity);
            InvalidateCache();
            return result;
        }

        public override async Task UpdateAsync(T entity)
        {
            await base.UpdateAsync(entity);
            InvalidateCache();
        }

        public override async Task DeleteAsync(T entity)
        {
            await base.DeleteAsync(entity);
            InvalidateCache();
        }

        protected void InvalidateCache()
        {
            _collectionCache.Clear();
            // Não limpa cache de itens individuais pois podem ainda ser válidos
        }

        private bool IsSimplePredicate(Expression<Func<T, bool>> predicate)
        {
            // Verifica se é uma expressão simples de comparação por Id ou chave
            if (predicate.Body is BinaryExpression binary)
            {
                if (binary.Left is MemberExpression member)
                {
                    var propertyName = member.Member.Name;
                    return propertyName == "Id" || propertyName.EndsWith("Id");
                }
            }
            return false;
        }

        private string GetPredicateCacheKey(Expression<Func<T, bool>> predicate)
        {
            // Gera uma chave única baseada na expressão
            return $"find_{predicate.ToString().GetHashCode()}";
        }
    }
}