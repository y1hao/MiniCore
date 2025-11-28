namespace MiniCore.Framework.Server.Abstractions;

/// <summary>
/// Represents a server that can process HTTP requests.
/// </summary>
public interface IServer
{
    /// <summary>
    /// Starts the server and begins listening for incoming requests.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the server and stops listening for incoming requests.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}

