using System.Reflection;
using System.Text;
using System.Text.Json;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Http;
using MiniCore.Framework.Mvc;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Controllers;
using MiniCore.Framework.Mvc.ModelBinding;
using MiniCore.Framework.Mvc.Results;
using MiniCore.Framework.Routing.Abstractions;
using Xunit;
using IServiceProvider = MiniCore.Framework.DependencyInjection.IServiceProvider;

namespace MiniCore.Framework.Tests.Mvc;

public class ControllerActionInvokerTests
{
    [Fact]
    public async Task InvokeAsync_CreatesControllerInstance()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var invoker = new ControllerActionInvoker(
            typeof(TestController),
            typeof(TestController).GetMethod(nameof(TestController.Get))!,
            serviceProvider);

        var context = CreateActionContext();

        // Act
        await invoker.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_BindsRouteParameter()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var method = typeof(TestController).GetMethod(nameof(TestController.GetById))!;
        var invoker = new ControllerActionInvoker(typeof(TestController), method, serviceProvider);

        var context = CreateActionContext();
        var routeData = new RouteData();
        routeData.Values.Add("id", "42");
        context.RouteData = routeData;

        // Act
        await invoker.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_BindsQueryParameter()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var method = typeof(TestController).GetMethod(nameof(TestController.Search))!;
        var invoker = new ControllerActionInvoker(typeof(TestController), method, serviceProvider);

        var context = CreateActionContext();
        context.HttpContext.Request.QueryString = "?query=test&page=2";

        // Act
        await invoker.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_BindsFromBody()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var method = typeof(TestController).GetMethod(nameof(TestController.Create))!;
        var invoker = new ControllerActionInvoker(typeof(TestController), method, serviceProvider);

        var request = new { name = "Test", value = 123 };
        var json = JsonSerializer.Serialize(request);
        var bytes = Encoding.UTF8.GetBytes(json);

        var context = CreateActionContext();
        context.HttpContext.Request.Body = new MemoryStream(bytes);
        context.HttpContext.Request.ContentType = "application/json";

        // Act
        await invoker.InvokeAsync(context);

        // Assert
        Assert.Equal(201, context.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ExecutesActionResult()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var method = typeof(TestController).GetMethod(nameof(TestController.Get))!;
        var invoker = new ControllerActionInvoker(typeof(TestController), method, serviceProvider);

        var context = CreateActionContext();

        // Act
        await invoker.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_HandlesAsyncMethods()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var method = typeof(TestController).GetMethod(nameof(TestController.GetAsync))!;
        var invoker = new ControllerActionInvoker(typeof(TestController), method, serviceProvider);

        var context = CreateActionContext();

        // Act
        await invoker.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_HandlesNonActionResultReturnTypes()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var method = typeof(TestController).GetMethod(nameof(TestController.GetData))!;
        var invoker = new ControllerActionInvoker(typeof(TestController), method, serviceProvider);

        var context = CreateActionContext();

        // Act
        await invoker.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.HttpContext.Response.StatusCode);
        Assert.Equal("application/json", context.HttpContext.Response.ContentType);
    }

    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        return services.BuildServiceProvider();
    }

    private static ActionContext CreateActionContext()
    {
        return new ActionContext
        {
            HttpContext = new HttpContext()
        };
    }

    private class TestController : ControllerBase
    {
        public IActionResult Get()
        {
            return Ok();
        }

        public IActionResult GetById(int id)
        {
            return Ok(new { id });
        }

        public IActionResult Search(string query, int page = 1)
        {
            return Ok(new { query, page });
        }

        public IActionResult Create([FromBody] CreateRequest request)
        {
            return Created($"/api/items/{request.Value}", request);
        }

        public async Task<IActionResult> GetAsync()
        {
            await Task.Delay(1);
            return Ok();
        }

        public object GetData()
        {
            return new { data = "test" };
        }
    }

    private class CreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}

