using MiniCore.Framework.Configuration.Abstractions;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Hosting;
using MiniCore.Framework.Logging;
using Xunit;

namespace MiniCore.Framework.Tests.Hosting;

public class HostBuilderTests
{
    [Fact]
    public void ConfigureServices_AddsServices()
    {
        // Arrange
        var builder = new HostBuilder();

        // Act
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ITestService, TestService>();
        });

        var host = builder.Build();

        // Assert
        var service = host.Services.GetService<ITestService>();
        Assert.NotNull(service);
        Assert.IsType<TestService>(service);
    }

    [Fact]
    public void ConfigureServices_CanBeCalledMultipleTimes()
    {
        // Arrange
        var builder = new HostBuilder();

        // Act
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ITestService, TestService>();
        });
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IAnotherService, AnotherService>();
        });

        var host = builder.Build();

        // Assert
        Assert.NotNull(host.Services.GetService<ITestService>());
        Assert.NotNull(host.Services.GetService<IAnotherService>());
    }

    [Fact]
    public void ConfigureAppConfiguration_ConfiguresConfiguration()
    {
        // Arrange
        var builder = new HostBuilder();

        // Act
        builder.ConfigureAppConfiguration(config =>
        {
            // Configuration builder is configured (even if empty, it still builds)
        });

        var host = builder.Build();

        // Assert
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        Assert.NotNull(configuration);
    }

    [Fact]
    public void ConfigureLogging_ConfiguresLogging()
    {
        // Arrange
        var builder = new HostBuilder();

        // Act
        builder.ConfigureLogging(logging =>
        {
            logging.AddConsole(LogLevel.Debug);
        });

        var host = builder.Build();

        // Assert
        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        Assert.NotNull(loggerFactory);
        var logger = loggerFactory.CreateLogger("Test");
        Assert.NotNull(logger);
    }

    [Fact]
    public void Build_RegistersConfiguration()
    {
        // Arrange
        var builder = new HostBuilder();

        // Act
        var host = builder.Build();

        // Assert
        var configuration = host.Services.GetService<IConfiguration>();
        Assert.NotNull(configuration);
        var configurationRoot = host.Services.GetService<IConfigurationRoot>();
        Assert.NotNull(configurationRoot);
        Assert.Same(configuration, configurationRoot);
    }

    [Fact]
    public void Build_RegistersHostApplicationLifetime()
    {
        // Arrange
        var builder = new HostBuilder();

        // Act
        var host = builder.Build();

        // Assert
        var lifetime = host.Services.GetService<IHostApplicationLifetime>();
        Assert.NotNull(lifetime);
    }

    [Fact]
    public void Build_CanOnlyBeCalledOnce()
    {
        // Arrange
        var builder = new HostBuilder();

        // Act
        builder.Build();

        // Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Properties_CanStoreValues()
    {
        // Arrange
        var builder = new HostBuilder();

        // Act
        builder.Properties["TestKey"] = "TestValue";

        // Assert
        Assert.Equal("TestValue", builder.Properties["TestKey"]);
    }
}

public interface ITestService { }
public class TestService : ITestService { }

public interface IAnotherService { }
public class AnotherService : IAnotherService { }

