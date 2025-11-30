using System.Reflection;
using MiniCore.Framework.Configuration.Abstractions;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Hosting;
using MiniCore.Framework.Http;
using IServiceProvider = MiniCore.Framework.DependencyInjection.IServiceProvider;

namespace MiniCore.Framework.Testing;

/// <summary>
/// Factory for bootstrapping an application in memory for functional end to end tests.
/// </summary>
/// <typeparam name="TEntryPoint">A type in the entry point assembly of the application. Typically the Startup or Program class can be used.</typeparam>
public class WebApplicationFactory<TEntryPoint> : IDisposable where TEntryPoint : class
{
    private WebApplication? _application;
    private TestServer? _server;
    private bool _disposed;
    private readonly List<Action<WebApplicationBuilder>> _configureActions = new();
    private readonly List<Action<IServiceCollection>> _configureServicesActions = new();
    private readonly List<Action<WebApplication>> _configureAppActions = new();

    /// <summary>
    /// Gets the <see cref="WebApplication"/> created by the factory.
    /// </summary>
    public WebApplication Application
    {
        get
        {
            EnsureApplication();
            return _application!;
        }
    }

    /// <summary>
    /// Gets the <see cref="TestServer"/> created by the factory.
    /// </summary>
    public TestServer Server
    {
        get
        {
            EnsureServer();
            return _server!;
        }
    }

    /// <summary>
    /// Gets the service provider from the application.
    /// </summary>
    public IServiceProvider Services => Application.Services;

    /// <summary>
    /// Creates an <see cref="HttpClient"/> for sending requests to the test server.
    /// </summary>
    /// <returns>An <see cref="HttpClient"/>.</returns>
    public HttpClient CreateClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions());
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> for sending requests to the test server.
    /// </summary>
    /// <param name="options">The client options.</param>
    /// <returns>An <see cref="HttpClient"/>.</returns>
    public HttpClient CreateClient(WebApplicationFactoryClientOptions options)
    {
        EnsureServer();
        return _server!.CreateClient(options);
    }

    /// <summary>
    /// Allows for configuring the <see cref="WebApplicationBuilder"/> used to create the application.
    /// </summary>
    /// <param name="configure">A callback to configure the <see cref="WebApplicationBuilder"/>.</param>
    /// <returns>The <see cref="WebApplicationFactory{TEntryPoint}"/>.</returns>
    public WebApplicationFactory<TEntryPoint> WithWebHostBuilder(Action<WebApplicationBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        if (_application != null)
        {
            throw new InvalidOperationException("Cannot configure the application after it has been built.");
        }

        _configureActions.Add(configure);
        return this;
    }

    /// <summary>
    /// Allows for configuring services after the application has been built.
    /// </summary>
    /// <param name="configureServices">A callback to configure the <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="WebApplicationFactory{TEntryPoint}"/>.</returns>
    public WebApplicationFactory<TEntryPoint> ConfigureTestServices(Action<IServiceCollection> configureServices)
    {
        if (configureServices == null)
        {
            throw new ArgumentNullException(nameof(configureServices));
        }

        if (_application != null)
        {
            throw new InvalidOperationException("Cannot configure services after the application has been built. Use WithWebHostBuilder instead.");
        }

        _configureServicesActions.Add(configureServices);
        return this;
    }

    /// <summary>
    /// Allows for configuring the application after it has been built (e.g., middleware pipeline).
    /// </summary>
    /// <param name="configureApp">A callback to configure the <see cref="WebApplication"/>.</param>
    /// <returns>The <see cref="WebApplicationFactory{TEntryPoint}"/>.</returns>
    public WebApplicationFactory<TEntryPoint> ConfigureApplication(Action<WebApplication> configureApp)
    {
        if (configureApp == null)
        {
            throw new ArgumentNullException(nameof(configureApp));
        }

        if (_application != null)
        {
            throw new InvalidOperationException("Cannot configure the application after it has been built.");
        }

        _configureAppActions.Add(configureApp);
        return this;
    }

    /// <summary>
    /// Creates the <see cref="WebApplication"/>.
    /// </summary>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    protected virtual WebApplication CreateApplication()
    {
        // Create builder
        var builder = WebApplicationBuilder.CreateBuilder();

        // Set environment to Testing
        builder.Environment.EnvironmentName = "Testing";

        // Apply configuration actions
        foreach (var configure in _configureActions)
        {
            configure(builder);
        }

        // Apply service configuration actions
        foreach (var configureServices in _configureServicesActions)
        {
            configureServices(builder.Services);
        }

        // Build the application
        var app = builder.Build();

        // Apply application configuration actions (e.g., middleware pipeline)
        foreach (var configureApp in _configureAppActions)
        {
            configureApp(app);
        }

        return app;
    }

    /// <summary>
    /// Ensures the application has been created.
    /// </summary>
    protected void EnsureApplication()
    {
        if (_application == null)
        {
            _application = CreateApplication();
        }
    }

    /// <summary>
    /// Ensures the test server has been created.
    /// </summary>
    protected void EnsureServer()
    {
        EnsureApplication();

        if (_server == null)
        {
            var requestDelegate = _application!.BuildRequestDelegate();
            _server = new TestServer(requestDelegate, _application.Services);
        }
    }

    /// <summary>
    /// Disposes the factory and the application.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _server?.Dispose();
            _application = null;
            _server = null;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}

