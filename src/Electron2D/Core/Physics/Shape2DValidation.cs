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

internal static class Shape2DValidation
{
    private const float Epsilon = 0.00001f;

    public static void RequirePositive(float value, string memberName)
    {
        if (!float.IsFinite(value) || value <= 0f)
        {
            throw new ArgumentOutOfRangeException(memberName, $"{memberName} must be a positive finite value.");
        }
    }

    public static void RequirePositiveSize(Vector2 value, string memberName)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y) || value.X <= 0f || value.Y <= 0f)
        {
            throw new ArgumentOutOfRangeException(memberName, $"{memberName} must have positive finite X and Y values.");
        }
    }

    public static void RequireCapsuleHeight(float radius, float height, string memberName)
    {
        RequirePositive(height, memberName);
        if (height <= radius * 2f)
        {
            throw new ArgumentOutOfRangeException(memberName, $"{memberName} must be greater than the capsule diameter.");
        }
    }

    public static void RequireDistinctSegment(Vector2 a, Vector2 b, string memberName)
    {
        RequireFinite(a, memberName);
        RequireFinite(b, memberName);
        if (a == b)
        {
            throw new ArgumentException($"{memberName} must use two distinct points.", memberName);
        }
    }

    public static Vector2[] CopyValidConvexPolygon(Vector2[] points, string memberName)
    {
        ArgumentNullException.ThrowIfNull(points);
        var copy = points.ToArray();
        if (copy.Length < 3)
        {
            throw new ArgumentException($"{memberName} must contain at least three points.", memberName);
        }

        for (var index = 0; index < copy.Length; index++)
        {
            RequireFinite(copy[index], memberName);
            for (var other = index + 1; other < copy.Length; other++)
            {
                if (copy[index] == copy[other])
                {
                    throw new ArgumentException($"{memberName} must not contain duplicate points.", memberName);
                }
            }
        }

        EnsureConvex(copy, memberName);
        return copy;
    }

    public static Vector2[] CopyValidConcaveSegments(Vector2[] segments, string memberName)
    {
        ArgumentNullException.ThrowIfNull(segments);
        var copy = segments.ToArray();
        if (copy.Length < 2 || copy.Length % 2 != 0)
        {
            throw new ArgumentException($"{memberName} must contain one or more point pairs.", memberName);
        }

        for (var index = 0; index < copy.Length; index += 2)
        {
            RequireDistinctSegment(copy[index], copy[index + 1], memberName);
        }

        return copy;
    }

    private static void EnsureConvex(Vector2[] points, string memberName)
    {
        var sign = 0f;
        for (var index = 0; index < points.Length; index++)
        {
            var a = points[index];
            var b = points[(index + 1) % points.Length];
            var c = points[(index + 2) % points.Length];
            var cross = (b - a).Cross(c - b);
            if (MathF.Abs(cross) <= Epsilon)
            {
                continue;
            }

            var currentSign = MathF.Sign(cross);
            if (sign == 0f)
            {
                sign = currentSign;
                continue;
            }

            if (currentSign != sign)
            {
                throw new ArgumentException($"{memberName} must describe a convex polygon.", memberName);
            }
        }

        if (sign == 0f)
        {
            throw new ArgumentException($"{memberName} must not be collinear.", memberName);
        }
    }

    private static void RequireFinite(Vector2 value, string memberName)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y))
        {
            throw new ArgumentException($"{memberName} must contain only finite points.", memberName);
        }
    }
}
