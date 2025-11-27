namespace MiniCore.Framework.Logging;

/// <summary>
/// A generic logger that uses the type name as the category name.
/// </summary>
/// <typeparam name="TCategoryName">The type whose name is used as the logger category name.</typeparam>
internal class Logger<TCategoryName> : ILogger<TCategoryName>
{
    private readonly ILogger _logger;

    public Logger(ILoggerFactory factory)
    {
        _logger = factory.CreateLogger(typeof(TCategoryName).FullName ?? typeof(TCategoryName).Name);
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _logger.Log(logLevel, eventId, state, exception, formatter);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel);
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _logger.BeginScope(state);
    }
}

