using MiniCore.Framework.Http;
using MiniCore.Framework.Routing;
using MiniCore.Framework.Routing.Abstractions;
using Xunit;

namespace MiniCore.Framework.Tests.Routing;

public class RouteRegistryTests
{
    private readonly IRouteRegistry _registry;

    public RouteRegistryTests()
    {
        _registry = new RouteRegistry(new RouteMatcher());
    }

    [Fact]
    public void Map_RegistersRoute()
    {
        var handler = CreateHandler("test");
        _registry.Map("GET", "/api/test", handler);

        var result = _registry.TryMatch("GET", "/api/test", out var matchedHandler, out var routeData);

        Assert.True(result);
        Assert.NotNull(matchedHandler);
    }

    [Fact]
    public void TryMatch_MatchesByMethod()
    {
        var getHandler = CreateHandler("get");
        var postHandler = CreateHandler("post");

        _registry.Map("GET", "/api/test", getHandler);
        _registry.Map("POST", "/api/test", postHandler);

        var getResult = _registry.TryMatch("GET", "/api/test", out var getMatchedHandler, out _);
        var postResult = _registry.TryMatch("POST", "/api/test", out var postMatchedHandler, out _);

        Assert.True(getResult);
        Assert.True(postResult);
        Assert.NotSame(getMatchedHandler, postMatchedHandler);
    }

    [Fact]
    public void TryMatch_WithParameters_ExtractsParameters()
    {
        var handler = CreateHandler("test");
        _registry.Map("GET", "/api/links/{id}", handler);

        var result = _registry.TryMatch("GET", "/api/links/123", out var matchedHandler, out var routeData);

        Assert.True(result);
        Assert.NotNull(routeData);
        Assert.Equal("123", routeData!.Values["id"]);
    }

    [Fact]
    public void MapFallback_MatchesWhenNoRouteMatches()
    {
        var fallbackHandler = CreateHandler("fallback");
        _registry.MapFallback(fallbackHandler);

        var result = _registry.TryMatch("GET", "/unknown/path", out var matchedHandler, out var routeData);

        Assert.True(result);
        Assert.Same(fallbackHandler, matchedHandler);
    }

    [Fact]
    public void TryMatch_NoMatch_ReturnsFalse()
    {
        var handler = CreateHandler("test");
        _registry.Map("GET", "/api/test", handler);

        var result = _registry.TryMatch("GET", "/api/unknown", out var matchedHandler, out var routeData);

        Assert.False(result);
        Assert.Null(matchedHandler);
        Assert.Null(routeData);
    }

    [Fact]
    public void TryMatch_MethodCaseInsensitive_Matches()
    {
        var handler = CreateHandler("test");
        _registry.Map("GET", "/api/test", handler);

        var result = _registry.TryMatch("get", "/api/test", out var matchedHandler, out _);

        Assert.True(result);
        Assert.NotNull(matchedHandler);
    }

    private static RequestDelegate CreateHandler(string name)
    {
        return async context =>
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(name);
            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        };
    }
}

