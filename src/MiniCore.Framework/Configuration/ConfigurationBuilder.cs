using MiniCore.Framework.Configuration.Abstractions;

namespace MiniCore.Framework.Configuration;

/// <summary>
/// Used to build key/value based configuration settings for use in an application.
/// </summary>
public class ConfigurationBuilder : IConfigurationBuilder
{
    /// <summary>
    /// Gets a key/value collection that can be used to share data between the <see cref="IConfigurationSource"/>
    /// instances and the <see cref="IConfigurationBuilder"/>.
    /// </summary>
    public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the sources used to obtain configuration values.
    /// </summary>
    public IList<IConfigurationSource> Sources { get; } = new List<IConfigurationSource>();

    /// <summary>
    /// Adds a new configuration source.
    /// </summary>
    /// <param name="source">The configuration source to add.</param>
    /// <returns>The same <see cref="IConfigurationBuilder"/>.</returns>
    public IConfigurationBuilder Add(IConfigurationSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        Sources.Add(source);
        return this;
    }

    /// <summary>
    /// Builds an <see cref="IConfiguration"/> with keys and values from the set of sources registered in
    /// <see cref="Sources"/>.
    /// </summary>
    /// <returns>An <see cref="IConfigurationRoot"/> with keys and values from the registered sources.</returns>
    public IConfigurationRoot Build()
    {
        var providers = new List<IConfigurationProvider>();
        foreach (var source in Sources)
        {
            var provider = source.Build(this);
            provider.Load();
            providers.Add(provider);
        }

        return new ConfigurationRoot(providers);
    }
}

