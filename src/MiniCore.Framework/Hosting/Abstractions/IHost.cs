using MiniCore.Framework.DependencyInjection;

namespace MiniCore.Framework.Hosting;

/// <summary>
/// Represents a program abstraction.
/// </summary>
public interface IHost : IDisposable
{
    /// <summary>
    /// The program's configured services.
    /// </summary>
    DependencyInjection.IServiceProvider Services { get; }

    /// <summary>
    /// Starts the host.
    /// </summary>
    /// <param name="cancellationToken">Used to abort program start.</param>
    /// <returns>A <see cref="Task"/> that completes when the <see cref="IHost"/> starts.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to gracefully stop the host.
    /// </summary>
    /// <param name="cancellationToken">Used to indicate when stop should no longer be graceful.</param>
    /// <returns>A <see cref="Task"/> that completes when the <see cref="IHost"/> stops.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}

