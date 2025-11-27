using MSLogging = Microsoft.Extensions.Logging;
using MiniLogging = MiniCore.Framework.Logging;

namespace MiniCore.Web;

/// <summary>
/// Adapter that bridges our custom logging framework with Microsoft's ILogger interface.
/// This allows ASP.NET Core components to use our custom logging implementation.
/// </summary>
/// <remarks>
/// TODO: REMOVE IN PHASE 4 (Host Abstraction)
/// In Phase 4, we'll replace WebApplication.CreateBuilder() with our own MiniHostBuilder
/// that uses our logging natively, eliminating the need for this adapter.
/// </remarks>
public class LoggingAdapter : MSLogging.ILogger
{
    private readonly MiniLogging.ILogger _logger;

    public LoggingAdapter(MiniLogging.ILogger logger)
    {
        _logger = logger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _logger.BeginScope(state);
    }

    public bool IsEnabled(MSLogging.LogLevel logLevel)
    {
        return _logger.IsEnabled(ConvertLogLevel(logLevel));
    }

    public void Log<TState>(
        MSLogging.LogLevel logLevel,
        MSLogging.EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var miniLogLevel = ConvertLogLevel(logLevel);
        var miniEventId = new MiniLogging.EventId(eventId.Id, eventId.Name);
        _logger.Log(
            miniLogLevel,
            miniEventId,
            state,
            exception,
            formatter);
    }

    private static MiniLogging.LogLevel ConvertLogLevel(MSLogging.LogLevel logLevel)
    {
        return logLevel switch
        {
            MSLogging.LogLevel.Trace => MiniLogging.LogLevel.Trace,
            MSLogging.LogLevel.Debug => MiniLogging.LogLevel.Debug,
            MSLogging.LogLevel.Information => MiniLogging.LogLevel.Information,
            MSLogging.LogLevel.Warning => MiniLogging.LogLevel.Warning,
            MSLogging.LogLevel.Error => MiniLogging.LogLevel.Error,
            MSLogging.LogLevel.Critical => MiniLogging.LogLevel.Critical,
            MSLogging.LogLevel.None => MiniLogging.LogLevel.None,
            _ => MiniLogging.LogLevel.Information
        };
    }
}

/// <summary>
/// Adapter for Microsoft's ILoggerFactory.
/// </summary>
public class LoggingFactoryAdapter : MSLogging.ILoggerFactory
{
    private readonly MiniLogging.ILoggerFactory _factory;

    public LoggingFactoryAdapter(MiniLogging.ILoggerFactory factory)
    {
        _factory = factory;
    }

    public void AddProvider(MSLogging.ILoggerProvider provider)
    {
        // Not supported - providers are added through our custom logging builder
    }

    public MSLogging.ILogger CreateLogger(string categoryName)
    {
        var logger = _factory.CreateLogger(categoryName);
        return new LoggingAdapter(logger);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}

/// <summary>
/// Adapter for Microsoft's ILogger&lt;T&gt;.
/// </summary>
public class LoggingAdapter<T> : MSLogging.ILogger<T>
{
    private readonly MiniLogging.ILogger<T> _logger;

    public LoggingAdapter(MiniLogging.ILogger<T> logger)
    {
        _logger = logger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _logger.BeginScope(state);
    }

    public bool IsEnabled(MSLogging.LogLevel logLevel)
    {
        return _logger.IsEnabled(ConvertLogLevel(logLevel));
    }

    public void Log<TState>(
        MSLogging.LogLevel logLevel,
        MSLogging.EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var miniLogLevel = ConvertLogLevel(logLevel);
        var miniEventId = new MiniLogging.EventId(eventId.Id, eventId.Name);
        _logger.Log(
            miniLogLevel,
            miniEventId,
            state,
            exception,
            formatter);
    }

    private static MiniLogging.LogLevel ConvertLogLevel(MSLogging.LogLevel logLevel)
    {
        return logLevel switch
        {
            MSLogging.LogLevel.Trace => MiniLogging.LogLevel.Trace,
            MSLogging.LogLevel.Debug => MiniLogging.LogLevel.Debug,
            MSLogging.LogLevel.Information => MiniLogging.LogLevel.Information,
            MSLogging.LogLevel.Warning => MiniLogging.LogLevel.Warning,
            MSLogging.LogLevel.Error => MiniLogging.LogLevel.Error,
            MSLogging.LogLevel.Critical => MiniLogging.LogLevel.Critical,
            MSLogging.LogLevel.None => MiniLogging.LogLevel.None,
            _ => MiniLogging.LogLevel.Information
        };
    }
}

