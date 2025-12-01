namespace MiniCore.Framework.Logging;

/// <summary>
/// Extension methods for <see cref="ILogger"/>.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void LogTrace(this ILogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Trace, 0, CreateState(message, args), null, FormatMessage);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void LogDebug(this ILogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Debug, 0, CreateState(message, args), null, FormatMessage);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void LogInformation(this ILogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Information, 0, CreateState(message, args), null, FormatMessage);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void LogWarning(this ILogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Warning, 0, CreateState(message, args), null, FormatMessage);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void LogError(this ILogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Error, 0, CreateState(message, args), null, FormatMessage);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void LogError(this ILogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Error, 0, CreateState(message, args), exception, FormatMessage);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void LogCritical(this ILogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Critical, 0, CreateState(message, args), null, FormatMessage);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void LogCritical(this ILogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Critical, 0, CreateState(message, args), exception, FormatMessage);
    }

    /// <summary>
    /// Represents structured log state that supports both structured formatting
    /// and meaningful <see cref="object.ToString"/> output for testing.
    /// </summary>
    private sealed class StructuredLogState : List<KeyValuePair<string, object?>>, IReadOnlyList<KeyValuePair<string, object?>>
    {
        /// <summary>
        /// Gets the original message template.
        /// </summary>
        public string Message { get; }

        public StructuredLogState(string message, IReadOnlyList<string> placeholders, object?[] args)
        {
            Message = message;

            for (var i = 0; i < Math.Min(placeholders.Count, args.Length); i++)
            {
                Add(new KeyValuePair<string, object?>(placeholders[i], args[i]));
            }
        }

        public override string ToString()
        {
            // Ensure that ToString contains the rendered message so tests that
            // inspect the state object (v.ToString()) see the expected text.
            return MessageFormatter.Format(Message, this);
        }
    }

    private static object CreateState(string? message, object?[] args)
    {
        if (message == null)
        {
            return new { Message = string.Empty };
        }

        // If message contains placeholders like {ShortCode}, we need to extract them
        // and create a state object with those properties
        var placeholders = System.Text.RegularExpressions.Regex.Matches(message, @"\{([^}]+)\}")
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(m => m.Groups[1].Value)
            .ToList();

        if (placeholders.Count == 0)
        {
            // No placeholders, keep behavior similar to before and just store the message,
            // optionally attempting simple formatting when args are provided.
            if (args.Length > 0)
            {
                try
                {
                    var formatted = string.Format(message, args);
                    return new { Message = formatted };
                }
                catch
                {
                    // If formatting fails, fall back to the original message
                }
            }

            return new { Message = message };
        }

        // Use a structured state that:
        // - Provides values for placeholders
        // - Produces a meaningful ToString() for tests that inspect the state object
        return new StructuredLogState(message, placeholders, args);
    }

    private static string FormatMessage(object? state, Exception? exception)
    {
        if (state == null)
        {
            return exception?.ToString() ?? string.Empty;
        }

        // Handle dictionary state from any older callers, if present
        if (state is Dictionary<string, object?> dict)
        {
            var message = dict.TryGetValue("Message", out var msg) ? msg?.ToString() ?? string.Empty : string.Empty;
            var formatted = MessageFormatter.Format(message, dict);

            if (exception != null)
            {
                return formatted + Environment.NewLine + exception;
            }
            return formatted;
        }

        // Handle anonymous types, POCOs, and StructuredLogState
        var stateType = state.GetType();
        var messageProperty = stateType.GetProperty("Message");
        string messageStr;
        if (messageProperty != null)
        {
            messageStr = messageProperty.GetValue(state)?.ToString() ?? string.Empty;
        }
        else
        {
            messageStr = state.ToString() ?? string.Empty;
        }

        // If message has no placeholders and we have the message string, return it directly
        if (!string.IsNullOrEmpty(messageStr) && !messageStr.Contains('{'))
        {
            if (exception != null)
            {
                return messageStr + Environment.NewLine + exception;
            }
            return messageStr;
        }

        var result = MessageFormatter.Format(messageStr, state);

        if (exception != null)
        {
            return result + Environment.NewLine + exception;
        }
        return result;
    }
}

