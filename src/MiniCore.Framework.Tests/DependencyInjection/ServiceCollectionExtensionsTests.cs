using MiniCore.Framework.DependencyInjection;

namespace MiniCore.Framework.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSingleton_WithTypes_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSingleton<IService, Service>();

        // Assert
        Assert.Single(services);
        var descriptor = services[0];
        Assert.Equal(typeof(IService), descriptor.ServiceType);
        Assert.Equal(typeof(Service), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddSingleton_WithInstance_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();
        var instance = new Service();

        // Act
        services.AddSingleton<IService>(instance);

        // Assert
        Assert.Single(services);
        var descriptor = services[0];
        Assert.Equal(typeof(IService), descriptor.ServiceType);
        Assert.Same(instance, descriptor.ImplementationInstance);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddSingleton_WithFactory_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSingleton<IService>(sp => new Service());

        // Assert
        Assert.Single(services);
        var descriptor = services[0];
        Assert.Equal(typeof(IService), descriptor.ServiceType);
        Assert.NotNull(descriptor.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddScoped_WithTypes_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddScoped<IService, Service>();

        // Assert
        Assert.Single(services);
        var descriptor = services[0];
        Assert.Equal(typeof(IService), descriptor.ServiceType);
        Assert.Equal(typeof(Service), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddScoped_WithFactory_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddScoped<IService>(sp => new Service());

        // Assert
        Assert.Single(services);
        var descriptor = services[0];
        Assert.Equal(typeof(IService), descriptor.ServiceType);
        Assert.NotNull(descriptor.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddTransient_WithTypes_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTransient<IService, Service>();

        // Assert
        Assert.Single(services);
        var descriptor = services[0];
        Assert.Equal(typeof(IService), descriptor.ServiceType);
        Assert.Equal(typeof(Service), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void AddTransient_WithFactory_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTransient<IService>(sp => new Service());

        // Assert
        Assert.Single(services);
        var descriptor = services[0];
        Assert.Equal(typeof(IService), descriptor.ServiceType);
        Assert.NotNull(descriptor.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void AddSingleton_WithTypeParameters_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSingleton(typeof(IService), typeof(Service));

        // Assert
        Assert.Single(services);
        var descriptor = services[0];
        Assert.Equal(typeof(IService), descriptor.ServiceType);
        Assert.Equal(typeof(Service), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddScoped_WithTypeParameters_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddScoped(typeof(IService), typeof(Service));

        // Assert
        Assert.Single(services);
        var descriptor = services[0];
        Assert.Equal(typeof(IService), descriptor.ServiceType);
        Assert.Equal(typeof(Service), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddTransient_WithTypeParameters_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTransient(typeof(IService), typeof(Service));

        // Assert
        Assert.Single(services);
        var descriptor = services[0];
        Assert.Equal(typeof(IService), descriptor.ServiceType);
        Assert.Equal(typeof(Service), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void BuildServiceProvider_CreatesServiceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, Service>();

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider);
        var service = provider.GetService<IService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void BuildServiceProvider_WithOptions_CreatesServiceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new ServiceProviderOptions { ValidateScopes = true };

        // Act
        var provider = services.BuildServiceProvider(options);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void AddSingleton_ReturnsSameCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddSingleton<IService, Service>();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddScoped_ReturnsSameCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddScoped<IService, Service>();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddTransient_ReturnsSameCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddTransient<IService, Service>();

        // Assert
        Assert.Same(services, result);
    }

    // Test interfaces and implementations
    public interface IService { }
    public class Service : IService { }
}

