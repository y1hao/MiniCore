using Microsoft.Data.Sqlite;
using MiniCore.Framework.Data.Abstractions;
using MiniCore.Framework.Data.Internal;
using MiniCore.Framework.Logging;

namespace MiniCore.Framework.Data;

/// <summary>
/// A DbContext instance represents a session with the database and can be used to query and save instances of entities.
/// </summary>
public abstract class DbContext : IDbContext
{
    private readonly DbContextOptions _options;
    private readonly Dictionary<object, EntityState> _trackedEntities = new();
    private readonly ILogger? _logger;
    private bool _disposed;

    protected DbContext(DbContextOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrEmpty(options.ConnectionString))
        {
            throw new InvalidOperationException("Connection string must be provided.");
        }

        // Create logger if logger factory is available
        _logger = _options.LoggerFactory?.CreateLogger(GetType().Name);
    }

    /// <summary>
    /// Gets the connection string.
    /// </summary>
    internal string ConnectionString => _options.ConnectionString!;

    /// <summary>
    /// Gets the logger for this context.
    /// </summary>
    internal ILogger? Logger => _logger;

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int changesCount = 0;

        if (_trackedEntities.Count == 0)
        {
            return 0;
        }

        using var connection = DatabaseHelper.CreateConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var kvp in _trackedEntities.ToList())
        {
            var entity = kvp.Key;
            var state = kvp.Value;
            var entityType = entity.GetType();
            var tableName = GetTableName(entityType);

            if (state == EntityState.Added)
            {
                var propertyValues = ObjectMapper.GetPropertyValues(entity);
                // Remove Id if it's 0 (auto-increment)
                if (propertyValues.ContainsKey("Id") && propertyValues["Id"] is int id && id == 0)
                {
                    propertyValues.Remove("Id");
                }

                var columnNames = propertyValues.Keys.ToList();
                var values = propertyValues.Values.ToList();
                var sql = QueryBuilder.BuildInsertQuery(tableName, columnNames);
                
                await DatabaseHelper.ExecuteNonQueryAsync(connection, sql, values.ToArray());
                changesCount++;

                // Update the entity with the generated ID if it's an auto-increment
                var idProperty = entityType.GetProperty("Id");
                if (idProperty != null && idProperty.PropertyType == typeof(int))
                {
                    var lastInsertRowId = await DatabaseHelper.ExecuteScalarAsync(connection, "SELECT last_insert_rowid()");
                    if (lastInsertRowId != null)
                    {
                        idProperty.SetValue(entity, Convert.ToInt32(lastInsertRowId));
                    }
                }
            }
            else if (state == EntityState.Deleted)
            {
                var keyPropertyName = ObjectMapper.GetPrimaryKeyPropertyName(entityType);
                if (!string.IsNullOrEmpty(keyPropertyName))
                {
                    var keyProperty = entityType.GetProperty(keyPropertyName);
                    if (keyProperty != null)
                    {
                        var keyValue = keyProperty.GetValue(entity);
                        var whereClause = $"[{keyPropertyName}] = @p0";
                        var sql = QueryBuilder.BuildDeleteQuery(tableName, whereClause);
                        
                        await DatabaseHelper.ExecuteNonQueryAsync(connection, sql, keyValue);
                        changesCount++;
                    }
                }
            }
            else if (state == EntityState.Modified)
            {
                var propertyValues = ObjectMapper.GetPropertyValues(entity);
                var keyPropertyName = ObjectMapper.GetPrimaryKeyPropertyName(entityType);
                
                if (!string.IsNullOrEmpty(keyPropertyName) && propertyValues.ContainsKey(keyPropertyName))
                {
                    var keyValue = propertyValues[keyPropertyName];
                    propertyValues.Remove(keyPropertyName); // Remove key from update

                    var columnNames = propertyValues.Keys.ToList();
                    var values = propertyValues.Values.ToList();
                    values.Add(keyValue); // Add key value at the end for WHERE clause
                    
                    var whereClause = $"[{keyPropertyName}] = @p{values.Count - 1}";
                    var sql = QueryBuilder.BuildUpdateQuery(tableName, columnNames, whereClause);
                    
                    await DatabaseHelper.ExecuteNonQueryAsync(connection, sql, values.ToArray());
                    changesCount++;
                }
            }
        }

        _trackedEntities.Clear();
        return changesCount;
    }

    /// <summary>
    /// Ensures that the database for the context exists. If it exists, no action is taken. If it does not exist, the database and all its schema are created.
    /// </summary>
    public virtual bool EnsureCreated()
    {
        using var connection = DatabaseHelper.CreateConnection(ConnectionString);
        connection.Open();

        // Get all DbSet properties
        var dbSetProperties = GetType().GetProperties()
            .Where(p => p.PropertyType.IsGenericType && 
                       p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToList();

        bool created = false;
        foreach (var property in dbSetProperties)
        {
            var entityType = property.PropertyType.GetGenericArguments()[0];
            var tableName = GetTableName(entityType);
            
            // Check if table exists using parameterized query
            var tableExistsSql = "SELECT name FROM sqlite_master WHERE type='table' AND name=@p0";
            var exists = DatabaseHelper.ExecuteScalarAsync(connection, tableExistsSql, tableName)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            
            if (exists == null)
            {
                DatabaseHelper.CreateTableIfNotExistsAsync(connection, entityType, tableName)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
                created = true;
            }
        }

        return created;
    }

    /// <summary>
    /// Gets the table name for an entity type (uses pluralized entity name).
    /// </summary>
    protected virtual string GetTableName(Type entityType)
    {
        var name = entityType.Name;
        // Simple pluralization: add 's' if not already plural
        if (!name.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            name += "s";
        }
        return name;
    }

    /// <summary>
    /// Tracks an entity for addition.
    /// </summary>
    internal void AddEntity(object entity, string tableName)
    {
        _trackedEntities[entity] = EntityState.Added;
    }

    /// <summary>
    /// Tracks an entity for removal.
    /// </summary>
    internal void RemoveEntity(object entity, string tableName)
    {
        if (_trackedEntities.ContainsKey(entity))
        {
            _trackedEntities[entity] = EntityState.Deleted;
        }
        else
        {
            // Entity might already be in database, mark as deleted
            _trackedEntities[entity] = EntityState.Deleted;
        }
    }

    /// <summary>
    /// Creates a DbSet for the specified entity type.
    /// </summary>
    protected DbSet<TEntity> Set<TEntity>() where TEntity : class
    {
        var tableName = GetTableName(typeof(TEntity));
        return new DbSet<TEntity>(this, tableName);
    }

    /// <summary>
    /// Disposes the context.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _trackedEntities.Clear();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Represents the state of an entity.
/// </summary>
internal enum EntityState
{
    Added,
    Modified,
    Deleted,
    Unchanged
}

