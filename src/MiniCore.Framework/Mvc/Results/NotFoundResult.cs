using MiniCore.Framework.Mvc.Abstractions;

namespace MiniCore.Framework.Mvc.Results;

/// <summary>
/// An <see cref="IActionResult"/> that returns an empty <see cref="StatusCodes.Status404NotFound"/> response.
/// </summary>
public class NotFoundResult : IActionResult
{
    /// <inheritdoc />
    public Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    }
}

