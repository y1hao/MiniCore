namespace MiniCore.Framework.DependencyInjection;

/// <summary>
/// A factory for creating instances of <see cref="IServiceScope"/>, which is used to create scoped service providers.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IServiceScopeFactory"/> is used to create service scopes, which are necessary for resolving
/// services with <see cref="ServiceLifetime.Scoped"/> lifetime.
/// </para>
/// <para>
/// Typical usage in singleton services (like background services):
/// <code>
/// public class MyBackgroundService
/// {
///     private readonly IServiceScopeFactory _scopeFactory;
///     
///     public MyBackgroundService(IServiceScopeFactory scopeFactory)
///     {
///         _scopeFactory = scopeFactory;
///     }
///     
///     public void DoWork()
///     {
///         using var scope = _scopeFactory.CreateScope();
///         var scopedService = scope.ServiceProvider.GetRequiredService&lt;IMyScopedService&gt;();
///         // Use scopedService...
///     }
/// }
/// </code>
/// </para>
/// <para>
/// This pattern is essential when singleton services need to use scoped services (like database contexts).
/// Each call to <see cref="CreateScope"/> creates a new isolated scope with its own set of scoped instances.
/// </para>
/// </remarks>
public interface IServiceScopeFactory
{
    /// <summary>
    /// Create an <see cref="IServiceScope"/> which contains an <see cref="IServiceProvider"/> used to resolve scoped services.
    /// </summary>
    /// <returns>
    /// An <see cref="IServiceScope"/> controlling the lifetime of the scope. Once this is disposed,
    /// any scoped services that have been resolved from the <see cref="IServiceScope.ServiceProvider"/>
    /// will also be disposed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Each call to this method creates a new scope. Scoped services resolved from different scopes
    /// are different instances, even if they are the same service type.
    /// </para>
    /// <para>
    /// It is important to dispose the returned scope when finished. The recommended pattern is to use
    /// a <c>using</c> statement:
    /// <code>
    /// using (var scope = scopeFactory.CreateScope())
    /// {
    ///     var service = scope.ServiceProvider.GetRequiredService&lt;IMyService&gt;();
    ///     // Use service...
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    IServiceScope CreateScope();
}

