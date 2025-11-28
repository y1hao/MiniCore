namespace MiniCore.Framework.Routing.Attributes;

/// <summary>
/// Identifies an action that supports the HTTP DELETE method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class HttpDeleteAttribute : HttpMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpDeleteAttribute"/> class.
    /// </summary>
    public HttpDeleteAttribute() : base("DELETE")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpDeleteAttribute"/> class with the specified route template.
    /// </summary>
    /// <param name="template">The route template.</param>
    public HttpDeleteAttribute(string template) : base("DELETE", template)
    {
    }
}

