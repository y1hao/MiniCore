using MiniCore.Framework.Mvc.Abstractions;

namespace MiniCore.Framework.Mvc.Results;

/// <summary>
/// An <see cref="IActionResult"/> that redirects to the specified URL.
/// </summary>
public class RedirectResult : IActionResult
{
    private readonly string _url;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectResult"/> class.
    /// </summary>
    /// <param name="url">The URL to redirect to.</param>
    public RedirectResult(string url)
    {
        _url = url ?? throw new ArgumentNullException(nameof(url));
    }

    /// <inheritdoc />
    public Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.StatusCode = 302; // Found
        context.HttpContext.Response.Headers["Location"] = _url;
        return Task.CompletedTask;
    }
}

