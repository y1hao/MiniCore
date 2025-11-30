namespace MiniCore.Framework.Testing;

/// <summary>
/// Options for creating an <see cref="HttpClient"/> with <see cref="WebApplicationFactory{TEntryPoint}.CreateClient(WebApplicationFactoryClientOptions)"/>.
/// </summary>
public class WebApplicationFactoryClientOptions
{
    /// <summary>
    /// Gets or sets whether redirect responses should be automatically followed.
    /// </summary>
    public bool AllowAutoRedirect { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of redirect responses to follow.
    /// </summary>
    public int? MaxAutomaticRedirections { get; set; } = 7;

    /// <summary>
    /// Gets or sets the base address of the client.
    /// </summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// Gets or sets the timeout for the client.
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}

