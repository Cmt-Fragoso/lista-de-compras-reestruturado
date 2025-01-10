using System.Collections.Generic;
using System.Threading.Tasks;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Data
{
    public interface IListaRepository : IRepository<ListaModel>
    {
        Task<IEnumerable<ListaModel>> GetByUsuarioAsync(int usuarioId);
        Task<ListaModel> SaveAsync(ListaModel lista);
    }
}