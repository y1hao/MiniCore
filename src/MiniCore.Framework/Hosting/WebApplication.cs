using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Http.Extensions;
using MiniCore.Framework.Routing;
using MiniCore.Framework.Routing.Abstractions;

namespace MiniCore.Framework.Hosting;

/// <summary>
/// The web application used to configure the HTTP pipeline and routes.
/// </summary>
public class WebApplication
{
    private readonly IHost _host;
    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationBuilder _applicationBuilder;
    private RequestDelegate? _requestDelegate;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplication"/> class.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <param name="environment">The web host environment.</param>
    internal WebApplication(IHost host, IWebHostEnvironment environment)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
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
    /// <exception cref="NotImplementedException">Thrown because HTTP server is not yet implemented (Phase 7).</exception>
    public void Run()
    {
        // Build the request delegate pipeline
        BuildRequestDelegate();

        // Start the host (this will start background services)
        _host.StartAsync().GetAwaiter().GetResult();

        // TODO: Phase 7 - Implement HTTP server that listens for requests and processes them through middleware pipeline
        // For now, we'll throw an exception to indicate this isn't fully implemented
        throw new NotImplementedException("HTTP server is not yet implemented. This will be available in Phase 7. The host has been started, but HTTP request handling is not yet available.");
    }
}
