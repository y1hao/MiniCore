namespace MiniCore.Framework.DependencyInjection;

/// <summary>
/// Defines a mechanism for retrieving service objects; that is, objects that provide custom support to other objects.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IServiceProvider"/> interface is the core abstraction for dependency injection.
/// It provides a way to resolve services that have been registered in an <see cref="IServiceCollection"/>.
/// </para>
/// <para>
/// Services are resolved by their type. The service provider will:
/// <list type="bullet">
/// <item>Look up the service registration</item>
/// <item>Resolve any dependencies recursively</item>
/// <item>Create an instance based on the service lifetime</item>
/// <item>Return the service instance</item>
/// </list>
/// </para>
/// <para>
/// If a service is not registered, <see cref="GetService"/> returns null.
/// Use <see cref="ServiceProviderExtensions.GetRequiredService{T}"/> to throw an exception if the service is not found.
/// </para>
/// </remarks>
public interface IServiceProvider
{
    /// <summary>
    /// Gets the service object of the specified type.
    /// </summary>
    /// <param name="serviceType">An object that specifies the type of service object to get.</param>
    /// <returns>
    /// A service object of type <paramref name="serviceType"/>.
    /// Returns null if there is no service object of type <paramref name="serviceType"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is the primary way to resolve services from the dependency injection container.
    /// </para>
    /// <para>
    /// The service provider will:
    /// <list type="number">
    /// <item>Check if the service type is registered</item>
    /// <item>Handle open generic types (e.g., ILogger&lt;T&gt;)</item>
    /// <item>Resolve dependencies recursively</item>
    /// <item>Create instances based on lifetime (Singleton, Scoped, Transient)</item>
    /// <item>Return the service instance</item>
    /// </list>
    /// </para>
    /// <para>
    /// If the service is not registered, this method returns null without throwing an exception.
    /// To get an exception when a service is not found, use the extension method
    /// <see cref="ServiceProviderExtensions.GetRequiredService(IServiceProvider, Type)"/>.
    /// </para>
    /// </remarks>
    object? GetService(Type serviceType);
}

