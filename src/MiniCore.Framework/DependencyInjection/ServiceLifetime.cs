namespace MiniCore.Framework.DependencyInjection;

/// <summary>
/// Specifies the lifetime of a service in an <see cref="IServiceProvider"/>.
/// </summary>
/// <remarks>
/// Service lifetime determines how instances are created and managed:
/// <list type="bullet">
/// <item><term>Singleton</term><description>One instance is created for the lifetime of the root service provider. All requests for this service return the same instance.</description></item>
/// <item><term>Scoped</term><description>One instance is created per service scope. Requests within the same scope return the same instance. Different scopes create different instances.</description></item>
/// <item><term>Transient</term><description>A new instance is created every time the service is requested. No caching is performed.</description></item>
/// </list>
/// </remarks>
public enum ServiceLifetime
{
    /// <summary>
    /// Specifies that a single instance of the service will be created and shared across the entire application lifetime.
    /// Singleton services are created once when first requested and disposed when the root service provider is disposed.
    /// </summary>
    /// <remarks>
    /// Use Singleton for:
    /// <list type="bullet">
    /// <item>Stateless services that are expensive to create</item>
    /// <item>Services that maintain application-wide state</item>
    /// <item>Configuration services</item>
    /// <item>Logging infrastructure</item>
    /// </list>
    /// Thread-safety: Singleton instances may be accessed from multiple threads, so ensure thread-safety if needed.
    /// </remarks>
    Singleton = 0,

    /// <summary>
    /// Specifies that a new instance of the service will be created for each service scope.
    /// Scoped services are created once per scope and shared within that scope. When the scope is disposed, all scoped instances are disposed.
    /// </summary>
    /// <remarks>
    /// Use Scoped for:
    /// <list type="bullet">
    /// <item>Database contexts (Entity Framework DbContext)</item>
    /// <item>Unit of Work patterns</item>
    /// <item>Services that should be isolated per HTTP request</item>
    /// <item>Services that need to be disposed at the end of a scope</item>
    /// </list>
    /// In web applications, a scope typically corresponds to a single HTTP request.
    /// Scoped services cannot be injected into Singleton services directly (would cause a scope leak).
    /// </remarks>
    Scoped = 1,

    /// <summary>
    /// Specifies that a new instance of the service will be created every time it is requested.
    /// Transient services are never cached and are created fresh for each resolution.
    /// </summary>
    /// <remarks>
    /// Use Transient for:
    /// <list type="bullet">
    /// <item>Lightweight, stateless services</item>
    /// <item>Services that are cheap to create</item>
    /// <item>Services that should not share state between consumers</item>
    /// <item>Most application services by default</item>
    /// </list>
    /// Transient services are disposed when they go out of scope (if they implement IDisposable).
    /// This is the default lifetime for most services.
    /// </remarks>
    Transient = 2
}

