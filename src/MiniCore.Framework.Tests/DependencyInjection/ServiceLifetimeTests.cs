using MiniCore.Framework.DependencyInjection;

namespace MiniCore.Framework.Tests.DependencyInjection;

public class ServiceLifetimeTests
{
    [Fact]
    public void Singleton_ReturnsSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, Service>();
        var provider = services.BuildServiceProvider();

        // Act
        var instance1 = provider.GetService<IService>();
        var instance2 = provider.GetService<IService>();

        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Singleton_WithInstance_ReturnsSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var instance = new Service();
        services.AddSingleton<IService>(instance);
        var provider = services.BuildServiceProvider();

        // Act
        var resolved1 = provider.GetService<IService>();
        var resolved2 = provider.GetService<IService>();

        // Assert
        Assert.Same(instance, resolved1);
        Assert.Same(instance, resolved2);
    }

    [Fact]
    public void Singleton_SharedAcrossScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, Service>();
        var provider = services.BuildServiceProvider();

        // Act
        var rootInstance = provider.GetService<IService>();
        using var scope1 = provider.CreateScope();
        var scope1Instance = scope1.ServiceProvider.GetService<IService>();
        using var scope2 = provider.CreateScope();
        var scope2Instance = scope2.ServiceProvider.GetService<IService>();

        // Assert
        Assert.Same(rootInstance, scope1Instance);
        Assert.Same(rootInstance, scope2Instance);
        Assert.Same(scope1Instance, scope2Instance);
    }

    [Fact]
    public void Scoped_ReturnsSameInstanceWithinScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IService, Service>();
        var provider = services.BuildServiceProvider();

        // Act
        using var scope = provider.CreateScope();
        var instance1 = scope.ServiceProvider.GetService<IService>();
        var instance2 = scope.ServiceProvider.GetService<IService>();

        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Scoped_ReturnsDifferentInstancesAcrossScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IService, Service>();
        var provider = services.BuildServiceProvider();

        // Act
        using var scope1 = provider.CreateScope();
        var instance1 = scope1.ServiceProvider.GetService<IService>();
        using var scope2 = provider.CreateScope();
        var instance2 = scope2.ServiceProvider.GetService<IService>();

        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.NotSame(instance1, instance2);
    }

    [Fact]
    public void Scoped_ResolvedFromRoot_ThrowsWhenValidateScopesEnabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IService, Service>();
        var options = new ServiceProviderOptions { ValidateScopes = true };
        var provider = services.BuildServiceProvider(options);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetService<IService>());
        Assert.Contains("Cannot resolve scoped service", exception.Message);
    }

    [Fact]
    public void Scoped_ResolvedFromRoot_WorksWhenValidateScopesDisabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IService, Service>();
        var options = new ServiceProviderOptions { ValidateScopes = false };
        var provider = services.BuildServiceProvider(options);

        // Act
        var instance = provider.GetService<IService>();

        // Assert
        Assert.NotNull(instance);
    }

    [Fact]
    public void Transient_ReturnsDifferentInstances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IService, Service>();
        var provider = services.BuildServiceProvider();

        // Act
        var instance1 = provider.GetService<IService>();
        var instance2 = provider.GetService<IService>();

        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.NotSame(instance1, instance2);
    }

    [Fact]
    public void Transient_ReturnsDifferentInstancesAcrossScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IService, Service>();
        var provider = services.BuildServiceProvider();

        // Act
        using var scope1 = provider.CreateScope();
        var instance1 = scope1.ServiceProvider.GetService<IService>();
        using var scope2 = provider.CreateScope();
        var instance2 = scope2.ServiceProvider.GetService<IService>();

        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.NotSame(instance1, instance2);
    }

    [Fact]
    public void Singleton_DisposedWhenProviderDisposed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDisposableService, DisposableService>();
        var provider = services.BuildServiceProvider();
        var service = (DisposableService)provider.GetService<IDisposableService>()!;

        // Act
        provider.Dispose();

        // Assert
        Assert.NotNull(service);
        Assert.True(service.IsDisposed);
    }

    [Fact]
    public void Scoped_DisposedWhenScopeDisposed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IDisposableService, DisposableService>();
        var provider = services.BuildServiceProvider();
        DisposableService service;
        using (var scope = provider.CreateScope())
        {
            service = (DisposableService)scope.ServiceProvider.GetService<IDisposableService>()!;
            Assert.False(service.IsDisposed);
        }

        // Act & Assert
        Assert.True(service.IsDisposed);
    }

    [Fact]
    public void CircularDependency_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ICircularA, CircularA>();
        services.AddSingleton<ICircularB, CircularB>();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetService<ICircularA>());
        Assert.Contains("Circular dependency detected", exception.Message);
    }

    [Fact]
    public void ValidateOnBuild_ThrowsWhenServiceCannotBeResolved()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, ServiceWithDependency>(); // Missing IDependency
        var options = new ServiceProviderOptions { ValidateOnBuild = true };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => services.BuildServiceProvider(options));
        Assert.Contains("Unable to resolve service", exception.Message);
    }

    // Test interfaces and implementations
    public interface IService { }
    public class Service : IService { }

    public interface IDisposableService : IDisposable { }
    public class DisposableService : IDisposableService
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    public interface ICircularA { }
    public class CircularA : ICircularA
    {
        public CircularA(ICircularB b) { }
    }

    public interface ICircularB { }
    public class CircularB : ICircularB
    {
        public CircularB(ICircularA a) { }
    }

    public interface IDependency { }
    public class ServiceWithDependency : IService
    {
        public ServiceWithDependency(IDependency dependency) { }
    }
}

