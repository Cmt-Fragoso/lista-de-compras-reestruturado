using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Interface para repositório de listas de compras
    /// </summary>
    public interface IListaRepository : IRepository<ListaModel>
    {
        /// <summary>
        /// Obtém todas as listas de um usuário
        /// </summary>
        Task<IEnumerable<ListaModel>> GetByUsuarioIdAsync(int usuarioId);

        /// <summary>
        /// Obtém listas por status
        /// </summary>
        Task<IEnumerable<ListaModel>> GetByStatusAsync(StatusLista status);

        /// <summary>
        /// Obtém listas ativas de um usuário
        /// </summary>
        Task<IEnumerable<ListaModel>> GetAtivasAsync(int usuarioId);

        /// <summary>
        /// Obtém listas arquivadas de um usuário
        /// </summary>
        Task<IEnumerable<ListaModel>> GetArquivadasAsync(int usuarioId);

        /// <summary>
        /// Atualiza o status de uma lista
        /// </summary>
        Task AtualizarStatusAsync(int listaId, StatusLista novoStatus);

        /// <summary>
        /// Arquiva uma lista
        /// </summary>
        Task ArquivarAsync(int listaId);

        /// <summary>
        /// Calcula o total real de uma lista
        /// </summary>
        Task<decimal> CalcularTotalRealAsync(int listaId);
    }
}