using System.Reflection;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Controllers;
using MiniCore.Framework.Routing.Attributes;

namespace MiniCore.Framework.Mvc;

/// <summary>
/// Discovers controllers and their action methods.
/// </summary>
public class ControllerDiscovery : IControllerDiscovery
{
    /// <inheritdoc />
    public IEnumerable<ControllerInfo> DiscoverControllers(params Assembly[] assemblies)
    {
        if (assemblies == null || assemblies.Length == 0)
        {
            assemblies = new[] { Assembly.GetCallingAssembly() };
        }

        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // If some types can't be loaded, use the ones that were loaded
                types = ex.Types.Where(t => t != null).ToArray()!;
            }

            var controllerTypes = types
                .Where(t => t != null &&
                           t.IsClass &&
                           !t.IsAbstract &&
                           (t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) ||
                            t.GetCustomAttribute<ControllerAttribute>() != null) &&
                           (typeof(Abstractions.IController).IsAssignableFrom(t) ||
                            typeof(ControllerBase).IsAssignableFrom(t)));

            foreach (var controllerType in controllerTypes)
            {
                var routeAttr = controllerType.GetCustomAttribute<RouteAttribute>();
                yield return new ControllerInfo
                {
                    ControllerType = controllerType,
                    RoutePrefix = GetTemplate(routeAttr)
                };
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<ActionMethodInfo> GetActionMethods(Type controllerType)
    {
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName &&
                       m.GetCustomAttribute<NonActionAttribute>() == null);

        foreach (var method in methods)
        {
            var httpGetAttrs = method.GetCustomAttributes<HttpGetAttribute>();
            var httpPostAttrs = method.GetCustomAttributes<HttpPostAttribute>();
            var httpPutAttrs = method.GetCustomAttributes<HttpPutAttribute>();
            var httpDeleteAttrs = method.GetCustomAttributes<HttpDeleteAttribute>();
            var httpPatchAttrs = method.GetCustomAttributes<HttpPatchAttribute>();
            var routeAttr = method.GetCustomAttribute<RouteAttribute>();

            var httpMethods = new List<(string Method, string? Template)>();

            foreach (var attr in httpGetAttrs)
            {
                httpMethods.Add((attr.HttpMethod, attr.Template));
            }
            foreach (var attr in httpPostAttrs)
            {
                httpMethods.Add((attr.HttpMethod, attr.Template));
            }
            foreach (var attr in httpPutAttrs)
            {
                httpMethods.Add((attr.HttpMethod, attr.Template));
            }
            foreach (var attr in httpDeleteAttrs)
            {
                httpMethods.Add((attr.HttpMethod, attr.Template));
            }
            foreach (var attr in httpPatchAttrs)
            {
                httpMethods.Add((attr.HttpMethod, attr.Template));
            }

            // If no HTTP method attribute, default to GET
            if (httpMethods.Count == 0)
            {
                httpMethods.Add(("GET", routeAttr?.Template));
            }
            else if (routeAttr != null)
            {
                // Apply route template to all HTTP methods
                for (int i = 0; i < httpMethods.Count; i++)
                {
                    httpMethods[i] = (httpMethods[i].Method, routeAttr.Template);
                }
            }

            if (httpMethods.Count > 0)
            {
                yield return new ActionMethodInfo
                {
                    Method = method,
                    HttpMethods = httpMethods
                };
            }
        }
    }

    private static string? GetTemplate(RouteAttribute? attribute)
    {
        return attribute?.Template;
    }
}

