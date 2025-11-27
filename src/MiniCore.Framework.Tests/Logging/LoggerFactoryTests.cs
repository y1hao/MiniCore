using MiniCore.Framework.Logging;
using MiniCore.Framework.Logging.Console;

namespace MiniCore.Framework.Tests.Logging;

public class LoggerFactoryTests
{
    [Fact]
    public void CreateLogger_WithCategoryName_ReturnsLogger()
    {
        // Arrange
        var factory = new LoggerFactory();

        // Act
        var logger = factory.CreateLogger("TestCategory");

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public void CreateLogger_WithSameCategoryName_ReturnsSameInstance()
    {
        // Arrange
        var factory = new LoggerFactory();

        // Act
        var logger1 = factory.CreateLogger("TestCategory");
        var logger2 = factory.CreateLogger("TestCategory");

        // Assert
        Assert.Same(logger1, logger2);
    }

    [Fact]
    public void CreateLogger_WithDifferentCategoryNames_ReturnsDifferentInstances()
    {
        // Arrange
        var factory = new LoggerFactory();

        // Act
        var logger1 = factory.CreateLogger("Category1");
        var logger2 = factory.CreateLogger("Category2");

        // Assert
        Assert.NotSame(logger1, logger2);
    }

    [Fact]
    public void CreateLogger_WithEmptyCategoryName_ThrowsArgumentException()
    {
        // Arrange
        var factory = new LoggerFactory();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => factory.CreateLogger(string.Empty));
    }

    [Fact]
    public void AddProvider_AddsProvider()
    {
        // Arrange
        var factory = new LoggerFactory();
        var provider = new ConsoleLoggerProvider();

        // Act
        factory.AddProvider(provider);

        // Assert
        // Provider is added, verify by creating a logger that uses it
        var logger = factory.CreateLogger("Test");
        Assert.NotNull(logger);
    }

    [Fact]
    public void AddProvider_WithNullProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var factory = new LoggerFactory();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.AddProvider(null!));
    }

    [Fact]
    public void AddProvider_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.Dispose();
        var provider = new ConsoleLoggerProvider();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => factory.AddProvider(provider));
    }

    [Fact]
    public void Dispose_DisposesProviders()
    {
        // Arrange
        var factory = new LoggerFactory();
        var provider = new ConsoleLoggerProvider();
        factory.AddProvider(provider);

        // Act
        factory.Dispose();

        // Assert
        // Multiple calls to Dispose should not throw
        factory.Dispose();
    }

    [Fact]
    public void CreateLogger_AfterDispose_StillWorks()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.AddProvider(new ConsoleLoggerProvider());
        factory.Dispose();

        // Act - CreateLogger doesn't throw after dispose, but loggers won't work properly
        var logger = factory.CreateLogger("Test");

        // Assert
        Assert.NotNull(logger);
        // Note: The logger may not function correctly after factory disposal,
        // but CreateLogger itself doesn't throw ObjectDisposedException
    }
}

