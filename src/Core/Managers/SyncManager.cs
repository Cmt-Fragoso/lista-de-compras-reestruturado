using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using ListaCompras.Core.Models;
using ListaCompras.Core.Services;
using ListaCompras.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace ListaCompras.Core.Managers
{
    public class SyncManager : IManager
    {
        private readonly RuntimeAppDbContextFactory _contextFactory;
        private readonly ILogger<SyncManager> _logger;
        private readonly ICurrentUserProvider _userProvider;
        private readonly CacheManager _cacheManager;
        private readonly SyncSettings _settings;
        private readonly ConcurrentDictionary<string, PeerInfo> _peers;
        private readonly ConcurrentQueue<SyncOperation> _pendingOperations;
        private readonly SemaphoreSlim _syncSemaphore;
        private readonly System.Threading.Timer _discoveryTimer;
        private readonly System.Threading.Timer _syncTimer;
        private bool _initialized;
        private bool _isSyncing;

        private readonly UdpClient _discoveryClient;
        private readonly TcpListener _syncListener;

        public SyncManager(
            RuntimeAppDbContextFactory contextFactory,
            ILogger<SyncManager> logger,
            ICurrentUserProvider userProvider,
            CacheManager cacheManager,
            SyncSettings settings = null)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _userProvider = userProvider;
            _cacheManager = cacheManager;
            _settings = settings ?? new SyncSettings();
            _peers = new ConcurrentDictionary<string, PeerInfo>();
            _pendingOperations = new ConcurrentQueue<SyncOperation>();
            _syncSemaphore = new SemaphoreSlim(1, 1);

            // Inicializa UDP para discovery
            _discoveryClient = new UdpClient(new IPEndPoint(IPAddress.Any, _settings.DiscoveryPort));

            // Inicializa TCP para sync
            _syncListener = new TcpListener(IPAddress.Any, _settings.SyncPort);

            // Inicializa timers
            _discoveryTimer = new System.Threading.Timer(
                DiscoveryCallback, 
                null, 
                Timeout.Infinite, 
                Timeout.Infinite);

            _syncTimer = new System.Threading.Timer(
                SyncCallback, 
                null, 
                Timeout.Infinite, 
                Timeout.Infinite);
        }

        public bool IsInitialized => _initialized;

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            _logger.LogInformation("Inicializando SyncManager");

            try
            {
                // Inicia listener TCP
                _syncListener.Start();
                _ = Task.Run(ListenForSyncRequestsAsync);

                // Inicia discovery
                _ = Task.Run(ListenForDiscoveryAsync);

                // Inicia timers
                _discoveryTimer.Change(0, _settings.DiscoveryIntervalMs);
                _syncTimer.Change(0, _settings.SyncIntervalMs);

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
                // Notifica peers
                var message = new DiscoveryMessage
                {
                    Type = MessageType.Leaving,
                    DeviceId = _settings.DeviceId
                };

                await BroadcastDiscoveryMessageAsync(message);

                // Para timers
                _discoveryTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _syncTimer.Change(Timeout.Infinite, Timeout.Infinite);

                // Para listeners
                _syncListener.Stop();
                _discoveryClient.Close();

                // Processa operações pendentes
                await ProcessPendingOperationsAsync();

                _initialized = false;
                _logger.LogInformation("SyncManager finalizado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao finalizar SyncManager");
                throw;
            }
        }

        private async Task DiscoveryCallback(object state)
        {
            if (!_initialized) return;

            try
            {
                // Limpa peers inativos
                var inactivePeers = _peers
                    .Where(p => DateTime.UtcNow - p.Value.LastSeen > 
                               TimeSpan.FromMilliseconds(_settings.PeerTimeoutMs))
                    .Select(p => p.Key)
                    .ToList();

                foreach (var peerId in inactivePeers)
                {
                    _peers.TryRemove(peerId, out _);
                    _logger.LogTrace("Peer inativo removido: {DeviceId}", peerId);
                }

                // Envia broadcast
                var message = new DiscoveryMessage
                {
                    Type = MessageType.Broadcast,
                    DeviceId = _settings.DeviceId
                };

                await BroadcastDiscoveryMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no discovery callback");
            }
        }

        private async Task SyncCallback(object state)
        {
            if (!_initialized || _isSyncing) return;

            await _syncSemaphore.WaitAsync();
            try
            {
                _isSyncing = true;
                await SyncWithPeersInternalAsync();
            }
            finally
            {
                _isSyncing = false;
                _syncSemaphore.Release();
            }
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("SyncManager não está inicializado");
        }

        private async Task SyncWithPeersInternalAsync()
        {
            // Implementação do sync com peers
            // ...
        }

        // Outros métodos internos...
    }

    public class SyncSettings
    {
        public string DeviceId { get; set; } = Guid.NewGuid().ToString();
        public int DiscoveryPort { get; set; } = 45678;
        public int SyncPort { get; set; } = 45679;
        public int DiscoveryIntervalMs { get; set; } = 5000;
        public int SyncIntervalMs { get; set; } = 30000;
        public int PeerTimeoutMs { get; set; } = 15000;
    }

    public class PeerInfo
    {
        public string DeviceId { get; set; }
        public DateTime LastSeen { get; set; }
        public IPEndPoint EndPoint { get; set; }
    }

    public enum MessageType
    {
        Broadcast,
        Leaving
    }

    public class DiscoveryMessage
    {
        public MessageType Type { get; set; }
        public string DeviceId { get; set; }
    }
}