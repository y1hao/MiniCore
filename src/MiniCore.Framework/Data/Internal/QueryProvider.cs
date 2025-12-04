using System.Data;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Linq.Expressions;
using MiniCore.Framework.Data;
using MiniCore.Framework.Data.Abstractions;
using MiniCore.Framework.Logging;

namespace MiniCore.Framework.Data.Internal;

/// <summary>
/// Provides query execution for DbSet.
/// </summary>
internal class QueryProvider : IQueryProvider
{
    private readonly DbContext _context;
    private readonly string _tableName;
    private readonly ILogger? _logger;

    public QueryProvider(DbContext context, string tableName, ILogger? logger = null)
    {
        _context = context;
        _tableName = tableName;
        _logger = logger;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        var elementType = TypeSystem.GetElementType(expression.Type);
        var dbSetType = typeof(DbSet<>).MakeGenericType(elementType);
        var constructor = dbSetType.GetConstructor(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(DbContext), typeof(string), typeof(Expression) },
            modifiers: null);

        if (constructor == null)
        {
            throw new InvalidOperationException($"Failed to find constructor for DbSet<{elementType}>");
        }

        return (IQueryable)constructor.Invoke(new object[] { _context, _tableName, expression })!;
    }

    IQueryable<TResult> IQueryProvider.CreateQuery<TResult>(Expression expression)
    {
        // Extract the actual entity type if TResult is IQueryable<T> or IOrderedQueryable<T>
        Type entityType = typeof(TResult);
        if (typeof(TResult).IsGenericType)
        {
            var genericTypeDef = typeof(TResult).GetGenericTypeDefinition();
            if (genericTypeDef == typeof(IQueryable<>) || genericTypeDef == typeof(IOrderedQueryable<>))
            {
                entityType = typeof(TResult).GetGenericArguments()[0];
            }
        }

        if (!entityType.IsClass)
        {
            throw new InvalidOperationException($"DbSet only supports reference types, but {entityType} is not a reference type.");
        }
        
        // Use reflection to create DbSet<entityType> (internal constructor) to satisfy generic constraint
        var dbSetType = typeof(DbSet<>).MakeGenericType(entityType);
        var constructor = dbSetType.GetConstructor(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(DbContext), typeof(string), typeof(Expression) },
            modifiers: null)
            ?? throw new InvalidOperationException($"Failed to find constructor for DbSet<{entityType}>");

        var dbSet = constructor.Invoke(new object[] { _context, _tableName, expression });
        
        // Cast to the requested type (IQueryable<TResult> or IOrderedQueryable<TResult>)
        return (IQueryable<TResult>)dbSet!;
    }

    public object? Execute(Expression expression)
    {
        return ExecuteAsync<object>(expression, CancellationToken.None).GetAwaiter().GetResult();
    }

    public TResult Execute<TResult>(Expression expression)
    {
        if (!typeof(TResult).IsClass)
        {
            throw new InvalidOperationException($"DbSet only supports reference types, but {typeof(TResult)} is not a reference type.");
        }
        var result = ExecuteAsync<TResult>(expression, CancellationToken.None).GetAwaiter().GetResult();
        return result!;
    }

    public async Task<TResult?> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        var queryInfo = QueryTranslator.Translate(expression, _tableName);
        
        // Log the SQL query and parameters
        _logger?.LogDebug("Executing SQL query: {Sql}", queryInfo.Sql);
        if (queryInfo.Parameters.Count > 0)
        {
            _logger?.LogDebug("Query parameters: {Parameters}", string.Join(", ", queryInfo.Parameters));
        }
        
        using var connection = DatabaseHelper.CreateConnection(_context.ConnectionString!);
        await connection.OpenAsync(cancellationToken);
        
        var dataTable = await DatabaseHelper.ExecuteQueryAsync(connection, queryInfo.Sql, queryInfo.Parameters.ToArray());
        
        _logger?.LogDebug("Query returned {RowCount} rows", dataTable.Rows.Count);
        
        // Get the entity type (TEntity from DbSet<TEntity>)
        var entityType = typeof(TResult);
        if (entityType.IsGenericType && entityType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            entityType = entityType.GetGenericArguments()[0];
        }
        else if (entityType.IsGenericType && entityType.GetGenericTypeDefinition() == typeof(IQueryable<>))
        {
            entityType = entityType.GetGenericArguments()[0];
        }
        else if (entityType.IsGenericType && entityType.GetGenericTypeDefinition() == typeof(List<>))
        {
            entityType = entityType.GetGenericArguments()[0];
        }

        var results = new List<object>();
        foreach (DataRow row in dataTable.Rows)
        {
            var entity = ObjectMapper.MapToObject(entityType, row);
            if (entity != null)
            {
                results.Add(entity);
            }
        }

        // Always apply Skip/Take in-memory (we don't apply them in SQL to handle runtime values)
        // This handles cases where Skip/Take use runtime values (like method parameters)
        if (queryInfo.SkipCount.HasValue && queryInfo.SkipCount.Value > 0)
        {
            var skipValue = queryInfo.SkipCount.Value;
            _logger?.LogDebug("Applying Skip({SkipValue}) in-memory", skipValue);
            if (skipValue < results.Count)
            {
                results = results.Skip(skipValue).ToList();
            }
            else
            {
                results.Clear();
            }
        }

        // Apply Take in-memory
        // Only apply if > 0 (if it's 0, we assume it couldn't be evaluated at translation time)
        // Note: We can't distinguish between "Take(0)" (intentional) and "Take(pageSize)" where pageSize couldn't be evaluated (defaulted to 0)
        // So we'll only apply Take when the value is > 0
        if (queryInfo.TakeCount.HasValue && queryInfo.TakeCount.Value > 0)
        {
            var takeValue = queryInfo.TakeCount.Value;
            _logger?.LogDebug("Applying Take({TakeValue}) in-memory", takeValue);
            results = results.Take(takeValue).ToList();
        }
        
        _logger?.LogDebug("Final result count: {Count}", results.Count);

        // Apply Select projection if needed (in-memory)
        if (queryInfo.SelectExpression != null)
        {
            var queryable = results.AsQueryable();
            var selectMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == "Select" && m.GetParameters().Length == 2);
            var genericSelectMethod = selectMethod.MakeGenericMethod(entityType, typeof(TResult).GetGenericArguments()[0]);
            var projected = genericSelectMethod.Invoke(null, new object[] { queryable, queryInfo.SelectExpression });
            
            if (projected is IQueryable<TResult> queryableResult)
            {
                return (TResult)(object)queryableResult.ToList();
            }
        }

        // Return as IEnumerable<TEntity>, IQueryable<TEntity>, or List<TEntity>
        if (typeof(TResult).IsGenericType)
        {
            var genericTypeDef = typeof(TResult).GetGenericTypeDefinition();
            if (genericTypeDef == typeof(IEnumerable<>) || genericTypeDef == typeof(IQueryable<>) || genericTypeDef == typeof(List<>))
            {
                var listType = typeof(List<>).MakeGenericType(entityType);
                // Convert List<object> to IEnumerable<entityType> for the constructor
                var enumerableType = typeof(IEnumerable<>).MakeGenericType(entityType);
                var castMethod = typeof(Enumerable).GetMethod("Cast")!.MakeGenericMethod(entityType);
                var typedEnumerable = castMethod.Invoke(null, new object[] { results });
                var list = Activator.CreateInstance(listType, typedEnumerable);
                if (list != null)
                {
                    return (TResult)list;
                }
            }
        }

        // Return single entity (shouldn't happen for queries, but handle it)
        if (results.Count > 0 && typeof(TResult).IsAssignableFrom(entityType))
        {
            return (TResult)results[0];
        }

        // Return default for collections
        if (typeof(TResult).IsGenericType)
        {
            var genericTypeDef = typeof(TResult).GetGenericTypeDefinition();
            if (genericTypeDef == typeof(IEnumerable<>) || genericTypeDef == typeof(IQueryable<>) || genericTypeDef == typeof(List<>))
            {
                var listType = typeof(List<>).MakeGenericType(entityType);
                var emptyList = Activator.CreateInstance(listType);
                return emptyList != null ? (TResult)emptyList : default(TResult)!;
            }
        }

        return default(TResult);
    }

    public async Task<int> ExecuteCountAsync(Expression expression, CancellationToken cancellationToken)
    {
        var queryInfo = QueryTranslator.TranslateCount(expression, _tableName);
        
        using var connection = DatabaseHelper.CreateConnection(_context.ConnectionString!);
        await connection.OpenAsync(cancellationToken);
        
        var result = await DatabaseHelper.ExecuteScalarAsync(connection, queryInfo.Sql, queryInfo.Parameters.ToArray());
        return Convert.ToInt32(result ?? 0);
    }
}

/// <summary>
/// Helper class for getting element types.
/// </summary>
internal static class TypeSystem
{
    internal static Type GetElementType(Type seqType)
    {
        var ienum = FindIEnumerable(seqType);
        if (ienum == null) return seqType;
        return ienum.GetGenericArguments()[0];
    }

    private static Type? FindIEnumerable(Type seqType)
    {
        if (seqType == null || seqType == typeof(string))
            return null;
        if (seqType.IsArray)
            return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType()!);
        if (seqType.IsGenericType)
        {
            foreach (var arg in seqType.GetGenericArguments())
            {
                var ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                if (ienum.IsAssignableFrom(seqType))
                    return ienum;
            }
        }
        var ifaces = seqType.GetInterfaces();
        if (ifaces != null && ifaces.Length > 0)
        {
            foreach (var iface in ifaces)
            {
                var ienum = FindIEnumerable(iface);
                if (ienum != null) return ienum;
            }
        }
        if (seqType.BaseType != null && seqType.BaseType != typeof(object))
            return FindIEnumerable(seqType.BaseType);
        return null;
    }
}

