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
/// This type stores only values that can be represented by <see cref="Variant"/>
/// and is intended for dynamic Electron2D APIs.
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
///
/// <seealso cref="Dictionary"/>
/// <seealso cref="Variant"/>
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
    ///
    /// <remarks>
    /// <para>
    /// The getter and setter access the live mutable container. Assigning a
    /// value replaces the existing element without changing <see cref="Count"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This indexer is not synchronized. External synchronization is required
    /// when the same instance is accessed from multiple threads.
    /// </threadsafety>
    ///
    /// <since>
    /// This indexer is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Add"/>
    /// <seealso cref="RemoveAt"/>
    public Variant this[int index]
    {
        get => _values[index];
        set => _values[index] = value;
    }

    /// <summary>
    /// Gets the number of values stored in the array.
    /// </summary>
    ///
    /// <value>
    /// The current number of <see cref="Variant"/> values.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// This value changes when <see cref="Add"/>, <see cref="RemoveAt"/> or
    /// <see cref="Clear"/> mutates the container.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. External synchronization is required
    /// when the same instance is accessed from multiple threads.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public int Count => _values.Count;

    /// <summary>
    /// Appends a value to the end of the array.
    /// </summary>
    ///
    /// <param name="value">The value to append.</param>
    ///
    /// <remarks>
    /// <para>
    /// The appended value becomes available at index <c>Count - 1</c> after the
    /// call returns.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. External synchronization is required
    /// when the same instance is mutated from multiple threads.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RemoveAt"/>
    /// <seealso cref="Clear"/>
    public void Add(Variant value)
    {
        _values.Add(value);
    }

    /// <summary>
    /// Removes all values from the array.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Existing references to this <see cref="Array"/> observe the same mutable
    /// instance after it has been cleared.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. External synchronization is required
    /// when the same instance is mutated from multiple threads.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Count"/>
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
    ///
    /// <remarks>
    /// <para>
    /// Values after <paramref name="index"/> shift down by one position.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. External synchronization is required
    /// when the same instance is mutated from multiple threads.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Add"/>
    /// <seealso cref="Clear"/>
    public void RemoveAt(int index)
    {
        _values.RemoveAt(index);
    }

    /// <summary>
    /// Returns a snapshot of the current array values.
    /// </summary>
    ///
    /// <returns>A new CLR array containing the current <see cref="Variant"/> values.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The returned CLR array is a snapshot. Later changes to this
    /// <see cref="Array"/> do not change the returned array instance.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. External synchronization is required
    /// when the same instance may be mutated while the snapshot is being made.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetEnumerator"/>
    public Variant[] ToArray()
    {
        return _values.ToArray();
    }

    /// <summary>
    /// Returns an enumerator over the current values.
    /// </summary>
    ///
    /// <returns>An enumerator over <see cref="Variant"/> values.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The enumerator follows the behavior of <see cref="List{T}.GetEnumerator"/>.
    /// Mutating this container while enumerating it invalidates the enumerator.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. External synchronization is required
    /// when the same instance may be mutated while it is being enumerated.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ToArray"/>
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
    ///
    /// <remarks>
    /// <para>
    /// The result is intended for diagnostics and logs. It is not a stable
    /// serialization format.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. External synchronization is required
    /// when the same instance may be mutated while the string is being built.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
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
