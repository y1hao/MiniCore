using System.Text;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Views;

namespace MiniCore.Framework.Mvc.Results;

/// <summary>
/// An <see cref="IActionResult"/> that renders a view.
/// </summary>
public class ViewResult : IActionResult
{
    private readonly string? _viewName;
    private readonly string? _controllerName;
    private readonly object? _model;
    private readonly Dictionary<string, object> _viewData;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewResult"/> class.
    /// </summary>
    /// <param name="viewName">The name of the view to render. If null, uses the action name.</param>
    /// <param name="controllerName">The name of the controller (without "Controller" suffix). If null, attempts to infer from context.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <param name="viewData">Additional view data dictionary.</param>
    public ViewResult(string? viewName = null, string? controllerName = null, object? model = null, Dictionary<string, object>? viewData = null)
    {
        _viewName = viewName;
        _controllerName = controllerName;
        _model = model;
        _viewData = viewData ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets the model to be passed to the view.
    /// </summary>
    public object? Model => _model;

    /// <inheritdoc />
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var serviceProvider = context.HttpContext.RequestServices ?? 
            throw new InvalidOperationException("RequestServices is not available.");

        var viewEngine = serviceProvider.GetService(typeof(IViewEngine)) as IViewEngine
            ?? throw new InvalidOperationException("IViewEngine is not registered in the service container. Call builder.Services.AddSingleton<IViewEngine, ViewEngine>() in Program.cs.");

        var viewName = _viewName;
        var controllerName = _controllerName;

        // Try to get view name and controller name from route data if not provided
        if (string.IsNullOrEmpty(viewName) || string.IsNullOrEmpty(controllerName))
        {
            var routeData = context.RouteData;
            if (routeData != null && routeData.Values != null)
            {
                if (string.IsNullOrEmpty(viewName) && routeData.Values.TryGetValue("action", out var actionValue))
                {
                    viewName = actionValue;
                }
                if (string.IsNullOrEmpty(controllerName) && routeData.Values.TryGetValue("controller", out var controllerValue))
                {
                    controllerName = controllerValue;
                }
            }
        }

        if (string.IsNullOrEmpty(viewName))
        {
            throw new InvalidOperationException("View name could not be determined. Specify a view name when calling View().");
        }

        // Remove "Controller" suffix from controller name if present
        if (!string.IsNullOrEmpty(controllerName) && controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
        {
            controllerName = controllerName.Substring(0, controllerName.Length - 10);
        }

        // Find view
        var viewPath = await viewEngine.FindViewAsync(viewName, controllerName);
        if (viewPath == null)
        {
            var searchPaths = string.IsNullOrEmpty(controllerName) 
                ? $"Views/{viewName}.html" 
                : $"Views/{controllerName}/{viewName}.html";
            throw new FileNotFoundException($"View '{viewName}' not found. Searched: {searchPaths}");
        }

        // Render view
        var html = await viewEngine.RenderViewAsync(viewPath, _model, _viewData);

        // Write response
        context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        context.HttpContext.Response.ContentType = "text/html; charset=utf-8";

        var bytes = Encoding.UTF8.GetBytes(html);
        await context.HttpContext.Response.Body.WriteAsync(bytes, 0, bytes.Length);
    }
}

