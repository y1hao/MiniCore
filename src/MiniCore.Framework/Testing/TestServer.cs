using System.Net;
using System.Net.Http.Headers;
using System.Text;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Http;
using MiniCore.Framework.Http.Abstractions;
using IServiceProvider = MiniCore.Framework.DependencyInjection.IServiceProvider;

namespace MiniCore.Framework.Testing;

/// <summary>
/// An in-memory HTTP server for testing that processes requests through the middleware pipeline.
/// </summary>
public class TestServer : IDisposable
{
    private readonly RequestDelegate _requestDelegate;
    private readonly IServiceProvider _serviceProvider;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestServer"/> class.
    /// </summary>
    /// <param name="requestDelegate">The request delegate pipeline.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public TestServer(RequestDelegate requestDelegate, IServiceProvider serviceProvider)
    {
        _requestDelegate = requestDelegate ?? throw new ArgumentNullException(nameof(requestDelegate));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets the base address for the test server.
    /// </summary>
    public Uri BaseAddress { get; } = new Uri("http://localhost/");

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    public IServiceProvider Services => _serviceProvider;

    /// <summary>
    /// Sends an HTTP request through the middleware pipeline and returns the response.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <returns>The HTTP response message.</returns>
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TestServer));
        }

        // Create HTTP context
        var context = new HttpContext
        {
            RequestServices = _serviceProvider
        };

        // Set up request
        var httpRequest = context.Request;
        httpRequest.Method = request.Method.Method;
        httpRequest.Scheme = request.RequestUri?.Scheme ?? "http";
        
        // Build host string (host:port format)
        var host = request.RequestUri?.Host ?? "localhost";
        var port = request.RequestUri?.Port;
        var hostString = port.HasValue && port.Value != 80 && port.Value != 443
            ? $"{host}:{port.Value}"
            : host;
        httpRequest.Host = new HostString(hostString);
        
        httpRequest.Path = request.RequestUri?.AbsolutePath ?? "/";
        httpRequest.QueryString = request.RequestUri?.Query ?? string.Empty;

        // Copy headers
        foreach (var header in request.Headers)
        {
            httpRequest.Headers[header.Key] = string.Join(", ", header.Value);
        }

        // Copy content headers and body
        if (request.Content != null)
        {
            if (request.Content.Headers.ContentType != null)
            {
                httpRequest.ContentType = request.Content.Headers.ContentType.ToString();
            }

            if (request.Content.Headers.ContentLength.HasValue)
            {
                httpRequest.ContentLength = request.Content.Headers.ContentLength.Value;
            }

            // Copy content body into a seekable memory stream so model binding can read it
            var sourceStream = await request.Content.ReadAsStreamAsync();
            var memoryStream = new MemoryStream();
            await sourceStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            httpRequest.Body = memoryStream;
        }

        // Execute the request delegate
        await _requestDelegate(context);

        // Build response message
        var response = new HttpResponseMessage((HttpStatusCode)context.Response.StatusCode);

        // Copy headers
        foreach (var header in context.Response.Headers)
        {
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                if (MediaTypeHeaderValue.TryParse(header.Value.ToString(), out var mediaType))
                {
                    response.Content = new StreamContent(context.Response.Body);
                    response.Content.Headers.ContentType = mediaType;
                }
            }
            else if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                // Content-Length will be set automatically by StreamContent
            }
            else if (!header.Key.Equals("Location", StringComparison.OrdinalIgnoreCase))
            {
                // Location header needs special handling for redirects
                try
                {
                    response.Headers.TryAddWithoutValidation(header.Key, header.Value.ToString());
                }
                catch
                {
                    // Ignore headers that can't be added
                }
            }
        }

        // Handle Location header for redirects
        if (context.Response.Headers.ContainsKey("Location"))
        {
            var location = context.Response.Headers["Location"].ToString();
            if (!string.IsNullOrEmpty(location))
            {
                response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
            }
        }

        // Set response content
        if (context.Response.Body != null && context.Response.Body.Length > 0)
        {
            context.Response.Body.Position = 0;
            if (response.Content == null)
            {
                response.Content = new StreamContent(context.Response.Body);
            }
            else
            {
                // Replace content if Content-Type was already set
                context.Response.Body.Position = 0;
                response.Content = new StreamContent(context.Response.Body);
                if (context.Response.ContentType != null)
                {
                    if (MediaTypeHeaderValue.TryParse(context.Response.ContentType, out var mediaType))
                    {
                        response.Content.Headers.ContentType = mediaType;
                    }
                }
            }
        }

        return response;
    }

    /// <summary>
    /// Creates an HTTP client configured to send requests to this test server.
    /// </summary>
    /// <param name="options">Optional client options.</param>
    /// <returns>An HTTP client.</returns>
    public HttpClient CreateClient(WebApplicationFactoryClientOptions? options = null)
    {
        options ??= new WebApplicationFactoryClientOptions();

        var handler = new TestServerHandler(this, options);
        var client = new HttpClient(handler)
        {
            BaseAddress = BaseAddress
        };

        if (options.Timeout.HasValue)
        {
            client.Timeout = options.Timeout.Value;
        }

        return client;
    }

    /// <summary>
    /// Disposes the test server.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    private class TestServerHandler : HttpMessageHandler
    {
        private readonly TestServer _server;
        private readonly WebApplicationFactoryClientOptions _options;

        public TestServerHandler(TestServer server, WebApplicationFactoryClientOptions options)
        {
            _server = server;
            _options = options;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await _server.SendAsync(request);

            // Handle redirects if AllowAutoRedirect is true
            if (_options.AllowAutoRedirect && IsRedirect(response.StatusCode))
            {
                var redirectCount = 0;
                var maxRedirects = _options.MaxAutomaticRedirections ?? 7;

                while (IsRedirect(response.StatusCode) && redirectCount < maxRedirects)
                {
                    redirectCount++;

                    var location = response.Headers.Location;
                    if (location == null)
                    {
                        break;
                    }

                    // Build absolute URI if location is relative
                    var redirectUri = location.IsAbsoluteUri
                        ? location
                        : new Uri(_server.BaseAddress, location);

                    // Create new request for redirect
                    var redirectRequest = new HttpRequestMessage(request.Method, redirectUri);
                    
                    // Copy headers (except Host and Authorization which shouldn't be copied)
                    foreach (var header in request.Headers)
                    {
                        if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                            !header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                        {
                            redirectRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }

                    // For GET/HEAD requests, don't copy content
                    if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Head)
                    {
                        // No content
                    }
                    else
                    {
                        // For other methods, copy content if present
                        if (request.Content != null)
                        {
                            var contentStream = await request.Content.ReadAsStreamAsync();
                            contentStream.Position = 0;
                            redirectRequest.Content = new StreamContent(contentStream);
                            if (request.Content.Headers.ContentType != null)
                            {
                                redirectRequest.Content.Headers.ContentType = request.Content.Headers.ContentType;
                            }
                        }
                    }

                    response.Dispose();
                    response = await _server.SendAsync(redirectRequest);
                }
            }

            return response;
        }

        private static bool IsRedirect(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.MovedPermanently ||
                   statusCode == HttpStatusCode.Found ||
                   statusCode == HttpStatusCode.SeeOther ||
                   statusCode == HttpStatusCode.TemporaryRedirect ||
                   statusCode == HttpStatusCode.PermanentRedirect;
        }
    }
}

