using System.Text;
using System.Text.Json;
using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Mvc.ModelBinding;
using MiniCore.Framework.Routing.Abstractions;
using Xunit;

namespace MiniCore.Framework.Tests.Mvc.ModelBinding;

public class DefaultModelBinderTests
{
    [Fact]
    public async Task BindModelAsync_BindsFromRouteData()
    {
        // Arrange
        var binder = new DefaultModelBinder();
        var routeData = new RouteData();
        routeData.Values.Add("id", "123");
        var context = new ModelBindingContext
        {
            ModelName = "id",
            ModelType = typeof(int),
            HttpContext = CreateHttpContext(),
            RouteData = routeData
        };

        // Act
        await binder.BindModelAsync(context);

        // Assert
        Assert.True(context.IsModelSet);
        Assert.Equal(123, context.Model);
    }

    [Fact]
    public async Task BindModelAsync_BindsFromQueryString()
    {
        // Arrange
        var binder = new DefaultModelBinder();
        var context = new ModelBindingContext
        {
            ModelName = "page",
            ModelType = typeof(int),
            HttpContext = CreateHttpContext(queryString: "?page=5"),
            RouteData = new RouteData()
        };

        // Act
        await binder.BindModelAsync(context);

        // Assert
        Assert.True(context.IsModelSet);
        Assert.Equal(5, context.Model);
    }

    [Fact]
    public async Task BindModelAsync_BindsStringFromQuery()
    {
        // Arrange
        var binder = new DefaultModelBinder();
        var context = new ModelBindingContext
        {
            ModelName = "search",
            ModelType = typeof(string),
            HttpContext = CreateHttpContext(queryString: "?search=test"),
            RouteData = new RouteData()
        };

        // Act
        await binder.BindModelAsync(context);

        // Assert
        Assert.True(context.IsModelSet);
        Assert.Equal("test", context.Model);
    }

    [Fact]
    public async Task BindModelAsync_BindsComplexTypeFromBody()
    {
        // Arrange
        var binder = new DefaultModelBinder();
        var requestBody = JsonSerializer.Serialize(new { name = "Test", value = 42 });
        var context = new ModelBindingContext
        {
            ModelName = "request",
            ModelType = typeof(TestModel),
            HttpContext = CreateHttpContext(body: requestBody),
            RouteData = new RouteData()
        };

        // Act
        await binder.BindModelAsync(context);

        // Assert
        Assert.True(context.IsModelSet);
        var model = Assert.IsType<TestModel>(context.Model);
        Assert.Equal("Test", model.Name);
        Assert.Equal(42, model.Value);
    }

    [Fact]
    public async Task BindModelAsync_ReturnsDefaultValueWhenNotFound()
    {
        // Arrange
        var binder = new DefaultModelBinder();
        var context = new ModelBindingContext
        {
            ModelName = "missing",
            ModelType = typeof(int),
            HttpContext = CreateHttpContext(),
            RouteData = new RouteData()
        };

        // Act
        await binder.BindModelAsync(context);

        // Assert
        Assert.False(context.IsModelSet);
        Assert.Equal(0, context.Model); // Default for int
    }

    [Fact]
    public async Task BindModelAsync_BindsNullableType()
    {
        // Arrange
        var binder = new DefaultModelBinder();
        var context = new ModelBindingContext
        {
            ModelName = "id",
            ModelType = typeof(int?),
            HttpContext = CreateHttpContext(queryString: "?id=42"),
            RouteData = new RouteData()
        };

        // Act
        await binder.BindModelAsync(context);

        // Assert
        Assert.True(context.IsModelSet);
        Assert.Equal(42, context.Model);
    }

    private static IHttpContext CreateHttpContext(string? queryString = null, string? body = null)
    {
        var httpContext = new MiniCore.Framework.Http.HttpContext();
        if (queryString != null)
        {
            httpContext.Request.QueryString = queryString;
        }
        if (body != null)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            httpContext.Request.Body = new MemoryStream(bytes);
        }
        return httpContext;
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}

