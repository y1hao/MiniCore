using System.Text;
using MiniCore.Framework.Hosting;

namespace MiniCore.Framework.Mvc.Views;

/// <summary>
/// Default view engine that locates and renders HTML templates.
/// </summary>
public class ViewEngine : IViewEngine
{
    private readonly IWebHostEnvironment _environment;
    private readonly TemplateEngine _templateEngine;
    private readonly Dictionary<string, string> _templateCache;

    public ViewEngine(IWebHostEnvironment environment)
    {
        _environment = environment;
        _templateEngine = new TemplateEngine();
        _templateCache = new Dictionary<string, string>();
    }

    /// <inheritdoc />
    public Task<string?> FindViewAsync(string viewName, string? controllerName = null)
    {
        var viewsPath = Path.Combine(_environment.ContentRootPath, "Views");

        // Try controller-specific view first
        if (!string.IsNullOrEmpty(controllerName))
        {
            var controllerViewPath = Path.Combine(viewsPath, controllerName, $"{viewName}.html");
            if (File.Exists(controllerViewPath))
            {
                return Task.FromResult<string?>(controllerViewPath);
            }
        }

        // Try direct view path
        var directViewPath = Path.Combine(viewsPath, $"{viewName}.html");
        if (File.Exists(directViewPath))
        {
            return Task.FromResult<string?>(directViewPath);
        }

        // Try with full path if viewName already contains path
        if (File.Exists(viewName))
        {
            return Task.FromResult<string?>(viewName);
        }

        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public async Task<string> RenderViewAsync(string viewPath, object? model, Dictionary<string, object>? viewData = null)
    {
        if (string.IsNullOrEmpty(viewPath) || !File.Exists(viewPath))
        {
            throw new FileNotFoundException($"View not found: {viewPath}");
        }

        // Load template (with caching)
        string template;
        if (!_templateCache.TryGetValue(viewPath, out var cachedTemplate))
        {
            template = await File.ReadAllTextAsync(viewPath, Encoding.UTF8);
            
            // Cache template (simple cache, no invalidation for now)
            lock (_templateCache)
            {
                if (!_templateCache.ContainsKey(viewPath))
                {
                    _templateCache[viewPath] = template;
                }
            }
        }
        else
        {
            template = cachedTemplate;
        }

        // Render template
        var rendered = _templateEngine.Render(template, model, viewData);
        
        // Replace ~/ with / to resolve Razor-style paths (e.g., ~/css/admin.css -> /css/admin.css)
        rendered = rendered.Replace("~/", "/");
        
        return rendered;
    }
}

