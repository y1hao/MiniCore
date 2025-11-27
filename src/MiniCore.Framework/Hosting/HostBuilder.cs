using MiniCore.Framework.Configuration;
using MiniCore.Framework.Configuration.Abstractions;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Logging;

namespace MiniCore.Framework.Hosting;

/// <summary>
/// A builder for creating an <see cref="IHost"/>.
/// </summary>
public class HostBuilder : IHostBuilder
{
    private readonly List<Action<IServiceCollection>> _configureServicesDelegates = new();
    private readonly List<Action<IConfigurationBuilder>> _configureAppConfigurationDelegates = new();
    private readonly List<Action<ILoggingBuilder>> _configureLoggingDelegates = new();
    private bool _hostBuilt;

    /// <summary>
    /// A central location for sharing state between components during the host building process.
    /// </summary>
    public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds services to the container. This can be called multiple times and the results will be additive.
    /// </summary>
    /// <param name="configureDelegate">The delegate for configuring the <see cref="IServiceCollection"/> that will be used
    /// to construct the <see cref="IServiceProvider"/>.</param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    public IHostBuilder ConfigureServices(Action<IServiceCollection> configureDelegate)
    {
        if (configureDelegate == null)
        {
            throw new ArgumentNullException(nameof(configureDelegate));
        }

        _configureServicesDelegates.Add(configureDelegate);
        return this;
    }

    /// <summary>
    /// Sets up the configuration for the remainder of the build process and application. This can be called multiple times and
    /// the results will be additive. The results will be available at <see cref="HostBuilderContext.Configuration"/> for
    /// subsequent operations, as well as in <see cref="IHost.Services"/>.
    /// </summary>
    /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder"/> that will be used
    /// to construct the <see cref="IConfiguration"/> for the host.</param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    public IHostBuilder ConfigureAppConfiguration(Action<IConfigurationBuilder> configureDelegate)
    {
        if (configureDelegate == null)
        {
            throw new ArgumentNullException(nameof(configureDelegate));
        }

        _configureAppConfigurationDelegates.Add(configureDelegate);
        return this;
    }

    /// <summary>
    /// Adds a delegate for configuring the provided <see cref="ILoggingBuilder"/>. This may be called multiple times.
    /// </summary>
    /// <param name="configureLogging">The delegate that configures the <see cref="ILoggingBuilder"/>.</param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    public IHostBuilder ConfigureLogging(Action<ILoggingBuilder> configureLogging)
    {
        if (configureLogging == null)
        {
            throw new ArgumentNullException(nameof(configureLogging));
        }

        _configureLoggingDelegates.Add(configureLogging);
        return this;
    }

    /// <summary>
    /// Run the given actions to initialize the host. This can only be called once.
    /// </summary>
    /// <returns>An initialized <see cref="IHost"/>.</returns>
    public IHost Build()
    {
        if (_hostBuilt)
        {
            throw new InvalidOperationException("Build can only be called once.");
        }

        _hostBuilt = true;

        // Step 1: Build configuration
        var configurationBuilder = new ConfigurationBuilder();
        foreach (var configureDelegate in _configureAppConfigurationDelegates)
        {
            configureDelegate(configurationBuilder);
        }
        var configuration = configurationBuilder.Build();

        // Step 2: Build service collection
        var services = new ServiceCollection();

        // Register configuration
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IConfigurationRoot>(configuration);

        // Step 3: Configure logging
        var loggingProviders = new List<ILoggerProvider>();
        var loggingBuilder = new LoggingBuilder(services, loggingProviders);
        foreach (var configureLogging in _configureLoggingDelegates)
        {
            configureLogging(loggingBuilder);
        }

        // Register logging if not already registered
        if (!services.Any(sd => sd.ServiceType == typeof(ILoggerFactory)))
        {
            services.AddLogging();
        }

        // Step 4: Register HostApplicationLifetime
        var lifetime = new HostApplicationLifetime();
        services.AddSingleton<IHostApplicationLifetime>(lifetime);

        // Step 5: Configure services (user-defined)
        foreach (var configureServices in _configureServicesDelegates)
        {
            configureServices(services);
        }

        // Step 6: Build service provider
        var serviceProvider = new ServiceProvider(services);

        // Step 7: Create and return host
        return new Host(serviceProvider, lifetime);
    }
}

