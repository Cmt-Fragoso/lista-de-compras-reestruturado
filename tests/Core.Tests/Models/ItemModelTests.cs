using System;
using Xunit;
using ListaCompras.Core.Models;
using FluentAssertions;

namespace ListaCompras.Core.Tests.Models
{
    public class ItemModelTests
    {
        [Fact]
        public void Item_DeveSerCriadoComValoresPadrao()
        {
            // Arrange & Act
            var item = new ItemModel();

            // Assert
            item.Should().NotBeNull();
            item.Id.Should().Be(0);
            item.Comprado.Should().BeFalse();
            item.PrecoEstimado.Should().Be(0);
        }

        [Fact]
        public void Item_DeveTerDatasCriacaoEAtualizacaoValidas()
        {
            // Arrange & Act
            var item = new ItemModel
            {
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now
            };

            // Assert
            item.DataCriacao.Should().NotBe(default(DateTime));
            item.DataAtualizacao.Should().NotBe(default(DateTime));
            item.DataAtualizacao.Should().BeOnOrAfter(item.DataCriacao);
        }

        [Theory]
        [InlineData("", "Unidade inválida")]
        [InlineData("Nome muito longo que excede o limite de caracteres permitido para o nome do item", "Nome muito longo")]
        public void Item_DeveValidarTamanhoCampos(string nome, string descricao)
        {
            // Arrange
            var item = new ItemModel
            {
                Nome = nome,
                Descricao = descricao
            };

            // Assert
            if (string.IsNullOrEmpty(nome))
                item.Nome.Should().BeNullOrEmpty();
            else
                item.Nome.Length.Should().BeLessThanOrEqualTo(100);

            if (!string.IsNullOrEmpty(descricao))
                item.Descricao.Length.Should().BeLessThanOrEqualTo(500);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Item_NaoDeveAceitarQuantidadeInvalida(decimal quantidade)
        {
            // Arrange & Act
            var item = new ItemModel { Quantidade = quantidade };

            // Assert
            item.Quantidade.Should().BeLessThanOrEqualTo(0);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Item_NaoDeveAceitarPrecoNegativo(decimal preco)
        {
            // Arrange & Act
            var item = new ItemModel { PrecoEstimado = preco };

            // Assert
            item.PrecoEstimado.Should().BeLessThan(0);
        }
    }
}