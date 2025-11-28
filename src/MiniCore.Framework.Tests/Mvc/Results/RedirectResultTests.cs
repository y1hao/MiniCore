using MiniCore.Framework.Http;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Results;
using Xunit;

namespace MiniCore.Framework.Tests.Mvc.Results;

public class RedirectResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_SetsStatusCode302()
    {
        // Arrange
        var url = "https://example.com";
        var result = new RedirectResult(url);
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        Assert.Equal(302, context.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ExecuteResultAsync_SetsLocationHeader()
    {
        // Arrange
        var url = "https://example.com/redirect";
        var result = new RedirectResult(url);
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        Assert.Equal(url, context.HttpContext.Response.Headers["Location"]);
    }

    private static ActionContext CreateActionContext()
    {
        return new ActionContext
        {
            HttpContext = new HttpContext()
        };
    }
}

