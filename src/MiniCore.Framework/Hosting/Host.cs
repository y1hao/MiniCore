using MiniCore.Framework.DependencyInjection;

namespace MiniCore.Framework.Hosting;

/// <summary>
/// The host.
/// </summary>
internal class Host : IHost
{
    private readonly DependencyInjection.IServiceProvider _serviceProvider;
    private readonly HostApplicationLifetime _applicationLifetime;
    private readonly List<IHostedService> _hostedServices = new();
    private bool _started;
    private bool _stopped;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="Host"/>.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="applicationLifetime">The application lifetime.</param>
    public Host(DependencyInjection.IServiceProvider serviceProvider, HostApplicationLifetime applicationLifetime)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
    }

    /// <summary>
    /// The program's configured services.
    /// </summary>
    public DependencyInjection.IServiceProvider Services => _serviceProvider;

    /// <summary>
    /// Starts the host.
    /// </summary>
    /// <param name="cancellationToken">Used to abort program start.</param>
    /// <returns>A <see cref="Task"/> that completes when the <see cref="IHost"/> starts.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started)
        {
            throw new InvalidOperationException("Host has already been started.");
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Host));
        }

        // Discover all hosted services by resolving IEnumerable<IHostedService>
        var hostedServicesEnumerable = _serviceProvider.GetService(typeof(IEnumerable<IHostedService>));
        if (hostedServicesEnumerable is IEnumerable<IHostedService> hostedServicesCollection)
        {
            _hostedServices.AddRange(hostedServicesCollection);
        }

        // Start hosted services in registration order
        foreach (var hostedService in _hostedServices)
        {
            await hostedService.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        _started = true;
        _applicationLifetime.NotifyStarted();
    }

    /// <summary>
    /// Attempts to gracefully stop the host.
    /// </summary>
    /// <param name="cancellationToken">Used to indicate when stop should no longer be graceful.</param>
    /// <returns>A <see cref="Task"/> that completes when the <see cref="IHost"/> stops.</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_stopped)
        {
            return;
        }

        if (!_started)
        {
            _stopped = true;
            return;
        }

        // Trigger ApplicationStopping
        _applicationLifetime.NotifyStopping();

        // Stop hosted services in reverse order
        for (int i = _hostedServices.Count - 1; i >= 0; i--)
        {
            try
            {
                await _hostedServices[i].StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Log but continue stopping other services
            }
        }

        _stopped = true;
        _applicationLifetime.NotifyStopped();
    }

    /// <summary>
    /// Disposes the host.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (!_stopped)
        {
            // Try to stop gracefully, but don't wait
            try
            {
                StopAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // Ignore errors during disposal
            }
        }

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
    }
}

