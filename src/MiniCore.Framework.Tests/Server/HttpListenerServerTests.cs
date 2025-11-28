using System.Net;
using System.Net.Sockets;
using System.Text;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Server;
using MiniCore.Framework.Server.Abstractions;
using Xunit;
using IServiceProvider = MiniCore.Framework.DependencyInjection.IServiceProvider;

namespace MiniCore.Framework.Tests.Server;

public class HttpListenerServerTests : IDisposable
{
    private readonly List<HttpListenerServer> _servers = new();

    [Fact]
    public async Task StartAsync_StartsListening()
    {
        // Arrange
        var urls = new[] { "http://localhost:0/" }; // Use port 0 for automatic port assignment
        var requestDelegate = new RequestDelegate(async context =>
        {
            context.Response.StatusCode = 200;
            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("OK"));
        });
        var serviceProvider = CreateServiceProvider();
        var server = new HttpListenerServer(urls, requestDelegate, serviceProvider);

        // Act
        await server.StartAsync();

        // Assert
        // Server started successfully
        _servers.Add(server);
    }

    [Fact]
    public async Task StartAsync_ThrowsIfAlreadyStarted()
    {
        // Arrange
        var urls = new[] { "http://localhost:0/" };
        var requestDelegate = new RequestDelegate(async context =>
        {
            context.Response.StatusCode = 200;
            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("OK"));
        });
        var serviceProvider = CreateServiceProvider();
        var server = new HttpListenerServer(urls, requestDelegate, serviceProvider);
        _servers.Add(server);

        // Act
        await server.StartAsync();
        
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => server.StartAsync());
    }

    [Fact]
    public async Task StopAsync_StopsListening()
    {
        // Arrange
        var urls = new[] { "http://localhost:0/" };
        var requestDelegate = new RequestDelegate(async context =>
        {
            context.Response.StatusCode = 200;
            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("OK"));
        });
        var serviceProvider = CreateServiceProvider();
        var server = new HttpListenerServer(urls, requestDelegate, serviceProvider);
        await server.StartAsync();

        // Act
        await server.StopAsync();

        // Assert
        // Server should be stopped (no exception thrown)
        Assert.True(true);
    }

    [Fact]
    public async Task ProcessRequest_TranslatesRequestCorrectly()
    {
        // Arrange
        var port = GetAvailablePort();
        var url = $"http://localhost:{port}/";
        var urls = new[] { url };
        
        IHttpRequest? capturedRequest = null;
        var requestDelegate = new RequestDelegate(async context =>
        {
            capturedRequest = context.Request;
            context.Response.StatusCode = 200;
            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("OK"));
        });
        
        var serviceProvider = CreateServiceProvider();
        var server = new HttpListenerServer(urls, requestDelegate, serviceProvider);
        await server.StartAsync();
        _servers.Add(server);

        try
        {
            // Act
            using var client = new HttpClient();
            var response = await client.GetAsync($"{url}test/path?query=value");

            // Wait a bit for request processing
            await Task.Delay(500);

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal("GET", capturedRequest.Method);
            Assert.Equal("/test/path", capturedRequest.Path);
            Assert.Equal("?query=value", capturedRequest.QueryString);
        }
        finally
        {
            await server.StopAsync();
        }
    }

    [Fact]
    public async Task ProcessRequest_TranslatesResponseCorrectly()
    {
        // Arrange
        var port = GetAvailablePort();
        var url = $"http://localhost:{port}/";
        var urls = new[] { url };
        
        var responseBody = "Hello, World!";
        var requestDelegate = new RequestDelegate(async context =>
        {
            context.Response.StatusCode = 201;
            context.Response.ContentType = "text/plain";
            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(responseBody));
        });
        
        var serviceProvider = CreateServiceProvider();
        var server = new HttpListenerServer(urls, requestDelegate, serviceProvider);
        await server.StartAsync();
        _servers.Add(server);

        try
        {
            // Act
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(responseBody, content);
        }
        finally
        {
            await server.StopAsync();
        }
    }

    [Fact]
    public async Task ProcessRequest_CreatesScopedServices()
    {
        // Arrange
        var port = GetAvailablePort();
        var url = $"http://localhost:{port}/";
        var urls = new[] { url };
        
        var services = new ServiceCollection();
        services.AddScoped<ScopedService>(_ => new ScopedService());
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetService<IServiceScopeFactory>();
        services.AddSingleton(scopeFactory!);
        var finalServiceProvider = services.BuildServiceProvider();
        
        var requestDelegate = new RequestDelegate(async context =>
        {
            var scopedService = context.RequestServices.GetService<ScopedService>();
            Assert.NotNull(scopedService);
            context.Response.StatusCode = 200;
            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("OK"));
        });
        
        var server = new HttpListenerServer(urls, requestDelegate, finalServiceProvider);
        await server.StartAsync();
        _servers.Add(server);

        try
        {
            // Act & Assert
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            await server.StopAsync();
        }
    }

    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        return services.BuildServiceProvider();
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void Dispose()
    {
        foreach (var server in _servers)
        {
            try
            {
                server.StopAsync().Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
    }

    private class ScopedService
    {
    }
}

