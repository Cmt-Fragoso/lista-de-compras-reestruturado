using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ListaCompras.Core.Models;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ListaCompras.Core.Data
{
    /// <summary>
    /// Contexto principal do Entity Framework com suporte a concorrência otimista,
    /// validações em nível de banco e pooling otimizado
    /// </summary>
    public class AppDbContext : DbContext
    {
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly ILogger<AppDbContext> _logger;

        // Configurações padrão
        private const int CommandTimeout = 30;
        private const int MaxRetryCount = 3;
        private const int MaxBatchSize = 1000;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ICurrentUserProvider currentUserProvider,
            ILogger<AppDbContext> logger = null) : base(options)
        {
            _currentUserProvider = currentUserProvider;
            _logger = logger;
        }

        public DbSet<ItemModel> Itens { get; set; }
        public DbSet<ListaModel> Listas { get; set; }
        public DbSet<CategoriaModel> Categorias { get; set; }
        public DbSet<PrecoModel> Precos { get; set; }
        public DbSet<UsuarioModel> Usuarios { get; set; }
        public DbSet<MigrationHistory> MigrationHistory { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                    .EnableDetailedErrors()
                    .EnableSensitiveDataLogging()
                    .UseLazyLoadingProxies();

                // Configuração de pooling
                if (Database.IsRelational())
                {
                    optionsBuilder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    
                    // Obter connection do pool
                    var connection = Database.GetDbConnection();
                    if (connection.State != System.Data.ConnectionState.Open)
                        connection.Open();

                    // Configurar timeout
                    using var command = connection.CreateCommand();
                    command.CommandTimeout = CommandTimeout;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração Global
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // Configurar RowVersion para todos
                var rowVersionProperty = entity.FindProperty("RowVersion");
                if (rowVersionProperty != null)
                {
                    rowVersionProperty.SetColumnType("rowversion");
                    rowVersionProperty.SetMaxLength(8);
                    rowVersionProperty.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate;
                }

                // Adicionar shadow properties para auditoria
                entity.AddProperty("CreatedAt", typeof(DateTime));
                entity.AddProperty("UpdatedAt", typeof(DateTime));
                entity.AddProperty("CreatedBy", typeof(string));
                entity.AddProperty("UpdatedBy", typeof(string));
            }

            // Configuração Item
            modelBuilder.Entity<ItemModel>(entity =>
            {
                entity.ToTable("Itens", b => b.IsTemporal());
                
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ListaId, e.CategoriaId }).HasDatabaseName("IX_Itens_Lista_Categoria");
                entity.HasIndex(e => new { e.ListaId, e.IsComprado }).HasDatabaseName("IX_Itens_Lista_Status");
                entity.HasIndex(e => e.DataAtualizacao).HasDatabaseName("IX_Itens_DataAtualizacao");

                entity.Property(e => e.Nome)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                entity.Property(e => e.Descricao)
                    .HasMaxLength(500)
                    .HasColumnType("nvarchar(500)");

                entity.Property(e => e.Quantidade)
                    .HasPrecision(10, 2)
                    .HasDefaultValue(1);

                entity.Property(e => e.PrecoEstimado)
                    .HasPrecision(10, 2)
                    .HasDefaultValue(0);

                // Validações
                entity.HasCheckConstraint("CK_Itens_PrecoEstimado", "[PrecoEstimado] >= 0");
                entity.HasCheckConstraint("CK_Itens_Quantidade", "[Quantidade] > 0");

                // Relacionamentos
                entity.HasOne(e => e.Lista)
                    .WithMany(l => l.Itens)
                    .HasForeignKey(e => e.ListaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Categoria)
                    .WithMany()
                    .HasForeignKey(e => e.CategoriaId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Queries frequentes
                entity.HasQueryFilter(e => !e.Deletado);
            });

            // [... Configurações similares para outras entidades ...]

            // Configuração para migração de dados
            modelBuilder.Entity<MigrationHistory>(entity =>
            {
                entity.ToTable("__MigrationHistory");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Version).IsRequired();
                entity.Property(e => e.AppliedAt).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var currentUserId = _currentUserProvider.GetCurrentUserId();
                var currentTime = DateTime.UtcNow;

                foreach (var entry in ChangeTracker.Entries<BaseModel>())
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            entry.Property("CreatedAt").CurrentValue = currentTime;
                            entry.Property("CreatedBy").CurrentValue = currentUserId.ToString();
                            entry.Property("UpdatedAt").CurrentValue = currentTime;
                            entry.Property("UpdatedBy").CurrentValue = currentUserId.ToString();
                            break;

                        case EntityState.Modified:
                            entry.Property("UpdatedAt").CurrentValue = currentTime;
                            entry.Property("UpdatedBy").CurrentValue = currentUserId.ToString();
                            break;
                    }

                    // Validar entidade antes de salvar
                    var validationResults = new List<ValidationResult>();
                    if (!Validator.TryValidateObject(entry.Entity, new ValidationContext(entry.Entity), validationResults, true))
                    {
                        throw new ValidationException(
                            $"Validação falhou para {entry.Entity.GetType().Name}: {string.Join(", ", validationResults)}");
                    }
                }

                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger?.LogError(ex, "Erro de concorrência ao salvar alterações");
                
                foreach (var entry in ex.Entries)
                {
                    var proposedValues = entry.CurrentValues;
                    var databaseValues = entry.GetDatabaseValues();

                    foreach (var property in proposedValues.Properties)
                    {
                        var proposedValue = proposedValues[property];
                        var databaseValue = databaseValues[property];

                        _logger?.LogWarning(
                            "Conflito de concorrência: Propriedade {Property} - Valor Proposto: {ProposedValue}, Valor no Banco: {DatabaseValue}",
                            property.Name, proposedValue, databaseValue);
                    }
                }

                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao salvar alterações no banco de dados");
                throw;
            }
        }

        public async Task<bool> MigrateDatabaseAsync()
        {
            try
            {
                await Database.MigrateAsync();
                await SaveMigrationHistoryAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro durante migração do banco de dados");
                return false;
            }
        }

        private async Task SaveMigrationHistoryAsync()
        {
            var lastMigration = await Database.GetAppliedMigrationsAsync()
                .ContinueWith(t => t.Result.LastOrDefault());

            if (lastMigration != null)
            {
                var history = new MigrationHistory
                {
                    Version = lastMigration,
                    AppliedAt = DateTime.UtcNow,
                    Description = $"Migration {lastMigration} applied automatically"
                };

                MigrationHistory.Add(history);
                await SaveChangesAsync();
            }
        }

        public override void Dispose()
        {
            _logger?.LogInformation("Disposing AppDbContext");
            base.Dispose();
        }
    }

    /// <summary>
    /// Entidade para controle de histórico de migrações
    /// </summary>
    public class MigrationHistory
    {
        public int Id { get; set; }
        public string Version { get; set; }
        public DateTime AppliedAt { get; set; }
        public string Description { get; set; }
    }
}