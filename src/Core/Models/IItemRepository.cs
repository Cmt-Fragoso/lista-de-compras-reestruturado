using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Interface para repositório de itens
    /// </summary>
    public interface IItemRepository : IRepository<ItemModel>
    {
        /// <summary>
        /// Obtém todos os itens de uma lista
        /// </summary>
        Task<IEnumerable<ItemModel>> GetByListaIdAsync(int listaId);

        /// <summary>
        /// Obtém itens por categoria
        /// </summary>
        Task<IEnumerable<ItemModel>> GetByCategoriaIdAsync(int categoriaId);

        /// <summary>
        /// Obtém itens não comprados de uma lista
        /// </summary>
        Task<IEnumerable<ItemModel>> GetPendentesAsync(int listaId);

        /// <summary>
        /// Obtém itens comprados de uma lista
        /// </summary>
        Task<IEnumerable<ItemModel>> GetCompradosAsync(int listaId);

        /// <summary>
        /// Marca um item como comprado
        /// </summary>
        Task MarcarCompradoAsync(int itemId, decimal precoReal);

        /// <summary>
        /// Desmarca um item como comprado
        /// </summary>
        Task DesmarcarCompradoAsync(int itemId);
    }
}