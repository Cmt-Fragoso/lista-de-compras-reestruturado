using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Data
{
    public class ListaRepository : BaseRepository<ListaModel>, IListaRepository
    {
        public ListaRepository(AppDbContext context, ILogger<ListaRepository> logger) 
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<ListaModel>> GetByUsuarioAsync(int usuarioId)
        {
            return await _dbSet
                .Include(l => l.Itens)
                .ThenInclude(i => i.Categoria)
                .Where(l => l.UsuarioId == usuarioId)
                .ToListAsync();
        }

        public async Task<ListaModel> SaveAsync(ListaModel lista)
        {
            if (lista.Id == 0)
            {
                lista.DataCriacao = DateTime.UtcNow;
                await AddAsync(lista);
            }
            else
            {
                lista.DataAtualizacao = DateTime.UtcNow;
                await UpdateAsync(lista);
            }
            return lista;
        }
    }
}