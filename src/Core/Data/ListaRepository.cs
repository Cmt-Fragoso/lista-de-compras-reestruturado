using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ListaCompras.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ListaCompras.Core.Data
{
    /// <summary>
    /// Implementação do repositório de listas
    /// </summary>
    public class ListaRepository : BaseRepository<ListaModel>, IListaRepository
    {
        public ListaRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ListaModel>> GetByUsuarioIdAsync(int usuarioId)
        {
            return await _dbSet.Where(l => l.UsuarioId == usuarioId)
                             .OrderByDescending(l => l.DataCriacao)
                             .Include(l => l.Itens)
                             .ToListAsync();
        }

        public async Task<IEnumerable<ListaModel>> GetByStatusAsync(StatusLista status)
        {
            return await _dbSet.Where(l => l.Status == status)
                             .OrderByDescending(l => l.DataCriacao)
                             .Include(l => l.Itens)
                             .ToListAsync();
        }

        public async Task<IEnumerable<ListaModel>> GetAtivasAsync(int usuarioId)
        {
            return await _dbSet.Where(l => l.UsuarioId == usuarioId && 
                                         (l.Status == StatusLista.Ativa || 
                                          l.Status == StatusLista.EmCompra))
                             .OrderByDescending(l => l.DataCriacao)
                             .Include(l => l.Itens)
                             .ToListAsync();
        }

        public async Task<IEnumerable<ListaModel>> GetArquivadasAsync(int usuarioId)
        {
            return await _dbSet.Where(l => l.UsuarioId == usuarioId && 
                                         l.Status == StatusLista.Arquivada)
                             .OrderByDescending(l => l.DataCriacao)
                             .Include(l => l.Itens)
                             .ToListAsync();
        }

        public async Task AtualizarStatusAsync(int listaId, StatusLista novoStatus)
        {
            var lista = await _dbSet.FindAsync(listaId);
            if (lista == null)
                throw new KeyNotFoundException($"Lista {listaId} não encontrada.");

            lista.Status = novoStatus;
            lista.DataAtualizacao = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task ArquivarAsync(int listaId)
        {
            var lista = await _dbSet.FindAsync(listaId);
            if (lista == null)
                throw new KeyNotFoundException($"Lista {listaId} não encontrada.");

            lista.Status = StatusLista.Arquivada;
            lista.DataAtualizacao = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task<decimal> CalcularTotalRealAsync(int listaId)
        {
            var lista = await _dbSet.Include(l => l.Itens)
                                  .FirstOrDefaultAsync(l => l.Id == listaId);

            if (lista == null)
                throw new KeyNotFoundException($"Lista {listaId} não encontrada.");

            decimal total = lista.Itens
                .Where(i => i.Comprado)
                .Sum(i => i.PrecoEstimado * i.Quantidade);

            lista.ValorTotal = total;
            lista.DataAtualizacao = DateTime.Now;

            await _context.SaveChangesAsync();

            return total;
        }
    }
}