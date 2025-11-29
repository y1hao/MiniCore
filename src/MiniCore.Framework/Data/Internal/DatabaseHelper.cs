using System.Data;
using Microsoft.Data.Sqlite;

namespace MiniCore.Framework.Data.Internal;

/// <summary>
/// Helper class for database operations.
/// </summary>
internal static class DatabaseHelper
{
    /// <summary>
    /// Creates a SQLite connection.
    /// </summary>
    public static SqliteConnection CreateConnection(string connectionString)
    {
        return new SqliteConnection(connectionString);
    }

    /// <summary>
    /// Executes a query and returns a DataTable.
    /// </summary>
    public static async Task<DataTable> ExecuteQueryAsync(SqliteConnection connection, string sql, params object?[] parameters)
    {
        var dataTable = new DataTable();
        
        await using var command = new SqliteCommand(sql, connection);
        AddParameters(command, parameters);
        
        await using var reader = await command.ExecuteReaderAsync();
        
        // Create columns based on schema
        if (reader.HasRows)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var columnType = reader.GetFieldType(i) ?? typeof(string);
                dataTable.Columns.Add(columnName, columnType);
            }
            
            // Read rows
            while (await reader.ReadAsync())
            {
                var row = dataTable.NewRow();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                    row[i] = value;
                }
                dataTable.Rows.Add(row);
            }
        }
        
        return dataTable;
    }

    /// <summary>
    /// Executes a non-query command (INSERT, UPDATE, DELETE) and returns the number of rows affected.
    /// </summary>
    public static async Task<int> ExecuteNonQueryAsync(SqliteConnection connection, string sql, params object?[] parameters)
    {
        await using var command = new SqliteCommand(sql, connection);
        AddParameters(command, parameters);
        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Executes a scalar query and returns the result.
    /// </summary>
    public static async Task<object?> ExecuteScalarAsync(SqliteConnection connection, string sql, params object?[] parameters)
    {
        await using var command = new SqliteCommand(sql, connection);
        AddParameters(command, parameters);
        return await command.ExecuteScalarAsync();
    }

    /// <summary>
    /// Adds parameters to a SQLite command.
    /// </summary>
    private static void AddParameters(SqliteCommand command, object?[] parameters)
    {
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = new SqliteParameter($"@p{i}", parameters[i] ?? DBNull.Value);
            command.Parameters.Add(parameter);
        }
    }

    /// <summary>
    /// Creates a table if it doesn't exist based on entity type.
    /// </summary>
    public static async Task CreateTableIfNotExistsAsync(SqliteConnection connection, Type entityType, string tableName)
    {
        var properties = entityType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => p.PropertyType.IsPrimitive || p.PropertyType == typeof(string) || p.PropertyType == typeof(DateTime) || 
                       p.PropertyType == typeof(DateTime?) || Nullable.GetUnderlyingType(p.PropertyType) != null)
            .ToList();

        if (properties.Count == 0)
            return;

        var columnDefinitions = new List<string>();
        foreach (var property in properties)
        {
            var columnName = property.Name;
            var sqlType = GetSqlType(property.PropertyType);
            var isPrimaryKey = columnName.Equals("Id", StringComparison.OrdinalIgnoreCase);
            var isRequired = !IsNullableType(property.PropertyType);

            var definition = $"[{columnName}] {sqlType}";
            if (isPrimaryKey)
            {
                definition += " PRIMARY KEY";
                if (property.PropertyType == typeof(int))
                {
                    definition += " AUTOINCREMENT";
                }
            }
            else if (!isRequired)
            {
                definition += " NULL";
            }
            else
            {
                definition += " NOT NULL";
            }

            columnDefinitions.Add(definition);
        }

        var createTableSql = $"CREATE TABLE IF NOT EXISTS [{tableName}] ({string.Join(", ", columnDefinitions)})";
        await ExecuteNonQueryAsync(connection, createTableSql);
    }

    /// <summary>
    /// Gets the SQL type for a .NET type.
    /// </summary>
    private static string GetSqlType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.Name switch
        {
            nameof(Int32) => "INTEGER",
            nameof(Int64) => "INTEGER",
            nameof(String) => "TEXT",
            nameof(DateTime) => "TEXT",
            nameof(Boolean) => "INTEGER",
            nameof(Decimal) => "REAL",
            nameof(Double) => "REAL",
            nameof(Single) => "REAL",
            _ => "TEXT"
        };
    }

    /// <summary>
    /// Checks if a type is nullable.
    /// </summary>
    private static bool IsNullableType(Type type)
    {
        return !type.IsValueType || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
    }
}

