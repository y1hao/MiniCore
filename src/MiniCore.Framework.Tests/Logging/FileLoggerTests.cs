using MiniCore.Framework.Logging;
using MiniCore.Framework.Logging.File;

namespace MiniCore.Framework.Tests.Logging;

public class FileLoggerTests : IDisposable
{
    private readonly string _testLogPath;

    public FileLoggerTests()
    {
        _testLogPath = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid()}.txt");
    }

    [Fact]
    public void IsEnabled_WithLevelBelowMinimum_ReturnsFalse()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.AddProvider(new FileLoggerProvider(_testLogPath, LogLevel.Warning));
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
        factory.AddProvider(new FileLoggerProvider(_testLogPath, LogLevel.Warning));
        var logger = factory.CreateLogger("Test");

        // Act & Assert
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.True(logger.IsEnabled(LogLevel.Critical));
    }

    [Fact]
    public void Log_WithEnabledLevel_WritesToFile()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.AddProvider(new FileLoggerProvider(_testLogPath, LogLevel.Information));
        var logger = factory.CreateLogger("Test");

        // Act
        logger.Log(LogLevel.Information, 0, "Test message", null, (state, ex) => state?.ToString() ?? string.Empty);

        // Assert
        Assert.True(File.Exists(_testLogPath));
        var content = File.ReadAllText(_testLogPath);
        Assert.Contains("Test message", content);
        Assert.Contains("Test", content); // Category name
    }

    [Fact]
    public void Log_WithDisabledLevel_DoesNotWrite()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.AddProvider(new FileLoggerProvider(_testLogPath, LogLevel.Warning));
        var logger = factory.CreateLogger("Test");

        // Act
        logger.Log(LogLevel.Information, 0, "Test message", null, (state, ex) => state?.ToString() ?? string.Empty);

        // Assert
        if (File.Exists(_testLogPath))
        {
            var content = File.ReadAllText(_testLogPath);
            Assert.DoesNotContain("Test message", content);
        }
    }

    [Fact]
    public void Log_WithException_IncludesExceptionDetails()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.AddProvider(new FileLoggerProvider(_testLogPath, LogLevel.Error));
        var logger = factory.CreateLogger("Test");
        var exception = new InvalidOperationException("Test exception");

        // Act
        logger.Log(LogLevel.Error, 0, "Error occurred", exception, (state, ex) => state?.ToString() ?? string.Empty);

        // Assert
        Assert.True(File.Exists(_testLogPath));
        var content = File.ReadAllText(_testLogPath);
        Assert.Contains("Error occurred", content);
        Assert.Contains("InvalidOperationException", content);
        Assert.Contains("Test exception", content);
    }

    [Fact]
    public void Log_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var directory = Path.Combine(Path.GetTempPath(), $"test_log_dir_{Guid.NewGuid()}");
        var logPath = Path.Combine(directory, "test.log");
        var factory = new LoggerFactory();
        factory.AddProvider(new FileLoggerProvider(logPath, LogLevel.Information));
        var logger = factory.CreateLogger("Test");

        try
        {
            // Act
            logger.Log(LogLevel.Information, 0, "Test message", null, (state, ex) => state?.ToString() ?? string.Empty);

            // Assert
            Assert.True(Directory.Exists(directory));
            Assert.True(File.Exists(logPath));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public void Log_MultipleMessages_AppendsToFile()
    {
        // Arrange
        var factory = new LoggerFactory();
        factory.AddProvider(new FileLoggerProvider(_testLogPath, LogLevel.Information));
        var logger = factory.CreateLogger("Test");

        // Act
        logger.Log(LogLevel.Information, 0, "Message 1", null, (state, ex) => state?.ToString() ?? string.Empty);
        logger.Log(LogLevel.Information, 0, "Message 2", null, (state, ex) => state?.ToString() ?? string.Empty);

        // Assert
        var content = File.ReadAllText(_testLogPath);
        Assert.Contains("Message 1", content);
        Assert.Contains("Message 2", content);
    }

    public void Dispose()
    {
        if (File.Exists(_testLogPath))
        {
            File.Delete(_testLogPath);
        }
    }
}

