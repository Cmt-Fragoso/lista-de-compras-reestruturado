using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using ListaCompras.Core.Models;
using ListaCompras.Core.Services;

namespace ListaCompras.Tests.Services
{
    public class ItemServiceTests : TestBase
    {
        private readonly IItemService _itemService;
        private readonly IListaService _listaService;
        private readonly ICategoriaService _categoriaService;

        public ItemServiceTests()
        {
            _itemService = GetService<IItemService>();
            _listaService = GetService<IListaService>();
            _categoriaService = GetService<ICategoriaService>();
        }

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldSucceed()
        {
            // Arrange
            var lista = await CreateListaAsync();
            var categoria = await CreateCategoriaAsync();

            var item = new ItemModel
            {
                Nome = "Item de Teste",
                Descricao = "Descrição do item",
                Quantidade = 1,
                Unidade = "Un",
                PrecoEstimado = 10.50m,
                CategoriaId = categoria.Id,
                ListaId = lista.Id
            };

            // Act
            var result = await _itemService.CreateAsync(item);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.Nome.Should().Be(item.Nome);
            result.PrecoEstimado.Should().Be(item.PrecoEstimado);
            result.Quantidade.Should().Be(item.Quantidade);
            result.DataAtualizacao.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.DataCriacao.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task CreateAsync_WithInvalidData_ShouldThrow()
        {
            // Arrange
            var item = new ItemModel
            {
                Nome = "", // Inválido
                Quantidade = 0, // Inválido
                PrecoEstimado = -1 // Inválido
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _itemService.CreateAsync(item));
        }

        [Fact]
        public async Task GetByListaAsync_ShouldReturnCorrectItems()
        {
            // Arrange
            var lista = await CreateListaAsync();
            var items = await CreateTestItemsAsync(lista.Id, 3);

            // Act
            var result = await _itemService.GetByListaAsync(lista.Id);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Select(i => i.Id).Should().BeEquivalentTo(items.Select(i => i.Id));
        }

        [Fact]
        public async Task MarcarCompradoAsync_ShouldUpdateItemCorrectly()
        {
            // Arrange
            var lista = await CreateListaAsync();
            var item = await CreateTestItemAsync(lista.Id);
            var precoCompra = 9.99m;

            // Act
            await _itemService.MarcarCompradoAsync(item.Id, precoCompra);
            var result = await _itemService.GetByIdAsync(item.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsComprado.Should().BeTrue();
            result.PrecoCompra.Should().Be(precoCompra);
            result.DataCompra.Should().NotBeNull();
            result.DataCompra.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveItem()
        {
            // Arrange
            var lista = await CreateListaAsync();
            var item = await CreateTestItemAsync(lista.Id);

            // Act
            await _itemService.DeleteAsync(item.Id);
            var result = await _itemService.GetByIdAsync(item.Id);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_WithValidChanges_ShouldSucceed()
        {
            // Arrange
            var lista = await CreateListaAsync();
            var item = await CreateTestItemAsync(lista.Id);
            
            item.Nome = "Nome Atualizado";
            item.PrecoEstimado = 15.99m;
            item.Quantidade = 2;

            // Act
            await _itemService.UpdateAsync(item);
            var result = await _itemService.GetByIdAsync(item.Id);

            // Assert
            result.Should().NotBeNull();
            result.Nome.Should().Be(item.Nome);
            result.PrecoEstimado.Should().Be(item.PrecoEstimado);
            result.Quantidade.Should().Be(item.Quantidade);
            result.DataAtualizacao.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidChanges_ShouldThrow()
        {
            // Arrange
            var lista = await CreateListaAsync();
            var item = await CreateTestItemAsync(lista.Id);
            
            item.Nome = ""; // Inválido
            item.PrecoEstimado = -1; // Inválido

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _itemService.UpdateAsync(item));
        }

        [Fact]
        public async Task UpdateAsync_WhenComprado_ShouldNotAllow()
        {
            // Arrange
            var lista = await CreateListaAsync();
            var item = await CreateTestItemAsync(lista.Id);
            
            // Marca como comprado
            await _itemService.MarcarCompradoAsync(item.Id, 9.99m);
            
            item.Nome = "Tentativa de Alteração";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _itemService.UpdateAsync(item));
        }

        [Fact]
        public async Task GetByCategoriaAsync_ShouldReturnCorrectItems()
        {
            // Arrange
            var lista = await CreateListaAsync();
            var categoria = await CreateCategoriaAsync();
            var items = await CreateTestItemsAsync(lista.Id, 3, categoria.Id);

            // Act
            var result = await _itemService.GetByCategoriaAsync(categoria.Id);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.All(i => i.CategoriaId == categoria.Id).Should().BeTrue();
        }

        [Fact]
        public async Task SearchAsync_ShouldFindCorrectItems()
        {
            // Arrange
            var lista = await CreateListaAsync();
            await CreateTestItemAsync(lista.Id, nome: "Arroz Integral");
            await CreateTestItemAsync(lista.Id, nome: "Feijão");
            await CreateTestItemAsync(lista.Id, nome: "Arroz Branco");

            // Act
            var result = await _itemService.SearchAsync("Arroz");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(i => i.Nome.Contains("Arroz")).Should().BeTrue();
        }

        [Fact]
        public async Task CalcularTotalAsync_ShouldComputeCorrectly()
        {
            // Arrange
            var lista = await CreateListaAsync();
            await CreateTestItemAsync(lista.Id, quantidade: 2, precoEstimado: 10);
            await CreateTestItemAsync(lista.Id, quantidade: 3, precoEstimado: 5);

            // Act
            var total = await _itemService.CalcularTotalAsync(lista.Id);

            // Assert
            total.Should().Be(35); // (2 * 10) + (3 * 5)
        }

        #region Helpers

        private async Task<ListaModel> CreateListaAsync()
        {
            var lista = new ListaModel
            {
                Nome = "Lista de Teste",
                UsuarioId = CurrentUserProvider.Object.GetCurrentUserId()
            };

            return await _listaService.CreateAsync(lista);
        }

        private async Task<CategoriaModel> CreateCategoriaAsync()
        {
            var categoria = new CategoriaModel
            {
                Nome = "Categoria de Teste",
                Cor = "#FF0000"
            };

            return await _categoriaService.CreateAsync(categoria);
        }

        private async Task<ItemModel> CreateTestItemAsync(
            int listaId, 
            string nome = "Item de Teste",
            decimal quantidade = 1,
            decimal precoEstimado = 10,
            int? categoriaId = null)
        {
            var item = new ItemModel
            {
                Nome = nome,
                Quantidade = quantidade,
                Unidade = "Un",
                PrecoEstimado = precoEstimado,
                CategoriaId = categoriaId,
                ListaId = listaId
            };

            return await _itemService.CreateAsync(item);
        }

        private async Task<List<ItemModel>> CreateTestItemsAsync(
            int listaId, 
            int count, 
            int? categoriaId = null)
        {
            var items = new List<ItemModel>();
            for (int i = 0; i < count; i++)
            {
                var item = await CreateTestItemAsync(
                    listaId,
                    nome: $"Item de Teste {i + 1}",
                    categoriaId: categoriaId);
                items.Add(item);
            }
            return items;
        }

        #endregion
    }
}