using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using ListaCompras.Core.Models;
using ListaCompras.Core.Services;
using ListaCompras.Core.Managers;

namespace ListaCompras.Core.Tests.Managers
{
    public class ListaComprasManagerTests
    {
        private readonly Mock<IItemService> _itemServiceMock;
        private readonly Mock<IListaService> _listaServiceMock;
        private readonly Mock<ICategoriaService> _categoriaServiceMock;
        private readonly Mock<IPrecoService> _precoServiceMock;
        private readonly Mock<IUsuarioService> _usuarioServiceMock;
        private readonly Mock<ILogger<ListaComprasManager>> _loggerMock;
        private readonly ListaComprasManager _manager;

        public ListaComprasManagerTests()
        {
            _itemServiceMock = new Mock<IItemService>();
            _listaServiceMock = new Mock<IListaService>();
            _categoriaServiceMock = new Mock<ICategoriaService>();
            _precoServiceMock = new Mock<IPrecoService>();
            _usuarioServiceMock = new Mock<IUsuarioService>();
            _loggerMock = new Mock<ILogger<ListaComprasManager>>();

            _manager = new ListaComprasManager(
                _itemServiceMock.Object,
                _listaServiceMock.Object,
                _categoriaServiceMock.Object,
                _precoServiceMock.Object,
                _usuarioServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task InitializeAsync_DeveInicializarCorretamente()
        {
            // Act
            await _manager.InitializeAsync();

            // Assert
            _manager.IsInitialized.Should().BeTrue();
            _categoriaServiceMock.Verify(s => s.GetCategoriasRaizAsync(), Times.Once);
        }

        [Fact]
        public async Task CriarListaAsync_DeveCriarLista_QuandoUsuarioValido()
        {
            // Arrange
            await _manager.InitializeAsync();
            var usuarioId = 1;
            var nomeLista = "Lista de Teste";
            var lista = new ListaModel
            {
                Nome = nomeLista,
                UsuarioId = usuarioId,
                Status = StatusLista.EmEdicao
            };

            _listaServiceMock.Setup(s => s.CreateAsync(It.IsAny<ListaModel>()))
                .ReturnsAsync(lista);

            // Act
            var result = await _manager.CriarListaAsync(usuarioId, nomeLista);

            // Assert
            result.Should().NotBeNull();
            result.Nome.Should().Be(nomeLista);
            result.UsuarioId.Should().Be(usuarioId);
            result.Status.Should().Be(StatusLista.EmEdicao);
        }

        [Fact]
        public async Task AdicionarItemAsync_DeveAdicionarItem_QuandoListaValida()
        {
            // Arrange
            await _manager.InitializeAsync();
            var listaId = 1;
            var nomeItem = "Item Teste";
            var quantidade = 2m;
            var unidade = "un";
            var item = new ItemModel
            {
                Nome = nomeItem,
                Quantidade = quantidade,
                Unidade = unidade,
                ListaId = listaId
            };

            _itemServiceMock.Setup(s => s.CreateAsync(It.IsAny<ItemModel>()))
                .ReturnsAsync(item);

            // Act
            var result = await _manager.AdicionarItemAsync(listaId, nomeItem, quantidade, unidade);

            // Assert
            result.Should().NotBeNull();
            result.Nome.Should().Be(nomeItem);
            result.Quantidade.Should().Be(quantidade);
            result.Unidade.Should().Be(unidade);
            result.ListaId.Should().Be(listaId);
        }

        [Fact]
        public async Task MarcarItemCompradoAsync_DeveMarcarItemERegistrarPreco()
        {
            // Arrange
            await _manager.InitializeAsync();
            var itemId = 1;
            var precoReal = 10.5m;
            var item = new ItemModel { Id = itemId };

            _itemServiceMock.Setup(s => s.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            // Act
            await _manager.MarcarItemCompradoAsync(itemId, precoReal);

            // Assert
            _itemServiceMock.Verify(s => s.MarcarCompradoAsync(itemId, precoReal), Times.Once);
            _precoServiceMock.Verify(s => s.RegistrarPrecoAsync(It.IsAny<PrecoModel>()), Times.Once);
        }

        [Fact]
        public async Task FinalizarListaAsync_DeveFinalizarLista_QuandoEmCompra()
        {
            // Arrange
            await _manager.InitializeAsync();
            var listaId = 1;
            var lista = new ListaModel
            {
                Id = listaId,
                Status = StatusLista.EmCompra
            };

            _listaServiceMock.Setup(s => s.GetByIdAsync(listaId))
                .ReturnsAsync(lista);
            _listaServiceMock.Setup(s => s.CalcularTotalAsync(listaId))
                .ReturnsAsync(100m);

            // Act
            await _manager.FinalizarListaAsync(listaId);

            // Assert
            _listaServiceMock.Verify(s => s.CalcularTotalAsync(listaId), Times.Once);
            _listaServiceMock.Verify(s => s.AtualizarStatusAsync(listaId, StatusLista.Concluida), Times.Once);
        }

        [Fact]
        public async Task RegistrarUsuarioAsync_DeveRegistrarUsuario_QuandoDadosValidos()
        {
            // Arrange
            await _manager.InitializeAsync();
            var nome = "Usuario Teste";
            var email = "teste@teste.com";
            var senha = "senha123";
            var dispositivoId = "device123";
            var usuario = new UsuarioModel
            {
                Nome = nome,
                Email = email,
                DispositivoId = dispositivoId,
                Status = StatusUsuario.PendenteConfirmacao
            };

            _usuarioServiceMock.Setup(s => s.CreateAsync(It.IsAny<UsuarioModel>(), senha))
                .ReturnsAsync(usuario);

            // Act
            var result = await _manager.RegistrarUsuarioAsync(nome, email, senha, dispositivoId);

            // Assert
            result.Should().NotBeNull();
            result.Nome.Should().Be(nome);
            result.Email.Should().Be(email);
            result.DispositivoId.Should().Be(dispositivoId);
            result.Status.Should().Be(StatusUsuario.PendenteConfirmacao);
        }

        [Fact]
        public async Task RealizarLoginAsync_DeveLogarUsuario_QuandoCredenciaisValidas()
        {
            // Arrange
            await _manager.InitializeAsync();
            var email = "teste@teste.com";
            var senha = "senha123";
            var usuario = new UsuarioModel
            {
                Id = 1,
                Email = email
            };

            _usuarioServiceMock.Setup(s => s.ValidarCredenciaisAsync(email, senha))
                .ReturnsAsync(true);
            _usuarioServiceMock.Setup(s => s.GetByEmailAsync(email))
                .ReturnsAsync(usuario);

            // Act
            var result = await _manager.RealizarLoginAsync(email, senha);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be(email);
            _usuarioServiceMock.Verify(s => s.RegistrarAcessoAsync(usuario.Id), Times.Once);
        }

        [Fact]
        public async Task AnalisarTendenciaPrecoAsync_DeveAnalisarTendencia()
        {
            // Arrange
            await _manager.InitializeAsync();
            var itemId = 1;
            var tendencia = (variacao: 10.5m, tendenciaAlta: true);

            _precoServiceMock.Setup(s => s.AnalisarTendenciaAsync(itemId, 30))
                .ReturnsAsync(tendencia);

            // Act
            var result = await _manager.AnalisarTendenciaPrecoAsync(itemId);

            // Assert
            result.Should().Be(tendencia);
            _precoServiceMock.Verify(s => s.AnalisarTendenciaAsync(itemId, 30), Times.Once);
        }

        [Fact]
        public async Task ObterPromocoesAtivasAsync_DeveRetornarPromocoes()
        {
            // Arrange
            await _manager.InitializeAsync();
            var promocoes = new[]
            {
                new PrecoModel { Id = 1, Promocional = true },
                new PrecoModel { Id = 2, Promocional = true }
            };

            _precoServiceMock.Setup(s => s.GetPromocionaisAtivosAsync())
                .ReturnsAsync(promocoes);

            // Act
            var result = await _manager.ObterPromocoesAtivasAsync();

            // Assert
            result.Should().BeEquivalentTo(promocoes);
            _precoServiceMock.Verify(s => s.GetPromocionaisAtivosAsync(), Times.Once);
        }
    }
}