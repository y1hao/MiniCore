using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Routing.Abstractions;

namespace MiniCore.Framework.Routing;

/// <summary>
/// Default implementation of <see cref="IRouteRegistry"/>.
/// </summary>
public class RouteRegistry : IRouteRegistry
{
    private readonly IRouteMatcher _matcher;
    private readonly List<RouteEntry> _routes = new();
    private RequestDelegate? _fallbackHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteRegistry"/> class.
    /// </summary>
    /// <param name="matcher">The route matcher.</param>
    public RouteRegistry(IRouteMatcher matcher)
    {
        _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
    }

    /// <inheritdoc />
    public void Map(string method, string pattern, RequestDelegate handler)
    {
        if (string.IsNullOrEmpty(method))
        {
            throw new ArgumentException("Method cannot be null or empty.", nameof(method));
        }
        if (string.IsNullOrEmpty(pattern))
        {
            throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern));
        }
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        _routes.Add(new RouteEntry
        {
            Method = method.ToUpperInvariant(),
            Pattern = pattern,
            Handler = handler
        });
    }

    /// <inheritdoc />
    public void MapFallback(RequestDelegate handler)
    {
        _fallbackHandler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <inheritdoc />
    public bool TryMatch(string method, string path, out RequestDelegate? handler, out RouteData? routeData)
    {
        handler = null;
        routeData = null;

        if (string.IsNullOrEmpty(method) || string.IsNullOrEmpty(path))
        {
            return false;
        }

        method = method.ToUpperInvariant();

        // First, try to match against specific (non-catch-all) routes
        foreach (var route in _routes)
        {
            // Skip catch-all routes in the first pass
            if (IsCatchAllRoute(route.Pattern))
            {
                continue;
            }

            if (route.Method == method && _matcher.TryMatch(route.Pattern, path, out var matchedRouteData))
            {
                handler = route.Handler;
                routeData = matchedRouteData;
                return true;
            }
        }

        // If no specific route matched, try catch-all routes
        foreach (var route in _routes)
        {
            // Only check catch-all routes in the second pass
            if (!IsCatchAllRoute(route.Pattern))
            {
                continue;
            }

            if (route.Method == method && _matcher.TryMatch(route.Pattern, path, out var matchedRouteData))
            {
                handler = route.Handler;
                routeData = matchedRouteData;
                return true;
            }
        }

        // If no route matched, try fallback
        if (_fallbackHandler != null)
        {
            handler = _fallbackHandler;
            routeData = new RouteData();
            return true;
        }

        return false;
    }

    private static bool IsCatchAllRoute(string pattern)
    {
        return pattern.EndsWith("{*path}", StringComparison.OrdinalIgnoreCase);
    }

    private class RouteEntry
    {
        public string Method { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public RequestDelegate Handler { get; set; } = null!;
    }
}

