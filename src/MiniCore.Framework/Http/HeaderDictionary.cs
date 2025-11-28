using System.Collections;

using MiniCore.Framework.Http.Abstractions;

namespace MiniCore.Framework.Http;

/// <summary>
/// Represents a collection of HTTP headers.
/// </summary>
public class HeaderDictionary : IHeaderDictionary
{
    private readonly Dictionary<string, StringValues> _headers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the Content-Type header.
    /// </summary>
    public string? ContentType
    {
        get => this["Content-Type"];
        set => this["Content-Type"] = value;
    }

    /// <summary>
    /// Gets or sets the Content-Length header.
    /// </summary>
    public long? ContentLength
    {
        get
        {
            if (TryGetValue("Content-Length", out var value) && long.TryParse(value, out var length))
            {
                return length;
            }
            return null;
        }
        set
        {
            if (value.HasValue)
            {
                this["Content-Length"] = value.Value.ToString();
            }
            else
            {
                Remove("Content-Length");
            }
        }
    }

    public StringValues this[string key]
    {
        get => _headers.TryGetValue(key, out var value) ? value : default;
        set => _headers[key] = value;
    }

    public ICollection<string> Keys => _headers.Keys;
    public ICollection<StringValues> Values => _headers.Values;
    public int Count => _headers.Count;
    public bool IsReadOnly => false;

    public void Add(string key, StringValues value) => _headers.Add(key, value);

    public bool ContainsKey(string key) => _headers.ContainsKey(key);

    public bool Remove(string key) => _headers.Remove(key);

    public bool TryGetValue(string key, out StringValues value) => _headers.TryGetValue(key, out value);

    public void Add(KeyValuePair<string, StringValues> item) => _headers.Add(item.Key, item.Value);

    public void Clear() => _headers.Clear();

    public bool Contains(KeyValuePair<string, StringValues> item) => 
        _headers.TryGetValue(item.Key, out var value) && value.Equals(item.Value);

    public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
    {
        foreach (var kvp in _headers)
        {
            array[arrayIndex++] = kvp;
        }
    }

    public bool Remove(KeyValuePair<string, StringValues> item)
    {
        if (Contains(item))
        {
            return _headers.Remove(item.Key);
        }
        return false;
    }

    public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator() => _headers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

