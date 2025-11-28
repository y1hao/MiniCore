using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using Xunit;

namespace MiniCore.Framework.Tests.Http;

public class ApplicationBuilderTests
{
    [Fact]
    public async Task Use_AddsMiddlewareToPipeline()
    {
        // Arrange
        var serviceProvider = new ServiceProvider(new ServiceCollection());
        var builder = new ApplicationBuilder(serviceProvider);
        var executed = new List<string>();

        // Act
        builder.Use(next => async context =>
        {
            executed.Add("middleware1");
            await next(context);
        });

        builder.Use(next => async context =>
        {
            executed.Add("middleware2");
            await next(context);
        });

        var pipeline = builder.Build();
        var context = new HttpContext();

        // Assert
        await pipeline(context);
        Assert.Equal(new[] { "middleware1", "middleware2" }, executed);
    }

    [Fact]
    public async Task Use_ExecutesMiddlewareInOrder()
    {
        // Arrange
        var serviceProvider = new ServiceProvider(new ServiceCollection());
        var builder = new ApplicationBuilder(serviceProvider);
        var executed = new List<string>();

        // Act
        builder.Use(next => async context =>
        {
            executed.Add("first");
            await next(context);
            executed.Add("first-after");
        });

        builder.Use(next => async context =>
        {
            executed.Add("second");
            await next(context);
            executed.Add("second-after");
        });

        var pipeline = builder.Build();
        var context = new HttpContext();

        // Assert
        await pipeline(context);
        Assert.Equal(new[] { "first", "second", "second-after", "first-after" }, executed);
    }

    [Fact]
    public async Task Build_Returns404IfNoMiddlewareHandlesRequest()
    {
        // Arrange
        var serviceProvider = new ServiceProvider(new ServiceCollection());
        var builder = new ApplicationBuilder(serviceProvider);
        var pipeline = builder.Build();
        var context = new HttpContext();

        // Act
        await pipeline(context);

        // Assert
        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task Use_CanModifyResponse()
    {
        // Arrange
        var serviceProvider = new ServiceProvider(new ServiceCollection());
        var builder = new ApplicationBuilder(serviceProvider);

        // Act
        builder.Use(next => async context =>
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/plain";
            await context.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("Hello"));
        });

        var pipeline = builder.Build();
        var context = new HttpContext();

        // Assert
        await pipeline(context);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal("text/plain", context.Response.ContentType);
    }

    [Fact]
    public void New_CreatesNewBuilderWithSharedProperties()
    {
        // Arrange
        var serviceProvider = new ServiceProvider(new ServiceCollection());
        var builder = new ApplicationBuilder(serviceProvider);
        builder.Properties["key"] = "value";

        // Act
        var newBuilder = builder.New();

        // Assert
        Assert.NotSame(builder, newBuilder);
        Assert.Equal("value", newBuilder.Properties["key"]);
        Assert.Same(builder.ApplicationServices, newBuilder.ApplicationServices);
    }
}

