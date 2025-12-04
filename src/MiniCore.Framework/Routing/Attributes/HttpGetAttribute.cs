namespace MiniCore.Framework.Routing.Attributes;

/// <summary>
/// Identifies an action that supports the HTTP GET method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class HttpGetAttribute : HttpMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpGetAttribute"/> class.
    /// </summary>
    public HttpGetAttribute() : base("GET")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpGetAttribute"/> class with the specified route template.
    /// </summary>
    /// <param name="template">The route template.</param>
    public HttpGetAttribute(string template) : base("GET", template)
    {
    }
}

