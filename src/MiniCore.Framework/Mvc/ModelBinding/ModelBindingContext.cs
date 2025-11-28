using MiniCore.Framework.Http.Abstractions;

namespace MiniCore.Framework.Mvc.ModelBinding;

/// <summary>
/// A context object for model binding.
/// </summary>
public class ModelBindingContext
{
    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model type.
    /// </summary>
    public Type ModelType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the HTTP context.
    /// </summary>
    public IHttpContext HttpContext { get; set; } = null!;

    /// <summary>
    /// Gets or sets the route data.
    /// </summary>
    public Routing.Abstractions.RouteData? RouteData { get; set; }

    /// <summary>
    /// Gets or sets the model value.
    /// </summary>
    public object? Model { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether model binding was successful.
    /// </summary>
    public bool IsModelSet { get; set; }
}

