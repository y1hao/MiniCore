using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Logging;

namespace MiniCore.Framework.Tests.Logging;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLogging_RegistersLoggerFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLogging();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetService<ILoggerFactory>();
        Assert.NotNull(factory);
    }

    [Fact]
    public void AddLogging_RegistersGenericLogger()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLogging();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetService<ILogger<ServiceCollectionExtensionsTests>>();
        Assert.NotNull(logger);
    }

    [Fact]
    public void AddLogging_WithConfiguration_AddsProviders()
    {
        // Arrange
        var services = new ServiceCollection();
        var logPath = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid()}.txt");

        try
        {
            // Act
            services.AddLogging(builder =>
            {
                builder.AddConsole(LogLevel.Information);
                builder.AddFile(logPath, LogLevel.Warning);
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("Test");
            
            // Logger should work
            Assert.NotNull(logger);
            Assert.True(logger.IsEnabled(LogLevel.Information));
        }
        finally
        {
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }
        }
    }

    [Fact]
    public void AddLogging_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            MiniCore.Framework.Logging.ServiceCollectionExtensions.AddLogging(null!));
    }
}

