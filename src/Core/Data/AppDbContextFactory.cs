using System;
using System.IO;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ListaCompras.Core.Services;
using Microsoft.AspNetCore.Http;
using Polly;
using Polly.Retry;

namespace ListaCompras.Core.Data
{
    /// <summary>
    /// Configurações para factory de contexto
    /// </summary>
    public class DbFactorySettings
    {
        public string DefaultConnection { get; set; }
        public Dictionary<string, string> ReadReplicas { get; set; } = new();
        public int CommandTimeout { get; set; } = 30;
        public int MaxRetryCount { get; set; } = 3;
        public int RetryWaitSeconds { get; set; } = 5;
        public bool EnableDetailedErrors { get; set; } = true;
        public bool EnableSensitiveDataLogging { get; set; }
        public int PoolSize { get; set; } = 128;
    }

    /// <summary>
    /// Factory para criação do DbContext com suporte a design-time
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var configuration = LoadConfiguration();
            var settings = configuration
                .GetSection("Database")
                .Get<DbFactorySettings>() ?? new DbFactorySettings();

            var builder = new DbContextOptionsBuilder<AppDbContext>();
            ConfigureDatabase(builder, settings);

            // Design-time providers
            var httpContextAccessor = new HttpContextAccessor();
            var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
            var logger = loggerFactory.CreateLogger<AppDbContext>();

            var currentUserProvider = new CurrentUserProvider(
                httpContextAccessor,
                new Microsoft.Extensions.Caching.Memory.MemoryCache(
                    new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
                loggerFactory.CreateLogger<CurrentUserProvider>(),
                Microsoft.Extensions.Options.Options.Create(new CurrentUserSettings()));

            return new AppDbContext(builder.Options, currentUserProvider, logger);
        }

        private static IConfiguration LoadConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{environment}.json", true)
                .AddEnvironmentVariables()
                .Build();
        }

        private static void ConfigureDatabase(DbContextOptionsBuilder builder, DbFactorySettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.DefaultConnection))
                throw new InvalidOperationException("Connection string não configurada");

            builder
                .UseSqlite(settings.DefaultConnection, options =>
                {
                    options.CommandTimeout(settings.CommandTimeout);
                    options.EnableRetryOnFailure(
                        maxRetryCount: settings.MaxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(settings.RetryWaitSeconds),
                        errorNumbersToAdd: null);
                    options.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                })
                .EnableDetailedErrors(settings.EnableDetailedErrors)
                .EnableSensitiveDataLogging(settings.EnableSensitiveDataLogging);
        }
    }

    /// <summary>
    /// Factory para criação do DbContext em runtime com suporte a múltiplos bancos
    /// </summary>
    public class RuntimeAppDbContextFactory
    {
        private readonly DbFactorySettings _settings;
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly ILogger<AppDbContext> _logger;
        private readonly Random _random = new();
        private readonly AsyncRetryPolicy _retryPolicy;

        public RuntimeAppDbContextFactory(
            DbFactorySettings settings,
            ICurrentUserProvider currentUserProvider,
            ILogger<AppDbContext> logger)
        {
            _settings = settings;
            _currentUserProvider = currentUserProvider;
            _logger = logger;

            // Configurar política de retry
            _retryPolicy = Policy
                .Handle<DbException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    _settings.MaxRetryCount,
                    attempt => TimeSpan.FromSeconds(Math.Pow(settings.RetryWaitSeconds, attempt)),
                    OnRetryException);
        }

        /// <summary>
        /// Cria um novo contexto para escrita
        /// </summary>
        public async Task<AppDbContext> CreateAsync()
        {
            return await CreateContextAsync(_settings.DefaultConnection, false);
        }

        /// <summary>
        /// Cria um novo contexto somente leitura
        /// </summary>
        public async Task<AppDbContext> CreateReadOnlyAsync()
        {
            // Se tiver réplicas, escolhe uma aleatoriamente
            var connection = _settings.ReadReplicas.Count > 0
                ? _settings.ReadReplicas.ElementAt(_random.Next(_settings.ReadReplicas.Count)).Value
                : _settings.DefaultConnection;

            return await CreateContextAsync(connection, true);
        }

        /// <summary>
        /// Cria um novo contexto para uma transação específica
        /// </summary>
        public async Task<AppDbContext> CreateForTransactionAsync()
        {
            var context = await CreateContextAsync(_settings.DefaultConnection, false);
            await context.Database.BeginTransactionAsync();
            return context;
        }

        private async Task<AppDbContext> CreateContextAsync(string connectionString, bool readOnly)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>();

            // Configurações base
            builder
                .UseSqlite(connectionString, options =>
                {
                    options.CommandTimeout(_settings.CommandTimeout);
                    options.EnableRetryOnFailure(
                        maxRetryCount: _settings.MaxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(_settings.RetryWaitSeconds),
                        errorNumbersToAdd: null);
                    options.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                })
                .EnableDetailedErrors(_settings.EnableDetailedErrors)
                .EnableSensitiveDataLogging(_settings.EnableSensitiveDataLogging);

            // Configurações específicas para leitura
            if (readOnly)
            {
                builder
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                    .UseModel(await CreateModelAsync());
            }

            var context = new AppDbContext(builder.Options, _currentUserProvider, _logger);

            try
            {
                // Verifica conexão
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await context.Database.CanConnectAsync();
                    return true;
                });

                return context;
            }
            catch (Exception ex)
            {
                await context.DisposeAsync();
                throw new InvalidOperationException("Falha ao conectar ao banco de dados", ex);
            }
        }

        private async Task<Microsoft.EntityFrameworkCore.Metadata.IModel> CreateModelAsync()
        {
            // Cache do modelo para contextos somente leitura
            using var tempContext = await CreateAsync();
            return tempContext.Model;
        }

        private void OnRetryException(Exception ex, TimeSpan waitTime, int attempt, Context context)
        {
            _logger.LogWarning(
                ex,
                "Tentativa {Attempt} de {MaxRetries} falhou. Aguardando {WaitTime} segundos...",
                attempt, _settings.MaxRetryCount, waitTime.TotalSeconds);
        }
    }
}