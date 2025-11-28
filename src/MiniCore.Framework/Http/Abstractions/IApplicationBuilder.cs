using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Http;

namespace MiniCore.Framework.Http.Abstractions;

/// <summary>
/// Defines a class that provides the mechanisms to configure an application's request pipeline.
/// </summary>
public interface IApplicationBuilder
{
    /// <summary>
    /// Gets the <see cref="DependencyInjection.IServiceProvider"/> that provides access to the application's service container.
    /// </summary>
    DependencyInjection.IServiceProvider ApplicationServices { get; set; }

    /// <summary>
    /// Gets a key/value collection that can be used to share data between middleware.
    /// </summary>
    IDictionary<string, object?> Properties { get; }

    /// <summary>
    /// Adds a middleware delegate to the application's request pipeline.
    /// </summary>
    /// <param name="middleware">The middleware delegate.</param>
    /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
    IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);

    /// <summary>
    /// Creates a new <see cref="IApplicationBuilder"/> that shares the <see cref="Properties"/> of this
    /// <see cref="IApplicationBuilder"/> but otherwise has its own request pipeline.
    /// </summary>
    /// <returns>The new <see cref="IApplicationBuilder"/>.</returns>
    IApplicationBuilder New();

    /// <summary>
    /// Builds the request delegate pipeline.
    /// </summary>
    /// <returns>The request delegate.</returns>
    RequestDelegate Build();
}

