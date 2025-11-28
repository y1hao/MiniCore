using System.Reflection;
using MiniCore.Framework.Mvc;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Controllers;
using MiniCore.Framework.Routing.Attributes;
using Xunit;

namespace MiniCore.Framework.Tests.Mvc;

public class ControllerDiscoveryTests
{
    [Fact]
    public void DiscoverControllers_FindsControllersByConvention()
    {
        // Arrange
        var discovery = new ControllerDiscovery();
        var assembly = typeof(TestController).Assembly;

        // Act
        var controllers = discovery.DiscoverControllers(assembly).ToList();

        // Assert
        Assert.Contains(controllers, c => c.ControllerType == typeof(TestController));
    }

    [Fact]
    public void DiscoverControllers_FindsControllersByAttribute()
    {
        // Arrange
        var discovery = new ControllerDiscovery();
        var assembly = typeof(AttributeTestController).Assembly;

        // Act
        var controllers = discovery.DiscoverControllers(assembly).ToList();

        // Assert
        Assert.Contains(controllers, c => c.ControllerType == typeof(AttributeTestController));
    }

    [Fact]
    public void DiscoverControllers_ExtractsRoutePrefix()
    {
        // Arrange
        var discovery = new ControllerDiscovery();
        var assembly = typeof(TestController).Assembly;

        // Act
        var controllers = discovery.DiscoverControllers(assembly).ToList();
        var controller = controllers.First(c => c.ControllerType == typeof(TestController));

        // Assert
        Assert.Equal("api/test", controller.RoutePrefix);
    }

    [Fact]
    public void GetActionMethods_FindsPublicMethods()
    {
        // Arrange
        var discovery = new ControllerDiscovery();

        // Act
        var actions = discovery.GetActionMethods(typeof(TestController)).ToList();

        // Assert
        Assert.Contains(actions, a => a.Method.Name == "Get");
        Assert.Contains(actions, a => a.Method.Name == "Post");
    }

    [Fact]
    public void GetActionMethods_ExcludesNonActionMethods()
    {
        // Arrange
        var discovery = new ControllerDiscovery();

        // Act
        var actions = discovery.GetActionMethods(typeof(TestController)).ToList();

        // Assert
        Assert.DoesNotContain(actions, a => a.Method.Name == "HelperMethod");
    }

    [Fact]
    public void GetActionMethods_DetectsHttpGet()
    {
        // Arrange
        var discovery = new ControllerDiscovery();

        // Act
        var actions = discovery.GetActionMethods(typeof(TestController)).ToList();
        var getAction = actions.First(a => a.Method.Name == "Get");

        // Assert
        Assert.Contains(getAction.HttpMethods, m => m.Method == "GET");
    }

    [Fact]
    public void GetActionMethods_DetectsHttpPost()
    {
        // Arrange
        var discovery = new ControllerDiscovery();

        // Act
        var actions = discovery.GetActionMethods(typeof(TestController)).ToList();
        var postAction = actions.First(a => a.Method.Name == "Post");

        // Assert
        Assert.Contains(postAction.HttpMethods, m => m.Method == "POST");
    }

    [Fact]
    public void GetActionMethods_DefaultsToGetWhenNoAttribute()
    {
        // Arrange
        var discovery = new ControllerDiscovery();

        // Act
        var actions = discovery.GetActionMethods(typeof(TestController)).ToList();
        var defaultAction = actions.First(a => a.Method.Name == "DefaultAction");

        // Assert
        Assert.Contains(defaultAction.HttpMethods, m => m.Method == "GET");
    }

    // Test controllers
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public void Get() { }

        [HttpPost]
        public void Post() { }

        public void DefaultAction() { }

        [NonAction]
        public void HelperMethod() { }
    }

    [Controller]
    public class AttributeTestController : ControllerBase
    {
        [HttpGet]
        public void Index() { }
    }
}

