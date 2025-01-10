using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Data
{
    public class PrecoRepository : BaseRepository<PrecoModel>, IPrecoRepository
    {
        public PrecoRepository(AppDbContext context, ILogger<PrecoRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<PrecoModel>> GetByItemAsync(int itemId)
        {
            return await _dbSet
                .Where(p => p.ItemId == itemId)
                .OrderByDescending(p => p.Data)
                .ToListAsync();
        }

        public async Task<PrecoModel> SaveAsync(PrecoModel preco)
        {
            if (preco.Id == 0)
            {
                preco.DataCriacao = DateTime.UtcNow;
                await AddAsync(preco);
            }
            else
            {
                preco.DataAtualizacao = DateTime.UtcNow;
                await UpdateAsync(preco);
            }
            return preco;
        }
    }
}