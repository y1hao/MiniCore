using System.Reflection;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Routing.Abstractions;
using MiniCore.Framework.Routing.Attributes;

namespace MiniCore.Framework.Routing;

/// <summary>
/// Maps controllers to routes using reflection.
/// This is a bridge implementation that works with Microsoft.AspNetCore.Mvc controllers.
/// 
/// TODO: Remove Microsoft dependencies when implementing our own MVC framework (Phase 8+).
/// This includes:
/// - Microsoft.AspNetCore.Http.HttpContext bridging
/// - Microsoft.AspNetCore.Mvc.ControllerBase/ControllerContext
/// - Microsoft.AspNetCore.Mvc.IActionResult execution
/// 
/// Note: All routing attributes (Route, HttpGet, HttpPost, etc.) are now our own implementations
/// in the MiniCore.Framework.Routing.Attributes namespace.
/// </summary>
public class ControllerMapper
{
    private readonly IRouteRegistry _routeRegistry;
    private readonly DependencyInjection.IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControllerMapper"/> class.
    /// </summary>
    /// <param name="routeRegistry">The route registry.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public ControllerMapper(IRouteRegistry routeRegistry, DependencyInjection.IServiceProvider serviceProvider)
    {
        _routeRegistry = routeRegistry ?? throw new ArgumentNullException(nameof(routeRegistry));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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

        foreach (var assembly in assemblies)
        {
            // Controller discovery: use our ControllerAttribute or convention-based (name ends with "Controller")
            var controllerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && 
                           !t.IsAbstract && 
                           (t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) ||
                            t.GetCustomAttribute<ControllerAttribute>() != null));

            foreach (var controllerType in controllerTypes)
            {
                MapController(controllerType);
            }
        }
    }

    private void MapController(Type controllerType)
    {
        // Get controller route prefix from [Route] attribute
        var controllerRouteAttr = controllerType.GetCustomAttribute<RouteAttribute>();
        var controllerRoutePrefix = GetTemplate(controllerRouteAttr) ?? "";

        // Get all public instance methods, excluding those marked with [NonAction]
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && 
                        m.GetCustomAttribute<NonActionAttribute>() == null);

        foreach (var method in methods)
        {
            // Get HTTP method attributes
            var httpGetAttr = method.GetCustomAttribute<HttpGetAttribute>();
            var httpPostAttr = method.GetCustomAttribute<HttpPostAttribute>();
            var httpPutAttr = method.GetCustomAttribute<HttpPutAttribute>();
            var httpDeleteAttr = method.GetCustomAttribute<HttpDeleteAttribute>();
            var httpPatchAttr = method.GetCustomAttribute<HttpPatchAttribute>();
            var routeAttr = method.GetCustomAttribute<RouteAttribute>();

            // Determine HTTP methods and route templates
            var httpMethods = new List<(string Method, string? Template)>();
            
            if (httpGetAttr != null)
            {
                httpMethods.Add((httpGetAttr.HttpMethod, httpGetAttr.Template));
            }
            if (httpPostAttr != null)
            {
                httpMethods.Add((httpPostAttr.HttpMethod, httpPostAttr.Template));
            }
            if (httpPutAttr != null)
            {
                httpMethods.Add((httpPutAttr.HttpMethod, httpPutAttr.Template));
            }
            if (httpDeleteAttr != null)
            {
                httpMethods.Add((httpDeleteAttr.HttpMethod, httpDeleteAttr.Template));
            }
            if (httpPatchAttr != null)
            {
                httpMethods.Add((httpPatchAttr.HttpMethod, httpPatchAttr.Template));
            }
            if (routeAttr != null && httpMethods.Count == 0)
            {
                // If only [Route] attribute, default to GET
                httpMethods.Add(("GET", routeAttr.Template));
            }
            if (httpMethods.Count == 0)
            {
                // If no HTTP method attribute, try to infer from method name
                var methodName = method.Name;
                if (methodName.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
                {
                    httpMethods.Add(("GET", null));
                }
                else if (methodName.StartsWith("Post", StringComparison.OrdinalIgnoreCase))
                {
                    httpMethods.Add(("POST", null));
                }
                else if (methodName.StartsWith("Put", StringComparison.OrdinalIgnoreCase))
                {
                    httpMethods.Add(("PUT", null));
                }
                else if (methodName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase))
                {
                    httpMethods.Add(("DELETE", null));
                }
                else
                {
                    // Default to GET
                    httpMethods.Add(("GET", null));
                }
            }

            // Build route pattern for each HTTP method
            foreach (var (httpMethod, methodRouteTemplate) in httpMethods)
            {
                var routePattern = BuildRoutePattern(controllerRoutePrefix, methodRouteTemplate);
                var handler = CreateControllerHandler(controllerType, method);
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
        return async context =>
        {
            // TODO: Phase 8+ - Remove Microsoft.AspNetCore.Http.HttpContext bridging
            // When we implement our own MVC framework, controllers will use our HttpContext directly
            // Get Microsoft's HttpContext from our context
            // We need to bridge our HttpContext with Microsoft's HttpContext
            var microsoftHttpContext = GetMicrosoftHttpContext(context);
            
            if (microsoftHttpContext == null)
            {
                context.Response.StatusCode = 500;
                await WriteText(context.Response.Body, "Failed to bridge HttpContext. Ensure Microsoft.AspNetCore packages are referenced.");
                return;
            }

            // Create controller instance using DI
            var controller = CreateControllerInstance(controllerType);
            if (controller == null)
            {
                context.Response.StatusCode = 500;
                await WriteText(context.Response.Body, "Failed to create controller instance");
                return;
            }

            // TODO: Phase 8+ - Remove Microsoft.AspNetCore.Mvc.ControllerBase/ControllerContext dependency
            // When we implement our own MVC framework, controllers will use our own base classes
            // Set controller context using reflection
            var controllerBaseType = Type.GetType("Microsoft.AspNetCore.Mvc.ControllerBase, Microsoft.AspNetCore.Mvc.Core");
            if (controllerBaseType != null && controllerBaseType.IsAssignableFrom(controllerType))
            {
                var controllerContextType = Type.GetType("Microsoft.AspNetCore.Mvc.ControllerContext, Microsoft.AspNetCore.Mvc.Core");
                if (controllerContextType != null)
                {
                    var controllerContext = Activator.CreateInstance(controllerContextType);
                    var httpContextProp = controllerContextType.GetProperty("HttpContext");
                    httpContextProp?.SetValue(controllerContext, microsoftHttpContext);
                    
                    var controllerContextProp = controllerType.GetProperty("ControllerContext");
                    controllerContextProp?.SetValue(controller, controllerContext);
                }
            }

            // Extract route parameters
            var routeData = (context as Http.HttpContext)?.RouteData;
            var routeValues = routeData?.Values ?? new Dictionary<string, string>();

            // Get method parameters and bind them
            var methodParams = method.GetParameters();
            var args = new object[methodParams.Length];

            for (int i = 0; i < methodParams.Length; i++)
            {
                var param = methodParams[i];
                var paramName = param.Name ?? "";

                // Try to get from route values first
                if (routeValues.TryGetValue(paramName, out var routeValue))
                {
                    args[i] = ConvertValue(routeValue, param.ParameterType);
                }
                else
                {
                    // Try query string - parse manually
                    var queryValue = context.Request.QueryString;
                    if (!string.IsNullOrEmpty(queryValue))
                    {
                        var queryString = queryValue.TrimStart('?');
                        var queryParams = ParseQueryString(queryString);
                        if (queryParams.TryGetValue(paramName, out var queryParamValue) && !string.IsNullOrEmpty(queryParamValue))
                        {
                            args[i] = ConvertValue(queryParamValue, param.ParameterType);
                            continue;
                        }
                    }

                    // Use default value if available
                    if (param.HasDefaultValue)
                    {
                        args[i] = param.DefaultValue ?? GetDefault(param.ParameterType);
                    }
                    else
                    {
                        args[i] = GetDefault(param.ParameterType);
                    }
                }
            }

            // Invoke method
            object? result;
            try
            {
                result = method.Invoke(controller, args);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await WriteText(context.Response.Body, $"Error invoking controller method: {ex.Message}");
                return;
            }

            // Handle async methods
            if (result is Task invokeTask)
            {
                await invokeTask;
                
                // If task returns a value, get it
                if (method.ReturnType.IsGenericType && 
                    method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultProperty = invokeTask.GetType().GetProperty("Result");
                    result = resultProperty?.GetValue(invokeTask);
                }
                else
                {
                    result = null;
                }
            }

            // TODO: Phase 8+ - Replace Microsoft.AspNetCore.Mvc.IActionResult with our own result execution
            // When we implement our own MVC framework, we'll have our own result types and execution pipeline
            // Execute ActionResult using reflection
            var actionResultType = Type.GetType("Microsoft.AspNetCore.Mvc.IActionResult, Microsoft.AspNetCore.Mvc.Core");
            if (actionResultType != null && actionResultType.IsAssignableFrom(result?.GetType() ?? typeof(object)))
            {
                var actionContextType = Type.GetType("Microsoft.AspNetCore.Mvc.ActionContext, Microsoft.AspNetCore.Mvc.Core");
                if (actionContextType != null)
                {
                    var actionContext = Activator.CreateInstance(actionContextType);
                    var httpContextProp = actionContextType.GetProperty("HttpContext");
                    httpContextProp?.SetValue(actionContext, microsoftHttpContext);
                    
                    var executeResultAsyncMethod = actionResultType.GetMethod("ExecuteResultAsync");
                    if (executeResultAsyncMethod != null && result != null)
                    {
                        var task = executeResultAsyncMethod.Invoke(result, new[] { actionContext }) as Task;
                        if (task != null)
                        {
                            await task;
                            return;
                        }
                    }
                }
            }
            else if (result != null)
            {
                // Handle non-ActionResult return types (shouldn't happen with proper controllers)
                context.Response.ContentType = "application/json";
                var json = System.Text.Json.JsonSerializer.Serialize(result);
                await WriteText(context.Response.Body, json);
            }
        };
    }

    private object? CreateControllerInstance(Type controllerType)
    {
        // Try to get from DI first
        var instance = _serviceProvider.GetService(controllerType);
        if (instance != null)
        {
            return instance;
        }

        // Try to create with constructor injection
        var constructors = controllerType.GetConstructors();
        if (constructors.Length > 0)
        {
            var constructor = constructors[0];
            var parameters = constructor.GetParameters();
            var args = new object[parameters.Length];
            
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                var service = _serviceProvider.GetService(paramType);
                if (service == null)
                {
                    return null; // Can't resolve dependency
                }
                args[i] = service;
            }
            
            return Activator.CreateInstance(controllerType, args);
        }

        // Try parameterless constructor
        return Activator.CreateInstance(controllerType);
    }

    /// <summary>
    /// Creates a Microsoft.AspNetCore.Http.HttpContext adapter from our HttpContext.
    /// TODO: Phase 8+ - Remove this method when we implement our own MVC framework.
    /// Controllers will then use our HttpContext directly without bridging.
    /// </summary>
    private object? GetMicrosoftHttpContext(IHttpContext context)
    {
        // Try to get Microsoft's HttpContext from Items dictionary
        // This might be set by a bridge/adapter
        if (context.Items.TryGetValue("Microsoft.AspNetCore.Http.HttpContext", out var msContext))
        {
            return msContext;
        }

        // TODO: Phase 8+ - Remove Microsoft.AspNetCore.Http.DefaultHttpContext dependency
        // Create a minimal adapter using Microsoft's DefaultHttpContext via reflection
        // This is a bridge to allow Microsoft's MVC to work with our HttpContext
        try
        {
            // Use reflection to access Microsoft.AspNetCore.Http types
            var defaultHttpContextType = Type.GetType("Microsoft.AspNetCore.Http.DefaultHttpContext, Microsoft.AspNetCore.Http.Abstractions");
            if (defaultHttpContextType == null)
            {
                return null;
            }

            var msHttpContext = Activator.CreateInstance(defaultHttpContextType);
            if (msHttpContext == null)
            {
                return null;
            }

            // Get Request and Response properties
            var requestProp = defaultHttpContextType.GetProperty("Request");
            var responseProp = defaultHttpContextType.GetProperty("Response");
            var itemsProp = defaultHttpContextType.GetProperty("Items");
            
            if (requestProp == null || responseProp == null || itemsProp == null)
            {
                return null;
            }

            var request = requestProp.GetValue(msHttpContext);
            var response = responseProp.GetValue(msHttpContext);
            var items = itemsProp.GetValue(msHttpContext) as IDictionary<object, object?>;
            
            if (request == null || response == null)
            {
                return null;
            }

            // Set request properties via reflection
            SetProperty(request, "Method", context.Request.Method);
            SetProperty(request, "Path", CreatePathString(context.Request.Path ?? "/"));
            SetProperty(request, "QueryString", CreateQueryString(context.Request.QueryString ?? ""));
            SetProperty(request, "Scheme", context.Request.Scheme ?? "http");
            SetProperty(request, "Body", context.Request.Body);
            SetProperty(request, "ContentType", context.Request.ContentType);
            SetProperty(request, "ContentLength", context.Request.ContentLength);
            
            // Copy request headers
            var requestHeadersProp = request.GetType().GetProperty("Headers");
            if (requestHeadersProp != null)
            {
                var requestHeaders = requestHeadersProp.GetValue(request);
                if (requestHeaders != null)
                {
                    var headersDict = requestHeaders as IDictionary<string, object>;
                    if (headersDict != null)
                    {
                        foreach (var header in context.Request.Headers)
                        {
                            headersDict[header.Key] = CreateStringValues(header.Value);
                        }
                    }
                }
            }
            
            // Set response properties
            SetProperty(response, "Body", context.Response.Body);
            SetProperty(response, "StatusCode", context.Response.StatusCode);
            SetProperty(response, "ContentType", context.Response.ContentType);
            SetProperty(response, "ContentLength", context.Response.ContentLength);
            
            // Copy response headers
            var responseHeadersProp = response.GetType().GetProperty("Headers");
            if (responseHeadersProp != null)
            {
                var responseHeaders = responseHeadersProp.GetValue(response);
                if (responseHeaders != null)
                {
                    var headersDict = responseHeaders as IDictionary<string, object>;
                    if (headersDict != null)
                    {
                        foreach (var header in context.Response.Headers)
                        {
                            headersDict[header.Key] = CreateStringValues(header.Value);
                        }
                    }
                }
            }
            
            // Copy route data
            if (context is Http.HttpContext httpContext2 && httpContext2.RouteData != null)
            {
                var setRouteDataMethod = defaultHttpContextType.GetMethod("SetRouteData");
                if (setRouteDataMethod != null)
                {
                    var routeDataType = Type.GetType("Microsoft.AspNetCore.Routing.RouteData, Microsoft.AspNetCore.Routing.Abstractions");
                    if (routeDataType != null)
                    {
                        var routeData = Activator.CreateInstance(routeDataType);
                        var valuesProp = routeDataType.GetProperty("Values");
                        if (valuesProp != null)
                        {
                            var values = valuesProp.GetValue(routeData) as IDictionary<string, object>;
                            if (values != null)
                            {
                                foreach (var kvp in httpContext2.RouteData.Values)
                                {
                                    values[kvp.Key] = kvp.Value;
                                }
                            }
                        }
                        setRouteDataMethod.Invoke(msHttpContext, new[] { routeData });
                    }
                }
            }
            
            // Store our context in Items for reference
            if (items != null)
            {
                items["MiniCore.HttpContext"] = context;
            }
            
            return msHttpContext;
        }
        catch
        {
            return null;
        }
    }

    private static void SetProperty(object obj, string propertyName, object? value)
    {
        var prop = obj.GetType().GetProperty(propertyName);
        prop?.SetValue(obj, value);
    }

    private static object? CreatePathString(string path)
    {
        var pathStringType = Type.GetType("Microsoft.AspNetCore.Http.PathString, Microsoft.AspNetCore.Http.Abstractions");
        if (pathStringType == null)
        {
            return null;
        }
        return Activator.CreateInstance(pathStringType, path);
    }

    private static object? CreateQueryString(string queryString)
    {
        var queryStringType = Type.GetType("Microsoft.AspNetCore.Http.QueryString, Microsoft.AspNetCore.Http.Abstractions");
        if (queryStringType == null)
        {
            return null;
        }
        return Activator.CreateInstance(queryStringType, queryString);
    }

    private static object? CreateStringValues(string[] values)
    {
        var stringValuesType = Type.GetType("Microsoft.Extensions.Primitives.StringValues, Microsoft.Extensions.Primitives");
        if (stringValuesType == null)
        {
            return values.Length > 0 ? values[0] : string.Empty;
        }
        if (values.Length == 1)
        {
            return Activator.CreateInstance(stringValuesType, values[0]);
        }
        return Activator.CreateInstance(stringValuesType, values);
    }

    private static object? ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(string))
        {
            return value;
        }

        if (targetType == typeof(int) && int.TryParse(value, out var intValue))
        {
            return intValue;
        }

        if (targetType == typeof(long) && long.TryParse(value, out var longValue))
        {
            return longValue;
        }

        if (targetType == typeof(bool) && bool.TryParse(value, out var boolValue))
        {
            return boolValue;
        }

        // Try Convert.ChangeType as fallback
        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return GetDefault(targetType);
        }
    }

    private static object? GetDefault(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }

    private static async Task WriteText(Stream stream, string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        await stream.WriteAsync(bytes, 0, bytes.Length);
    }

    private static string? GetTemplate(RouteAttribute? attribute)
    {
        return attribute?.Template;
    }

    /// <summary>
    /// Maps a fallback route to a controller action.
    /// TODO: Phase 8+ - Update this to use our own controller execution when we implement our MVC framework.
    /// Currently stores route data for Microsoft's MVC to pick up, but should directly invoke our controller.
    /// </summary>
    /// <param name="action">The action name.</param>
    /// <param name="controller">The controller name.</param>
    /// <param name="pattern">The route pattern.</param>
    public void MapFallbackToController(string action, string controller, string? pattern = null)
    {
        // TODO: Phase 8+ - Replace with direct controller invocation using our own MVC framework
        // Create a handler that will delegate to Microsoft's MVC system
        // This is a bridge implementation for Phase 6
        RequestDelegate handler = async context =>
        {
            // Store controller and action in route data for Microsoft MVC to pick up
            if (context is Http.HttpContext httpContext)
            {
                if (httpContext.RouteData == null)
                {
                    httpContext.RouteData = new RouteData();
                }
                httpContext.RouteData.Values["action"] = action;
                httpContext.RouteData.Values["controller"] = controller;
                if (!string.IsNullOrEmpty(pattern))
                {
                    httpContext.RouteData.Values["path"] = pattern;
                }
            }

            // Pass through to next middleware (Microsoft's routing will handle it)
            // This is a temporary bridge until we fully implement controller routing
        };

        _routeRegistry.MapFallback(handler);
    }

    private static Dictionary<string, string> ParseQueryString(string queryString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(queryString))
        {
            return result;
        }

        var pairs = queryString.Split('&');
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                result[key] = value;
            }
            else if (parts.Length == 1)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                result[key] = "";
            }
        }
        return result;
    }
}

