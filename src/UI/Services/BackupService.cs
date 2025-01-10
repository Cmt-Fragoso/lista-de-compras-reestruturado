using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using ListaCompras.Core.Models;
using ListaCompras.UI.Models;
using Newtonsoft.Json;

namespace ListaCompras.UI.Services
{
    public class BackupService
    {
        private readonly IDataService _dataService;
        private readonly ConfigModel _config;
        private readonly string _backupExtension = ".lcbk";
        
        public event EventHandler<string> BackupProgress;
        public event EventHandler<(int current, int total)> BackupProgressPercentage;

        public BackupService(IDataService dataService, ConfigModel config)
        {
            _dataService = dataService;
            _config = config;
        }

        public async Task CreateBackupAsync(string fileName)
        {
            ReportProgress("Iniciando backup...");
            ReportProgressPercentage(0, 6);

            var backupData = new BackupData
            {
                Version = "1.0",
                CreatedAt = DateTime.Now,
                Config = _config
            };

            // Carrega os dados
            ReportProgress("Carregando categorias...");
            backupData.Categorias = await _dataService.GetCategoriasAsync();
            ReportProgressPercentage(1, 6);

            ReportProgress("Carregando itens...");
            backupData.Itens = await _dataService.GetItensAsync();
            ReportProgressPercentage(2, 6);

            ReportProgress("Carregando listas...");
            backupData.Listas = await _dataService.GetListasAsync();
            ReportProgressPercentage(3, 6);

            ReportProgress("Carregando preços...");
            backupData.Precos = new List<PrecoModel>();
            foreach (var item in backupData.Itens)
            {
                var precos = await _dataService.GetPrecosAsync(item.Id);
                backupData.Precos.AddRange(precos);
            }
            ReportProgressPercentage(4, 6);

            ReportProgress("Compactando dados...");
            // Serializa e compacta os dados
            var json = JsonConvert.SerializeObject(backupData, Formatting.Indented);
            using (var fileStream = File.Create(fileName))
            using (var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal))
            using (var writer = new StreamWriter(gzipStream))
            {
                await writer.WriteAsync(json);
            }
            ReportProgressPercentage(5, 6);

            ReportProgress("Limpando backups antigos...");
            await CleanOldBackupsAsync();
            ReportProgressPercentage(6, 6);

            ReportProgress("Backup concluído com sucesso!");
        }

        public async Task RestoreBackupAsync(string fileName)
        {
            ReportProgress("Iniciando restauração...");
            ReportProgressPercentage(0, 5);

            string json;
            using (var fileStream = File.OpenRead(fileName))
            using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzipStream))
            {
                json = await reader.ReadToEndAsync();
            }

            var backupData = JsonConvert.DeserializeObject<BackupData>(json);
            
            // Validar versão
            if (Version.Parse(backupData.Version) > Version.Parse("1.0"))
            {
                throw new Exception("Versão do backup não suportada");
            }

            ReportProgress("Restaurando categorias...");
            foreach (var categoria in backupData.Categorias)
            {
                await _dataService.SaveCategoriaAsync(categoria);
            }
            ReportProgressPercentage(1, 5);

            ReportProgress("Restaurando itens...");
            foreach (var item in backupData.Itens)
            {
                await _dataService.SaveItemAsync(item);
            }
            ReportProgressPercentage(2, 5);

            ReportProgress("Restaurando preços...");
            foreach (var preco in backupData.Precos)
            {
                await _dataService.SavePrecoAsync(preco);
            }
            ReportProgressPercentage(3, 5);

            ReportProgress("Restaurando listas...");
            foreach (var lista in backupData.Listas)
            {
                await _dataService.SaveListaAsync(lista);
            }
            ReportProgressPercentage(4, 5);

            ReportProgress("Restaurando configurações...");
            await _dataService.SaveConfigAsync(backupData.Config);
            ReportProgressPercentage(5, 5);

            ReportProgress("Restauração concluída com sucesso!");
        }

        public async Task CreateAutoBackupAsync()
        {
            if (!_config.BackupAutomatico || string.IsNullOrEmpty(_config.DiretorioBackup))
                return;

            var fileName = Path.Combine(
                _config.DiretorioBackup,
                $"backup_auto_{DateTime.Now:yyyyMMdd_HHmmss}{_backupExtension}");

            await CreateBackupAsync(fileName);
        }

        private async Task CleanOldBackupsAsync()
        {
            if (string.IsNullOrEmpty(_config.DiretorioBackup))
                return;

            var directory = new DirectoryInfo(_config.DiretorioBackup);
            if (!directory.Exists)
                return;

            var cutoffDate = DateTime.Now.AddDays(-_config.DiasManterBackup);
            var oldFiles = directory.GetFiles($"*{_backupExtension}")
                                  .Where(f => f.LastWriteTime < cutoffDate);

            foreach (var file in oldFiles)
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                    // Ignora erros ao deletar arquivos antigos
                }
            }

            await Task.CompletedTask;
        }

        private void ReportProgress(string message)
        {
            BackupProgress?.Invoke(this, message);
        }

        private void ReportProgressPercentage(int current, int total)
        {
            BackupProgressPercentage?.Invoke(this, (current, total));
        }
    }

    internal class BackupData
    {
        public string Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public ConfigModel Config { get; set; }
        public List<CategoriaModel> Categorias { get; set; }
        public List<ItemModel> Itens { get; set; }
        public List<ListaModel> Listas { get; set; }
        public List<PrecoModel> Precos { get; set; }
    }
}