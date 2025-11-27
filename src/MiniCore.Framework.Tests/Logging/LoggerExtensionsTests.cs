using System.Text;
using MiniCore.Framework.Logging;
using MiniCore.Framework.Logging.Console;

namespace MiniCore.Framework.Tests.Logging;

public class LoggerExtensionsTests
{
    [Fact]
    public void LogInformation_WithMessage_WritesToConsole()
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
            logger.LogInformation("Test message");

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains("Test message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogInformation_WithMessageTemplate_FormatsCorrectly()
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
            logger.LogInformation("Created {ShortCode} -> {OriginalUrl}", "abc123", "https://example.com");

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains("abc123", output);
            Assert.Contains("https://example.com", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogError_WithException_IncludesException()
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
            logger.LogError(exception, "Error occurred");

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
    public void LogWarning_WithMessage_WritesToConsole()
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
            logger.LogWarning("Warning message");

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains("Warning message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogDebug_WithLevelBelowMinimum_DoesNotWrite()
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
            logger.LogDebug("Debug message");

            // Assert
            var output = stringWriter.ToString();
            Assert.Empty(output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}

