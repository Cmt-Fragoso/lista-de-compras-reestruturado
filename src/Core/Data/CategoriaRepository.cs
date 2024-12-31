using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ListaCompras.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ListaCompras.Core.Data
{
    /// <summary>
    /// Implementação do repositório de categorias
    /// </summary>
    public class CategoriaRepository : BaseRepository<CategoriaModel>, ICategoriaRepository
    {
        public CategoriaRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CategoriaModel>> GetSubcategoriasAsync(int categoriaPaiId)
        {
            return await _dbSet.Where(c => c.CategoriaPaiId == categoriaPaiId)
                             .OrderBy(c => c.Ordem)
                             .ThenBy(c => c.Nome)
                             .ToListAsync();
        }

        public async Task<IEnumerable<CategoriaModel>> GetCategoriasRaizAsync()
        {
            return await _dbSet.Where(c => c.CategoriaPaiId == null)
                             .OrderBy(c => c.Ordem)
                             .ThenBy(c => c.Nome)
                             .ToListAsync();
        }

        public async Task<IEnumerable<CategoriaModel>> GetArvoreCategoriaAsync()
        {
            var todasCategorias = await _dbSet.OrderBy(c => c.Ordem)
                                            .ThenBy(c => c.Nome)
                                            .ToListAsync();

            var raizes = todasCategorias.Where(c => c.CategoriaPaiId == null);
            return ConstruirArvoreRecursiva(raizes, todasCategorias);
        }

        private IEnumerable<CategoriaModel> ConstruirArvoreRecursiva(
            IEnumerable<CategoriaModel> categorias,
            List<CategoriaModel> todasCategorias)
        {
            foreach (var categoria in categorias)
            {
                var subcategorias = todasCategorias
                    .Where(c => c.CategoriaPaiId == categoria.Id)
                    .OrderBy(c => c.Ordem)
                    .ThenBy(c => c.Nome);

                // Recursive call
                ConstruirArvoreRecursiva(subcategorias, todasCategorias);
            }

            return categorias;
        }

        public async Task ReordenarAsync(IEnumerable<(int categoriaId, int novaOrdem)> novasOrdens)
        {
            foreach (var (categoriaId, novaOrdem) in novasOrdens)
            {
                var categoria = await _dbSet.FindAsync(categoriaId);
                if (categoria != null)
                {
                    categoria.Ordem = novaOrdem;
                    categoria.DataAtualizacao = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task MoverParaCategoriaAsync(int categoriaId, int? novoCategoriaPaiId)
        {
            var categoria = await _dbSet.FindAsync(categoriaId);
            if (categoria == null)
                throw new KeyNotFoundException($"Categoria {categoriaId} não encontrada.");

            // Verifica se não está tentando mover para uma subcategoria dela mesma
            if (novoCategoriaPaiId.HasValue)
            {
                var subcategorias = await GetTodasSubcategoriasRecursivasAsync(categoriaId);
                if (subcategorias.Contains(novoCategoriaPaiId.Value))
                    throw new InvalidOperationException("Não é possível mover uma categoria para uma de suas subcategorias.");
            }

            categoria.CategoriaPaiId = novoCategoriaPaiId;
            categoria.DataAtualizacao = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        private async Task<HashSet<int>> GetTodasSubcategoriasRecursivasAsync(int categoriaId)
        {
            var subcategorias = new HashSet<int>();
            var filhas = await _dbSet.Where(c => c.CategoriaPaiId == categoriaId).ToListAsync();

            foreach (var filha in filhas)
            {
                subcategorias.Add(filha.Id);
                var subFilhas = await GetTodasSubcategoriasRecursivasAsync(filha.Id);
                subcategorias.UnionWith(subFilhas);
            }

            return subcategorias;
        }
    }
}