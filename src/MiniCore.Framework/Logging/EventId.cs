namespace MiniCore.Framework.Logging;

/// <summary>
/// Identifies a logging event. The primary identifier is the "Id" property, with the "Name" property providing a short description of this type of event.
/// </summary>
public readonly struct EventId
{
    /// <summary>
    /// Initializes an instance of the <see cref="EventId"/> struct.
    /// </summary>
    /// <param name="id">The numeric identifier for this event.</param>
    /// <param name="name">The name of this event.</param>
    public EventId(int id, string? name = null)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Gets the numeric identifier for this event.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the name of this event.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Implicitly creates an EventId from the given <see cref="int"/>.
    /// </summary>
    /// <param name="i">The <see cref="int"/> to convert to an EventId.</param>
    public static implicit operator EventId(int i) => new(i);

    /// <summary>
    /// Checks if two specified <see cref="EventId"/> instances have the same value.
    /// </summary>
    /// <param name="left">The first <see cref="EventId"/>.</param>
    /// <param name="right">The second <see cref="EventId"/>.</param>
    /// <returns><c>true</c> if the objects are equal.</returns>
    public static bool operator ==(EventId left, EventId right) => left.Equals(right);

    /// <summary>
    /// Checks if two specified <see cref="EventId"/> instances have different values.
    /// </summary>
    /// <param name="left">The first <see cref="EventId"/>.</param>
    /// <param name="right">The second <see cref="EventId"/>.</param>
    /// <returns><c>true</c> if the objects are not equal.</returns>
    public static bool operator !=(EventId left, EventId right) => !left.Equals(right);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is EventId other && Equals(other);

    /// <inheritdoc />
    public bool Equals(EventId other) => Id == other.Id;

    /// <inheritdoc />
    public override int GetHashCode() => Id;

    /// <inheritdoc />
    public override string ToString() => Name == null ? Id.ToString() : $"{Id}:{Name}";
}

