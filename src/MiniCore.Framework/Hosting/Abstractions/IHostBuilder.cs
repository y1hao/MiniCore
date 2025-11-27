using MiniCore.Framework.Configuration.Abstractions;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Logging;

namespace MiniCore.Framework.Hosting;

/// <summary>
/// A program initialization abstraction.
/// </summary>
public interface IHostBuilder
{
    /// <summary>
    /// A central location for sharing state between components during the host building process.
    /// </summary>
    IDictionary<string, object> Properties { get; }

    /// <summary>
    /// Adds services to the container. This can be called multiple times and the results will be additive.
    /// </summary>
    /// <param name="configureDelegate">The delegate for configuring the <see cref="IServiceCollection"/> that will be used
    /// to construct the <see cref="IServiceProvider"/>.</param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    IHostBuilder ConfigureServices(Action<IServiceCollection> configureDelegate);

    /// <summary>
    /// Sets up the configuration for the remainder of the build process and application. This can be called multiple times and
    /// the results will be additive. The results will be available at <see cref="HostBuilderContext.Configuration"/> for
    /// subsequent operations, as well as in <see cref="IHost.Services"/>.
    /// </summary>
    /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder"/> that will be used
    /// to construct the <see cref="IConfiguration"/> for the host.</param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    IHostBuilder ConfigureAppConfiguration(Action<IConfigurationBuilder> configureDelegate);

    /// <summary>
    /// Adds a delegate for configuring the provided <see cref="ILoggingBuilder"/>. This may be called multiple times.
    /// </summary>
    /// <param name="configureLogging">The delegate that configures the <see cref="ILoggingBuilder"/>.</param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    IHostBuilder ConfigureLogging(Action<ILoggingBuilder> configureLogging);

    /// <summary>
    /// Run the given actions to initialize the host. This can only be called once.
    /// </summary>
    /// <returns>An initialized <see cref="IHost"/>.</returns>
    IHost Build();
}

