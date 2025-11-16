using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniCore.Web;
using MiniCore.Web.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MiniCore.Web.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove all existing DbContext registrations
                var descriptorsToRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(AppDbContext) ||
                    (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))).ToList();
                
                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Remove background service if registered (to avoid issues in tests)
                var hostedServices = services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
                foreach (var service in hostedServices)
                {
                    services.Remove(service);
                }

                // Add in-memory database for testing
                var dbName = "TestDb_" + Guid.NewGuid().ToString();
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });
            });
        });

        // Create client that doesn't follow redirects automatically
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        
        // Ensure database is created
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Database.EnsureCreated();
        }
    }

    [Fact]
    public async Task GetLinks_ReturnsEmptyList_WhenNoLinksExist()
    {
        // Act
        var response = await _client.GetAsync("/api/links");

        // Assert
        response.EnsureSuccessStatusCode();
        var links = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(links);
        Assert.True(links != null && links.Count == 0);
    }

    [Fact]
    public async Task CreateLink_WithValidUrl_ReturnsCreatedLink()
    {
        // Arrange
        var request = new
        {
            OriginalUrl = "https://example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/links", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var link = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(link.TryGetProperty("shortCode", out var shortCode));
        Assert.True(link.TryGetProperty("originalUrl", out var originalUrl));
        Assert.Equal("https://example.com", originalUrl.GetString());
        var shortCodeValue = shortCode.GetString();
        Assert.NotNull(shortCodeValue);
        Assert.NotEmpty(shortCodeValue);
    }

    [Fact]
    public async Task CreateLink_WithInvalidUrl_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            OriginalUrl = "not-a-valid-url"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/links", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        // Verify we can parse the error response
        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType?.Contains("json") == true)
        {
            var error = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(error.TryGetProperty("error", out _) || error.TryGetProperty("title", out _));
        }
    }

    [Fact]
    public async Task CreateLink_WithCustomShortCode_ReturnsCreatedLink()
    {
        // Arrange
        var request = new
        {
            OriginalUrl = "https://example.com",
            ShortCode = "my-custom-link"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/links", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var link = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(link.TryGetProperty("shortCode", out var shortCode));
        Assert.Equal("my-custom-link", shortCode.GetString());
    }

    [Fact]
    public async Task CreateLink_WithDuplicateCustomShortCode_ReturnsConflict()
    {
        // Arrange - Create first link
        var createRequest = new { OriginalUrl = "https://example.com", ShortCode = "duplicate-code" };
        var createResponse = await _client.PostAsJsonAsync("/api/links", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // Act - Try to create another with same short code
        var duplicateRequest = new { OriginalUrl = "https://another.com", ShortCode = "duplicate-code" };
        var duplicateResponse = await _client.PostAsJsonAsync("/api/links", duplicateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteLink_WithValidId_ReturnsNoContent()
    {
        // Arrange - Create a link first
        var createRequest = new { OriginalUrl = "https://example.com" };
        var createResponse = await _client.PostAsJsonAsync("/api/links", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdLink = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var linkId = createdLink.GetProperty("id").GetInt32();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/links/{linkId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify it's deleted - handle empty response gracefully
        var getResponse = await _client.GetAsync("/api/links");
        if (getResponse.IsSuccessStatusCode)
        {
            var links = await getResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
            Assert.NotNull(links);
            Assert.DoesNotContain(links!, l => l.GetProperty("id").GetInt32() == linkId);
        }
    }

    [Fact]
    public async Task DeleteLink_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/links/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Redirect_WithValidShortCode_ReturnsRedirect()
    {
        // Arrange - Create a link
        var createRequest = new { OriginalUrl = "https://example.com" };
        var createResponse = await _client.PostAsJsonAsync("/api/links", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdLink = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var shortCode = createdLink.GetProperty("shortCode").GetString();
        Assert.NotNull(shortCode);

        // Act - Don't follow redirects, and ensure we're not following redirects automatically
        var request = new HttpRequestMessage(HttpMethod.Get, $"/{shortCode}");
        var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // Assert - Check what status we actually got
        var statusCode = response.StatusCode;
        var location = response.Headers.Location?.ToString();
        
        // Debug output
        if (statusCode != HttpStatusCode.Redirect && 
            statusCode != HttpStatusCode.MovedPermanently && 
            statusCode != HttpStatusCode.Found)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected redirect but got {statusCode} for shortCode '{shortCode}'. Location header: {location}. Content: {content.Substring(0, Math.Min(200, content.Length))}");
        }
        
        Assert.NotNull(location);
        // Normalize URL (remove trailing slash if present) for comparison
        var normalizedLocation = location.TrimEnd('/');
        Assert.Equal("https://example.com", normalizedLocation);
    }

    [Fact]
    public async Task Redirect_WithInvalidShortCode_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/nonexistentcode");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminPage_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/admin");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("URL Shortener Admin", content);
    }

    [Fact]
    public async Task CreateLink_WithExpirationDate_StoresExpiration()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var request = new
        {
            OriginalUrl = "https://example.com",
            ExpiresAt = expiresAt
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/links", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var link = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(link.TryGetProperty("expiresAt", out var expiresAtProp));
        Assert.NotNull(expiresAtProp.GetString());
    }
}

