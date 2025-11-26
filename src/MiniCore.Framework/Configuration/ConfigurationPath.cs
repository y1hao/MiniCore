namespace MiniCore.Framework.Configuration;

/// <summary>
/// Utility methods and constants for manipulating configuration paths.
/// </summary>
public static class ConfigurationPath
{
    /// <summary>
    /// The delimiter ":" used to separate individual keys in a path.
    /// </summary>
    public static readonly string KeyDelimiter = ":";

    /// <summary>
    /// Combines path segments into one path.
    /// </summary>
    /// <param name="path1">The path to combine.</param>
    /// <param name="path2">The path to combine.</param>
    /// <returns>The combined path.</returns>
    public static string Combine(string path1, string path2)
    {
        if (string.IsNullOrEmpty(path1))
        {
            return path2 ?? string.Empty;
        }

        if (string.IsNullOrEmpty(path2))
        {
            return path1;
        }

        return path1 + KeyDelimiter + path2;
    }

    /// <summary>
    /// Combines path segments into one path.
    /// </summary>
    /// <param name="paths">The paths to combine.</param>
    /// <returns>The combined path.</returns>
    public static string Combine(params string[] paths)
    {
        if (paths == null || paths.Length == 0)
        {
            return string.Empty;
        }

        return string.Join(KeyDelimiter, paths.Where(p => !string.IsNullOrEmpty(p)));
    }

    /// <summary>
    /// Extracts the last path segment from the path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The last path segment of the path.</returns>
    public static string GetSectionKey(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        var lastDelimiterIndex = path.LastIndexOf(KeyDelimiter, StringComparison.OrdinalIgnoreCase);
        return lastDelimiterIndex < 0 ? path : path.Substring(lastDelimiterIndex + 1);
    }

    /// <summary>
    /// Extracts the path corresponding to the parent node for a given path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The original path minus the last individual segment found in it. Null or empty string is returned if the given path has no parent node.</returns>
    public static string? GetParentPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var lastDelimiterIndex = path.LastIndexOf(KeyDelimiter, StringComparison.OrdinalIgnoreCase);
        return lastDelimiterIndex < 0 ? null : path.Substring(0, lastDelimiterIndex);
    }
}

