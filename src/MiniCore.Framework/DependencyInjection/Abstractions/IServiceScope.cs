using System;

namespace MiniCore.Framework.DependencyInjection;

/// <summary>
/// Represents a scope for service resolution. Scoped services are created once per scope and disposed when the scope is disposed.
/// </summary>
/// <remarks>
/// <para>
/// A service scope provides isolation for scoped lifetime services. Services registered with <see cref="ServiceLifetime.Scoped"/>
/// are created once per scope and shared within that scope. When the scope is disposed, all scoped instances are disposed.
/// </para>
/// <para>
/// Typical usage:
/// <code>
/// using (var scope = serviceProvider.CreateScope())
/// {
///     var scopedService = scope.ServiceProvider.GetRequiredService&lt;IMyScopedService&gt;();
///     // Use scopedService...
/// } // scopedService is disposed here
/// </code>
/// </para>
/// <para>
/// In web applications, a scope typically corresponds to a single HTTP request. The framework creates a scope
/// at the beginning of each request and disposes it at the end.
/// </para>
/// <para>
/// Important: Scoped services cannot be injected into Singleton services directly, as this would cause a scope leak.
/// Instead, use <see cref="IServiceScopeFactory"/> to create scopes within singleton services.
/// </para>
/// </remarks>
public interface IServiceScope : IDisposable
{
    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> used to resolve services within this scope.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service provider is scoped and will:
    /// <list type="bullet">
    /// <item>Create new instances for Transient services</item>
    /// <item>Reuse instances for Scoped services within this scope</item>
    /// <item>Reuse instances for Singleton services (shared with root provider)</item>
    /// </list>
    /// </para>
    /// <para>
    /// When the scope is disposed, all scoped instances that implement <see cref="IDisposable"/> are disposed.
    /// </para>
    /// </remarks>
    IServiceProvider ServiceProvider { get; }
}

