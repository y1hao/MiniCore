namespace MiniCore.Framework.DependencyInjection;

/// <summary>
/// Describes a service registration in an <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// A ServiceDescriptor contains all the information needed to register and resolve a service:
/// <list type="bullet">
/// <item>The service type (interface or base class)</item>
/// <item>The implementation (type, instance, or factory)</item>
/// <item>The service lifetime (Singleton, Scoped, or Transient)</item>
/// </list>
/// Only one of ImplementationType, ImplementationInstance, or ImplementationFactory should be set.
/// </remarks>
public class ServiceDescriptor
{
    /// <summary>
    /// Gets the service type (typically an interface or base class).
    /// </summary>
    /// <remarks>
    /// This is the type that will be requested when resolving the service.
    /// For example, if registering ILogger&lt;T&gt;, this would be ILogger&lt;T&gt;.
    /// </remarks>
    public Type ServiceType { get; }

    /// <summary>
    /// Gets the implementation type, if registered via type-to-type registration.
    /// </summary>
    /// <remarks>
    /// This is used when registering a service with its concrete implementation type.
    /// Example: Registering IService with ServiceImplementation.
    /// The service provider will use constructor injection to create instances of this type.
    /// </remarks>
    public Type? ImplementationType { get; }

    /// <summary>
    /// Gets the implementation instance, if registered with a pre-created instance.
    /// </summary>
    /// <remarks>
    /// This is used when registering a singleton with an existing instance.
    /// The instance is typically created before registration and will be reused for all requests.
    /// </remarks>
    public object? ImplementationInstance { get; }

    /// <summary>
    /// Gets the factory function, if registered via factory registration.
    /// </summary>
    /// <remarks>
    /// This factory function receives an <see cref="IServiceProvider"/> and returns the service instance.
    /// Useful for complex initialization logic or when you need access to other services during creation.
    /// </remarks>
    public Func<IServiceProvider, object>? ImplementationFactory { get; }

    /// <summary>
    /// Gets the lifetime of the service.
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ServiceDescriptor"/>.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <param name="implementationType">The implementation type, or null if using instance or factory.</param>
    /// <param name="implementationInstance">The implementation instance, or null if using type or factory.</param>
    /// <param name="implementationFactory">The factory function, or null if using type or instance.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <exception cref="ArgumentNullException">Thrown when serviceType is null.</exception>
    /// <exception cref="ArgumentException">Thrown when multiple implementation methods are provided or none are provided.</exception>
    private ServiceDescriptor(
        Type serviceType,
        Type? implementationType,
        object? implementationInstance,
        Func<IServiceProvider, object>? implementationFactory,
        ServiceLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ServiceType = serviceType;
        ImplementationType = implementationType;
        ImplementationInstance = implementationInstance;
        ImplementationFactory = implementationFactory;
        Lifetime = lifetime;

        // Validate that exactly one implementation method is provided
        var implementationCount = (implementationType != null ? 1 : 0) +
                                  (implementationInstance != null ? 1 : 0) +
                                  (implementationFactory != null ? 1 : 0);

        if (implementationCount != 1)
        {
            throw new ArgumentException(
                "Exactly one of ImplementationType, ImplementationInstance, or ImplementationFactory must be provided.");
        }
    }

    /// <summary>
    /// Creates a <see cref="ServiceDescriptor"/> for a singleton service registered with a type.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>A new ServiceDescriptor.</returns>
    public static ServiceDescriptor Singleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        return Describe(typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Creates a <see cref="ServiceDescriptor"/> for a singleton service registered with an instance.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="instance">The singleton instance.</param>
    /// <returns>A new ServiceDescriptor.</returns>
    public static ServiceDescriptor Singleton<TService>(TService instance)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(instance);

        return new ServiceDescriptor(
            typeof(TService),
            null,
            instance,
            null,
            ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Creates a <see cref="ServiceDescriptor"/> for a singleton service registered with a factory.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="factory">The factory function that creates the service instance.</param>
    /// <returns>A new ServiceDescriptor.</returns>
    public static ServiceDescriptor Singleton<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        return new ServiceDescriptor(
            typeof(TService),
            null,
            null,
            sp => factory(sp)!,
            ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Creates a <see cref="ServiceDescriptor"/> for a scoped service registered with a type.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>A new ServiceDescriptor.</returns>
    public static ServiceDescriptor Scoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        return Describe(typeof(TService), typeof(TImplementation), ServiceLifetime.Scoped);
    }

    /// <summary>
    /// Creates a <see cref="ServiceDescriptor"/> for a scoped service registered with a factory.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="factory">The factory function that creates the service instance.</param>
    /// <returns>A new ServiceDescriptor.</returns>
    public static ServiceDescriptor Scoped<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        return new ServiceDescriptor(
            typeof(TService),
            null,
            null,
            sp => factory(sp)!,
            ServiceLifetime.Scoped);
    }

    /// <summary>
    /// Creates a <see cref="ServiceDescriptor"/> for a transient service registered with a type.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>A new ServiceDescriptor.</returns>
    public static ServiceDescriptor Transient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        return Describe(typeof(TService), typeof(TImplementation), ServiceLifetime.Transient);
    }

    /// <summary>
    /// Creates a <see cref="ServiceDescriptor"/> for a transient service registered with a factory.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="factory">The factory function that creates the service instance.</param>
    /// <returns>A new ServiceDescriptor.</returns>
    public static ServiceDescriptor Transient<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        return new ServiceDescriptor(
            typeof(TService),
            null,
            null,
            sp => factory(sp)!,
            ServiceLifetime.Transient);
    }

    /// <summary>
    /// Creates a <see cref="ServiceDescriptor"/> with the specified service type, implementation type, and lifetime.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>A new ServiceDescriptor.</returns>
    /// <remarks>
    /// This method is useful for open generic registrations (e.g., ILogger&lt;&gt; → Logger&lt;&gt;).
    /// </remarks>
    public static ServiceDescriptor Describe(Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementationType);

        return new ServiceDescriptor(serviceType, implementationType, null, null, lifetime);
    }

    /// <summary>
    /// Creates a <see cref="ServiceDescriptor"/> with the specified service type, factory, and lifetime.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <param name="factory">The factory function that creates the service instance.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>A new ServiceDescriptor.</returns>
    public static ServiceDescriptor Describe(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(factory);

        return new ServiceDescriptor(serviceType, null, null, factory, lifetime);
    }

    /// <summary>
    /// Creates a <see cref="ServiceDescriptor"/> with the specified service type, instance, and lifetime.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <param name="instance">The service instance.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>A new ServiceDescriptor.</returns>
    /// <remarks>
    /// Typically used for singleton registrations with pre-created instances.
    /// </remarks>
    public static ServiceDescriptor Describe(Type serviceType, object instance, ServiceLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(instance);

        return new ServiceDescriptor(serviceType, null, instance, null, lifetime);
    }
}

