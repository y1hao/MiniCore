namespace MiniCore.Framework.Routing.Attributes;

/// <summary>
/// Identifies an action that supports the HTTP PUT method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class HttpPutAttribute : HttpMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpPutAttribute"/> class.
    /// </summary>
    public HttpPutAttribute() : base("PUT")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpPutAttribute"/> class with the specified route template.
    /// </summary>
    /// <param name="template">The route template.</param>
    public HttpPutAttribute(string template) : base("PUT", template)
    {
    }
}

