using MiniCore.Framework.Configuration.Abstractions;

namespace MiniCore.Framework.Configuration.Json;

/// <summary>
/// Represents a JSON file as an <see cref="IConfigurationSource"/>.
/// </summary>
public class JsonConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Gets or sets the path to the JSON file.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets whether the file is optional.
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    /// Gets or sets whether the source will be loaded if the underlying file changes.
    /// </summary>
    public bool ReloadOnChange { get; set; }

    /// <summary>
    /// Builds the <see cref="JsonConfigurationProvider"/> for this source.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
    /// <returns>A <see cref="JsonConfigurationProvider"/>.</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new JsonConfigurationProvider(this);
    }
}

