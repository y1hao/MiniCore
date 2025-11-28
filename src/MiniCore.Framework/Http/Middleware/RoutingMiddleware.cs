using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Http;

namespace MiniCore.Framework.Http.Middleware;

/// <summary>
/// Middleware that handles routing. This is a stub implementation that will be fully implemented in Phase 6.
/// </summary>
public class RoutingMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoutingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public RoutingMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public Task InvokeAsync(IHttpContext context)
    {
        // Phase 6 will implement full routing logic here
        // For now, just pass through to the next middleware
        return _next(context);
    }
}

