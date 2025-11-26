using MiniCore.Framework.Configuration.Abstractions;

namespace MiniCore.Framework.Configuration;

/// <summary>
/// Represents a section of application configuration values.
/// </summary>
public class ConfigurationSection : IConfigurationSection
{
    private readonly ConfigurationRoot _root;
    private readonly string _path;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="root">The configuration root.</param>
    /// <param name="path">The path to this section.</param>
    public ConfigurationSection(ConfigurationRoot root, string path)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _path = path ?? throw new ArgumentNullException(nameof(path));
    }

    /// <summary>
    /// Gets the key this section occupies in its parent.
    /// </summary>
    public string Key
    {
        get
        {
            var lastDelimiterIndex = _path.LastIndexOf(ConfigurationPath.KeyDelimiter, StringComparison.OrdinalIgnoreCase);
            return lastDelimiterIndex < 0 ? _path : _path.Substring(lastDelimiterIndex + 1);
        }
    }

    /// <summary>
    /// Gets the full path to this section within the <see cref="IConfiguration"/>.
    /// </summary>
    public string Path => _path;

    /// <summary>
    /// Gets or sets the section value.
    /// </summary>
    public string? Value
    {
        get => _root[_path];
        set => _root[_path] = value;
    }

    /// <summary>
    /// Gets or sets the value corresponding to a configuration key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The configuration value.</returns>
    public string? this[string key]
    {
        get
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _root[ConfigurationPath.Combine(_path, key)];
        }
        set
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _root[ConfigurationPath.Combine(_path, key)] = value;
        }
    }

    /// <summary>
    /// Gets a configuration sub-section with the specified key.
    /// </summary>
    /// <param name="key">The key of the configuration section.</param>
    /// <returns>The <see cref="IConfigurationSection"/>.</returns>
    public IConfigurationSection GetSection(string key)
    {
        return _root.GetSection(ConfigurationPath.Combine(_path, key));
    }

    /// <summary>
    /// Gets the immediate descendant configuration sub-sections.
    /// </summary>
    /// <returns>The configuration sub-sections.</returns>
    public IEnumerable<IConfigurationSection> GetChildren()
    {
        return _root.GetChildrenImplementation(_path);
    }

    /// <summary>
    /// Gets a reload token that can be used to observe when this configuration is reloaded.
    /// </summary>
    /// <returns>The <see cref="IChangeToken"/>.</returns>
    public IChangeToken GetReloadToken()
    {
        return _root.GetReloadToken();
    }
}

