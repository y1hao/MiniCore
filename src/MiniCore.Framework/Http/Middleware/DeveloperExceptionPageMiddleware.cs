using System.Text;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Hosting;

namespace MiniCore.Framework.Http.Middleware;

/// <summary>
/// Middleware that catches exceptions, logs them, and re-executes the request in an alternate pipeline.
/// </summary>
public class DeveloperExceptionPageMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeveloperExceptionPageMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="environment">The web host environment.</param>
    public DeveloperExceptionPageMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(IHttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (!_environment.IsDevelopment())
            {
                // In non-development environments, rethrow the exception
                throw;
            }

            // Generate developer exception page
            var html = GenerateExceptionPage(ex, context);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/html; charset=utf-8";

            var bytes = Encoding.UTF8.GetBytes(html);
            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }

    private static string GenerateExceptionPage(Exception ex, IHttpContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<title>An error occurred</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }");
        sb.AppendLine(".error-container { background-color: white; padding: 20px; border-radius: 4px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
        sb.AppendLine("h1 { color: #d32f2f; margin-top: 0; }");
        sb.AppendLine(".error-message { background-color: #ffebee; padding: 15px; border-left: 4px solid #d32f2f; margin: 15px 0; }");
        sb.AppendLine(".stack-trace { background-color: #f5f5f5; padding: 15px; font-family: 'Courier New', monospace; font-size: 12px; overflow-x: auto; white-space: pre-wrap; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class=\"error-container\">");
        sb.AppendLine("<h1>An unhandled exception occurred while processing your request.</h1>");
        sb.AppendLine($"<div class=\"error-message\"><strong>Exception:</strong> {HtmlEncode(ex.GetType().FullName)}</div>");
        sb.AppendLine($"<div class=\"error-message\"><strong>Message:</strong> {HtmlEncode(ex.Message)}</div>");
        sb.AppendLine($"<div class=\"error-message\"><strong>Path:</strong> {HtmlEncode(context.Request.Path)}</div>");
        sb.AppendLine("<h2>Stack Trace:</h2>");
        sb.AppendLine($"<div class=\"stack-trace\">{HtmlEncode(ex.StackTrace ?? "No stack trace available")}</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static string HtmlEncode(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}

