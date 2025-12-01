using System.Text;
using System.Text.Json;
using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Routing.Abstractions;

namespace MiniCore.Framework.Mvc.ModelBinding;

/// <summary>
/// Default model binder implementation.
/// </summary>
public class DefaultModelBinder : IModelBinder
{
    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // Try route data first
        if (context.RouteData != null && context.RouteData.Values.TryGetValue(context.ModelName, out var routeValue))
        {
            context.Model = ConvertValue(routeValue, context.ModelType);
            context.IsModelSet = true;
            return Task.CompletedTask;
        }

        // Try query string
        var queryValue = GetQueryValue(context.HttpContext.Request, context.ModelName);
        if (queryValue != null)
        {
            context.Model = ConvertValue(queryValue, context.ModelType);
            context.IsModelSet = true;
            return Task.CompletedTask;
        }

        // Try request body for complex types
        if (context.ModelType.IsClass && context.ModelType != typeof(string))
        {
            var bodyModel = BindFromBody(context.HttpContext.Request, context.ModelType);
            if (bodyModel != null)
            {
                context.Model = bodyModel;
                context.IsModelSet = true;
                return Task.CompletedTask;
            }
        }

        // Use default value
        context.Model = GetDefaultValue(context.ModelType);
        context.IsModelSet = false;
        return Task.CompletedTask;
    }

    private static string? GetQueryValue(IHttpRequest request, string parameterName)
    {
        if (string.IsNullOrEmpty(request.QueryString))
        {
            return null;
        }

        var queryString = request.QueryString.TrimStart('?');
        var queryParams = ParseQueryString(queryString);
        return queryParams.TryGetValue(parameterName, out var value) ? value : null;
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

    private static object? BindFromBody(IHttpRequest request, Type modelType)
    {
        if (request.Body == null)
        {
            return null;
        }

        try
        {
            // Check if stream is readable
            if (!request.Body.CanRead)
            {
                return null;
            }

            // If the stream is seekable, rewind to the beginning so we can read the full body
            long? originalPosition = null;
            if (request.Body.CanSeek)
            {
                originalPosition = request.Body.Position;
                request.Body.Position = 0;

                // If there's no content, bail early
                if (request.Body.Length == 0)
                {
                    if (originalPosition.HasValue)
                    {
                        request.Body.Position = originalPosition.Value;
                    }
                    return null;
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
                return null;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize(json, modelType, options);
        }
        catch
        {
            return null;
        }
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
}

