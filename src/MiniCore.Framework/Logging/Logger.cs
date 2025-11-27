using System.Collections.Concurrent;

namespace MiniCore.Framework.Logging;

/// <summary>
/// A generic logger that aggregates loggers from multiple providers.
/// </summary>
internal class Logger : ILogger
{
    private readonly string _categoryName;
    private readonly LoggerFactory _factory;
    private readonly ConcurrentDictionary<Type, ILogger> _loggers = new();

    public Logger(string categoryName, LoggerFactory factory)
    {
        _categoryName = categoryName;
        _factory = factory;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        // Get all providers and log to each
        var providers = _factory.GetProviders();
        foreach (var provider in providers)
        {
            var logger = GetOrCreateLogger(provider);
            logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        // Check if any provider is enabled for this log level
        var providers = _factory.GetProviders();
        foreach (var provider in providers)
        {
            var logger = GetOrCreateLogger(provider);
            if (logger.IsEnabled(logLevel))
            {
                return true;
            }
        }
        return false;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        // For now, return a no-op scope. Scoped logging can be enhanced later.
        return new LoggerScope();
    }

    private ILogger GetOrCreateLogger(ILoggerProvider provider)
    {
        var providerType = provider.GetType();
        return _loggers.GetOrAdd(providerType, _ => provider.CreateLogger(_categoryName));
    }

    private class LoggerScope : IDisposable
    {
        public void Dispose() { }
    }
}

