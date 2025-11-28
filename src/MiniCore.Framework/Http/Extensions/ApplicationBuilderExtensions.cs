using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Http.Middleware;
using MiniCore.Framework.Hosting;
using MiniCore.Framework.Logging;

namespace MiniCore.Framework.Http.Extensions;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/>.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds a middleware to the application pipeline that will catch exceptions, log them, and re-execute the request in an alternate pipeline.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseDeveloperExceptionPage(this IApplicationBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        return app.Use(next =>
        {
            var environment = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
            var middleware = new DeveloperExceptionPageMiddleware(next, environment);
            return middleware.InvokeAsync;
        });
    }

    /// <summary>
    /// Enables static file serving for the current request path.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <param name="rootPath">The root path for static files (defaults to wwwroot).</param>
    /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseStaticFiles(this IApplicationBuilder app, string? rootPath = null)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        return app.Use(next =>
        {
            var environment = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
            var middleware = new StaticFileMiddleware(next, environment, rootPath);
            return middleware.InvokeAsync;
        });
    }

    /// <summary>
    /// Adds middleware to log HTTP requests and responses.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        return app.Use(next =>
        {
            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<RequestLoggingMiddleware>();
            var middleware = new RequestLoggingMiddleware(next, logger);
            return middleware.InvokeAsync;
        });
    }

    /// <summary>
    /// Adds routing middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseRouting(this IApplicationBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        return app.Use(next =>
        {
            var middleware = new RoutingMiddleware(next);
            return middleware.InvokeAsync;
        });
    }
}

