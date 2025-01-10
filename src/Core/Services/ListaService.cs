using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ListaCompras.Core.Models;
using ListaCompras.Core.Data;

namespace ListaCompras.Core.Services
{
    public class ListaService : IListaService
    {
        private readonly IListaRepository _repository;
        private readonly IItemRepository _itemRepository;

        public ListaService(IListaRepository repository, IItemRepository itemRepository)
        {
            _repository = repository;
            _itemRepository = itemRepository;
        }

        public async Task<List<ListaModel>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<ListaModel> GetByIdAsync(int id)
        {
            var lista = await _repository.GetByIdAsync(id);
            if (lista != null)
            {
                lista.Itens = await _itemRepository.GetByListaAsync(id);
            }
            return lista;
        }

        public async Task<IEnumerable<ListaModel>> GetByUsuarioAsync(int usuarioId)
        {
            var todas = await _repository.GetAllAsync();
            return todas.Where(l => l.UsuarioId == usuarioId);
        }

        public async Task<ListaModel> CreateAsync(ListaModel lista)
        {
            lista.DataCriacao = DateTime.Now;
            lista.DataAtualizacao = DateTime.Now;
            lista.Status = StatusLista.EmAndamento;
            return await _repository.SaveAsync(lista);
        }

        public async Task UpdateAsync(ListaModel lista)
        {
            var existente = await _repository.GetByIdAsync(lista.Id);
            if (existente == null)
                throw new KeyNotFoundException($"Lista {lista.Id} não encontrada");

            lista.DataAtualizacao = DateTime.Now;
            await _repository.SaveAsync(lista);
        }

        public async Task DeleteAsync(int id)
        {
            var itens = await _itemRepository.GetByListaAsync(id);
            foreach (var item in itens)
            {
                await _itemRepository.DeleteAsync(item.Id);
            }
            await _repository.DeleteAsync(id);
        }

        public async Task AtualizarStatusAsync(int id, StatusLista status)
        {
            var lista = await _repository.GetByIdAsync(id);
            if (lista == null)
                throw new KeyNotFoundException($"Lista {id} não encontrada");

            lista.Status = status;
            lista.DataAtualizacao = DateTime.Now;
            await _repository.SaveAsync(lista);
        }

        public async Task ArquivarAsync(int id)
        {
            var lista = await _repository.GetByIdAsync(id);
            if (lista == null)
                throw new KeyNotFoundException($"Lista {id} não encontrada");

            lista.Arquivada = true;
            lista.DataArquivamento = DateTime.Now;
            await _repository.SaveAsync(lista);
        }

        public async Task<ListaModel> DuplicarAsync(int id, string novoNome)
        {
            var original = await GetByIdAsync(id);
            if (original == null)
                throw new KeyNotFoundException($"Lista {id} não encontrada");

            var nova = new ListaModel
            {
                Nome = novoNome,
                UsuarioId = original.UsuarioId,
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now,
                Status = StatusLista.EmAndamento
            };

            nova = await CreateAsync(nova);

            foreach (var item in original.Itens)
            {
                var novoItem = new ItemModel
                {
                    Nome = item.Nome,
                    Quantidade = item.Quantidade,
                    Unidade = item.Unidade,
                    PrecoEstimado = item.PrecoEstimado,
                    CategoriaId = item.CategoriaId,
                    ListaId = nova.Id
                };
                await _itemRepository.SaveAsync(novoItem);
            }

            return nova;
        }

        public async Task<decimal> CalcularTotalAsync(int id)
        {
            var lista = await GetByIdAsync(id);
            if (lista == null)
                throw new KeyNotFoundException($"Lista {id} não encontrada");

            return lista.Itens?.Sum(i => i.PrecoEstimado * i.Quantidade) ?? 0;
        }

        public async Task CompartilharAsync(int listaId, int usuarioId)
        {
            var lista = await _repository.GetByIdAsync(listaId);
            if (lista == null)
                throw new KeyNotFoundException($"Lista {listaId} não encontrada");

            // Lógica de compartilhamento aqui
            lista.Compartilhada = true;
            lista.UsuariosCompartilhados ??= new List<int>();
            if (!lista.UsuariosCompartilhados.Contains(usuarioId))
            {
                lista.UsuariosCompartilhados.Add(usuarioId);
            }

            await _repository.SaveAsync(lista);
        }

        public async Task<IEnumerable<ListaModel>> GetCompartilhadasAsync(int usuarioId)
        {
            var todas = await _repository.GetAllAsync();
            return todas.Where(l => l.Compartilhada && 
                                  l.UsuariosCompartilhados != null && 
                                  l.UsuariosCompartilhados.Contains(usuarioId));
        }
    }
}