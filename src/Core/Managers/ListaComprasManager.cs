using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ListaCompras.Core.Models;
using ListaCompras.Core.Services;
using Microsoft.Extensions.Logging;

namespace ListaCompras.Core.Managers
{
    /// <summary>
    /// Manager principal do sistema de lista de compras
    /// </summary>
    public class ListaComprasManager : IManager
    {
        private readonly IItemService _itemService;
        private readonly IListaService _listaService;
        private readonly ICategoriaService _categoriaService;
        private readonly IPrecoService _precoService;
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<ListaComprasManager> _logger;
        private bool _initialized;

        public ListaComprasManager(
            IItemService itemService,
            IListaService listaService,
            ICategoriaService categoriaService,
            IPrecoService precoService,
            IUsuarioService usuarioService,
            ILogger<ListaComprasManager> logger)
        {
            _itemService = itemService;
            _listaService = listaService;
            _categoriaService = categoriaService;
            _precoService = precoService;
            _usuarioService = usuarioService;
            _logger = logger;
        }

        public bool IsInitialized => _initialized;

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            _logger.LogInformation("Inicializando ListaComprasManager");

            try
            {
                // Verifica categorias padrão
                await EnsureDefaultCategoriesAsync();
                
                _initialized = true;
                _logger.LogInformation("ListaComprasManager inicializado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar ListaComprasManager");
                throw;
            }
        }

        public async Task ShutdownAsync()
        {
            if (!_initialized)
                return;

            _logger.LogInformation("Finalizando ListaComprasManager");
            _initialized = false;
        }

        #region Operações de Lista

        /// <summary>
        /// Cria uma nova lista de compras
        /// </summary>
        public async Task<ListaModel> CriarListaAsync(int usuarioId, string nome, string descricao = null)
        {
            EnsureInitialized();

            var lista = new ListaModel
            {
                Nome = nome,
                Descricao = descricao,
                UsuarioId = usuarioId,
                Status = StatusLista.EmEdicao
            };

            return await _listaService.CreateAsync(lista);
        }

        /// <summary>
        /// Adiciona um item a uma lista
        /// </summary>
        public async Task<ItemModel> AdicionarItemAsync(
            int listaId, 
            string nome, 
            decimal quantidade, 
            string unidade, 
            int? categoriaId = null)
        {
            EnsureInitialized();

            var item = new ItemModel
            {
                Nome = nome,
                Quantidade = quantidade,
                Unidade = unidade,
                CategoriaId = categoriaId ?? 0,
                ListaId = listaId
            };

            return await _itemService.CreateAsync(item);
        }

        /// <summary>
        /// Marca um item como comprado
        /// </summary>
        public async Task MarcarItemCompradoAsync(int itemId, decimal precoReal)
        {
            EnsureInitialized();

            await _itemService.MarcarCompradoAsync(itemId, precoReal);
            
            // Registra o preço no histórico
            var item = await _itemService.GetByIdAsync(itemId);
            await _precoService.RegistrarPrecoAsync(new PrecoModel
            {
                ItemId = itemId,
                Valor = precoReal,
                Fonte = FontePreco.Manual,
                DataPreco = DateTime.Now
            });
        }

        /// <summary>
        /// Finaliza uma lista de compras
        /// </summary>
        public async Task FinalizarListaAsync(int listaId)
        {
            EnsureInitialized();

            var lista = await _listaService.GetByIdAsync(listaId);
            if (lista.Status != StatusLista.EmCompra)
                throw new InvalidOperationException("Lista precisa estar em compra para ser finalizada");

            // Calcula o total
            var total = await _listaService.CalcularTotalAsync(listaId);

            // Atualiza o status
            await _listaService.AtualizarStatusAsync(listaId, StatusLista.Concluida);
        }

        #endregion

        #region Operações de Usuário

        /// <summary>
        /// Registra um novo usuário
        /// </summary>
        public async Task<UsuarioModel> RegistrarUsuarioAsync(
            string nome, 
            string email, 
            string senha, 
            string dispositivoId)
        {
            EnsureInitialized();

            var usuario = new UsuarioModel
            {
                Nome = nome,
                Email = email,
                DispositivoId = dispositivoId,
                Status = StatusUsuario.PendenteConfirmacao
            };

            return await _usuarioService.CreateAsync(usuario, senha);
        }

        /// <summary>
        /// Realiza login do usuário
        /// </summary>
        public async Task<UsuarioModel> RealizarLoginAsync(string email, string senha)
        {
            EnsureInitialized();

            var credenciaisValidas = await _usuarioService.ValidarCredenciaisAsync(email, senha);
            if (!credenciaisValidas)
                throw new UnauthorizedAccessException("Credenciais inválidas");

            var usuario = await _usuarioService.GetByEmailAsync(email);
            await _usuarioService.RegistrarAcessoAsync(usuario.Id);

            return usuario;
        }

        #endregion

        #region Operações de Preço

        /// <summary>
        /// Analisa tendência de preço de um item
        /// </summary>
        public async Task<(decimal variacao, bool tendenciaAlta)> AnalisarTendenciaPrecoAsync(int itemId)
        {
            EnsureInitialized();

            return await _precoService.AnalisarTendenciaAsync(itemId, 30); // Analisa últimos 30 dias
        }

        /// <summary>
        /// Obtém preços promocionais ativos
        /// </summary>
        public async Task<IEnumerable<PrecoModel>> ObterPromocoesAtivasAsync()
        {
            EnsureInitialized();

            return await _precoService.GetPromocionaisAtivosAsync();
        }

        #endregion

        #region Métodos Privados

        private void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("ListaComprasManager não está inicializado");
        }

        private async Task EnsureDefaultCategoriesAsync()
        {
            var categoriasRaiz = await _categoriaService.GetCategoriasRaizAsync();
            if (!categoriasRaiz.Any())
            {
                var categoriasDefault = new[]
                {
                    new CategoriaModel { Nome = "Alimentos", Cor = "#FF0000", Ordem = 1 },
                    new CategoriaModel { Nome = "Bebidas", Cor = "#00FF00", Ordem = 2 },
                    new CategoriaModel { Nome = "Limpeza", Cor = "#0000FF", Ordem = 3 },
                    new CategoriaModel { Nome = "Higiene", Cor = "#FFFF00", Ordem = 4 },
                    new CategoriaModel { Nome = "Outros", Cor = "#808080", Ordem = 5 }
                };

                foreach (var categoria in categoriasDefault)
                {
                    await _categoriaService.CreateAsync(categoria);
                }
            }
        }

        #endregion
    }
}