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
    public class PrecoServiceTests : TestBase
    {
        private readonly IPrecoService _precoService;
        private readonly IItemService _itemService;
        private readonly IListaService _listaService;

        public PrecoServiceTests()
        {
            _precoService = GetService<IPrecoService>();
            _itemService = GetService<IItemService>();
            _listaService = GetService<IListaService>();
        }

        [Fact]
        public async Task RegistrarPrecoAsync_WithValidData_ShouldSucceed()
        {
            // Arrange
            var item = await CreateTestItemAsync();
            var preco = new PrecoModel
            {
                ItemId = item.Id,
                Valor = 9.99m,
                Local = "Supermercado Teste",
                Observacoes = "Preço promocional"
            };

            // Act
            var result = await _precoService.RegistrarPrecoAsync(preco);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.Data.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.Valor.Should().Be(preco.Valor);
        }

        [Fact]
        public async Task RegistrarPrecoAsync_WithInvalidData_ShouldThrow()
        {
            // Arrange
            var preco = new PrecoModel
            {
                ItemId = 0, // Inválido
                Valor = -1 // Inválido
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _precoService.RegistrarPrecoAsync(preco));
        }

        [Fact]
        public async Task GetByItemIdAsync_ShouldReturnOrderedByDate()
        {
            // Arrange
            var item = await CreateTestItemAsync();
            var precos = new List<decimal> { 10.0m, 9.50m, 11.0m };
            
            foreach (var valor in precos)
            {
                await RegistrarPrecoTestAsync(item.Id, valor);
                await Task.Delay(10); // Garante ordem diferente
            }

            // Act
            var result = await _precoService.GetByItemIdAsync(item.Id);

            // Assert
            result.Should().NotBeNull()
                .And.HaveCount(3)
                .And.BeInDescendingOrder(p => p.Data);
        }

        [Fact]
        public async Task GetHistoricoAsync_ShouldFilterByDateRange()
        {
            // Arrange
            var item = await CreateTestItemAsync();
            var now = DateTime.UtcNow;
            
            // Preços em diferentes datas
            await RegistrarPrecoTestAsync(item.Id, 10.0m, now.AddDays(-10));
            await RegistrarPrecoTestAsync(item.Id, 11.0m, now.AddDays(-5));
            await RegistrarPrecoTestAsync(item.Id, 12.0m, now);

            // Act
            var result = await _precoService.GetHistoricoAsync(
                item.Id,
                now.AddDays(-7),
                now);

            // Assert
            result.Should().NotBeNull()
                .And.HaveCount(2)
                .And.OnlyContain(p => p.Data >= now.AddDays(-7));
        }

        [Fact]
        public async Task GetUltimoPrecoAsync_ShouldReturnMostRecent()
        {
            // Arrange
            var item = await CreateTestItemAsync();
            await RegistrarPrecoTestAsync(item.Id, 10.0m);
            await Task.Delay(10);
            await RegistrarPrecoTestAsync(item.Id, 11.0m);

            // Act
            var result = await _precoService.GetUltimoPrecoAsync(item.Id);

            // Assert
            result.Should().NotBeNull();
            result.Valor.Should().Be(11.0m);
        }

        [Fact]
        public async Task RegistrarMultiplosAsync_ShouldHandleBulkInsert()
        {
            // Arrange
            var item = await CreateTestItemAsync();
            var precos = new List<PrecoModel>
            {
                new() { ItemId = item.Id, Valor = 10.0m },
                new() { ItemId = item.Id, Valor = 11.0m },
                new() { ItemId = item.Id, Valor = 12.0m }
            };

            // Act
            var result = await _precoService.RegistrarMultiplosAsync(precos);

            // Assert
            result.Should().NotBeNull()
                .And.HaveCount(3)
                .And.OnlyContain(p => p.Id > 0);
        }

        [Fact]
        public async Task CalcularMediaPeriodoAsync_ShouldComputeCorrectly()
        {
            // Arrange
            var item = await CreateTestItemAsync();
            var inicio = DateTime.UtcNow.AddDays(-30);
            var fim = DateTime.UtcNow;

            await RegistrarPrecoTestAsync(item.Id, 10.0m, inicio.AddDays(5));
            await RegistrarPrecoTestAsync(item.Id, 12.0m, inicio.AddDays(15));
            await RegistrarPrecoTestAsync(item.Id, 14.0m, inicio.AddDays(25));

            // Act
            var result = await _precoService.CalcularMediaPeriodoAsync(item.Id, inicio, fim);

            // Assert
            result.Should().Be(12.0m); // Média de 10, 12 e 14
        }

        [Fact]
        public async Task GetPromocionaisAtivosAsync_ShouldFilterCorrectly()
        {
            // Arrange
            var item = await CreateTestItemAsync();
            
            // Preço normal
            await RegistrarPrecoTestAsync(item.Id, 10.0m, isPromocional: false);
            
            // Preço promocional expirado
            await RegistrarPrecoTestAsync(item.Id, 8.0m, 
                isPromocional: true,
                dataFimPromocao: DateTime.UtcNow.AddDays(-1));
            
            // Preço promocional ativo
            await RegistrarPrecoTestAsync(item.Id, 9.0m,
                isPromocional: true,
                dataFimPromocao: DateTime.UtcNow.AddDays(1));

            // Act
            var result = await _precoService.GetPromocionaisAtivosAsync();

            // Assert
            result.Should().NotBeNull()
                .And.HaveCount(1)
                .And.OnlyContain(p => p.Valor == 9.0m);
        }

        [Fact]
        public async Task AnalisarTendenciaAsync_ShouldDetectTrend()
        {
            // Arrange
            var item = await CreateTestItemAsync();
            var dataBase = DateTime.UtcNow.AddDays(-30);

            // Preços em alta
            for (int i = 0; i < 5; i++)
            {
                await RegistrarPrecoTestAsync(
                    item.Id,
                    10.0m + i, // Aumenta 1 real cada vez
                    dataBase.AddDays(i * 5));
            }

            // Act
            var (variacao, tendenciaAlta) = 
                await _precoService.AnalisarTendenciaAsync(item.Id, 30);

            // Assert
            tendenciaAlta.Should().BeTrue();
            variacao.Should().BeGreaterThan(0);
        }

        #region Helpers

        private async Task<ItemModel> CreateTestItemAsync()
        {
            var lista = await CreateTestListAsync();
            var item = new ItemModel
            {
                Nome = "Item de Teste",
                Quantidade = 1,
                Unidade = "Un",
                PrecoEstimado = 10,
                ListaId = lista.Id
            };

            return await _itemService.CreateAsync(item);
        }

        private async Task<ListaModel> CreateTestListAsync()
        {
            var lista = new ListaModel
            {
                Nome = "Lista de Teste",
                UsuarioId = CurrentUserProvider.Object.GetCurrentUserId()
            };

            return await _listaService.CreateAsync(lista);
        }

        private async Task<PrecoModel> RegistrarPrecoTestAsync(
            int itemId,
            decimal valor,
            DateTime? data = null,
            bool isPromocional = false,
            DateTime? dataFimPromocao = null)
        {
            var preco = new PrecoModel
            {
                ItemId = itemId,
                Valor = valor,
                Data = data ?? DateTime.UtcNow,
                IsPromocional = isPromocional,
                DataFimPromocao = dataFimPromocao,
                Local = "Local Teste"
            };

            return await _precoService.RegistrarPrecoAsync(preco);
        }

        #endregion
    }
}