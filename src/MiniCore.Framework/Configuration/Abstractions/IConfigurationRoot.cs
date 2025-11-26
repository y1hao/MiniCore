namespace MiniCore.Framework.Configuration.Abstractions;

/// <summary>
/// Represents the root of an <see cref="IConfiguration"/> hierarchy.
/// </summary>
public interface IConfigurationRoot : IConfiguration
{
    /// <summary>
    /// Force the configuration values to be reloaded from the underlying <see cref="IConfigurationProvider"/>s.
    /// </summary>
    void Reload();
}

