using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using ListaCompras.Core.Data;
using ListaCompras.Core.Models;
using System.Linq;

namespace ListaCompras.Tests.Integration
{
    public class DatabaseIntegrationTests : IClassFixture<DbFixture>
    {
        private readonly DbFixture _fixture;

        public DatabaseIntegrationTests(DbFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task DatabaseMigration_ShouldCreateAllTables()
        {
            // Arrange & Act
            var tables = await _fixture.GetAllTableNames();

            // Assert
            tables.Should().Contain("Users");
            tables.Should().Contain("Lists");
            tables.Should().Contain("Items");
            tables.Should().Contain("Categories");
            tables.Should().Contain("Prices");
        }

        [Fact]
        public async Task DatabaseIndexes_ShouldBeCreatedCorrectly()
        {
            // Arrange & Act
            var indexes = await _fixture.GetAllIndexes();

            // Assert - Verifica índices críticos
            indexes.Should().Contain(i => i.TableName == "Users" && i.IndexName.Contains("Email"));
            indexes.Should().Contain(i => i.TableName == "Items" && i.IndexName.Contains("ListId"));
            indexes.Should().Contain(i => i.TableName == "Prices" && i.IndexName.Contains("ItemId"));
        }

        [Fact]
        public async Task ConcurrencyControl_ShouldHandleConflicts()
        {
            // Arrange
            var item = await CreateTestItem();
            var context1 = _fixture.CreateContext();
            var context2 = _fixture.CreateContext();

            // Act - Simula edição concorrente
            var item1 = await context1.Items.FindAsync(item.Id);
            var item2 = await context2.Items.FindAsync(item.Id);

            item1.Nome = "Nome 1";
            item2.Nome = "Nome 2";

            await context1.SaveChangesAsync();

            // Assert
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => context2.SaveChangesAsync());
        }

        [Fact]
        public async Task Transactions_ShouldRollbackOnError()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var initialCount = await context.Items.CountAsync();

            // Act
            try
            {
                using var transaction = await context.Database.BeginTransactionAsync();

                // Adiciona item válido
                context.Items.Add(new ItemModel
                {
                    Nome = "Item 1",
                    Quantidade = 1,
                    Unidade = "Un",
                    PrecoEstimado = 10,
                    ListaId = 1
                });
                await context.SaveChangesAsync();

                // Tenta adicionar item inválido
                context.Items.Add(new ItemModel()); // Inválido, sem nome
                await context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                // Ignora exceção
            }

            // Assert - Verifica que nenhum item foi salvo
            var finalCount = await context.Items.CountAsync();
            finalCount.Should().Be(initialCount);
        }

        [Fact]
        public async Task SoftDelete_ShouldWorkCorrectly()
        {
            // Arrange
            var item = await CreateTestItem();
            var context = _fixture.CreateContext();

            // Act
            item.MarcarComoExcluido(1);
            await context.SaveChangesAsync();

            // Assert
            var itemDeletado = await context.Items.IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.Id == item.Id);
            var itemFiltrado = await context.Items
                .FirstOrDefaultAsync(i => i.Id == item.Id);

            itemDeletado.Should().NotBeNull();
            itemDeletado.Deletado.Should().BeTrue();
            itemFiltrado.Should().BeNull(); // Filtro global aplicado
        }

        [Fact]
        public async Task Relationships_ShouldCascadeCorrectly()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var lista = await CreateTestList();
            var item = await CreateTestItem(lista.Id);

            // Act
            context.Lists.Remove(lista);
            await context.SaveChangesAsync();

            // Assert
            var deletedItem = await context.Items.IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.Id == item.Id);
            deletedItem.Should().BeNull(); // Item foi excluído em cascata
        }

        private async Task<ItemModel> CreateTestItem(int? listaId = null)
        {
            using var context = _fixture.CreateContext();
            
            if (!listaId.HasValue)
            {
                var lista = await CreateTestList();
                listaId = lista.Id;
            }

            var item = new ItemModel
            {
                Nome = "Item de Teste",
                Quantidade = 1,
                Unidade = "Un",
                PrecoEstimado = 10,
                ListaId = listaId.Value
            };

            context.Items.Add(item);
            await context.SaveChangesAsync();
            return item;
        }

        private async Task<ListaModel> CreateTestList()
        {
            using var context = _fixture.CreateContext();
            
            var lista = new ListaModel
            {
                Nome = "Lista de Teste",
                UsuarioId = 1
            };

            context.Lists.Add(lista);
            await context.SaveChangesAsync();
            return lista;
        }
    }

    public class DbFixture : IDisposable
    {
        private readonly string _databaseName;
        private readonly IServiceProvider _serviceProvider;

        public DbFixture()
        {
            _databaseName = Guid.NewGuid().ToString();
            
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Cria e configura o banco
            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        public AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={_databaseName}.db")
                .Options;

            return new AppDbContext(
                options,
                _serviceProvider.GetRequiredService<ICurrentUserProvider>());
        }

        public async Task<string[]> GetAllTableNames()
        {
            using var context = CreateContext();
            var tables = await context.Database.SqlQuery<string>(
                "SELECT name FROM sqlite_master WHERE type='table'").ToArrayAsync();
            return tables;
        }

        public async Task<IndexInfo[]> GetAllIndexes()
        {
            using var context = CreateContext();
            var indexes = await context.Database.SqlQuery<IndexInfo>(
                "SELECT tbl_name as TableName, name as IndexName FROM sqlite_master WHERE type='index'")
                .ToArrayAsync();
            return indexes;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var currentUser = new Mock<ICurrentUserProvider>();
            currentUser.Setup(x => x.GetCurrentUserId()).Returns(1);
            services.AddSingleton(currentUser.Object);
        }

        public void Dispose()
        {
            using var context = CreateContext();
            context.Database.EnsureDeleted();
        }
    }

    public class IndexInfo
    {
        public string TableName { get; set; }
        public string IndexName { get; set; }
    }
}