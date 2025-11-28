using MiniCore.Framework.Http.Abstractions;

namespace MiniCore.Framework.Mvc.Abstractions;

/// <summary>
/// Defines the contract for a controller.
/// </summary>
public interface IController
{
    /// <summary>
    /// Gets the HTTP context for the current request.
    /// </summary>
    IHttpContext HttpContext { get; set; }
}

