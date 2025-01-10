using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace ListaCompras.Core.Data
{
    /// <summary>
    /// Leitor otimizado para operações em massa
    /// </summary>
    public class BulkReader
    {
        private readonly AppDbContext _context;

        public BulkReader(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Executa uma query SQL diretamente usando ADO.NET para melhor performance
        /// </summary>
        public async Task<List<T>> ExecuteQueryAsync<T>(string sql, params object[] parameters) where T : class, new()
        {
            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@p{i}";
                    parameter.Value = parameters[i] ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }
            }

            await _context.Database.OpenConnectionAsync();

            try
            {
                using var result = await command.ExecuteReaderAsync();
                var list = new List<T>();
                var props = typeof(T).GetProperties();

                while (await result.ReadAsync())
                {
                    var item = new T();
                    for (int i = 0; i < result.FieldCount; i++)
                    {
                        if (!result.IsDBNull(i))
                        {
                            var prop = props.FirstOrDefault(p => 
                                p.Name.Equals(result.GetName(i), StringComparison.OrdinalIgnoreCase));
                            
                            if (prop != null && prop.CanWrite)
                            {
                                var value = result.GetValue(i);
                                if (value != DBNull.Value)
                                {
                                    if (prop.PropertyType.IsEnum)
                                    {
                                        value = Enum.ToObject(prop.PropertyType, value);
                                    }
                                    prop.SetValue(item, value);
                                }
                            }
                        }
                    }
                    list.Add(item);
                }

                return list;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        /// <summary>
        /// Executa consulta paginada com ordenação otimizada
        /// </summary>
        public async Task<(List<T> Items, int Total)> GetPagedAsync<T>(
            IQueryable<T> query,
            int page,
            int pageSize,
            string sortField = null,
            bool ascending = true) where T : class
        {
            // Conta total de registros
            var total = await query.CountAsync();

            // Aplica ordenação se especificada
            if (!string.IsNullOrEmpty(sortField))
            {
                var prop = typeof(T).GetProperty(sortField);
                if (prop != null)
                {
                    var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
                    var property = System.Linq.Expressions.Expression.Property(parameter, prop);
                    var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, object>>(
                        System.Linq.Expressions.Expression.Convert(property, typeof(object)),
                        parameter);

                    query = ascending ? query.OrderBy(lambda) : query.OrderByDescending(lambda);
                }
            }

            // Aplica paginação
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        /// <summary>
        /// Executa consulta com carregamento seletivo de propriedades
        /// </summary>
        public IQueryable<T> SelectProperties<T>(IQueryable<T> query, params string[] properties) where T : class
        {
            if (properties == null || properties.Length == 0)
                return query;

            // Cria expressão de seleção dinâmica
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
            var bindings = properties.Select(propertyName =>
            {
                var property = typeof(T).GetProperty(propertyName);
                if (property == null)
                    throw new ArgumentException($"Property {propertyName} not found");

                var propertyAccess = System.Linq.Expressions.Expression.Property(parameter, property);
                return System.Linq.Expressions.Expression.Bind(property, propertyAccess);
            });

            var ctor = System.Linq.Expressions.Expression.New(typeof(T));
            var init = System.Linq.Expressions.Expression.MemberInit(ctor, bindings);
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, T>>(init, parameter);

            return query.Select(lambda);
        }

        /// <summary>
        /// Cria índices para otimizar consultas frequentes
        /// </summary>
        public async Task CreateOptimizationIndexesAsync()
        {
            // Índices são criados apenas uma vez
            var connection = _context.Database.GetDbConnection() as SqliteConnection;
            if (connection == null)
                return;

            await using var command = connection.CreateCommand();

            // Índice para busca de itens por lista
            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_Itens_ListaId ON Itens(ListaId)
                WHERE ListaId IS NOT NULL;";
            await command.ExecuteNonQueryAsync();

            // Índice para busca de preços recentes
            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_Precos_DataPreco ON Precos(DataPreco DESC);";
            await command.ExecuteNonQueryAsync();

            // Índice para busca de usuários por email
            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_Usuarios_Email ON Usuarios(Email)
                WHERE Email IS NOT NULL;";
            await command.ExecuteNonQueryAsync();

            // Índice para busca de categorias por pai
            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_Categorias_CategoriaPaiId ON Categorias(CategoriaPaiId)
                WHERE CategoriaPaiId IS NOT NULL;";
            await command.ExecuteNonQueryAsync();

            // Índice para ordenação de categorias
            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_Categorias_Ordem ON Categorias(Ordem ASC);";
            await command.ExecuteNonQueryAsync();

            // Índice para filtragem de itens comprados
            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_Itens_Comprado ON Itens(Comprado)
                WHERE Comprado = 1;";
            await command.ExecuteNonQueryAsync();

            // Índice para histórico de preços por item
            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_Precos_ItemId_DataPreco 
                ON Precos(ItemId, DataPreco DESC);";
            await command.ExecuteNonQueryAsync();

            // Índice para status de listas
            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_Listas_Status_UsuarioId 
                ON Listas(Status, UsuarioId);";
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Otimiza o banco de dados
        /// </summary>
        public async Task OptimizeDatabaseAsync()
        {
            var connection = _context.Database.GetDbConnection() as SqliteConnection;
            if (connection == null)
                return;

            await using var command = connection.CreateCommand();

            // Atualiza estatísticas
            command.CommandText = "ANALYZE;";
            await command.ExecuteNonQueryAsync();

            // Compacta banco
            command.CommandText = "VACUUM;";
            await command.ExecuteNonQueryAsync();
        }
    }
}