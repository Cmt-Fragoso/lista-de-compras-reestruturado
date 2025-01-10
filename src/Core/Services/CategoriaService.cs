using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ListaCompras.Core.Models;
using ListaCompras.Core.Data;

namespace ListaCompras.Core.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly ICategoriaRepository _repository;
        private readonly ICurrentUserProvider _userProvider;

        public CategoriaService(
            ICategoriaRepository repository,
            ICurrentUserProvider userProvider)
        {
            _repository = repository;
            _userProvider = userProvider;
        }

        public async Task<CategoriaModel> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id) ??
                throw new KeyNotFoundException($"Categoria {id} não encontrada");
        }

        public async Task<IEnumerable<CategoriaModel>> GetAllAsync()
        {
            var userId = _userProvider.GetCurrentUserId();
            return await _repository.GetByUsuarioAsync(userId);
        }

        public async Task<CategoriaModel> CreateAsync(CategoriaModel categoria)
        {
            if (categoria == null)
                throw new ArgumentNullException(nameof(categoria));

            categoria.UsuarioId = _userProvider.GetCurrentUserId();
            return await _repository.SaveAsync(categoria);
        }

        public async Task UpdateAsync(CategoriaModel categoria)
        {
            if (categoria == null)
                throw new ArgumentNullException(nameof(categoria));

            var existente = await GetByIdAsync(categoria.Id);
            await _repository.SaveAsync(existente);
        }

        public async Task DeleteAsync(int id)
        {
            var categoria = await GetByIdAsync(id);
            if (await _repository.IsUsedAsync(id))
                throw new InvalidOperationException("Não é possível excluir uma categoria em uso");

            await _repository.DeleteAsync(categoria);
        }

        public async Task<bool> ExistsAsync(string nome, int? excludeId = null)
        {
            var userId = _userProvider.GetCurrentUserId();
            return await _repository.ExistsAsync(c => 
                c.UsuarioId == userId && 
                c.Nome == nome && 
                (excludeId == null || c.Id != excludeId));
        }

        public async Task<IEnumerable<CategoriaModel>> GetCategoriasRaizAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<CategoriaModel>> GetSubcategoriasAsync(int categoriaId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<CategoriaModel>> GetArvoreCategoriaAsync()
        {
            throw new NotImplementedException();
        }

        public async Task MoverParaCategoriaAsync(int categoriaId, int? novaPaiId)
        {
            throw new NotImplementedException();
        }

        public async Task ReordenarAsync(IEnumerable<(int categoriaId, int novaOrdem)> ordenacao)
        {
            throw new NotImplementedException();
        }

        public async Task AtualizarVisualizacaoAsync(int id, string corFundo, string corTexto)
        {
            throw new NotImplementedException();
        }
    }
}