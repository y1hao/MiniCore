using MiniCore.Framework.Http;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Results;
using Xunit;

namespace MiniCore.Framework.Tests.Mvc.Results;

public class NoContentResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_SetsStatusCode204()
    {
        // Arrange
        var result = new NoContentResult();
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        Assert.Equal(204, context.HttpContext.Response.StatusCode);
    }

    private static ActionContext CreateActionContext()
    {
        return new ActionContext
        {
            HttpContext = new HttpContext()
        };
    }
}

