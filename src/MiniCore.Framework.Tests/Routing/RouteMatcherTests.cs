using MiniCore.Framework.Routing;
using MiniCore.Framework.Routing.Abstractions;
using Xunit;

namespace MiniCore.Framework.Tests.Routing;

public class RouteMatcherTests
{
    private readonly IRouteMatcher _matcher = new RouteMatcher();

    [Fact]
    public void TryMatch_ExactMatch_ReturnsTrue()
    {
        var result = _matcher.TryMatch("/api/links", "/api/links", out var routeData);

        Assert.True(result);
        Assert.NotNull(routeData);
        Assert.Empty(routeData!.Values);
    }

    [Fact]
    public void TryMatch_WithParameter_ExtractsParameter()
    {
        var result = _matcher.TryMatch("/api/links/{id}", "/api/links/123", out var routeData);

        Assert.True(result);
        Assert.NotNull(routeData);
        Assert.Single(routeData!.Values);
        Assert.Equal("123", routeData.Values["id"]);
    }

    [Fact]
    public void TryMatch_MultipleParameters_ExtractsAllParameters()
    {
        var result = _matcher.TryMatch("/api/{controller}/{action}/{id}", "/api/links/get/123", out var routeData);

        Assert.True(result);
        Assert.NotNull(routeData);
        Assert.Equal(3, routeData!.Values.Count);
        Assert.Equal("links", routeData.Values["controller"]);
        Assert.Equal("get", routeData.Values["action"]);
        Assert.Equal("123", routeData.Values["id"]);
    }

    [Fact]
    public void TryMatch_NoMatch_ReturnsFalse()
    {
        var result = _matcher.TryMatch("/api/links/{id}", "/api/links", out var routeData);

        Assert.False(result);
        Assert.Null(routeData);
    }

    [Fact]
    public void TryMatch_CatchAllPattern_MatchesRemainingPath()
    {
        var result = _matcher.TryMatch("/api/{*path}", "/api/links/123/details", out var routeData);

        Assert.True(result);
        Assert.NotNull(routeData);
        Assert.Single(routeData!.Values);
        Assert.Equal("links/123/details", routeData.Values["path"]);
    }

    [Fact]
    public void TryMatch_CaseInsensitive_Matches()
    {
        var result = _matcher.TryMatch("/API/LINKS", "/api/links", out var routeData);

        Assert.True(result);
        Assert.NotNull(routeData);
    }

    [Fact]
    public void TryMatch_EmptyPath_ReturnsFalse()
    {
        var result = _matcher.TryMatch("/api/links", "", out var routeData);

        Assert.False(result);
        Assert.Null(routeData);
    }
}

