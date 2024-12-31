using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ListaCompras.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ListaCompras.Core.Data
{
    /// <summary>
    /// Implementação do repositório de itens
    /// </summary>
    public class ItemRepository : BaseRepository<ItemModel>, IItemRepository
    {
        public ItemRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ItemModel>> GetByListaIdAsync(int listaId)
        {
            return await _dbSet.Where(i => i.ListaId == listaId)
                             .OrderBy(i => i.DataCriacao)
                             .ToListAsync();
        }

        public async Task<IEnumerable<ItemModel>> GetByCategoriaIdAsync(int categoriaId)
        {
            return await _dbSet.Where(i => i.CategoriaId == categoriaId)
                             .OrderBy(i => i.Nome)
                             .ToListAsync();
        }

        public async Task<IEnumerable<ItemModel>> GetPendentesAsync(int listaId)
        {
            return await _dbSet.Where(i => i.ListaId == listaId && !i.Comprado)
                             .OrderBy(i => i.DataCriacao)
                             .ToListAsync();
        }

        public async Task<IEnumerable<ItemModel>> GetCompradosAsync(int listaId)
        {
            return await _dbSet.Where(i => i.ListaId == listaId && i.Comprado)
                             .OrderByDescending(i => i.DataAtualizacao)
                             .ToListAsync();
        }

        public async Task MarcarCompradoAsync(int itemId, decimal precoReal)
        {
            var item = await _dbSet.FindAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Item {itemId} não encontrado.");

            item.Comprado = true;
            item.PrecoEstimado = precoReal; // Atualiza com o preço real
            item.DataAtualizacao = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task DesmarcarCompradoAsync(int itemId)
        {
            var item = await _dbSet.FindAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Item {itemId} não encontrado.");

            item.Comprado = false;
            item.DataAtualizacao = DateTime.Now;

            await _context.SaveChangesAsync();
        }
    }
}