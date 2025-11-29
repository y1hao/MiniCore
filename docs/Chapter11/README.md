# Chapter 11: Background Services ✅

## Overview

Phase 11 documents the background service system that was implemented in Phase 4. This provides a minimal background service system to mirror `IHostedService` and enables recurring jobs, maintenance tasks, and long-running services that run independently of HTTP requests.

**Status:** ✅ Complete (implemented in Phase 4)

## Goals

- Document the `IHostedService` interface implementation
- Explain host-managed lifecycle integration (start and stop)
- Provide example implementation: `LinkCleanupService`
- Demonstrate configuration-driven background services
- Show integration with DI, logging, and data access

## Key Requirements

### IHostedService Interface

1. **IHostedService**
   - `StartAsync(CancellationToken)` - Start the background service
   - `StopAsync(CancellationToken)` - Stop the background service gracefully

2. **Host Integration**
   - Host automatically discovers all registered `IHostedService` instances
   - Services start in registration order
   - Services stop in reverse registration order
   - Cancellation tokens provided for graceful shutdown

3. **Example Service: LinkCleanupService**
   - Runs on a configurable interval (default: hourly)
   - Deletes expired links from database
   - Logs summary of each cleanup run
   - Reads retention settings from configuration
   - Uses scoped services for database access

## Architecture

```
MiniCore.Framework/
└── Hosting/
    ├── Abstractions/
    │   └── IHostedService.cs          # Background service interface
    └── Host.cs                         # Host manages lifecycle
```

## Implementation Summary

Phase 11 (documented from Phase 4 implementation) successfully provides all core background service components:

### ✅ Core Interface

- **IHostedService.cs** - Background service interface:
  - `StartAsync(CancellationToken)` - Called when host starts
  - `StopAsync(CancellationToken)` - Called when host stops

### ✅ Host Integration

- **Host.cs** - Manages background services:
  - Discovers all `IHostedService` instances from DI container
  - Starts services in registration order during `StartAsync()`
  - Stops services in reverse order during `StopAsync()`
  - Provides cancellation tokens for graceful shutdown
  - Handles errors during shutdown gracefully

### ✅ Example Implementation

- **LinkCleanupService.cs** - Real-world background service:
  - Implements `IHostedService` interface
  - Runs cleanup task on configurable interval
  - Uses `IServiceScopeFactory` for scoped database access
  - Integrates with logging and configuration
  - Handles errors gracefully

## Current Usage Patterns

### Basic Background Service

```csharp
public class MyBackgroundService : IHostedService
{
    private readonly ILogger<MyBackgroundService> _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _executingTask;

    public MyBackgroundService(ILogger<MyBackgroundService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask == null)
        {
            return;
        }

        _cancellationTokenSource?.Cancel();
        await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MyBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Do work here
                await DoWork(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during background work");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("MyBackgroundService stopped");
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        // Background work implementation
    }
}
```

### Service Registration

```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Services.AddHostedService<MyBackgroundService>();
// Or using AddSingleton for IHostedService
builder.Services.AddSingleton(typeof(IHostedService), typeof(MyBackgroundService));

var app = builder.Build();
await app.RunAsync();
```

### LinkCleanupService Example

```csharp
public class LinkCleanupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LinkCleanupService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _executingTask;

    public LinkCleanupService(
        IServiceProvider serviceProvider,
        ILogger<LinkCleanupService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        // Read interval from configuration
        var intervalHoursStr = _configuration["LinkCleanup:IntervalHours"];
        var intervalHours = int.TryParse(intervalHoursStr, out var hours) ? hours : 1;
        _interval = TimeSpan.FromHours(intervalHours);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask == null)
        {
            return;
        }

        _cancellationTokenSource?.Cancel();
        await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LinkCleanupService started. Cleanup interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredLinks(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during link cleanup");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("LinkCleanupService stopped");
    }

    private async Task CleanupExpiredLinks(CancellationToken cancellationToken)
    {
        // Use scoped services for database access
        var scopeFactory = _serviceProvider.GetService<IServiceScopeFactory>();
        if (scopeFactory == null)
        {
            _logger.LogError("IServiceScopeFactory is not available");
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var expiredLinks = await context.ShortLinks
            .Where(l => l.ExpiresAt.HasValue && l.ExpiresAt.Value < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (expiredLinks.Count > 0)
        {
            context.ShortLinks.RemoveRange(expiredLinks);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Cleaned up {Count} expired link(s). Removed short codes: {ShortCodes}",
                expiredLinks.Count,
                string.Join(", ", expiredLinks.Select(l => l.ShortCode)));
        }
        else
        {
            _logger.LogDebug("No expired links found to clean up");
        }
    }
}
```

### Configuration

```json
{
  "LinkCleanup": {
    "IntervalHours": 1
  }
}
```

## Testing Strategy

### Unit Tests

1. **IHostedService Tests**
   - Service starts correctly
   - Service stops gracefully
   - Cancellation tokens work correctly
   - Errors during shutdown are handled

2. **LinkCleanupService Tests**
   - Expired links are removed
   - Active links are not removed
   - Configuration is read correctly
   - Logging works correctly
   - Scoped services work correctly

### Integration Tests

1. **Host Integration Tests**
   - Multiple services start in correct order
   - Multiple services stop in reverse order
   - Services can access DI services
   - Services can access configuration
   - Services can access logging

## Success Criteria

- ✅ `IHostedService` interface implemented
- ✅ Host discovers and manages background services
- ✅ Services start in registration order
- ✅ Services stop in reverse order
- ✅ Cancellation tokens work correctly
- ✅ LinkCleanupService example implemented
- ✅ Configuration-driven intervals work
- ✅ Scoped services work correctly
- ✅ Logging integration works
- ✅ Unit tests pass
- ✅ Integration tests pass

## Known Limitations

### BackgroundService Base Class

**Status:** Not implemented

**Current Behavior:** Services must implement `IHostedService` directly. There's no `BackgroundService` base class that provides common patterns.

**Real-world Usage:** ASP.NET Core provides a `BackgroundService` abstract base class that simplifies common patterns like long-running tasks with cancellation support.

**Future Enhancement:** Add `BackgroundService` base class with common patterns:
```csharp
public abstract class BackgroundService : IHostedService
{
    protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
    
    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        _executingTask = ExecuteAsync(cancellationToken);
        return Task.CompletedTask;
    }
    
    // ... StopAsync implementation
}
```

### Service Health Monitoring

**Status:** Not implemented

**Current Behavior:** No built-in health monitoring for background services. Services must implement their own health checks.

**Future Enhancement:** Add health check integration for background services.

### Service Dependencies

**Status:** Basic support

**Current Behavior:** Services can depend on other services via constructor injection, but there's no explicit dependency ordering.

**Future Enhancement:** Add support for service dependencies and startup ordering.

### Graceful Shutdown Timeout

**Status:** Basic implementation

**Current Behavior:** Services are stopped with cancellation tokens, but there's no timeout for graceful shutdown.

**Future Enhancement:** Add configurable timeout for graceful shutdown.

## Key Implementation Details

### Service Discovery

Host discovers `IHostedService` instances by:

1. Resolving `IEnumerable<IHostedService>` from service provider
2. Collecting all registered services
3. Starting them in registration order
4. Stopping them in reverse order

### Lifecycle Flow

When `Host.StartAsync()` is called:

1. **Discover Services**: Resolve all `IHostedService` instances
2. **Start Services**: Call `StartAsync()` on each service in order
3. **Trigger Started Event**: Notify that host has started

When `Host.StopAsync()` is called:

1. **Trigger Stopping Event**: Notify that host is stopping
2. **Stop Services**: Call `StopAsync()` on each service in reverse order
3. **Handle Errors**: Continue stopping other services even if one fails
4. **Trigger Stopped Event**: Notify that host has stopped

### Scoped Services Pattern

Background services that need scoped services (like `DbContext`) should:

1. Inject `IServiceProvider` or `IServiceScopeFactory`
2. Create a scope for each operation
3. Resolve scoped services from the scope
4. Dispose the scope when done

Example:
```csharp
var scopeFactory = _serviceProvider.GetService<IServiceScopeFactory>();
using var scope = scopeFactory.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
// Use context...
```

### Cancellation Token Pattern

Background services should:

1. Create a linked cancellation token source in `StartAsync()`
2. Use the linked token for the background task
3. Cancel the linked token in `StopAsync()`
4. Wait for the task to complete or timeout

Example:
```csharp
public Task StartAsync(CancellationToken cancellationToken)
{
    _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    _executingTask = ExecuteAsync(_cancellationTokenSource.Token);
    return Task.CompletedTask;
}

public async Task StopAsync(CancellationToken cancellationToken)
{
    _cancellationTokenSource?.Cancel();
    await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
}
```

## Migration from Microsoft.Extensions.Hosting

The following patterns were migrated:

| Microsoft Pattern | MiniCore Pattern |
|-------------------|-----------------|
| `BackgroundService` base class | Implement `IHostedService` directly |
| `AddHostedService<T>()` | `AddSingleton(typeof(IHostedService), typeof(T))` |
| `IHostApplicationLifetime` | `IHostApplicationLifetime` (same interface) |
| Service scope in background service | Use `IServiceScopeFactory` |

## Next Steps

Phase 11 is complete. Remaining phases:

- **Phase 12**: Testing Framework

## References

- [Microsoft.Extensions.Hosting Background Services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)
- [BackgroundService Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice)
- [IHostedService Interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostedservice)

