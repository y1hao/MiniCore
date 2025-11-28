using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;

namespace MiniCore.Framework.Routing.Abstractions;

/// <summary>
/// Interface for registering and matching routes.
/// </summary>
public interface IRouteRegistry
{
    /// <summary>
    /// Registers a route with the specified HTTP method, pattern, and handler.
    /// </summary>
    /// <param name="method">The HTTP method (e.g., "GET", "POST").</param>
    /// <param name="pattern">The route pattern (e.g., "/api/links/{id}").</param>
    /// <param name="handler">The request delegate to handle the route.</param>
    void Map(string method, string pattern, RequestDelegate handler);

    /// <summary>
    /// Registers a fallback route that matches any HTTP method and path.
    /// </summary>
    /// <param name="handler">The request delegate to handle the fallback route.</param>
    void MapFallback(RequestDelegate handler);

    /// <summary>
    /// Attempts to find a matching route for the given HTTP method and path.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The request path.</param>
    /// <param name="handler">When this method returns, contains the handler if a match was found; otherwise, null.</param>
    /// <param name="routeData">When this method returns, contains the route data if a match was found; otherwise, null.</param>
    /// <returns>True if a matching route was found; otherwise, false.</returns>
    bool TryMatch(string method, string path, out RequestDelegate? handler, out RouteData? routeData);
}

