using MiniCore.Framework.Configuration.Abstractions;
using MiniCore.Framework.Configuration.EnvironmentVariables;
using MiniCore.Framework.Configuration.Json;

namespace MiniCore.Framework.Configuration;

/// <summary>
/// Extension methods for adding configuration sources to an <see cref="IConfigurationBuilder"/>.
/// </summary>
public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds the JSON configuration provider at <paramref name="path"/> to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="path">Path relative to the base path stored in
    /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddJsonFile(
        this IConfigurationBuilder builder,
        string path,
        bool optional = false,
        bool reloadOnChange = false)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        return builder.Add(new JsonConfigurationSource
        {
            Path = path,
            Optional = optional,
            ReloadOnChange = reloadOnChange
        });
    }

    /// <summary>
    /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from environment variables.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="prefix">A prefix used to filter environment variables.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddEnvironmentVariables(
        this IConfigurationBuilder builder,
        string? prefix = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.Add(new EnvironmentVariablesConfigurationSource
        {
            Prefix = prefix
        });
    }
}

