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
/// Provides a concave polygon 2D physics shape resource backed by line segments.
/// </summary>
///
/// <remarks>
/// Concave polygon shapes are allowed only under <see cref="StaticBody2D" /> in
/// the preview physics model. Use <see cref="ConvexPolygonShape2D" /> for
/// non-static bodies.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate resources on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
public sealed class ConcavePolygonShape2D : Shape2D
{

    /// <summary>
    /// Initializes a new instance of the ConcavePolygonShape2D type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="ConcavePolygonShape2D" />
    ///
    public ConcavePolygonShape2D()
    {
    }

    private Vector2[] segments =
    [
        new(0f, 0f),
        new(10f, 0f)
    ];

    /// <summary>
    /// Gets or sets the line segment endpoint pairs that make up the shape.
    /// </summary>
    ///
    /// <value>
    /// An even-length array containing one or more finite, non-zero point pairs.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="ConcavePolygonShape2D" />
    ///
    public Vector2[] Segments
    {
        get
        {
            ThrowIfFreed();
            return segments.ToArray();
        }
        set
        {
            ThrowIfFreed();
            segments = Shape2DValidation.CopyValidConcaveSegments(
                value,
                $"{nameof(ConcavePolygonShape2D)}.{nameof(Segments)}");
        }
    }

    /// <inheritdoc />
    protected override Rid CreatePhysicsRid()
    {
        return PhysicsServer2D.ConcavePolygonShapeCreate();
    }
}
