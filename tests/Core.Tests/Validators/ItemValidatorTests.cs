using System;
using System.Threading.Tasks;
using System.Linq;
using Xunit;
using Moq;
using FluentAssertions;
using ListaCompras.Core.Models;
using ListaCompras.Core.Data;
using ListaCompras.Core.Validators;

namespace ListaCompras.Core.Tests.Validators
{
    public class ItemValidatorTests
    {
        private readonly Mock<ICategoriaRepository> _categoriaRepositoryMock;
        private readonly Mock<IListaRepository> _listaRepositoryMock;
        private readonly ItemValidator _validator;

        public ItemValidatorTests()
        {
            _categoriaRepositoryMock = new Mock<ICategoriaRepository>();
            _listaRepositoryMock = new Mock<IListaRepository>();
            _validator = new ItemValidator(_categoriaRepositoryMock.Object, _listaRepositoryMock.Object);
        }

        [Fact]
        public async Task Validate_DeveRetornarSucesso_QuandoItemValido()
        {
            // Arrange
            var item = new ItemModel
            {
                Nome = "Item Teste",
                Quantidade = 1,
                Unidade = "un",
                PrecoEstimado = 10,
                CategoriaId = 1,
                ListaId = 1
            };

            _categoriaRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<Func<CategoriaModel, bool>>()))
                .ReturnsAsync(true);
            _listaRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<Func<ListaModel, bool>>()))
                .ReturnsAsync(true);

            // Act
            var result = await _validator.ValidateAsync(item);

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task Validate_DeveRetornarErro_QuandoNomeInvalido(string nome)
        {
            // Arrange
            var item = new ItemModel
            {
                Nome = nome,
                Quantidade = 1,
                Unidade = "un"
            };

            // Act
            var result = await _validator.ValidateAsync(item);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().Contain(e => e.Contains("Nome"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Validate_DeveRetornarErro_QuandoQuantidadeInvalida(decimal quantidade)
        {
            // Arrange
            var item = new ItemModel
            {
                Nome = "Item Teste",
                Quantidade = quantidade,
                Unidade = "un"
            };

            // Act
            var result = await _validator.ValidateAsync(item);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().Contain(e => e.Contains("Quantidade"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task Validate_DeveRetornarErro_QuandoUnidadeInvalida(string unidade)
        {
            // Arrange
            var item = new ItemModel
            {
                Nome = "Item Teste",
                Quantidade = 1,
                Unidade = unidade
            };

            // Act
            var result = await _validator.ValidateAsync(item);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().Contain(e => e.Contains("Unidade"));
        }

        [Fact]
        public async Task Validate_DeveRetornarErro_QuandoCategoriaInexistente()
        {
            // Arrange
            var item = new ItemModel
            {
                Nome = "Item Teste",
                Quantidade = 1,
                Unidade = "un",
                CategoriaId = 999
            };

            _categoriaRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<Func<CategoriaModel, bool>>()))
                .ReturnsAsync(false);

            // Act
            var result = await _validator.ValidateAsync(item);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().Contain(e => e.Contains("Categoria"));
        }

        [Fact]
        public async Task Validate_DeveRetornarErro_QuandoListaInexistente()
        {
            // Arrange
            var item = new ItemModel
            {
                Nome = "Item Teste",
                Quantidade = 1,
                Unidade = "un",
                ListaId = 999
            };

            _listaRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<Func<ListaModel, bool>>()))
                .ReturnsAsync(false);

            // Act
            var result = await _validator.ValidateAsync(item);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().Contain(e => e.Contains("Lista"));
        }
    }
}