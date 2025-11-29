using MiniCore.Framework.Mvc.Abstractions;

namespace MiniCore.Framework.Mvc.Views;

/// <summary>
/// Context for rendering a view.
/// </summary>
public class ViewContext
{
    /// <summary>
    /// Gets or sets the action context.
    /// </summary>
    public ActionContext ActionContext { get; set; } = null!;

    /// <summary>
    /// Gets or sets the view name.
    /// </summary>
    public string ViewName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model.
    /// </summary>
    public object? Model { get; set; }

    /// <summary>
    /// Gets or sets the view data dictionary.
    /// </summary>
    public Dictionary<string, object> ViewData { get; set; } = new();
}

