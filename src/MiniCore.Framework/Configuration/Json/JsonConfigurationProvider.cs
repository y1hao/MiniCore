using System.Text.Json;
using MiniCore.Framework.Configuration.Abstractions;

namespace MiniCore.Framework.Configuration.Json;

/// <summary>
/// A JSON file based <see cref="IConfigurationProvider"/>.
/// </summary>
public class JsonConfigurationProvider : IConfigurationProvider
{
    private readonly JsonConfigurationSource _source;
    private readonly Dictionary<string, string?> _data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    private readonly ConfigurationReloadToken _reloadToken = new();

    /// <summary>
    /// Initializes a new instance with the specified source.
    /// </summary>
    /// <param name="source">The source settings.</param>
    public JsonConfigurationProvider(JsonConfigurationSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary>
    /// Loads the JSON data from a stream.
    /// </summary>
    public void Load()
    {
        _data.Clear();

        if (string.IsNullOrEmpty(_source.Path))
        {
            return;
        }

        if (!File.Exists(_source.Path))
        {
            if (!_source.Optional)
            {
                throw new FileNotFoundException($"The configuration file '{_source.Path}' was not found and is not optional.");
            }
            return;
        }

        try
        {
            var json = File.ReadAllText(_source.Path);
            var jsonDocument = JsonDocument.Parse(json);
            LoadFromJsonElement(jsonDocument.RootElement, string.Empty);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Error parsing JSON file '{_source.Path}'.", ex);
        }
    }

    private void LoadFromJsonElement(JsonElement element, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : ConfigurationPath.Combine(prefix, property.Name);
                    LoadFromJsonElement(property.Value, key);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = ConfigurationPath.Combine(prefix, index.ToString());
                    LoadFromJsonElement(item, key);
                    index++;
                }
                break;

            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                _data[prefix] = element.GetRawText().Trim('"');
                break;
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

/// <summary>
/// IComparer implementation used to order configuration keys.
/// </summary>
internal class ConfigurationKeyComparer : IComparer<string>
{
    public static ConfigurationKeyComparer Instance { get; } = new ConfigurationKeyComparer();

    public int Compare(string? x, string? y)
    {
        if (x == null && y == null)
        {
            return 0;
        }

        if (x == null)
        {
            return -1;
        }

        if (y == null)
        {
            return 1;
        }

        return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
    }
}

