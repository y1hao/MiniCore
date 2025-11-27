# WebApplication.CreateBuilder Replacement Analysis

## Overview

This document analyzes what's needed to replace `WebApplication.CreateBuilder()` and `WebApplication` with our own implementations in `MiniCore.Web`.

## Current Usage Pattern

From `MiniCore.Web/Program.cs`, we can see the following usage:

### WebApplicationBuilder Usage (`builder`)

1. **`builder.Host`** - Access to `IHostBuilder`
   - Used for: `builder.Host.UseServiceProviderFactory(...)`

2. **`builder.Environment`** - Access to `IWebHostEnvironment`
   - Properties used:
     - `ContentRootPath` - Path to content root directory
     - `EnvironmentName` - Current environment name (Development, Production, Testing)
   - Methods used:
     - `IsDevelopment()` - Returns true if environment is Development
     - `IsEnvironment(string name)` - Checks if environment matches a specific name

3. **`builder.Services`** - Access to `IServiceCollection`
   - Used for: `AddDbContext`, `AddControllers`, `AddRazorPages`, `AddControllersWithViews`, `AddHostedService`

4. **`builder.Configuration`** - Access to `IConfiguration`
   - Used for: `GetConnectionString("DefaultConnection")`

5. **`builder.Build()`** - Builds and returns `WebApplication`

### WebApplication Usage (`app`)

1. **`app.Environment`** - Access to `IWebHostEnvironment`
   - Same properties/methods as `builder.Environment`

2. **`app.Services`** - Access to `IServiceProvider`
   - Used for: `CreateScope()`, `GetRequiredService<T>()`

3. **Middleware Methods:**
   - `UseDeveloperExceptionPage()` - Exception handling middleware
   - `UseStaticFiles()` - Static file serving middleware
   - `UseRouting()` - Routing middleware

4. **Routing Methods:**
   - `MapControllers()` - Maps controller endpoints
   - `MapRazorPages()` - Maps Razor page endpoints
   - `MapFallbackToController(...)` - Maps fallback route

5. **`app.Run()`** - Starts the application and begins listening for requests

## What We Have (Chapter 4 Complete)

✅ **IHostBuilder** - Basic host builder with:
- `ConfigureServices()`
- `ConfigureAppConfiguration()`
- `ConfigureLogging()`
- `Build()` - Returns `IHost`

✅ **IHost** - Basic host with:
- `Services` property (IServiceProvider)
- `StartAsync()` / `StopAsync()`

✅ **Configuration** - Full configuration system
✅ **Logging** - Full logging system
✅ **Dependency Injection** - Full DI container

## What's Missing

### 1. IWebHostEnvironment Interface ❌

**Required Properties:**
- `string ContentRootPath { get; }` - Path to the application's content root
- `string EnvironmentName { get; }` - Current environment name (Development, Production, Testing)

**Required Methods:**
- `bool IsDevelopment()` - Returns true if environment is "Development"
- `bool IsEnvironment(string environmentName)` - Checks if environment matches

**Implementation Notes:**
- Should be registered in DI container
- ContentRootPath typically defaults to `Directory.GetCurrentDirectory()` or can be configured
- EnvironmentName typically comes from `ASPNETCORE_ENVIRONMENT` environment variable or `--environment` command line argument

### 2. WebApplicationBuilder Class ❌

**Required Properties:**
- `IHostBuilder Host { get; }` - Access to the underlying host builder
- `IWebHostEnvironment Environment { get; }` - Environment information
- `IServiceCollection Services { get; }` - Service collection for DI registration
- `IConfiguration Configuration { get; }` - Configuration root

**Required Methods:**
- `WebApplication Build()` - Builds the application and returns `WebApplication`

**Implementation Notes:**
- Should wrap our `HostBuilder` and provide convenient access to its components
- Should initialize default configuration, logging, and services
- Should create and register `IWebHostEnvironment` in the service collection

### 3. WebApplication Class ❌

**Required Properties:**
- `IWebHostEnvironment Environment { get; }` - Environment information
- `IServiceProvider Services { get; }` - Service provider from built host

**Required Methods (Middleware Pipeline - Phase 5):**
- `IApplicationBuilder UseDeveloperExceptionPage()` - Adds exception handling middleware
- `IApplicationBuilder UseStaticFiles()` - Adds static file serving middleware
- `IApplicationBuilder UseRouting()` - Adds routing middleware

**Required Methods (Routing - Phase 6):**
- `IEndpointRouteBuilder MapControllers()` - Maps controller endpoints
- `IEndpointRouteBuilder MapRazorPages()` - Maps Razor page endpoints
- `IEndpointRouteBuilder MapFallbackToController(...)` - Maps fallback route

**Required Methods:**
- `Task Run()` - Starts the host and begins listening for HTTP requests

**Implementation Notes:**
- Should wrap `IHost` and provide web-specific functionality
- Middleware methods will need Phase 5 (Middleware Pipeline) to be implemented
- Routing methods will need Phase 6 (Routing Framework) to be implemented
- `Run()` will need Phase 7 (HTTP Server) to be implemented

### 4. WebApplication.CreateBuilder() Static Method ❌

**Required Signature:**
```csharp
public static WebApplicationBuilder CreateBuilder(string[]? args = null)
```

**Implementation Notes:**
- Should create a new `WebApplicationBuilder` instance
- Should initialize default configuration (command line args, environment variables, JSON files)
- Should initialize default logging
- Should set up default environment detection
- Should return the builder for further configuration

### 5. Middleware Pipeline (Phase 5) ❌

**Required Components:**
- `RequestDelegate` delegate type
- `IApplicationBuilder` interface with `Use()` methods
- Middleware execution pipeline
- Built-in middleware implementations:
  - Exception handling middleware
  - Static file serving middleware
  - Routing middleware

**Status:** Not yet implemented (Phase 5)

### 6. Routing Framework (Phase 6) ❌

**Required Components:**
- `IEndpointRouteBuilder` interface
- Route registration and matching
- Controller endpoint mapping
- Razor page endpoint mapping
- Fallback route support

**Status:** Not yet implemented (Phase 6)

### 7. HTTP Server (Phase 7) ❌

**Required Components:**
- `IServer` interface
- `HttpListener`-based server implementation
- `HttpContext` abstraction
- Request/response handling
- Integration with middleware pipeline

**Status:** Not yet implemented (Phase 7)

## Implementation Strategy

### Immediate (Can Do Now)

Even without Phase 5-7, we can create the basic structure:

1. **Create `IWebHostEnvironment` interface and implementation**
   - Simple wrapper around environment name and content root path
   - Can be implemented immediately

2. **Create `WebApplicationBuilder` class**
   - Wraps `HostBuilder`
   - Provides convenient access to `Services`, `Configuration`, `Environment`
   - Can initialize defaults similar to ASP.NET Core

3. **Create `WebApplication` class (skeleton)**
   - Wraps `IHost`
   - Provides `Environment` and `Services` properties
   - Middleware/routing methods can be stubs that throw `NotImplementedException` until Phase 5-7
   - `Run()` can call `host.StartAsync()` and wait (but won't handle HTTP requests until Phase 7)

### Future (Requires Phase 5-7)

4. **Implement middleware pipeline** (Phase 5)
   - Replace middleware method stubs with real implementations

5. **Implement routing** (Phase 6)
   - Replace routing method stubs with real implementations

6. **Implement HTTP server** (Phase 7)
   - Replace `Run()` stub with real HTTP server that processes requests through middleware pipeline

## Recommended Next Steps

1. **Create `IWebHostEnvironment` interface and `WebHostEnvironment` implementation**
   - Location: `MiniCore.Framework/Hosting/Abstractions/`
   - Register in `HostBuilder.Build()` method

2. **Create `WebApplicationBuilder` class**
   - Location: `MiniCore.Framework/Hosting/`
   - Implement `CreateBuilder()` static method
   - Wrap `HostBuilder` and provide convenient properties

3. **Create `WebApplication` class (skeleton)**
   - Location: `MiniCore.Framework/Hosting/`
   - Wrap `IHost` and provide web-specific properties
   - Add stub methods for middleware/routing (throw `NotImplementedException`)

4. **Update `MiniCore.Web/Program.cs`**
   - Replace `WebApplication.CreateBuilder()` with `MiniCore.Framework.Hosting.WebApplication.CreateBuilder()`
   - This will allow compilation but runtime will fail on middleware/routing calls until Phase 5-7

## Summary

**To fully replace `WebApplication.CreateBuilder()`:**

✅ **Can implement now:**
- `IWebHostEnvironment` interface and implementation
- `WebApplicationBuilder` class (basic structure)
- `WebApplication` class (basic structure with stubs)

❌ **Requires future phases:**
- Middleware pipeline (Phase 5)
- Routing framework (Phase 6)
- HTTP server (Phase 7)

**The basic structure can be put in place now, but full functionality requires completing Phases 5-7.**

