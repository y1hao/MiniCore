using MiniCore.Framework.DependencyInjection;

namespace MiniCore.Framework.Tests.DependencyInjection;

public class ServiceProviderTests
{
    [Fact]
    public void GetService_WhenServiceNotRegistered_ReturnsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IService));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetService_WhenServiceRegistered_ReturnsInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, Service>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IService));

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Service>(result);
    }

    [Fact]
    public void GetService_WithFactoryRegistration_UsesFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var instance = new Service();
        services.AddSingleton<IService>(_ => instance);
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IService));

        // Assert
        Assert.Same(instance, result);
    }

    [Fact]
    public void GetService_WithInstanceRegistration_ReturnsInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var instance = new Service();
        services.AddSingleton<IService>(instance);
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IService));

        // Assert
        Assert.Same(instance, result);
    }

    [Fact]
    public void GetService_WithConstructorInjection_ResolvesDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDependency, Dependency>();
        services.AddSingleton<IService, ServiceWithDependency>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IService)) as ServiceWithDependency;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Dependency);
        Assert.IsType<Dependency>(result.Dependency);
    }

    [Fact]
    public void GetService_WithMultipleDependencies_ResolvesAll()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDependency1, Dependency1>();
        services.AddSingleton<IDependency2, Dependency2>();
        services.AddSingleton<IService, ServiceWithMultipleDependencies>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IService)) as ServiceWithMultipleDependencies;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Dependency1);
        Assert.NotNull(result.Dependency2);
    }

    [Fact]
    public void GetService_WithDeepDependencyChain_ResolvesAll()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILevel1, Level1>();
        services.AddSingleton<ILevel2, Level2>();
        services.AddSingleton<ILevel3, Level3>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(ILevel1)) as Level1;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Level2);
        var level2 = result.Level2 as Level2;
        Assert.NotNull(level2);
        Assert.NotNull(level2.Level3);
    }

    [Fact]
    public void GetService_WhenDependencyMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, ServiceWithDependency>(); // Missing IDependency
        var provider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetService(typeof(IService)));
        // Should throw either "Unable to resolve service" or "No resolvable constructor found"
        Assert.True(
            exception.Message.Contains("Unable to resolve service") ||
            exception.Message.Contains("No resolvable constructor found"),
            $"Expected error message about missing dependency, but got: {exception.Message}");
    }

    [Fact]
    public void GetService_WithMultipleConstructors_ChoosesBestMatch()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDependency, Dependency>();
        services.AddSingleton<IService, ServiceWithMultipleConstructors>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IService)) as ServiceWithMultipleConstructors;

        // Assert
        Assert.NotNull(result);
        // Should use constructor with IDependency (more parameters, all resolvable)
        Assert.NotNull(result.Dependency);
    }

    [Fact]
    public void GetService_WhenNoPublicConstructor_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, ServiceWithPrivateConstructor>();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetService(typeof(IService)));
        Assert.Contains("No public constructors found", exception.Message);
    }

    [Fact]
    public void GetService_WhenNoResolvableConstructor_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, ServiceWithUnresolvableDependency>();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetService(typeof(IService)));
        Assert.Contains("No resolvable constructor found", exception.Message);
    }

    [Fact]
    public void CreateScope_ReturnsServiceScope()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
        var scope = provider.CreateScope();

        // Assert
        Assert.NotNull(scope);
        Assert.NotNull(scope.ServiceProvider);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        provider.Dispose(); // Should not throw
        Assert.Throws<ObjectDisposedException>(() => provider.GetService(typeof(IService)));
    }

    // Test interfaces and implementations
    public interface IService { }
    public class Service : IService { }

    public interface IDependency { }
    public class Dependency : IDependency { }

    public class ServiceWithDependency : IService
    {
        public IDependency Dependency { get; }
        public ServiceWithDependency(IDependency dependency)
        {
            Dependency = dependency;
        }
    }

    public interface IDependency1 { }
    public class Dependency1 : IDependency1 { }

    public interface IDependency2 { }
    public class Dependency2 : IDependency2 { }

    public class ServiceWithMultipleDependencies : IService
    {
        public IDependency1 Dependency1 { get; }
        public IDependency2 Dependency2 { get; }
        public ServiceWithMultipleDependencies(IDependency1 dependency1, IDependency2 dependency2)
        {
            Dependency1 = dependency1;
            Dependency2 = dependency2;
        }
    }

    public interface ILevel1 { }
    public class Level1 : ILevel1
    {
        public ILevel2 Level2 { get; }
        public Level1(ILevel2 level2)
        {
            Level2 = level2;
        }
    }

    public interface ILevel2 { }
    public class Level2 : ILevel2
    {
        public ILevel3 Level3 { get; }
        public Level2(ILevel3 level3)
        {
            Level3 = level3;
        }
    }

    public interface ILevel3 { }
    public class Level3 : ILevel3 { }

    public class ServiceWithMultipleConstructors : IService
    {
        public IDependency? Dependency { get; }
        public ServiceWithMultipleConstructors()
        {
        }
        public ServiceWithMultipleConstructors(IDependency dependency)
        {
            Dependency = dependency;
        }
    }

    public class ServiceWithPrivateConstructor : IService
    {
        private ServiceWithPrivateConstructor() { }
    }

    public class ServiceWithUnresolvableDependency : IService
    {
        public ServiceWithUnresolvableDependency(IDependency dependency)
        {
            // IDependency not registered
        }
    }

    public class ServiceWithEnumerableDependency : IService
    {
        public IEnumerable<IDependency> Dependencies { get; }

        public ServiceWithEnumerableDependency(IEnumerable<IDependency> dependencies)
        {
            Dependencies = dependencies;
        }
    }

    public class ServiceWithOptionalParameter : IService
    {
        public string? OptionalValue { get; }

        public ServiceWithOptionalParameter(string? optionalValue = null)
        {
            OptionalValue = optionalValue;
        }
    }

    public class ServiceWithOptionalAndRequiredParameters : IService
    {
        public IDependency Required { get; }
        public string? Optional { get; }

        public ServiceWithOptionalAndRequiredParameters(IDependency required, string? optional = "default")
        {
            Required = required;
            Optional = optional;
        }
    }

    [Fact]
    public void GetService_IEnumerable_WhenNoServicesRegistered_ReturnsEmptyCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IEnumerable<IService>));

        // Assert
        Assert.NotNull(result);
        var enumerable = Assert.IsAssignableFrom<IEnumerable<IService>>(result);
        Assert.Empty(enumerable);
    }

    [Fact]
    public void GetService_IEnumerable_WhenOneServiceRegistered_ReturnsCollectionWithOneItem()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, Service>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IEnumerable<IService>));

        // Assert
        Assert.NotNull(result);
        var enumerable = Assert.IsAssignableFrom<IEnumerable<IService>>(result);
        var items = enumerable.ToList();
        Assert.Single(items);
        Assert.IsType<Service>(items[0]);
    }

    [Fact]
    public void GetService_IEnumerable_WhenMultipleServicesRegistered_ReturnsAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, Service>();
        services.AddSingleton<IService, ServiceWithDependency>();
        services.AddSingleton<IDependency, Dependency>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IEnumerable<IService>));

        // Assert
        Assert.NotNull(result);
        var enumerable = Assert.IsAssignableFrom<IEnumerable<IService>>(result);
        var items = enumerable.ToList();
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void GetService_IEnumerable_WithDifferentLifetimes_ReturnsAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, Service>();
        services.AddTransient<IService, ServiceWithDependency>();
        services.AddSingleton<IDependency, Dependency>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IEnumerable<IService>));

        // Assert
        Assert.NotNull(result);
        var enumerable = Assert.IsAssignableFrom<IEnumerable<IService>>(result);
        var items = enumerable.ToList();
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void ConstructorInjection_WithIEnumerableParameter_WhenNoServicesRegistered_UsesEmptyCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, ServiceWithEnumerableDependency>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IService)) as ServiceWithEnumerableDependency;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Dependencies);
        Assert.Empty(result.Dependencies);
    }

    [Fact]
    public void ConstructorInjection_WithIEnumerableParameter_WhenServicesRegistered_UsesRegisteredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDependency, Dependency>();
        services.AddSingleton<IService, ServiceWithEnumerableDependency>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IService)) as ServiceWithEnumerableDependency;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Dependencies);
        var items = result.Dependencies.ToList();
        Assert.Single(items);
        Assert.IsType<Dependency>(items[0]);
    }

    [Fact]
    public void ConstructorInjection_WithIEnumerableParameter_WhenMultipleServicesRegistered_UsesAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDependency, Dependency>();
        // Register another implementation of IDependency
        services.AddSingleton<IDependency>(sp => new Dependency());
        services.AddSingleton<IService, ServiceWithEnumerableDependency>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IService)) as ServiceWithEnumerableDependency;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Dependencies);
        var items = result.Dependencies.ToList();
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void ConstructorInjection_WithOptionalParameter_WhenNotRegistered_UsesDefaultValue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IService, ServiceWithOptionalParameter>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IService)) as ServiceWithOptionalParameter;

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.OptionalValue);
    }

    [Fact]
    public void ConstructorInjection_WithOptionalParameter_WhenRegistered_UsesRegisteredService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<string>("test-value");
        services.AddSingleton<IService, ServiceWithOptionalParameter>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IService)) as ServiceWithOptionalParameter;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-value", result.OptionalValue);
    }

    [Fact]
    public void ConstructorInjection_WithOptionalAndRequiredParameters_ResolvesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDependency, Dependency>();
        services.AddSingleton<IService, ServiceWithOptionalAndRequiredParameters>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IService)) as ServiceWithOptionalAndRequiredParameters;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Required);
        Assert.Equal("default", result.Optional); // Uses default value
    }

    [Fact]
    public void ConstructorInjection_WithOptionalAndRequiredParameters_WhenOptionalRegistered_UsesRegisteredService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDependency, Dependency>();
        services.AddSingleton<string>("custom-value");
        services.AddSingleton<IService, ServiceWithOptionalAndRequiredParameters>();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetService(typeof(IService)) as ServiceWithOptionalAndRequiredParameters;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Required);
        Assert.Equal("custom-value", result.Optional); // Uses registered service
    }
}

