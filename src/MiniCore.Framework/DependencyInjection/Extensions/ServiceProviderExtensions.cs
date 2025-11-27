using System;

namespace MiniCore.Framework.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceProvider"/>.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Get service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
    /// <returns>
    /// A service object of type <typeparamref name="T"/> or null if there is no such service.
    /// </returns>
    /// <remarks>
    /// This is a convenience method that calls <see cref="IServiceProvider.GetService(Type)"/> with the type parameter.
    /// </remarks>
    public static T? GetService<T>(this IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return (T?)provider.GetService(typeof(T));
    }

    /// <summary>
    /// Get service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
    /// <returns>
    /// A service object of type <typeparamref name="T"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when there is no service of type <typeparamref name="T"/> registered.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is a convenience wrapper around <see cref="IServiceProvider.GetService(Type)"/> that
    /// throws an exception if the service is not found, rather than returning null.
    /// </para>
    /// <para>
    /// Use this method when you expect the service to always be registered. If the service might not be registered,
    /// use <see cref="GetService{T}(IServiceProvider)"/> instead and check for null.
    /// </para>
    /// </remarks>
    public static T GetRequiredService<T>(this IServiceProvider provider)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(provider);

        return (T)GetRequiredService(provider, typeof(T));
    }

    /// <summary>
    /// Get service of type <paramref name="serviceType"/> from the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
    /// <param name="serviceType">An object that specifies the type of service object to get.</param>
    /// <returns>
    /// A service object of type <paramref name="serviceType"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider"/> or <paramref name="serviceType"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when there is no service of type <paramref name="serviceType"/> registered.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is a convenience wrapper around <see cref="IServiceProvider.GetService(Type)"/> that
    /// throws an exception if the service is not found, rather than returning null.
    /// </para>
    /// <para>
    /// Use this method when you expect the service to always be registered. If the service might not be registered,
    /// use <see cref="IServiceProvider.GetService(Type)"/> instead and check for null.
    /// </para>
    /// </remarks>
    public static object GetRequiredService(this IServiceProvider provider, Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(serviceType);

        var service = provider.GetService(serviceType);
        if (service == null)
        {
            throw new InvalidOperationException($"Unable to resolve service for type '{serviceType}'.");
        }

        return service;
    }

    /// <summary>
    /// Creates a new <see cref="IServiceScope"/> that can be used to resolve scoped services.
    /// </summary>
    /// <param name="provider">The <see cref="IServiceProvider"/> to create a scope from.</param>
    /// <returns>A new <see cref="IServiceScope"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the provider does not implement <see cref="IServiceScopeFactory"/>.</exception>
    /// <remarks>
    /// This extension method allows creating scopes from <see cref="IServiceProvider"/> instances.
    /// The provider must implement <see cref="IServiceScopeFactory"/> for this to work.
    /// </remarks>
    public static IServiceScope CreateScope(this IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (provider is IServiceScopeFactory scopeFactory)
        {
            return scopeFactory.CreateScope();
        }

        throw new InvalidOperationException($"The service provider does not implement {nameof(IServiceScopeFactory)}.");
    }
}

