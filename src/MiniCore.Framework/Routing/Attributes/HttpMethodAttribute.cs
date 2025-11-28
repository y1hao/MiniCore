namespace MiniCore.Framework.Routing.Attributes;

/// <summary>
/// Base class for HTTP method attributes (HttpGet, HttpPost, etc.).
/// </summary>
public abstract class HttpMethodAttribute : Attribute
{
    /// <summary>
    /// Gets the HTTP method name.
    /// </summary>
    public string HttpMethod { get; }

    /// <summary>
    /// Gets the route template, if specified.
    /// </summary>
    public string? Template { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpMethodAttribute"/> class.
    /// </summary>
    /// <param name="httpMethod">The HTTP method name.</param>
    protected HttpMethodAttribute(string httpMethod)
    {
        HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpMethodAttribute"/> class with the specified route template.
    /// </summary>
    /// <param name="httpMethod">The HTTP method name.</param>
    /// <param name="template">The route template.</param>
    protected HttpMethodAttribute(string httpMethod, string template)
    {
        HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
        Template = template;
    }
}

