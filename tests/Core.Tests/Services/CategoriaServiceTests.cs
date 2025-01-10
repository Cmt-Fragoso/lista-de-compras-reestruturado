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
    public class CategoriaServiceTests : TestBase
    {
        private readonly ICategoriaService _categoriaService;
        private readonly IItemService _itemService;

        public CategoriaServiceTests()
        {
            _categoriaService = GetService<ICategoriaService>();
            _itemService = GetService<IItemService>();
        }

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldSucceed()
        {
            // Arrange
            var categoria = new CategoriaModel
            {
                Nome = "Alimentos",
                Descricao = "Produtos alimentícios",
                Cor = "#FF0000",
                Icone = "food",
                Ordem = 1
            };

            // Act
            var result = await _categoriaService.CreateAsync(categoria);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.Nome.Should().Be(categoria.Nome);
            result.DataCriacao.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task CreateAsync_WithInvalidData_ShouldThrow()
        {
            // Arrange
            var categoria = new CategoriaModel
            {
                Nome = "", // Inválido
                Cor = "INVALID" // Inválido
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _categoriaService.CreateAsync(categoria));
        }

        [Fact]
        public async Task CreateSubcategoriaAsync_ShouldCreateHierarchy()
        {
            // Arrange
            var categoriaPai = await CreateTestCategoriaAsync();
            var subcategoria = new CategoriaModel
            {
                Nome = "Subcategoria",
                Cor = "#00FF00",
                CategoriaPaiId = categoriaPai.Id
            };

            // Act
            var result = await _categoriaService.CreateAsync(subcategoria);
            var pai = await _categoriaService.GetByIdAsync(categoriaPai.Id);

            // Assert
            result.Should().NotBeNull();
            result.CategoriaPaiId.Should().Be(categoriaPai.Id);
            pai.SubCategorias.Should().Contain(s => s.Id == result.Id);
        }

        [Fact]
        public async Task GetCategoriasRaizAsync_ShouldReturnTopLevelOnly()
        {
            // Arrange
            var categoria1 = await CreateTestCategoriaAsync("Categoria 1");
            var categoria2 = await CreateTestCategoriaAsync("Categoria 2");
            var subcategoria = await CreateTestCategoriaAsync("Sub", categoria1.Id);

            // Act
            var result = await _categoriaService.GetCategoriasRaizAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(c => c.CategoriaPaiId == null).Should().BeTrue();
        }

        [Fact]
        public async Task GetHierarquiaCompletaAsync_ShouldReturnFullTree()
        {
            // Arrange
            var raiz = await CreateTestCategoriaAsync("Raiz");
            var nivel1 = await CreateTestCategoriaAsync("Nível 1", raiz.Id);
            var nivel2 = await CreateTestCategoriaAsync("Nível 2", nivel1.Id);

            // Act
            var result = await _categoriaService.GetHierarquiaCompletaAsync();
            var categoriaRaiz = result.First(c => c.Id == raiz.Id);

            // Assert
            result.Should().NotBeNull();
            categoriaRaiz.SubCategorias.Should().HaveCount(1);
            categoriaRaiz.SubCategorias.First().SubCategorias.Should().HaveCount(1);
        }

        [Fact]
        public async Task MoverCategoriaAsync_ShouldUpdateHierarchy()
        {
            // Arrange
            var categoriaOriginal = await CreateTestCategoriaAsync("Original");
            var categoriaDestino = await CreateTestCategoriaAsync("Destino");
            var categoria = await CreateTestCategoriaAsync("Mover", categoriaOriginal.Id);

            // Act
            await _categoriaService.MoverCategoriaAsync(categoria.Id, categoriaDestino.Id);
            var result = await _categoriaService.GetByIdAsync(categoria.Id);
            var original = await _categoriaService.GetByIdAsync(categoriaOriginal.Id);
            var destino = await _categoriaService.GetByIdAsync(categoriaDestino.Id);

            // Assert
            result.CategoriaPaiId.Should().Be(categoriaDestino.Id);
            original.SubCategorias.Should().NotContain(c => c.Id == categoria.Id);
            destino.SubCategorias.Should().Contain(c => c.Id == categoria.Id);
        }

        [Fact]
        public async Task MoverCategoriaAsync_ShouldPreventCycles()
        {
            // Arrange
            var pai = await CreateTestCategoriaAsync("Pai");
            var filho = await CreateTestCategoriaAsync("Filho", pai.Id);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _categoriaService.MoverCategoriaAsync(pai.Id, filho.Id));
        }

        [Fact]
        public async Task DeleteAsync_WithSubcategorias_ShouldFail()
        {
            // Arrange
            var pai = await CreateTestCategoriaAsync("Pai");
            await CreateTestCategoriaAsync("Filho", pai.Id);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _categoriaService.DeleteAsync(pai.Id));
        }

        [Fact]
        public async Task DeleteAsync_WithItems_ShouldFail()
        {
            // Arrange
            var categoria = await CreateTestCategoriaAsync();
            await CreateTestItemAsync(categoria.Id);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _categoriaService.DeleteAsync(categoria.Id));
        }

        [Fact]
        public async Task AtualizarOrdemAsync_ShouldReorderCategories()
        {
            // Arrange
            var ordem = new[]
            {
                await CreateTestCategoriaAsync("Cat 1"),
                await CreateTestCategoriaAsync("Cat 2"),
                await CreateTestCategoriaAsync("Cat 3")
            };

            // Inverter ordem
            var novaOrdem = ordem.Select((c, i) => (
                CategoriaId: c.Id,
                NovaOrdem: ordem.Length - i - 1
            )).ToList();

            // Act
            await _categoriaService.AtualizarOrdemAsync(novaOrdem);
            var categorias = await _categoriaService.GetCategoriasRaizAsync();

            // Assert
            categorias.Should().BeInDescendingOrder(c => c.Ordem);
        }

        [Fact]
        public async Task GetEstatisticasAsync_ShouldCalculateCorrectly()
        {
            // Arrange
            var categoria = await CreateTestCategoriaAsync();
            await CreateTestItemAsync(categoria.Id, quantidade: 2, precoEstimado: 10);
            await CreateTestItemAsync(categoria.Id, quantidade: 3, precoEstimado: 5);

            // Act
            var result = await _categoriaService.GetEstatisticasAsync(categoria.Id);

            // Assert
            result.Should().NotBeNull();
            result.TotalItens.Should().Be(2);
            result.ValorTotal.Should().Be(35); // (2 * 10) + (3 * 5)
        }

        #region Helpers

        private async Task<CategoriaModel> CreateTestCategoriaAsync(
            string nome = "Categoria de Teste",
            int? categoriaPaiId = null)
        {
            var categoria = new CategoriaModel
            {
                Nome = nome,
                Cor = "#FF0000",
                Icone = "test",
                CategoriaPaiId = categoriaPaiId
            };

            return await _categoriaService.CreateAsync(categoria);
        }

        private async Task<ItemModel> CreateTestItemAsync(
            int categoriaId,
            decimal quantidade = 1,
            decimal precoEstimado = 10)
        {
            var lista = await CreateTestListAsync();
            var item = new ItemModel
            {
                Nome = "Item de Teste",
                Quantidade = quantidade,
                Unidade = "Un",
                PrecoEstimado = precoEstimado,
                CategoriaId = categoriaId,
                ListaId = lista.Id
            };

            return await _itemService.CreateAsync(item);
        }

        private async Task<ListaModel> CreateTestListAsync()
        {
            using var context = DbContext;
            var lista = new ListaModel
            {
                Nome = "Lista de Teste",
                UsuarioId = CurrentUserProvider.Object.GetCurrentUserId()
            };

            context.Lists.Add(lista);
            await context.SaveChangesAsync();
            return lista;
        }

        #endregion
    }
}