namespace MiniCore.Framework.Mvc.ModelBinding;

/// <summary>
/// Specifies that a parameter or property should be bound using route data.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromRouteAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the route parameter to bind.
    /// </summary>
    public string? Name { get; set; }
}

