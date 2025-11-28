using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Http;
using MiniCore.Framework.Routing.Abstractions;

namespace MiniCore.Framework.Http.Middleware;

/// <summary>
/// Middleware that handles routing.
/// </summary>
public class RoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRouteRegistry _routeRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoutingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="routeRegistry">The route registry.</param>
    public RoutingMiddleware(RequestDelegate next, IRouteRegistry routeRegistry)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _routeRegistry = routeRegistry ?? throw new ArgumentNullException(nameof(routeRegistry));
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public Task InvokeAsync(IHttpContext context)
    {
        // Try to match a route
        var method = context.Request.Method;
        var path = context.Request.Path ?? "/";

        if (_routeRegistry.TryMatch(method, path, out var handler, out var routeData))
        {
            // Store route data in context
            if (context is Http.HttpContext httpContext && routeData != null)
            {
                httpContext.RouteData = routeData;
            }

            // Invoke the matched handler
            if (handler != null)
            {
                return handler(context);
            }
        }

        // No route matched, pass to next middleware
        return _next(context);
    }
}

