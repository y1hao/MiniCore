# Chapter 4: Host Abstraction ✅

## Overview

Phase 4 implements a minimal Host abstraction to replace `Microsoft.Extensions.Hosting`. This provides the composition root that ties together the DI container, configuration sources, logging providers, and manages the application lifecycle with graceful startup and shutdown.

**Status:** ✅ Complete

## Goals

- Implement core hosting interfaces matching Microsoft's API surface
- Provide `HostBuilder` with fluent configuration API (`ConfigureServices`, `ConfigureLogging`, `ConfigureAppConfiguration`)
- Build unified `Host` object that composes DI + Config + Logging
- Support graceful startup and shutdown via `IHostApplicationLifetime`
- Enable background service lifecycle management
- Integrate with existing DI, Configuration, and Logging frameworks

## Key Requirements

### Interfaces to Implement

1. **IHost**
   - `Services` - Get the service provider (IServiceProvider)
   - `StartAsync(CancellationToken)` - Start the host
   - `StopAsync(CancellationToken)` - Stop the host
   - `Dispose()` - Dispose the host

2. **IHostBuilder**
   - `ConfigureServices(Action<IServiceCollection>)` - Configure services
   - `ConfigureLogging(Action<ILoggingBuilder>)` - Configure logging
   - `ConfigureAppConfiguration(Action<IConfigurationBuilder>)` - Configure configuration sources
   - `Build()` - Build the host instance
   - `Properties` - Dictionary for storing builder properties

3. **IHostApplicationLifetime**
   - `ApplicationStarted` - CancellationToken triggered when application has started
   - `ApplicationStopping` - CancellationToken triggered when application is stopping
   - `ApplicationStopped` - CancellationToken triggered when application has stopped
   - `StopApplication()` - Request application shutdown

4. **IHostedService** (for background services)
   - `StartAsync(CancellationToken)` - Start the background service
   - `StopAsync(CancellationToken)` - Stop the background service

### Core Features

1. **HostBuilder Pattern**
   - Fluent API for configuration
   - Chainable configuration methods
   - Builds unified Host object
   - Stores properties for cross-cutting concerns (e.g., environment name)

2. **Service Composition**
   - Automatically registers `IConfiguration`, `IConfigurationRoot`
   - Automatically registers `ILoggerFactory`, `ILogger<T>`
   - Automatically registers `IHostApplicationLifetime`
   - Allows custom service registration via `ConfigureServices`

3. **Lifecycle Management**
   - **StartAsync**: 
     - Builds service provider
     - Starts all registered `IHostedService` instances
     - Triggers `ApplicationStarted` token
   - **StopAsync**:
     - Triggers `ApplicationStopping` token
     - Stops all `IHostedService` instances
     - Disposes service provider
     - Triggers `ApplicationStopped` token

4. **Background Services**
   - Host automatically discovers and manages `IHostedService` registrations
   - Starts services in registration order
   - Stops services in reverse order
   - Provides cancellation tokens for graceful shutdown

## Architecture

```
MiniCore.Framework/
└── Hosting/
    ├── Abstractions/
    │   ├── IHost.cs                    # Core host interface
    │   ├── IHostBuilder.cs             # Builder interface
    │   ├── IHostApplicationLifetime.cs # Lifetime interface
    │   └── IHostedService.cs          # Background service interface
    ├── Host.cs                         # Host implementation
    ├── HostBuilder.cs                  # Builder implementation
    ├── HostApplicationLifetime.cs     # Lifetime implementation
    └── Extensions/
        └── HostBuilderExtensions.cs    # Extension methods
```

## Implementation Summary

Phase 4 successfully implements all core components:

### ✅ Core Types and Interfaces

- **IHost.cs** - Core host interface with lifecycle methods
- **IHostBuilder.cs** - Builder interface with configuration methods
- **IHostApplicationLifetime.cs** - Lifetime management interface
- **IHostedService.cs** - Background service interface

### ✅ Implementations

- **Host.cs** - Main host implementation:
  - Composes DI container, configuration, and logging
  - Manages lifecycle: StartAsync, StopAsync, Dispose
  - Discovers and manages IHostedService instances
  - Triggers lifetime events (ApplicationStarted, ApplicationStopping, ApplicationStopped)

- **HostBuilder.cs** - Builder implementation:
  - Fluent API: ConfigureServices, ConfigureLogging, ConfigureAppConfiguration
  - Stores configuration delegates
  - Builds unified Host object
  - Manages properties dictionary

- **HostApplicationLifetime.cs** - Lifetime implementation:
  - CancellationTokenSource for each lifecycle event
  - StopApplication() method to request shutdown
  - Thread-safe event triggering

### ✅ Extension Methods

- **HostBuilderExtensions.cs** - Builder extensions:
  - Convenience methods for common configurations
  - Integration with existing DI, Config, Logging frameworks

### Key Features Implemented

- **Fluent Configuration API**: Chainable Configure* methods
- **Service Composition**: Automatic registration of core services
- **Lifecycle Management**: Graceful startup and shutdown
- **Background Services**: Automatic discovery and management
- **Cancellation Support**: Cancellation tokens for all async operations
- **Thread Safety**: Thread-safe lifetime event triggering

## Current Usage Patterns

### Basic Host Creation

```csharp
var host = new HostBuilder()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IMyService, MyService>();
    })
    .ConfigureLogging(builder =>
    {
        builder.AddConsole(LogLevel.Information);
    })
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddJsonFile("appsettings.json");
        builder.AddEnvironmentVariables();
    })
    .Build();

await host.StartAsync();
// Application runs...
await host.StopAsync();
```

### Background Service Registration

```csharp
var host = new HostBuilder()
    .ConfigureServices(services =>
    {
        services.AddHostedService<LinkCleanupService>();
    })
    .Build();

await host.StartAsync();
// Background service runs automatically
await host.StopAsync();
// Background service stops gracefully
```

### Lifetime Events

```csharp
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("Application is stopping...");
});

// Request shutdown
lifetime.StopApplication();
```

## Testing Strategy

### Unit Tests

1. **HostBuilder Tests**
   - ConfigureServices adds services correctly
   - ConfigureLogging configures logging correctly
   - ConfigureAppConfiguration configures config correctly
   - Build creates Host instance
   - Properties dictionary works correctly

2. **Host Tests**
   - StartAsync builds service provider
   - StartAsync starts hosted services
   - StartAsync triggers ApplicationStarted
   - StopAsync stops hosted services
   - StopAsync triggers ApplicationStopping/Stopped
   - Dispose cleans up resources

3. **HostApplicationLifetime Tests**
   - ApplicationStarted token triggers correctly
   - ApplicationStopping token triggers correctly
   - ApplicationStopped token triggers correctly
   - StopApplication triggers stopping event

4. **HostedService Tests**
   - Hosted services start in registration order
   - Hosted services stop in reverse order
   - Cancellation tokens work correctly

### Integration Tests

1. **Real-world Scenarios**
   - Build host with DI, Config, Logging
   - Start host and verify services available
   - Background service runs correctly
   - Graceful shutdown works

## Migration Status

Phase 4 replaces the adapter pattern used in previous phases:

- ✅ Custom HostBuilder replaces `WebApplication.CreateBuilder()`
- ✅ Native integration with custom DI, Config, Logging
- ✅ No more adapter classes needed
- ✅ Cleaner Program.cs with fluent API
- ✅ Background services work natively

## Integration Details

### Program.cs Migration

**Before (Phase 3):**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseServiceProviderFactory(new ServiceProviderFactory());
var customConfiguration = ConfigurationFactory.CreateConfiguration(...);
builder.Services.AddSingleton<IConfiguration>(new ConfigurationAdapter(...));
var loggingFactory = new LoggerFactory();
builder.Services.AddSingleton<ILoggerFactory>(new LoggingFactoryAdapter(...));
```

**After (Phase 4):**
```csharp
var host = new HostBuilder()
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddJsonFile("appsettings.json");
        builder.AddEnvironmentVariables();
    })
    .ConfigureLogging(builder =>
    {
        builder.AddConsole(LogLevel.Information);
    })
    .ConfigureServices(services =>
    {
        services.AddDbContext<AppDbContext>(...);
        services.AddControllers();
        services.AddHostedService<LinkCleanupService>();
    })
    .Build();

await host.StartAsync();
```

## Success Criteria

- ✅ All interfaces match Microsoft's API surface
- ✅ HostBuilder fluent API works correctly
- ✅ Host composes DI + Config + Logging correctly
- ✅ Lifecycle management works (StartAsync/StopAsync)
- ✅ Background services start and stop correctly
- ✅ Lifetime events trigger correctly
- ✅ Unit tests for hosting framework pass
- ✅ No breaking changes to application code
- ✅ Program.cs uses native HostBuilder

## Known Limitations

### Web Host vs Generic Host

**Status:** Generic Host only (no WebHost)

**Current Behavior:** We implement `IHost` and `IHostBuilder` which are generic hosting abstractions. We don't implement `IWebHost` and `IWebHostBuilder` which are ASP.NET Core specific.

**Real-world Usage:** ASP.NET Core 3.0+ uses generic host for both web apps and non-web apps. Web-specific features (middleware, routing) will be added in later phases.

**Future Enhancement:** Add `IWebHost` and `IWebHostBuilder` in Phase 5 (Middleware Pipeline).

### Environment Name

**Status:** Basic environment support

**Current Behavior:** Environment name can be set via HostBuilder properties, but there's no built-in `IWebHostEnvironment` equivalent.

**Future Enhancement:** Add `IHostEnvironment` interface with `EnvironmentName`, `ApplicationName`, `ContentRootPath` properties.

### Configuration Reloading

**Status:** Configuration is built once at host build time

**Current Behavior:** Configuration sources are built when `Build()` is called. Changes to configuration files are not automatically reloaded.

**Real-world Usage:** Production apps often need configuration reloading for dynamic configuration updates.

**Future Enhancement:** Implement configuration reloading via `IChangeToken` (already supported in Configuration framework).

## Key Implementation Details

### Host Composition Flow

When `HostBuilder.Build()` is called:

1. **Build Configuration**: Execute all `ConfigureAppConfiguration` delegates
2. **Build Logging**: Execute all `ConfigureLogging` delegates
3. **Register Core Services**: Add IConfiguration, IConfigurationRoot, ILoggerFactory to service collection
4. **Configure Services**: Execute all `ConfigureServices` delegates
5. **Build Service Provider**: Create IServiceProvider from service collection
6. **Create Host**: Instantiate Host with service provider and lifetime

### Lifecycle Flow

When `Host.StartAsync()` is called:

1. **Build Service Provider** (if not already built)
2. **Resolve IHostApplicationLifetime** from service provider
3. **Discover IHostedService instances** from service provider
4. **Start Hosted Services** in registration order
5. **Trigger ApplicationStarted** token

When `Host.StopAsync()` is called:

1. **Trigger ApplicationStopping** token
2. **Stop Hosted Services** in reverse registration order
3. **Dispose Service Provider**
4. **Trigger ApplicationStopped** token

### Background Service Management

Host discovers `IHostedService` instances by:

1. Resolving all registered `IHostedService` instances from service provider
2. Starting them in registration order (first registered = first started)
3. Stopping them in reverse order (last registered = first stopped)
4. Passing cancellation tokens for graceful shutdown

## Next Steps

Phase 4 is complete. Next phases:

- **Phase 5**: Middleware Pipeline (will integrate with Host for request handling)
- **Phase 6**: Routing Framework (will use Host's service provider)
- **Phase 7**: HTTP Server (will be started by Host)
- **Phase 10**: Background Services (already supported, but will add more features)

## References

- [Microsoft.Extensions.Hosting Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host)
- [ASP.NET Core Host](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host)

