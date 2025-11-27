using System.Text;
using MiniCore.Framework.Logging;
using MiniCore.Framework.Logging.Console;

namespace MiniCore.Framework.Tests.Logging;

public class ConsoleLoggerTests
{
    [Fact]
    public void IsEnabled_WithLevelBelowMinimum_ReturnsFalse()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.AddProvider(new ConsoleLoggerProvider(LogLevel.Warning));
        var logger = factory.CreateLogger("Test");

        // Act & Assert
        Assert.False(logger.IsEnabled(LogLevel.Trace));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
        Assert.False(logger.IsEnabled(LogLevel.Information));
    }

    [Fact]
    public void IsEnabled_WithLevelAtOrAboveMinimum_ReturnsTrue()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.AddProvider(new ConsoleLoggerProvider(LogLevel.Warning));
        var logger = factory.CreateLogger("Test");

        // Act & Assert
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.True(logger.IsEnabled(LogLevel.Critical));
    }

    [Fact]
    public void IsEnabled_WithNoneLevel_ReturnsFalse()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.AddProvider(new ConsoleLoggerProvider(LogLevel.Information));
        var logger = factory.CreateLogger("Test");

        // Act & Assert
        Assert.False(logger.IsEnabled(LogLevel.None));
    }

    [Fact]
    public void Log_WithEnabledLevel_WritesToConsole()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.AddProvider(new ConsoleLoggerProvider(LogLevel.Information));
        var logger = factory.CreateLogger("Test");
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            logger.Log(LogLevel.Information, 0, "Test message", null, (state, ex) => state?.ToString() ?? string.Empty);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains("Test message", output);
            Assert.Contains("Test", output); // Category name
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Log_WithDisabledLevel_DoesNotWrite()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.AddProvider(new ConsoleLoggerProvider(LogLevel.Warning));
        var logger = factory.CreateLogger("Test");
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            logger.Log(LogLevel.Information, 0, "Test message", null, (state, ex) => state?.ToString() ?? string.Empty);

            // Assert
            var output = stringWriter.ToString();
            Assert.Empty(output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Log_WithException_IncludesExceptionDetails()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.AddProvider(new ConsoleLoggerProvider(LogLevel.Error));
        var logger = factory.CreateLogger("Test");
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        var exception = new InvalidOperationException("Test exception");

        try
        {
            // Act
            logger.Log(LogLevel.Error, 0, "Error occurred", exception, (state, ex) => state?.ToString() ?? string.Empty);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains("Error occurred", output);
            Assert.Contains("InvalidOperationException", output);
            Assert.Contains("Test exception", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void BeginScope_ReturnsDisposable()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.AddProvider(new ConsoleLoggerProvider(LogLevel.Information));
        var logger = factory.CreateLogger("Test");

        // Act
        var scope = logger.BeginScope("Test scope");

        // Assert
        Assert.NotNull(scope);
        scope?.Dispose(); // Should not throw
    }
}

