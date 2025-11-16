using System.Collections.Generic;

namespace MiniCore.Framework.DependencyInjection;

/// <summary>
/// Specifies the contract for a collection of service descriptors.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IServiceCollection"/> is used to register services before building an <see cref="IServiceProvider"/>.
/// It extends <see cref="IList{T}"/> of <see cref="ServiceDescriptor"/>, allowing you to add, remove, and enumerate service registrations.
/// </para>
/// <para>
/// Typical usage:
/// <code>
/// var services = new ServiceCollection();
/// services.AddSingleton&lt;IMyService, MyService&gt;();
/// services.AddScoped&lt;IDbContext, DbContext&gt;();
/// var serviceProvider = services.BuildServiceProvider();
/// </code>
/// </para>
/// <para>
/// Extension methods provide convenient ways to register services:
/// <list type="bullet">
/// <item><see cref="ServiceCollectionExtensions.AddSingleton{TService, TImplementation}(IServiceCollection)"/></item>
/// <item><see cref="ServiceCollectionExtensions.AddScoped{TService, TImplementation}(IServiceCollection)"/></item>
/// <item><see cref="ServiceCollectionExtensions.AddTransient{TService, TImplementation}(IServiceCollection)"/></item>
/// </list>
/// </para>
/// <para>
/// Multiple registrations for the same service type are allowed. When resolving, the last registration is used.
/// </para>
/// </remarks>
public interface IServiceCollection : IList<ServiceDescriptor>
{
    // This interface extends IList<ServiceDescriptor> and provides no additional members.
    // The extension methods in ServiceCollectionExtensions provide the convenient registration APIs.
}

