using MiniCore.Framework.DependencyInjection;

namespace MiniCore.Framework.Tests.DependencyInjection;

public class ServiceProviderExtensionsTests
{
    [Fact]
    public void GetService_Generic_WhenServiceNotRegistered_ReturnsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService<IService>();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetService_Generic_WhenServiceRegistered_ReturnsInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, Service>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService<IService>();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Service>(result);
    }

    [Fact]
    public void GetRequiredService_Generic_WhenServiceNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IService>());
        Assert.Contains("Unable to resolve service", exception.Message);
    }

    [Fact]
    public void GetRequiredService_Generic_WhenServiceRegistered_ReturnsInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, Service>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetRequiredService<IService>();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Service>(result);
    }

    [Fact]
    public void GetRequiredService_WhenServiceNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService(typeof(IService)));
        Assert.Contains("Unable to resolve service", exception.Message);
    }

    [Fact]
    public void GetRequiredService_WhenServiceRegistered_ReturnsInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, Service>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetRequiredService(typeof(IService));

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Service>(result);
    }

    [Fact]
    public void GetService_WithNullProvider_ThrowsArgumentNullException()
    {
        // Arrange
        MiniCore.Framework.DependencyInjection.IServiceProvider? provider = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => provider!.GetService<IService>());
    }

    [Fact]
    public void GetRequiredService_WithNullProvider_ThrowsArgumentNullException()
    {
        // Arrange
        MiniCore.Framework.DependencyInjection.IServiceProvider? provider = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => provider!.GetRequiredService<IService>());
        Assert.Throws<ArgumentNullException>(() => provider!.GetRequiredService(typeof(IService)));
    }

    [Fact]
    public void GetRequiredService_WithNullServiceType_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => provider.GetRequiredService(null!));
    }

    // Test interfaces and implementations
    public interface IService { }
    public class Service : IService { }
}

