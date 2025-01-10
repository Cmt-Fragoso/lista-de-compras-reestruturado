using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ListaCompras.Core.Data;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Managers
{
    /// <summary>
    /// Gerenciador de backup com suporte a compressão, validação e versionamento
    /// </summary>
    public class BackupManager : IManager
    {
        private readonly RuntimeAppDbContextFactory _contextFactory;
        private readonly ILogger<BackupManager> _logger;
        private readonly ConcurrentDictionary<string, BackupProgress> _progress;
        private readonly string _backupDirectory;
        private bool _initialized;

        private const string BACKUP_EXTENSION = ".lcbk";
        private const string TEMP_EXTENSION = ".temp";
        private const string METADATA_FILE = "backup_metadata.json";
        private const int BUFFER_SIZE = 81920; // 80KB

        public BackupManager(
            RuntimeAppDbContextFactory contextFactory,
            ILogger<BackupManager> logger,
            string backupDirectory = null)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _progress = new ConcurrentDictionary<string, BackupProgress>();
            _backupDirectory = backupDirectory ?? 
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
        }

        public bool IsInitialized => _initialized;

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            _logger.LogInformation("Inicializando BackupManager");

            try
            {
                // Garante que o diretório existe
                Directory.CreateDirectory(_backupDirectory);

                // Verifica espaço disponível
                var drive = new DriveInfo(Path.GetPathRoot(_backupDirectory));
                if (drive.AvailableFreeSpace < 1024 * 1024 * 100) // 100MB mínimo
                {
                    throw new InvalidOperationException("Espaço em disco insuficiente para backups");
                }

                // Limpa arquivos temporários antigos
                var tempFiles = Directory.GetFiles(_backupDirectory, $"*{TEMP_EXTENSION}");
                foreach (var file in tempFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao limpar arquivo temporário: {File}", file);
                    }
                }

                _initialized = true;
                _logger.LogInformation("BackupManager inicializado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar BackupManager");
                throw;
            }
        }

        public async Task ShutdownAsync()
        {
            if (!_initialized)
                return;

            _logger.LogInformation("Finalizando BackupManager");

            try
            {
                // Cancela backups em andamento
                foreach (var progress in _progress.Values)
                {
                    progress.CancellationSource?.Cancel();
                }

                _initialized = false;
                _logger.LogInformation("BackupManager finalizado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao finalizar BackupManager");
                throw;
            }
        }

        public async Task<string> CreateBackupAsync(BackupOptions options = null)
        {
            EnsureInitialized();
            options ??= new BackupOptions();

            var backupId = Guid.NewGuid().ToString("N");
            var progress = new BackupProgress
            {
                Id = backupId,
                StartTime = DateTime.UtcNow,
                CancellationSource = new CancellationTokenSource()
            };

            if (!_progress.TryAdd(backupId, progress))
            {
                throw new InvalidOperationException("Não foi possível iniciar o backup");
            }

            try
            {
                _logger.LogInformation("Iniciando backup {BackupId}", backupId);

                // Cria arquivo temporário
                var tempFile = Path.Combine(_backupDirectory, $"{backupId}{TEMP_EXTENSION}");
                var finalFile = Path.Combine(_backupDirectory, $"{backupId}{BACKUP_EXTENSION}");

                using (var fileStream = File.Create(tempFile))
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
                {
                    // Backup dos dados
                    await using (var context = await _contextFactory.CreateReadOnlyAsync())
                    {
                        // Metadados do backup
                        var metadata = new BackupMetadata
                        {
                            Id = backupId,
                            CreatedAt = DateTime.UtcNow,
                            Version = typeof(BackupManager).Assembly.GetName().Version.ToString(),
                            Options = options
                        };

                        // Salva metadados
                        var metadataEntry = archive.CreateEntry(METADATA_FILE, CompressionLevel.Optimal);
                        await using (var entryStream = metadataEntry.Open())
                        {
                            await JsonSerializer.SerializeAsync(entryStream, metadata);
                        }

                        // Backup das tabelas
                        await BackupTableAsync<ItemModel>(context, archive, progress);
                        await BackupTableAsync<ListaModel>(context, archive, progress);
                        await BackupTableAsync<CategoriaModel>(context, archive, progress);
                        await BackupTableAsync<PrecoModel>(context, archive, progress);
                        await BackupTableAsync<UsuarioModel>(context, archive, progress);

                        progress.Status = BackupStatus.Finalizing;
                    }
                }

                // Calcula hash do arquivo
                var hash = await CalculateFileHashAsync(tempFile);

                // Move para arquivo final
                File.Move(tempFile, finalFile, true);

                // Atualiza progresso
                progress.EndTime = DateTime.UtcNow;
                progress.Status = BackupStatus.Completed;
                progress.FileHash = hash;

                _logger.LogInformation(
                    "Backup {BackupId} concluído. Duração: {Duration}, Tamanho: {Size}",
                    backupId,
                    progress.EndTime - progress.StartTime,
                    new FileInfo(finalFile).Length);

                return backupId;
            }
            catch (Exception ex)
            {
                progress.Status = BackupStatus.Failed;
                progress.Error = ex.Message;
                _logger.LogError(ex, "Erro no backup {BackupId}", backupId);
                throw;
            }
            finally
            {
                progress.CancellationSource?.Dispose();
            }
        }

        public async Task RestoreBackupAsync(string backupId, RestoreOptions options = null)
        {
            EnsureInitialized();
            options ??= new RestoreOptions();

            var backupFile = Path.Combine(_backupDirectory, $"{backupId}{BACKUP_EXTENSION}");
            if (!File.Exists(backupFile))
            {
                throw new FileNotFoundException("Arquivo de backup não encontrado", backupFile);
            }

            try
            {
                _logger.LogInformation("Iniciando restauração do backup {BackupId}", backupId);

                // Valida hash do arquivo
                var hash = await CalculateFileHashAsync(backupFile);
                var progress = _progress.GetValueOrDefault(backupId);
                if (progress?.FileHash != null && progress.FileHash != hash)
                {
                    throw new InvalidOperationException("Hash do arquivo de backup não confere");
                }

                using (var archive = ZipFile.OpenRead(backupFile))
                {
                    // Lê metadados
                    var metadataEntry = archive.GetEntry(METADATA_FILE);
                    if (metadataEntry == null)
                    {
                        throw new InvalidOperationException("Arquivo de backup corrompido");
                    }

                    BackupMetadata metadata;
                    using (var entryStream = metadataEntry.Open())
                    {
                        metadata = await JsonSerializer.DeserializeAsync<BackupMetadata>(entryStream);
                    }

                    // Restaura dados
                    await using (var context = await _contextFactory.CreateForTransactionAsync())
                    {
                        if (options.ClearExistingData)
                        {
                            await ClearExistingDataAsync(context);
                        }

                        await RestoreTableAsync<ItemModel>(context, archive, metadata);
                        await RestoreTableAsync<ListaModel>(context, archive, metadata);
                        await RestoreTableAsync<CategoriaModel>(context, archive, metadata);
                        await RestoreTableAsync<PrecoModel>(context, archive, metadata);
                        await RestoreTableAsync<UsuarioModel>(context, archive, metadata);

                        await context.SaveChangesAsync();
                    }
                }

                _logger.LogInformation("Restauração do backup {BackupId} concluída", backupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na restauração do backup {BackupId}", backupId);
                throw;
            }
        }

        public BackupProgress GetProgress(string backupId)
        {
            return _progress.GetValueOrDefault(backupId);
        }

        private async Task BackupTableAsync<T>(
            AppDbContext context, 
            ZipArchive archive, 
            BackupProgress progress) where T : BaseModel
        {
            var tableName = typeof(T).Name;
            progress.CurrentTable = tableName;

            _logger.LogInformation("Iniciando backup da tabela {Table}", tableName);

            var entries = await context.Set<T>()
                .AsNoTracking()
                .ToListAsync(progress.CancellationSource.Token);

            var entry = archive.CreateEntry($"{tableName}.json", CompressionLevel.Optimal);
            await using var entryStream = entry.Open();
            await JsonSerializer.SerializeAsync(entryStream, entries, cancellationToken: progress.CancellationSource.Token);

            progress.ProcessedTables++;
            _logger.LogInformation("Backup da tabela {Table} concluído. Items: {Count}", tableName, entries.Count);
        }

        private async Task RestoreTableAsync<T>(
            AppDbContext context,
            ZipArchive archive,
            BackupMetadata metadata) where T : BaseModel
        {
            var tableName = typeof(T).Name;
            _logger.LogInformation("Restaurando tabela {Table}", tableName);

            var entry = archive.GetEntry($"{tableName}.json");
            if (entry == null)
            {
                _logger.LogWarning("Tabela {Table} não encontrada no backup", tableName);
                return;
            }

            await using var entryStream = entry.Open();
            var items = await JsonSerializer.DeserializeAsync<List<T>>(entryStream);

            if (items?.Any() == true)
            {
                await context.BulkInsertAsync(items);
            }

            _logger.LogInformation("Restauração da tabela {Table} concluída. Items: {Count}", tableName, items?.Count ?? 0);
        }

        private async Task ClearExistingDataAsync(AppDbContext context)
        {
            _logger.LogWarning("Limpando dados existentes");

            await context.Set<ItemModel>().BatchDeleteAsync();
            await context.Set<ListaModel>().BatchDeleteAsync();
            await context.Set<CategoriaModel>().BatchDeleteAsync();
            await context.Set<PrecoModel>().BatchDeleteAsync();
            await context.Set<UsuarioModel>().BatchDeleteAsync();
        }

        private static async Task<string> CalculateFileHashAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            await using var stream = File.OpenRead(filePath);
            var hash = await sha256.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("BackupManager não está inicializado");
        }
    }

    public class BackupOptions
    {
        public bool CompressData { get; set; } = true;
        public bool IncludeMetadata { get; set; } = true;
        public bool ValidateIntegrity { get; set; } = true;
    }

    public class RestoreOptions
    {
        public bool ClearExistingData { get; set; }
        public bool ValidateIntegrity { get; set; } = true;
    }

    public class BackupMetadata
    {
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Version { get; set; }
        public BackupOptions Options { get; set; }
    }

    public class BackupProgress
    {
        public string Id { get; set; }
        public BackupStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string CurrentTable { get; set; }
        public int ProcessedTables { get; set; }
        public string Error { get; set; }
        public string FileHash { get; set; }
        internal CancellationTokenSource CancellationSource { get; set; }
    }

    public enum BackupStatus
    {
        Running,
        Finalizing,
        Completed,
        Failed,
        Cancelled
    }
}