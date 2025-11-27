namespace MiniCore.Framework.Hosting;

/// <summary>
/// Allows consumers to be notified of application lifetime events.
/// </summary>
internal class HostApplicationLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _applicationStartedSource = new();
    private readonly CancellationTokenSource _applicationStoppingSource = new();
    private readonly CancellationTokenSource _applicationStoppedSource = new();

    /// <summary>
    /// Triggered when the application host has fully started and is about to wait
    /// for a graceful shutdown.
    /// </summary>
    public CancellationToken ApplicationStarted => _applicationStartedSource.Token;

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// Request may still be in flight. Shutdown will block until this event completes.
    /// </summary>
    public CancellationToken ApplicationStopping => _applicationStoppingSource.Token;

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// All requests should be complete at this point. Shutdown will block
    /// until this event completes.
    /// </summary>
    public CancellationToken ApplicationStopped => _applicationStoppedSource.Token;

    /// <summary>
    /// Signals that ApplicationStarted has been triggered.
    /// </summary>
    internal void NotifyStarted()
    {
        _applicationStartedSource.Cancel();
    }

    /// <summary>
    /// Signals that ApplicationStopping has been triggered.
    /// </summary>
    internal void NotifyStopping()
    {
        _applicationStoppingSource.Cancel();
    }

    /// <summary>
    /// Signals that ApplicationStopped has been triggered.
    /// </summary>
    internal void NotifyStopped()
    {
        _applicationStoppedSource.Cancel();
    }

    /// <summary>
    /// Requests termination of the current application.
    /// </summary>
    public void StopApplication()
    {
        NotifyStopping();
    }
}

