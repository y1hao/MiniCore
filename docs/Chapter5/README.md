# Chapter 5: Middleware Pipeline ✅

## Overview

Phase 5 implements a minimal Middleware Pipeline to replace `Microsoft.AspNetCore.Builder`. This provides the core request/response processing pipeline that allows middleware components to be composed in a chain, processing HTTP requests and responses in order.

**Status:** ✅ Complete

## Goals

- Implement core HTTP abstractions (`HttpContext`, `HttpRequest`, `HttpResponse`)
- Provide `RequestDelegate` and `IApplicationBuilder` interfaces matching Microsoft's API surface
- Build middleware pipeline with order-preserving execution
- Implement built-in middlewares:
  - Exception handling middleware (`UseDeveloperExceptionPage`)
  - Static file serving middleware (`UseStaticFiles`)
  - Request/response logging middleware
  - Routing middleware stub (full implementation in Phase 6)
- Integrate middleware pipeline with `WebApplication`

## Key Requirements

### HTTP Abstractions

1. **HttpContext**
   - `Request` - The incoming HTTP request
   - `Response` - The outgoing HTTP response
   - `Items` - Dictionary for sharing data within request scope
   - `RequestServices` - Service provider for request-scoped services
   - `Abort()` - Abort the connection

2. **HttpRequest**
   - `Method` - HTTP method (GET, POST, etc.)
   - `Path` - Request path
   - `QueryString` - Query string
   - `Headers` - Request headers
   - `Body` - Request body stream
   - `ContentLength`, `ContentType` - Content metadata
   - `PathBase`, `Scheme`, `Host` - Request URI components

3. **HttpResponse**
   - `Headers` - Response headers
   - `Body` - Response body stream
   - `StatusCode` - HTTP status code
   - `ContentLength`, `ContentType` - Content metadata
   - `HasStarted` - Whether response has started
   - `OnStarting()`, `OnCompleted()` - Lifecycle callbacks

### Middleware Pipeline

1. **RequestDelegate**
   - Delegate: `Task Invoke(HttpContext context)`
   - Represents a middleware component in the pipeline

2. **IApplicationBuilder**
   - `Use(Func<RequestDelegate, RequestDelegate> middleware)` - Add middleware to pipeline
   - `Build()` - Build the request delegate pipeline
   - `ApplicationServices` - Service provider
   - `Properties` - Dictionary for sharing data between middleware

3. **ApplicationBuilder**
   - Implements `IApplicationBuilder`
   - Maintains ordered list of middleware components
   - Builds pipeline in reverse order (last registered = first executed)
   - Terminal middleware returns 404 if no middleware handles request

### Built-in Middlewares

1. **DeveloperExceptionPageMiddleware**
   - Catches unhandled exceptions
   - Generates HTML error page in Development environment
   - Rethrows exceptions in non-Development environments
   - Shows exception type, message, stack trace, and request path

2. **StaticFileMiddleware**
   - Serves static files from `wwwroot` directory
   - Only handles GET and HEAD requests
   - Prevents directory traversal attacks
   - Sets appropriate content types based on file extensions
   - Sets cache headers

3. **RequestLoggingMiddleware**
   - Logs HTTP requests and responses
   - Logs request method, path, status code, and elapsed time
   - Uses structured logging with `ILogger<T>`

4. **RoutingMiddleware**
   - Stub implementation for Phase 6
   - Currently just passes through to next middleware
   - Will be fully implemented in Phase 6 (Routing Framework)

## Architecture

```
MiniCore.Framework/
└── Http/
    ├── Abstractions/
    │   ├── HttpContext.cs              # Core HTTP context abstraction
    │   ├── HttpRequest.cs               # Request abstraction
    │   ├── HttpResponse.cs              # Response abstraction
    │   ├── IHeaderDictionary.cs         # Header collection interface
    │   ├── HeaderDictionary.cs          # Header collection implementation
    │   ├── StringValues.cs              # String values helper
    │   ├── HostString.cs                # Host string helper
    │   └── IApplicationBuilder.cs       # Application builder interface
    ├── DefaultHttpContext.cs            # Default HTTP context implementation
    ├── DefaultHttpRequest.cs            # Default HTTP request implementation
    ├── DefaultHttpResponse.cs           # Default HTTP response implementation
    ├── RequestDelegate.cs               # Request delegate type
    ├── ApplicationBuilder.cs            # Application builder implementation
    ├── Extensions/
    │   └── ApplicationBuilderExtensions.cs  # Extension methods for middleware
    └── Middleware/
        ├── DeveloperExceptionPageMiddleware.cs  # Exception handling middleware
        ├── StaticFileMiddleware.cs              # Static file middleware
        ├── RequestLoggingMiddleware.cs          # Logging middleware
        └── RoutingMiddleware.cs                 # Routing middleware stub
```

## Implementation Summary

Phase 5 successfully implements all core components:

### ✅ HTTP Abstractions

- **HttpContext.cs** - Abstract base class for HTTP context
- **HttpRequest.cs** - Abstract base class for HTTP request
- **HttpResponse.cs** - Abstract base class for HTTP response
- **IHeaderDictionary.cs** - Header collection interface
- **HeaderDictionary.cs** - Header collection implementation
- **StringValues.cs** - Helper struct for string values
- **HostString.cs** - Helper struct for host strings

### ✅ Default Implementations

- **DefaultHttpContext.cs** - Default HTTP context implementation
- **DefaultHttpRequest.cs** - Default HTTP request implementation
- **DefaultHttpResponse.cs** - Default HTTP response implementation

### ✅ Middleware Pipeline

- **RequestDelegate.cs** - Request delegate type
- **IApplicationBuilder.cs** - Application builder interface
- **ApplicationBuilder.cs** - Application builder implementation:
  - Maintains ordered list of middleware components
  - Builds pipeline in reverse order
  - Terminal middleware returns 404

### ✅ Built-in Middlewares

- **DeveloperExceptionPageMiddleware.cs** - Exception handling middleware
- **StaticFileMiddleware.cs** - Static file serving middleware
- **RequestLoggingMiddleware.cs** - Request/response logging middleware
- **RoutingMiddleware.cs** - Routing middleware stub

### ✅ Extension Methods

- **ApplicationBuilderExtensions.cs** - Extension methods for adding middleware:
  - `UseDeveloperExceptionPage()`
  - `UseStaticFiles()`
  - `UseRequestLogging()`
  - `UseRouting()`

### ✅ WebApplication Integration

- **WebApplication.cs** - Updated to use middleware pipeline:
  - Creates `ApplicationBuilder` instance
  - Implements `UseDeveloperExceptionPage()`, `UseStaticFiles()`, `UseRouting()`
  - Builds request delegate pipeline
  - Exposes `BuildRequestDelegate()` for Phase 7 (HTTP Server)

## Current Usage Patterns

### Basic Middleware Pipeline

```csharp
var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseStaticFiles();
app.UseRouting();

app.Run();
```

### Custom Middleware

```csharp
app.Use(next => async context =>
{
    // Before next middleware
    await next(context);
    // After next middleware
});
```

### Middleware Order

Middleware executes in the order it's registered:
1. First registered middleware is outermost (executes first)
2. Last registered middleware is innermost (executes last)
3. Terminal middleware returns 404 if no middleware handles request

## Testing Strategy

### Unit Tests

1. **ApplicationBuilder Tests**
   - Middleware is added to pipeline correctly
   - Middleware executes in correct order
   - Pipeline returns 404 if no middleware handles request
   - Middleware can modify response
   - `New()` creates new builder with shared properties

2. **DeveloperExceptionPageMiddleware Tests**
   - Catches exceptions and returns error page in Development
   - Rethrows exceptions in non-Development environments
   - Passes through when no exception occurs

3. **StaticFileMiddleware Tests**
   - Serves existing files correctly
   - Returns 404 for non-existent files
   - Ignores non-GET/HEAD requests
   - Prevents directory traversal attacks

4. **RequestLoggingMiddleware Tests**
   - Logs request and response information
   - Uses structured logging

## Integration Details

### WebApplication Integration

**Before (Phase 4):**
```csharp
public WebApplication UseDeveloperExceptionPage()
{
    throw new NotImplementedException("Middleware pipeline is not yet implemented. This will be available in Phase 5.");
}
```

**After (Phase 5):**
```csharp
public WebApplication UseDeveloperExceptionPage()
{
    _applicationBuilder.UseDeveloperExceptionPage();
    return this;
}
```

### Pipeline Building

When `WebApplication.Run()` is called:
1. `BuildRequestDelegate()` is called to build the pipeline
2. Middleware components are composed in reverse order
3. Terminal middleware returns 404 if no middleware handles request
4. Pipeline is ready for Phase 7 (HTTP Server) to invoke

## Success Criteria

- ✅ All HTTP abstractions match Microsoft's API surface
- ✅ Middleware pipeline executes in correct order
- ✅ Built-in middlewares work correctly
- ✅ WebApplication integrates with middleware pipeline
- ✅ Unit tests for middleware framework pass
- ✅ No breaking changes to application code
- ✅ Ready for Phase 6 (Routing Framework) and Phase 7 (HTTP Server)

## Known Limitations

### HTTP Server Integration

**Status:** Not yet implemented (Phase 7)

**Current Behavior:** Middleware pipeline is built but not invoked by HTTP server. `WebApplication.Run()` throws `NotImplementedException`.

**Future Enhancement:** Phase 7 will implement HTTP server that invokes the middleware pipeline for each request.

### Routing Integration

**Status:** Stub implementation (Phase 6)

**Current Behavior:** `RoutingMiddleware` just passes through to next middleware. No actual routing logic.

**Future Enhancement:** Phase 6 will implement full routing framework that integrates with middleware pipeline.

### HttpContext Implementation

**Status:** Basic implementation

**Current Behavior:** `DefaultHttpContext` provides basic HTTP context functionality. Not yet integrated with actual HTTP server.

**Future Enhancement:** Phase 7 will create `HttpContext` instances from actual HTTP requests.

### Response Streaming

**Status:** Basic implementation

**Current Behavior:** Response body is written to `MemoryStream`. No streaming support.

**Future Enhancement:** Add support for streaming responses in Phase 7.

## Key Implementation Details

### Middleware Pipeline Building

When `ApplicationBuilder.Build()` is called:

1. **Start with terminal middleware**: Returns 404 if no middleware handles request
2. **Build pipeline in reverse order**: Last registered middleware wraps terminal middleware
3. **First registered middleware wraps everything**: Creates outermost middleware
4. **Return composed delegate**: Single `RequestDelegate` that executes entire pipeline

### Middleware Execution Order

Example:
```csharp
app.Use(middleware1);
app.Use(middleware2);
app.Use(middleware3);
```

Execution order:
1. `middleware1` executes (before)
2. `middleware2` executes (before)
3. `middleware3` executes (before)
4. Terminal middleware (404)
5. `middleware3` executes (after)
6. `middleware2` executes (after)
7. `middleware1` executes (after)

### Exception Handling

`DeveloperExceptionPageMiddleware`:
- Wraps entire pipeline
- Catches exceptions from any middleware
- Generates HTML error page in Development
- Rethrows exceptions in non-Development environments

### Static File Serving

`StaticFileMiddleware`:
- Checks if request is GET or HEAD
- Resolves file path from request path
- Prevents directory traversal attacks
- Serves file if exists, otherwise passes through

## Next Steps

Phase 5 is complete. Next phases:

- **Phase 6**: Routing Framework (will integrate with middleware pipeline)
- **Phase 7**: HTTP Server (will invoke middleware pipeline for each request)

## References

- [ASP.NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/)
- [Request Delegate](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write)
- [Built-in Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/)

