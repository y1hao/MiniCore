# Phase 1: DI Implementation - Technical Design

## Detailed Implementation Plan

### 1. Project Structure

```
src/
└── MiniCore.Framework/
    ├── MiniCore.Framework.csproj
    └── DependencyInjection/
        ├── IServiceProvider.cs
        ├── IServiceCollection.cs
        ├── IServiceScope.cs
        ├── IServiceScopeFactory.cs
        ├── ServiceLifetime.cs
        ├── ServiceDescriptor.cs
        ├── ServiceCollection.cs
        ├── ServiceProvider.cs
        ├── ServiceScope.cs
        ├── ServiceProviderOptions.cs
        └── Extensions/
            ├── ServiceCollectionExtensions.cs
            └── ServiceProviderExtensions.cs
```

### 2. Core Types

#### ServiceLifetime.cs
```csharp
namespace MiniCore.Framework.DependencyInjection;

public enum ServiceLifetime
{
    Singleton = 0,
    Scoped = 1,
    Transient = 2
}
```

#### ServiceDescriptor.cs
```csharp
namespace MiniCore.Framework.DependencyInjection;

public class ServiceDescriptor
{
    public Type ServiceType { get; }
    public Type? ImplementationType { get; }
    public object? ImplementationInstance { get; }
    public Func<IServiceProvider, object>? ImplementationFactory { get; }
    public ServiceLifetime Lifetime { get; }

    // Constructors for different registration patterns
    public static ServiceDescriptor Singleton<TService, TImplementation>()
    public static ServiceDescriptor Singleton<TService>(TService instance)
    public static ServiceDescriptor Singleton<TService>(Func<IServiceProvider, TService> factory)
    public static ServiceDescriptor Scoped<TService, TImplementation>()
    public static ServiceDescriptor Scoped<TService>(Func<IServiceProvider, TService> factory)
    public static ServiceDescriptor Transient<TService, TImplementation>()
    public static ServiceDescriptor Transient<TService>(Func<IServiceProvider, TService> factory)
    public static ServiceDescriptor Describe(Type serviceType, Type implementationType, ServiceLifetime lifetime)
    public static ServiceDescriptor Describe(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime)
    public static ServiceDescriptor Describe(Type serviceType, object instance, ServiceLifetime lifetime)
}
```

### 3. Interfaces

#### IServiceProvider.cs
```csharp
namespace MiniCore.Framework.DependencyInjection;

public interface IServiceProvider
{
    object? GetService(Type serviceType);
}

public static class ServiceProviderExtensions
{
    public static T GetRequiredService<T>(this IServiceProvider provider);
    public static T? GetService<T>(this IServiceProvider provider);
    public static object GetRequiredService(this IServiceProvider provider, Type serviceType);
}
```

#### IServiceCollection.cs
```csharp
namespace MiniCore.Framework.DependencyInjection;

public interface IServiceCollection : IList<ServiceDescriptor>
{
}
```

#### IServiceScope.cs
```csharp
namespace MiniCore.Framework.DependencyInjection;

public interface IServiceScope : IDisposable
{
    IServiceProvider ServiceProvider { get; }
}
```

#### IServiceScopeFactory.cs
```csharp
namespace MiniCore.Framework.DependencyInjection;

public interface IServiceScopeFactory
{
    IServiceScope CreateScope();
}
```

### 4. Implementations

#### ServiceCollection.cs
```csharp
namespace MiniCore.Framework.DependencyInjection;

public class ServiceCollection : List<ServiceDescriptor>, IServiceCollection
{
    // Inherits all List<T> functionality
    // Extension methods provide AddSingleton, AddScoped, AddTransient
}
```

#### ServiceProvider.cs (Core Logic)

**Key Data Structures:**
```csharp
public class ServiceProvider : IServiceProvider, IServiceScopeFactory, IDisposable
{
    private readonly ServiceCollection _services;
    private readonly Dictionary<Type, object> _singletons;
    private readonly Dictionary<Type, object> _scopedInstances; // For root scope
    private readonly bool _validateScopes;
    private readonly bool _validateOnBuild;
    
    // For tracking resolution stack (circular dependency detection)
    private readonly ThreadLocal<HashSet<Type>> _resolutionStack;
}
```

**Key Methods:**

1. **GetService(Type serviceType)**
   - Check if service is registered
   - Handle open generics
   - Check lifetime and return appropriate instance
   - Resolve dependencies via constructor injection

2. **ResolveService(ServiceDescriptor descriptor, Type serviceType)**
   - Check singleton cache
   - Check scoped cache (if in scope)
   - Create new instance (transient or new scoped)
   - Use factory if provided
   - Use constructor injection if implementation type provided

3. **CreateInstance(Type implementationType, Type serviceType)**
   - Find best constructor (most parameters, all resolvable)
   - Resolve constructor parameters recursively
   - Detect circular dependencies
   - Instantiate with resolved dependencies

4. **ResolveOpenGeneric(Type serviceType)**
   - Extract generic type arguments from service type
   - Find matching open generic registration
   - Construct closed generic implementation type
   - Register and resolve

5. **GetConstructor(Type type)**
   - Get all public constructors
   - Score constructors by number of resolvable parameters
   - Return best match
   - Throw if no valid constructor found

#### ServiceScope.cs
```csharp
namespace MiniCore.Framework.DependencyInjection;

public class ServiceScope : IServiceScope
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Dictionary<Type, object> _scopedInstances;
    private bool _disposed;

    public IServiceProvider ServiceProvider { get; }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Dispose all scoped instances that implement IDisposable
            foreach (var instance in _scopedInstances.Values)
            {
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _scopedInstances.Clear();
            _disposed = true;
        }
    }
}
```

### 5. Extension Methods

#### ServiceCollectionExtensions.cs
```csharp
namespace MiniCore.Framework.DependencyInjection;

public static class ServiceCollectionExtensions
{
    // Singleton
    public static IServiceCollection AddSingleton<TService, TImplementation>(this IServiceCollection services)
    public static IServiceCollection AddSingleton<TService>(this IServiceCollection services, TService instance)
    public static IServiceCollection AddSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
    
    // Scoped
    public static IServiceCollection AddScoped<TService, TImplementation>(this IServiceCollection services)
    public static IServiceCollection AddScoped<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
    
    // Transient
    public static IServiceCollection AddTransient<TService, TImplementation>(this IServiceCollection services)
    public static IServiceCollection AddTransient<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
    
    // Open generics
    public static IServiceCollection AddSingleton(this IServiceCollection services, Type serviceType, Type implementationType)
    public static IServiceCollection AddScoped(this IServiceCollection services, Type serviceType, Type implementationType)
    public static IServiceCollection AddTransient(this IServiceCollection services, Type serviceType, Type implementationType)
    
    // Build service provider
    public static ServiceProvider BuildServiceProvider(this IServiceCollection services)
    public static ServiceProvider BuildServiceProvider(this IServiceCollection services, ServiceProviderOptions options)
}
```

#### ServiceProviderOptions.cs
```csharp
namespace MiniCore.Framework.DependencyInjection;

public class ServiceProviderOptions
{
    public bool ValidateScopes { get; set; }
    public bool ValidateOnBuild { get; set; }
}
```

### 6. Algorithm Details

#### Constructor Selection Algorithm

```csharp
private ConstructorInfo? GetBestConstructor(Type type)
{
    var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
    
    if (constructors.Length == 0)
        throw new InvalidOperationException($"No public constructors found for {type}");
    
    if (constructors.Length == 1)
        return constructors[0];
    
    // Score constructors by number of resolvable parameters
    var scoredConstructors = constructors
        .Select(c => new
        {
            Constructor = c,
            Parameters = c.GetParameters(),
            Score = c.GetParameters().Count(p => CanResolve(p.ParameterType))
        })
        .Where(x => x.Score == x.Parameters.Length) // All parameters must be resolvable
        .OrderByDescending(x => x.Score)
        .ToList();
    
    if (scoredConstructors.Count == 0)
        throw new InvalidOperationException($"No resolvable constructor found for {type}");
    
    return scoredConstructors[0].Constructor;
}
```

#### Circular Dependency Detection

```csharp
private object ResolveService(Type serviceType)
{
    // Check if already resolving this type (circular dependency)
    if (_resolutionStack.Value.Contains(serviceType))
    {
        throw new InvalidOperationException(
            $"Circular dependency detected: {string.Join(" -> ", _resolutionStack.Value)} -> {serviceType}");
    }
    
    _resolutionStack.Value.Add(serviceType);
    try
    {
        // Resolve service...
    }
    finally
    {
        _resolutionStack.Value.Remove(serviceType);
    }
}
```

#### Open Generic Resolution

```csharp
private ServiceDescriptor? ResolveOpenGeneric(Type serviceType)
{
    if (!serviceType.IsGenericType)
        return null;
    
    var genericTypeDefinition = serviceType.GetGenericTypeDefinition();
    var genericArguments = serviceType.GetGenericArguments();
    
    // Find matching open generic registration
    var descriptor = _services.FirstOrDefault(d =>
        d.ServiceType.IsGenericTypeDefinition &&
        d.ServiceType.GetGenericTypeDefinition() == genericTypeDefinition &&
        d.ImplementationType != null);
    
    if (descriptor == null)
        return null;
    
    // Construct closed generic implementation type
    var closedImplementationType = descriptor.ImplementationType!
        .MakeGenericType(genericArguments);
    
    // Create new descriptor for this closed generic
    return ServiceDescriptor.Describe(
        serviceType,
        closedImplementationType,
        descriptor.Lifetime);
}
```

### 7. Error Handling

**Common Exceptions:**
- `InvalidOperationException`: Service not registered, circular dependency, no valid constructor
- `ArgumentNullException`: Null arguments to methods
- `ObjectDisposedException`: Using disposed service provider/scope

**Error Messages:**
- "Unable to resolve service for type '{Type}'"
- "Circular dependency detected: {TypeChain}"
- "No public constructors found for type '{Type}'"
- "No resolvable constructor found for type '{Type}'"

### 8. Performance Considerations

1. **Caching:**
   - Singleton instances cached permanently
   - Scoped instances cached per scope
   - Constructor info cached (if needed)

2. **Lazy Resolution:**
   - Services resolved on-demand, not pre-built
   - Reduces startup time

3. **Thread Safety:**
   - Use `ThreadLocal` for resolution stack
   - Consider locking for singleton creation (double-check pattern)

### 9. Testing Checklist

#### Unit Tests

- [ ] Register and resolve simple type
- [ ] Register with instance
- [ ] Register with factory
- [ ] Transient lifetime - different instances
- [ ] Singleton lifetime - same instance
- [ ] Scoped lifetime - same in scope, different across scopes
- [ ] Constructor injection - single dependency
- [ ] Constructor injection - multiple dependencies
- [ ] Constructor injection - deep dependency chain
- [ ] Circular dependency detection
- [ ] Missing dependency error
- [ ] Open generic registration and resolution
- [ ] Service scope creation and disposal
- [ ] Disposed service provider throws exception
- [ ] Disposed scope disposes scoped services

#### Integration Tests

- [ ] Register services as in Program.cs
- [ ] Resolve controller with all dependencies
- [ ] Use scoped service in background service
- [ ] Verify scoped services are disposed correctly

### 10. Migration Notes

**Breaking Changes:** None (we're replacing Microsoft's implementation)

**Compatibility:**
- Must match Microsoft's API exactly
- Extension methods must have same signatures
- Behavior should match Microsoft's implementation

**Gradual Migration:**
1. Create MiniCore.Framework.DependencyInjection namespace
2. Implement all interfaces and classes
3. Update Program.cs to use `MiniCore.Framework.DependencyInjection` instead of `Microsoft.Extensions.DependencyInjection`
4. Verify all tests pass
5. Remove Microsoft.Extensions.DependencyInjection package reference

