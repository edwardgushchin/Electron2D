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

namespace Electron2D.Collections;

/// <summary>
/// Stores an ordered mutable list of <see cref="Variant"/> values.
/// </summary>
///
/// <remarks>
/// <para>
/// This type is the Electron2D 0.1.0 Preview counterpart of Godot's
/// <c>Godot.Collections.Array</c>. It stores only values that can be represented
/// by <see cref="Variant"/> and is intended for dynamic engine APIs.
/// </para>
///
/// <para>
/// The container itself is reference-like: assigning the same instance to
/// multiple variants stores the same mutable array object.
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
public sealed class Array : IEnumerable<Variant>
{
    private readonly List<Variant> _values = new();

    /// <summary>
    /// Gets or sets the value at the specified index.
    /// </summary>
    ///
    /// <param name="index">The zero-based index of the value.</param>
    ///
    /// <returns>The stored <see cref="Variant"/> value.</returns>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the array bounds.
    /// </exception>
    public Variant this[int index]
    {
        get => _values[index];
        set => _values[index] = value;
    }

    /// <summary>
    /// Gets the number of values stored in the array.
    /// </summary>
    public int Count => _values.Count;

    /// <summary>
    /// Appends a value to the end of the array.
    /// </summary>
    ///
    /// <param name="value">The value to append.</param>
    public void Add(Variant value)
    {
        _values.Add(value);
    }

    /// <summary>
    /// Removes all values from the array.
    /// </summary>
    public void Clear()
    {
        _values.Clear();
    }

    /// <summary>
    /// Removes the value at the specified index.
    /// </summary>
    ///
    /// <param name="index">The zero-based index of the value to remove.</param>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the array bounds.
    /// </exception>
    public void RemoveAt(int index)
    {
        _values.RemoveAt(index);
    }

    /// <summary>
    /// Returns a snapshot of the current array values.
    /// </summary>
    ///
    /// <returns>A new CLR array containing the current <see cref="Variant"/> values.</returns>
    public Variant[] ToArray()
    {
        return _values.ToArray();
    }

    /// <summary>
    /// Returns an enumerator over the current values.
    /// </summary>
    ///
    /// <returns>An enumerator over <see cref="Variant"/> values.</returns>
    public IEnumerator<Variant> GetEnumerator()
    {
        return _values.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Returns a diagnostic string representation of this array.
    /// </summary>
    ///
    /// <returns>A comma-separated list of values wrapped in square brackets.</returns>
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append('[');

        for (var index = 0; index < _values.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(_values[index]);
        }

        builder.Append(']');
        return builder.ToString();
    }
}
