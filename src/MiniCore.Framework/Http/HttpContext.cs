using System.Collections;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Http.Abstractions;

namespace MiniCore.Framework.Http;

/// <summary>
/// Default implementation of <see cref="IHttpContext"/>.
/// </summary>
public class HttpContext : IHttpContext
{
    private bool _aborted;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpContext"/> class.
    /// </summary>
    public HttpContext()
    {
        Request = new HttpRequest(this);
        Response = new HttpResponse(this);
        Items = new Dictionary<object, object?>();
    }

    /// <inheritdoc />
    public IHttpRequest Request { get; }

    /// <inheritdoc />
    public IHttpResponse Response { get; }

    /// <inheritdoc />
    public IDictionary<object, object?> Items { get; set; }

    /// <inheritdoc />
    public DependencyInjection.IServiceProvider RequestServices { get; set; } = null!;

    /// <inheritdoc />
    public void Abort()
    {
        _aborted = true;
    }

    /// <summary>
    /// Gets a value indicating whether the request has been aborted.
    /// </summary>
    public bool IsAborted => _aborted;
}

