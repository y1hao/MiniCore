using System.Reflection;
using System.Text;
using System.Text.Json;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.ModelBinding;
using IServiceProvider = MiniCore.Framework.DependencyInjection.IServiceProvider;

namespace MiniCore.Framework.Mvc;

/// <summary>
/// Invokes controller actions.
/// </summary>
public class ControllerActionInvoker : IActionInvoker
{
    private readonly Type _controllerType;
    private readonly MethodInfo _actionMethod;
    private readonly IServiceProvider _serviceProvider;
    private readonly IModelBinder _modelBinder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControllerActionInvoker"/> class.
    /// </summary>
    /// <param name="controllerType">The controller type.</param>
    /// <param name="actionMethod">The action method.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="modelBinder">The model binder.</param>
    public ControllerActionInvoker(
        Type controllerType,
        MethodInfo actionMethod,
        IServiceProvider serviceProvider,
        IModelBinder? modelBinder = null)
    {
        _controllerType = controllerType ?? throw new ArgumentNullException(nameof(controllerType));
        _actionMethod = actionMethod ?? throw new ArgumentNullException(nameof(actionMethod));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _modelBinder = modelBinder ?? new DefaultModelBinder();
    }

    /// <inheritdoc />
    public async Task InvokeAsync(ActionContext context)
    {
        // Create controller instance
        var controller = CreateControllerInstance();
        if (controller == null)
        {
            context.HttpContext.Response.StatusCode = 500;
            await WriteText(context.HttpContext.Response.Body, "Failed to create controller instance");
            return;
        }

        // Set HttpContext on controller
        if (controller is Controllers.ControllerBase controllerBase)
        {
            controllerBase.HttpContext = context.HttpContext;
        }
        else if (controller is Abstractions.IController iController)
        {
            iController.HttpContext = context.HttpContext;
        }

        // Bind action parameters
        var parameters = _actionMethod.GetParameters();
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            args[i] = await BindParameterAsync(parameter, context);
        }

        // Invoke action method
        object? result;
        try
        {
            result = _actionMethod.Invoke(controller, args);
        }
        catch (Exception ex)
        {
            context.HttpContext.Response.StatusCode = 500;
            await WriteText(context.HttpContext.Response.Body, $"Error invoking action: {ex.Message}");
            return;
        }

        // Handle async methods
        if (result is Task task)
        {
            await task;

            // Get result from Task<T>
            if (_actionMethod.ReturnType.IsGenericType &&
                _actionMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultProperty = task.GetType().GetProperty("Result");
                result = resultProperty?.GetValue(task);
            }
            else
            {
                result = null;
            }
        }

        // Execute result
        await ExecuteResultAsync(result, context);
    }

    private object? CreateControllerInstance()
    {
        // Try to get from DI first
        var instance = _serviceProvider.GetService(_controllerType);
        if (instance != null)
        {
            return instance;
        }

        // Try constructor injection
        var constructors = _controllerType.GetConstructors();
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

            return Activator.CreateInstance(_controllerType, args);
        }

        // Try parameterless constructor
        return Activator.CreateInstance(_controllerType);
    }

    private async Task<object?> BindParameterAsync(ParameterInfo parameter, ActionContext context)
    {
        var bindingContext = new ModelBindingContext
        {
            ModelName = parameter.Name ?? string.Empty,
            ModelType = parameter.ParameterType,
            HttpContext = context.HttpContext,
            RouteData = context.RouteData
        };

        // Check for binding attributes
        var fromBodyAttr = parameter.GetCustomAttribute<FromBodyAttribute>();
        var fromQueryAttr = parameter.GetCustomAttribute<FromQueryAttribute>();
        var fromRouteAttr = parameter.GetCustomAttribute<FromRouteAttribute>();

        if (fromBodyAttr != null)
        {
            return BindFromBody(context.HttpContext.Request, parameter.ParameterType);
        }

        if (fromQueryAttr != null)
        {
            var name = fromQueryAttr.Name ?? parameter.Name ?? string.Empty;
            bindingContext.ModelName = name;
            return BindFromQuery(context.HttpContext.Request, name, parameter.ParameterType);
        }

        if (fromRouteAttr != null)
        {
            var name = fromRouteAttr.Name ?? parameter.Name ?? string.Empty;
            if (context.RouteData != null && context.RouteData.Values.TryGetValue(name, out var routeValue))
            {
                return ConvertValue(routeValue, parameter.ParameterType);
            }
            return GetDefaultValue(parameter.ParameterType);
        }

        // Default binding: try route, then query, then body
        await _modelBinder.BindModelAsync(bindingContext);
        return bindingContext.Model;
    }

    private static object? BindFromBody(IHttpRequest request, Type parameterType)
    {
        if (request.Body == null)
        {
            return GetDefaultValue(parameterType);
        }

        try
        {
            // Ensure the stream is readable
            if (!request.Body.CanRead)
            {
                return GetDefaultValue(parameterType);
            }

            // Save and reset position if seekable
            long? originalPosition = null;
            if (request.Body.CanSeek)
            {
                originalPosition = request.Body.Position;
                request.Body.Position = 0;

                if (request.Body.Length == 0)
                {
                    if (originalPosition.HasValue)
                    {
                        request.Body.Position = originalPosition.Value;
                    }
                    return GetDefaultValue(parameterType);
                }
            }

            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var json = reader.ReadToEnd();

            // Restore original position if we changed it
            if (originalPosition.HasValue && request.Body.CanSeek)
            {
                request.Body.Position = originalPosition.Value;
            }

            if (string.IsNullOrEmpty(json))
            {
                return GetDefaultValue(parameterType);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize(json, parameterType, options);
        }
        catch
        {
            return GetDefaultValue(parameterType);
        }
    }

    private static object? BindFromQuery(IHttpRequest request, string parameterName, Type parameterType)
    {
        if (string.IsNullOrEmpty(request.QueryString))
        {
            return GetDefaultValue(parameterType);
        }

        var queryString = request.QueryString.TrimStart('?');
        var queryParams = ParseQueryString(queryString);
        
        if (queryParams.TryGetValue(parameterName, out var value) && !string.IsNullOrEmpty(value))
        {
            return ConvertValue(value, parameterType);
        }

        return GetDefaultValue(parameterType);
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
        }

        return result;
    }

    private static object? ConvertValue(string? value, Type targetType)
    {
        if (value == null)
        {
            return GetDefaultValue(targetType);
        }

        if (targetType == typeof(string))
        {
            return value;
        }

        if (targetType.IsPrimitive || targetType == typeof(decimal) || targetType == typeof(DateTime) || targetType == typeof(DateTimeOffset))
        {
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return GetDefaultValue(targetType);
            }
        }

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                return ConvertValue(value, underlyingType);
            }
        }

        return GetDefaultValue(targetType);
    }

    private static object? GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }

    private static async Task ExecuteResultAsync(object? result, ActionContext context)
    {
        if (result is IActionResult actionResult)
        {
            await actionResult.ExecuteResultAsync(context);
            return;
        }

        // Handle non-ActionResult return types (serialize as JSON)
        if (result != null)
        {
            context.HttpContext.Response.StatusCode = 200;
            context.HttpContext.Response.ContentType = "application/json";
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(result, options);
            var bytes = Encoding.UTF8.GetBytes(json);
            await context.HttpContext.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }
        else
        {
            context.HttpContext.Response.StatusCode = 204; // No Content
        }
    }

    private static async Task WriteText(Stream stream, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await stream.WriteAsync(bytes, 0, bytes.Length);
    }
}

