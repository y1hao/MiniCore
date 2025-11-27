namespace MiniCore.Framework.Logging;

/// <summary>
/// Extension methods for <see cref="ILoggerFactory"/>.
/// </summary>
public static class LoggerFactoryExtensions
{
    /// <summary>
    /// Creates a new <see cref="ILogger"/> instance using the full name of the given type.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="factory">The factory.</param>
    /// <returns>The <see cref="ILogger"/> that was created.</returns>
    public static ILogger<T> CreateLogger<T>(this ILoggerFactory factory)
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        return new Logger<T>(factory);
    }
}

