using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Routing.Abstractions;

namespace MiniCore.Framework.Routing;

/// <summary>
/// Default implementation of <see cref="IEndpointRouteBuilder"/>.
/// </summary>
public class EndpointRouteBuilder : IEndpointRouteBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointRouteBuilder"/> class.
    /// </summary>
    /// <param name="routeRegistry">The route registry.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public EndpointRouteBuilder(IRouteRegistry routeRegistry, DependencyInjection.IServiceProvider serviceProvider)
    {
        RouteRegistry = routeRegistry ?? throw new ArgumentNullException(nameof(routeRegistry));
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public IRouteRegistry RouteRegistry { get; }

    /// <inheritdoc />
    public DependencyInjection.IServiceProvider ServiceProvider { get; }
}

