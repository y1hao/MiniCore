using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Results;

namespace MiniCore.Framework.Mvc.Controllers;

/// <summary>
/// A base class for an MVC controller with view support.
/// </summary>
public abstract class Controller : ControllerBase
{
    private Dictionary<string, object>? _viewData;

    /// <summary>
    /// Gets the view data dictionary.
    /// </summary>
    public Dictionary<string, object> ViewData
    {
        get
        {
            _viewData ??= new Dictionary<string, object>();
            return _viewData;
        }
    }

    /// <summary>
    /// Creates a <see cref="ViewResult"/> object that renders a view.
    /// </summary>
    /// <returns>The created <see cref="ViewResult"/> for the response.</returns>
    protected ViewResult View()
    {
        return new ViewResult(viewData: ViewData);
    }

    /// <summary>
    /// Creates a <see cref="ViewResult"/> object that renders a view with the specified model.
    /// </summary>
    /// <param name="model">The model to pass to the view.</param>
    /// <returns>The created <see cref="ViewResult"/> for the response.</returns>
    protected ViewResult View(object? model)
    {
        return new ViewResult(model: model, viewData: ViewData);
    }

    /// <summary>
    /// Creates a <see cref="ViewResult"/> object that renders the specified view.
    /// </summary>
    /// <param name="viewName">The name of the view to render.</param>
    /// <returns>The created <see cref="ViewResult"/> for the response.</returns>
    protected ViewResult View(string viewName)
    {
        return new ViewResult(viewName: viewName, viewData: ViewData);
    }

    /// <summary>
    /// Creates a <see cref="ViewResult"/> object that renders the specified view with the specified model.
    /// </summary>
    /// <param name="viewName">The name of the view to render.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <returns>The created <see cref="ViewResult"/> for the response.</returns>
    protected ViewResult View(string viewName, object? model)
    {
        return new ViewResult(viewName: viewName, model: model, viewData: ViewData);
    }
}

/// <summary>
/// A base class for an MVC controller without view support.
/// </summary>
public abstract class ControllerBase : IController
{
    private IHttpContext? _httpContext;

    /// <inheritdoc />
    public IHttpContext HttpContext
    {
        get => _httpContext ?? throw new InvalidOperationException("HttpContext has not been set.");
        set => _httpContext = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets the HTTP request.
    /// </summary>
    protected IHttpRequest Request => HttpContext.Request;

    /// <summary>
    /// Gets the HTTP response.
    /// </summary>
    protected IHttpResponse Response => HttpContext.Response;

    /// <summary>
    /// Creates an <see cref="OkResult"/> object that produces an empty <see cref="StatusCodes.Status200OK"/> response.
    /// </summary>
    /// <returns>The created <see cref="OkResult"/> for the response.</returns>
    protected OkResult Ok() => new();

    /// <summary>
    /// Creates an <see cref="OkObjectResult"/> object that produces an <see cref="StatusCodes.Status200OK"/> response.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The content value to format in the entity body.</param>
    /// <returns>The created <see cref="OkObjectResult"/> for the response.</returns>
    protected OkObjectResult Ok<T>(T? value) => new(value);

    /// <summary>
    /// Creates a <see cref="BadRequestResult"/> that produces a <see cref="StatusCodes.Status400BadRequest"/> response.
    /// </summary>
    /// <returns>The created <see cref="BadRequestResult"/> for the response.</returns>
    protected BadRequestResult BadRequest() => new();

    /// <summary>
    /// Creates a <see cref="BadRequestObjectResult"/> that produces a <see cref="StatusCodes.Status400BadRequest"/> response.
    /// </summary>
    /// <param name="error">An error object to be returned.</param>
    /// <returns>The created <see cref="BadRequestObjectResult"/> for the response.</returns>
    protected BadRequestObjectResult BadRequest(object? error) => new(error);

    /// <summary>
    /// Creates a <see cref="NotFoundResult"/> that produces a <see cref="StatusCodes.Status404NotFound"/> response.
    /// </summary>
    /// <returns>The created <see cref="NotFoundResult"/> for the response.</returns>
    protected NotFoundResult NotFound() => new();

    /// <summary>
    /// Creates a <see cref="NotFoundObjectResult"/> that produces a <see cref="StatusCodes.Status404NotFound"/> response.
    /// </summary>
    /// <param name="value">The content value to format in the entity body.</param>
    /// <returns>The created <see cref="NotFoundObjectResult"/> for the response.</returns>
    protected NotFoundObjectResult NotFound(object? value) => new(value);

    /// <summary>
    /// Creates a <see cref="NoContentResult"/> object that produces an empty <see cref="StatusCodes.Status204NoContent"/> response.
    /// </summary>
    /// <returns>The created <see cref="NoContentResult"/> object for the response.</returns>
    protected NoContentResult NoContent() => new();

    /// <summary>
    /// Creates a <see cref="CreatedResult"/> object that produces a <see cref="StatusCodes.Status201Created"/> response.
    /// </summary>
    /// <param name="uri">The URI at which the content has been created.</param>
    /// <param name="value">The content value to format in the entity body.</param>
    /// <returns>The created <see cref="CreatedResult"/> for the response.</returns>
    protected CreatedResult Created(string uri, object? value) => new(uri, value);

    /// <summary>
    /// Creates a <see cref="RedirectResult"/> object that redirects to the specified URL.
    /// </summary>
    /// <param name="url">The URL to redirect to.</param>
    /// <returns>The created <see cref="RedirectResult"/> for the response.</returns>
    protected RedirectResult Redirect(string url) => new(url);
}

