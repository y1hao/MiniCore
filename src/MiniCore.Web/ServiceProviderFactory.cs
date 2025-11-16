// TODO: REMOVE THIS FILE IN PHASE 4 (Host Abstraction)
// This file is a temporary bridge between ASP.NET Core's Microsoft.Extensions.DependencyInjection
// and our custom DI container. Once we implement our own HostBuilder in Phase 4, we can:
// - Remove this ServiceProviderFactory entirely
// - Remove the Microsoft.Extensions.DependencyInjection package dependency
// - Register services directly into our IServiceCollection without conversion
// See: docs/Chapter1/MICROSOFT_DI_DEPENDENCY_ANALYSIS.md for details

using Microsoft.Extensions.DependencyInjection;
using MiniCore.Framework.DependencyInjection;

namespace MiniCore.Web;

/// <summary>
/// Factory for creating service providers that use our custom DI implementation.
/// This allows ASP.NET Core to use our custom DI container instead of the default one.
/// 
/// NOTE: This is temporary bridge code that will be removed in Phase 4 (Host Abstraction)
/// when we implement our own HostBuilder that uses our DI natively.
/// </summary>
public class ServiceProviderFactory : IServiceProviderFactory<MiniCore.Framework.DependencyInjection.ServiceCollection>
{
    private readonly MiniCore.Framework.DependencyInjection.ServiceProviderOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="ServiceProviderFactory"/>.
    /// </summary>
    /// <param name="options">Optional service provider options.</param>
    public ServiceProviderFactory(MiniCore.Framework.DependencyInjection.ServiceProviderOptions? options = null)
    {
        _options = options ?? new MiniCore.Framework.DependencyInjection.ServiceProviderOptions();
    }

    /// <summary>
    /// Creates a builder from the Microsoft service collection.
    /// </summary>
    /// <param name="services">The Microsoft service collection.</param>
    /// <returns>A builder that wraps the service collection.</returns>
    public MiniCore.Framework.DependencyInjection.ServiceCollection CreateBuilder(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        // Convert Microsoft's IServiceCollection to our ServiceCollection
        var ourCollection = new MiniCore.Framework.DependencyInjection.ServiceCollection();
        
        foreach (var descriptor in services)
        {
            // Convert Microsoft's ServiceDescriptor to our ServiceDescriptor
            var ourDescriptor = ConvertDescriptor(descriptor);
            ourCollection.Add(ourDescriptor);
        }
        
        return ourCollection;
    }

    /// <summary>
    /// Creates a service provider from our service collection.
    /// </summary>
    /// <param name="containerBuilder">Our service collection.</param>
    /// <returns>A service provider that implements System.IServiceProvider.</returns>
    public System.IServiceProvider CreateServiceProvider(MiniCore.Framework.DependencyInjection.ServiceCollection containerBuilder)
    {
        var provider = containerBuilder.BuildServiceProvider(_options);
        // Our ServiceProvider already implements System.IServiceProvider, so we can return it directly
        // But we need to wrap it to also implement IServiceScopeFactory
        return new ServiceProviderAdapter(provider);
    }

    /// <summary>
    /// Converts a Microsoft ServiceDescriptor to our ServiceDescriptor.
    /// </summary>
    private static MiniCore.Framework.DependencyInjection.ServiceDescriptor ConvertDescriptor(Microsoft.Extensions.DependencyInjection.ServiceDescriptor msDescriptor)
    {
        var lifetime = msDescriptor.Lifetime switch
        {
            Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton => MiniCore.Framework.DependencyInjection.ServiceLifetime.Singleton,
            Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped => MiniCore.Framework.DependencyInjection.ServiceLifetime.Scoped,
            Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient => MiniCore.Framework.DependencyInjection.ServiceLifetime.Transient,
            _ => throw new ArgumentException($"Unknown lifetime: {msDescriptor.Lifetime}")
        };

        // Handle different descriptor types
        if (msDescriptor.ImplementationInstance != null)
        {
            return MiniCore.Framework.DependencyInjection.ServiceDescriptor.Describe(
                msDescriptor.ServiceType,
                msDescriptor.ImplementationInstance,
                lifetime);
        }
        
        if (msDescriptor.ImplementationFactory != null)
        {
            // Wrap Microsoft's factory to work with our ServiceProvider
            // The factory expects System.IServiceProvider, so we need to wrap our IServiceProvider
            return MiniCore.Framework.DependencyInjection.ServiceDescriptor.Describe(
                msDescriptor.ServiceType,
                sp => msDescriptor.ImplementationFactory!(new ServiceProviderAdapter((MiniCore.Framework.DependencyInjection.ServiceProvider)sp)),
                lifetime);
        }
        
        if (msDescriptor.ImplementationType != null)
        {
            return MiniCore.Framework.DependencyInjection.ServiceDescriptor.Describe(
                msDescriptor.ServiceType,
                msDescriptor.ImplementationType,
                lifetime);
        }

        throw new InvalidOperationException($"Invalid service descriptor for type {msDescriptor.ServiceType}");
    }
}

/// <summary>
/// Adapter that wraps our ServiceProvider to implement System.IServiceProvider
/// and Microsoft.Extensions.DependencyInjection.IServiceScopeFactory.
/// </summary>
internal class ServiceProviderAdapter : System.IServiceProvider, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory, IDisposable
{
    private readonly MiniCore.Framework.DependencyInjection.ServiceProvider _provider;

    public ServiceProviderAdapter(MiniCore.Framework.DependencyInjection.ServiceProvider provider)
    {
        _provider = provider;
    }

    object? System.IServiceProvider.GetService(Type serviceType)
    {
        return _provider.GetService(serviceType);
    }

    public Microsoft.Extensions.DependencyInjection.IServiceScope CreateScope()
    {
        var scope = _provider.CreateScope();
        return new ServiceScopeAdapter(scope);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}

/// <summary>
/// Adapter that wraps our IServiceScope to implement Microsoft's IServiceScope.
/// </summary>
internal class ServiceScopeAdapter : Microsoft.Extensions.DependencyInjection.IServiceScope
{
    private readonly MiniCore.Framework.DependencyInjection.IServiceScope _scope;

    public ServiceScopeAdapter(MiniCore.Framework.DependencyInjection.IServiceScope scope)
    {
        _scope = scope;
    }

    public System.IServiceProvider ServiceProvider => new ServiceProviderAdapter((MiniCore.Framework.DependencyInjection.ServiceProvider)_scope.ServiceProvider);

    public void Dispose()
    {
        _scope.Dispose();
    }
}

