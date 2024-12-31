using Microsoft.EntityFrameworkCore;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Data
{
    /// <summary>
    /// Contexto principal do Entity Framework
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<ItemModel> Itens { get; set; }
        public DbSet<ListaModel> Listas { get; set; }
        public DbSet<CategoriaModel> Categorias { get; set; }
        public DbSet<PrecoModel> Precos { get; set; }
        public DbSet<UsuarioModel> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração Item
            modelBuilder.Entity<ItemModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Descricao).HasMaxLength(500);
                entity.Property(e => e.Quantidade).HasPrecision(10, 2);
                entity.Property(e => e.PrecoEstimado).HasPrecision(10, 2);
                entity.Property(e => e.Version).IsRowVersion();
            });

            // Configuração Lista
            modelBuilder.Entity<ListaModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Descricao).HasMaxLength(500);
                entity.Property(e => e.OrcamentoPrevisto).HasPrecision(10, 2);
                entity.Property(e => e.ValorTotal).HasPrecision(10, 2);
                entity.Property(e => e.Version).IsRowVersion();
                
                entity.HasMany(e => e.Itens)
                      .WithOne()
                      .HasForeignKey(e => e.ListaId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuração Categoria
            modelBuilder.Entity<CategoriaModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Descricao).HasMaxLength(200);
                entity.Property(e => e.Cor).HasMaxLength(7);
                entity.Property(e => e.Icone).HasMaxLength(50);
                entity.Property(e => e.Version).IsRowVersion();
            });

            // Configuração Preço
            modelBuilder.Entity<PrecoModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Valor).HasPrecision(10, 2);
                entity.Property(e => e.Local).HasMaxLength(200);
                entity.Property(e => e.Observacoes).HasMaxLength(500);
                entity.Property(e => e.Version).IsRowVersion();
            });

            // Configuração Usuário
            modelBuilder.Entity<UsuarioModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.SenhaHash).IsRequired();
                entity.Property(e => e.DispositivoId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Version).IsRowVersion();

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.DispositivoId);
            });
        }
    }
}