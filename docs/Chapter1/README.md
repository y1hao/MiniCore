# Chapter 1: Dependency Injection Framework

## Overview

Phase 1 focuses on implementing a minimal Dependency Injection (DI) container to replace `Microsoft.Extensions.DependencyInjection`. This is the foundation that all other framework components will build upon.

## Goals

- Implement core DI interfaces matching Microsoft's API surface
- Support three service lifetimes: Transient, Scoped, and Singleton
- Enable constructor injection with automatic dependency resolution
- Support open-generic types (e.g., `ILogger<T>`)
- Provide service scope management for scoped lifetime services

## Key Requirements

### Interfaces to Implement

1. **IServiceProvider**
   - `GetService(Type serviceType)` - Resolve service by type
   - `GetRequiredService(Type serviceType)` - Resolve service, throw if not found

2. **IServiceCollection**
   - Collection of service descriptors
   - Methods: `Add`, `AddSingleton`, `AddScoped`, `AddTransient`
   - Support for factory-based registration

3. **IServiceScope**
   - Represents a scoped service container
   - `ServiceProvider` property
   - `Dispose()` for cleanup

4. **IServiceScopeFactory**
   - `CreateScope()` - Create new service scope

5. **ServiceLifetime Enum**
   - `Transient` - New instance every time
   - `Scoped` - One instance per scope
   - `Singleton` - One instance for the lifetime of the root container

### Core Features

1. **Service Registration**
   - Type-to-type registration (`AddTransient<IService, Implementation>`)
   - Instance registration (`AddSingleton<IService>(instance)`)
   - Factory registration (`AddTransient<IService>(sp => new Implementation())`)
   - Open-generic registration (`AddTransient(typeof(ILogger<>), typeof(Logger<>))`)

2. **Service Resolution**
   - Constructor injection with automatic dependency resolution
   - Circular dependency detection
   - Missing dependency error messages
   - Support for optional dependencies

3. **Lifetime Management**
   - **Transient**: Create new instance on each resolution
   - **Scoped**: Create one instance per scope, reuse within scope
   - **Singleton**: Create once, reuse across all scopes

4. **Open Generic Support**
   - Register `ILogger<>` → `Logger<>`
   - Resolve `ILogger<MyClass>` → `Logger<MyClass>`
   - Match generic type parameters correctly

## Architecture

```
MiniCore.Framework/
└── DependencyInjection/
    ├── IServiceProvider.cs          # Core service provider interface
    ├── IServiceCollection.cs        # Service registration interface
    ├── IServiceScope.cs             # Scope interface
    ├── IServiceScopeFactory.cs      # Scope factory interface
    ├── ServiceLifetime.cs           # Lifetime enum
    ├── ServiceDescriptor.cs         # Service registration descriptor
    ├── ServiceCollection.cs         # Implementation of IServiceCollection
    ├── ServiceProvider.cs           # Implementation of IServiceProvider
    ├── ServiceScope.cs              # Implementation of IServiceScope
    └── ServiceProviderOptions.cs    # Configuration options
```

## Implementation Plan

### Step 1: Core Types and Interfaces

1. **ServiceLifetime.cs**
   - Define enum: Transient, Scoped, Singleton

2. **ServiceDescriptor.cs**
   - Service type, implementation type/factory/instance
   - Lifetime
   - Factory delegate support

3. **IServiceProvider.cs**
   - `object? GetService(Type serviceType)`
   - Extension: `T GetRequiredService<T>()`

4. **IServiceCollection.cs**
   - Extends `IList<ServiceDescriptor>`
   - Extension methods: `Add`, `AddSingleton`, `AddScoped`, `AddTransient`

5. **IServiceScope.cs**
   - `IServiceProvider ServiceProvider { get; }`
   - `void Dispose()`

6. **IServiceScopeFactory.cs**
   - `IServiceScope CreateScope()`

### Step 2: Service Collection Implementation

1. **ServiceCollection.cs**
   - Implement `IServiceCollection`
   - Store list of `ServiceDescriptor`
   - Provide registration methods

### Step 3: Service Provider Implementation

1. **ServiceProvider.cs**
   - Store service descriptors
   - Track singleton instances
   - Track scoped instances (per scope)
   - Constructor injection resolver
   - Open generic resolver

2. **Key Algorithms:**
   - **Constructor Selection**: Choose constructor with most resolvable parameters
   - **Dependency Resolution**: Recursive resolution with cycle detection
   - **Open Generic Matching**: Match `ILogger<T>` to `Logger<T>` when resolving `ILogger<MyClass>`

### Step 4: Service Scope Implementation

1. **ServiceScope.cs**
   - Own scoped instance cache
   - Dispose scoped instances on disposal
   - Delegate singleton resolution to root provider

### Step 5: Extension Methods

1. **ServiceCollectionExtensions.cs**
   - `AddSingleton<T>()`, `AddSingleton<T>(T instance)`, `AddSingleton<T>(Func<IServiceProvider, T> factory)`
   - `AddScoped<T>()`, `AddScoped<T>(Func<IServiceProvider, T> factory)`
   - `AddTransient<T>()`, `AddTransient<T>(Func<IServiceProvider, T> factory)`
   - Open generic variants

2. **ServiceProviderExtensions.cs**
   - `GetRequiredService<T>()`
   - `GetService<T>()`

## Current Usage Analysis

From the baseline application, we need to support:

1. **Constructor Injection**
   ```csharp
   public ShortLinkController(AppDbContext context, ILogger<ShortLinkController> logger, IConfiguration configuration)
   ```

2. **Scoped Services**
   ```csharp
   builder.Services.AddDbContext<AppDbContext>(options => ...);
   ```

3. **Singleton Services**
   ```csharp
   builder.Services.AddHostedService<LinkCleanupService>();
   ```

4. **Service Scopes**
   ```csharp
   using var scope = _serviceProvider.CreateScope();
   var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
   ```

5. **Open Generics**
   ```csharp
   ILogger<ShortLinkController> logger  // Resolves to Logger<ShortLinkController>
   ```

## Testing Strategy

### Unit Tests

1. **Service Registration Tests**
   - Register and resolve simple types
   - Register with different lifetimes
   - Register with factory functions
   - Register open generics

2. **Lifetime Tests**
   - Transient: Different instances each time
   - Scoped: Same instance within scope, different across scopes
   - Singleton: Same instance across all scopes

3. **Constructor Injection Tests**
   - Simple dependency chain
   - Multiple dependencies
   - Optional dependencies
   - Circular dependency detection

4. **Open Generic Tests**
   - Register `ILogger<>` → `Logger<>`
   - Resolve `ILogger<MyClass>` → `Logger<MyClass>`
   - Multiple generic parameters

5. **Scope Tests**
   - Create scope and resolve scoped services
   - Dispose scope and verify cleanup
   - Nested scopes (if supported)

### Integration Tests

1. **Real-world Scenarios**
   - Register services as in Program.cs
   - Resolve controllers with dependencies
   - Use scoped services in background services

## Migration Strategy

### Phase 1.1: Create Framework Structure
- Create `MiniCore.Framework` project
- Create `DI` namespace and folder structure
- Add project to solution

### Phase 1.2: Implement Core Interfaces
- Define all interfaces matching Microsoft's API
- Ensure API compatibility

### Phase 1.3: Implement Basic Container
- Basic service registration and resolution
- Support for Transient and Singleton lifetimes
- Simple constructor injection

### Phase 1.4: Add Scoped Lifetime Support
- Implement `IServiceScope` and `IServiceScopeFactory`
- Add scoped instance tracking

### Phase 1.5: Add Open Generic Support
- Implement generic type matching
- Support `ILogger<T>` pattern

### Phase 1.6: Testing and Validation
- Comprehensive unit tests
- Integration tests with baseline app
- Performance considerations

### Phase 1.7: Documentation
- API documentation
- Usage examples
- Migration guide

## Success Criteria

- ✅ All core interfaces implemented and match Microsoft's API surface
- ✅ All three lifetimes work correctly
- ✅ Constructor injection works for all current use cases
- ✅ Open generics work (`ILogger<T>`)
- ✅ Service scopes work correctly
- ✅ All existing tests pass with new DI container
- ✅ No breaking changes to application code

## Next Steps

After Phase 1 completion:
- Phase 2: Configuration Framework (will use DI for registering configuration sources)
- Phase 3: Logging Framework (will use DI and open generics)
- Phase 4: Host Abstraction (will use DI as composition root)

## References

- [Microsoft.Extensions.DependencyInjection Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [ASP.NET Core Dependency Injection](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)

