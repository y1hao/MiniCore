namespace MiniCore.Framework.Routing.Attributes;

/// <summary>
/// Identifies an action that supports the HTTP PATCH method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class HttpPatchAttribute : HttpMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpPatchAttribute"/> class.
    /// </summary>
    public HttpPatchAttribute() : base("PATCH")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpPatchAttribute"/> class with the specified route template.
    /// </summary>
    /// <param name="template">The route template.</param>
    public HttpPatchAttribute(string template) : base("PATCH", template)
    {
    }
}

