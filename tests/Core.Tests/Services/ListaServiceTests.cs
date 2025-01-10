using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using ListaCompras.Core.Models;
using ListaCompras.Core.Services;

namespace ListaCompras.Tests.Services
{
    public class ListaServiceTests : TestBase
    {
        private readonly IListaService _listaService;
        private readonly IItemService _itemService;
        private readonly ICategoriaService _categoriaService;

        public ListaServiceTests()
        {
            _listaService = GetService<IListaService>();
            _itemService = GetService<IItemService>();
            _categoriaService = GetService<ICategoriaService>();
        }

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldSucceed()
        {
            // Arrange
            var lista = new ListaModel
            {
                Nome = "Lista de Supermercado",
                Descricao = "Compras do mês",
                UsuarioId = CurrentUserProvider.Object.GetCurrentUserId(),
                OrcamentoPrevisto = 500
            };

            // Act
            var result = await _listaService.CreateAsync(lista);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.Nome.Should().Be(lista.Nome);
            result.Status.Should().Be(StatusLista.EmEdicao);
            result.DataCriacao.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task CreateAsync_WithInvalidData_ShouldThrow()
        {
            // Arrange
            var lista = new ListaModel
            {
                Nome = "", // Inválido
                UsuarioId = 0 // Inválido
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _listaService.CreateAsync(lista));
        }

        [Fact]
        public async Task GetByUsuarioAsync_ShouldReturnCorrectLists()
        {
            // Arrange
            var usuarioId = CurrentUserProvider.Object.GetCurrentUserId();
            var listas = await CreateTestListsAsync(usuarioId, 3);

            // Act
            var result = await _listaService.GetByUsuarioAsync(usuarioId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Select(l => l.Id).Should().BeEquivalentTo(listas.Select(l => l.Id));
        }

        [Fact]
        public async Task GetByIdAsync_WithItems_ShouldLoadItems()
        {
            // Arrange
            var lista = await CreateTestListAsync();
            await CreateTestItemsAsync(lista.Id, 3);

            // Act
            var result = await _listaService.GetByIdAsync(lista.Id);

            // Assert
            result.Should().NotBeNull();
            result.Itens.Should().NotBeNull();
            result.Itens.Should().HaveCount(3);
        }

        [Fact]
        public async Task AtualizarStatusAsync_ShouldUpdateCorrectly()
        {
            // Arrange
            var lista = await CreateTestListAsync();

            // Act
            await _listaService.AtualizarStatusAsync(lista.Id, StatusLista.EmCompra);
            var result = await _listaService.GetByIdAsync(lista.Id);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(StatusLista.EmCompra);
        }

        [Fact]
        public async Task ArquivarAsync_ShouldArchiveList()
        {
            // Arrange
            var lista = await CreateTestListAsync();

            // Act
            await _listaService.ArquivarAsync(lista.Id);
            var result = await _listaService.GetByIdAsync(lista.Id);

            // Assert
            result.Should().NotBeNull();
            result.Arquivada.Should().BeTrue();
            result.DataArquivamento.Should().NotBeNull();
            result.DataArquivamento.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task DuplicarAsync_ShouldCopyAllItems()
        {
            // Arrange
            var lista = await CreateTestListAsync();
            var items = await CreateTestItemsAsync(lista.Id, 3);
            var novoNome = "Lista Duplicada";

            // Act
            var result = await _listaService.DuplicarAsync(lista.Id, novoNome);

            // Assert
            result.Should().NotBeNull();
            result.Nome.Should().Be(novoNome);
            result.Itens.Should().NotBeNull();
            result.Itens.Should().HaveCount(3);
            result.Itens.Select(i => i.Nome)
                .Should().BeEquivalentTo(items.Select(i => i.Nome));
        }

        [Fact]
        public async Task CalcularTotalAsync_ShouldComputeCorrectly()
        {
            // Arrange
            var lista = await CreateTestListAsync();
            await CreateTestItemAsync(lista.Id, quantidade: 2, precoEstimado: 10);
            await CreateTestItemAsync(lista.Id, quantidade: 3, precoEstimado: 5);

            // Act
            var total = await _listaService.CalcularTotalAsync(lista.Id);

            // Assert
            total.Should().Be(35); // (2 * 10) + (3 * 5)
        }

        [Fact]
        public async Task CompartilharAsync_ShouldShareList()
        {
            // Arrange
            var lista = await CreateTestListAsync();
            var usuarioCompartilhado = 2;

            // Act
            await _listaService.CompartilharAsync(lista.Id, usuarioCompartilhado);
            var result = await _listaService.GetByIdAsync(lista.Id);

            // Assert
            result.Should().NotBeNull();
            result.Compartilhada.Should().BeTrue();
            result.UsuariosCompartilhados.Should().Contain(usuarioCompartilhado);
        }

        [Fact]
        public async Task GetCompartilhadasAsync_ShouldReturnSharedLists()
        {
            // Arrange
            var usuarioId = 2;
            var lista = await CreateTestListAsync();
            await _listaService.CompartilharAsync(lista.Id, usuarioId);

            // Act
            var result = await _listaService.GetCompartilhadasAsync(usuarioId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Id.Should().Be(lista.Id);
        }

        #region Helpers

        private async Task<ListaModel> CreateTestListAsync(string nome = "Lista de Teste")
        {
            var lista = new ListaModel
            {
                Nome = nome,
                UsuarioId = CurrentUserProvider.Object.GetCurrentUserId()
            };

            return await _listaService.CreateAsync(lista);
        }

        private async Task<List<ListaModel>> CreateTestListsAsync(int usuarioId, int count)
        {
            var listas = new List<ListaModel>();
            for (int i = 0; i < count; i++)
            {
                var lista = new ListaModel
                {
                    Nome = $"Lista de Teste {i + 1}",
                    UsuarioId = usuarioId
                };
                listas.Add(await _listaService.CreateAsync(lista));
            }
            return listas;
        }

        private async Task<ItemModel> CreateTestItemAsync(
            int listaId,
            string nome = "Item de Teste",
            decimal quantidade = 1,
            decimal precoEstimado = 10)
        {
            var item = new ItemModel
            {
                Nome = nome,
                Quantidade = quantidade,
                Unidade = "Un",
                PrecoEstimado = precoEstimado,
                ListaId = listaId
            };

            return await _itemService.CreateAsync(item);
        }

        private async Task<List<ItemModel>> CreateTestItemsAsync(
            int listaId,
            int count)
        {
            var items = new List<ItemModel>();
            for (int i = 0; i < count; i++)
            {
                var item = await CreateTestItemAsync(
                    listaId,
                    nome: $"Item de Teste {i + 1}");
                items.Add(item);
            }
            return items;
        }

        #endregion
    }
}