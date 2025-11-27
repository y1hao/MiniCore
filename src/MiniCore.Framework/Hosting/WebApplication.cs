using MiniCore.Framework.DependencyInjection;

namespace MiniCore.Framework.Hosting;

/// <summary>
/// The web application used to configure the HTTP pipeline and routes.
/// </summary>
public class WebApplication
{
    private readonly IHost _host;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplication"/> class.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <param name="environment">The web host environment.</param>
    internal WebApplication(IHost host, IWebHostEnvironment environment)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
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
    /// <exception cref="NotImplementedException">Thrown because middleware pipeline is not yet implemented (Phase 5).</exception>
    public WebApplication UseDeveloperExceptionPage()
    {
        throw new NotImplementedException("Middleware pipeline is not yet implemented. This will be available in Phase 5.");
    }

    /// <summary>
    /// Enables static file serving for the current request path.
    /// </summary>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    /// <exception cref="NotImplementedException">Thrown because middleware pipeline is not yet implemented (Phase 5).</exception>
    public WebApplication UseStaticFiles()
    {
        throw new NotImplementedException("Middleware pipeline is not yet implemented. This will be available in Phase 5.");
    }

    /// <summary>
    /// Adds routing middleware to the application pipeline.
    /// </summary>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    /// <exception cref="NotImplementedException">Thrown because middleware pipeline is not yet implemented (Phase 5).</exception>
    public WebApplication UseRouting()
    {
        throw new NotImplementedException("Middleware pipeline is not yet implemented. This will be available in Phase 5.");
    }

    /// <summary>
    /// Adds endpoints for controller actions to the <see cref="WebApplication"/> request execution pipeline.
    /// </summary>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    /// <exception cref="NotImplementedException">Thrown because routing framework is not yet implemented (Phase 6).</exception>
    public WebApplication MapControllers()
    {
        throw new NotImplementedException("Routing framework is not yet implemented. This will be available in Phase 6.");
    }

    /// <summary>
    /// Adds endpoints for Razor Pages to the <see cref="WebApplication"/> request execution pipeline.
    /// </summary>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    /// <exception cref="NotImplementedException">Thrown because routing framework is not yet implemented (Phase 6).</exception>
    public WebApplication MapRazorPages()
    {
        throw new NotImplementedException("Routing framework is not yet implemented. This will be available in Phase 6.");
    }

    /// <summary>
    /// Adds a fallback route that will match if no other route matches.
    /// </summary>
    /// <param name="action">The action name.</param>
    /// <param name="controller">The controller name.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    /// <exception cref="NotImplementedException">Thrown because routing framework is not yet implemented (Phase 6).</exception>
    public WebApplication MapFallbackToController(string action, string controller, string? pattern = null)
    {
        throw new NotImplementedException("Routing framework is not yet implemented. This will be available in Phase 6.");
    }

    /// <summary>
    /// Runs the application and blocks the calling thread until host shutdown.
    /// </summary>
    /// <exception cref="NotImplementedException">Thrown because HTTP server is not yet implemented (Phase 7).</exception>
    public void Run()
    {
        // Start the host (this will start background services)
        _host.StartAsync().GetAwaiter().GetResult();

        // TODO: Phase 7 - Implement HTTP server that listens for requests and processes them through middleware pipeline
        // For now, we'll throw an exception to indicate this isn't fully implemented
        throw new NotImplementedException("HTTP server is not yet implemented. This will be available in Phase 7. The host has been started, but HTTP request handling is not yet available.");
    }
}
