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
/// Represents the node path value type.
/// </summary>
///
/// <remarks>
/// This type is part of the Electron2D 0.1.0 Preview public API.
/// </remarks>
///
/// <threadsafety>
/// Instances of this type are not synchronized. Access them from the thread that owns the object unless the member documentation states otherwise.
/// </threadsafety>
///
/// <since>
/// This API is available since Electron2D 0.1.0 Preview.
/// </since>
///
public readonly struct NodePath : IEquatable<NodePath>
{
    private readonly string? _path;

    /// <summary>
    /// Initializes a new instance of the NodePath type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="path">
    /// The path value.
    /// </param>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="NodePath" />
    ///
    public NodePath(string path)
    {
        _path = path ?? string.Empty;
    }

    /// <summary>
    /// Gets the name count value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current name count value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="NodePath" />
    ///
    public int GetNameCount()
    {
        return GetNodeNames().Length;
    }

    /// <summary>
    /// Gets the name value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="index">
    /// The index value.
    /// </param>
    ///
    /// <returns>
    /// The current name value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="NodePath" />
    ///
    public string GetName(int index)
    {
        var names = GetNodeNames();
        if (index < 0 || index >= names.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return names[index];
    }

    /// <summary>
    /// Gets the subname count value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current subname count value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="NodePath" />
    ///
    public int GetSubnameCount()
    {
        return GetSubnames().Length;
    }

    /// <summary>
    /// Gets the subname value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="index">
    /// The index value.
    /// </param>
    ///
    /// <returns>
    /// The current subname value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="NodePath" />
    ///
    public string GetSubname(int index)
    {
        var subnames = GetSubnames();
        if (index < 0 || index >= subnames.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return subnames[index];
    }

    /// <summary>
    /// Checks whether absolute is true.
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
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="NodePath" />
    ///
    public bool IsAbsolute()
    {
        return Text.StartsWith("/", StringComparison.Ordinal);
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
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="NodePath" />
    ///
    public bool IsEmpty()
    {
        return Text.Length == 0;
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
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="NodePath" />
    ///
    public override string ToString()
    {
        return Text;
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
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="NodePath" />
    ///
    public bool Equals(NodePath other)
    {
        return string.Equals(Text, other.Text, StringComparison.Ordinal);
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
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="NodePath" />
    ///
    public override bool Equals(object? obj)
    {
        return obj is NodePath other && Equals(other);
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
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="NodePath" />
    ///
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Text);
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
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="NodePath" />
    ///
    public static bool operator ==(NodePath left, NodePath right)
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
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="NodePath" />
    ///
    public static bool operator !=(NodePath left, NodePath right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Converts the supplied value to the target type.
    /// </summary>
    ///
    /// <remarks>
    /// The conversion follows the validation rules of the source and target types.
    /// </remarks>
    ///
    /// <param name="path">
    /// The path value.
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
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="NodePath" />
    ///
    public static implicit operator NodePath(string path)
    {
        return new NodePath(path);
    }

    internal string[] GetNodeNames()
    {
        var nodePart = GetNodePart();
        if (IsAbsolute())
        {
            nodePart = nodePart.Length > 0 ? nodePart[1..] : string.Empty;
        }

        if (nodePart.Length == 0)
        {
            return Array.Empty<string>();
        }

        return nodePart.Split('/', StringSplitOptions.RemoveEmptyEntries);
    }

    private string[] GetSubnames()
    {
        var separatorIndex = Text.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex < 0 || separatorIndex == Text.Length - 1)
        {
            return Array.Empty<string>();
        }

        return Text[(separatorIndex + 1)..].Split(':', StringSplitOptions.RemoveEmptyEntries);
    }

    private string GetNodePart()
    {
        var separatorIndex = Text.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex < 0 ? Text : Text[..separatorIndex];
    }

    private string Text => _path ?? string.Empty;
}
