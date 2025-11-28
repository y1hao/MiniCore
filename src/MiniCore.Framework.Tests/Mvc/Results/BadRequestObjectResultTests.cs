using System.Text;
using MiniCore.Framework.Http;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Results;
using Xunit;

namespace MiniCore.Framework.Tests.Mvc.Results;

public class BadRequestObjectResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_SetsStatusCode400()
    {
        // Arrange
        var error = new { error = "Invalid input" };
        var result = new BadRequestObjectResult(error);
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        Assert.Equal(400, context.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ExecuteResultAsync_SerializesErrorToJson()
    {
        // Arrange
        var error = new { error = "Validation failed", field = "email" };
        var result = new BadRequestObjectResult(error);
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        context.HttpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(context.HttpContext.Response.Body, Encoding.UTF8, leaveOpen: true);
        var json = reader.ReadToEnd();
        Assert.Contains("Validation failed", json);
        Assert.Contains("email", json);
    }

    private static ActionContext CreateActionContext()
    {
        return new ActionContext
        {
            HttpContext = new HttpContext()
        };
    }
}

