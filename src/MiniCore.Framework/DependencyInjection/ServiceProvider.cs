using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace MiniCore.Framework.DependencyInjection;

/// <summary>
/// Default implementation of <see cref="IServiceProvider"/> and <see cref="IServiceScopeFactory"/>.
/// </summary>
/// <remarks>
/// <para>
/// This service provider resolves services by:
/// <list type="number">
/// <item>Looking up the service registration in the service collection</item>
/// <item>Checking lifetime caches (Singleton, Scoped)</item>
/// <item>Resolving dependencies recursively with circular dependency detection</item>
/// <item>Creating instances using constructor injection</item>
/// </list>
/// </para>
/// </remarks>
public class ServiceProvider : IServiceProvider, System.IServiceProvider, IServiceScopeFactory, IDisposable
{
    private readonly ServiceCollection _services;
    private readonly ServiceProviderOptions _options;
    private readonly Dictionary<Type, object> _singletons;
    private readonly Dictionary<Type, ServiceDescriptor> _closedGenericDescriptors = new Dictionary<Type, ServiceDescriptor>();
    private readonly object _singletonLock = new object();
    private readonly object _closedGenericLock = new object();
    private readonly ThreadLocal<HashSet<Type>> _resolutionStack = new ThreadLocal<HashSet<Type>>(() => new HashSet<Type>());
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="ServiceProvider"/>.
    /// </summary>
    /// <param name="services">The service collection containing registered services.</param>
    /// <param name="options">Optional service provider options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public ServiceProvider(ServiceCollection services, ServiceProviderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
        _options = options ?? new ServiceProviderOptions();
        _singletons = new Dictionary<Type, object>();

        if (_options.ValidateOnBuild)
        {
            ValidateOnBuild();
        }
    }

    /// <summary>
    /// Gets the service object of the specified type.
    /// </summary>
    /// <param name="serviceType">An object that specifies the type of service object to get.</param>
    /// <returns>
    /// A service object of type <paramref name="serviceType"/>.
    /// Returns null if there is no service object of type <paramref name="serviceType"/>.
    /// </returns>
    public object? GetService(Type serviceType)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(serviceType);

        return GetService(serviceType, null);
    }

    /// <summary>
    /// Internal method to get service with optional scoped cache for scoped lifetime services.
    /// </summary>
    internal object? GetService(Type serviceType, Dictionary<Type, object>? scopedInstances)
    {
        // Special handling for IServiceProvider and IServiceScopeFactory - always return this provider
        if (serviceType == typeof(IServiceProvider) || serviceType == typeof(System.IServiceProvider))
        {
            if (scopedInstances != null)
            {
                return new ScopedServiceProviderWrapper(this, scopedInstances);
            }
            return this;
        }
        
        if (serviceType == typeof(IServiceScopeFactory))
        {
            return this; // ServiceProvider implements IServiceScopeFactory
        }
        
        // Handle IEnumerable<T> - Microsoft DI returns all registered services of that type
        if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elementType = serviceType.GetGenericArguments()[0];
            // Find all descriptors that match the element type
            var descriptors = _services.Where(d => d.ServiceType == elementType).ToList();
            
            if (descriptors.Count > 0)
            {
                // Return array with all registered services
                var array = Array.CreateInstance(elementType, descriptors.Count);
                for (int i = 0; i < descriptors.Count; i++)
                {
                    var item = ResolveService(descriptors[i], elementType, scopedInstances);
                    array.SetValue(item, i);
                }
                return array;
            }
            else
            {
                // Return empty collection if nothing registered
                return Array.CreateInstance(elementType, 0);
            }
        }
        
        // Find service descriptor
        var serviceDescriptor = FindServiceDescriptor(serviceType);
        if (serviceDescriptor == null)
        {
            return null;
        }

        return ResolveService(serviceDescriptor, serviceType, scopedInstances);
    }

    /// <summary>
    /// Creates a new <see cref="IServiceScope"/>.
    /// </summary>
    /// <returns>A new <see cref="IServiceScope"/>.</returns>
    public IServiceScope CreateScope()
    {
        ThrowIfDisposed();
        return new ServiceScope(this, new Dictionary<Type, object>());
    }

    /// <summary>
    /// Disposes the service provider and any singleton services that implement <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_singletonLock)
            {
                foreach (var singleton in _singletons.Values)
                {
                    if (singleton is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _singletons.Clear();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Finds the service descriptor for the given service type.
    /// </summary>
    /// <param name="serviceType">The service type to find.</param>
    /// <returns>The service descriptor, or null if not found.</returns>
    private ServiceDescriptor? FindServiceDescriptor(Type serviceType)
    {
        // Check for exact match first
        var descriptor = _services.LastOrDefault(d => d.ServiceType == serviceType);
        if (descriptor != null)
        {
            return descriptor;
        }

        // Check cached closed generic descriptors
        lock (_closedGenericLock)
        {
            if (_closedGenericDescriptors.TryGetValue(serviceType, out var cachedDescriptor))
            {
                return cachedDescriptor;
            }
        }

        // Handle open generics
        if (serviceType.IsGenericType && !serviceType.IsGenericTypeDefinition)
        {
            return ResolveOpenGeneric(serviceType);
        }

        return null;
    }

    /// <summary>
    /// Resolves an open generic registration for a closed generic service type.
    /// </summary>
    /// <param name="closedGenericServiceType">The closed generic service type (e.g., ILogger&lt;MyClass&gt;).</param>
    /// <returns>The service descriptor for the closed generic, or null if no matching open generic is registered.</returns>
    private ServiceDescriptor? ResolveOpenGeneric(Type closedGenericServiceType)
    {
        var genericTypeDefinition = closedGenericServiceType.GetGenericTypeDefinition();
        var genericArguments = closedGenericServiceType.GetGenericArguments();

        // Find matching open generic registration
        var openGenericDescriptor = _services.LastOrDefault(d =>
            d.ServiceType.IsGenericTypeDefinition &&
            d.ServiceType == genericTypeDefinition &&
            d.ImplementationType != null &&
            d.ImplementationType.IsGenericTypeDefinition);

        if (openGenericDescriptor == null)
        {
            return null;
        }

        // Construct closed generic implementation type
        Type closedImplementationType;
        try
        {
            closedImplementationType = openGenericDescriptor.ImplementationType!.MakeGenericType(genericArguments);
        }
        catch (ArgumentException)
        {
            // Generic arguments don't match (e.g., wrong number of type parameters)
            return null;
        }

        // Verify the closed implementation type is assignable to the closed service type
        if (!closedGenericServiceType.IsAssignableFrom(closedImplementationType))
        {
            return null;
        }

        // Create descriptor for closed generic
        var closedDescriptor = ServiceDescriptor.Describe(
            closedGenericServiceType,
            closedImplementationType,
            openGenericDescriptor.Lifetime);

        // Cache the closed generic descriptor
        lock (_closedGenericLock)
        {
            _closedGenericDescriptors[closedGenericServiceType] = closedDescriptor;
        }

        return closedDescriptor;
    }

    /// <summary>
    /// Resolves a service from a descriptor with lifetime management.
    /// </summary>
    /// <param name="descriptor">The service descriptor.</param>
    /// <param name="serviceType">The service type being requested.</param>
    /// <param name="scopedInstances">Optional scoped instances cache.</param>
    /// <returns>The resolved service instance.</returns>
    private object ResolveService(ServiceDescriptor descriptor, Type serviceType, Dictionary<Type, object>? scopedInstances)
    {
        // Handle different lifetimes
        switch (descriptor.Lifetime)
        {
            case ServiceLifetime.Singleton:
                return ResolveSingleton(descriptor, serviceType);

            case ServiceLifetime.Scoped:
                if (scopedInstances == null)
                {
                    if (_options.ValidateScopes)
                    {
                        throw new InvalidOperationException(
                            $"Cannot resolve scoped service '{serviceType}' from root service provider.");
                    }
                    // Fall back to creating a new instance if scope validation is disabled
                    return CreateServiceInstance(descriptor, serviceType, scopedInstances);
                }
                return ResolveScoped(descriptor, serviceType, scopedInstances);

            case ServiceLifetime.Transient:
                return CreateServiceInstance(descriptor, serviceType, scopedInstances);

            default:
                throw new InvalidOperationException(
                    $"Unknown service lifetime: {descriptor.Lifetime}");
        }
    }

    /// <summary>
    /// Resolves a singleton service, creating it if necessary.
    /// </summary>
    private object ResolveSingleton(ServiceDescriptor descriptor, Type serviceType)
    {
        // Check cache first
        lock (_singletonLock)
        {
            if (_singletons.TryGetValue(serviceType, out var cached))
            {
                return cached;
            }
        }

        // Create instance (double-check locking pattern)
        lock (_singletonLock)
        {
            if (_singletons.TryGetValue(serviceType, out var cached))
            {
                return cached;
            }

            var instance = CreateServiceInstance(descriptor, serviceType, null);
            _singletons[serviceType] = instance;
            return instance;
        }
    }

    /// <summary>
    /// Resolves a scoped service, creating it if necessary.
    /// </summary>
    private object ResolveScoped(ServiceDescriptor descriptor, Type serviceType, Dictionary<Type, object> scopedInstances)
    {
        // Check scoped cache first
        if (scopedInstances.TryGetValue(serviceType, out var cached))
        {
            return cached;
        }

        // Create instance and cache it
        var instance = CreateServiceInstance(descriptor, serviceType, scopedInstances);
        scopedInstances[serviceType] = instance;
        return instance;
    }

    /// <summary>
    /// Creates a service instance from a descriptor.
    /// </summary>
    private object CreateServiceInstance(ServiceDescriptor descriptor, Type serviceType, Dictionary<Type, object>? scopedInstances)
    {
        // If instance is provided, return it
        if (descriptor.ImplementationInstance != null)
        {
            return descriptor.ImplementationInstance;
        }

        // If factory is provided, call it
        if (descriptor.ImplementationFactory != null)
        {
            // Use scoped provider if we have scoped instances, otherwise use this provider
            IServiceProvider provider = this;
            if (scopedInstances != null)
            {
                provider = new ScopedServiceProviderWrapper(this, scopedInstances);
            }
            return descriptor.ImplementationFactory(provider);
        }

        // If implementation type is provided, create instance via constructor injection
        if (descriptor.ImplementationType != null)
        {
            return CreateInstance(descriptor.ImplementationType, scopedInstances, descriptor);
        }

        throw new InvalidOperationException(
            $"Service descriptor for type '{serviceType}' has no implementation specified.");
    }

    /// <summary>
    /// Creates an instance of the specified type using constructor injection with circular dependency detection.
    /// </summary>
    /// <param name="implementationType">The type to create an instance of.</param>
    /// <param name="scopedInstances">Optional scoped instances cache.</param>
    /// <param name="descriptor">The service descriptor (for lifetime checking).</param>
    /// <returns>The created instance.</returns>
    private object CreateInstance(Type implementationType, Dictionary<Type, object>? scopedInstances, ServiceDescriptor descriptor)
    {
        var resolutionStack = _resolutionStack.Value!;
        return CreateInstance(implementationType, scopedInstances, resolutionStack, descriptor);
    }

    /// <summary>
    /// Creates an instance with circular dependency detection.
    /// </summary>
    private object CreateInstance(Type implementationType, Dictionary<Type, object>? scopedInstances, HashSet<Type> resolutionStack, ServiceDescriptor descriptor)
    {
        // For singletons, check cache before adding to resolution stack
        if (descriptor.Lifetime == ServiceLifetime.Singleton)
        {
            lock (_singletonLock)
            {
                if (_singletons.TryGetValue(descriptor.ServiceType, out var cached))
                {
                    return cached;
                }
            }
        }
        else if (scopedInstances != null && descriptor.Lifetime == ServiceLifetime.Scoped)
        {
            // For scoped, check scoped cache
            if (scopedInstances.TryGetValue(descriptor.ServiceType, out var cached))
            {
                return cached;
            }
        }

        // Check for circular dependency in resolution stack (check both service type and implementation type)
        if (resolutionStack.Contains(descriptor.ServiceType) || resolutionStack.Contains(implementationType))
        {
            var stackTrace = string.Join(" -> ", resolutionStack.Select(t => t.Name)) + " -> " + implementationType.Name;
            throw new InvalidOperationException(
                $"Circular dependency detected: {stackTrace}");
        }

        // Add both service type and implementation type to resolution stack
        resolutionStack.Add(descriptor.ServiceType);
        resolutionStack.Add(implementationType);
        try
        {
            var constructors = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No public constructors found for type '{implementationType}'.");
            }

            // Find the best constructor (one with most resolvable parameters)
            var bestConstructor = FindBestConstructor(constructors);
            if (bestConstructor == null)
            {
                throw new InvalidOperationException(
                    $"No resolvable constructor found for type '{implementationType}'.");
            }

            // Resolve constructor parameters
            var parameters = bestConstructor.GetParameters();
            var arguments = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                
                var paramType = parameter.ParameterType;
                
                // Handle IEnumerable<T> - Microsoft DI returns empty collection if nothing registered
                if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    var elementType = paramType.GetGenericArguments()[0];
                    var resolved = GetService(paramType, scopedInstances);
                    if (resolved != null)
                    {
                        arguments[i] = resolved;
                    }
                    else
                    {
                        // Create empty array for IEnumerable<T> if nothing registered
                        var emptyArray = Array.CreateInstance(elementType, 0);
                        arguments[i] = emptyArray;
                    }
                }
                else
                {
                    // Try to resolve the parameter
                    var resolved = GetService(paramType, scopedInstances);
                    
                    if (resolved != null)
                    {
                        // Service is registered, use it
                        arguments[i] = resolved;
                    }
                    else if (parameter.HasDefaultValue)
                    {
                        // Parameter has default value, use it
                        var defaultValue = parameter.DefaultValue;
                        // Handle DBNull.Value (used for reference type defaults)
                        if (defaultValue == DBNull.Value)
                        {
                            arguments[i] = null!;
                        }
                        else
                        {
                            arguments[i] = defaultValue!;
                        }
                    }
                    else
                    {
                        // Required parameter without default value - must be registered
                        throw new InvalidOperationException(
                            $"Unable to resolve service for type '{paramType}' required by constructor parameter '{parameter.Name}' of type '{implementationType}'.");
                    }
                }
            }

            // Create instance
            return bestConstructor.Invoke(arguments);
        }
        finally
        {
            // Always remove from stack - we added them above, and this ensures cleanup even if an exception occurs
            resolutionStack.Remove(descriptor.ServiceType);
            resolutionStack.Remove(implementationType);
        }
    }

    /// <summary>
    /// Finds the best constructor from the available constructors.
    /// </summary>
    /// <param name="constructors">The available constructors.</param>
    /// <returns>The best constructor, or null if none are resolvable.</returns>
    private ConstructorInfo? FindBestConstructor(ConstructorInfo[] constructors)
    {
        if (constructors.Length == 1)
        {
            var constructor = constructors[0];
            return CanResolveConstructor(constructor) ? constructor : null;
        }

        // Score constructors by number of resolvable parameters
        var scoredConstructors = constructors
            .Select(c => new
            {
                Constructor = c,
                Parameters = c.GetParameters(),
                ResolvableCount = c.GetParameters().Count(p => CanResolveParameter(p))
            })
            .Where(x => x.ResolvableCount == x.Parameters.Length) // All parameters must be resolvable
            .OrderByDescending(x => x.ResolvableCount)
            .ToList();

        return scoredConstructors.FirstOrDefault()?.Constructor;
    }

    /// <summary>
    /// Checks if a constructor can be resolved (all parameters are resolvable).
    /// </summary>
    /// <param name="constructor">The constructor to check.</param>
    /// <returns>True if all parameters can be resolved.</returns>
    private bool CanResolveConstructor(ConstructorInfo constructor)
    {
        return constructor.GetParameters().All(p => CanResolveParameter(p));
    }

    /// <summary>
    /// Checks if a type can be resolved.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type can be resolved.</returns>
    private bool CanResolveType(Type type)
    {
        // IServiceProvider and IServiceScopeFactory are always resolvable
        if (type == typeof(IServiceProvider) || type == typeof(System.IServiceProvider) || type == typeof(IServiceScopeFactory))
        {
            return true;
        }
        
        // Check if directly registered
        if (FindServiceDescriptor(type) != null)
        {
            return true;
        }
        
        // Check if it's a closed generic that can be resolved from an open generic
        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            return ResolveOpenGeneric(type) != null;
        }
        
        return false;
    }

    /// <summary>
    /// Checks if a parameter can be resolved (either registered or has default value).
    /// </summary>
    /// <param name="parameter">The parameter to check.</param>
    /// <returns>True if the parameter can be resolved or has a default value.</returns>
    private bool CanResolveParameter(ParameterInfo parameter)
    {
        // If parameter has a default value (optional parameter), it's always resolvable
        if (parameter.HasDefaultValue)
        {
            return true;
        }
        
        var paramType = parameter.ParameterType;
        
        // IEnumerable<T> parameters are always resolvable (can return empty collection)
        if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return true;
        }
        
        // Otherwise, check if the type is registered
        return CanResolveType(paramType);
    }

    /// <summary>
    /// Validates that all registered services can be resolved during build.
    /// </summary>
    private void ValidateOnBuild()
    {
        foreach (var descriptor in _services)
        {
            try
            {
                // Try to resolve the service
                GetService(descriptor.ServiceType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Unable to resolve service for type '{descriptor.ServiceType}' during validation. {ex.Message}",
                    ex);
            }
        }
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException"/> if the service provider has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ServiceProvider));
        }
    }

    /// <summary>
    /// Internal wrapper that provides scoped service resolution for factory calls.
    /// </summary>
    private sealed class ScopedServiceProviderWrapper : IServiceProvider
    {
        private readonly ServiceProvider _rootProvider;
        private readonly Dictionary<Type, object> _scopedInstances;

        public ScopedServiceProviderWrapper(ServiceProvider rootProvider, Dictionary<Type, object> scopedInstances)
        {
            _rootProvider = rootProvider;
            _scopedInstances = scopedInstances;
        }

        public object? GetService(Type serviceType)
        {
            return _rootProvider.GetService(serviceType, _scopedInstances);
        }
    }
}

