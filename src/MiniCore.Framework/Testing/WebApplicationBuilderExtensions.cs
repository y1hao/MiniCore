using MiniCore.Framework.Configuration.Abstractions;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Hosting;

namespace MiniCore.Framework.Testing;

/// <summary>
/// Extension methods for <see cref="WebApplicationBuilder"/> used in testing.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Sets the environment name for the application.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="environmentName">The environment name.</param>
    /// <returns>The <see cref="WebApplicationBuilder"/> for chaining.</returns>
    public static WebApplicationBuilder UseEnvironment(this WebApplicationBuilder builder, string environmentName)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrEmpty(environmentName))
        {
            throw new ArgumentException("Environment name cannot be null or empty.", nameof(environmentName));
        }

        builder.Environment.EnvironmentName = environmentName;
        return builder;
    }

    /// <summary>
    /// Configures services for the application.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="configureServices">A callback to configure services.</param>
    /// <returns>The <see cref="WebApplicationBuilder"/> for chaining.</returns>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder, Action<IServiceCollection> configureServices)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configureServices == null)
        {
            throw new ArgumentNullException(nameof(configureServices));
        }

        configureServices(builder.Services);
        return builder;
    }

    /// <summary>
    /// Configures the application configuration.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="configureAppConfiguration">A callback to configure the configuration builder.</param>
    /// <returns>The <see cref="WebApplicationBuilder"/> for chaining.</returns>
    public static WebApplicationBuilder ConfigureAppConfiguration(this WebApplicationBuilder builder, Action<IConfigurationBuilder> configureAppConfiguration)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configureAppConfiguration == null)
        {
            throw new ArgumentNullException(nameof(configureAppConfiguration));
        }

        // We need to rebuild configuration after this, but for now we'll just configure the host builder
        builder.Host.ConfigureAppConfiguration(configureAppConfiguration);
        return builder;
    }
}


