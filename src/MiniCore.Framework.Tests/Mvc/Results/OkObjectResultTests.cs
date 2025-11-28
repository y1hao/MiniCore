using System.Text;
using MiniCore.Framework.Http;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Results;
using Xunit;

namespace MiniCore.Framework.Tests.Mvc.Results;

public class OkObjectResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_SetsStatusCode200()
    {
        // Arrange
        var value = new { message = "Hello" };
        var result = new OkObjectResult(value);
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        Assert.Equal(200, context.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ExecuteResultAsync_SetsContentTypeToJson()
    {
        // Arrange
        var value = new { message = "Hello" };
        var result = new OkObjectResult(value);
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        Assert.Equal("application/json", context.HttpContext.Response.ContentType);
    }

    [Fact]
    public async Task ExecuteResultAsync_SerializesValueToJson()
    {
        // Arrange
        var value = new { message = "Hello", count = 42 };
        var result = new OkObjectResult(value);
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        context.HttpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(context.HttpContext.Response.Body, Encoding.UTF8);
        var json = reader.ReadToEnd();
        Assert.Contains("Hello", json);
        Assert.Contains("42", json);
    }

    [Fact]
    public async Task ExecuteResultAsync_HandlesNullValue()
    {
        // Arrange
        var result = new OkObjectResult(null);
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        Assert.Equal(200, context.HttpContext.Response.StatusCode);
        Assert.Equal("application/json", context.HttpContext.Response.ContentType);
    }

    private static ActionContext CreateActionContext()
    {
        return new ActionContext
        {
            HttpContext = new HttpContext()
        };
    }
}

