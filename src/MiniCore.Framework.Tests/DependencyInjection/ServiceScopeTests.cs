using MiniCore.Framework.DependencyInjection;

namespace MiniCore.Framework.Tests.DependencyInjection;

public class ServiceScopeTests
{
    [Fact]
    public void ServiceProvider_Property_ReturnsProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
        var scope = provider.CreateScope();

        // Assert
        Assert.NotNull(scope.ServiceProvider);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();

        // Act & Assert
        scope.Dispose(); // Should not throw
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();

        // Act & Assert
        scope.Dispose();
        scope.Dispose(); // Should not throw
    }

    [Fact]
    public void ServiceProvider_CanResolveServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, Service>();
        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();

        // Act
        var service = scope.ServiceProvider.GetService<IService>();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<Service>(service);
    }

    // Test interfaces and implementations
    public interface IService { }
    public class Service : IService { }
}

