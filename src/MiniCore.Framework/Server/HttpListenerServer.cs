using System.Net;
using System.Text;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using MiniCore.Framework.Logging;
using MiniCore.Framework.Server.Abstractions;
using IServiceProvider = MiniCore.Framework.DependencyInjection.IServiceProvider;

namespace MiniCore.Framework.Server;

/// <summary>
/// An HTTP server implementation using HttpListener.
/// </summary>
public class HttpListenerServer : IServer
{
    private readonly HttpListener _listener;
    private readonly RequestDelegate _requestDelegate;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HttpListenerServer>? _logger;
    private readonly string[] _urls;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listeningTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpListenerServer"/> class.
    /// </summary>
    /// <param name="urls">The URLs to listen on.</param>
    /// <param name="requestDelegate">The request delegate pipeline.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger.</param>
    public HttpListenerServer(
        string[] urls,
        RequestDelegate requestDelegate,
        IServiceProvider serviceProvider,
        ILogger<HttpListenerServer>? logger = null)
    {
        _urls = urls ?? throw new ArgumentNullException(nameof(urls));
        _requestDelegate = requestDelegate ?? throw new ArgumentNullException(nameof(requestDelegate));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger;
        _listener = new HttpListener();
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_listeningTask != null)
        {
            throw new InvalidOperationException("Server is already started.");
        }

        // Add prefixes to listener
        // HttpListener requires all prefixes to end with '/'
        foreach (var url in _urls)
        {
            var prefix = url;
            if (!prefix.EndsWith("/"))
            {
                prefix += "/";
            }
            _listener.Prefixes.Add(prefix);
        }

        // Start the listener
        _listener.Start();
        _logger?.LogInformation("HTTP server started listening on: {Urls}", string.Join(", ", _urls));

        // Create cancellation token source
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Start listening for requests
        _listeningTask = Task.Run(() => ListenAsync(_cancellationTokenSource.Token), cancellationToken);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_listeningTask == null)
        {
            return;
        }

        _logger?.LogInformation("Stopping HTTP server...");

        // Stop accepting new requests
        _listener.Stop();

        // Cancel the listening task
        _cancellationTokenSource?.Cancel();

        // Wait for the listening task to complete
        try
        {
            await _listeningTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }

        _listeningTask = null;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _logger?.LogInformation("HTTP server stopped.");
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Get context asynchronously
                var context = await _listener.GetContextAsync().ConfigureAwait(false);
                
                // Process request on thread pool (don't await to allow concurrent requests)
                _ = Task.Run(() => ProcessRequestAsync(context, cancellationToken), cancellationToken);
            }
            catch (HttpListenerException ex) when (cancellationToken.IsCancellationRequested)
            {
                // Expected when stopping the listener
                _logger?.LogDebug("HttpListener stopped: {Message}", ex.Message);
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error accepting HTTP request");
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext listenerContext, CancellationToken cancellationToken)
    {
        // Create a scope for this request (for scoped services)
        var scope = _serviceProvider.GetService<IServiceScopeFactory>()?.CreateScope();
        if (scope == null)
        {
            // Fallback to root service provider if no scope factory
            await ProcessRequestWithProviderAsync(listenerContext, _serviceProvider, cancellationToken);
            return;
        }

        try
        {
            await ProcessRequestWithProviderAsync(listenerContext, scope.ServiceProvider, cancellationToken);
        }
        finally
        {
            scope.Dispose();
        }
    }

    private async Task ProcessRequestWithProviderAsync(
        HttpListenerContext listenerContext,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var request = listenerContext.Request;
        var response = listenerContext.Response;

        // Create our HttpContext
        var httpContext = new HttpContext
        {
            RequestServices = serviceProvider
        };

        try
        {
            // Translate HttpListenerRequest to HttpRequest
            TranslateRequest(request, httpContext.Request);

            // Process through middleware pipeline
            await _requestDelegate(httpContext).ConfigureAwait(false);

            // Translate HttpResponse to HttpListenerResponse
            await TranslateResponseAsync(httpContext.Response, response, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing HTTP request {Method} {Path}", request.HttpMethod, request.Url?.PathAndQuery);
            
            // Send 500 error response
            if (!response.OutputStream.CanWrite)
            {
                return;
            }

            try
            {
                response.StatusCode = 500;
                response.ContentType = "text/plain";
                var errorBytes = Encoding.UTF8.GetBytes("Internal Server Error");
                await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Ignore errors when sending error response
            }
        }
        finally
        {
            try
            {
                response.Close();
            }
            catch
            {
                // Ignore errors when closing response
            }
        }
    }

    private static void TranslateRequest(System.Net.HttpListenerRequest listenerRequest, IHttpRequest httpRequest)
    {
        httpRequest.Method = listenerRequest.HttpMethod;
        httpRequest.Path = listenerRequest.Url?.AbsolutePath ?? "/";
        httpRequest.QueryString = listenerRequest.Url?.Query ?? string.Empty;
        httpRequest.Scheme = listenerRequest.Url?.Scheme ?? "http";
        httpRequest.Host = new HostString(listenerRequest.Url?.Host ?? "localhost");
        httpRequest.PathBase = string.Empty;

        // Copy headers
        foreach (string key in listenerRequest.Headers.AllKeys)
        {
            if (key == null) continue;
            var values = listenerRequest.Headers.GetValues(key);
            if (values != null)
            {
                httpRequest.Headers[key] = new StringValues(values);
            }
        }

        // Copy body
        if (listenerRequest.HasEntityBody)
        {
            httpRequest.Body = listenerRequest.InputStream;
        }
        else
        {
            httpRequest.Body = Stream.Null;
        }
    }

    private static async Task TranslateResponseAsync(
        IHttpResponse httpResponse,
        System.Net.HttpListenerResponse listenerResponse,
        CancellationToken cancellationToken)
    {
        // Set status code
        listenerResponse.StatusCode = httpResponse.StatusCode;

        // Copy headers (skip Content-Length and Transfer-Encoding as they're handled automatically)
        foreach (var header in httpResponse.Headers)
        {
            var key = header.Key;
            if (string.Equals(key, "Content-Length", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = header.Value.ToString();
            if (!string.IsNullOrEmpty(value))
            {
                listenerResponse.Headers[key] = value;
            }
        }

        // Set content type if specified
        if (!string.IsNullOrEmpty(httpResponse.ContentType))
        {
            listenerResponse.ContentType = httpResponse.ContentType;
        }

        // Set content length if specified
        if (httpResponse.ContentLength.HasValue)
        {
            listenerResponse.ContentLength64 = httpResponse.ContentLength.Value;
        }

        // Copy body
        if (httpResponse.Body != null && httpResponse.Body.Length > 0)
        {
            httpResponse.Body.Position = 0;
            await httpResponse.Body.CopyToAsync(listenerResponse.OutputStream, cancellationToken).ConfigureAwait(false);
        }
    }
}

