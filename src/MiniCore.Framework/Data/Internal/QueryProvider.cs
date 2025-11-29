using System.Data;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Linq.Expressions;
using MiniCore.Framework.Data;
using MiniCore.Framework.Data.Abstractions;

namespace MiniCore.Framework.Data.Internal;

/// <summary>
/// Provides query execution for DbSet.
/// </summary>
internal class QueryProvider : IQueryProvider
{
    private readonly DbContext _context;
    private readonly string _tableName;

    public QueryProvider(DbContext context, string tableName)
    {
        _context = context;
        _tableName = tableName;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        var elementType = TypeSystem.GetElementType(expression.Type);
        try
        {
            return (IQueryable)Activator.CreateInstance(typeof(DbSet<>).MakeGenericType(elementType), _context, _tableName, expression)!;
        }
        catch (System.Reflection.TargetInvocationException tie)
        {
            throw tie.InnerException!;
        }
    }

    IQueryable<TResult> IQueryProvider.CreateQuery<TResult>(Expression expression)
    {
        if (!typeof(TResult).IsClass)
        {
            throw new InvalidOperationException($"DbSet only supports reference types, but {typeof(TResult)} is not a reference type.");
        }
        // Use reflection to create DbSet<TResult> to satisfy generic constraint
        var dbSetType = typeof(DbSet<>).MakeGenericType(typeof(TResult));
        var constructor = dbSetType.GetConstructor(new[] { typeof(DbContext), typeof(string), typeof(Expression) })
            ?? throw new InvalidOperationException($"Failed to find constructor for DbSet<{typeof(TResult)}>");
        return (IQueryable<TResult>)constructor.Invoke(new object[] { _context, _tableName, expression })!;
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
        
        using var connection = DatabaseHelper.CreateConnection(_context.ConnectionString!);
        await connection.OpenAsync(cancellationToken);
        
        var dataTable = await DatabaseHelper.ExecuteQueryAsync(connection, queryInfo.Sql, queryInfo.Parameters.ToArray());
        
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
                var list = Activator.CreateInstance(listType, results);
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

