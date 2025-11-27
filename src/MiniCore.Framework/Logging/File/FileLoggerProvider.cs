namespace MiniCore.Framework.Logging.File;

/// <summary>
/// Provider for creating <see cref="FileLogger"/> instances.
/// </summary>
public class FileLoggerProvider : ILoggerProvider
{
    private readonly LogLevel _minLevel;
    private readonly string _logFilePath;

    /// <summary>
    /// Initializes a new instance of <see cref="FileLoggerProvider"/>.
    /// </summary>
    /// <param name="logFilePath">The path to the log file.</param>
    /// <param name="minLevel">The minimum log level to log.</param>
    public FileLoggerProvider(string logFilePath, LogLevel minLevel = LogLevel.Information)
    {
        if (string.IsNullOrWhiteSpace(logFilePath))
        {
            throw new ArgumentException("Log file path cannot be null or empty.", nameof(logFilePath));
        }

        _logFilePath = logFilePath;
        _minLevel = minLevel;
    }

    /// <summary>
    /// Creates a new <see cref="ILogger"/> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The instance of <see cref="ILogger"/> that was created.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _minLevel, _logFilePath);
    }

    /// <summary>
    /// Disposes the provider.
    /// </summary>
    public void Dispose()
    {
        // Nothing to dispose for file logger
    }
}

