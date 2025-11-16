using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MiniCore.Framework.DependencyInjection;

/// <summary>
/// Default implementation of <see cref="IServiceProvider"/> and <see cref="IServiceScopeFactory"/>.
/// </summary>
/// <remarks>
/// <para>
/// This service provider resolves services by:
/// <list type="number">
/// <item>Looking up the service registration in the service collection</item>
/// <item>Resolving dependencies recursively</item>
/// <item>Creating instances using constructor injection</item>
/// </list>
/// </para>
/// <para>
/// This initial implementation does not yet support service lifetimes (Singleton, Scoped, Transient).
/// All services are created fresh on each resolution.
/// </para>
/// </remarks>
public class ServiceProvider : IServiceProvider, IServiceScopeFactory, IDisposable
{
    private readonly ServiceCollection _services;
    private readonly ServiceProviderOptions _options;
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

        // TODO: Implement ValidateOnBuild when lifetimes are added
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

        // Find service descriptor
        var descriptor = FindServiceDescriptor(serviceType);
        if (descriptor == null)
        {
            return null;
        }

        return ResolveService(descriptor, serviceType);
    }

    /// <summary>
    /// Creates a new <see cref="IServiceScope"/>.
    /// </summary>
    /// <returns>A new <see cref="IServiceScope"/>.</returns>
    public IServiceScope CreateScope()
    {
        ThrowIfDisposed();
        return new ServiceScope(this);
    }

    /// <summary>
    /// Disposes the service provider and any singleton services that implement <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // TODO: Dispose singleton instances when lifetimes are implemented
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

        // TODO: Handle open generics when lifetimes are added
        // For now, return null if not found
        return null;
    }

    /// <summary>
    /// Resolves a service from a descriptor.
    /// </summary>
    /// <param name="descriptor">The service descriptor.</param>
    /// <param name="serviceType">The service type being requested.</param>
    /// <returns>The resolved service instance.</returns>
    private object ResolveService(ServiceDescriptor descriptor, Type serviceType)
    {
        // If instance is provided, return it
        if (descriptor.ImplementationInstance != null)
        {
            return descriptor.ImplementationInstance;
        }

        // If factory is provided, call it
        if (descriptor.ImplementationFactory != null)
        {
            return descriptor.ImplementationFactory(this);
        }

        // If implementation type is provided, create instance via constructor injection
        if (descriptor.ImplementationType != null)
        {
            return CreateInstance(descriptor.ImplementationType);
        }

        throw new InvalidOperationException(
            $"Service descriptor for type '{serviceType}' has no implementation specified.");
    }

    /// <summary>
    /// Creates an instance of the specified type using constructor injection.
    /// </summary>
    /// <param name="implementationType">The type to create an instance of.</param>
    /// <returns>The created instance.</returns>
    private object CreateInstance(Type implementationType)
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
            var resolved = GetService(parameter.ParameterType);
            if (resolved == null)
            {
                throw new InvalidOperationException(
                    $"Unable to resolve service for type '{parameter.ParameterType}' required by constructor parameter '{parameter.Name}' of type '{implementationType}'.");
            }
            arguments[i] = resolved;
        }

        // Create instance
        return bestConstructor.Invoke(arguments);
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
                ResolvableCount = c.GetParameters().Count(p => CanResolveType(p.ParameterType))
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
        return constructor.GetParameters().All(p => CanResolveType(p.ParameterType));
    }

    /// <summary>
    /// Checks if a type can be resolved.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type can be resolved.</returns>
    private bool CanResolveType(Type type)
    {
        return FindServiceDescriptor(type) != null;
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
}

