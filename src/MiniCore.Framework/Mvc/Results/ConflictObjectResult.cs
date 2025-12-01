using System.Text;
using System.Text.Json;
using MiniCore.Framework.Mvc.Abstractions;

namespace MiniCore.Framework.Mvc.Results;

/// <summary>
/// An <see cref="IActionResult"/> that returns an <see cref="StatusCodes.Status409Conflict"/> response with content.
/// </summary>
public class ConflictObjectResult : IActionResult
{
    private readonly object? _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictObjectResult"/> class.
    /// </summary>
    /// <param name="error">The error object to return.</param>
    public ConflictObjectResult(object? error)
    {
        _error = error;
    }

    /// <summary>
    /// Gets the error object to return.
    /// </summary>
    public object? Value => _error;

    /// <inheritdoc />
    public async Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        context.HttpContext.Response.ContentType = "application/json";

        if (_error != null)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(_error, options);
            var bytes = Encoding.UTF8.GetBytes(json);
            await context.HttpContext.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}

