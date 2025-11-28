using MiniCore.Framework.Http.Abstractions;

namespace MiniCore.Framework.Routing.Abstractions;

/// <summary>
/// Interface for matching routes against request paths.
/// </summary>
public interface IRouteMatcher
{
    /// <summary>
    /// Attempts to match a route pattern against a request path.
    /// </summary>
    /// <param name="pattern">The route pattern (e.g., "/api/links/{id}").</param>
    /// <param name="path">The request path (e.g., "/api/links/123").</param>
    /// <param name="routeData">When this method returns, contains the route data if a match was found; otherwise, null.</param>
    /// <returns>True if the pattern matches the path; otherwise, false.</returns>
    bool TryMatch(string pattern, string path, out RouteData? routeData);
}

/// <summary>
/// Represents route data extracted from a matched route.
/// </summary>
public class RouteData
{
    /// <summary>
    /// Gets the route values dictionary containing parameter names and values.
    /// </summary>
    public Dictionary<string, string> Values { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

