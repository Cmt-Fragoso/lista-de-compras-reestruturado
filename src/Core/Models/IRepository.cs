using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Interface base para repositórios
    /// </summary>
    /// <typeparam name="T">Tipo da entidade</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Obtém uma entidade por ID
        /// </summary>
        Task<T> GetByIdAsync(int id);

        /// <summary>
        /// Obtém todas as entidades
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Encontra entidades que atendam uma condição
        /// </summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Adiciona uma nova entidade
        /// </summary>
        Task<T> AddAsync(T entity);

        /// <summary>
        /// Atualiza uma entidade existente
        /// </summary>
        Task UpdateAsync(T entity);

        /// <summary>
        /// Remove uma entidade
        /// </summary>
        Task DeleteAsync(T entity);

        /// <summary>
        /// Verifica se existe alguma entidade que atenda a condição
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Conta quantas entidades atendem a condição
        /// </summary>
        Task<int> CountAsync(Expression<Func<T, bool>> predicate = null);
    }
}