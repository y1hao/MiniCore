using MiniCore.Framework.Configuration.Abstractions;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Http.Extensions;
using MiniCore.Framework.Logging;
using MiniCore.Framework.Routing;
using MiniCore.Framework.Routing.Abstractions;
using MiniCore.Framework.Server;
using MiniCore.Framework.Server.Abstractions;
using IServiceProvider = MiniCore.Framework.DependencyInjection.IServiceProvider;

namespace MiniCore.Framework.Hosting;

/// <summary>
/// The web application used to configure the HTTP pipeline and routes.
/// </summary>
public class WebApplication
{
    private readonly IHost _host;
    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationBuilder _applicationBuilder;
    private readonly IConfiguration _configuration;
    private RequestDelegate? _requestDelegate;
    private IServer? _server;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplication"/> class.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <param name="environment">The web host environment.</param>
    /// <param name="configuration">The configuration.</param>
    internal WebApplication(IHost host, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _applicationBuilder = new ApplicationBuilder(_host.Services);
    }

    /// <summary>
    /// Gets the <see cref="IWebHostEnvironment"/>.
    /// </summary>
    public IWebHostEnvironment Environment => _environment;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> for the application.
    /// </summary>
    public DependencyInjection.IServiceProvider Services => _host.Services;

    /// <summary>
    /// Adds a middleware to the application pipeline that will catch exceptions, log them, and re-execute the request in an alternate pipeline.
    /// </summary>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    public WebApplication UseDeveloperExceptionPage()
    {
        _applicationBuilder.UseDeveloperExceptionPage();
        return this;
    }

    /// <summary>
    /// Enables static file serving for the current request path.
    /// </summary>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    public WebApplication UseStaticFiles()
    {
        _applicationBuilder.UseStaticFiles();
        return this;
    }

    /// <summary>
    /// Adds routing middleware to the application pipeline.
    /// </summary>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    public WebApplication UseRouting()
    {
        _applicationBuilder.UseRouting();
        return this;
    }

    /// <summary>
    /// Adds endpoints for controller actions to the <see cref="WebApplication"/> request execution pipeline.
    /// </summary>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    public WebApplication MapControllers()
    {
        // Get controller mapper from services
        var mapper = Services.GetService<ControllerMapper>();
        if (mapper != null)
        {
            mapper.MapControllers();
        }
        return this;
    }

    /// <summary>
    /// Adds endpoints for Razor Pages to the <see cref="WebApplication"/> request execution pipeline.
    /// </summary>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    public WebApplication MapRazorPages()
    {
        // Phase 6: Razor Pages support is not yet implemented
        // This is a placeholder for future implementation
        return this;
    }

    /// <summary>
    /// Adds a fallback route that will match if no other route matches.
    /// </summary>
    /// <param name="action">The action name.</param>
    /// <param name="controller">The controller name.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    public WebApplication MapFallbackToController(string action, string controller, string? pattern = null)
    {
        var mapper = Services.GetService<ControllerMapper>();
        if (mapper != null)
        {
            mapper.MapFallbackToController(action, controller, pattern);
        }
        return this;
    }

    /// <summary>
    /// Gets the built request delegate pipeline.
    /// </summary>
    /// <returns>The request delegate.</returns>
    internal RequestDelegate BuildRequestDelegate()
    {
        if (_requestDelegate == null)
        {
            _requestDelegate = _applicationBuilder.Build();
        }
        return _requestDelegate;
    }

    /// <summary>
    /// Runs the application and blocks the calling thread until host shutdown.
    /// </summary>
    public void Run()
    {
        RunAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Runs the application and returns a task that completes on shutdown.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RunAsync()
    {
        // Build the request delegate pipeline
        var requestDelegate = BuildRequestDelegate();

        // Get URLs from configuration
        var urls = GetUrls();

        // Get logger
        var loggerFactory = _host.Services.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<HttpListenerServer>();

        // Create and start the server
        // Explicitly use DependencyInjection.IServiceProvider to avoid type ambiguity
        DependencyInjection.IServiceProvider serviceProvider = _host.Services;
        _server = new HttpListenerServer(urls, requestDelegate, serviceProvider, logger);
        await _server.StartAsync().ConfigureAwait(false);

        // Start the host (this will start background services)
        await _host.StartAsync().ConfigureAwait(false);

        // Wait for host shutdown
        var lifetime = _host.Services.GetService<IHostApplicationLifetime>();
        if (lifetime != null)
        {
            // Wait for shutdown signal
            var tcs = new TaskCompletionSource();
            lifetime.ApplicationStopping.Register(() => tcs.SetResult());
            await tcs.Task.ConfigureAwait(false);
        }
        else
        {
            // Fallback: wait indefinitely (or until Ctrl+C)
            await Task.Delay(Timeout.Infinite).ConfigureAwait(false);
        }

        // Stop the server
        if (_server != null)
        {
            await _server.StopAsync().ConfigureAwait(false);
        }

        // Stop the host
        await _host.StopAsync().ConfigureAwait(false);
    }

    private string[] GetUrls()
    {
        // Check configuration for URLs
        var urls = _configuration["Urls"];
        if (!string.IsNullOrEmpty(urls))
        {
            return urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        // Check ASPNETCORE_URLS environment variable
        var envUrls = System.Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        if (!string.IsNullOrEmpty(envUrls))
        {
            return envUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        // Default URLs
        return new[] { "http://localhost:5000", "https://localhost:5001" };
    }
}
