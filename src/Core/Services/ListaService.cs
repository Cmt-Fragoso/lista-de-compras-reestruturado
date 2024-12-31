using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ListaCompras.Core.Models;
using ListaCompras.Core.Validators;
using ListaCompras.Core.Data;
using Microsoft.Extensions.Logging;

namespace ListaCompras.Core.Services
{
    /// <summary>
    /// Implementação do serviço de listas
    /// </summary>
    public class ListaService : BaseService<ListaModel>, IListaService
    {
        private readonly IListaRepository _listaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IItemRepository _itemRepository;

        public ListaService(
            IListaRepository listaRepository,
            IUsuarioRepository usuarioRepository,
            IItemRepository itemRepository,
            IValidator<ListaModel> validator,
            ILogger<ListaService> logger)
            : base(validator, logger)
        {
            _listaRepository = listaRepository;
            _usuarioRepository = usuarioRepository;
            _itemRepository = itemRepository;
        }

        public async Task<ListaModel> GetByIdAsync(int id)
        {
            return await ExecuteOperationAsync(
                async () => await _listaRepository.GetByIdAsync(id),
                $"Obter lista {id}");
        }

        public async Task<IEnumerable<ListaModel>> GetByUsuarioAsync(int usuarioId)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
            if (usuario == null)
                throw new NotFoundException($"Usuário {usuarioId} não encontrado");

            return await ExecuteOperationAsync(
                async () => await _listaRepository.GetByUsuarioIdAsync(usuarioId),
                $"Obter listas do usuário {usuarioId}");
        }

        public async Task<ListaModel> CreateAsync(ListaModel lista)
        {
            await ValidateAndThrowAsync(lista);

            lista.DataCriacao = DateTime.Now;
            lista.DataAtualizacao = DateTime.Now;
            lista.Status = StatusLista.EmEdicao;

            return await ExecuteOperationAsync(
                async () => await _listaRepository.AddAsync(lista),
                "Criar nova lista");
        }

        public async Task UpdateAsync(ListaModel lista)
        {
            await ValidateAndThrowAsync(lista);

            var existingLista = await _listaRepository.GetByIdAsync(lista.Id);
            if (existingLista == null)
                throw new NotFoundException($"Lista {lista.Id} não encontrada");

            lista.DataCriacao = existingLista.DataCriacao;
            lista.DataAtualizacao = DateTime.Now;

            await ExecuteOperationAsync(
                async () => await _listaRepository.UpdateAsync(lista),
                $"Atualizar lista {lista.Id}");
        }

        public async Task DeleteAsync(int id)
        {
            var lista = await _listaRepository.GetByIdAsync(id);
            if (lista == null)
                throw new NotFoundException($"Lista {id} não encontrada");

            // Verifica se pode ser excluída
            if (lista.Status == StatusLista.EmCompra)
                throw new InvalidOperationException("Não é possível excluir uma lista em compra");

            await ExecuteOperationAsync(
                async () => await _listaRepository.DeleteAsync(lista),
                $"Excluir lista {id}");
        }

        public async Task AtualizarStatusAsync(int id, StatusLista novoStatus)
        {
            var lista = await _listaRepository.GetByIdAsync(id);
            if (lista == null)
                throw new NotFoundException($"Lista {id} não encontrada");

            // Validações de transição de status
            ValidarTransicaoStatus(lista.Status, novoStatus);

            await ExecuteOperationAsync(
                async () => await _listaRepository.AtualizarStatusAsync(id, novoStatus),
                $"Atualizar status da lista {id}");
        }

        private void ValidarTransicaoStatus(StatusLista statusAtual, StatusLista novoStatus)
        {
            switch (statusAtual)
            {
                case StatusLista.EmEdicao:
                    if (novoStatus != StatusLista.Ativa && novoStatus != StatusLista.Arquivada)
                        throw new InvalidOperationException($"Não é possível mudar status de {statusAtual} para {novoStatus}");
                    break;
                case StatusLista.Ativa:
                    if (novoStatus != StatusLista.EmCompra && novoStatus != StatusLista.Arquivada)
                        throw new InvalidOperationException($"Não é possível mudar status de {statusAtual} para {novoStatus}");
                    break;
                case StatusLista.EmCompra:
                    if (novoStatus != StatusLista.Concluida && novoStatus != StatusLista.Ativa)
                        throw new InvalidOperationException($"Não é possível mudar status de {statusAtual} para {novoStatus}");
                    break;
                case StatusLista.Concluida:
                    if (novoStatus != StatusLista.Arquivada)
                        throw new InvalidOperationException($"Não é possível mudar status de {statusAtual} para {novoStatus}");
                    break;
                case StatusLista.Arquivada:
                    throw new InvalidOperationException("Não é possível mudar o status de uma lista arquivada");
            }
        }

        public async Task ArquivarAsync(int id)
        {
            var lista = await _listaRepository.GetByIdAsync(id);
            if (lista == null)
                throw new NotFoundException($"Lista {id} não encontrada");

            await ExecuteOperationAsync(
                async () => await _listaRepository.ArquivarAsync(id),
                $"Arquivar lista {id}");
        }

        public async Task<ListaModel> DuplicarAsync(int id, string novoNome = null)
        {
            var lista = await _listaRepository.GetByIdAsync(id);
            if (lista == null)
                throw new NotFoundException($"Lista {id} não encontrada");

            // Cria nova lista com dados básicos
            var novaLista = new ListaModel
            {
                Nome = novoNome ?? $"Cópia de {lista.Nome}",
                Descricao = lista.Descricao,
                UsuarioId = lista.UsuarioId,
                DataPrevista = null,
                OrcamentoPrevisto = lista.OrcamentoPrevisto,
                Status = StatusLista.EmEdicao,
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now
            };

            // Salva nova lista
            novaLista = await _listaRepository.AddAsync(novaLista);

            // Duplica os itens
            var itens = await _itemRepository.GetByListaIdAsync(id);
            foreach (var item in itens)
            {
                var novoItem = new ItemModel
                {
                    Nome = item.Nome,
                    Descricao = item.Descricao,
                    Quantidade = item.Quantidade,
                    Unidade = item.Unidade,
                    PrecoEstimado = item.PrecoEstimado,
                    CategoriaId = item.CategoriaId,
                    ListaId = novaLista.Id,
                    Comprado = false,
                    DataCriacao = DateTime.Now,
                    DataAtualizacao = DateTime.Now
                };

                await _itemRepository.AddAsync(novoItem);
            }

            return novaLista;
        }

        public async Task<decimal> CalcularTotalAsync(int id)
        {
            return await ExecuteOperationAsync(
                async () => await _listaRepository.CalcularTotalRealAsync(id),
                $"Calcular total da lista {id}");
        }

        public async Task CompartilharAsync(int id, int usuarioDestinoId)
        {
            var lista = await _listaRepository.GetByIdAsync(id);
            if (lista == null)
                throw new NotFoundException($"Lista {id} não encontrada");

            var usuarioDestino = await _usuarioRepository.GetByIdAsync(usuarioDestinoId);
            if (usuarioDestino == null)
                throw new NotFoundException($"Usuário destino {usuarioDestinoId} não encontrado");

            // Será implementado quando o sistema de compartilhamento estiver pronto
            throw new NotImplementedException("Sistema de compartilhamento ainda não implementado");
        }

        public async Task<IEnumerable<ListaModel>> GetCompartilhadasAsync(int usuarioId)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
            if (usuario == null)
                throw new NotFoundException($"Usuário {usuarioId} não encontrado");

            // Será implementado quando o sistema de compartilhamento estiver pronto
            return new List<ListaModel>();
        }
    }
}