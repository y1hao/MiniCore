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

### Phase 1: Dependency Injection Framework
Implement a minimal DI container to replace `Microsoft.Extensions.DependencyInjection`.

**Key Features:**
- `IServiceCollection`, `IServiceProvider`
- Transient / Singleton / Scoped lifetimes
- Constructor injection
- Open-generic support (`ILogger<T>`)

### Phase 2: Configuration Framework
Replace `Microsoft.Extensions.Configuration`.

**Key Features:**
- Hierarchical key-value store (`IConfiguration`, `IConfigurationSection`)
- JSON + Environment variable sources
- `IConfigurationBuilder` to compose multiple sources

### Phase 3: Logging Framework
Implement the basic abstractions of `Microsoft.Extensions.Logging`.

**Key Features:**
- `ILogger`, `ILoggerFactory`, `ILogProvider`
- Console + File loggers
- Log levels, message templates

### Phase 4: Host Abstraction
Build a minimal equivalent of `IHost` and `HostBuilder`.

**Key Features:**
- `HostBuilder` with `ConfigureServices`, `ConfigureLogging`, `ConfigureAppConfiguration`
- Builds unified `MiniHost` object
- Registers `IHostApplicationLifetime` for graceful start/stop

### Phase 5: Middleware Pipeline
Recreate `Use`, `UseMiddleware`, and request-delegate chaining.

**Key Features:**
- `RequestDelegate` delegate pattern
- Order-preserving execution
- Built-in middlewares: Exception handling, Static file serving, Logging, Routing

### Phase 6: Routing Framework
Implement a lightweight router.

**Key Features:**
- Route registration: `Map("GET", "/api/links/{id}", handler)`
- Path parameter extraction
- Verb matching (GET/POST/PUT/DELETE)
- Route fallback (404)

### Phase 7: HTTP Server (HttpListener Backend)
Replace Kestrel with an HttpListener-based implementation.

**Key Features:**
- Implement `IServer` interface
- Wrap `HttpListener` for HTTP/1.1
- Translate incoming requests into `HttpContext`
- Invoke middleware pipeline

### Phase 8: Mini ORM / Data Integration
Replace EF Core with a lightweight reflection-based ORM.

**Key Features:**
- CRUD via ADO.NET (`System.Data.SQLite`)
- Map rows ↔ objects via reflection
- Simple query builder (select/insert/update/delete)

### Phase 9: Frontend Templating
Replace Razor with a simple templating engine.

**Key Features:**
- Load `.html` templates from disk
- Replace `{{variable}}` placeholders
- Optional loops/conditionals

### Phase 10: Background Services
Implement a minimal background service system to mirror `IHostedService` and `BackgroundService`.

**Key Features:**
- `IHostedService` interface
- Host-managed lifecycle integration
- Example: `LinkCleanupService` runs hourly

## 📁 Project Structure

```
MiniCore/
├── src/
│   ├── MiniCore.Web/              # Baseline application (will evolve)
│   ├── MiniCore.Web.Tests/         # Tests for baseline app
│   ├── MiniCore.Reference/         # Static reference copy (unchanged)
│   ├── MiniCore.Reference.Tests/   # Tests for reference app
│   └── MiniCore.Framework/         # Custom framework (future phases)
│       ├── DI/
│       ├── Config/
│       ├── Logging/
│       ├── Hosting/
│       ├── Server/
│       ├── Routing/
│       ├── Middleware/
│       └── Background/
├── docs/
│   ├── Chapter0/                   # Phase 0 documentation
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

### Running the Baseline Application

```bash
# Navigate to the project
cd src/MiniCore.Web

# Restore dependencies
dotnet restore

# Run the application
dotnet run

# Access the admin interface
# HTTP: http://localhost:5037/admin
# HTTPS: https://localhost:7133/admin
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
| `IServer` | HTTP Server | `StartAsync`, `StopAsync` |
| `IHostedService` | Background tasks | `StartAsync`, `StopAsync` |
| `RequestDelegate` | Middleware link | Async invocation |

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

---

## 📝 License

This project is part of an educational effort to understand ASP.NET Core internals.

## 🤝 Contributing

This is an educational project. Feel free to explore, learn, and adapt the code for your own learning purposes.

---

**Status:** Phase 0 Complete ✅ | Next: Phase 1 - Dependency Injection Framework

