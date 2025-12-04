using System.Data;
using System.Text;

namespace MiniCore.Framework.Data.Internal;

/// <summary>
/// Builds SQL queries for CRUD operations.
/// </summary>
internal static class QueryBuilder
{
    /// <summary>
    /// Builds a SELECT query.
    /// </summary>
    public static string BuildSelectQuery(string tableName, string? whereClause = null, string? orderBy = null, int? skip = null, int? take = null)
    {
        var sql = new StringBuilder();
        sql.Append($"SELECT * FROM {EscapeTableName(tableName)}");

        if (!string.IsNullOrEmpty(whereClause))
        {
            sql.Append($" WHERE {whereClause}");
        }

        if (!string.IsNullOrEmpty(orderBy))
        {
            sql.Append($" ORDER BY {orderBy}");
        }

        // Only add LIMIT if it's a positive value (LIMIT 0 returns no rows)
        if (take.HasValue && take.Value > 0)
        {
            sql.Append($" LIMIT {take.Value}");
        }

        if (skip.HasValue && skip.Value > 0)
        {
            sql.Append($" OFFSET {skip.Value}");
        }

        return sql.ToString();
    }

    /// <summary>
    /// Builds an INSERT query.
    /// </summary>
    public static string BuildInsertQuery(string tableName, IEnumerable<string> columnNames)
    {
        var columns = columnNames.ToList();
        var columnList = string.Join(", ", columns.Select(EscapeColumnName));
        var valuePlaceholders = string.Join(", ", columns.Select((_, i) => $"@p{i}"));

        return $"INSERT INTO {EscapeTableName(tableName)} ({columnList}) VALUES ({valuePlaceholders})";
    }

    /// <summary>
    /// Builds an UPDATE query.
    /// </summary>
    public static string BuildUpdateQuery(string tableName, IEnumerable<string> columnNames, string whereClause)
    {
        var columns = columnNames.ToList();
        var setClause = string.Join(", ", columns.Select((c, i) => $"{EscapeColumnName(c)} = @p{i}"));

        return $"UPDATE {EscapeTableName(tableName)} SET {setClause} WHERE {whereClause}";
    }

    /// <summary>
    /// Builds a DELETE query.
    /// </summary>
    public static string BuildDeleteQuery(string tableName, string whereClause)
    {
        return $"DELETE FROM {EscapeTableName(tableName)} WHERE {whereClause}";
    }

    /// <summary>
    /// Builds a COUNT query.
    /// </summary>
    public static string BuildCountQuery(string tableName, string? whereClause = null)
    {
        var sql = $"SELECT COUNT(*) FROM {EscapeTableName(tableName)}";
        if (!string.IsNullOrEmpty(whereClause))
        {
            sql += $" WHERE {whereClause}";
        }
        return sql;
    }

    /// <summary>
    /// Escapes a table name (simple implementation, can be extended for SQL injection protection).
    /// </summary>
    private static string EscapeTableName(string tableName)
    {
        // SQLite uses square brackets or double quotes
        return $"[{tableName}]";
    }

    /// <summary>
    /// Escapes a column name.
    /// </summary>
    private static string EscapeColumnName(string columnName)
    {
        return $"[{columnName}]";
    }
}

