namespace MiniCore.Framework.Http.Abstractions;

/// <summary>
/// Represents the outgoing side of an individual HTTP request.
/// </summary>
public interface IHttpResponse
{
    /// <summary>
    /// Gets the response headers.
    /// </summary>
    IHeaderDictionary Headers { get; }

    /// <summary>
    /// Gets the response body as a stream.
    /// </summary>
    Stream Body { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the Content-Length response header.
    /// </summary>
    long? ContentLength { get; set; }

    /// <summary>
    /// Gets or sets the Content-Type response header.
    /// </summary>
    string? ContentType { get; set; }

    /// <summary>
    /// Gets a value indicating whether response headers have been sent to the client.
    /// </summary>
    bool HasStarted { get; }

    /// <summary>
    /// Adds a delegate to be invoked just before response headers will be sent to the client.
    /// </summary>
    /// <param name="callback">The delegate to execute.</param>
    /// <param name="state">A state object to capture and pass back to the delegate.</param>
    void OnStarting(Func<object, Task> callback, object state);

    /// <summary>
    /// Adds a delegate to be invoked after the response has finished being sent to the client.
    /// </summary>
    /// <param name="callback">The delegate to execute.</param>
    /// <param name="state">A state object to capture and pass back to the delegate.</param>
    void OnCompleted(Func<object, Task> callback, object state);
}

