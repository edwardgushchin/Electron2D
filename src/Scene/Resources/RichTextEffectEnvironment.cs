namespace Electron2D;

/// <summary>Retains named, exact typed arguments for one rich-text effect block.</summary>
/// <remarks>Values remain borrowed. This parameter context is not a scene property or dynamic invocation
/// protocol. Parsed mixed argument arrays are accessed by typed element queries.</remarks>
public sealed class RichTextEffectEnvironment
{
    private sealed record Value<T>(T Item);
    private sealed class Elements { internal readonly List<object> Values = []; }
    private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);
    /// <summary>Creates an empty argument context.</summary>
    public RichTextEffectEnvironment() { }
    /// <summary>Gets the number of named arguments.</summary><value>Current key count.</value>
    public int Count => _values.Count;
    /// <summary>Gets an independent key array.</summary><returns>Ordinal argument names.</returns>
    public string[] GetKeys() => _values.Keys.ToArray();
    /// <summary>Sets a borrowed argument under its exact type.</summary><typeparam name="T">Argument type.</typeparam><param name="name">Nonnull name.</param><param name="value">Borrowed value.</param>
    public void Set<T>(string name, T value) { ArgumentNullException.ThrowIfNull(name); _values[name] = new Value<T>(value); }
    /// <summary>Reads an argument only when its stored type is exactly T.</summary><typeparam name="T">Stored type.</typeparam><param name="name">Nonnull name.</param><param name="value">Borrowed value or default.</param><returns>Whether the exact value exists.</returns>
    public bool TryGet<T>(string name, out T value)
    { ArgumentNullException.ThrowIfNull(name); if (_values.TryGetValue(name, out var item) && item is Value<T> stored) { value = stored.Item; return true; } value = default!; return false; }
    /// <summary>Reads a numeric parameter using the supported numeric argument representations.</summary><param name="name">Argument name.</param><param name="value">Number or zero.</param><returns>Whether a numeric value exists.</returns>
    public bool TryGetNumber(string name, out double value)
    { if (TryGet<double>(name, out value)) return true; if (TryGet<long>(name, out var integer)) { value = integer; return true; } if (TryGet<float>(name, out var single)) { value = single; return true; } if (TryGet<int>(name, out var small)) { value = small; return true; } value = 0; return false; }
    /// <summary>Appends one exact typed value to a mixed argument array.</summary><typeparam name="T">Element type.</typeparam><param name="name">Nonnull name.</param><param name="value">Borrowed element.</param>
    public void AddElement<T>(string name, T value)
    { ArgumentNullException.ThrowIfNull(name); if (!_values.TryGetValue(name, out var item)) _values.Add(name, item = new Elements()); if (item is not Elements elements) throw new InvalidOperationException("The argument is not an element array."); elements.Values.Add(new Value<T>(value)); }
    /// <summary>Gets the length of a mixed argument array, or zero for another value.</summary><param name="name">Nonnull name.</param><returns>Element count.</returns>
    public int GetElementCount(string name) { ArgumentNullException.ThrowIfNull(name); return _values.TryGetValue(name, out var item) && item is Elements elements ? elements.Values.Count : 0; }
    /// <summary>Reads one mixed array element under its exact type.</summary><typeparam name="T">Stored type.</typeparam><param name="name">Argument name.</param><param name="index">Nonnegative element index.</param><param name="value">Borrowed element or default.</param><returns>Whether that exact element exists.</returns>
    public bool TryGetElement<T>(string name, int index, out T value)
    { ArgumentNullException.ThrowIfNull(name); if (index < 0) throw new ArgumentOutOfRangeException(nameof(index)); if (_values.TryGetValue(name, out var item) && item is Elements elements && index < elements.Values.Count && elements.Values[index] is Value<T> stored) { value = stored.Item; return true; } value = default!; return false; }
    /// <summary>Removes a named argument.</summary><param name="name">Nonnull name.</param><returns>Whether it existed.</returns>
    public bool Remove(string name) { ArgumentNullException.ThrowIfNull(name); return _values.Remove(name); }
    /// <summary>Clears all arguments without disposing their values.</summary>
    public void Clear() => _values.Clear();
    internal void StoreParsed(string name, object value) => _values[name] = value;
    internal static object Parsed<T>(T value) => new Value<T>(value);
    internal static object ParsedElements(List<object> values) { var result = new Elements(); result.Values.AddRange(values); return result; }
}
