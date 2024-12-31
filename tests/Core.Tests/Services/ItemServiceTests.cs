using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using ListaCompras.Core.Models;
using ListaCompras.Core.Services;
using ListaCompras.Core.Data;
using ListaCompras.Core.Validators;

namespace ListaCompras.Core.Tests.Services
{
    public class ItemServiceTests
    {
        private readonly Mock<IItemRepository> _itemRepositoryMock;
        private readonly Mock<IListaRepository> _listaRepositoryMock;
        private readonly Mock<ICategoriaRepository> _categoriaRepositoryMock;
        private readonly Mock<IValidator<ItemModel>> _validatorMock;
        private readonly Mock<ILogger<ItemService>> _loggerMock;
        private readonly ItemService _service;

        public ItemServiceTests()
        {
            _itemRepositoryMock = new Mock<IItemRepository>();
            _listaRepositoryMock = new Mock<IListaRepository>();
            _categoriaRepositoryMock = new Mock<ICategoriaRepository>();
            _validatorMock = new Mock<IValidator<ItemModel>>();
            _loggerMock = new Mock<ILogger<ItemService>>();

            _service = new ItemService(
                _itemRepositoryMock.Object,
                _listaRepositoryMock.Object,
                _categoriaRepositoryMock.Object,
                _validatorMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GetByIdAsync_DeveRetornarItem_QuandoExistente()
        {
            // Arrange
            var itemId = 1;
            var item = new ItemModel { Id = itemId };
            _itemRepositoryMock.Setup(r => r.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            // Act
            var result = await _service.GetByIdAsync(itemId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(itemId);
            _itemRepositoryMock.Verify(r => r.GetByIdAsync(itemId), Times.Once);
        }

        [Fact]
        public async Task GetByListaAsync_DeveRetornarItens_QuandoListaExistente()
        {
            // Arrange
            var listaId = 1;
            var items = new List<ItemModel> 
            { 
                new ItemModel { Id = 1, ListaId = listaId },
                new ItemModel { Id = 2, ListaId = listaId }
            };

            _listaRepositoryMock.Setup(r => r.GetByIdAsync(listaId))
                .ReturnsAsync(new ListaModel { Id = listaId });
            _itemRepositoryMock.Setup(r => r.GetByListaIdAsync(listaId))
                .ReturnsAsync(items);

            // Act
            var result = await _service.GetByListaAsync(listaId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            _listaRepositoryMock.Verify(r => r.GetByIdAsync(listaId), Times.Once);
            _itemRepositoryMock.Verify(r => r.GetByListaIdAsync(listaId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_DeveCriarItem_QuandoValido()
        {
            // Arrange
            var item = new ItemModel
            {
                Nome = "Item Teste",
                Quantidade = 1,
                Unidade = "un"
            };

            _validatorMock.Setup(v => v.ValidateAsync(item))
                .ReturnsAsync(new List<string>());
            _itemRepositoryMock.Setup(r => r.AddAsync(item))
                .ReturnsAsync(item);

            // Act
            var result = await _service.CreateAsync(item);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(item);
            _validatorMock.Verify(v => v.ValidateAsync(item), Times.Once);
            _itemRepositoryMock.Verify(r => r.AddAsync(item), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_DeveAtualizarItem_QuandoValido()
        {
            // Arrange
            var itemId = 1;
            var item = new ItemModel
            {
                Id = itemId,
                Nome = "Item Atualizado",
                Quantidade = 2,
                Unidade = "kg"
            };

            _validatorMock.Setup(v => v.ValidateAsync(item))
                .ReturnsAsync(new List<string>());
            _itemRepositoryMock.Setup(r => r.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            // Act
            await _service.UpdateAsync(item);

            // Assert
            _validatorMock.Verify(v => v.ValidateAsync(item), Times.Once);
            _itemRepositoryMock.Verify(r => r.UpdateAsync(item), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_DeveRemoverItem_QuandoExistente()
        {
            // Arrange
            var itemId = 1;
            var item = new ItemModel { Id = itemId };
            _itemRepositoryMock.Setup(r => r.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            // Act
            await _service.DeleteAsync(itemId);

            // Assert
            _itemRepositoryMock.Verify(r => r.DeleteAsync(item), Times.Once);
        }

        [Fact]
        public async Task MarcarCompradoAsync_DeveMarcarItem_QuandoExistente()
        {
            // Arrange
            var itemId = 1;
            var precoReal = 10.5m;

            // Act
            await _service.MarcarCompradoAsync(itemId, precoReal);

            // Assert
            _itemRepositoryMock.Verify(r => r.MarcarCompradoAsync(itemId, precoReal), Times.Once);
        }

        [Fact]
        public async Task DesmarcarCompradoAsync_DeveDesmarcarItem_QuandoExistente()
        {
            // Arrange
            var itemId = 1;

            // Act
            await _service.DesmarcarCompradoAsync(itemId);

            // Assert
            _itemRepositoryMock.Verify(r => r.DesmarcarCompradoAsync(itemId), Times.Once);
        }

        [Fact]
        public async Task AtualizarPrecoEstimadoAsync_DeveAtualizarPreco_QuandoValido()
        {
            // Arrange
            var itemId = 1;
            var novoPreco = 15.75m;
            var item = new ItemModel { Id = itemId };

            _itemRepositoryMock.Setup(r => r.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            // Act
            await _service.AtualizarPrecoEstimadoAsync(itemId, novoPreco);

            // Assert
            item.PrecoEstimado.Should().Be(novoPreco);
            _itemRepositoryMock.Verify(r => r.UpdateAsync(item), Times.Once);
        }

        [Fact]
        public async Task MoverParaListaAsync_DeveMoverItem_QuandoListaExistente()
        {
            // Arrange
            var itemId = 1;
            var novaListaId = 2;
            var item = new ItemModel { Id = itemId };

            _itemRepositoryMock.Setup(r => r.GetByIdAsync(itemId))
                .ReturnsAsync(item);
            _listaRepositoryMock.Setup(r => r.GetByIdAsync(novaListaId))
                .ReturnsAsync(new ListaModel { Id = novaListaId });

            // Act
            await _service.MoverParaListaAsync(itemId, novaListaId);

            // Assert
            item.ListaId.Should().Be(novaListaId);
            _itemRepositoryMock.Verify(r => r.UpdateAsync(item), Times.Once);
        }
    }
}