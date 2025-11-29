namespace MiniCore.Framework.Mvc.Views;

/// <summary>
/// Defines methods to locate and render views.
/// </summary>
public interface IViewEngine
{
    /// <summary>
    /// Finds a view by name and returns the view path if found.
    /// </summary>
    /// <param name="viewName">The name of the view.</param>
    /// <param name="controllerName">The name of the controller (optional).</param>
    /// <returns>The path to the view file if found, otherwise null.</returns>
    Task<string?> FindViewAsync(string viewName, string? controllerName = null);

    /// <summary>
    /// Renders a view template with the given model and view data.
    /// </summary>
    /// <param name="viewPath">The path to the view file.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <param name="viewData">Additional view data dictionary.</param>
    /// <returns>The rendered HTML string.</returns>
    Task<string> RenderViewAsync(string viewPath, object? model, Dictionary<string, object>? viewData = null);
}

