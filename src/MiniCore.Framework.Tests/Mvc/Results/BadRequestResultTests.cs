using MiniCore.Framework.Http;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Results;
using Xunit;

namespace MiniCore.Framework.Tests.Mvc.Results;

public class BadRequestResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_SetsStatusCode400()
    {
        // Arrange
        var result = new BadRequestResult();
        var context = CreateActionContext();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        Assert.Equal(400, context.HttpContext.Response.StatusCode);
    }

    private static ActionContext CreateActionContext()
    {
        return new ActionContext
        {
            HttpContext = new HttpContext()
        };
    }
}

