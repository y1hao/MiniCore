using MiniCore.Framework.Logging;

namespace MiniCore.Framework.Data;

/// <summary>
/// Provides a simple API surface for configuring DbContextOptions.
/// </summary>
public class DbContextOptionsBuilder
{
    private readonly DbContextOptions _options = new();

    /// <summary>
    /// Gets the options being configured.
    /// </summary>
    public DbContextOptions Options => _options;

    /// <summary>
    /// Sets the logger factory for the DbContext.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public DbContextOptionsBuilder UseLoggerFactory(ILoggerFactory? loggerFactory)
    {
        _options.LoggerFactory = loggerFactory;
        return this;
    }

    /// <summary>
    /// Configures the connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public DbContextOptionsBuilder UseSqlite(string connectionString)
    {
        // Handle :memory: shorthand for in-memory database used in tests.
        // SQLite's pure in-memory databases are scoped to a single connection,
        // but our ORM opens new connections per operation. To ensure schema/data
        // persist across connections in tests, map this to a unique temporary
        // file-based database per test instance to avoid file locking issues.
        if (connectionString == ":memory:")
        {
            var tempFileName = $"MiniCoreTests_{Guid.NewGuid():N}.db";
            var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
            _options.ConnectionString = $"Data Source={tempPath}";
        }
        else
        {
            _options.ConnectionString = connectionString;
        }
        return this;
    }
}

/// <summary>
/// Provides a simple API surface for configuring DbContextOptions.
/// </summary>
/// <typeparam name="TContext">The type of the context.</typeparam>
public class DbContextOptionsBuilder<TContext> : DbContextOptionsBuilder where TContext : DbContext
{
    /// <summary>
    /// Gets the options being configured.
    /// </summary>
    public new DbContextOptions<TContext> Options => new() { ConnectionString = base.Options.ConnectionString };
}

