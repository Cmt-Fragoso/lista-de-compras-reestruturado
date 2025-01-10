using System.Collections.Generic;
using System.Threading.Tasks;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Data
{
    public interface IPrecoRepository : IRepository<PrecoModel>
    {
        Task<IEnumerable<PrecoModel>> GetByItemAsync(int itemId);
        Task<PrecoModel> SaveAsync(PrecoModel preco);
    }
}