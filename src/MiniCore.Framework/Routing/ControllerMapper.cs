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
    /// <param name="assemblies">The assemblies to scan for controllers. If not specified, searches all loaded assemblies.</param>
    public void MapControllers(params Assembly[] assemblies)
    {
        // Track whether assemblies were explicitly provided before any reassignment
        var wasExplicitAssembly = assemblies != null && assemblies.Length > 0;

        if (assemblies == null || assemblies.Length == 0)
        {
            // Search all loaded assemblies to find controllers (needed for test scenarios)
            assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .ToArray();
        }

        var controllers = _controllerDiscovery.DiscoverControllers(assemblies).ToList();

        // If no controllers found with specified assemblies, try searching all assemblies as fallback
        if (controllers.Count == 0)
        {
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .ToArray();
            controllers = _controllerDiscovery.DiscoverControllers(allAssemblies).ToList();
        }

        // If still no controllers found and assemblies were explicitly provided, this indicates a problem
        if (controllers.Count == 0 && wasExplicitAssembly)
        {
            var assemblyNames = string.Join(", ", assemblies.Select(a => a.GetName().Name ?? "Unknown"));
            throw new InvalidOperationException(
                $"No controllers found in the specified assembly(ies): {assemblyNames}. " +
                "Ensure controllers inherit from ControllerBase and are public non-abstract classes.");
        }

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
                // If method template is also absolute, it takes precedence
                if (!string.IsNullOrEmpty(methodTemplate) && methodTemplate.StartsWith("/"))
                {
                    return methodTemplate;
                }
                // Split the prefix into segments and add them
                var prefixSegments = controllerPrefix.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                parts.AddRange(prefixSegments);
            }
            else
            {
                // Split the prefix into segments and add them
                var prefixSegments = controllerPrefix.Split('/', StringSplitOptions.RemoveEmptyEntries);
                parts.AddRange(prefixSegments);
            }
        }

        if (!string.IsNullOrEmpty(methodTemplate))
        {
            // Handle absolute routes (starting with /)
            if (methodTemplate.StartsWith("/"))
            {
                return methodTemplate;
            }
            // Split the template into segments and add them
            var templateSegments = methodTemplate.Split('/', StringSplitOptions.RemoveEmptyEntries);
            parts.AddRange(templateSegments);
        }

        return "/" + string.Join("/", parts);
    }

    private RequestDelegate CreateControllerHandler(Type controllerType, MethodInfo method)
    {
        var invoker = new ControllerActionInvoker(controllerType, method, _serviceProvider, _modelBinder);

        // Get controller name (remove "Controller" suffix if present)
        var controllerName = controllerType.Name;
        if (controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
        {
            controllerName = controllerName.Substring(0, controllerName.Length - 10);
        }

        // Get action name
        var actionName = method.Name;

        return async context =>
        {
            var routeData = (context as Http.HttpContext)?.RouteData;
            
            // Ensure RouteData exists and populate controller/action
            if (routeData == null)
            {
                routeData = new RouteData();
                if (context is Http.HttpContext httpContext)
                {
                    httpContext.RouteData = routeData;
                }
            }

            // Set controller and action in route data
            routeData.Values["controller"] = controllerName;
            routeData.Values["action"] = actionName;

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
        // First try the calling assembly, then search all loaded assemblies
        var assemblies = new[] { Assembly.GetCallingAssembly() };
        var controllers = _controllerDiscovery.DiscoverControllers(assemblies);
        var controllerInfo = controllers.FirstOrDefault(c => 
            c.ControllerType.Name.Equals(controller + "Controller", StringComparison.OrdinalIgnoreCase) ||
            c.ControllerType.Name.Equals(controller, StringComparison.OrdinalIgnoreCase));

        // If not found, search all loaded assemblies (needed for test scenarios)
        if (controllerInfo == null)
        {
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .ToArray();
            controllers = _controllerDiscovery.DiscoverControllers(allAssemblies);
            controllerInfo = controllers.FirstOrDefault(c => 
                c.ControllerType.Name.Equals(controller + "Controller", StringComparison.OrdinalIgnoreCase) ||
                c.ControllerType.Name.Equals(controller, StringComparison.OrdinalIgnoreCase));
        }

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

