using System.Reflection;

namespace MiniCore.Framework.Mvc.Abstractions;

/// <summary>
/// Discovers controllers and their action methods.
/// </summary>
public interface IControllerDiscovery
{
    /// <summary>
    /// Discovers controllers in the specified assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>A collection of discovered controllers.</returns>
    IEnumerable<ControllerInfo> DiscoverControllers(params Assembly[] assemblies);

    /// <summary>
    /// Gets the action methods for a controller type.
    /// </summary>
    /// <param name="controllerType">The controller type.</param>
    /// <returns>A collection of action method information.</returns>
    IEnumerable<ActionMethodInfo> GetActionMethods(Type controllerType);
}

/// <summary>
/// Information about a discovered controller.
/// </summary>
public class ControllerInfo
{
    /// <summary>
    /// Gets or sets the controller type.
    /// </summary>
    public Type ControllerType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the route prefix.
    /// </summary>
    public string? RoutePrefix { get; set; }
}

/// <summary>
/// Information about an action method.
/// </summary>
public class ActionMethodInfo
{
    /// <summary>
    /// Gets or sets the method info.
    /// </summary>
    public MethodInfo Method { get; set; } = null!;

    /// <summary>
    /// Gets or sets the HTTP methods.
    /// </summary>
    public List<(string Method, string? Template)> HttpMethods { get; set; } = new();
}

