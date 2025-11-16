using System;

namespace MiniCore.Framework.DependencyInjection;

/// <summary>
/// Default implementation of <see cref="IServiceScope"/>.
/// </summary>
/// <remarks>
/// <para>
/// A service scope provides isolation for scoped lifetime services. This initial implementation
/// delegates to the root service provider for all resolutions. Lifetime management will be added later.
/// </para>
/// </remarks>
public class ServiceScope : IServiceScope
{
    private readonly ServiceProvider _serviceProvider;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="ServiceScope"/>.
    /// </summary>
    /// <param name="serviceProvider">The root service provider.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> is null.</exception>
    public ServiceScope(ServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
        ServiceProvider = _serviceProvider;
    }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> used to resolve services within this scope.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Disposes the scope and any scoped services that implement <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // TODO: Dispose scoped instances when lifetimes are implemented
            _disposed = true;
        }
    }
}

