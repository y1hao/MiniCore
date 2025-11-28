namespace MiniCore.Framework.Routing.Attributes;

/// <summary>
/// Specifies a route template for a controller or action method.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RouteAttribute : Attribute
{
    /// <summary>
    /// Gets the route template.
    /// </summary>
    public string Template { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteAttribute"/> class with the specified route template.
    /// </summary>
    /// <param name="template">The route template.</param>
    public RouteAttribute(string template)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
    }
}

