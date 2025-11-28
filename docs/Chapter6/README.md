# Chapter 6: Routing Framework ✅

## Overview

Phase 6 implements a minimal Routing Framework to replace `Microsoft.AspNetCore.Routing`. This provides the core route matching and parameter extraction functionality that transforms the middleware pipeline into application endpoints.

**Status:** ✅ Complete

## Goals

- Implement route pattern matching with parameter extraction
- Provide route registration API (`Map`, `MapGet`, `MapPost`, etc.)
- Support route patterns with parameters (e.g., `/api/links/{id}`)
- Support catch-all patterns (e.g., `{*path}`)
- Integrate routing with middleware pipeline
- Implement `MapControllers()`, `MapRazorPages()`, and `MapFallbackToController()` methods
- Extract route parameters and make them available in `HttpContext`

## Key Requirements

### Route Matching

1. **Route Pattern Matching**
   - Match routes by HTTP method and path pattern
   - Extract path parameters from route patterns
   - Support parameter placeholders: `{paramName}`
   - Support catch-all patterns: `{*path}`
   - Case-insensitive matching

2. **Route Registry**
   - Register routes with HTTP method, pattern, and handler
   - Match incoming requests against registered routes
   - Support fallback routes that match when no other route matches
   - Return route data containing extracted parameters

3. **Route Data**
   - Store extracted route parameters
   - Make route data available in `HttpContext`
   - Support accessing route values by name

### Integration

1. **Middleware Integration**
   - `RoutingMiddleware` uses `IRouteRegistry` to match routes
   - Route data is stored in `HttpContext.RouteData`
   - If no route matches, request passes to next middleware

2. **WebApplication Integration**
   - `MapControllers()` - Maps controller endpoints (stub for Phase 6)
   - `MapRazorPages()` - Maps Razor page endpoints (stub for Phase 6)
   - `MapFallbackToController()` - Maps fallback route to controller action

## Architecture

```
MiniCore.Framework/
└── Routing/
    ├── Abstractions/
    │   ├── IRouteMatcher.cs           # Route pattern matching interface
    │   ├── IRouteRegistry.cs          # Route registration interface
    │   ├── IEndpointRouteBuilder.cs   # Endpoint route builder interface
    │   └── RouteData.cs                # Route data with extracted parameters
    ├── RouteMatcher.cs                 # Route pattern matcher implementation
    ├── RouteRegistry.cs                # Route registry implementation
    ├── EndpointRouteBuilder.cs         # Endpoint route builder implementation
    ├── ControllerMapper.cs             # Controller mapper (bridge for Phase 6)
    └── Extensions/
        └── EndpointRouteBuilderExtensions.cs  # Extension methods for route mapping
```

## Implementation Summary

Phase 6 successfully implements all core routing components:

### ✅ Route Matching

- **IRouteMatcher.cs** - Interface for matching route patterns
- **RouteMatcher.cs** - Implementation that:
  - Matches exact paths
  - Extracts parameters from `{paramName}` patterns
  - Supports catch-all patterns `{*path}`
  - Performs case-insensitive matching
  - Normalizes paths (handles leading/trailing slashes)

### ✅ Route Registry

- **IRouteRegistry.cs** - Interface for route registration and matching
- **RouteRegistry.cs** - Implementation that:
  - Stores registered routes with HTTP method, pattern, and handler
  - Matches requests against registered routes
  - Extracts route parameters using `IRouteMatcher`
  - Supports fallback routes
  - Returns matched handler and route data

### ✅ Route Data

- **RouteData.cs** - Class containing extracted route parameters
  - `Values` dictionary stores parameter names and values
  - Case-insensitive key lookup

### ✅ HttpContext Integration

- **HttpContext.cs** - Updated to include:
  - `RouteData` property for storing route data
  - Route values also stored in `Items` dictionary for compatibility

### ✅ Middleware Integration

- **RoutingMiddleware.cs** - Updated to:
  - Use `IRouteRegistry` from DI
  - Match routes based on HTTP method and path
  - Store route data in `HttpContext`
  - Invoke matched handler or pass to next middleware

### ✅ WebApplication Integration

- **WebApplication.cs** - Updated to:
  - Implement `MapControllers()` (stub for Phase 6)
  - Implement `MapRazorPages()` (stub for Phase 6)
  - Implement `MapFallbackToController()` (basic implementation)

### ✅ Service Registration

- **WebApplicationBuilder.cs** - Updated to:
  - Register `IRouteMatcher` as singleton
  - Register `IRouteRegistry` as singleton
  - Register `ControllerMapper` as singleton

## Current Usage Patterns

### Basic Route Registration

```csharp
var app = builder.Build();

app.UseRouting();

// Manual route registration (when HTTP server is implemented)
var routeRegistry = app.Services.GetRequiredService<IRouteRegistry>();
routeRegistry.Map("GET", "/api/test", async context =>
{
    await context.Response.Body.WriteAsync(/* ... */);
});
```

### Route Patterns

```csharp
// Exact match
routeRegistry.Map("GET", "/api/links", handler);

// With parameter
routeRegistry.Map("GET", "/api/links/{id}", handler);
// Matches: /api/links/123
// Extracts: id = "123"

// Multiple parameters
routeRegistry.Map("GET", "/api/{controller}/{action}/{id}", handler);
// Matches: /api/links/get/123
// Extracts: controller = "links", action = "get", id = "123"

// Catch-all pattern
routeRegistry.Map("GET", "/api/{*path}", handler);
// Matches: /api/links/123/details
// Extracts: path = "links/123/details"
```

### Fallback Routes

```csharp
app.MapFallbackToController(
    action: "RedirectToUrl",
    controller: "Redirect",
    pattern: "{*path}");
```

## Testing Strategy

### Unit Tests

1. **RouteMatcher Tests**
   - Exact path matching
   - Parameter extraction
   - Multiple parameters
   - Catch-all patterns
   - Case-insensitive matching
   - Edge cases (empty paths, no match)

2. **RouteRegistry Tests**
   - Route registration
   - Route matching by HTTP method
   - Parameter extraction
   - Fallback route matching
   - Method case insensitivity

## Integration Details

### RoutingMiddleware Integration

**Before (Phase 5):**
```csharp
public Task InvokeAsync(IHttpContext context)
{
    // Phase 6 will implement full routing logic here
    return _next(context);
}
```

**After (Phase 6):**
```csharp
public Task InvokeAsync(IHttpContext context)
{
    var method = context.Request.Method;
    var path = context.Request.Path ?? "/";

    if (_routeRegistry.TryMatch(method, path, out var handler, out var routeData))
    {
        if (context is Http.HttpContext httpContext && routeData != null)
        {
            httpContext.RouteData = routeData;
        }
        return handler(context);
    }

    return _next(context);
}
```

### WebApplication Integration

**Before (Phase 5):**
```csharp
public WebApplication MapControllers()
{
    throw new NotImplementedException("Routing framework is not yet implemented. This will be available in Phase 6.");
}
```

**After (Phase 6):**
```csharp
public WebApplication MapControllers()
{
    var mapper = Services.GetService<ControllerMapper>();
    if (mapper != null)
    {
        mapper.MapControllers();
    }
    return this;
}
```

## Success Criteria

- ✅ Route pattern matching works correctly
- ✅ Parameter extraction works for all pattern types
- ✅ Route registry matches routes by HTTP method and path
- ✅ Fallback routes work correctly
- ✅ Route data is available in `HttpContext`
- ✅ Routing middleware integrates with route registry
- ✅ WebApplication routing methods are implemented
- ✅ Unit tests for routing framework pass
- ✅ No breaking changes to application code
- ✅ Ready for Phase 7 (HTTP Server)

## Known Limitations

### Controller Mapping

**Status:** Stub implementation (Phase 6)

**Current Behavior:** `MapControllers()` is a stub that doesn't actually map controllers. The application still uses Microsoft's endpoint routing for controllers.

**Future Enhancement:** Full controller discovery and mapping will be implemented in a future phase. For now, Microsoft's routing handles controller endpoints.

### Razor Pages Mapping

**Status:** Stub implementation (Phase 6)

**Current Behavior:** `MapRazorPages()` is a stub that doesn't actually map Razor pages.

**Future Enhancement:** Razor Pages support will be implemented in a future phase.

### HTTP Server Integration

**Status:** Not yet implemented (Phase 7)

**Current Behavior:** Routes can be registered, but there's no HTTP server to receive requests and invoke the routing middleware.

**Future Enhancement:** Phase 7 will implement HTTP server that receives requests and invokes the middleware pipeline, which will use the routing framework.

## Key Implementation Details

### Route Pattern Matching

The `RouteMatcher` uses a simple segment-by-segment matching algorithm:

1. **Normalize paths**: Remove leading/trailing slashes
2. **Split into segments**: Split pattern and path by `/`
3. **Match segments**: For each segment:
   - If pattern segment is `{paramName}`, extract parameter value
   - If pattern segment is literal, must match exactly (case-insensitive)
   - If pattern ends with `{*path}`, capture remaining path as parameter
4. **Return route data**: Dictionary of parameter names and values

### Route Registry Matching

The `RouteRegistry` matches routes in order:

1. **Try registered routes**: Check each registered route:
   - Match HTTP method (case-insensitive)
   - Match path pattern using `IRouteMatcher`
   - Return handler and route data if match found
2. **Try fallback**: If no route matched and fallback is registered:
   - Return fallback handler
   - Return empty route data

### Route Data Storage

Route data is stored in two places for compatibility:

1. **HttpContext.RouteData**: Primary storage for route data
2. **HttpContext.Items**: Also stored as `route:{paramName}` for compatibility with Microsoft's routing

## Next Steps

Phase 6 is complete. Next phases:

- **Phase 7**: HTTP Server (will invoke middleware pipeline and routing for each request)
- **Future**: Full controller discovery and mapping
- **Future**: Razor Pages support

## References

- [ASP.NET Core Routing](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing)
- [Route Templates](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing#route-template-reference)
- [Route Parameters](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing#route-parameters)

