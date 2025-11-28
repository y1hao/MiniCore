using System.Text;
using MiniCore.Framework.Http;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Results;
using Xunit;

namespace MiniCore.Framework.Tests.Mvc.Results;

public class CreatedResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_SetsStatusCode201()
    {
        // Arrange
        var value = new { id = 1, name = "Test" };
        var result = new CreatedResult("/api/items/1", value);
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        Assert.Equal(201, context.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ExecuteResultAsync_SetsLocationHeader()
    {
        // Arrange
        var uri = "/api/items/123";
        var value = new { id = 123 };
        var result = new CreatedResult(uri, value);
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        Assert.Equal(uri, context.HttpContext.Response.Headers["Location"]);
    }

    [Fact]
    public async Task ExecuteResultAsync_SerializesValueToJson()
    {
        // Arrange
        var value = new { id = 123, name = "Created Item" };
        var result = new CreatedResult("/api/items/123", value);
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        context.HttpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(context.HttpContext.Response.Body, Encoding.UTF8, leaveOpen: true);
        var json = reader.ReadToEnd();
        Assert.Contains("123", json);
        Assert.Contains("Created Item", json);
    }

    private static ActionContext CreateActionContext()
    {
        return new ActionContext
        {
            HttpContext = new HttpContext()
        };
    }
}

