using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ListaCompras.Core.Services;

namespace ListaCompras.Core.Managers
{
    /// <summary>
    /// Manager responsável pela sincronização P2P
    /// </summary>
    public class SyncManager : IManager
    {
        private readonly IListaService _listaService;
        private readonly IItemService _itemService;
        private readonly IPrecoService _precoService;
        private readonly ILogger<SyncManager> _logger;
        private readonly ConcurrentQueue<SyncAction> _syncQueue;
        private bool _initialized;
        private bool _isSyncing;

        public SyncManager(
            IListaService listaService,
            IItemService itemService,
            IPrecoService precoService,
            ILogger<SyncManager> logger)
        {
            _listaService = listaService;
            _itemService = itemService;
            _precoService = precoService;
            _logger = logger;
            _syncQueue = new ConcurrentQueue<SyncAction>();
        }

        public bool IsInitialized => _initialized;

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            _logger.LogInformation("Inicializando SyncManager");

            try
            {
                // Por enquanto apenas marca como inicializado
                // No futuro, iniciará a descoberta de peers e sincronização inicial
                _initialized = true;
                _logger.LogInformation("SyncManager inicializado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar SyncManager");
                throw;
            }
        }

        public async Task ShutdownAsync()
        {
            if (!_initialized)
                return;

            _logger.LogInformation("Finalizando SyncManager");

            try
            {
                // Aguarda sincronização de ações pendentes
                await ProcessPendingActionsAsync();

                _initialized = false;
                _logger.LogInformation("SyncManager finalizado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao finalizar SyncManager");
                throw;
            }
        }

        /// <summary>
        /// Enfileira uma ação para sincronização
        /// </summary>
        public void EnqueueAction(SyncAction action)
        {
            EnsureInitialized();
            _syncQueue.Enqueue(action);
        }

        /// <summary>
        /// Inicia sincronização com outros peers
        /// </summary>
        public async Task StartSyncAsync()
        {
            EnsureInitialized();

            if (_isSyncing)
            {
                _logger.LogWarning("Sincronização já está em andamento");
                return;
            }

            try
            {
                _isSyncing = true;
                await ProcessPendingActionsAsync();
                // Futuramente: Descoberta de peers e sincronização bidirecional
            }
            finally
            {
                _isSyncing = false;
            }
        }

        #region Métodos Privados

        private void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("SyncManager não está inicializado");
        }

        private async Task ProcessPendingActionsAsync()
        {
            while (_syncQueue.TryDequeue(out var action))
            {
                try
                {
                    await ProcessActionAsync(action);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao processar ação de sincronização: {action.Type}");
                }
            }
        }

        private async Task ProcessActionAsync(SyncAction action)
        {
            // Por enquanto apenas loga a ação
            // No futuro, implementará a sincronização real
            _logger.LogInformation($"Processando ação de sincronização: {action.Type} - {action.EntityId}");
        }

        #endregion
    }

    /// <summary>
    /// Representa uma ação de sincronização
    /// </summary>
    public class SyncAction
    {
        public SyncActionType Type { get; set; }
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Data { get; set; }
    }

    /// <summary>
    /// Tipos de ação de sincronização
    /// </summary>
    public enum SyncActionType
    {
        Create,
        Update,
        Delete
    }
}