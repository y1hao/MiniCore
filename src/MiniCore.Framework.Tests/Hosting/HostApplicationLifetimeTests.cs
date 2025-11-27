using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Hosting;
using Xunit;

namespace MiniCore.Framework.Tests.Hosting;

public class HostApplicationLifetimeTests
{
    [Fact]
    public async Task StopApplication_TriggersApplicationStopping()
    {
        // Arrange
        var builder = new HostBuilder();
        var host = builder.Build();
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        var stopping = false;

        lifetime.ApplicationStopping.Register(() => stopping = true);

        await host.StartAsync();

        // Act
        lifetime.StopApplication();

        // Assert
        Assert.True(stopping);
    }

    [Fact]
    public async Task ApplicationStarted_IsTriggeredOnStart()
    {
        // Arrange
        var builder = new HostBuilder();
        var host = builder.Build();
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        var started = false;

        lifetime.ApplicationStarted.Register(() => started = true);

        // Act
        await host.StartAsync();

        // Assert
        Assert.True(started);
    }

    [Fact]
    public async Task ApplicationStopping_IsTriggeredOnStop()
    {
        // Arrange
        var builder = new HostBuilder();
        var host = builder.Build();
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        var stopping = false;

        lifetime.ApplicationStopping.Register(() => stopping = true);

        await host.StartAsync();

        // Act
        await host.StopAsync();

        // Assert
        Assert.True(stopping);
    }

    [Fact]
    public async Task ApplicationStopped_IsTriggeredOnStop()
    {
        // Arrange
        var builder = new HostBuilder();
        var host = builder.Build();
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        var stopped = false;

        lifetime.ApplicationStopped.Register(() => stopped = true);

        await host.StartAsync();

        // Act
        await host.StopAsync();

        // Assert
        Assert.True(stopped);
    }
}

