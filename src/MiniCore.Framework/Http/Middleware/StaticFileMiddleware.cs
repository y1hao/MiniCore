using System.IO.Compression;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Hosting;

namespace MiniCore.Framework.Http.Middleware;

/// <summary>
/// Middleware that enables static file serving for the current request path.
/// </summary>
public class StaticFileMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _rootPath;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticFileMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="environment">The web host environment.</param>
    /// <param name="rootPath">The root path for static files (defaults to wwwroot).</param>
    public StaticFileMiddleware(RequestDelegate next, IWebHostEnvironment environment, string? rootPath = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _rootPath = rootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(IHttpContext context)
    {
        var path = context.Request.Path ?? "/";

        // Only handle GET and HEAD requests
        if (context.Request.Method != "GET" && context.Request.Method != "HEAD")
        {
            await _next(context);
            return;
        }

        // Remove leading slash and resolve path
        var filePath = path.TrimStart('/');
        if (string.IsNullOrEmpty(filePath))
        {
            await _next(context);
            return;
        }

        // Security: prevent directory traversal
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, filePath));
        if (!fullPath.StartsWith(Path.GetFullPath(_rootPath), StringComparison.Ordinal))
        {
            await _next(context);
            return;
        }

        // Check if file exists
        if (!File.Exists(fullPath))
        {
            await _next(context);
            return;
        }

        // Set content type based on file extension
        var contentType = GetContentType(fullPath);
        context.Response.ContentType = contentType;

        // Set content length
        var fileInfo = new FileInfo(fullPath);
        context.Response.ContentLength = fileInfo.Length;

        // Set cache headers
        context.Response.Headers["Cache-Control"] = "public, max-age=3600";

        // Write file to response
        using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            await fileStream.CopyToAsync(context.Response.Body);
        }

        context.Response.StatusCode = 200;
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".txt" => "text/plain",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".eot" => "application/vnd.ms-fontobject",
            _ => "application/octet-stream"
        };
    }
}

