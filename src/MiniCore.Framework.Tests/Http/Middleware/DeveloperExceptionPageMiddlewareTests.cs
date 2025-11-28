using MiniCore.Framework.Hosting;
using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Http.Middleware;
using Xunit;

namespace MiniCore.Framework.Tests.Http.Middleware;

public class DeveloperExceptionPageMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_CatchesExceptionAndReturnsErrorPage()
    {
        // Arrange
        var environment = new WebHostEnvironment
        {
            EnvironmentName = "Development",
            ContentRootPath = Directory.GetCurrentDirectory()
        };

        RequestDelegate next = context => throw new InvalidOperationException("Test exception");
        var middleware = new DeveloperExceptionPageMiddleware(next, environment);
        var context = new HttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", context.Response.ContentType);
        Assert.True(context.Response.Body.Length > 0);
    }

    [Fact]
    public async Task InvokeAsync_RethrowsExceptionInNonDevelopmentEnvironment()
    {
        // Arrange
        var environment = new WebHostEnvironment
        {
            EnvironmentName = "Production",
            ContentRootPath = Directory.GetCurrentDirectory()
        };

        RequestDelegate next = context => throw new InvalidOperationException("Test exception");
        var middleware = new DeveloperExceptionPageMiddleware(next, environment);
        var context = new HttpContext();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
    }

    [Fact]
    public async Task InvokeAsync_PassesThroughWhenNoException()
    {
        // Arrange
        var environment = new WebHostEnvironment
        {
            EnvironmentName = "Development",
            ContentRootPath = Directory.GetCurrentDirectory()
        };

        var executed = false;
        RequestDelegate next = context =>
        {
            executed = true;
            return Task.CompletedTask;
        };

        var middleware = new DeveloperExceptionPageMiddleware(next, environment);
        var context = new HttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(executed);
    }
}

