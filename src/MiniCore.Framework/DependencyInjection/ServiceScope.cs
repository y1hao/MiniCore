using System;
using System.Collections.Generic;

namespace MiniCore.Framework.DependencyInjection;

/// <summary>
/// Default implementation of <see cref="IServiceScope"/>.
/// </summary>
/// <remarks>
/// <para>
/// A service scope provides isolation for scoped lifetime services. Services registered with
/// <see cref="ServiceLifetime.Scoped"/> are created once per scope and cached. When the scope
/// is disposed, all scoped instances that implement <see cref="IDisposable"/> are disposed.
/// </para>
/// </remarks>
public class ServiceScope : IServiceScope
{
    private readonly ServiceProvider _rootProvider;
    private readonly Dictionary<Type, object> _scopedInstances;
    private readonly ScopedServiceProvider _serviceProvider;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="ServiceScope"/>.
    /// </summary>
    /// <param name="rootProvider">The root service provider.</param>
    /// <param name="scopedInstances">The scoped instances cache for this scope.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rootProvider"/> or <paramref name="scopedInstances"/> is null.</exception>
    internal ServiceScope(ServiceProvider rootProvider, Dictionary<Type, object> scopedInstances)
    {
        ArgumentNullException.ThrowIfNull(rootProvider);
        ArgumentNullException.ThrowIfNull(scopedInstances);
        _rootProvider = rootProvider;
        _scopedInstances = scopedInstances;
        _serviceProvider = new ScopedServiceProvider(rootProvider, scopedInstances);
    }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> used to resolve services within this scope.
    /// </summary>
    public IServiceProvider ServiceProvider => _serviceProvider;

    /// <summary>
    /// Disposes the scope and any scoped services that implement <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // Dispose all scoped instances that implement IDisposable
            foreach (var instance in _scopedInstances.Values)
            {
                if (instance is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch
                    {
                        // Swallow exceptions during disposal to ensure all services are disposed
                    }
                }
            }
            _scopedInstances.Clear();
            _disposed = true;
        }
    }

    /// <summary>
    /// Internal service provider that uses scoped instances cache.
    /// </summary>
    private sealed class ScopedServiceProvider : IServiceProvider
    {
        private readonly ServiceProvider _rootProvider;
        private readonly Dictionary<Type, object> _scopedInstances;

        public ScopedServiceProvider(ServiceProvider rootProvider, Dictionary<Type, object> scopedInstances)
        {
            _rootProvider = rootProvider;
            _scopedInstances = scopedInstances;
        }

        public object? GetService(Type serviceType)
        {
            return _rootProvider.GetService(serviceType, _scopedInstances);
        }
    }
}

