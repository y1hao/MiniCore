# SPEC.md

**Project:** Re-implementing the core of ASP.NET Core from scratch
**Goal:** Build a minimal yet realistic framework that mirrors .NET Core’s core concepts — DI, configuration, logging, hosting, middleware, routing, and a simple HTTP server — without any dependency on Microsoft’s implementations.

---

## 0. Overview

We start from a working ASP.NET Core web app (`dotnet new web`) and progressively **replace** the underlying runtime components (using a strangler-pattern approach).
The final product will be a self-contained “Mini .NET Core” that can run the same application logic with identical behaviour at the user level.

Our **baseline app** will be a small **URL Shortener**, complete with:

* CRUD API for managing short links,
* A redirect endpoint (`/{shortCode}` → original URL),
* Simple HTML admin page rendered via templating,
* SQLite for persistence,
* Background service for cleaning up expired links.

---

## 1. Objectives

* Understand and re-implement the essential abstractions of .NET Core:

  * Dependency Injection
  * Configuration
  * Logging
  * Host Builder & Lifetime
  * Middleware Pipeline & Routing
  * HTTP Server (HttpListener)
  * ORM Layer
  * Frontend Templating
  * Background Services
* Keep each subsystem **modular, testable, and replaceable**.
* Maintain parity with a baseline ASP.NET Core app’s behaviour and public API surface (where reasonable).

---

## 2. Guiding Principles

| Principle                                | Description                                                                                               |
| ---------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| **Progressive strangling**               | Replace one subsystem at a time, keeping others intact for regression comparison.                         |
| **Faithful abstractions**                | Mirror official interfaces (`IServiceProvider`, `IServer`, `IHost`) even if implementation is simplified. |
| **Educational clarity over performance** | Readability and conceptual accuracy take precedence over optimization.                                    |
| **Isolation and observability**          | Each phase includes basic logging and tests to verify correctness.                                        |

---

## 3. Architecture Summary

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

### Cross-cutting concerns

* Logging, configuration, and DI are consumed by nearly all higher layers.
* The host is the root composition point and orchestrates startup, background jobs, and graceful shutdown.

---

## 4. Implementation Phases

### **Phase 0 — Baseline Application**

**Goal:** Establish a reference ASP.NET Core app for functional parity.

**Deliverables**

* `dotnet new web`
* API endpoints:

  * `GET /api/links` — list all links
  * `POST /api/links` — create short link
  * `DELETE /api/links/{id}` — remove link
  * `GET /{shortCode}` — redirect endpoint
* SQLite + EF Core for persistence
* HTML admin page listing all links
* Unit tests for key endpoints
* Background service: cleanup expired short links every hour

**Reasoning:**
Provides a small, real-world web app that uses routing, DI, ORM, templating, logging, and a background service — all of which you’ll later reimplement.

---

### **Phase 1 — Dependency Injection Framework**

**Goal:** Implement a minimal DI container to replace `Microsoft.Extensions.DependencyInjection`.

**Key Features**

* `IServiceCollection`, `IServiceProvider`
* Transient / Singleton / Scoped lifetimes
* Constructor injection
* Open-generic support (`ILogger<T>`)
* `IServiceScopeFactory`

**Reasoning:**
DI is the foundation of ASP.NET Core; nearly all services depend on it. Re-implementing first allows every later phase to build atop your container.

---

### **Phase 2 — Configuration Framework**

**Goal:** Replace `Microsoft.Extensions.Configuration`.

**Key Features**

* Hierarchical key-value store (`IConfiguration`, `IConfigurationSection`)
* JSON + Environment variable sources
* `IConfigurationBuilder` to compose multiple sources
* Optional: simple POCO binding (`config.Bind<T>()`)

**Reasoning:**
Configuration feeds both the Host and application settings. It must precede Host Builder construction.

---

### **Phase 3 — Logging Framework**

**Goal:** Implement the basic abstractions of `Microsoft.Extensions.Logging`.

**Key Features**

* `ILogger`, `ILoggerFactory`, `ILogProvider`
* Console + File loggers
* Log levels, message templates
* Scoped logging context

**Reasoning:**
Logging is cross-cutting and required by DI, Host, and Middleware. Building it early ensures traceability through all subsequent layers.

---

### **Phase 4 — Host Abstraction**

**Goal:** Build a minimal equivalent of `IHost` and `HostBuilder`.

**Key Features**

* `HostBuilder` with `ConfigureServices`, `ConfigureLogging`, `ConfigureAppConfiguration`
* Builds unified `MiniHost` object
* Registers `IHostApplicationLifetime` for graceful start/stop
* Composes DI + Config + Logging
* Calls `StartAsync()` / `StopAsync()`

**Reasoning:**
The Host is the composition root: it ties together the DI container, configuration sources, logging providers, and the server.
Having it early ensures realistic lifecycle management.

---

### **Phase 5 — Middleware Pipeline**

**Goal:** Recreate `Use`, `UseMiddleware`, and request-delegate chaining.

**Key Features**

* `RequestDelegate` delegate (`Task Invoke(HttpContext ctx)`)
* `app.Use(next => ctx => {...})` pattern
* Order-preserving execution
* Built-in middlewares: ExceptionHandler, Logging, Routing

**Reasoning:**
Middleware is the heart of the ASP.NET Core runtime model. It provides extensibility and composition semantics for all request handling.

---

### **Phase 6 — Routing Framework**

**Goal:** Implement a lightweight router.

**Key Features**

* Route registration: `Map("GET", "/api/links/{id}", handler)`
* Path parameter extraction
* Verb matching (GET/POST/PUT/DELETE)
* Route fallback (404)
* Integration with redirect endpoint (`/{shortCode}`)

**Reasoning:**
Routing transforms the middleware pipeline into application endpoints.
By separating it from middleware, we mirror the real `UseRouting`/`UseEndpoints` pattern.

---

### **Phase 7 — HTTP Server (HttpListener Backend)**

**Goal:** Replace Kestrel with an HttpListener-based implementation.

**Key Features**

* Implement `IServer` interface
* Wrap `HttpListener` for HTTP/1.1
* Translate incoming requests into your `HttpContext`
* Invoke middleware pipeline
* Implement minimal `IHttpRequestFeature`, `IHttpResponseFeature`

**Reasoning:**
Although Kestrel is high-performance, the framework is designed to allow alternate `IServer` implementations.
Using `HttpListener` keeps it simple while demonstrating the server abstraction boundary.

---

### **Phase 8 — Mini ORM / Data Integration**

**Goal:** Replace EF Core with a lightweight reflection-based ORM.

**Key Features**

* CRUD via ADO.NET (`System.Data.SQLite`)
* Map rows ↔ objects via reflection
* Simple query builder (select/insert/update/delete)
* Optional change tracking

**Reasoning:**
Adds persistence and demonstrates DI integration with application services.
The goal is conceptual understanding of data mapping, not LINQ translation.

---

### **Phase 9 — Frontend Templating**

**Goal:** Replace Razor with a simple templating engine.

**Key Features**

* Load `.html` templates from disk
* Replace `{{variable}}` placeholders
* Optional loops/conditionals
* Render to string or stream

**Reasoning:**
Razor’s Roslyn integration is complex; a lightweight static template system demonstrates server-side rendering concepts without compiler complexity.

---

### **Phase 10 — Background Services**

**Goal:** Implement a minimal background service system to mirror `IHostedService` and `BackgroundService`.

**Key Features**

* `IHostedService` interface
* Host-managed lifecycle integration (start and stop)
* Example: `LinkCleanupService`

  * Runs hourly
  * Deletes expired links from database
  * Logs summary each run
  * Reads retention settings from configuration

**Reasoning:**
Completes the realism of the Host model — enabling recurring jobs, maintenance tasks, or message consumers.
In this project, it supports a **real, useful feature** of the baseline app: automatically removing expired short links.

---

### **Phase 11 — Cross-Cutting Concerns**

**Goal:** Add final polish and utility features.

**Key Features**

* Logging middleware
* Static file serving
* Error handling and graceful shutdown
* Config binding helpers
* Simple dependency graph diagnostics

**Reasoning:**
These make the framework usable end-to-end and demonstrate how cross-cutting concerns interact via DI + Middleware.

---

## 5. Key Abstractions Summary

| Interface          | Purpose              | Minimal Requirements       |
| ------------------ | -------------------- | -------------------------- |
| `IServiceProvider` | Dependency Injection | `GetService`, lifetimes    |
| `IConfiguration`   | Config Access        | Key lookup, sections       |
| `ILogger`          | Logging              | Levels, message formatting |
| `IHost`            | Composition root     | Lifecycle control          |
| `IServer`          | HTTP Server          | `StartAsync`, `StopAsync`  |
| `IHostedService`   | Background tasks     | `StartAsync`, `StopAsync`  |
| `RequestDelegate`  | Middleware link      | Async invocation           |

---

## 6. Testing & Validation Strategy

* Maintain the Phase 0 baseline as ground truth.
* After each phase:

  * Run existing integration tests against new layer.
  * Compare outputs (HTTP responses, logs, config values).
  * Benchmark startup and per-request overhead.
* Unit-test each component in isolation (e.g., routing table, DI resolution).

---

## 7. Expected Learning Outcomes

* Deep understanding of ASP.NET Core architecture.
* Experience with reflection, async I/O, and inversion of control.
* Practical grasp of how frameworks compose modular abstractions.
* Insight into where complexity hides in production frameworks (e.g., Kestrel pipelines, EF Core LINQ translation).
* Understanding of host lifecycle and background job integration.

---

## 8. Directory Structure

```
/src
  /MiniCore.Framework
    /DI
    /Config
    /Logging
    /Hosting
    /Server
    /Routing
    /Middleware
    /Background
  /MiniCore.Web
    /Controllers
    /Views
    /Program.cs
  /MiniCore.Tests
/docs
  SPEC.md
  diagrams/
```

---

## 9. Future Extensions

* Async I/O rewrite using `System.IO.Pipelines`
* HTTP/2 + TLS support
* Plugin system for third-party middleware
* Mini-SignalR (WebSocket server)
* Frontend Hot-Reload integration

---

## 10. Final Thoughts

This project is **feasible, cohesive, and highly educational**.
The URL Shortener baseline app ensures every layer — from configuration to DI, routing, templating, ORM, and background jobs — has a clear, realistic purpose.

By intentionally simplifying each subsystem while preserving its role and contract, you’ll recreate the essence of ASP.NET Core and develop a deep, working understanding of its architectural DNA.
