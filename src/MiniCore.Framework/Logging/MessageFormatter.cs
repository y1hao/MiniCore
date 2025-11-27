using System.Text;
using System.Text.RegularExpressions;

namespace MiniCore.Framework.Logging;

/// <summary>
/// Utility class for formatting log messages with placeholders.
/// </summary>
internal static class MessageFormatter
{
    private static readonly Regex PlaceholderRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);

    /// <summary>
    /// Formats a message template with values from the state object.
    /// </summary>
    /// <param name="template">The message template (e.g., "Created {ShortCode} -> {OriginalUrl}").</param>
    /// <param name="state">The state object containing values to substitute.</param>
    /// <returns>The formatted message.</returns>
    public static string Format(string template, object? state)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        if (state == null)
        {
            return template;
        }

        // Handle IReadOnlyList<KeyValuePair<string, object?>> (structured logging)
        if (state is IReadOnlyList<KeyValuePair<string, object?>> structuredState)
        {
            return FormatStructured(template, structuredState);
        }

        // Handle anonymous types and objects with properties
        return FormatObject(template, state);
    }

    private static string FormatStructured(string template, IReadOnlyList<KeyValuePair<string, object?>> state)
    {
        var result = template;
        var stateDict = state.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

        result = PlaceholderRegex.Replace(result, match =>
        {
            var placeholderName = match.Groups[1].Value;
            if (stateDict.TryGetValue(placeholderName, out var value))
            {
                return FormatValue(value);
            }
            return match.Value; // Keep original placeholder if not found
        });

        return result;
    }

    private static string FormatObject(string template, object state)
    {
        var result = template;

        // Handle Dictionary<string, object?> specially
        if (state is Dictionary<string, object?> dict)
        {
            result = PlaceholderRegex.Replace(result, match =>
            {
                var placeholderName = match.Groups[1].Value;
                if (dict.TryGetValue(placeholderName, out var value))
                {
                    return FormatValue(value);
                }
                return match.Value; // Keep original placeholder if not found
            });
            return result;
        }

        // Handle other objects with properties
        var properties = state.GetType().GetProperties();

        result = PlaceholderRegex.Replace(result, match =>
        {
            var placeholderName = match.Groups[1].Value;
            var property = properties.FirstOrDefault(p =>
                string.Equals(p.Name, placeholderName, StringComparison.OrdinalIgnoreCase));

            if (property != null)
            {
                var value = property.GetValue(state);
                return FormatValue(value);
            }
            return match.Value; // Keep original placeholder if not found
        });

        return result;
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
        {
            return "(null)";
        }

        if (value is string str)
        {
            return str;
        }

        if (value is DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        if (value is TimeSpan ts)
        {
            return ts.ToString(@"hh\:mm\:ss");
        }

        if (value is IEnumerable<object> enumerable && !(value is string))
        {
            return string.Join(", ", enumerable.Select(FormatValue));
        }

        return value.ToString() ?? "(null)";
    }
}

