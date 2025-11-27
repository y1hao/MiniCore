using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Hosting;
using Xunit;

namespace MiniCore.Framework.Tests.Hosting;

public class HostTests
{
    [Fact]
    public async Task StartAsync_BuildsServiceProvider()
    {
        // Arrange
        var builder = new HostBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ITestService, TestService>();
        });

        var host = builder.Build();

        // Act
        await host.StartAsync();

        // Assert
        var service = host.Services.GetService<ITestService>();
        Assert.NotNull(service);
    }

    [Fact]
    public async Task StartAsync_StartsHostedServices()
    {
        // Arrange
        var testService = new TestHostedService();
        var builder = new HostBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IHostedService>(testService);
        });

        var host = builder.Build();

        // Act
        await host.StartAsync();

        // Assert
        Assert.True(testService.Started);
        Assert.False(testService.Stopped);
    }

    [Fact]
    public async Task StartAsync_TriggersApplicationStarted()
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
    public async Task StopAsync_StopsHostedServices()
    {
        // Arrange
        var testService = new TestHostedService();
        var builder = new HostBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IHostedService>(testService);
        });

        var host = builder.Build();
        await host.StartAsync();

        // Act
        await host.StopAsync();

        // Assert
        Assert.True(testService.Stopped);
    }

    [Fact]
    public async Task StopAsync_TriggersApplicationStopping()
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
    public async Task StopAsync_TriggersApplicationStopped()
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

    [Fact]
    public async Task StartAsync_CanOnlyBeCalledOnce()
    {
        // Arrange
        var builder = new HostBuilder();
        var host = builder.Build();

        // Act
        await host.StartAsync();

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());
    }

    [Fact]
    public async Task StopAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var builder = new HostBuilder();
        var host = builder.Build();
        await host.StartAsync();

        // Act & Assert - should not throw
        await host.StopAsync();
        await host.StopAsync();
    }

    [Fact]
    public async Task Dispose_StopsHostIfNotStopped()
    {
        // Arrange
        var testService = new TestHostedService();
        var builder = new HostBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IHostedService>(testService);
        });

        var host = builder.Build();
        await host.StartAsync();

        // Act
        host.Dispose();

        // Assert
        Assert.True(testService.Stopped);
    }

    [Fact]
    public void Dispose_DisposesServiceProvider()
    {
        // Arrange
        var builder = new HostBuilder();
        var host = builder.Build();

        // Act
        host.Dispose();

        // Assert - should not throw when accessing disposed provider
        Assert.Throws<ObjectDisposedException>(() => host.Services.GetService<object>());
    }
}

public class TestHostedService : IHostedService
{
    public bool Started { get; private set; }
    public bool Stopped { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Started = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Stopped = true;
        return Task.CompletedTask;
    }
}

