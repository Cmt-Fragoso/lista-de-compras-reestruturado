using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using ListaCompras.Core.Services;

namespace ListaCompras.Core.Data
{
    public static class DbContextExtensions
    {
        /// <summary>
        /// Adiciona os serviços de banco de dados ao container de DI
        /// </summary>
        public static IServiceCollection AddDatabaseServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Carrega configurações
            var settings = configuration
                .GetSection("Database")
                .Get<DbFactorySettings>() ?? new DbFactorySettings();

            services.AddSingleton(settings);

            // Configura EF Core
            services.AddDbContext<AppDbContext>((provider, options) =>
            {
                options
                    .UseSqlite(settings.DefaultConnection, sqliteOptions =>
                    {
                        sqliteOptions.CommandTimeout(settings.CommandTimeout);
                        sqliteOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                        sqliteOptions.EnableRetryOnFailure(
                            maxRetryCount: settings.MaxRetryCount,
                            maxRetryDelay: TimeSpan.FromSeconds(settings.RetryWaitSeconds),
                            errorNumbersToAdd: null);
                    })
                    .EnableDetailedErrors(settings.EnableDetailedErrors)
                    .EnableSensitiveDataLogging(settings.EnableSensitiveDataLogging);

                if (settings.PoolSize > 0)
                {
                    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                }
            }, ServiceLifetime.Scoped);

            // Adiciona factory
            services.AddSingleton<RuntimeAppDbContextFactory>();

            // Adiciona repositórios
            services.Scan(scan => scan
                .FromAssemblyOf<AppDbContext>()
                .AddClasses(classes => classes.AssignableTo(typeof(IRepository<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            return services;
        }

        /// <summary>
        /// Garante que o banco de dados está criado e atualizado
        /// </summary>
        public static async Task EnsureDatabaseAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            if (await context.Database.CanConnectAsync())
            {
                if ((await context.Database.GetPendingMigrationsAsync()).Any())
                {
                    await context.MigrateDatabaseAsync();
                }
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }
        }

        /// <summary>
        /// Executa uma ação com retry em caso de falha
        /// </summary>
        public static async Task<T> ExecuteWithRetryAsync<T>(
            this RuntimeAppDbContextFactory factory,
            Func<AppDbContext, Task<T>> action,
            bool readOnly = false)
        {
            AppDbContext context = null;
            try
            {
                context = readOnly 
                    ? await factory.CreateReadOnlyAsync()
                    : await factory.CreateAsync();
                
                return await action(context);
            }
            finally
            {
                if (context != null)
                    await context.DisposeAsync();
            }
        }

        /// <summary>
        /// Executa uma ação dentro de uma transação
        /// </summary>
        public static async Task<T> ExecuteInTransactionAsync<T>(
            this RuntimeAppDbContextFactory factory,
            Func<AppDbContext, Task<T>> action)
        {
            AppDbContext context = null;
            try
            {
                context = await factory.CreateForTransactionAsync();
                var result = await action(context);
                await context.Database.CommitTransactionAsync();
                return result;
            }
            catch
            {
                if (context?.Database.CurrentTransaction != null)
                    await context.Database.RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (context != null)
                    await context.DisposeAsync();
            }
        }
    }
}