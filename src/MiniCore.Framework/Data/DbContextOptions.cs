using MiniCore.Framework.Logging;

namespace MiniCore.Framework.Data;

/// <summary>
/// The options to be used by a DbContext.
/// </summary>
public class DbContextOptions
{
    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the logger factory for creating loggers.
    /// </summary>
    public ILoggerFactory? LoggerFactory { get; set; }
}

/// <summary>
/// The options to be used by a DbContext. You normally override DbContextOptions&lt;TContext&gt; instead of this class.
/// </summary>
/// <typeparam name="TContext">The type of the context these options apply to.</typeparam>
public class DbContextOptions<TContext> : DbContextOptions where TContext : DbContext
{
}

