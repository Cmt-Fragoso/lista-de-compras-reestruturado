using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Interface para repositório de categorias
    /// </summary>
    public interface ICategoriaRepository : IRepository<CategoriaModel>
    {
        /// <summary>
        /// Obtém subcategorias de uma categoria
        /// </summary>
        Task<IEnumerable<CategoriaModel>> GetSubcategoriasAsync(int categoriaPaiId);

        /// <summary>
        /// Obtém categorias raiz (sem pai)
        /// </summary>
        Task<IEnumerable<CategoriaModel>> GetCategoriasRaizAsync();

        /// <summary>
        /// Obtém a árvore completa de categorias
        /// </summary>
        Task<IEnumerable<CategoriaModel>> GetArvoreCategoriaAsync();

        /// <summary>
        /// Reordena categorias
        /// </summary>
        Task ReordenarAsync(IEnumerable<(int categoriaId, int novaOrdem)> novasOrdens);

        /// <summary>
        /// Move uma categoria para novo pai
        /// </summary>
        Task MoverParaCategoriaAsync(int categoriaId, int? novoCategoriaPaiId);
    }
}