namespace MiniCore.Framework.Mvc.Abstractions;

/// <summary>
/// Defines an interface for invoking an action method.
/// </summary>
public interface IActionInvoker
{
    /// <summary>
    /// Invokes the action asynchronously.
    /// </summary>
    /// <param name="context">The action context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task InvokeAsync(ActionContext context);
}

