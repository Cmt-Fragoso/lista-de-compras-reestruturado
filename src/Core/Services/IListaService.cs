using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Services
{
    /// <summary>
    /// Interface para o serviço de listas
    /// </summary>
    public interface IListaService
    {
        /// <summary>
        /// Obtém uma lista por ID
        /// </summary>
        Task<ListaModel> GetByIdAsync(int id);

        /// <summary>
        /// Obtém todas as listas de um usuário
        /// </summary>
        Task<IEnumerable<ListaModel>> GetByUsuarioAsync(int usuarioId);

        /// <summary>
        /// Cria uma nova lista
        /// </summary>
        Task<ListaModel> CreateAsync(ListaModel lista);

        /// <summary>
        /// Atualiza uma lista existente
        /// </summary>
        Task UpdateAsync(ListaModel lista);

        /// <summary>
        /// Remove uma lista
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// Atualiza o status de uma lista
        /// </summary>
        Task AtualizarStatusAsync(int id, StatusLista novoStatus);

        /// <summary>
        /// Arquiva uma lista
        /// </summary>
        Task ArquivarAsync(int id);

        /// <summary>
        /// Duplica uma lista existente
        /// </summary>
        Task<ListaModel> DuplicarAsync(int id, string novoNome = null);

        /// <summary>
        /// Calcula o total real da lista
        /// </summary>
        Task<decimal> CalcularTotalAsync(int id);

        /// <summary>
        /// Compartilha uma lista com outro usuário
        /// </summary>
        Task CompartilharAsync(int id, int usuarioDestinoId);

        /// <summary>
        /// Obtém listas compartilhadas com o usuário
        /// </summary>
        Task<IEnumerable<ListaModel>> GetCompartilhadasAsync(int usuarioId);
    }
}