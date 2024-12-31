using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ListaCompras.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ListaCompras.Core.Data
{
    /// <summary>
    /// Implementação do repositório de preços
    /// </summary>
    public class PrecoRepository : BaseRepository<PrecoModel>, IPrecoRepository
    {
        public PrecoRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<PrecoModel>> GetHistoricoAsync(int itemId)
        {
            return await _dbSet.Where(p => p.ItemId == itemId)
                             .OrderByDescending(p => p.DataPreco)
                             .ToListAsync();
        }

        public async Task<PrecoModel> GetUltimoPrecoAsync(int itemId)
        {
            return await _dbSet.Where(p => p.ItemId == itemId)
                             .OrderByDescending(p => p.DataPreco)
                             .FirstOrDefaultAsync();
        }

        public async Task<decimal> GetMediaPrecoPeriodoAsync(int itemId, DateTime inicio, DateTime fim)
        {
            var precos = await _dbSet.Where(p => p.ItemId == itemId &&
                                                p.DataPreco >= inicio &&
                                                p.DataPreco <= fim)
                                    .ToListAsync();

            if (!precos.Any())
                return 0;

            return precos.Average(p => p.Valor);
        }

        public async Task<IEnumerable<PrecoModel>> GetByFonteAsync(FontePreco fonte)
        {
            return await _dbSet.Where(p => p.Fonte == fonte)
                             .OrderByDescending(p => p.DataPreco)
                             .ToListAsync();
        }

        public async Task<IEnumerable<PrecoModel>> GetPromocionaisAtivosAsync()
        {
            var dataLimite = DateTime.Now.AddDays(-30); // Considera promoções dos últimos 30 dias
            return await _dbSet.Where(p => p.Promocional && p.DataPreco >= dataLimite)
                             .OrderByDescending(p => p.DataPreco)
                             .ToListAsync();
        }

        public async Task<PrecoModel> RegistrarPrecoAsync(int itemId, decimal valor, string local, FontePreco fonte, bool promocional = false)
        {
            var preco = new PrecoModel
            {
                ItemId = itemId,
                Valor = valor,
                Local = local,
                Fonte = fonte,
                Promocional = promocional,
                DataPreco = DateTime.Now,
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now
            };

            await _dbSet.AddAsync(preco);
            await _context.SaveChangesAsync();

            // Atualiza o preço estimado do item
            var item = await _context.Set<ItemModel>().FindAsync(itemId);
            if (item != null)
            {
                item.PrecoEstimado = valor;
                item.DataAtualizacao = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return preco;
        }
    }
}