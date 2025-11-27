namespace MiniCore.Framework.Logging;

/// <summary>
/// Represents a type that can create instances of <see cref="ILogger"/>.
/// </summary>
public interface ILoggerProvider : IDisposable
{
    /// <summary>
    /// Creates a new <see cref="ILogger"/> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The instance of <see cref="ILogger"/> that was created.</returns>
    ILogger CreateLogger(string categoryName);
}

