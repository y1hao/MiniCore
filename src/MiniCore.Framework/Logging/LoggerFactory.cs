using System.Collections.Concurrent;

namespace MiniCore.Framework.Logging;

/// <summary>
/// Produces instances of <see cref="ILogger"/> classes based on the given providers.
/// </summary>
public class LoggerFactory : ILoggerFactory
{
    private readonly ConcurrentDictionary<string, ILogger> _loggers = new();
    private readonly List<ILoggerProvider> _providers = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new <see cref="ILogger"/> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The <see cref="ILogger"/>.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        if (string.IsNullOrEmpty(categoryName))
        {
            throw new ArgumentException("Category name cannot be null or empty.", nameof(categoryName));
        }

        return _loggers.GetOrAdd(categoryName, name => new Logger(name, this));
    }

    /// <summary>
    /// Adds an <see cref="ILoggerProvider"/> to the logging system.
    /// </summary>
    /// <param name="provider">The <see cref="ILoggerProvider"/>.</param>
    public void AddProvider(ILoggerProvider provider)
    {
        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        lock (_lock)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }

            _providers.Add(provider);
            // Clear cached loggers so they can be recreated with the new provider
            _loggers.Clear();
        }
    }

    /// <summary>
    /// Gets all registered providers.
    /// </summary>
    internal IReadOnlyList<ILoggerProvider> GetProviders()
    {
        lock (_lock)
        {
            return _providers.ToList();
        }
    }

    /// <summary>
    /// Disposes the factory and all registered providers.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (var provider in _providers)
            {
                try
                {
                    provider.Dispose();
                }
                catch
                {
                    // Ignore exceptions during disposal
                }
            }

            _providers.Clear();
            _loggers.Clear();
        }
    }
}

