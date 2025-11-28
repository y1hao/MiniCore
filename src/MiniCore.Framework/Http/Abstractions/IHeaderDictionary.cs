using MiniCore.Framework.Http;

namespace MiniCore.Framework.Http.Abstractions;

/// <summary>
/// Represents a collection of HTTP headers.
/// </summary>
public interface IHeaderDictionary : IDictionary<string, StringValues>
{
    /// <summary>
    /// Gets or sets the Content-Type header.
    /// </summary>
    string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the Content-Length header.
    /// </summary>
    long? ContentLength { get; set; }
}

