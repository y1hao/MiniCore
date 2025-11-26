using System.Collections;
using MiniCore.Framework.Configuration.Abstractions;
using MiniCore.Framework.Configuration.Json;

namespace MiniCore.Framework.Configuration.EnvironmentVariables;

/// <summary>
/// An environment variable based <see cref="IConfigurationProvider"/>.
/// </summary>
public class EnvironmentVariablesConfigurationProvider : IConfigurationProvider
{
    private readonly EnvironmentVariablesConfigurationSource _source;
    private readonly Dictionary<string, string?> _data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    private readonly ConfigurationReloadToken _reloadToken = new();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="source">The source settings.</param>
    public EnvironmentVariablesConfigurationProvider(EnvironmentVariablesConfigurationSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary>
    /// Loads the environment variables.
    /// </summary>
    public void Load()
    {
        _data.Clear();

        var prefix = _source.Prefix ?? string.Empty;
        var prefixLength = prefix.Length;

        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key = entry.Key.ToString();
            if (key == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(prefix))
            {
                if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                key = key.Substring(prefixLength);
            }

            // Replace double underscores with colons (common pattern for nested config)
            key = key.Replace("__", ConfigurationPath.KeyDelimiter);
            
            _data[key] = entry.Value?.ToString();
        }
    }

    /// <summary>
    /// Returns a list of the child keys for a given parent path.
    /// </summary>
    /// <param name="earlierKeys">The child keys returned by the preceding providers for the same parent path.</param>
    /// <param name="parentPath">The parent path.</param>
    /// <returns>The child keys.</returns>
    public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
    {
        var prefix = parentPath == null ? string.Empty : parentPath + ConfigurationPath.KeyDelimiter;
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in _data.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var remainder = key.Substring(prefix.Length);
                var indexOfDelimiter = remainder.IndexOf(ConfigurationPath.KeyDelimiter, StringComparison.OrdinalIgnoreCase);
                var childKey = indexOfDelimiter < 0 ? remainder : remainder.Substring(0, indexOfDelimiter);
                if (!string.IsNullOrEmpty(childKey))
                {
                    keys.Add(childKey);
                }
            }
        }

        return keys.Concat(earlierKeys).OrderBy(k => k, ConfigurationKeyComparer.Instance);
    }

    /// <summary>
    /// Returns a <see cref="IChangeToken"/> that can be used to listen when this
    /// <see cref="IConfigurationProvider"/> reloads.
    /// </summary>
    /// <returns>The <see cref="IChangeToken"/>.</returns>
    public IChangeToken GetReloadToken()
    {
        return _reloadToken;
    }

    /// <summary>
    /// Sets a configuration value for the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void Set(string key, string? value)
    {
        _data[key] = value;
    }

    /// <summary>
    /// Attempts to find a value with the given key, returns true if one is found, false otherwise.
    /// </summary>
    /// <param name="key">The key to lookup.</param>
    /// <param name="value">The value found at key if one is found.</param>
    /// <returns>True if key has a value, false otherwise.</returns>
    public bool TryGet(string key, out string? value)
    {
        return _data.TryGetValue(key, out value);
    }
}

