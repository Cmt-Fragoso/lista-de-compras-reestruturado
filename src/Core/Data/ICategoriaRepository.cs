using System.Collections.Generic;
using System.Threading.Tasks;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Data
{
    public interface ICategoriaRepository : IRepository<CategoriaModel>
    {
        Task<IEnumerable<CategoriaModel>> GetByUsuarioAsync(int usuarioId);
        Task<CategoriaModel> SaveAsync(CategoriaModel categoria);
        Task<bool> IsUsedAsync(int categoriaId);
    }
}