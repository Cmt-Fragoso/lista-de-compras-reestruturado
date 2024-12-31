using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ListaCompras.Core.Models;
using ListaCompras.Core.Validators;
using ListaCompras.Core.Data;
using Microsoft.Extensions.Logging;

namespace ListaCompras.Core.Services
{
    /// <summary>
    /// Implementação do serviço de itens
    /// </summary>
    public class ItemService : BaseService<ItemModel>, IItemService
    {
        private readonly IItemRepository _itemRepository;
        private readonly IListaRepository _listaRepository;
        private readonly ICategoriaRepository _categoriaRepository;

        public ItemService(
            IItemRepository itemRepository,
            IListaRepository listaRepository,
            ICategoriaRepository categoriaRepository,
            IValidator<ItemModel> validator,
            ILogger<ItemService> logger)
            : base(validator, logger)
        {
            _itemRepository = itemRepository;
            _listaRepository = listaRepository;
            _categoriaRepository = categoriaRepository;
        }

        public async Task<ItemModel> GetByIdAsync(int id)
        {
            return await ExecuteOperationAsync(
                async () => await _itemRepository.GetByIdAsync(id),
                $"Obter item {id}");
        }

        public async Task<IEnumerable<ItemModel>> GetByListaAsync(int listaId)
        {
            // Verifica se a lista existe
            var lista = await _listaRepository.GetByIdAsync(listaId);
            if (lista == null)
                throw new NotFoundException($"Lista {listaId} não encontrada");

            return await ExecuteOperationAsync(
                async () => await _itemRepository.GetByListaIdAsync(listaId),
                $"Obter itens da lista {listaId}");
        }

        public async Task<ItemModel> CreateAsync(ItemModel item)
        {
            await ValidateAndThrowAsync(item);

            // Define data de criação/atualização
            item.DataCriacao = DateTime.Now;
            item.DataAtualizacao = DateTime.Now;

            return await ExecuteOperationAsync(
                async () => await _itemRepository.AddAsync(item),
                "Criar novo item");
        }

        public async Task UpdateAsync(ItemModel item)
        {
            await ValidateAndThrowAsync(item);

            var existingItem = await _itemRepository.GetByIdAsync(item.Id);
            if (existingItem == null)
                throw new NotFoundException($"Item {item.Id} não encontrado");

            // Preserva data de criação e atualiza data de modificação
            item.DataCriacao = existingItem.DataCriacao;
            item.DataAtualizacao = DateTime.Now;

            await ExecuteOperationAsync(
                async () => await _itemRepository.UpdateAsync(item),
                $"Atualizar item {item.Id}");
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _itemRepository.GetByIdAsync(id);
            if (item == null)
                throw new NotFoundException($"Item {id} não encontrado");

            await ExecuteOperationAsync(
                async () => await _itemRepository.DeleteAsync(item),
                $"Excluir item {id}");
        }

        public async Task MarcarCompradoAsync(int id, decimal precoReal)
        {
            if (precoReal < 0)
                throw new ValidationException("Preço real não pode ser negativo");

            await ExecuteOperationAsync(
                async () => await _itemRepository.MarcarCompradoAsync(id, precoReal),
                $"Marcar item {id} como comprado");
        }

        public async Task DesmarcarCompradoAsync(int id)
        {
            await ExecuteOperationAsync(
                async () => await _itemRepository.DesmarcarCompradoAsync(id),
                $"Desmarcar item {id} como comprado");
        }

        public async Task AtualizarPrecoEstimadoAsync(int id, decimal novoPreco)
        {
            if (novoPreco < 0)
                throw new ValidationException("Preço estimado não pode ser negativo");

            var item = await _itemRepository.GetByIdAsync(id);
            if (item == null)
                throw new NotFoundException($"Item {id} não encontrado");

            item.PrecoEstimado = novoPreco;
            item.DataAtualizacao = DateTime.Now;

            await ExecuteOperationAsync(
                async () => await _itemRepository.UpdateAsync(item),
                $"Atualizar preço do item {id}");
        }

        public async Task MoverParaListaAsync(int id, int novaListaId)
        {
            // Verifica se a nova lista existe
            var lista = await _listaRepository.GetByIdAsync(novaListaId);
            if (lista == null)
                throw new NotFoundException($"Lista {novaListaId} não encontrada");

            var item = await _itemRepository.GetByIdAsync(id);
            if (item == null)
                throw new NotFoundException($"Item {id} não encontrado");

            item.ListaId = novaListaId;
            item.DataAtualizacao = DateTime.Now;

            await ExecuteOperationAsync(
                async () => await _itemRepository.UpdateAsync(item),
                $"Mover item {id} para lista {novaListaId}");
        }

        public async Task AtualizarCategoriaAsync(int id, int novaCategoriaId)
        {
            // Verifica se a nova categoria existe
            var categoria = await _categoriaRepository.GetByIdAsync(novaCategoriaId);
            if (categoria == null)
                throw new NotFoundException($"Categoria {novaCategoriaId} não encontrada");

            var item = await _itemRepository.GetByIdAsync(id);
            if (item == null)
                throw new NotFoundException($"Item {id} não encontrado");

            item.CategoriaId = novaCategoriaId;
            item.DataAtualizacao = DateTime.Now;

            await ExecuteOperationAsync(
                async () => await _itemRepository.UpdateAsync(item),
                $"Atualizar categoria do item {id}");
        }
    }

    /// <summary>
    /// Exceção para quando uma entidade não é encontrada
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}