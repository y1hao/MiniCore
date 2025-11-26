using MiniCore.Framework.Configuration.Abstractions;

namespace MiniCore.Framework.Configuration;

/// <summary>
/// Implements <see cref="IChangeToken"/> by wrapping another token.
/// </summary>
public class ConfigurationReloadToken : IChangeToken
{
    private readonly object _lock = new object();
    private CancellationTokenSource _cts = new CancellationTokenSource();

    /// <summary>
    /// Indicates if this token will proactively raise callbacks. Callbacks are still guaranteed to fire, eventually.
    /// </summary>
    public bool ActiveChangeCallbacks => true;

    /// <summary>
    /// Gets a value that indicates if a change has occurred.
    /// </summary>
    public bool HasChanged => _cts.Token.IsCancellationRequested;

    /// <summary>
    /// Registers for a callback that will be invoked when the entry has changed.
    /// </summary>
    /// <param name="callback">The callback to invoke.</param>
    /// <param name="state">State to be passed into the callback.</param>
    /// <returns>An <see cref="IDisposable"/> that is used to unregister the callback.</returns>
    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
    {
        return _cts.Token.Register(callback, state);
    }

    /// <summary>
    /// Used to trigger the change token when a reload occurs.
    /// </summary>
    public void OnReload()
    {
        lock (_lock)
        {
            var previousCts = _cts;
            _cts = new CancellationTokenSource();
            previousCts.Cancel();
            previousCts.Dispose();
        }
    }
}

