using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Http.Abstractions;

namespace MiniCore.Framework.Http;

/// <summary>
/// Provides the mechanisms to configure an application's request pipeline.
/// </summary>
public class ApplicationBuilder : IApplicationBuilder
{
    private readonly List<Func<RequestDelegate, RequestDelegate>> _components = new();
    private readonly IDictionary<string, object?> _properties = new Dictionary<string, object?>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationBuilder"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public ApplicationBuilder(DependencyInjection.IServiceProvider serviceProvider)
    {
        ApplicationServices = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationBuilder"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="properties">The properties dictionary.</param>
    private ApplicationBuilder(DependencyInjection.IServiceProvider serviceProvider, IDictionary<string, object?> properties)
    {
        ApplicationServices = serviceProvider;
        _properties = new Dictionary<string, object?>(properties);
    }

    /// <inheritdoc />
    public DependencyInjection.IServiceProvider ApplicationServices { get; set; }

    /// <inheritdoc />
    public IDictionary<string, object?> Properties => _properties;

    /// <inheritdoc />
    public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
    {
        if (middleware == null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        _components.Add(middleware);
        return this;
    }

    /// <inheritdoc />
    public IApplicationBuilder New()
    {
        return new ApplicationBuilder(ApplicationServices, _properties);
    }

    /// <inheritdoc />
    public RequestDelegate Build()
    {
        // Start with a terminal middleware that returns 404
        RequestDelegate app = context =>
        {
            context.Response.StatusCode = 404;
            return Task.CompletedTask;
        };

        // Build the pipeline in reverse order (last registered = first executed)
        // This ensures that the first middleware registered is the outermost middleware
        for (int i = _components.Count - 1; i >= 0; i--)
        {
            app = _components[i](app);
        }

        return app;
    }
}

