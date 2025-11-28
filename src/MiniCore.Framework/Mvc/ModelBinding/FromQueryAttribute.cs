namespace MiniCore.Framework.Mvc.ModelBinding;

/// <summary>
/// Specifies that a parameter or property should be bound using the request query string.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromQueryAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the query string parameter to bind.
    /// </summary>
    public string? Name { get; set; }
}

