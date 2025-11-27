namespace MiniCore.Framework.Logging;

/// <summary>
/// A generic interface for logging where the category name is derived from the specified
/// <typeparamref name="TCategoryName"/> type name.
/// Generally used to enable activation of a named <see cref="ILogger"/> from dependency injection.
/// </summary>
/// <typeparam name="TCategoryName">The type whose name is used as the logger category name.</typeparam>
public interface ILogger<out TCategoryName> : ILogger
{
}

