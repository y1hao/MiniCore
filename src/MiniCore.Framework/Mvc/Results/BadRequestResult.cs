using MiniCore.Framework.Mvc.Abstractions;

namespace MiniCore.Framework.Mvc.Results;

/// <summary>
/// An <see cref="IActionResult"/> that returns an empty <see cref="StatusCodes.Status400BadRequest"/> response.
/// </summary>
public class BadRequestResult : IActionResult
{
    /// <inheritdoc />
    public Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        return Task.CompletedTask;
    }
}

