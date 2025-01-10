using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Data
{
    public class ItemRepository : BaseRepository<ItemModel>, IItemRepository
    {
        public ItemRepository(AppDbContext context, ILogger<ItemRepository> logger) 
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<ItemModel>> GetByListaAsync(int listaId)
        {
            return await _dbSet
                .Include(i => i.Categoria)
                .Where(i => i.ListaId == listaId)
                .ToListAsync();
        }

        public async Task<IEnumerable<ItemModel>> GetByCategoriaAsync(int categoriaId)
        {
            return await _dbSet
                .Include(i => i.Lista)
                .Where(i => i.CategoriaId == categoriaId)
                .ToListAsync();
        }

        public async Task<ItemModel> SaveAsync(ItemModel item)
        {
            if (item.Id == 0)
            {
                item.DataCriacao = DateTime.UtcNow;
                await AddAsync(item);
            }
            else
            {
                item.DataAtualizacao = DateTime.UtcNow;
                await UpdateAsync(item);
            }
            return item;
        }
    }
}