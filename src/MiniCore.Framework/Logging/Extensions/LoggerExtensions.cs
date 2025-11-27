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
            // No placeholders, format with string.Format if args are provided
            if (args.Length > 0)
            {
                try
                {
                    return new { Message = string.Format(message, args) };
                }
                catch
                {
                    // If formatting fails, return as-is
                    return new { Message = message };
                }
            }
            return new { Message = message };
        }

        // Create a dictionary with placeholder names and values
        var stateDict = new Dictionary<string, object?>();
        for (int i = 0; i < Math.Min(placeholders.Count, args.Length); i++)
        {
            stateDict[placeholders[i]] = args[i];
        }

        // Also include the original message template
        stateDict["Message"] = message;

        return stateDict;
    }

    private static string FormatMessage(object? state, Exception? exception)
    {
        if (state == null)
        {
            return exception?.ToString() ?? string.Empty;
        }

        // Handle dictionary state (from structured logging)
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

        // Handle anonymous types - try to extract Message property first
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

