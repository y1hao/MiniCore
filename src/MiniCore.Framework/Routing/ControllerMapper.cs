using System.Reflection;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Mvc;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.ModelBinding;
using MiniCore.Framework.Routing.Abstractions;
using MiniCore.Framework.Routing.Attributes;

namespace MiniCore.Framework.Routing;

/// <summary>
/// Maps controllers to routes using reflection.
/// Uses the MVC framework (Phase 8) for controller discovery and action invocation.
/// </summary>
public class ControllerMapper
{
    private readonly IRouteRegistry _routeRegistry;
    private readonly DependencyInjection.IServiceProvider _serviceProvider;
    private readonly IControllerDiscovery _controllerDiscovery;
    private readonly IModelBinder _modelBinder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControllerMapper"/> class.
    /// </summary>
    /// <param name="routeRegistry">The route registry.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="controllerDiscovery">The controller discovery service.</param>
    /// <param name="modelBinder">The model binder.</param>
    public ControllerMapper(
        IRouteRegistry routeRegistry,
        DependencyInjection.IServiceProvider serviceProvider,
        IControllerDiscovery? controllerDiscovery = null,
        IModelBinder? modelBinder = null)
    {
        _routeRegistry = routeRegistry ?? throw new ArgumentNullException(nameof(routeRegistry));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _controllerDiscovery = controllerDiscovery ?? new ControllerDiscovery();
        _modelBinder = modelBinder ?? new DefaultModelBinder();
    }

    /// <summary>
    /// Maps controllers from the specified assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for controllers. If not specified, uses calling assembly.</param>
    public void MapControllers(params Assembly[] assemblies)
    {
        if (assemblies == null || assemblies.Length == 0)
        {
            assemblies = new[] { Assembly.GetCallingAssembly() };
        }

        var controllers = _controllerDiscovery.DiscoverControllers(assemblies);

        foreach (var controllerInfo in controllers)
        {
            MapController(controllerInfo);
        }
    }

    private void MapController(ControllerInfo controllerInfo)
    {
        var controllerType = controllerInfo.ControllerType;
        var controllerRoutePrefix = controllerInfo.RoutePrefix ?? "";

        var actionMethods = _controllerDiscovery.GetActionMethods(controllerType);

        foreach (var actionMethodInfo in actionMethods)
        {
            // Build route pattern for each HTTP method
            foreach (var (httpMethod, methodRouteTemplate) in actionMethodInfo.HttpMethods)
            {
                var routePattern = BuildRoutePattern(controllerRoutePrefix, methodRouteTemplate);
                var handler = CreateControllerHandler(controllerType, actionMethodInfo.Method);
                _routeRegistry.Map(httpMethod, routePattern, handler);
            }
        }
    }

    private static string BuildRoutePattern(string controllerPrefix, string? methodTemplate)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(controllerPrefix))
        {
            // Handle absolute routes (starting with /)
            if (controllerPrefix.StartsWith("/"))
            {
                return controllerPrefix;
            }
            parts.Add(controllerPrefix.TrimStart('/'));
        }

        if (!string.IsNullOrEmpty(methodTemplate))
        {
            // Handle absolute routes (starting with /)
            if (methodTemplate.StartsWith("/"))
            {
                return methodTemplate;
            }
            parts.Add(methodTemplate.TrimStart('/'));
        }

        return "/" + string.Join("/", parts);
    }

    private RequestDelegate CreateControllerHandler(Type controllerType, MethodInfo method)
    {
        var invoker = new ControllerActionInvoker(controllerType, method, _serviceProvider, _modelBinder);

        return async context =>
        {
            var routeData = (context as Http.HttpContext)?.RouteData;
            var actionContext = new ActionContext
            {
                HttpContext = context,
                RouteData = routeData
            };

            await invoker.InvokeAsync(actionContext);
        };
    }

    /// <summary>
    /// Maps a fallback route to a controller action.
    /// </summary>
    /// <param name="action">The action name.</param>
    /// <param name="controller">The controller name.</param>
    /// <param name="pattern">The route pattern.</param>
    public void MapFallbackToController(string action, string controller, string? pattern = null)
    {
        // Find the controller type
        var controllers = _controllerDiscovery.DiscoverControllers(Assembly.GetCallingAssembly());
        var controllerInfo = controllers.FirstOrDefault(c => 
            c.ControllerType.Name.Equals(controller + "Controller", StringComparison.OrdinalIgnoreCase) ||
            c.ControllerType.Name.Equals(controller, StringComparison.OrdinalIgnoreCase));

        if (controllerInfo == null)
        {
            throw new InvalidOperationException($"Controller '{controller}' not found.");
        }

        // Find the action method
        var actionMethods = _controllerDiscovery.GetActionMethods(controllerInfo.ControllerType);
        var actionMethodInfo = actionMethods.FirstOrDefault(a => 
            a.Method.Name.Equals(action, StringComparison.OrdinalIgnoreCase));

        if (actionMethodInfo == null)
        {
            throw new InvalidOperationException($"Action '{action}' not found on controller '{controller}'.");
        }

        // Create handler using the first HTTP method (or GET if none specified)
        var httpMethod = actionMethodInfo.HttpMethods.FirstOrDefault().Method ?? "GET";
        var handler = CreateControllerHandler(controllerInfo.ControllerType, actionMethodInfo.Method);

        _routeRegistry.MapFallback(handler);
    }
}

