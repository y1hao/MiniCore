namespace MiniCore.Framework.Http;

/// <summary>
/// Represents the host portion of a URI can be used to construct URI's properly formatted and encoded for use in
/// HTTP headers.
/// </summary>
public readonly struct HostString : IEquatable<HostString>
{
    private readonly string _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostString"/> struct.
    /// </summary>
    /// <param name="value">The host value.</param>
    public HostString(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets the host value.
    /// </summary>
    public string Value => _value;

    /// <summary>
    /// Returns the host value as a string.
    /// </summary>
    public override string ToString() => _value;

    public static implicit operator HostString(string value) => new HostString(value);
    public static implicit operator string(HostString host) => host._value;

    public bool Equals(HostString other) => _value == other._value;

    public override bool Equals(object? obj) => obj is HostString other && Equals(other);

    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
}

