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
    /// Configures the connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public DbContextOptionsBuilder UseSqlite(string connectionString)
    {
        _options.ConnectionString = connectionString;
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

