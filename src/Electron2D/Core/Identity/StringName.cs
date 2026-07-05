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
namespace Electron2D;

/// <summary>
/// Represents the string name value type.
/// </summary>
///
/// <remarks>
/// This type is part of the Electron2D 0.1-preview public API.
/// </remarks>
///
/// <threadsafety>
/// Instances of this type are not synchronized. Access them from the thread that owns the object unless the member documentation states otherwise.
/// </threadsafety>
///
/// <since>
/// This API is available since Electron2D 0.1-preview.
/// </since>
///
public readonly struct StringName : IEquatable<StringName>
{
    private readonly string? value;

    /// <summary>
    /// Initializes a new instance of the StringName type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="value">
    /// The value to use.
    /// </param>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="StringName" />
    ///
    public StringName(string? value)
    {
        this.value = string.Intern(value ?? string.Empty);
    }

    /// <summary>
    /// Checks whether empty is true.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// <c>true</c> when the condition is met; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="StringName" />
    ///
    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(Value);
    }

    /// <summary>
    /// Executes the to string operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="StringName" />
    ///
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    /// Executes the equals operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="obj">
    /// The obj value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="StringName" />
    ///
    public override bool Equals(object? obj)
    {
        return obj switch
        {
            StringName other => Equals(other),
            string other => Equals(new StringName(other)),
            _ => false
        };
    }

    /// <summary>
    /// Executes the equals operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="other">
    /// The other value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="StringName" />
    ///
    public bool Equals(StringName other)
    {
        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the hash code value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current hash code value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="StringName" />
    ///
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Value);
    }

    /// <summary>
    /// Applies the <c>==</c> operator.
    /// </summary>
    ///
    /// <remarks>
    /// This operator returns a value derived from the supplied operands.
    /// </remarks>
    ///
    /// <param name="left">
    /// The left value.
    /// </param>
    ///
    /// <param name="right">
    /// The right value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operator.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="StringName" />
    ///
    public static bool operator ==(StringName left, StringName right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Applies the <c>!=</c> operator.
    /// </summary>
    ///
    /// <remarks>
    /// This operator returns a value derived from the supplied operands.
    /// </remarks>
    ///
    /// <param name="left">
    /// The left value.
    /// </param>
    ///
    /// <param name="right">
    /// The right value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operator.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="StringName" />
    ///
    public static bool operator !=(StringName left, StringName right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Applies the <c>==</c> operator.
    /// </summary>
    ///
    /// <remarks>
    /// This operator returns a value derived from the supplied operands.
    /// </remarks>
    ///
    /// <param name="left">
    /// The left value.
    /// </param>
    ///
    /// <param name="right">
    /// The right value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operator.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="StringName" />
    ///
    public static bool operator ==(StringName left, string? right)
    {
        return left.Equals(new StringName(right));
    }

    /// <summary>
    /// Applies the <c>!=</c> operator.
    /// </summary>
    ///
    /// <remarks>
    /// This operator returns a value derived from the supplied operands.
    /// </remarks>
    ///
    /// <param name="left">
    /// The left value.
    /// </param>
    ///
    /// <param name="right">
    /// The right value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operator.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="StringName" />
    ///
    public static bool operator !=(StringName left, string? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Applies the <c>==</c> operator.
    /// </summary>
    ///
    /// <remarks>
    /// This operator returns a value derived from the supplied operands.
    /// </remarks>
    ///
    /// <param name="left">
    /// The left value.
    /// </param>
    ///
    /// <param name="right">
    /// The right value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operator.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="StringName" />
    ///
    public static bool operator ==(string? left, StringName right)
    {
        return new StringName(left).Equals(right);
    }

    /// <summary>
    /// Applies the <c>!=</c> operator.
    /// </summary>
    ///
    /// <remarks>
    /// This operator returns a value derived from the supplied operands.
    /// </remarks>
    ///
    /// <param name="left">
    /// The left value.
    /// </param>
    ///
    /// <param name="right">
    /// The right value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operator.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="StringName" />
    ///
    public static bool operator !=(string? left, StringName right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Converts the supplied value to the target type.
    /// </summary>
    ///
    /// <remarks>
    /// The conversion follows the validation rules of the source and target types.
    /// </remarks>
    ///
    /// <param name="value">
    /// The value to use.
    /// </param>
    ///
    /// <returns>
    /// The converted value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="StringName" />
    ///
    public static implicit operator StringName(string? value)
    {
        return new StringName(value);
    }

    private string Value => value ?? string.Empty;
}
