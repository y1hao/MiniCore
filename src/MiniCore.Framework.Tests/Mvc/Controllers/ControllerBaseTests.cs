using MiniCore.Framework.Http;
using MiniCore.Framework.Mvc.Controllers;
using MiniCore.Framework.Mvc.Results;
using Xunit;

namespace MiniCore.Framework.Tests.Mvc.Controllers;

public class ControllerBaseTests
{
    [Fact]
    public void HttpContext_ThrowsWhenNotSet()
    {
        // Arrange
        var controller = new TestController();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _ = controller.HttpContext);
    }

    [Fact]
    public void HttpContext_ReturnsSetValue()
    {
        // Arrange
        var controller = new TestController();
        var httpContext = new HttpContext();

        // Act
        controller.HttpContext = httpContext;

        // Assert
        Assert.Same(httpContext, controller.HttpContext);
    }

    // Note: Request and Response are protected, so we can't test them directly
    // They are tested indirectly through action result methods

    // Note: Ok(), BadRequest(), NotFound(), etc. are protected methods
    // They are tested indirectly through ControllerActionInvoker tests

    // Note: BadRequest(), NotFound(), NoContent(), Created(), Redirect() are protected methods
    // They are tested indirectly through ControllerActionInvoker tests

    private class TestController : ControllerBase
    {
    }
}

