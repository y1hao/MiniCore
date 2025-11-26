namespace MiniCore.Framework.Configuration.Abstractions;

/// <summary>
/// Represents a set of key/value application configuration properties.
/// </summary>
public interface IConfiguration
{
    /// <summary>
    /// Gets or sets a configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The configuration value.</returns>
    string? this[string key] { get; set; }

    /// <summary>
    /// Gets a configuration sub-section with the specified key.
    /// </summary>
    /// <param name="key">The key of the configuration section.</param>
    /// <returns>The <see cref="IConfigurationSection"/>.</returns>
    IConfigurationSection GetSection(string key);

    /// <summary>
    /// Gets the immediate descendant configuration sub-sections.
    /// </summary>
    /// <returns>The configuration sub-sections.</returns>
    IEnumerable<IConfigurationSection> GetChildren();

    /// <summary>
    /// Gets a reload token that can be used to observe when this configuration is reloaded.
    /// </summary>
    /// <returns>The <see cref="IChangeToken"/>.</returns>
    IChangeToken GetReloadToken();
}

