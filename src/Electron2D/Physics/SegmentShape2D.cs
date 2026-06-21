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
/// Provides a finite line segment 2D physics shape resource.
/// </summary>
///
/// <remarks>
/// Segment shapes are useful for simple static boundaries. A segment must use
/// two distinct finite points.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate resources on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
public sealed class SegmentShape2D : Shape2D
{
    private Vector2 a = new(-10f, 0f);
    private Vector2 b = new(10f, 0f);

    /// <summary>
    /// Gets or sets the first segment endpoint.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Vector2 A
    {
        get
        {
            ThrowIfFreed();
            return a;
        }
        set
        {
            ThrowIfFreed();
            Shape2DValidation.RequireDistinctSegment(value, b, nameof(SegmentShape2D));
            a = value;
        }
    }

    /// <summary>
    /// Gets or sets the second segment endpoint.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Vector2 B
    {
        get
        {
            ThrowIfFreed();
            return b;
        }
        set
        {
            ThrowIfFreed();
            Shape2DValidation.RequireDistinctSegment(a, value, nameof(SegmentShape2D));
            b = value;
        }
    }

    /// <inheritdoc />
    protected override Rid CreatePhysicsRid()
    {
        return PhysicsServer2D.SegmentShapeCreate();
    }
}
