using System.Collections.Generic;
using System.Threading.Tasks;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Data
{
    public interface IItemRepository : IRepository<ItemModel>
    {
        Task<IEnumerable<ItemModel>> GetByListaAsync(int listaId);
        Task<IEnumerable<ItemModel>> GetByCategoriaAsync(int categoriaId);
        Task<ItemModel> SaveAsync(ItemModel item);
    }
}