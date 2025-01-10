using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ListaCompras.Core.Data
{
    /// <summary>
    /// Interface base para repositórios
    /// </summary>
    public interface IRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Obtém uma entidade por ID
        /// </summary>
        Task<TEntity> GetByIdAsync(int id);

        /// <summary>
        /// Obtém todas as entidades
        /// </summary>
        Task<IEnumerable<TEntity>> GetAllAsync();

        /// <summary>
        /// Busca entidades por um predicado
        /// </summary>
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Adiciona uma nova entidade
        /// </summary>
        Task<TEntity> AddAsync(TEntity entity);

        /// <summary>
        /// Atualiza uma entidade existente
        /// </summary>
        Task<TEntity> UpdateAsync(TEntity entity);

        /// <summary>
        /// Remove uma entidade
        /// </summary>
        Task DeleteAsync(TEntity entity);

        /// <summary>
        /// Verifica se existe uma entidade que atende ao predicado
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate = null);
    }
}