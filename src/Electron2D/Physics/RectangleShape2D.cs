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
/// Provides a rectangular 2D physics shape resource.
/// </summary>
///
/// <remarks>
/// The rectangle is centered on the owning <see cref="CollisionShape2D" /> and
/// uses the <see cref="Size" /> property as its full width and height.
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
public sealed class RectangleShape2D : Shape2D
{

    /// <summary>
    /// Initializes a new instance of the RectangleShape2D type.
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
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RectangleShape2D" />
    ///
    public RectangleShape2D()
    {
    }

    private Vector2 size = new(20f, 20f);

    /// <summary>
    /// Gets or sets the full width and height of the rectangle.
    /// </summary>
    ///
    /// <value>
    /// A finite vector with positive X and Y values.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="RectangleShape2D" />
    ///
    public Vector2 Size
    {
        get
        {
            ThrowIfFreed();
            return size;
        }
        set
        {
            ThrowIfFreed();
            Shape2DValidation.RequirePositiveSize(value, $"{nameof(RectangleShape2D)}.{nameof(Size)}");
            size = value;
        }
    }

    /// <inheritdoc />
    protected override Rid CreatePhysicsRid()
    {
        return PhysicsServer2D.RectangleShapeCreate();
    }
}
