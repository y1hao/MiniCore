using MiniCore.Framework.Http.Abstractions;

namespace MiniCore.Framework.Routing.Abstractions;

/// <summary>
/// Defines a contract for a route builder in an application.
/// </summary>
public interface IEndpointRouteBuilder
{
    /// <summary>
    /// Gets the <see cref="IRouteRegistry"/> used to configure routes.
    /// </summary>
    IRouteRegistry RouteRegistry { get; }

    /// <summary>
    /// Gets the <see cref="DependencyInjection.IServiceProvider"/> used to resolve services.
    /// </summary>
    DependencyInjection.IServiceProvider ServiceProvider { get; }
}

