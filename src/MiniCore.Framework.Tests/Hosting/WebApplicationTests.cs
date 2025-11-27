using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Hosting;
using Xunit;
using IServiceProvider = MiniCore.Framework.DependencyInjection.IServiceProvider;

namespace MiniCore.Framework.Tests.Hosting;

public class WebApplicationTests
{
    [Fact]
    public void Environment_ReturnsWebHostEnvironment()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Assert
        Assert.NotNull(app.Environment);
        Assert.IsAssignableFrom<IWebHostEnvironment>(app.Environment);
    }

    [Fact]
    public void Services_ReturnsServiceProvider()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Assert
        Assert.NotNull(app.Services);
        Assert.IsAssignableFrom<IServiceProvider>(app.Services);
    }

    [Fact]
    public void Services_CanResolveRegisteredServices()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        builder.Services.AddSingleton<ITestService, TestService>();
        var app = builder.Build();

        // Act
        var service = app.Services.GetService<ITestService>();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<TestService>(service);
    }

    [Fact(Skip = "Middleware pipeline is not yet implemented (Phase 5)")]
    public void UseDeveloperExceptionPage_AddsMiddleware()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Act
        var result = app.UseDeveloperExceptionPage();

        // Assert
        Assert.Same(app, result);
    }

    [Fact]
    public void UseDeveloperExceptionPage_ThrowsNotImplementedException()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Act & Assert
        var exception = Assert.Throws<NotImplementedException>(() => app.UseDeveloperExceptionPage());
        Assert.Contains("Phase 5", exception.Message);
    }

    [Fact(Skip = "Middleware pipeline is not yet implemented (Phase 5)")]
    public void UseStaticFiles_AddsMiddleware()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Act
        var result = app.UseStaticFiles();

        // Assert
        Assert.Same(app, result);
    }

    [Fact]
    public void UseStaticFiles_ThrowsNotImplementedException()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Act & Assert
        var exception = Assert.Throws<NotImplementedException>(() => app.UseStaticFiles());
        Assert.Contains("Phase 5", exception.Message);
    }

    [Fact(Skip = "Middleware pipeline is not yet implemented (Phase 5)")]
    public void UseRouting_AddsMiddleware()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Act
        var result = app.UseRouting();

        // Assert
        Assert.Same(app, result);
    }

    [Fact]
    public void UseRouting_ThrowsNotImplementedException()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Act & Assert
        var exception = Assert.Throws<NotImplementedException>(() => app.UseRouting());
        Assert.Contains("Phase 5", exception.Message);
    }

    [Fact(Skip = "Routing framework is not yet implemented (Phase 6)")]
    public void MapControllers_MapsControllerEndpoints()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Act
        var result = app.MapControllers();

        // Assert
        Assert.Same(app, result);
    }

    [Fact]
    public void MapControllers_ThrowsNotImplementedException()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Act & Assert
        var exception = Assert.Throws<NotImplementedException>(() => app.MapControllers());
        Assert.Contains("Phase 6", exception.Message);
    }

    [Fact(Skip = "Routing framework is not yet implemented (Phase 6)")]
    public void MapRazorPages_MapsRazorPageEndpoints()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Act
        var result = app.MapRazorPages();

        // Assert
        Assert.Same(app, result);
    }

    [Fact]
    public void MapRazorPages_ThrowsNotImplementedException()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Act & Assert
        var exception = Assert.Throws<NotImplementedException>(() => app.MapRazorPages());
        Assert.Contains("Phase 6", exception.Message);
    }

    [Fact(Skip = "Routing framework is not yet implemented (Phase 6)")]
    public void MapFallbackToController_MapsFallbackRoute()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Act
        var result = app.MapFallbackToController("Action", "Controller", "pattern");

        // Assert
        Assert.Same(app, result);
    }

    [Fact]
    public void MapFallbackToController_ThrowsNotImplementedException()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Act & Assert
        var exception = Assert.Throws<NotImplementedException>(() => 
            app.MapFallbackToController("Action", "Controller", "pattern"));
        Assert.Contains("Phase 6", exception.Message);
    }

    [Fact(Skip = "HTTP server is not yet implemented (Phase 7)")]
    public void Run_StartsApplication()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Act & Assert
        // This would block, so we can't actually test it without async/timeout
        // For now, we'll skip this test
    }

    [Fact]
    public void Run_ThrowsNotImplementedException()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var app = builder.Build();

        // Act & Assert
        var exception = Assert.Throws<NotImplementedException>(() => app.Run());
        Assert.Contains("Phase 7", exception.Message);
    }

    [Fact]
    public void Run_StartsHostBeforeThrowing()
    {
        // Arrange
        var builder = WebApplicationBuilder.CreateBuilder();
        var testService = new TestHostedService();
        builder.Services.AddSingleton<IHostedService>(testService);
        var app = builder.Build();

        // Act
        try
        {
            app.Run();
        }
        catch (NotImplementedException)
        {
            // Expected
        }

        // Assert
        // The host should have been started before the exception is thrown
        Assert.True(testService.Started);
    }
}

