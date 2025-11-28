using System.Diagnostics;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Logging;
using MiniCore.Framework.Http;

namespace MiniCore.Framework.Http.Middleware;

/// <summary>
/// Middleware that logs HTTP requests and responses.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestLoggingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger.</param>
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(IHttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path ?? "/";
        var method = context.Request.Method;

        _logger.LogInformation("HTTP {Method} {Path} started", method, path);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms",
                method,
                path,
                statusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}

