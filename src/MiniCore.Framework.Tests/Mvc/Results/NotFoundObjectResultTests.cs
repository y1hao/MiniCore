using System.Text;
using MiniCore.Framework.Http;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Results;
using Xunit;

namespace MiniCore.Framework.Tests.Mvc.Results;

public class NotFoundObjectResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_SetsStatusCode404()
    {
        // Arrange
        var value = new { error = "Not found" };
        var result = new NotFoundObjectResult(value);
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        Assert.Equal(404, context.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ExecuteResultAsync_SerializesValueToJson()
    {
        // Arrange
        var value = new { error = "Resource not found", id = 123 };
        var result = new NotFoundObjectResult(value);
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        context.HttpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(context.HttpContext.Response.Body, Encoding.UTF8, leaveOpen: true);
        var json = reader.ReadToEnd();
        Assert.Contains("Resource not found", json);
        Assert.Contains("123", json);
    }

    private static ActionContext CreateActionContext()
    {
        return new ActionContext
        {
            HttpContext = new HttpContext()
        };
    }
}

