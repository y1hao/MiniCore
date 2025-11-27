namespace MiniCore.Framework.Hosting;

/// <summary>
/// Allows consumers to be notified of application lifetime events. This interface is not intended to be user-replaceable.
/// </summary>
public interface IHostApplicationLifetime
{
    /// <summary>
    /// Triggered when the application host has fully started and is about to wait
    /// for a graceful shutdown.
    /// </summary>
    CancellationToken ApplicationStarted { get; }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// Request may still be in flight. Shutdown will block until this event completes.
    /// </summary>
    CancellationToken ApplicationStopping { get; }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// All requests should be complete at this point. Shutdown will block
    /// until this event completes.
    /// </summary>
    CancellationToken ApplicationStopped { get; }

    /// <summary>
    /// Requests termination of the current application.
    /// </summary>
    void StopApplication();
}

