using MiniCore.Framework.DependencyInjection;

namespace MiniCore.Framework.Testing;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> used in testing.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Removes all service registrations of the specified type.
    /// </summary>
    /// <typeparam name="T">The service type to remove.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection RemoveAll<T>(this IServiceCollection services)
    {
        return services.RemoveAll(typeof(T));
    }

    /// <summary>
    /// Removes all service registrations of the specified type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceType">The service type to remove.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection RemoveAll(this IServiceCollection services, Type serviceType)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        for (var i = services.Count - 1; i >= 0; i--)
        {
            var descriptor = services[i];
            if (descriptor.ServiceType == serviceType)
            {
                services.RemoveAt(i);
            }
        }

        return services;
    }
}

