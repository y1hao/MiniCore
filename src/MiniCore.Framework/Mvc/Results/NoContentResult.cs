using MiniCore.Framework.Mvc.Abstractions;

namespace MiniCore.Framework.Mvc.Results;

/// <summary>
/// An <see cref="IActionResult"/> that returns an empty <see cref="StatusCodes.Status204NoContent"/> response.
/// </summary>
public class NoContentResult : IActionResult
{
    /// <inheritdoc />
    public Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status204NoContent;
        return Task.CompletedTask;
    }
}

