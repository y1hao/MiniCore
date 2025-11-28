namespace MiniCore.Framework.Routing.Attributes;

/// <summary>
/// Indicates that a type is a controller. This attribute can be used to mark a class as a controller
/// even if it doesn't follow the naming convention (ending with "Controller").
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ControllerAttribute : Attribute
{
}

