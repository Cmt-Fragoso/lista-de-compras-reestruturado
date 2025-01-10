using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ListaCompras.Core.Models;
using ListaCompras.Core.Data;

namespace ListaCompras.Core.Services
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _repository;

        public ItemService(IItemRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ItemModel>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<IEnumerable<ItemModel>> GetByListaAsync(int listaId)
        {
            var todos = await _repository.GetAllAsync();
            return todos.Where(i => i.ListaId == listaId);
        }

        public async Task<ItemModel> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<ItemModel> CreateAsync(ItemModel item)
        {
            return await _repository.SaveAsync(item);
        }

        public async Task UpdateAsync(ItemModel item)
        {
            var existente = await _repository.GetByIdAsync(item.Id);
            if (existente == null)
                throw new KeyNotFoundException($"Item {item.Id} não encontrado");
            
            await _repository.SaveAsync(item);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task MarcarCompradoAsync(int id, decimal precoCompra)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                throw new KeyNotFoundException($"Item {id} não encontrado");

            item.IsComprado = true;
            item.PrecoCompra = precoCompra;
            item.DataCompra = DateTime.Now;

            await _repository.SaveAsync(item);
        }

        public async Task DesmarcarCompradoAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                throw new KeyNotFoundException($"Item {id} não encontrado");

            item.IsComprado = false;
            item.PrecoCompra = null;
            item.DataCompra = null;

            await _repository.SaveAsync(item);
        }

        public async Task AtualizarPrecoEstimadoAsync(int id, decimal precoEstimado)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                throw new KeyNotFoundException($"Item {id} não encontrado");

            item.PrecoEstimado = precoEstimado;
            await _repository.SaveAsync(item);
        }

        public async Task MoverParaListaAsync(int id, int novaListaId)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                throw new KeyNotFoundException($"Item {id} não encontrado");

            item.ListaId = novaListaId;
            await _repository.SaveAsync(item);
        }

        public async Task AtualizarCategoriaAsync(int id, int categoriaId)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                throw new KeyNotFoundException($"Item {id} não encontrado");

            item.CategoriaId = categoriaId;
            await _repository.SaveAsync(item);
        }
    }
}