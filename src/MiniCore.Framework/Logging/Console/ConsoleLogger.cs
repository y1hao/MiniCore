using System.Collections.Concurrent;

namespace MiniCore.Framework.Logging.Console;

/// <summary>
/// A logger that writes to the console.
/// </summary>
internal class ConsoleLogger : ILogger
{
    private readonly string _categoryName;
    private readonly LogLevel _minLevel;
    private static readonly ConcurrentDictionary<LogLevel, ConsoleColor> LevelColors = new()
    {
        [LogLevel.Trace] = ConsoleColor.Gray,
        [LogLevel.Debug] = ConsoleColor.Gray,
        [LogLevel.Information] = ConsoleColor.White,
        [LogLevel.Warning] = ConsoleColor.Yellow,
        [LogLevel.Error] = ConsoleColor.Red,
        [LogLevel.Critical] = ConsoleColor.Red
    };

    public ConsoleLogger(string categoryName, LogLevel minLevel)
    {
        _categoryName = categoryName;
        _minLevel = minLevel;
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

        var message = formatter(state, exception);
        var formattedMessage = FormatMessage(logLevel, message, exception);

        var originalColor = System.Console.ForegroundColor;
        try
        {
            if (LevelColors.TryGetValue(logLevel, out var color))
            {
                System.Console.ForegroundColor = color;
            }

            System.Console.WriteLine(formattedMessage);
        }
        finally
        {
            System.Console.ForegroundColor = originalColor;
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _minLevel && logLevel != LogLevel.None;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        // Console logger doesn't support scopes for now
        return null;
    }

    private string FormatMessage(LogLevel logLevel, string message, Exception? exception)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var levelString = logLevel.ToString().ToUpperInvariant().PadRight(11);
        var category = _categoryName;

        var builder = new System.Text.StringBuilder();
        builder.Append($"[{timestamp}] [{levelString}] [{category}] {message}");

        if (exception != null)
        {
            builder.AppendLine();
            builder.AppendLine($"Exception: {exception.GetType().Name}");
            builder.AppendLine($"Message: {exception.Message}");
            if (exception.StackTrace != null)
            {
                builder.AppendLine($"StackTrace: {exception.StackTrace}");
            }
        }

        return builder.ToString();
    }
}

