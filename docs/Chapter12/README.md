# Chapter 12: Testing Framework ✅

## Overview

Phase 12 successfully implemented a minimal Testing Framework to replace `Microsoft.AspNetCore.Mvc.Testing`. This provides integration testing capabilities for MiniCore applications, enabling end-to-end testing of HTTP requests through the middleware pipeline without requiring a real HTTP server.

**Status:** ✅ Complete

## Goals

- Implement `WebApplicationFactory<TEntryPoint>` for creating test hosts
- Implement `TestServer` for in-memory HTTP request processing
- Provide `HttpClient` creation from test hosts
- Support service replacement and configuration overrides
- Enable test host builder pattern for customization
- Integrate with xUnit testing framework

## Key Requirements

### TestServer

1. **In-Memory HTTP Server**
   - Processes requests through middleware pipeline without network I/O
   - Translates `HttpRequestMessage` → `HttpContext` → `HttpResponseMessage`
   - Uses `MemoryStream` for request/response bodies
   - Supports synchronous and asynchronous request processing

2. **HttpClient Creation**
   - `CreateClient()` method returns HttpClient configured for test server
   - `CreateClient(WebApplicationFactoryClientOptions)` for custom options
   - Automatic base URL configuration
   - Support for `AllowAutoRedirect` and redirect following

### WebApplicationFactory

1. **Test Host Creation**
   - Creates `WebApplicationBuilder` instance for testing
   - Allows customization before building the application
   - Manages test host lifecycle (start on first use, dispose on fixture disposal)

2. **Configuration Support**
   - `WithWebHostBuilder(Action<WebApplicationBuilder>)` for builder configuration
   - `ConfigureTestServices(Action<IServiceCollection>)` for service replacement
   - `ConfigureApplication(Action<WebApplication>)` for middleware pipeline configuration
   - `UseEnvironment(string)` for setting test environment

3. **Service Replacement**
   - `RemoveAll<T>()` extension method for removing service registrations
   - Support for replacing services in tests
   - Maintains service registration order and lifetime semantics

## Architecture

```
MiniCore.Framework/
└── Testing/
    ├── TestServer.cs                          # In-memory HTTP server
    ├── WebApplicationFactory.cs               # Test host factory
    ├── WebApplicationFactoryClientOptions.cs  # HttpClient options
    ├── ServiceCollectionExtensions.cs          # Service removal helpers
    └── WebApplicationBuilderExtensions.cs     # Test builder extensions
```

## Implementation Summary

Phase 12 successfully provides all core testing framework components:

### ✅ TestServer

- **TestServer.cs** - In-memory HTTP server:
  - Wraps middleware pipeline for request processing
  - Translates between `HttpRequestMessage` and `HttpContext`
  - Handles request/response body streaming
  - Supports redirect following in HttpClient handler
  - Provides `CreateClient()` method for HttpClient creation

### ✅ WebApplicationFactory

- **WebApplicationFactory.cs** - Test host factory:
  - Creates `WebApplicationBuilder` for testing
  - Supports builder, services, and application configuration
  - Manages application and test server lifecycle
  - Provides `CreateClient()` methods for HTTP client creation
  - Implements `IDisposable` for proper cleanup

### ✅ Supporting Classes

- **WebApplicationFactoryClientOptions.cs** - HttpClient configuration:
  - `AllowAutoRedirect` property
  - `MaxAutomaticRedirections` property
  - `BaseAddress` and `Timeout` properties

- **ServiceCollectionExtensions.cs** - Service management:
  - `RemoveAll<T>()` extension method
  - `RemoveAll(Type)` extension method

- **WebApplicationBuilderExtensions.cs** - Test builder extensions:
  - `UseEnvironment(string)` method
  - `ConfigureServices(Action<IServiceCollection>)` method
  - `ConfigureAppConfiguration(Action<IConfigurationBuilder>)` method

## Current Usage Patterns

### Basic Integration Test

```csharp
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
                // Replace DbContext with in-memory database
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<AppDbContext>();
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite(":memory:"));
            });
        }).ConfigureApplication(app =>
        {
            // Configure middleware pipeline
            app.UseStaticFiles();
            app.UseRouting();
            app.MapControllers();
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetLinks_ReturnsEmptyList_WhenNoLinksExist()
    {
        var response = await _client.GetAsync("/api/links");
        response.EnsureSuccessStatusCode();
        var links = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.Empty(links);
    }
}
```

### Service Replacement

```csharp
_factory = factory.WithWebHostBuilder(builder =>
{
    builder.ConfigureServices(services =>
    {
        // Remove existing registrations
        services.RemoveAll<IMyService>();
        
        // Add test implementation
        services.AddSingleton<IMyService, TestMyService>();
    });
});
```

### Environment Configuration

```csharp
_factory = factory.WithWebHostBuilder(builder =>
{
    builder.UseEnvironment("Testing");
    builder.ConfigureAppConfiguration(config =>
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MySetting"] = "TestValue"
        });
    });
});
```

## Testing Strategy

### Unit Tests

1. **TestServer Tests**
   - Request/response translation works correctly
   - Middleware pipeline execution
   - Redirect handling
   - Error handling

2. **WebApplicationFactory Tests**
   - Application creation
   - Service replacement
   - Configuration overrides
   - Lifecycle management

### Integration Tests

1. **End-to-End Tests**
   - Full HTTP request/response cycle
   - Controller action execution
   - Model binding
   - Database operations
   - Middleware pipeline execution

## Success Criteria

- ✅ `TestServer` implementation complete
- ✅ `WebApplicationFactory<TEntryPoint>` implementation complete
- ✅ `HttpClient` creation from test host
- ✅ Service replacement utilities (`RemoveAll`, `ConfigureTestServices`)
- ✅ Test host builder pattern (`WithWebHostBuilder`, `UseEnvironment`)
- ✅ Application configuration support (`ConfigureApplication`)
- ✅ Redirect handling in HttpClient
- ✅ xUnit integration (`IClassFixture` support)
- ✅ Proper resource cleanup and disposal

## Known Limitations

### Program.cs Entry Point

**Status:** Manual configuration required

**Current Behavior:** Since `Program.cs` uses top-level statements, `WebApplicationFactory` cannot automatically invoke the Program configuration. Tests must manually configure the application using `WithWebHostBuilder` and `ConfigureApplication`.

**Workaround:** Tests replicate the Program.cs configuration in their setup:

```csharp
.ConfigureApplication(app =>
{
    // Replicate Program.cs middleware configuration
    app.UseStaticFiles();
    app.UseRouting();
    app.MapControllers();
    app.MapFallbackToController(...);
})
```

**Future Enhancement:** Consider extracting Program.cs configuration into a method that can be called from both Program.cs and tests.

### In-Memory Database Support

**Status:** Basic support

**Current Behavior:** Tests can use `:memory:` SQLite connection string for in-memory databases, but there's no specialized helper for database testing.

**Future Enhancement:** Add `UseInMemoryDatabase(string)` extension method and automatic database cleanup between tests.

### Test Output Logging

**Status:** Not implemented

**Current Behavior:** Test output is not automatically captured or logged.

**Future Enhancement:** Add test output logging integration for better debugging.

## Key Implementation Details

### TestServer Architecture

TestServer processes requests by:

1. **Request Translation:**
   - Creates `HttpContext` from `HttpRequestMessage`
   - Copies headers, method, path, query string
   - Streams request body to `HttpContext.Request.Body`

2. **Pipeline Execution:**
   - Invokes `RequestDelegate` (middleware pipeline)
   - Processes request through all middleware components
   - Executes controller actions and routing

3. **Response Translation:**
   - Reads response from `HttpContext.Response`
   - Copies status code, headers, body
   - Creates `HttpResponseMessage` with response content

### WebApplicationFactory Lifecycle

1. **Lazy Initialization:**
   - Application created on first access to `Application` or `Server` property
   - Test server created on first `CreateClient()` call

2. **Configuration Order:**
   - Builder configuration (`WithWebHostBuilder`)
   - Service configuration (`ConfigureTestServices`)
   - Application build
   - Application configuration (`ConfigureApplication`)

3. **Disposal:**
   - Test server disposed first
   - Application disposed second
   - Resources cleaned up properly

### Service Replacement Pattern

Services are replaced by:

1. **Removing Existing Registrations:**
   ```csharp
   services.RemoveAll<MyService>();
   ```

2. **Adding Test Implementation:**
   ```csharp
   services.AddSingleton<MyService, TestMyService>();
   ```

3. **Maintaining Order:**
   - Services removed in reverse order
   - New services added after removal
   - Lifetime semantics preserved

## Migration from Microsoft.AspNetCore.Mvc.Testing

The following patterns were migrated:

| Microsoft Pattern | MiniCore Pattern |
|-------------------|-----------------|
| `WebApplicationFactory<TEntryPoint>` | `WebApplicationFactory<TEntryPoint>` (same API) |
| `TestServer` | `TestServer` (same API) |
| `WebApplicationFactoryClientOptions` | `WebApplicationFactoryClientOptions` (same API) |
| `WithWebHostBuilder()` | `WithWebHostBuilder()` (same API) |
| `ConfigureTestServices()` | `ConfigureTestServices()` (same API) |
| `services.RemoveAll<T>()` | `services.RemoveAll<T>()` (same API) |
| `builder.UseEnvironment()` | `builder.UseEnvironment()` (same API) |

## Next Steps

Phase 12 is complete. The testing framework enables comprehensive integration testing of MiniCore applications.

## References

- [Microsoft.AspNetCore.Mvc.Testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [WebApplicationFactory Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1)
- [TestServer Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.testserver)


