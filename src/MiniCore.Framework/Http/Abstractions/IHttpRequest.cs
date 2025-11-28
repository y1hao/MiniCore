namespace MiniCore.Framework.Http.Abstractions;

/// <summary>
/// Represents the incoming side of an individual HTTP request.
/// </summary>
public interface IHttpRequest
{
    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    string Method { get; set; }

    /// <summary>
    /// Gets or sets the request path from RequestPath.
    /// </summary>
    string Path { get; set; }

    /// <summary>
    /// Gets or sets the query string from RequestQueryString.
    /// </summary>
    string QueryString { get; set; }

    /// <summary>
    /// Gets the request headers.
    /// </summary>
    IHeaderDictionary Headers { get; }

    /// <summary>
    /// Gets the request body as a stream.
    /// </summary>
    Stream Body { get; set; }

    /// <summary>
    /// Gets or sets the Content-Length header value if it is available.
    /// </summary>
    long? ContentLength { get; set; }

    /// <summary>
    /// Gets or sets the Content-Type header value.
    /// </summary>
    string? ContentType { get; set; }

    /// <summary>
    /// Gets the request path base.
    /// </summary>
    string PathBase { get; set; }

    /// <summary>
    /// Gets the request scheme (http or https).
    /// </summary>
    string Scheme { get; set; }

    /// <summary>
    /// Gets the request host.
    /// </summary>
    Http.HostString Host { get; set; }
}

