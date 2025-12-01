using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MiniCore.Framework.Data;
using MiniCore.Framework.Data.Extensions;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Hosting;
using MiniCore.Framework.Logging;
using MiniCore.Framework.Logging.Console;
using MiniCore.Framework.Mvc.Views;
using MiniCore.Framework.Testing;
using MiniCore.Web.Controllers;
using MiniCore.Web.Data;
using MiniCore.Web.Services;
using MiniHostedService = MiniCore.Framework.Hosting.IHostedService;

namespace MiniCore.Web.Tests.Integration;

// Note: WebApplicationFactory<TEntryPoint> requires a type parameter from the application assembly.
// Since Program.cs uses top-level statements (no Program class), we use ShortLinkController
// as the type parameter. Any public type from MiniCore.Web would work (e.g., AppDbContext, 
// other controllers, services, etc.). The type is only used to identify the assembly.
public class ApiIntegrationTests : IDisposable
{
    private readonly WebApplicationFactory<ShortLinkController> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests()
    {
        _factory = new WebApplicationFactory<ShortLinkController>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            
            // Set ContentRootPath to the application directory so views can be found
            // Get the directory of the test assembly, then navigate to the web project
            var testAssemblyLocation = typeof(ApiIntegrationTests).Assembly.Location;
            var testProjectDir = Path.GetDirectoryName(testAssemblyLocation);
            if (testProjectDir != null)
            {
                // Navigate from MiniCore.Web.Tests/bin/Debug/net10.0 to MiniCore.Web
                var solutionRoot = Path.GetFullPath(Path.Combine(testProjectDir, "..", "..", "..", "..", ".."));
                var webProjectPath = Path.Combine(solutionRoot, "src", "MiniCore.Web");
                if (Directory.Exists(webProjectPath))
                {
                    builder.Environment.ContentRootPath = webProjectPath;
                }
            }
            
            builder.ConfigureServices(services =>
            {
                // Remove all existing DbContext registrations
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<AppDbContext>();

                // Remove background service if registered (to avoid issues in tests)
                services.RemoveAll<MiniHostedService>();

                // Add in-memory database for testing
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite(":memory:"));

                // Register ViewEngine for templating
                services.AddSingleton<IViewEngine, ViewEngine>();
            });
        }).ConfigureApplication(app =>
        {
            // Configure the HTTP request pipeline (replicate Program.cs configuration)
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseRouting();

            // Map API controllers first (attribute routing takes precedence)
            // Call without arguments to search all loaded assemblies
            app.MapControllers();

            // Map redirect endpoint as fallback - only matches if no other route matched
            app.MapFallbackToController(
                action: "RedirectToUrl",
                controller: "Redirect",
                pattern: "{*path}");
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
            context.EnsureCreated();
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

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
