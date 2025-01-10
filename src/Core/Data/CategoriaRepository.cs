using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Data
{
    public class CategoriaRepository : BaseRepository<CategoriaModel>, ICategoriaRepository
    {
        public CategoriaRepository(AppDbContext context, ILogger<CategoriaRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<CategoriaModel>> GetByUsuarioAsync(int usuarioId)
        {
            return await _dbSet
                .Where(c => c.UsuarioId == usuarioId)
                .OrderBy(c => c.Nome)
                .ToListAsync();
        }

        public async Task<bool> IsUsedAsync(int categoriaId)
        {
            return await _context.Itens.AnyAsync(i => i.CategoriaId == categoriaId);
        }

        public async Task<CategoriaModel> SaveAsync(CategoriaModel categoria)
        {
            if (categoria.Id == 0)
            {
                categoria.DataCriacao = DateTime.UtcNow;
                await AddAsync(categoria);
            }
            else
            {
                categoria.DataAtualizacao = DateTime.UtcNow;
                await UpdateAsync(categoria);
            }
            return categoria;
        }
    }
}