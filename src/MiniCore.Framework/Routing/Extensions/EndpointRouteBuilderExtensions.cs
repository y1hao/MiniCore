using System.Reflection;
using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Routing.Abstractions;

namespace MiniCore.Framework.Routing.Extensions;

/// <summary>
/// Extension methods for <see cref="IEndpointRouteBuilder"/>.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps a route with the specified HTTP method and pattern to a handler.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="method">The HTTP method (e.g., "GET", "POST").</param>
    /// <param name="pattern">The route pattern (e.g., "/api/links/{id}").</param>
    /// <param name="handler">The request delegate to handle the route.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/>.</returns>
    public static IEndpointRouteBuilder Map(this IEndpointRouteBuilder endpoints, string method, string pattern, RequestDelegate handler)
    {
        if (endpoints == null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        endpoints.RouteRegistry.Map(method, pattern, handler);
        return endpoints;
    }

    /// <summary>
    /// Maps a GET route.
    /// </summary>
    public static IEndpointRouteBuilder MapGet(this IEndpointRouteBuilder endpoints, string pattern, RequestDelegate handler)
    {
        return Map(endpoints, "GET", pattern, handler);
    }

    /// <summary>
    /// Maps a POST route.
    /// </summary>
    public static IEndpointRouteBuilder MapPost(this IEndpointRouteBuilder endpoints, string pattern, RequestDelegate handler)
    {
        return Map(endpoints, "POST", pattern, handler);
    }

    /// <summary>
    /// Maps a PUT route.
    /// </summary>
    public static IEndpointRouteBuilder MapPut(this IEndpointRouteBuilder endpoints, string pattern, RequestDelegate handler)
    {
        return Map(endpoints, "PUT", pattern, handler);
    }

    /// <summary>
    /// Maps a DELETE route.
    /// </summary>
    public static IEndpointRouteBuilder MapDelete(this IEndpointRouteBuilder endpoints, string pattern, RequestDelegate handler)
    {
        return Map(endpoints, "DELETE", pattern, handler);
    }

    /// <summary>
    /// Maps a fallback route that matches any HTTP method and path.
    /// </summary>
    public static IEndpointRouteBuilder MapFallback(this IEndpointRouteBuilder endpoints, RequestDelegate handler)
    {
        if (endpoints == null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        endpoints.RouteRegistry.MapFallback(handler);
        return endpoints;
    }
}

