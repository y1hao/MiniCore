namespace MiniCore.Framework.Routing.Attributes;

/// <summary>
/// Identifies an action that supports the HTTP POST method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class HttpPostAttribute : HttpMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpPostAttribute"/> class.
    /// </summary>
    public HttpPostAttribute() : base("POST")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpPostAttribute"/> class with the specified route template.
    /// </summary>
    /// <param name="template">The route template.</param>
    public HttpPostAttribute(string template) : base("POST", template)
    {
    }
}

