/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using System.Collections;
using System.Text;

using VariantDictionary = System.Collections.Generic.Dictionary<Electron2D.Variant, Electron2D.Variant>;

namespace Electron2D.Collections;

/// <summary>
/// Stores mutable key/value pairs where both keys and values are <see cref="Variant"/>.
/// </summary>
///
/// <remarks>
/// <para>
/// This type is the Electron2D 0.1.0 Preview counterpart of Godot's
/// <c>Godot.Collections.Dictionary</c>. It is intentionally limited to Variant
/// keys and Variant values so the supported value set stays closed.
/// </para>
///
/// <para>
/// The container itself is reference-like: assigning the same instance to
/// multiple variants stores the same mutable dictionary object.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. External synchronization is required when the
/// same instance is mutated from multiple threads.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
public sealed class Dictionary : IEnumerable<KeyValuePair<Variant, Variant>>
{
    private readonly VariantDictionary _values = new();

    /// <summary>
    /// Gets or sets the value stored for a key.
    /// </summary>
    ///
    /// <param name="key">The key to read or write.</param>
    ///
    /// <returns>The value stored for <paramref name="key"/>.</returns>
    ///
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the getter is used and <paramref name="key"/> is not present.
    /// </exception>
    public Variant this[Variant key]
    {
        get => _values[key];
        set => _values[key] = value;
    }

    /// <summary>
    /// Gets the number of key/value pairs stored in the dictionary.
    /// </summary>
    public int Count => _values.Count;

    /// <summary>
    /// Adds a key/value pair to the dictionary.
    /// </summary>
    ///
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to store for <paramref name="key"/>.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key"/> already exists.
    /// </exception>
    public void Add(Variant key, Variant value)
    {
        _values.Add(key, value);
    }

    /// <summary>
    /// Removes all key/value pairs from the dictionary.
    /// </summary>
    public void Clear()
    {
        _values.Clear();
    }

    /// <summary>
    /// Checks whether the dictionary contains a key.
    /// </summary>
    ///
    /// <param name="key">The key to search for.</param>
    ///
    /// <returns><c>true</c> if the key is present; otherwise, <c>false</c>.</returns>
    public bool ContainsKey(Variant key)
    {
        return _values.ContainsKey(key);
    }

    /// <summary>
    /// Removes a key and its value.
    /// </summary>
    ///
    /// <param name="key">The key to remove.</param>
    ///
    /// <returns><c>true</c> if the key was found and removed; otherwise, <c>false</c>.</returns>
    public bool Remove(Variant key)
    {
        return _values.Remove(key);
    }

    /// <summary>
    /// Attempts to read a value without throwing when the key is missing.
    /// </summary>
    ///
    /// <param name="key">The key to search for.</param>
    /// <param name="value">
    /// Receives the stored value when the key is present; otherwise receives
    /// <c>default(Variant)</c>.
    /// </param>
    ///
    /// <returns><c>true</c> if the key is present; otherwise, <c>false</c>.</returns>
    public bool TryGetValue(Variant key, out Variant value)
    {
        return _values.TryGetValue(key, out value);
    }

    /// <summary>
    /// Returns an enumerator over the current key/value pairs.
    /// </summary>
    ///
    /// <returns>An enumerator over key/value pairs.</returns>
    public IEnumerator<KeyValuePair<Variant, Variant>> GetEnumerator()
    {
        return _values.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Returns a diagnostic string representation of this dictionary.
    /// </summary>
    ///
    /// <returns>A comma-separated list of key/value pairs wrapped in braces.</returns>
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append('{');
        var first = true;

        foreach (var pair in _values)
        {
            if (!first)
            {
                builder.Append(", ");
            }

            builder.Append(pair.Key);
            builder.Append(": ");
            builder.Append(pair.Value);
            first = false;
        }

        builder.Append('}');
        return builder.ToString();
    }
}
