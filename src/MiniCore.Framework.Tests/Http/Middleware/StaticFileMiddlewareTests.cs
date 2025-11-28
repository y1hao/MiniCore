using System.Text;
using MiniCore.Framework.Hosting;
using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Http.Middleware;
using Xunit;

namespace MiniCore.Framework.Tests.Http.Middleware;

public class StaticFileMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ServesExistingFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var wwwroot = Path.Combine(tempDir, "wwwroot");
        Directory.CreateDirectory(wwwroot);

        var testFile = Path.Combine(wwwroot, "test.txt");
        await File.WriteAllTextAsync(testFile, "Hello World");

        var environment = new WebHostEnvironment
        {
            ContentRootPath = tempDir
        };

        RequestDelegate next = context => Task.CompletedTask;
        var middleware = new StaticFileMiddleware(next, environment, wwwroot);
        var context = new HttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/test.txt";

        try
        {
            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal("text/plain", context.Response.ContentType);
            context.Response.Body.Position = 0;
            var content = await new StreamReader(context.Response.Body).ReadToEndAsync();
            Assert.Equal("Hello World", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task InvokeAsync_Returns404ForNonExistentFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var wwwroot = Path.Combine(tempDir, "wwwroot");
        Directory.CreateDirectory(wwwroot);

        var environment = new WebHostEnvironment
        {
            ContentRootPath = tempDir
        };

        var nextCalled = false;
        RequestDelegate next = context =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new StaticFileMiddleware(next, environment, wwwroot);
        var context = new HttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/nonexistent.txt";

        try
        {
            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled); // Should pass through to next middleware
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task InvokeAsync_IgnoresNonGetRequests()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var wwwroot = Path.Combine(tempDir, "wwwroot");
        Directory.CreateDirectory(wwwroot);

        var environment = new WebHostEnvironment
        {
            ContentRootPath = tempDir
        };

        var nextCalled = false;
        RequestDelegate next = context =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new StaticFileMiddleware(next, environment, wwwroot);
        var context = new HttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/test.txt";

        try
        {
            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled); // Should pass through to next middleware
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task InvokeAsync_PreventsDirectoryTraversal()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var wwwroot = Path.Combine(tempDir, "wwwroot");
        Directory.CreateDirectory(wwwroot);

        var outsideFile = Path.Combine(tempDir, "outside.txt");
        await File.WriteAllTextAsync(outsideFile, "Should not be served");

        var environment = new WebHostEnvironment
        {
            ContentRootPath = tempDir
        };

        var nextCalled = false;
        RequestDelegate next = context =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new StaticFileMiddleware(next, environment, wwwroot);
        var context = new HttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/../outside.txt";

        try
        {
            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled); // Should pass through to next middleware (security check prevents serving)
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}

