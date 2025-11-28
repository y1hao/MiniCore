using System.Collections;
using MiniCore.Framework.Http.Abstractions;

namespace MiniCore.Framework.Http;

/// <summary>
/// Default implementation of <see cref="IHttpResponse"/>.
/// </summary>
public class HttpResponse : IHttpResponse
{
    private readonly HttpContext _context;
    private readonly HeaderDictionary _headers = new();
    private readonly List<Func<object, Task>> _onStartingCallbacks = new();
    private readonly List<Func<object, Task>> _onCompletedCallbacks = new();
    private bool _hasStarted;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpResponse"/> class.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public HttpResponse(HttpContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Body = new MemoryStream();
        StatusCode = 200;
    }

    /// <inheritdoc />
    public IHeaderDictionary Headers => _headers;

    /// <inheritdoc />
    public Stream Body { get; set; }

    /// <inheritdoc />
    public int StatusCode { get; set; }

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
    public bool HasStarted => _hasStarted;

    /// <summary>
    /// Marks the response as started.
    /// </summary>
    public void Start()
    {
        if (!_hasStarted)
        {
            _hasStarted = true;
            foreach (var callback in _onStartingCallbacks)
            {
                callback(_context);
            }
        }
    }

    /// <summary>
    /// Marks the response as completed.
    /// </summary>
    public void Complete()
    {
        foreach (var callback in _onCompletedCallbacks)
        {
            callback(_context);
        }
    }

    /// <inheritdoc />
    public void OnStarting(Func<object, Task> callback, object state)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        if (_hasStarted)
        {
            throw new InvalidOperationException("Response has already started.");
        }

        _onStartingCallbacks.Add(callback);
    }

    /// <inheritdoc />
    public void OnCompleted(Func<object, Task> callback, object state)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        _onCompletedCallbacks.Add(callback);
    }
}

