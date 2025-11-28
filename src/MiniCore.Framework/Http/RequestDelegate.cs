using MiniCore.Framework.Http.Abstractions;

namespace MiniCore.Framework.Http;

/// <summary>
/// A function that can process an HTTP request.
/// </summary>
/// <param name="context">The <see cref="IHttpContext"/> for the request.</param>
/// <returns>A task that represents the completion of request processing.</returns>
public delegate Task RequestDelegate(IHttpContext context);

