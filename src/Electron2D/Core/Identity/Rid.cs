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

public readonly struct Rid : IEquatable<Rid>, IComparable<Rid>
{
    private readonly long id;

    internal Rid(long id)
    {
        if (id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Rid ID cannot be negative.");
        }

        this.id = id;
    }

    public long GetId()
    {
        return id;
    }

    public bool IsValid()
    {
        return id != 0L;
    }

    public override string ToString()
    {
        return $"Rid({id})";
    }

    public override bool Equals(object? obj)
    {
        return obj is Rid other && Equals(other);
    }

    public bool Equals(Rid other)
    {
        return id == other.id;
    }

    public int CompareTo(Rid other)
    {
        return id.CompareTo(other.id);
    }

    public override int GetHashCode()
    {
        return id.GetHashCode();
    }

    public static bool operator ==(Rid left, Rid right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Rid left, Rid right)
    {
        return !left.Equals(right);
    }

    public static bool operator <(Rid left, Rid right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(Rid left, Rid right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(Rid left, Rid right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(Rid left, Rid right)
    {
        return left.CompareTo(right) >= 0;
    }
}
