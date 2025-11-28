using System.Text;
using System.Text.Json;
using MiniCore.Framework.Mvc.Abstractions;

namespace MiniCore.Framework.Mvc.Results;

/// <summary>
/// An <see cref="IActionResult"/> that returns an <see cref="StatusCodes.Status404NotFound"/> response with content.
/// </summary>
public class NotFoundObjectResult : IActionResult
{
    private readonly object? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundObjectResult"/> class.
    /// </summary>
    /// <param name="value">The value to return.</param>
    public NotFoundObjectResult(object? value)
    {
        _value = value;
    }

    /// <inheritdoc />
    public async Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        context.HttpContext.Response.ContentType = "application/json";

        if (_value != null)
        {
            var json = JsonSerializer.Serialize(_value);
            var bytes = Encoding.UTF8.GetBytes(json);
            await context.HttpContext.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}

