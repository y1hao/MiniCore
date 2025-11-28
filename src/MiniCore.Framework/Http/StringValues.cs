namespace MiniCore.Framework.Http;

/// <summary>
/// Represents zero/null, one, or many strings in an efficient way.
/// </summary>
public readonly struct StringValues : IEquatable<StringValues>, IEquatable<string>, IEquatable<string[]>
{
    private readonly string? _value;
    private readonly string[]? _values;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringValues"/> struct.
    /// </summary>
    /// <param name="value">The string value.</param>
    public StringValues(string? value)
    {
        _value = value;
        _values = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringValues"/> struct.
    /// </summary>
    /// <param name="values">The string array.</param>
    public StringValues(string[]? values)
    {
        _value = null;
        _values = values;
    }

    /// <summary>
    /// Gets the number of string values.
    /// </summary>
    public int Count
    {
        get
        {
            if (_values != null)
            {
                return _values.Length;
            }
            return _value != null ? 1 : 0;
        }
    }

    /// <summary>
    /// Gets the string value, or the first value if multiple values exist.
    /// </summary>
    public override string? ToString()
    {
        if (_values != null && _values.Length > 0)
        {
            return _values[0];
        }
        return _value;
    }

    /// <summary>
    /// Gets the string array, or creates one from the single value.
    /// </summary>
    public string[] ToArray()
    {
        if (_values != null)
        {
            return _values;
        }
        if (_value != null)
        {
            return new[] { _value };
        }
        return Array.Empty<string>();
    }

    /// <summary>
    /// Gets the value at the specified index.
    /// </summary>
    public string? this[int index]
    {
        get
        {
            if (_values != null)
            {
                return _values[index];
            }
            if (index == 0 && _value != null)
            {
                return _value;
            }
            throw new IndexOutOfRangeException();
        }
    }

    public static implicit operator StringValues(string? value) => new StringValues(value);
    public static implicit operator StringValues(string[]? values) => new StringValues(values);
    public static implicit operator string?(StringValues values) => values.ToString();
    public static implicit operator string[]?(StringValues values) => values.ToArray();

    public bool Equals(StringValues other)
    {
        if (_value != null && other._value != null)
        {
            return _value == other._value;
        }
        if (_values != null && other._values != null)
        {
            if (_values.Length != other._values.Length)
            {
                return false;
            }
            for (int i = 0; i < _values.Length; i++)
            {
                if (_values[i] != other._values[i])
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    public bool Equals(string? other)
    {
        return ToString() == other;
    }

    public bool Equals(string[]? other)
    {
        return ToArray().SequenceEqual(other ?? Array.Empty<string>());
    }

    public override bool Equals(object? obj)
    {
        if (obj is StringValues sv)
        {
            return Equals(sv);
        }
        if (obj is string s)
        {
            return Equals(s);
        }
        if (obj is string[] sa)
        {
            return Equals(sa);
        }
        return false;
    }

    public override int GetHashCode()
    {
        if (_value != null)
        {
            return _value.GetHashCode();
        }
        if (_values != null)
        {
            var hash = new HashCode();
            foreach (var value in _values)
            {
                hash.Add(value);
            }
            return hash.ToHashCode();
        }
        return 0;
    }
}

