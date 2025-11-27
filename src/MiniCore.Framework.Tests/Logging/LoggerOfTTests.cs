using System.Text;
using MiniCore.Framework.Logging;
using MiniCore.Framework.Logging.Console;

namespace MiniCore.Framework.Tests.Logging;

public class LoggerOfTTests
{
    [Fact]
    public void CreateLogger_WithType_ReturnsLoggerOfT()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.AddProvider(new ConsoleLoggerProvider());

        // Act
        var logger = factory.CreateLogger<LoggerOfTTests>();

        // Assert
        Assert.NotNull(logger);
        Assert.IsAssignableFrom<ILogger<LoggerOfTTests>>(logger);
    }

    [Fact]
    public void LoggerOfT_LogsWithCategoryName()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.AddProvider(new ConsoleLoggerProvider(LogLevel.Information));
        var logger = factory.CreateLogger<LoggerOfTTests>();
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
            // Category name should be the full type name
            Assert.Contains(typeof(LoggerOfTTests).FullName ?? typeof(LoggerOfTTests).Name, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}

