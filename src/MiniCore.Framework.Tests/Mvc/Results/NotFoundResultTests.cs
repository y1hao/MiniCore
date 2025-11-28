using MiniCore.Framework.Http;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Results;
using Xunit;

namespace MiniCore.Framework.Tests.Mvc.Results;

public class NotFoundResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_SetsStatusCode404()
    {
        // Arrange
        var result = new NotFoundResult();
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        Assert.Equal(404, context.HttpContext.Response.StatusCode);
    }

    private static ActionContext CreateActionContext()
    {
        return new ActionContext
        {
            HttpContext = new HttpContext()
        };
    }
}

