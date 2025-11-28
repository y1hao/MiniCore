using MiniCore.Framework.Http.Abstractions;

namespace MiniCore.Framework.Mvc.Abstractions;

/// <summary>
/// Context object for execution of action which has been selected as part of an HTTP request.
/// </summary>
public class ActionContext
{
    /// <summary>
    /// Gets or sets the HTTP context.
    /// </summary>
    public IHttpContext HttpContext { get; set; } = null!;

    /// <summary>
    /// Gets or sets the route data.
    /// </summary>
    public Routing.Abstractions.RouteData? RouteData { get; set; }
}

