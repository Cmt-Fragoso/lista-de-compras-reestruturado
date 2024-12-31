using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Services
{
    /// <summary>
    /// Interface para o serviço de itens
    /// </summary>
    public interface IItemService
    {
        /// <summary>
        /// Obtém um item por ID
        /// </summary>
        Task<ItemModel> GetByIdAsync(int id);

        /// <summary>
        /// Obtém itens de uma lista
        /// </summary>
        Task<IEnumerable<ItemModel>> GetByListaAsync(int listaId);

        /// <summary>
        /// Cria um novo item
        /// </summary>
        Task<ItemModel> CreateAsync(ItemModel item);

        /// <summary>
        /// Atualiza um item existente
        /// </summary>
        Task UpdateAsync(ItemModel item);

        /// <summary>
        /// Remove um item
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// Marca um item como comprado
        /// </summary>
        Task MarcarCompradoAsync(int id, decimal precoReal);

        /// <summary>
        /// Desmarca um item como comprado
        /// </summary>
        Task DesmarcarCompradoAsync(int id);

        /// <summary>
        /// Atualiza o preço estimado de um item
        /// </summary>
        Task AtualizarPrecoEstimadoAsync(int id, decimal novoPreco);

        /// <summary>
        /// Move um item para outra lista
        /// </summary>
        Task MoverParaListaAsync(int id, int novaListaId);

        /// <summary>
        /// Atualiza a categoria de um item
        /// </summary>
        Task AtualizarCategoriaAsync(int id, int novaCategoriaId);
    }
}