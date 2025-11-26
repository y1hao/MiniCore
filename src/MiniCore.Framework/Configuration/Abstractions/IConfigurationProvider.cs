namespace MiniCore.Framework.Configuration.Abstractions;

/// <summary>
/// Provides configuration key/value pairs for an application.
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Returns a list of the child keys for a given parent path based on this
    /// <see cref="IConfigurationProvider"/>'s data and the set of keys returned by all the
    /// preceding <see cref="IConfigurationProvider"/>s.
    /// </summary>
    /// <param name="earlierKeys">The child keys returned by the preceding providers for the same parent path.</param>
    /// <param name="parentPath">The parent path.</param>
    /// <returns>The child keys.</returns>
    IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath);

    /// <summary>
    /// Returns a <see cref="IChangeToken"/> that can be used to listen when this
    /// <see cref="IConfigurationProvider"/> reloads.
    /// </summary>
    /// <returns>The <see cref="IChangeToken"/>.</returns>
    IChangeToken GetReloadToken();

    /// <summary>
    /// Loads configuration values from the source represented by this <see cref="IConfigurationProvider"/>.
    /// </summary>
    void Load();

    /// <summary>
    /// Sets a configuration value for the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    void Set(string key, string? value);

    /// <summary>
    /// Attempts to find a value with the given key, returns true if one is found, false otherwise.
    /// </summary>
    /// <param name="key">The key to lookup.</param>
    /// <param name="value">The value found at key if one is found.</param>
    /// <returns>True if key has a value, false otherwise.</returns>
    bool TryGet(string key, out string? value);
}

