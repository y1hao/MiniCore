using System.Collections.Generic;

namespace MiniCore.Framework.DependencyInjection;

/// <summary>
/// Default implementation of <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class extends <see cref="List{T}"/> of <see cref="ServiceDescriptor"/> and implements
/// <see cref="IServiceCollection"/>. It provides a mutable collection for registering services
/// before building an <see cref="IServiceProvider"/>.
/// </para>
/// <para>
/// Services are registered by adding <see cref="ServiceDescriptor"/> instances to this collection.
/// Extension methods in <see cref="ServiceCollectionExtensions"/> provide convenient ways to
/// register services with different lifetimes.
/// </para>
/// </remarks>
public class ServiceCollection : List<ServiceDescriptor>, IServiceCollection
{
    /// <summary>
    /// Initializes a new instance of <see cref="ServiceCollection"/>.
    /// </summary>
    public ServiceCollection()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ServiceCollection"/> that contains elements copied from the specified collection.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new list.</param>
    public ServiceCollection(IEnumerable<ServiceDescriptor> collection)
        : base(collection)
    {
    }
}

