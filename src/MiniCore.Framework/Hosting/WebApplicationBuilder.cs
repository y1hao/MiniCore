using MiniCore.Framework.Configuration;
using MiniCore.Framework.Configuration.Abstractions;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Logging;
using MiniCore.Framework.Routing;
using MiniCore.Framework.Routing.Abstractions;

namespace MiniCore.Framework.Hosting;

/// <summary>
/// A builder for web applications and services.
/// </summary>
public class WebApplicationBuilder
{
    private readonly HostBuilder _hostBuilder;
    private readonly IWebHostEnvironment _environment;
    private readonly ServiceCollection _services;
    private IConfigurationRoot? _configuration;
    private bool _built;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplicationBuilder"/> class.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    internal WebApplicationBuilder(string[]? args = null)
    {
        // Initialize environment
        _environment = new WebHostEnvironment
        {
            ContentRootPath = Directory.GetCurrentDirectory(),
            EnvironmentName = GetEnvironmentName()
        };

        // Initialize service collection
        _services = new ServiceCollection();

        // Initialize host builder
        _hostBuilder = new HostBuilder();

        // Set up default configuration
        ConfigureDefaultConfiguration(args);

        // Set up default logging
        ConfigureDefaultLogging();

        // Register environment in services
        _services.AddSingleton<IWebHostEnvironment>(_environment);
    }

    /// <summary>
    /// Gets the <see cref="IHostBuilder"/> for configuring host-specific properties.
    /// </summary>
    public IHostBuilder Host => _hostBuilder;

    /// <summary>
    /// Gets the <see cref="IWebHostEnvironment"/>.
    /// </summary>
    public IWebHostEnvironment Environment => _environment;

    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> where services are configured.
    /// </summary>
    public IServiceCollection Services => _services;

    /// <summary>
    /// Gets the <see cref="IConfigurationRoot"/> containing the configuration of the application.
    /// </summary>
    public IConfigurationRoot Configuration
    {
        get
        {
            if (_configuration == null)
            {
                var configBuilder = new ConfigurationBuilder();
                ConfigureDefaultConfigurationSources(configBuilder);
                _configuration = configBuilder.Build();
            }
            return _configuration;
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="WebApplicationBuilder"/> with preconfigured defaults.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>The <see cref="WebApplicationBuilder"/>.</returns>
    public static WebApplicationBuilder CreateBuilder(string[]? args = null)
    {
        return new WebApplicationBuilder(args);
    }

    /// <summary>
    /// Builds the <see cref="WebApplication"/>.
    /// </summary>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    public WebApplication Build()
    {
        if (_built)
        {
            throw new InvalidOperationException("Build can only be called once.");
        }

        _built = true;

        // Ensure configuration is built
        if (_configuration == null)
        {
            var configBuilder = new ConfigurationBuilder();
            ConfigureDefaultConfigurationSources(configBuilder);
            _configuration = configBuilder.Build();
        }

        // Register configuration in services
        _services.AddSingleton<IConfiguration>(_configuration);
        _services.AddSingleton<IConfigurationRoot>(_configuration);

        // Register routing services
        _services.AddSingleton<IRouteMatcher, RouteMatcher>();
        _services.AddSingleton<IRouteRegistry>(serviceProvider =>
        {
            var matcher = serviceProvider.GetRequiredService<IRouteMatcher>();
            return new RouteRegistry(matcher);
        });
        _services.AddSingleton<ControllerMapper>(serviceProvider =>
        {
            var routeRegistry = serviceProvider.GetRequiredService<IRouteRegistry>();
            return new ControllerMapper(routeRegistry, serviceProvider);
        });

        // Configure host builder with our services
        _hostBuilder.ConfigureServices(services =>
        {
            // Copy all services from our collection to the host builder's collection
            foreach (var descriptor in _services)
            {
                services.Add(descriptor);
            }
        });

        // Build the host
        var host = _hostBuilder.Build();

        // Get configuration from host services
        var configuration = host.Services.GetService<IConfiguration>() 
            ?? throw new InvalidOperationException("Configuration is not registered in services.");

        // Create and return the web application
        return new WebApplication(host, _environment, configuration);
    }

    private void ConfigureDefaultConfiguration(string[]? args)
    {
        _hostBuilder.ConfigureAppConfiguration(builder =>
        {
            ConfigureDefaultConfigurationSources(builder);
        });
    }

    private void ConfigureDefaultConfigurationSources(IConfigurationBuilder builder)
    {
        var contentRootPath = _environment.ContentRootPath;

        // Add appsettings.json
        var appsettingsPath = Path.Combine(contentRootPath, "appsettings.json");
        if (File.Exists(appsettingsPath))
        {
            builder.AddJsonFile(appsettingsPath, optional: false, reloadOnChange: false);
        }

        // Add appsettings.{Environment}.json
        var envAppsettingsPath = Path.Combine(contentRootPath, $"appsettings.{_environment.EnvironmentName}.json");
        if (File.Exists(envAppsettingsPath))
        {
            builder.AddJsonFile(envAppsettingsPath, optional: true, reloadOnChange: false);
        }

        // Add environment variables (they override JSON values)
        builder.AddEnvironmentVariables();

        // TODO: Add command line arguments support if needed
        // For now, we'll skip command line args parsing as it's not used in the current app
    }

    private void ConfigureDefaultLogging()
    {
        _hostBuilder.ConfigureLogging(builder =>
        {
            var minLogLevel = _environment.IsDevelopment()
                ? LogLevel.Debug
                : LogLevel.Information;

            builder.AddConsole(minLogLevel);
        });
    }

    private static string GetEnvironmentName()
    {
        // Check environment variable first
        var envName = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (!string.IsNullOrEmpty(envName))
        {
            return envName;
        }

        // Default to Production
        return "Production";
    }
}
