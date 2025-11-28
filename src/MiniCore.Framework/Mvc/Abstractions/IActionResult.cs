namespace MiniCore.Framework.Mvc.Abstractions;

/// <summary>
/// Defines a contract that represents the result of an action method.
/// </summary>
public interface IActionResult
{
    /// <summary>
    /// Executes the result asynchronously.
    /// </summary>
    /// <param name="context">The action context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteResultAsync(ActionContext context);
}

