using System.Text;
using System.Text.Json;
using MiniCore.Framework.Mvc.Abstractions;

namespace MiniCore.Framework.Mvc.Results;

/// <summary>
/// An <see cref="IActionResult"/> that returns a <see cref="StatusCodes.Status201Created"/> response with a location header.
/// </summary>
public class CreatedResult : IActionResult
{
    private readonly string _uri;
    private readonly object? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedResult"/> class.
    /// </summary>
    /// <param name="uri">The URI at which the content has been created.</param>
    /// <param name="value">The content value to format in the entity body.</param>
    public CreatedResult(string uri, object? value)
    {
        _uri = uri ?? throw new ArgumentNullException(nameof(uri));
        _value = value;
    }

    /// <summary>
    /// Gets the URI at which the content has been created.
    /// </summary>
    public string Uri => _uri;

    /// <summary>
    /// Gets the content value to format in the entity body.
    /// </summary>
    public object? Value => _value;

    /// <inheritdoc />
    public async Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status201Created;
        context.HttpContext.Response.Headers["Location"] = _uri;
        context.HttpContext.Response.ContentType = "application/json";

        if (_value != null)
        {
            var json = JsonSerializer.Serialize(_value);
            var bytes = Encoding.UTF8.GetBytes(json);
            await context.HttpContext.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}

