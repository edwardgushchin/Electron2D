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

public readonly struct StringName : IEquatable<StringName>
{
    private readonly string? value;

    public StringName(string? value)
    {
        this.value = string.Intern(value ?? string.Empty);
    }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(Value);
    }

    public override string ToString()
    {
        return Value;
    }

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            StringName other => Equals(other),
            string other => Equals(new StringName(other)),
            _ => false
        };
    }

    public bool Equals(StringName other)
    {
        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Value);
    }

    public static bool operator ==(StringName left, StringName right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StringName left, StringName right)
    {
        return !left.Equals(right);
    }

    public static bool operator ==(StringName left, string? right)
    {
        return left.Equals(new StringName(right));
    }

    public static bool operator !=(StringName left, string? right)
    {
        return !(left == right);
    }

    public static bool operator ==(string? left, StringName right)
    {
        return new StringName(left).Equals(right);
    }

    public static bool operator !=(string? left, StringName right)
    {
        return !(left == right);
    }

    public static implicit operator StringName(string? value)
    {
        return new StringName(value);
    }

    private string Value => value ?? string.Empty;
}
