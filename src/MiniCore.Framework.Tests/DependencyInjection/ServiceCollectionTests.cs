using MiniCore.Framework.DependencyInjection;

namespace MiniCore.Framework.Tests.DependencyInjection;

public class ServiceCollectionTests
{
    [Fact]
    public void ServiceCollection_IsEmpty_Initially()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Empty(services);
    }

    [Fact]
    public void ServiceCollection_CanAddServiceDescriptor()
    {
        // Arrange
        var services = new ServiceCollection();
        var descriptor = ServiceDescriptor.Singleton<IService, Service>();

        // Act
        services.Add(descriptor);

        // Assert
        Assert.Single(services);
        Assert.Equal(descriptor, services[0]);
    }

    [Fact]
    public void ServiceCollection_CanAddMultipleDescriptors()
    {
        // Arrange
        var services = new ServiceCollection();
        var descriptor1 = ServiceDescriptor.Singleton<IService, Service>();
        var descriptor2 = ServiceDescriptor.Transient<IOtherService, OtherService>();

        // Act
        services.Add(descriptor1);
        services.Add(descriptor2);

        // Assert
        Assert.Equal(2, services.Count);
    }

    [Fact]
    public void ServiceCollection_Constructor_WithCollection_CopiesItems()
    {
        // Arrange
        var original = new ServiceCollection
        {
            ServiceDescriptor.Singleton<IService, Service>(),
            ServiceDescriptor.Transient<IOtherService, OtherService>()
        };

        // Act
        var copy = new ServiceCollection(original);

        // Assert
        Assert.Equal(2, copy.Count);
        Assert.Equal(original[0].ServiceType, copy[0].ServiceType);
        Assert.Equal(original[1].ServiceType, copy[1].ServiceType);
    }

    // Test interfaces and implementations
    public interface IService { }
    public class Service : IService { }

    public interface IOtherService { }
    public class OtherService : IOtherService { }
}

