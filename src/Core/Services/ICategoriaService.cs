using System.Collections.Generic;
using System.Threading.Tasks;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Services
{
    public interface ICategoriaService
    {
        Task<CategoriaModel> GetByIdAsync(int id);
        Task<IEnumerable<CategoriaModel>> GetAllAsync();
        Task<CategoriaModel> CreateAsync(CategoriaModel categoria);
        Task UpdateAsync(CategoriaModel categoria);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(string nome, int? excludeId = null);
        Task<IEnumerable<CategoriaModel>> GetCategoriasRaizAsync();
        Task<IEnumerable<CategoriaModel>> GetSubcategoriasAsync(int categoriaId);
        Task<IEnumerable<CategoriaModel>> GetArvoreCategoriaAsync();
        Task MoverParaCategoriaAsync(int categoriaId, int? novaPaiId);
        Task ReordenarAsync(IEnumerable<(int categoriaId, int novaOrdem)> ordenacao);
        Task AtualizarVisualizacaoAsync(int id, string corFundo, string corTexto);
    }
}