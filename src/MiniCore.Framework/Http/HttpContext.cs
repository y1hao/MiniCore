using System.Collections;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Routing.Abstractions;

namespace MiniCore.Framework.Http;

/// <summary>
/// Default implementation of <see cref="IHttpContext"/>.
/// </summary>
public class HttpContext : IHttpContext
{
    private bool _aborted;
    private RouteData? _routeData;

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

    /// <summary>
    /// Gets or sets the route data for this request.
    /// </summary>
    public RouteData? RouteData
    {
        get => _routeData;
        set
        {
            _routeData = value;
            // Also store route values in Items for compatibility
            if (value != null)
            {
                foreach (var kvp in value.Values)
                {
                    Items[$"route:{kvp.Key}"] = kvp.Value;
                }
            }
        }
    }

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

