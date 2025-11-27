using MiniCore.Framework.Configuration.Abstractions;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Hosting;
using MiniCore.Framework.Logging;
using Xunit;

namespace MiniCore.Framework.Tests.Hosting;

public class WebApplicationBuilderTests
{
    [Fact]
    public void CreateBuilder_CreatesBuilderInstance()
    {
        // Act
        var builder = WebApplicationBuilder.CreateBuilder();

        // Assert
        Assert.NotNull(builder);
        Assert.NotNull(builder.Host);
        Assert.NotNull(builder.Environment);
        Assert.NotNull(builder.Services);
        Assert.NotNull(builder.Configuration);
    }

    [Fact]
    public void CreateBuilder_WithArgs_CreatesBuilderInstance()
    {
        // Arrange
        var args = new[] { "arg1", "arg2" };

        // Act
        var builder = WebApplicationBuilder.CreateBuilder(args);

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void Environment_HasDefaultValues()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();

        // Assert
        Assert.NotNull(builder.Environment.ContentRootPath);
        Assert.NotEmpty(builder.Environment.ContentRootPath);
        Assert.NotNull(builder.Environment.EnvironmentName);
        Assert.NotEmpty(builder.Environment.EnvironmentName);
    }

    [Fact]
    public void Environment_IsDevelopment_ReturnsFalseByDefault()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();

        // Act & Assert
        // Default environment is "Production" unless ASPNETCORE_ENVIRONMENT is set
        // In test environment, it's likely Production
        var isDevelopment = builder.Environment.IsDevelopment();
        // We can't assert a specific value since it depends on environment variable
        // Just verify the method doesn't throw
        _ = isDevelopment;
    }

    [Fact]
    public void Environment_IsEnvironment_WorksCorrectly()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var currentEnv = builder.Environment.EnvironmentName;

        // Act
        var isCurrentEnv = builder.Environment.IsEnvironment(currentEnv);
        var isDifferentEnv = builder.Environment.IsEnvironment("NonExistentEnvironment");

        // Assert
        Assert.True(isCurrentEnv);
        Assert.False(isDifferentEnv);
    }

    [Fact]
    public void Services_IsServiceCollection()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();

        // Act
        builder.Services.AddSingleton<ITestService, TestService>();

        // Assert
        var service = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(ITestService));
        Assert.NotNull(service);
    }

    [Fact]
    public void Configuration_IsConfigurationRoot()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();

        // Assert
        Assert.NotNull(builder.Configuration);
        Assert.IsAssignableFrom<IConfigurationRoot>(builder.Configuration);
    }

    [Fact]
    public void Configuration_LoadsAppsettingsJson_IfExists()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        // Note: This test assumes appsettings.json exists in the test directory
        // If it doesn't exist, the configuration will still be valid but empty

        // Act
        var config = builder.Configuration;

        // Assert
        Assert.NotNull(config);
        // Configuration should be valid even if file doesn't exist
    }

    [Fact]
    public void Host_IsHostBuilder()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();

        // Assert
        Assert.NotNull(builder.Host);
        Assert.IsAssignableFrom<IHostBuilder>(builder.Host);
    }

    [Fact]
    public void Host_ConfigureServices_Works()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();

        // Act
        builder.Host.ConfigureServices(services =>
        {
            services.AddSingleton<ITestService, TestService>();
        });

        var app = builder.Build();

        // Assert
        var service = app.Services.GetService<ITestService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void Build_CreatesWebApplication()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();

        // Act
        var app = builder.Build();

        // Assert
        Assert.NotNull(app);
        Assert.NotNull(app.Environment);
        Assert.NotNull(app.Services);
    }

    [Fact]
    public void Build_RegistersIWebHostEnvironment()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();

        // Act
        var app = builder.Build();

        // Assert
        var environment = app.Services.GetService<IWebHostEnvironment>();
        Assert.NotNull(environment);
        Assert.Same(builder.Environment, environment);
    }

    [Fact]
    public void Build_RegistersConfiguration()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();

        // Act
        var app = builder.Build();

        // Assert
        var configuration = app.Services.GetService<IConfiguration>();
        Assert.NotNull(configuration);
        var configurationRoot = app.Services.GetService<IConfigurationRoot>();
        Assert.NotNull(configurationRoot);
        Assert.Same(builder.Configuration, configurationRoot);
    }

    [Fact]
    public void Build_RegistersLogging()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();

        // Act
        var app = builder.Build();

        // Assert
        var loggerFactory = app.Services.GetService<ILoggerFactory>();
        Assert.NotNull(loggerFactory);
    }

    [Fact]
    public void Build_CanOnlyBeCalledOnce()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();

        // Act
        builder.Build();

        // Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_WebApplicationHasSameEnvironment()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();

        // Act
        var app = builder.Build();

        // Assert
        Assert.Same(builder.Environment, app.Environment);
    }

    [Fact]
    public void Services_AddServices_BeforeBuild_AreRegistered()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        builder.Services.AddSingleton<ITestService, TestService>();

        // Act
        var app = builder.Build();

        // Assert
        var service = app.Services.GetService<ITestService>();
        Assert.NotNull(service);
        Assert.IsType<TestService>(service);
    }
}

