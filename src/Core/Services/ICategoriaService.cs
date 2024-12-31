using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Services
{
    /// <summary>
    /// Interface para o serviço de categorias
    /// </summary>
    public interface ICategoriaService
    {
        /// <summary>
        /// Obtém uma categoria por ID
        /// </summary>
        Task<CategoriaModel> GetByIdAsync(int id);

        /// <summary>
        /// Obtém todas as categorias raiz
        /// </summary>
        Task<IEnumerable<CategoriaModel>> GetCategoriasRaizAsync();

        /// <summary>
        /// Obtém a árvore completa de categorias
        /// </summary>
        Task<IEnumerable<CategoriaModel>> GetArvoreCategoriaAsync();

        /// <summary>
        /// Cria uma nova categoria
        /// </summary>
        Task<CategoriaModel> CreateAsync(CategoriaModel categoria);

        /// <summary>
        /// Atualiza uma categoria existente
        /// </summary>
        Task UpdateAsync(CategoriaModel categoria);

        /// <summary>
        /// Remove uma categoria
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// Move uma categoria para um novo pai
        /// </summary>
        Task MoverParaCategoriaAsync(int id, int? novoCategoriaPaiId);

        /// <summary>
        /// Reordena categorias
        /// </summary>
        Task ReordenarAsync(IEnumerable<(int categoriaId, int novaOrdem)> novasOrdens);

        /// <summary>
        /// Obtém subcategorias de uma categoria
        /// </summary>
        Task<IEnumerable<CategoriaModel>> GetSubcategoriasAsync(int categoriaId);

        /// <summary>
        /// Atualiza a representação visual de uma categoria
        /// </summary>
        Task AtualizarVisualizacaoAsync(int id, string cor, string icone);
    }
}