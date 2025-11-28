using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Http.Middleware;
using MiniCore.Framework.Logging;
using Xunit;

namespace MiniCore.Framework.Tests.Http.Middleware;

public class RequestLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_LogsRequestAndResponse()
    {
        // Arrange
        var loggerFactory = new LoggerFactory();
        var testLogger = new TestLogger<RequestLoggingMiddleware>();
        loggerFactory.AddProvider(new TestLoggerProvider<RequestLoggingMiddleware>(testLogger));

        RequestDelegate next = context =>
        {
            context.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var middleware = new RequestLoggingMiddleware(next, loggerFactory.CreateLogger<RequestLoggingMiddleware>());
        var context = new HttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/test";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(testLogger.LogEntries.Count >= 2);
        Assert.Contains(testLogger.LogEntries, e => e.Message.Contains("started"));
        Assert.Contains(testLogger.LogEntries, e => e.Message.Contains("responded"));
    }

    private class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> LogEntries { get; } = new();

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogEntries.Add(new LogEntry
            {
                LogLevel = logLevel,
                Message = formatter(state, exception),
                Exception = exception
            });
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }

    private class TestLoggerProvider<T> : ILoggerProvider
    {
        private readonly ILogger<T> _logger;

        public TestLoggerProvider(ILogger<T> logger)
        {
            _logger = logger;
        }

        public ILogger CreateLogger(string categoryName) => _logger;

        public void Dispose() { }
    }

    private class LogEntry
    {
        public LogLevel LogLevel { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }
}

