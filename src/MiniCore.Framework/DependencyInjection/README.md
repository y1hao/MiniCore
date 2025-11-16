# MiniCore.Framework.DependencyInjection

A minimal Dependency Injection container implementation that mirrors Microsoft's `Microsoft.Extensions.DependencyInjection` API while providing a custom implementation.

## Overview

This DI container provides the core abstractions and implementations for dependency injection in MiniCore, supporting service registration, resolution, lifetime management, and advanced features like open generics and circular dependency detection.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Code                         │
│  (Controllers, Services, Background Services)                │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ uses
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  Service Registration                       │
│  ServiceCollection → ServiceDescriptor[]                    │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ builds
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  Service Resolution                         │
│  ServiceProvider → ResolveService()                          │
│    ├─ Singleton Cache                                       │
│    ├─ Scoped Cache (per scope)                              │
│    └─ Transient (always new)                                │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ creates
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  Service Scopes                             │
│  ServiceScope → Scoped Instance Cache                       │
└─────────────────────────────────────────────────────────────┘
```

## Key Components

### Core Types

- **`ServiceLifetime`** - Enum defining service lifetimes (Singleton, Scoped, Transient)
- **`ServiceDescriptor`** - Describes a service registration (type, implementation, lifetime)
- **`ServiceCollection`** - Collection of service descriptors
- **`ServiceProvider`** - Service resolution engine with lifetime management
- **`ServiceScope`** - Isolated container for scoped services
- **`ServiceProviderOptions`** - Configuration options (validation, etc.)

### Interfaces

- **`IServiceProvider`** - Core service resolution interface
- **`IServiceCollection`** - Service registration interface
- **`IServiceScope`** - Scope interface for scoped lifetime management
- **`IServiceScopeFactory`** - Factory for creating scopes

## How It Works

### 1. Service Registration

Services are registered in a `ServiceCollection` using extension methods:

```csharp
var services = new ServiceCollection();

// Type-to-type registration
services.AddSingleton<IService, Service>();
services.AddScoped<IDbContext, DbContext>();
services.AddTransient<IMyService, MyService>();

// Instance registration
services.AddSingleton<IConfiguration>(configInstance);

// Factory registration
services.AddSingleton<IService>(sp => new Service(sp.GetRequiredService<IDependency>()));

// Open generic registration
services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
```

**Registration Process:**
1. Extension method creates a `ServiceDescriptor` with service type, implementation, and lifetime
2. Descriptor is added to the `ServiceCollection`
3. Multiple registrations for the same type are allowed (last one wins)

### 2. Service Lookup

When `GetService(Type)` is called, the provider follows this lookup order:

```
GetService(serviceType)
    │
    ├─► FindServiceDescriptor(serviceType)
    │   │
    │   ├─► Check exact match in ServiceCollection
    │   │
    │   ├─► Check cached closed generic descriptors
    │   │
    │   └─► If generic type: ResolveOpenGeneric()
    │       ├─► Find open generic registration (ILogger<>)
    │       ├─► Extract generic arguments from closed type (ILogger<MyClass>)
    │       ├─► Construct closed implementation type (Logger<MyClass>)
    │       ├─► Verify assignability
    │       └─► Cache descriptor for future lookups
    │
    └─► ResolveService(descriptor, serviceType, scopedInstances)
```

**Lookup Algorithm:**
1. **Exact Match**: Check if service type is registered exactly
2. **Cached Closed Generic**: Check if we've already resolved this closed generic
3. **Open Generic**: If service type is a closed generic, try to match an open generic registration
4. **Return null**: If no match found

### 3. Service Instantiation

Service instantiation follows this process:

```
ResolveService(descriptor, serviceType, scopedInstances)
    │
    ├─► Check Lifetime
    │   │
    │   ├─► Singleton
    │   │   ├─► Check singleton cache
    │   │   └─► If not cached: CreateServiceInstance() → Cache → Return
    │   │
    │   ├─► Scoped
    │   │   ├─► Check scoped cache (if in scope)
    │   │   └─► If not cached: CreateServiceInstance() → Cache → Return
    │   │
    │   └─► Transient
    │       └─► CreateServiceInstance() → Return (no caching)
    │
    └─► CreateServiceInstance(descriptor, serviceType, scopedInstances)
        │
        ├─► If ImplementationInstance: Return instance
        │
        ├─► If ImplementationFactory: Call factory with appropriate IServiceProvider
        │
        └─► If ImplementationType: CreateInstance() via constructor injection
            │
            ├─► Find best constructor (most resolvable parameters)
            │
            ├─► Resolve constructor parameters recursively
            │   └─► For each parameter: GetService(parameterType)
            │
            └─► Invoke constructor with resolved arguments
```

**Constructor Selection:**
- Scores constructors by number of resolvable parameters
- Selects constructor with most resolvable parameters where ALL parameters are resolvable
- Throws exception if no constructor has all resolvable parameters

### 4. Scoping

Scoping provides isolation for services with `Scoped` lifetime:

```csharp
using var scope = serviceProvider.CreateScope();
var scopedService = scope.ServiceProvider.GetRequiredService<IMyScopedService>();
// scopedService is disposed when scope is disposed
```

**How Scoping Works:**

1. **Scope Creation**:
   ```
   CreateScope()
       │
       └─► Create new Dictionary<Type, object> (scoped cache)
       └─► Create ServiceScope with scoped cache
   ```

2. **Scoped Resolution**:
   ```
   GetService(serviceType) [from scope]
       │
       └─► GetService(serviceType, scopedInstances)
           │
           └─► ResolveService(descriptor, serviceType, scopedInstances)
               │
               └─► If Scoped lifetime:
                   ├─► Check scopedInstances cache
                   ├─► If not cached: Create → Cache → Return
                   └─► Singleton services delegate to root provider
   ```

3. **Scope Disposal**:
   ```
   scope.Dispose()
       │
       └─► For each scoped instance:
           └─► If IDisposable: Dispose()
       └─► Clear scoped cache
   ```

**Key Points:**
- Each scope has its own cache of scoped instances
- Scoped services are isolated between scopes
- Singleton services are shared across all scopes
- Scoped services are disposed when scope is disposed

### 5. Open Generics

Open generics allow registering `ILogger<>` and resolving `ILogger<MyClass>`:

```csharp
// Registration
services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

// Resolution
var logger = provider.GetService<ILogger<MyService>>();
// Returns: Logger<MyService>
```

**Open Generic Resolution Process:**

```
ResolveOpenGeneric(ILogger<MyService>)
    │
    ├─► Extract generic type definition: ILogger<>
    ├─► Extract generic arguments: [MyService]
    │
    ├─► Find open generic registration:
    │   └─► ServiceType.IsGenericTypeDefinition == true
    │   └─► ServiceType == ILogger<>
    │   └─► ImplementationType.IsGenericTypeDefinition == true
    │
    ├─► Construct closed implementation type:
    │   └─► Logger<>.MakeGenericType([MyService]) → Logger<MyService>
    │
    ├─► Verify assignability:
    │   └─► ILogger<MyService>.IsAssignableFrom(Logger<MyService>)
    │
    ├─► Create descriptor for closed generic:
    │   └─► ServiceDescriptor.Describe(ILogger<MyService>, Logger<MyService>, Lifetime)
    │
    └─► Cache descriptor for future lookups
```

**Features:**
- Supports multiple type parameters: `IRepository<TKey, TValue>`
- Caches closed generic descriptors for performance
- Exact match (closed generic registration) takes precedence
- Validates generic argument count and assignability

### 6. Circular Dependency Detection

The container detects circular dependencies to prevent infinite loops:

```csharp
// This will throw InvalidOperationException
services.AddSingleton<A>(sp => new A(sp.GetRequiredService<B>()));
services.AddSingleton<B>(sp => new B(sp.GetRequiredService<A>()));
```

**Detection Mechanism:**

```
CreateInstance(type, scopedInstances, resolutionStack)
    │
    ├─► Check resolution stack (ThreadLocal HashSet<Type>)
    │   └─► If type already in stack: CIRCULAR DEPENDENCY!
    │
    ├─► Add type to resolution stack
    │
    ├─► Resolve constructor parameters:
    │   └─► For each parameter: GetService(parameterType)
    │       └─► (Recursive call, may add to resolution stack)
    │
    └─► Remove type from resolution stack (finally block)
```

**How It Works:**
1. Uses `ThreadLocal<HashSet<Type>>` to track resolution chain per thread
2. Before creating instance, checks if type is already in resolution stack
3. Adds type to stack before resolving dependencies
4. Removes type from stack after resolution (in finally block)
5. Throws `InvalidOperationException` with dependency chain if circular dependency detected

**Example Error:**
```
Circular dependency detected: A -> B -> C -> A
```

### 7. Lifetime Management

#### Singleton
- **Cache Location**: Root `ServiceProvider._singletons` dictionary
- **Thread Safety**: Double-check locking pattern
- **Lifetime**: Created once, shared across all scopes, disposed when provider disposed
- **Use Case**: Stateless services, configuration, expensive-to-create services

#### Scoped
- **Cache Location**: `ServiceScope._scopedInstances` dictionary (per scope)
- **Lifetime**: Created once per scope, disposed when scope disposed
- **Use Case**: Database contexts, unit of work, request-scoped services
- **Validation**: Can be configured to prevent resolving from root provider

#### Transient
- **Cache Location**: None (always creates new instance)
- **Lifetime**: New instance every time, disposed when out of scope (if IDisposable)
- **Use Case**: Lightweight, stateless services

### 8. Disposal

Services that implement `IDisposable` are automatically disposed:

**Singleton Disposal:**
```
ServiceProvider.Dispose()
    │
    └─► For each singleton in cache:
        └─► If IDisposable: Dispose()
    └─► Clear singleton cache
```

**Scoped Disposal:**
```
ServiceScope.Dispose()
    │
    └─► For each scoped instance in cache:
        └─► If IDisposable: Dispose() (swallows exceptions)
    └─► Clear scoped cache
```

**Important:**
- Disposal happens in reverse order of creation (LIFO)
- Exceptions during disposal are swallowed to ensure all services are disposed
- Transient services are disposed when they go out of scope (garbage collection)

## Usage Examples

### Basic Registration and Resolution

```csharp
var services = new ServiceCollection();
services.AddSingleton<IService, Service>();
services.AddScoped<IDbContext, DbContext>();
services.AddTransient<IMyService, MyService>();

var provider = services.BuildServiceProvider();

var service = provider.GetRequiredService<IService>();
```

### Using Scopes

```csharp
var provider = services.BuildServiceProvider();

using (var scope = provider.CreateScope())
{
    var scopedService = scope.ServiceProvider.GetRequiredService<IDbContext>();
    // Use scopedService...
} // scopedService is disposed here
```

### Open Generics

```csharp
services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

var logger = provider.GetService<ILogger<MyClass>>();
// Returns Logger<MyClass>
```

### Factory Registration

```csharp
services.AddSingleton<IService>(sp =>
{
    var dependency = sp.GetRequiredService<IDependency>();
    return new Service(dependency);
});
```

### Instance Registration

```csharp
var config = new Configuration();
services.AddSingleton<IConfiguration>(config);
```

## Validation Options

### ValidateScopes

Prevents resolving scoped services from root provider:

```csharp
var options = new ServiceProviderOptions { ValidateScopes = true };
var provider = services.BuildServiceProvider(options);

// This will throw InvalidOperationException
var scoped = provider.GetService<IScopedService>();
```

### ValidateOnBuild

Validates all services can be resolved during build:

```csharp
var options = new ServiceProviderOptions { ValidateOnBuild = true };
var provider = services.BuildServiceProvider(options);
// Throws if any service cannot be resolved
```

## Thread Safety

- **Singleton Creation**: Thread-safe using double-check locking
- **Scoped Instances**: Per-scope cache (not shared across threads)
- **Resolution Stack**: Thread-local (ThreadLocal<HashSet<Type>>)
- **Closed Generic Cache**: Thread-safe with locking

## Performance Considerations

1. **Caching**: Singleton and scoped instances are cached to avoid repeated creation
2. **Closed Generic Caching**: Closed generic descriptors are cached to avoid repeated type construction
3. **Constructor Selection**: Cached per type (via reflection)
4. **Lookup Order**: Exact match checked first, then cached generics, then open generics

## Limitations

- No support for property injection (constructor injection only)
- No support for optional dependencies (all dependencies must be registered)
- No support for named registrations (only type-based)
- No support for decorator pattern or interception
- Open generics require implementation type (no factory support for open generics)

## Testing

Comprehensive test suite with 70 tests covering:
- Service registration and resolution
- All three lifetimes
- Scoping and isolation
- Open generics
- Circular dependency detection
- Disposal
- Error handling
- Edge cases

See `MiniCore.Framework.Tests/DependencyInjection/` for test implementations.

## API Compatibility

This implementation maintains API compatibility with `Microsoft.Extensions.DependencyInjection`:
- Same interface signatures
- Same extension method names
- Same behavior for common scenarios
- Can be used as a drop-in replacement (with namespace change)

## Future Enhancements

Potential future improvements:
- Property injection support
- Optional dependencies
- Named registrations
- Decorator pattern support
- Performance optimizations (faster lookups, less reflection)
- Diagnostic APIs for debugging resolution issues

