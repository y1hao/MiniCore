using System.Collections.Concurrent;
using System.Text;

namespace MiniCore.Framework.Logging.File;

/// <summary>
/// A logger that writes to a file.
/// </summary>
internal class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly LogLevel _minLevel;
    private readonly string _logFilePath;
    private readonly object _lock = new();

    public FileLogger(string categoryName, LogLevel minLevel, string logFilePath)
    {
        _categoryName = categoryName;
        _minLevel = minLevel;
        _logFilePath = logFilePath;

        // Ensure directory exists
        var directory = System.IO.Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
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

        lock (_lock)
        {
            try
            {
                System.IO.File.AppendAllText(_logFilePath, formattedMessage + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Silently fail if we can't write to the log file
                // In production, you might want to fall back to console or handle this differently
            }
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _minLevel && logLevel != LogLevel.None;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        // File logger doesn't support scopes for now
        return null;
    }

    private string FormatMessage(LogLevel logLevel, string message, Exception? exception)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelString = logLevel.ToString().ToUpperInvariant().PadRight(11);
        var category = _categoryName;

        var builder = new StringBuilder();
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

            // Include inner exception if present
            var innerException = exception.InnerException;
            var depth = 1;
            while (innerException != null && depth < 5) // Limit depth to avoid infinite loops
            {
                builder.AppendLine($"InnerException[{depth}]: {innerException.GetType().Name}");
                builder.AppendLine($"InnerException[{depth}] Message: {innerException.Message}");
                innerException = innerException.InnerException;
                depth++;
            }
        }

        return builder.ToString();
    }
}

