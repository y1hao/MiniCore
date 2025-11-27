namespace MiniCore.Framework.Logging.Console;

/// <summary>
/// Provider for creating <see cref="ConsoleLogger"/> instances.
/// </summary>
public class ConsoleLoggerProvider : ILoggerProvider
{
    private readonly LogLevel _minLevel;

    /// <summary>
    /// Initializes a new instance of <see cref="ConsoleLoggerProvider"/>.
    /// </summary>
    /// <param name="minLevel">The minimum log level to log.</param>
    public ConsoleLoggerProvider(LogLevel minLevel = LogLevel.Information)
    {
        _minLevel = minLevel;
    }

    /// <summary>
    /// Creates a new <see cref="ILogger"/> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The instance of <see cref="ILogger"/> that was created.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        return new ConsoleLogger(categoryName, _minLevel);
    }

    /// <summary>
    /// Disposes the provider.
    /// </summary>
    public void Dispose()
    {
        // Nothing to dispose for console logger
    }
}

