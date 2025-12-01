using System.Data;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Linq.Expressions;
using MiniCore.Framework.Data.Abstractions;
using MiniCore.Framework.Data.Internal;

namespace MiniCore.Framework.Data;

/// <summary>
/// A DbSet can be used to query and save instances of TEntity.
/// </summary>
/// <typeparam name="TEntity">The type of entity.</typeparam>
public class DbSet<TEntity> : IQueryable<TEntity> where TEntity : class
{
    private readonly DbContext _context;
    private readonly string _tableName;
    private readonly QueryProvider _queryProvider;

    public DbSet(DbContext context, string tableName)
    {
        _context = context;
        _tableName = tableName;
        _queryProvider = new QueryProvider(context, tableName);
        Expression = Expression.Constant(this);
    }

    internal DbSet(DbContext context, string tableName, Expression expression)
    {
        _context = context;
        _tableName = tableName;
        _queryProvider = new QueryProvider(context, tableName);
        Expression = expression;
    }

    public Expression Expression { get; }
    public Type ElementType => typeof(TEntity);
    public IQueryProvider Provider => _queryProvider;

    public IEnumerator<TEntity> GetEnumerator()
    {
        return Execute().GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Executes the query and returns the results.
    /// </summary>
    private IEnumerable<TEntity> Execute()
    {
        return _queryProvider.Execute<IEnumerable<TEntity>>(Expression) ?? Enumerable.Empty<TEntity>();
    }

    /// <summary>
    /// Executes the query asynchronously and returns a list.
    /// </summary>
    public async Task<List<TEntity>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var results = await _queryProvider.ExecuteAsync<IEnumerable<TEntity>>(Expression, cancellationToken);
        return results?.ToList() ?? new List<TEntity>();
    }

    /// <summary>
    /// Executes the query asynchronously and returns the first element, or null if no element is found.
    /// </summary>
    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = this;
        if (predicate != null)
        {
            query = query.Where(predicate);
        }
        var results = await ((DbSet<TEntity>)query).ToListAsync(cancellationToken);
        return results.FirstOrDefault();
    }

    /// <summary>
    /// Determines whether any element satisfies a condition.
    /// </summary>
    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = this;
        if (predicate != null)
        {
            query = query.Where(predicate);
        }
        var count = await ((DbSet<TEntity>)query).CountAsync(cancellationToken);
        return count > 0;
    }

    /// <summary>
    /// Returns the number of elements.
    /// </summary>
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _queryProvider.ExecuteCountAsync(Expression, cancellationToken);
    }

    /// <summary>
    /// Finds an entity with the given primary key value.
    /// </summary>
    public async Task<TEntity?> FindAsync(object?[]? keyValues, CancellationToken cancellationToken = default)
    {
        if (keyValues == null || keyValues.Length == 0)
            return null;

        var keyPropertyName = ObjectMapper.GetPrimaryKeyPropertyName(typeof(TEntity));
        if (string.IsNullOrEmpty(keyPropertyName))
            return null;

        var keyValue = keyValues[0];
        var whereClause = $"[{keyPropertyName}] = @p0";
        var sql = QueryBuilder.BuildSelectQuery(_tableName, whereClause);

        using var connection = DatabaseHelper.CreateConnection(_context.ConnectionString!);
        await connection.OpenAsync(cancellationToken);
        var dataTable = await DatabaseHelper.ExecuteQueryAsync(connection, sql, keyValue);
        
        if (dataTable.Rows.Count == 0)
            return null;

        return (TEntity)ObjectMapper.MapToObject(typeof(TEntity), dataTable.Rows[0]);
    }

    /// <summary>
    /// Adds an entity to the context.
    /// </summary>
    public void Add(TEntity entity)
    {
        _context.AddEntity(entity, _tableName);
    }

    /// <summary>
    /// Removes an entity from the context.
    /// </summary>
    public void Remove(TEntity entity)
    {
        _context.RemoveEntity(entity, _tableName);
    }

    /// <summary>
    /// Removes multiple entities from the context.
    /// </summary>
    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
        {
            Remove(entity);
        }
    }
}

