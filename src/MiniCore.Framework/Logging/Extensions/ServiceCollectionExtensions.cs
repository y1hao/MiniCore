using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Logging.Console;
using MiniCore.Framework.Logging.File;

namespace MiniCore.Framework.Logging;

/// <summary>
/// Extension methods for setting up logging services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds logging services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddLogging(this IServiceCollection services)
    {
        return AddLogging(services, builder => { });
    }

    /// <summary>
    /// Adds logging services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">The <see cref="ILoggingBuilder"/> configuration delegate.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddLogging(
        this IServiceCollection services,
        Action<ILoggingBuilder> configure)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Create a list to store providers during configuration
        var providers = new List<ILoggerProvider>();
        services.AddSingleton<ILoggerFactory>(sp =>
        {
            var factory = new LoggerFactory();
            // Add all providers that were registered during configuration
            foreach (var provider in providers)
            {
                factory.AddProvider(provider);
            }
            return factory;
        });
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        var builder = new LoggingBuilder(services, providers);
        configure(builder);

        return services;
    }
}

/// <summary>
/// An interface for configuring logging providers.
/// </summary>
public interface ILoggingBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> where logging services are configured.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Adds a console logger.
    /// </summary>
    /// <param name="minLevel">The minimum log level.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    ILoggingBuilder AddConsole(LogLevel minLevel = LogLevel.Information);

    /// <summary>
    /// Adds a file logger.
    /// </summary>
    /// <param name="logFilePath">The path to the log file.</param>
    /// <param name="minLevel">The minimum log level.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    ILoggingBuilder AddFile(string logFilePath, LogLevel minLevel = LogLevel.Information);
}

internal class LoggingBuilder : ILoggingBuilder
{
    public IServiceCollection Services { get; }
    private readonly List<ILoggerProvider> _providers;

    public LoggingBuilder(IServiceCollection services, List<ILoggerProvider> providers)
    {
        Services = services;
        _providers = providers;
    }

    public ILoggingBuilder AddConsole(LogLevel minLevel = LogLevel.Information)
    {
        var provider = new ConsoleLoggerProvider(minLevel);
        _providers.Add(provider);
        Services.AddSingleton<ILoggerProvider>(sp => provider);
        return this;
    }

    public ILoggingBuilder AddFile(string logFilePath, LogLevel minLevel = LogLevel.Information)
    {
        var provider = new FileLoggerProvider(logFilePath, minLevel);
        _providers.Add(provider);
        Services.AddSingleton<ILoggerProvider>(sp => provider);
        return this;
    }
}

