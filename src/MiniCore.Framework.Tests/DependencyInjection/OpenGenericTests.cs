using MiniCore.Framework.DependencyInjection;

namespace MiniCore.Framework.Tests.DependencyInjection;

public class OpenGenericTests
{
    [Fact]
    public void ResolveOpenGeneric_WithSingletonLifetime_ReturnsInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        var provider = services.BuildServiceProvider();

        // Act
        var logger = provider.GetService<ILogger<MyService>>();

        // Assert
        Assert.NotNull(logger);
        Assert.IsType<Logger<MyService>>(logger);
    }

    [Fact]
    public void ResolveOpenGeneric_WithScopedLifetime_ReturnsInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped(typeof(ILogger<>), typeof(Logger<>));
        var provider = services.BuildServiceProvider();

        // Act
        using var scope = provider.CreateScope();
        var logger = scope.ServiceProvider.GetService<ILogger<MyService>>();

        // Assert
        Assert.NotNull(logger);
        Assert.IsType<Logger<MyService>>(logger);
    }

    [Fact]
    public void ResolveOpenGeneric_WithTransientLifetime_ReturnsInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient(typeof(ILogger<>), typeof(Logger<>));
        var provider = services.BuildServiceProvider();

        // Act
        var logger = provider.GetService<ILogger<MyService>>();

        // Assert
        Assert.NotNull(logger);
        Assert.IsType<Logger<MyService>>(logger);
    }

    [Fact]
    public void ResolveOpenGeneric_Singleton_ReturnsSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        var provider = services.BuildServiceProvider();

        // Act
        var logger1 = provider.GetService<ILogger<MyService>>();
        var logger2 = provider.GetService<ILogger<MyService>>();

        // Assert
        Assert.NotNull(logger1);
        Assert.NotNull(logger2);
        Assert.Same(logger1, logger2);
    }

    [Fact]
    public void ResolveOpenGeneric_Singleton_DifferentClosedGenerics_ReturnsDifferentInstances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        var provider = services.BuildServiceProvider();

        // Act
        var logger1 = provider.GetService<ILogger<MyService>>();
        var logger2 = provider.GetService<ILogger<OtherService>>();

        // Assert
        Assert.NotNull(logger1);
        Assert.NotNull(logger2);
        Assert.NotSame(logger1, logger2);
        Assert.IsType<Logger<MyService>>(logger1);
        Assert.IsType<Logger<OtherService>>(logger2);
    }

    [Fact]
    public void ResolveOpenGeneric_Scoped_ReturnsSameInstanceWithinScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped(typeof(ILogger<>), typeof(Logger<>));
        var provider = services.BuildServiceProvider();

        // Act
        using var scope = provider.CreateScope();
        var logger1 = scope.ServiceProvider.GetService<ILogger<MyService>>();
        var logger2 = scope.ServiceProvider.GetService<ILogger<MyService>>();

        // Assert
        Assert.NotNull(logger1);
        Assert.NotNull(logger2);
        Assert.Same(logger1, logger2);
    }

    [Fact]
    public void ResolveOpenGeneric_Scoped_DifferentScopes_ReturnsDifferentInstances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped(typeof(ILogger<>), typeof(Logger<>));
        var provider = services.BuildServiceProvider();

        // Act
        using var scope1 = provider.CreateScope();
        var logger1 = scope1.ServiceProvider.GetService<ILogger<MyService>>();
        using var scope2 = provider.CreateScope();
        var logger2 = scope2.ServiceProvider.GetService<ILogger<MyService>>();

        // Assert
        Assert.NotNull(logger1);
        Assert.NotNull(logger2);
        Assert.NotSame(logger1, logger2);
    }

    [Fact]
    public void ResolveOpenGeneric_Transient_ReturnsDifferentInstances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient(typeof(ILogger<>), typeof(Logger<>));
        var provider = services.BuildServiceProvider();

        // Act
        var logger1 = provider.GetService<ILogger<MyService>>();
        var logger2 = provider.GetService<ILogger<MyService>>();

        // Assert
        Assert.NotNull(logger1);
        Assert.NotNull(logger2);
        Assert.NotSame(logger1, logger2);
    }

    [Fact]
    public void ResolveOpenGeneric_WithDependencies_ResolvesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration, Configuration>();
        services.AddSingleton(typeof(ILogger<>), typeof(LoggerWithDependency<>));
        var provider = services.BuildServiceProvider();

        // Act
        var logger = provider.GetService<ILogger<MyService>>() as LoggerWithDependency<MyService>;

        // Assert
        Assert.NotNull(logger);
        Assert.NotNull(logger.Configuration);
    }

    [Fact]
    public void ResolveOpenGeneric_NotRegistered_ReturnsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
        var logger = provider.GetService<ILogger<MyService>>();

        // Assert
        Assert.Null(logger);
    }

    [Fact]
    public void ResolveOpenGeneric_WithMultipleTypeParameters_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(typeof(IRepository<,>), typeof(Repository<,>));
        var provider = services.BuildServiceProvider();

        // Act
        var repository = provider.GetService<IRepository<string, int>>();

        // Assert
        Assert.NotNull(repository);
        Assert.IsType<Repository<string, int>>(repository);
    }

    [Fact]
    public void ResolveOpenGeneric_ExactMatchTakesPrecedence()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton(typeof(ILogger<MyService>), typeof(CustomLogger));
        var provider = services.BuildServiceProvider();

        // Act
        var logger = provider.GetService<ILogger<MyService>>();

        // Assert
        Assert.NotNull(logger);
        Assert.IsType<CustomLogger>(logger);
    }


    // Test interfaces and implementations
    public interface ILogger<T> { }
    public class Logger<T> : ILogger<T> { }

    public class MyService { }
    public class OtherService { }

    public interface IConfiguration { }
    public class Configuration : IConfiguration { }

    public class LoggerWithDependency<T> : ILogger<T>
    {
        public IConfiguration Configuration { get; }
        public LoggerWithDependency(IConfiguration configuration)
        {
            Configuration = configuration;
        }
    }

    public class CustomLogger : ILogger<MyService> { }

    public interface IRepository<TKey, TValue> { }
    public class Repository<TKey, TValue> : IRepository<TKey, TValue> { }
}

