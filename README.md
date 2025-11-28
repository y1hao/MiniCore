# MiniCore

**Re-implementing the core of ASP.NET Core from scratch**

An educational project to build a minimal yet realistic framework that mirrors ASP.NET Core's core concepts — Dependency Injection, Configuration, Logging, Hosting, Middleware, Routing, and HTTP Server — without any dependency on Microsoft's implementations.

## 🎯 Project Goal

Build a self-contained "Mini .NET Core" framework by progressively replacing ASP.NET Core's underlying runtime components using a **strangler pattern approach**. The final product will run the same application logic with identical behavior at the user level, but powered entirely by custom implementations.

## 📋 Overview

We start from a working ASP.NET Core web application and progressively **replace** the underlying runtime components one subsystem at a time. Our baseline application is a **URL Shortener** that exercises all the key concepts we'll re-implement:

- CRUD API for managing short links
- Redirect endpoint (`/{shortCode}` → original URL)
- HTML admin page rendered via templating
- SQLite for persistence
- Background service for cleaning up expired links

**Current Status:** MiniCore.Web now uses MiniCore.Framework for hosting, MVC, configuration, logging, and dependency injection. The only remaining Microsoft dependency is Entity Framework Core (to be replaced in Phase 9).

## 🏗️ Architecture

```
+----------------------------------------------------+
| Application (Controllers, Views, Services)         |
|  - ShortLinkController                             |
|  - CleanupBackgroundService                        |
+----------------------------------------------------+
| Routing + Middleware Pipeline                      |
+----------------------------------------------------+
| Host (DI, Config, Logging, Lifetime)               |
+----------------------------------------------------+
| Server (HttpListener-based)                        |
+----------------------------------------------------+
| OS / .NET Runtime                                  |
+----------------------------------------------------+
```

## 🎓 Guiding Principles

| Principle | Description |
|-----------|-------------|
| **Progressive strangling** | Replace one subsystem at a time, keeping others intact for regression comparison |
| **Faithful abstractions** | Mirror official interfaces (`IServiceProvider`, `IServer`, `IHost`) even if implementation is simplified |
| **Educational clarity over performance** | Readability and conceptual accuracy take precedence over optimization |
| **Isolation and observability** | Each phase includes basic logging and tests to verify correctness |

## 🚀 Implementation Phases

### Phase 0: Baseline Application ✅
Establish a reference ASP.NET Core app for functional parity.

**Status:** Complete  
**See:** [Chapter 0 Documentation](docs/Chapter0/README.md)

### Phase 1: Dependency Injection Framework ✅
Implemented a minimal DI container to replace `Microsoft.Extensions.DependencyInjection`.

**Status:** ✅ Complete  
**See:** [Chapter 1 Documentation](docs/Chapter1/README.md)

**Key Features:**
- ✅ `IServiceCollection`, `IServiceProvider`
- ✅ Transient / Singleton / Scoped lifetimes
- ✅ Constructor injection
- ✅ Open-generic support (`ILogger<T>`)

### Phase 2: Configuration Framework ✅
Replace `Microsoft.Extensions.Configuration`.

**Status:** ✅ Complete  
**See:** [Chapter 2 Documentation](docs/Chapter2/README.md)

**Key Features:**
- ✅ Hierarchical key-value store (`IConfiguration`, `IConfigurationSection`)
- ✅ JSON + Environment variable sources
- ✅ `IConfigurationBuilder` to compose multiple sources
- ✅ POCO binding (`Bind<T>()`, `GetValue<T>()`)
- ✅ Configuration reload tokens (`IChangeToken`)

### Phase 3: Logging Framework ✅
Implement the basic abstractions of `Microsoft.Extensions.Logging`.

**Status:** ✅ Complete  
**See:** [Chapter 3 Documentation](docs/Chapter3/README.md)

**Key Features:**
- ✅ `ILogger`, `ILoggerFactory`, `ILoggerProvider`
- ✅ Console + File loggers with color coding
- ✅ Log levels (Trace, Debug, Information, Warning, Error, Critical)
- ✅ Message templates with placeholder support
- ✅ Generic `ILogger<T>` for automatic category naming
- ✅ Exception logging with stack traces
- ✅ DI integration (`AddLogging()`, `AddConsole()`, `AddFile()`)

### Phase 4: Host Abstraction ✅
Build a minimal equivalent of `IHost` and `HostBuilder`, plus `WebApplicationBuilder` and `WebApplication` for web applications.

**Status:** ✅ Complete  
**See:** [Chapter 4 Documentation](docs/Chapter4/README.md)

**Key Features:**
- ✅ `HostBuilder` with `ConfigureServices`, `ConfigureLogging`, `ConfigureAppConfiguration`
- ✅ Builds unified `Host` object
- ✅ Registers `IHostApplicationLifetime` for graceful start/stop
- ✅ `IWebHostEnvironment` interface for environment information
- ✅ `WebApplicationBuilder` class for building web applications
- ✅ `WebApplication` class with middleware pipeline (Phase 5), routing (Phase 6), server stub (Phase 7)
- ✅ Composes DI + Config + Logging
- ✅ Background service lifecycle management

### Phase 5: Middleware Pipeline ✅
Recreate `Use`, `UseMiddleware`, and request-delegate chaining.

**Status:** ✅ Complete  
**See:** [Chapter 5 Documentation](docs/Chapter5/README.md)

**Key Features:**
- ✅ `RequestDelegate` delegate pattern (`Task Invoke(IHttpContext context)`)
- ✅ `IApplicationBuilder` interface and `ApplicationBuilder` implementation
- ✅ Order-preserving middleware execution
- ✅ Built-in middlewares:
  - ✅ Exception handling (`UseDeveloperExceptionPage`)
  - ✅ Static file serving (`UseStaticFiles`)
  - ✅ Request/response logging (`UseRequestLogging`)
  - ✅ Routing middleware (`UseRouting` - Phase 6 complete)
- ✅ HTTP abstractions (`IHttpContext`, `IHttpRequest`, `IHttpResponse`)
- ✅ `WebApplication` integration with middleware pipeline

### Phase 6: Routing Framework ✅
Implement a lightweight router.

**Status:** ✅ Complete  
**See:** [Chapter 6 Documentation](docs/Chapter6/README.md)

**Key Features:**
- ✅ Route pattern matching with parameter extraction (`{param}`, `{*path}`)
- ✅ Route registration: `Map("GET", "/api/links/{id}", handler)`
- ✅ HTTP verb matching (GET/POST/PUT/DELETE/PATCH)
- ✅ Route fallback support
- ✅ Controller discovery and routing (`MapControllers()`)
- ✅ Route parameter binding from route data and query strings
- ✅ Custom routing attributes (Route, HttpGet, HttpPost, HttpDelete, etc.)
- ✅ Integration with middleware pipeline
- ✅ `MapFallbackToController()` for fallback routes

### Phase 7: HTTP Server (HttpListener Backend) ✅
Replace Kestrel with an HttpListener-based implementation.

**Status:** ✅ Complete  
**See:** [Chapter 7 Documentation](docs/Chapter7/README.md)

**Key Features:**
- ✅ Implement `IServer` interface
- ✅ Wrap `HttpListener` for HTTP/1.1
- ✅ Translate incoming requests into `HttpContext`
- ✅ Invoke middleware pipeline

### Phase 8: MVC Framework ✅
Replace Microsoft.AspNetCore.Mvc with our own MVC implementation.

**Status:** ✅ Complete

**Key Features:**
- ✅ `IController` interface and `Controller` base class
- ✅ `IActionResult` interface and implementations (Ok, BadRequest, NotFound, NoContent, Created, Redirect, etc.)
- ✅ Model binding from route parameters, query strings, and request body
- ✅ Action method invocation with parameter binding
- ✅ Controller discovery and action method discovery
- ✅ Support for `[FromBody]`, `[FromQuery]`, `[FromRoute]` attributes
- ✅ Integration with routing framework
- ✅ All controllers migrated to use MiniCore.Framework types

### Phase 9: Mini ORM / Data Integration
Replace EF Core with a lightweight reflection-based ORM.

**Key Features:**
- CRUD via ADO.NET (`System.Data.SQLite`)
- Map rows ↔ objects via reflection
- Simple query builder (select/insert/update/delete)

### Phase 10: Frontend Templating
Replace Razor with a simple templating engine.

**Key Features:**
- Load `.html` templates from disk
- Replace `{{variable}}` placeholders
- Optional loops/conditionals

### Phase 11: Background Services ✅
Implement a minimal background service system to mirror `IHostedService` and `BackgroundService`.

**Status:** ✅ Complete (implemented in Phase 4)

**Key Features:**
- ✅ `IHostedService` interface
- ✅ Host-managed lifecycle integration
- ✅ Example: `LinkCleanupService` runs hourly

## 📁 Project Structure

```
MiniCore/
├── src/
│   ├── MiniCore.Web/              # Baseline application (will evolve)
│   ├── MiniCore.Web.Tests/         # Tests for baseline app
│   ├── MiniCore.Reference/         # Static reference copy (unchanged)
│   ├── MiniCore.Reference.Tests/   # Tests for reference app
│   └── MiniCore.Framework/         # Custom framework
│       ├── DependencyInjection/    # ✅ Phase 1 Complete
│       ├── Configuration/          # ✅ Phase 2 Complete
│       ├── Logging/                 # ✅ Phase 3 Complete
│       ├── Hosting/                 # ✅ Phase 4 Complete
│       ├── Http/                    # ✅ Phase 5 Complete
│       │   ├── Abstractions/        # HTTP interfaces
│       │   ├── Middleware/          # Built-in middlewares
│       │   └── Extensions/          # Extension methods
│       ├── Server/                  # ✅ Phase 7 Complete
│       ├── Routing/                 # ✅ Phase 6 Complete
│       │   ├── Abstractions/        # Routing interfaces
│       │   ├── Attributes/          # Routing attributes
│       │   └── Extensions/          # Extension methods
│       ├── Mvc/                     # ✅ Phase 8 Complete
│       │   ├── Abstractions/        # MVC interfaces
│       │   ├── Controllers/         # Controller base classes
│       │   ├── Results/             # ActionResult implementations
│       │   └── ModelBinding/        # Model binding
│       └── Background/              # ✅ Phase 11 Complete (in Hosting)
├── docs/
│   ├── Chapter0/                   # Phase 0 documentation ✅
│   ├── Chapter1/                   # Phase 1 documentation ✅
│   ├── Chapter2/                   # Phase 2 documentation ✅
│   ├── Chapter3/                   # Phase 3 documentation ✅
│   ├── Chapter4/                   # Phase 4 documentation ✅
│   ├── Chapter5/                   # Phase 5 documentation ✅
│   ├── Chapter6/                   # Phase 6 documentation ✅
│   ├── Chapter7/                   # Phase 7 documentation ✅
│   └── SPEC.md                    # Detailed specification
└── README.md                      # This file
```

## 🧪 Testing Strategy

- Maintain the Phase 0 baseline as ground truth
- After each phase:
  - Run existing integration tests against new layer
  - Compare outputs (HTTP responses, logs, config values)
  - Benchmark startup and per-request overhead
- Unit-test each component in isolation

## 🛠️ Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- SQLite (included with .NET runtime)

## 🚦 Getting Started

### Running the Application

**Note:** MiniCore.Web now uses MiniCore.Framework for hosting, MVC, configuration, logging, and dependency injection. The application runs entirely on our custom framework, with Entity Framework Core being the only remaining Microsoft dependency.

```bash
# Navigate to the project
cd src/MiniCore.Web

# Restore dependencies
dotnet restore

# Run the application
dotnet run

# Access the admin interface
# HTTP: http://localhost:5000/admin
# HTTPS: https://localhost:5001/admin
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests for a specific project
dotnet test src/MiniCore.Web.Tests/MiniCore.Web.Tests.csproj
```

## 📚 Documentation

- **[SPEC.md](SPEC.md)** - Complete project specification with detailed phase descriptions
- **[Chapter 0: Baseline Application](docs/Chapter0/README.md)** - Phase 0 implementation details
- **[Chapter 1: Dependency Injection Framework](docs/Chapter1/README.md)** - Phase 1 implementation details ✅
- **[Chapter 2: Configuration Framework](docs/Chapter2/README.md)** - Phase 2 implementation details ✅
- **[Chapter 3: Logging Framework](docs/Chapter3/README.md)** - Phase 3 implementation details ✅
- **[Chapter 4: Host Abstraction](docs/Chapter4/README.md)** - Phase 4 implementation details ✅
- **[Chapter 5: Middleware Pipeline](docs/Chapter5/README.md)** - Phase 5 implementation details ✅
- **[Chapter 6: Routing Framework](docs/Chapter6/README.md)** - Phase 6 implementation details ✅
- **[Chapter 7: HTTP Server](docs/Chapter7/README.md)** - Phase 7 implementation details ✅
- **[Chapter 8: MVC Framework](docs/Chapter8/README.md)** - Phase 8 implementation details ✅

## 🎯 Expected Learning Outcomes

- Deep understanding of ASP.NET Core architecture
- Experience with reflection, async I/O, and inversion of control
- Practical grasp of how frameworks compose modular abstractions
- Insight into where complexity hides in production frameworks
- Understanding of host lifecycle and background job integration

## 🔑 Key Abstractions

| Interface | Purpose | Minimal Requirements |
|-----------|---------|----------------------|
| `IServiceProvider` | Dependency Injection | `GetService`, lifetimes |
| `IConfiguration` | Config Access | Key lookup, sections |
| `ILogger` | Logging | Levels, message formatting |
| `IHost` | Composition root | Lifecycle control |
| `IHostBuilder` | Host configuration | `ConfigureServices`, `Build()` |
| `IWebHostEnvironment` | Web environment | `ContentRootPath`, `EnvironmentName` |
| `WebApplicationBuilder` | Web app builder | `CreateBuilder()`, `Build()` |
| `WebApplication` | Web application | `Run()`, middleware pipeline |
| `IHttpContext` | HTTP context | Request, Response, Items, RequestServices |
| `IHttpRequest` | HTTP request | Method, Path, Headers, Body |
| `IHttpResponse` | HTTP response | StatusCode, Headers, Body |
| `IApplicationBuilder` | Middleware builder | `Use()`, `Build()` |
| `RequestDelegate` | Middleware delegate | `Task Invoke(IHttpContext)` |
| `IServer` | HTTP Server | `StartAsync`, `StopAsync` |
| `IHostedService` | Background tasks | `StartAsync`, `StopAsync` |
| `IController` | MVC Controller | `HttpContext` property |
| `IActionResult` | MVC Result | `ExecuteResultAsync(ActionContext)` |

## 📖 Chapter Summaries

### [Chapter 0: Baseline Application](docs/Chapter0/README.md)

Phase 0 establishes the foundation by creating a fully functional URL shortener application using standard ASP.NET Core. This baseline application serves as both a reference implementation and a test target for all subsequent phases.

**Key Accomplishments:**
- ✅ Created MiniCore.Web - a production-ready URL shortener
- ✅ Built comprehensive test suite (42 tests, all passing)
- ✅ Created MiniCore.Reference - static reference copy for comparison
- ✅ Documented all features and API endpoints
- ✅ Established foundation for progressive component replacement

**Read More:** [Chapter 0 Documentation](docs/Chapter0/README.md)

### [Chapter 1: Dependency Injection Framework](docs/Chapter1/README.md) ✅

Phase 1 successfully implemented a minimal Dependency Injection container to replace `Microsoft.Extensions.DependencyInjection`. This is the foundation that all other framework components will build upon.

**Status:** ✅ Complete

**Key Accomplishments:**
- ✅ Implemented core DI interfaces matching Microsoft's API surface
- ✅ Support three service lifetimes: Transient, Scoped, and Singleton
- ✅ Constructor injection with automatic dependency resolution
- ✅ Open-generic support (e.g., `ILogger<T>`)
- ✅ Service scope management for scoped lifetime services
- ✅ Comprehensive test coverage
- ✅ Integrated into MiniCore.Web application

**Read More:** [Chapter 1 Documentation](docs/Chapter1/README.md)

### [Chapter 2: Configuration Framework](docs/Chapter2/README.md) ✅

Phase 2 successfully implemented a minimal Configuration framework to replace `Microsoft.Extensions.Configuration`. This provides a hierarchical key-value store for application settings, supporting multiple configuration sources with proper precedence handling.

**Status:** ✅ Complete

**Key Accomplishments:**
- ✅ Implemented core configuration interfaces matching Microsoft's API surface
- ✅ Hierarchical key-value storage with colon-separated keys (e.g., `"A:B:C"`)
- ✅ Multiple configuration sources (JSON files, environment variables)
- ✅ `IConfigurationBuilder` to compose multiple sources
- ✅ Configuration sections (`IConfigurationSection`) with path-aware navigation
- ✅ POCO binding (`Bind<T>()`, `GetValue<T>()`) for mapping configuration to objects
- ✅ Configuration reload tokens (`IChangeToken`) for change notifications
- ✅ Comprehensive test coverage
- ✅ Integrated into MiniCore.Web application

**Read More:** [Chapter 2 Documentation](docs/Chapter2/README.md)

### [Chapter 3: Logging Framework](docs/Chapter3/README.md) ✅

Phase 3 successfully implemented a minimal Logging framework to replace `Microsoft.Extensions.Logging`. This provides cross-cutting logging infrastructure with support for multiple providers and automatic category naming.

**Status:** ✅ Complete

**Key Accomplishments:**
- ✅ Implemented core logging interfaces matching Microsoft's API surface
- ✅ Console logger with color-coded output by log level
- ✅ File logger with thread-safe writing and directory creation
- ✅ Log level filtering (Trace, Debug, Information, Warning, Error, Critical)
- ✅ Message template formatting with placeholder support (`{PropertyName}`)
- ✅ Generic `ILogger<T>` for automatic category naming from type
- ✅ Exception logging with stack traces and inner exception support
- ✅ DI integration (`AddLogging()`, `AddConsole()`, `AddFile()`)
- ✅ Comprehensive test coverage (32/34 tests passing)
- ✅ Integrated into MiniCore.Web application

**Read More:** [Chapter 3 Documentation](docs/Chapter3/README.md)

### [Chapter 4: Host Abstraction](docs/Chapter4/README.md) ✅

Phase 4 successfully implemented a minimal Host abstraction to replace `Microsoft.Extensions.Hosting`. This provides the composition root that ties together the DI container, configuration sources, logging providers, and manages the application lifecycle. Additionally, we implemented `WebApplicationBuilder` and `WebApplication` with stub methods for future middleware, routing, and HTTP server implementations.

**Status:** ✅ Complete

**Key Accomplishments:**
- ✅ Implemented core hosting interfaces matching Microsoft's API surface
- ✅ `HostBuilder` with fluent configuration API (`ConfigureServices`, `ConfigureLogging`, `ConfigureAppConfiguration`)
- ✅ `Host` implementation that composes DI + Config + Logging
- ✅ `IHostApplicationLifetime` for graceful startup and shutdown
- ✅ Background service lifecycle management (`IHostedService`)
- ✅ `IWebHostEnvironment` interface for environment information
- ✅ `WebApplicationBuilder` class for building web applications
- ✅ `WebApplication` class with middleware pipeline (Phase 5), routing/server stubs (Phases 6-7)
- ✅ Comprehensive test coverage (35 tests: 28 passing, 7 skipped for unimplemented features)
- ✅ Middleware pipeline integrated (Phase 5), ready for routing (Phase 6) and HTTP server (Phase 7)

**Read More:** [Chapter 4 Documentation](docs/Chapter4/README.md)

### [Chapter 5: Middleware Pipeline](docs/Chapter5/README.md) ✅

Phase 5 successfully implemented a minimal Middleware Pipeline to replace `Microsoft.AspNetCore.Builder`. This provides the core request/response processing pipeline that allows middleware components to be composed in a chain, processing HTTP requests and responses in order.

**Status:** ✅ Complete

**Key Accomplishments:**
- ✅ Implemented HTTP abstractions (`IHttpContext`, `IHttpRequest`, `IHttpResponse`)
- ✅ `RequestDelegate` delegate type for middleware components
- ✅ `IApplicationBuilder` interface and `ApplicationBuilder` implementation
- ✅ Order-preserving middleware execution pipeline
- ✅ Built-in middlewares:
  - ✅ Exception handling middleware (`UseDeveloperExceptionPage`)
  - ✅ Static file serving middleware (`UseStaticFiles`)
  - ✅ Request/response logging middleware
  - ✅ Routing middleware (`UseRouting` - Phase 6 complete)
- ✅ `WebApplication` integration with middleware pipeline
- ✅ Comprehensive test coverage (13 tests, all passing)
- ✅ Routing framework integrated (Phase 6), ready for Phase 7 (HTTP Server)

**Read More:** [Chapter 5 Documentation](docs/Chapter5/README.md)

### [Chapter 6: Routing Framework](docs/Chapter6/README.md) ✅

Phase 6 successfully implemented a minimal Routing Framework to replace `Microsoft.AspNetCore.Routing`. This provides route pattern matching, parameter extraction, and controller discovery capabilities.

**Status:** ✅ Complete

**Key Accomplishments:**
- ✅ Route pattern matching with parameter extraction (`{param}`, `{*path}` patterns)
- ✅ Route registry for storing and matching routes by HTTP method and path
- ✅ Controller discovery using reflection (convention-based and attribute-based)
- ✅ Custom routing attributes (Route, HttpGet, HttpPost, HttpDelete, HttpPut, HttpPatch, NonAction, Controller)
- ✅ Route parameter binding from route data and query strings
- ✅ Integration with middleware pipeline via `RoutingMiddleware`
- ✅ `MapControllers()` and `MapFallbackToController()` methods
- ✅ HttpContext route data storage
- ✅ Comprehensive test coverage (14 tests, all passing)
- ✅ All routing attributes are our own implementations (no Microsoft dependencies for attributes)
- ✅ Ready for Phase 7 (HTTP Server)

**Read More:** [Chapter 6 Documentation](docs/Chapter6/README.md)

### [Chapter 8: MVC Framework](docs/Chapter8/README.md) ✅

Phase 8 successfully implemented a minimal MVC Framework to replace `Microsoft.AspNetCore.Mvc`. This provides controller base classes, action result types, and model binding capabilities.

**Status:** ✅ Complete

**Key Accomplishments:**
- ✅ Implemented `IController` interface and `ControllerBase`/`Controller` base classes
- ✅ `IActionResult` interface with implementations (Ok, BadRequest, NotFound, NoContent, Created, Redirect)
- ✅ Model binding from route parameters, query strings, and request body
- ✅ Action method invocation with automatic parameter binding
- ✅ Controller discovery and action method discovery via reflection
- ✅ Support for `[FromBody]`, `[FromQuery]`, `[FromRoute]` attributes
- ✅ Integration with routing framework (Phase 6)
- ✅ All controllers in MiniCore.Web migrated to use MiniCore.Framework types
- ✅ Removed adapter files (ConfigurationAdapter, LoggingAdapter, ServiceProviderFactory)
- ✅ MiniCore.Web now uses MiniCore.Framework exclusively (except EF Core)

**Read More:** [Chapter 8 Documentation](docs/Chapter8/README.md)

---

## 📝 License

This project is part of an educational effort to understand ASP.NET Core internals.

## 🤝 Contributing

This is an educational project. Feel free to explore, learn, and adapt the code for your own learning purposes.

---

**Status:** Phase 0 Complete ✅ | Phase 1 Complete ✅ | Phase 2 Complete ✅ | Phase 3 Complete ✅ | Phase 4 Complete ✅ | Phase 5 Complete ✅ | Phase 6 Complete ✅ | Phase 7 Complete ✅ | Phase 8 Complete ✅ | Next: Phase 9 - Mini ORM / Data Integration

**Migration Status:** MiniCore.Web now uses MiniCore.Framework for all core components. Only Entity Framework Core remains as a Microsoft dependency.

