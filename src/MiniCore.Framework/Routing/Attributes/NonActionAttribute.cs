namespace MiniCore.Framework.Routing.Attributes;

/// <summary>
/// Represents an attribute that is used to indicate that a controller method is not an action method.
/// Methods marked with this attribute will be ignored during controller discovery and route mapping.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class NonActionAttribute : Attribute
{
}

