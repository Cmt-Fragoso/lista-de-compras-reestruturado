using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
// Removendo referências ambíguas do Microsoft.Build
// usando apenas System.Threading.Tasks

namespace ListaCompras.Core.Managers
{
    /// <summary>
    /// Gerenciador de build e deploy com suporte multi-plataforma
    /// </summary>
    public class BuildManager : IManager
    {
        private readonly ILogger<BuildManager> _logger;
        private readonly BuildSettings _settings;
        private bool _initialized;

        public BuildManager(
            ILogger<BuildManager> logger,
            BuildSettings settings = null)
        {
            _logger = logger;
            _settings = settings ?? new BuildSettings();
        }

        public bool IsInitialized => _initialized;

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            _logger.LogInformation("Inicializando BuildManager");

            try
            {
                // Verifica ambiente de build
                await ValidateBuildEnvironmentAsync();

                // Cria diretórios necessários
                EnsureDirectoryStructure();

                _initialized = true;
                _logger.LogInformation("BuildManager inicializado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar BuildManager");
                throw;
            }
        }

        public async Task ShutdownAsync()
        {
            if (!_initialized)
                return;

            _logger.LogInformation("Finalizando BuildManager");

            try
            {
                // Limpa arquivos temporários
                CleanupTempFiles();

                _initialized = false;
                _logger.LogInformation("BuildManager finalizado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao finalizar BuildManager");
                throw;
            }
        }

        private async Task ValidateBuildEnvironmentAsync()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!File.Exists("C:\\Program Files\\dotnet\\dotnet.exe"))
                    throw new InvalidOperationException(".NET SDK não encontrado");
            }
            else
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "dotnet",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                });

                if (process == null)
                    throw new InvalidOperationException("Não foi possível verificar o .NET SDK");

                await process.WaitForExitAsync();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException(".NET SDK não encontrado");
            }
        }

        private void EnsureDirectoryStructure()
        {
            Directory.CreateDirectory(_settings.PublishDirectory);
            Directory.CreateDirectory(_settings.DeployDirectory);
            Directory.CreateDirectory(_settings.BackupDirectory);
            Directory.CreateDirectory(_settings.TempDirectory);
        }

        private void CleanupTempFiles()
        {
            if (Directory.Exists(_settings.TempDirectory))
                Directory.Delete(_settings.TempDirectory, true);
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("BuildManager não está inicializado");
        }
    }

    public class BuildSettings
    {
        public string PublishDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "publish");
        public string DeployDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deploy");
        public string BackupDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups");
        public string TempDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
        public string[] SupportedPlatforms { get; set; } = new[]
        {
            "win-x64",
            "win-x86",
            "linux-x64",
            "osx-x64"
        };
    }
}