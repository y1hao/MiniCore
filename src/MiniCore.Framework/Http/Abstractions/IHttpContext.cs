using MiniCore.Framework.DependencyInjection;

namespace MiniCore.Framework.Http.Abstractions;

/// <summary>
/// Encapsulates all HTTP-specific information about an individual HTTP request.
/// </summary>
public interface IHttpContext
{
    /// <summary>
    /// Gets the <see cref="IHttpRequest"/> object for this request.
    /// </summary>
    IHttpRequest Request { get; }

    /// <summary>
    /// Gets the <see cref="IHttpResponse"/> object for this request.
    /// </summary>
    IHttpResponse Response { get; }

    /// <summary>
    /// Gets or sets a key/value collection that can be used to share data within the scope of this request.
    /// </summary>
    IDictionary<object, object?> Items { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DependencyInjection.IServiceProvider"/> that provides access to the request's service container.
    /// </summary>
    DependencyInjection.IServiceProvider RequestServices { get; set; }

    /// <summary>
    /// Aborts the connection underlying this request.
    /// </summary>
    void Abort();
}

