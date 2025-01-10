using System.Collections.Generic;
using System.Threading.Tasks;

namespace ListaCompras.Core.Validators
{
    /// <summary>
    /// Interface base para validadores
    /// </summary>
    /// <typeparam name="T">Tipo do modelo a ser validado</typeparam>
    public interface IValidator<T>
    {
        /// <summary>
        /// Valida uma entidade
        /// </summary>
        /// <param name="entity">Entidade a ser validada</param>
        /// <returns>Lista de erros de validação, vazia se válido</returns>
        Task<IEnumerable<string>> ValidateAsync(T entity);

        /// <summary>
        /// Verifica se uma entidade é válida
        /// </summary>
        /// <param name="entity">Entidade a ser verificada</param>
        /// <returns>True se válida, False caso contrário</returns>
        Task<bool> IsValidAsync(T entity);
    }
}