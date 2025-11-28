# Chapter 7: HTTP Server (HttpListener Backend) ✅

## Overview

Phase 7 implements a minimal HTTP Server using `HttpListener` to replace Kestrel. This provides the final piece that connects incoming HTTP requests to the middleware pipeline and routing framework.

**Status:** ✅ Complete

## Goals

- Implement `IServer` interface
- Wrap `HttpListener` for HTTP/1.1 support
- Translate incoming `HttpListenerRequest` to `HttpContext`/`HttpRequest`
- Translate `HttpResponse` to `HttpListenerResponse`
- Invoke middleware pipeline for each request
- Support scoped service creation per request
- Integrate with `WebApplication.Run()` for application startup

## Key Requirements

### Server Interface

1. **IServer Interface**
   - `StartAsync()` - Start listening for incoming requests
   - `StopAsync()` - Stop listening and shutdown gracefully

2. **HttpListenerServer Implementation**
   - Wraps `System.Net.HttpListener`
   - Accepts URLs to listen on (from configuration or environment)
   - Processes requests asynchronously
   - Creates request scopes for scoped services

### Request/Response Translation

1. **Request Translation**
   - Map `HttpListenerRequest` to `IHttpRequest`
   - Extract HTTP method, path, query string, headers, body
   - Set scheme, host, path base

2. **Response Translation**
   - Map `IHttpResponse` to `HttpListenerResponse`
   - Set status code, headers, content type, content length
   - Copy response body to output stream

### Integration

1. **WebApplication Integration**
   - `Run()` method starts the server
   - `RunAsync()` method for async startup
   - Reads URLs from configuration (`Urls` key or `ASPNETCORE_URLS` environment variable)
   - Defaults to `http://localhost:5000` and `https://localhost:5001` if not configured

2. **Service Scope Management**
   - Creates a new service scope for each request
   - Uses `IServiceScopeFactory` if available
   - Falls back to root service provider if no scope factory

## Architecture

```
MiniCore.Framework/
└── Server/
    ├── Abstractions/
    │   └── IServer.cs                    # Server interface
    └── HttpListenerServer.cs              # HttpListener-based server implementation
```

## Implementation Summary

Phase 7 successfully implements all core server components:

### ✅ IServer Interface

- **IServer.cs** - Interface for HTTP server implementations
  - `StartAsync()` - Start the server
  - `StopAsync()` - Stop the server gracefully

### ✅ HttpListenerServer Implementation

- **HttpListenerServer.cs** - Implementation that:
  - Wraps `System.Net.HttpListener`
  - Accepts multiple URLs to listen on
  - Processes requests asynchronously (non-blocking)
  - Creates service scopes per request
  - Translates requests/responses between HttpListener and HttpContext
  - Handles errors gracefully (sends 500 on unhandled exceptions)
  - Logs server lifecycle events

### ✅ Request Translation

- **TranslateRequest()** - Converts `HttpListenerRequest` to `IHttpRequest`:
  - HTTP method (GET, POST, etc.)
  - Path and query string
  - Headers (all headers copied)
  - Request body stream
  - Scheme (http/https)
  - Host information

### ✅ Response Translation

- **TranslateResponseAsync()** - Converts `IHttpResponse` to `HttpListenerResponse`:
  - Status code
  - Headers (skips Content-Length and Transfer-Encoding as HttpListener handles these)
  - Content type
  - Content length
  - Response body stream

### ✅ WebApplication Integration

- **WebApplication.Run()** - Updated to:
  - Build request delegate pipeline
  - Get URLs from configuration
  - Create and start HttpListenerServer
  - Start the host (background services)
  - Wait for shutdown signal
  - Stop server and host gracefully

- **WebApplication.RunAsync()** - Async version of Run()

- **GetUrls()** - Reads URLs from:
  1. Configuration key `Urls`
  2. Environment variable `ASPNETCORE_URLS`
  3. Defaults to `http://localhost:5000` and `https://localhost:5001`

## Current Usage Patterns

### Basic Server Usage

```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();

// Starts the server and blocks until shutdown
app.Run();
```

### Configuration URLs

```json
// appsettings.json
{
  "Urls": "http://localhost:5000;https://localhost:5001"
}
```

Or via environment variable:
```bash
export ASPNETCORE_URLS="http://localhost:5000;https://localhost:5001"
```

### Async Startup

```csharp
var app = builder.Build();

// Configure middleware...

// Async startup (non-blocking)
await app.RunAsync();
```

## Testing Strategy

### Unit Tests

1. **HttpListenerServerTests**
   - Server start/stop lifecycle
   - Request/response translation
   - Scoped service creation per request
   - Error handling

### Integration Tests

- Full request/response cycle through middleware pipeline
- Routing integration
- Static file serving
- Exception handling

## Success Criteria

- ✅ `IServer` interface implemented
- ✅ `HttpListenerServer` wraps `HttpListener` correctly
- ✅ Request translation works for all HTTP methods
- ✅ Response translation works correctly
- ✅ Middleware pipeline invoked for each request
- ✅ Service scopes created per request
- ✅ `WebApplication.Run()` starts server and blocks until shutdown
- ✅ URLs configurable via configuration or environment variable
- ✅ Graceful shutdown works correctly
- ✅ Unit tests pass
- ✅ No breaking changes to application code
- ✅ Ready for Phase 8 (Mini ORM)

## Known Limitations

### HTTPS Support

**Status:** Limited support

**Current Behavior:** HttpListener can listen on HTTPS URLs, but certificate configuration must be done outside the framework (via Windows certificate store or `netsh http add sslcert`).

**Future Enhancement:** Add certificate configuration API similar to Kestrel's `UseHttps()`.

### HTTP/2 Support

**Status:** Not supported

**Current Behavior:** HttpListener only supports HTTP/1.1.

**Future Enhancement:** HTTP/2 support would require a different server implementation (e.g., using `System.Net.Http.HttpClient` or a custom implementation).

### Performance

**Status:** Basic implementation

**Current Behavior:** HttpListener is single-threaded for accepting connections but processes requests on thread pool threads.

**Future Enhancement:** For better performance, consider:
- Connection pooling
- Async I/O using `System.IO.Pipelines`
- Request queuing and throttling

### Request Body Streaming

**Status:** Basic support

**Current Behavior:** Request body is read from `HttpListenerRequest.InputStream` which is a synchronous stream.

**Future Enhancement:** Consider async streaming for large request bodies.

## Key Implementation Details

### Request Processing Flow

1. **Accept Connection**: `HttpListener.GetContextAsync()` waits for incoming request
2. **Create Scope**: Create new service scope for this request (if `IServiceScopeFactory` available)
3. **Create HttpContext**: Instantiate `HttpContext` with `HttpRequest` and `HttpResponse`
4. **Translate Request**: Copy data from `HttpListenerRequest` to `IHttpRequest`
5. **Invoke Pipeline**: Call `RequestDelegate` (middleware pipeline)
6. **Translate Response**: Copy data from `IHttpResponse` to `HttpListenerResponse`
7. **Send Response**: Write response body and close connection
8. **Dispose Scope**: Dispose service scope (disposes scoped services)

### Error Handling

- Unhandled exceptions in request processing are caught
- 500 Internal Server Error response sent to client
- Exception is logged (if logger available)
- Response stream errors are ignored (connection may be closed)

### Service Scope Management

- Each request gets its own service scope
- Scoped services (like DbContext) are isolated per request
- Scope is disposed after request completes (even on error)
- Falls back to root service provider if no scope factory available

### URL Configuration

URLs are resolved in order:
1. Configuration key `Urls` (semicolon-separated)
2. Environment variable `ASPNETCORE_URLS` (semicolon-separated)
3. Default: `http://localhost:5000` and `https://localhost:5001`

## Next Steps

Phase 7 is complete. Next phases:

- **Phase 8**: Mini ORM / Data Integration (replace EF Core)
- **Phase 9**: Frontend Templating (replace Razor)
- **Phase 10**: Background Services (already implemented in Phase 4)

## References

- [HttpListener Class](https://learn.microsoft.com/en-us/dotnet/api/system.net.httplistener)
- [ASP.NET Core Hosting](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/web-host)
- [Kestrel Web Server](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel)

