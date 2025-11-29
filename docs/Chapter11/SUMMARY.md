# Chapter 11 Summary: Background Services

## What Was Implemented

Phase 11 documents the background service system that was implemented in Phase 4. This provides a minimal background service system to mirror `IHostedService` and enables recurring jobs, maintenance tasks, and long-running services.

### Core Components

1. **IHostedService Interface** (`Hosting/Abstractions/IHostedService.cs`)
   - `StartAsync(CancellationToken)` - Start the background service
   - `StopAsync(CancellationToken)` - Stop the background service gracefully

2. **Host Integration** (`Hosting/Host.cs`)
   - Automatically discovers all registered `IHostedService` instances
   - Starts services in registration order
   - Stops services in reverse registration order
   - Provides cancellation tokens for graceful shutdown

3. **Example Service: LinkCleanupService** (`MiniCore.Web/Services/LinkCleanupService.cs`)
   - Runs on configurable interval (default: hourly)
   - Deletes expired links from database
   - Logs summary of each cleanup run
   - Reads retention settings from configuration
   - Uses scoped services for database access

### Key Features

- ✅ `IHostedService` interface implementation
- ✅ Host-managed lifecycle integration
- ✅ Service discovery from DI container
- ✅ Graceful startup and shutdown
- ✅ Cancellation token support
- ✅ Scoped service access pattern
- ✅ Configuration-driven intervals
- ✅ Logging integration

## Files Created

```
docs/Chapter11/
├── README.md
└── SUMMARY.md
```

## Files Referenced

- `MiniCore.Framework/Hosting/Abstractions/IHostedService.cs` - Background service interface
- `MiniCore.Framework/Hosting/Host.cs` - Host manages lifecycle
- `MiniCore.Web/Services/LinkCleanupService.cs` - Example background service
- `MiniCore.Web/Program.cs` - Service registration

## Implementation Details

### Service Discovery

Host discovers `IHostedService` instances by:
1. Resolving `IEnumerable<IHostedService>` from service provider
2. Collecting all registered services
3. Starting them in registration order
4. Stopping them in reverse order

### Lifecycle Flow

**Startup:**
1. Discover all `IHostedService` instances
2. Call `StartAsync()` on each service in order
3. Trigger `ApplicationStarted` event

**Shutdown:**
1. Trigger `ApplicationStopping` event
2. Call `StopAsync()` on each service in reverse order
3. Handle errors gracefully (continue stopping other services)
4. Trigger `ApplicationStopped` event

### Scoped Services Pattern

Background services that need scoped services (like `DbContext`) should:
1. Inject `IServiceProvider` or `IServiceScopeFactory`
2. Create a scope for each operation
3. Resolve scoped services from the scope
4. Dispose the scope when done

### Cancellation Token Pattern

Background services should:
1. Create a linked cancellation token source in `StartAsync()`
2. Use the linked token for the background task
3. Cancel the linked token in `StopAsync()`
4. Wait for the task to complete or timeout

## Migration from Microsoft.Extensions.Hosting

### Changes Required

1. **Service Registration**
   - Changed from `AddHostedService<T>()` to `AddSingleton(typeof(IHostedService), typeof(T))`
   - Or use `AddSingleton<IHostedService, T>()` if supported

2. **Base Class**
   - Changed from `BackgroundService` base class to implementing `IHostedService` directly
   - Must implement cancellation token pattern manually

3. **Service Scope**
   - Use `IServiceScopeFactory` for scoped services (same pattern as Microsoft)

## Testing

- Unit tests for `LinkCleanupService`:
  - Expired links are removed
  - Active links are not removed
  - Configuration is read correctly
  - Logging works correctly
  - Scoped services work correctly

## Integration Points

- **DI**: Services registered via `IServiceCollection`
- **Configuration**: Services can read from `IConfiguration`
- **Logging**: Services can use `ILogger<T>`
- **Data Access**: Services use `IServiceScopeFactory` for scoped `DbContext`
- **Host**: Services managed by `Host` lifecycle

## Limitations

1. **BackgroundService Base Class**: Not implemented - services must implement `IHostedService` directly
2. **Service Health Monitoring**: No built-in health checks
3. **Service Dependencies**: No explicit dependency ordering
4. **Graceful Shutdown Timeout**: No configurable timeout

## Next Phase

**Phase 12: Testing Framework**
- Implement `WebApplicationFactory<T>`
- Implement `TestServer` for in-memory testing
- Support service replacement in tests
- Integration with xUnit

