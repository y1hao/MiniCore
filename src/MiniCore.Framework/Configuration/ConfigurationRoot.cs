using MiniCore.Framework.Configuration.Abstractions;

namespace MiniCore.Framework.Configuration;

/// <summary>
/// The root node for a configuration hierarchy.
/// </summary>
public class ConfigurationRoot : IConfigurationRoot
{
    private readonly IList<IConfigurationProvider> _providers;
    private readonly ConfigurationReloadToken _reloadToken = new();

    /// <summary>
    /// Initializes a Configuration root with a list of providers.
    /// </summary>
    /// <param name="providers">The <see cref="IConfigurationProvider"/>s for this configuration.</param>
    public ConfigurationRoot(IList<IConfigurationProvider> providers)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
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

            // Try each provider in reverse order (last added first) to match Microsoft's behavior
            // where later sources override earlier ones
            for (int i = _providers.Count - 1; i >= 0; i--)
            {
                if (_providers[i].TryGet(key, out var value))
                {
                    return value;
                }
            }

            return null;
        }
        set
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            // Set the value in the first provider that supports it
            if (_providers.Count > 0)
            {
                _providers[0].Set(key, value);
            }
        }
    }

    /// <summary>
    /// Gets the immediate descendant configuration sub-sections.
    /// </summary>
    /// <returns>The configuration sub-sections.</returns>
    public IEnumerable<IConfigurationSection> GetChildren()
    {
        return GetChildrenImplementation(null);
    }

    /// <summary>
    /// Gets a configuration sub-section with the specified key.
    /// </summary>
    /// <param name="key">The key of the configuration section.</param>
    /// <returns>The <see cref="IConfigurationSection"/>.</returns>
    public IConfigurationSection GetSection(string key)
    {
        return new ConfigurationSection(this, key);
    }

    /// <summary>
    /// Gets a reload token that can be used to observe when this configuration is reloaded.
    /// </summary>
    /// <returns>The <see cref="IChangeToken"/>.</returns>
    public IChangeToken GetReloadToken()
    {
        return _reloadToken;
    }

    /// <summary>
    /// Force the configuration values to be reloaded from the underlying <see cref="IConfigurationProvider"/>s.
    /// </summary>
    public void Reload()
    {
        foreach (var provider in _providers)
        {
            provider.Load();
        }

        _reloadToken.OnReload();
    }

    internal IEnumerable<IConfigurationSection> GetChildrenImplementation(string? path)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in _providers)
        {
            foreach (var key in provider.GetChildKeys(Enumerable.Empty<string>(), path))
            {
                keys.Add(key);
            }
        }

        return keys.Select(key => new ConfigurationSection(this, path == null ? key : ConfigurationPath.Combine(path, key)));
    }

    internal IConfigurationProvider? GetProvider(string key)
    {
        foreach (var provider in _providers)
        {
            if (provider.TryGet(key, out _))
            {
                return provider;
            }
        }

        return null;
    }
}

