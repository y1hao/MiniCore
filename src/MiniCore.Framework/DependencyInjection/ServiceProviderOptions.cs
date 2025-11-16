namespace MiniCore.Framework.DependencyInjection;

/// <summary>
/// Options for configuring various behaviors of the default <see cref="ServiceProvider"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// These options control validation and behavior of the service provider.
/// </para>
/// </remarks>
public class ServiceProviderOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to validate scopes when building the service provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the service provider will validate that scoped services are not
    /// resolved from the root service provider. This helps catch common lifetime mistakes.
    /// </para>
    /// <para>
    /// Defaults to <c>false</c>.
    /// </para>
    /// </remarks>
    public bool ValidateScopes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to validate that all services can be created when building the service provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the service provider will attempt to resolve all registered services
    /// during construction to catch any missing dependencies or circular dependencies early.
    /// </para>
    /// <para>
    /// Defaults to <c>false</c>.
    /// </para>
    /// </remarks>
    public bool ValidateOnBuild { get; set; }
}

