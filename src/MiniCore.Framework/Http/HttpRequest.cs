using MiniCore.Framework.Http.Abstractions;

namespace MiniCore.Framework.Http;

/// <summary>
/// Default implementation of <see cref="IHttpRequest"/>.
/// </summary>
public class HttpRequest : IHttpRequest
{
    private readonly HttpContext _context;
    private HeaderDictionary _headers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequest"/> class.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public HttpRequest(HttpContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Body = Stream.Null;
        Scheme = "http";
        PathBase = string.Empty;
    }

    /// <inheritdoc />
    public string Method { get; set; } = "GET";

    /// <inheritdoc />
    public string Path { get; set; } = "/";

    /// <inheritdoc />
    public string QueryString { get; set; } = string.Empty;

    /// <inheritdoc />
    public IHeaderDictionary Headers => _headers;

    /// <inheritdoc />
    public Stream Body { get; set; }

    /// <inheritdoc />
    public long? ContentLength
    {
        get => Headers.ContentLength;
        set => Headers.ContentLength = value;
    }

    /// <inheritdoc />
    public string? ContentType
    {
        get => Headers.ContentType;
        set => Headers.ContentType = value;
    }

    /// <inheritdoc />
    public string PathBase { get; set; }

    /// <inheritdoc />
    public string Scheme { get; set; }

    /// <inheritdoc />
    public HostString Host { get; set; }
}

