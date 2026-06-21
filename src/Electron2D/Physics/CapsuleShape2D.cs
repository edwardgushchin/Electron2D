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
/// Provides a capsule-shaped 2D physics resource.
/// </summary>
///
/// <remarks>
/// The capsule is centered on the owning <see cref="CollisionShape2D" />. Its
/// height must stay greater than its diameter.
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
public sealed class CapsuleShape2D : Shape2D
{
    private float radius;
    private float height;

    /// <summary>
    /// Creates a capsule shape with Electron2D preview defaults.
    /// </summary>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    public CapsuleShape2D()
        : this(10f, 20.01f)
    {
    }

    internal CapsuleShape2D(float radius, float height)
    {
        Shape2DValidation.RequirePositive(radius, $"{nameof(CapsuleShape2D)}.{nameof(Radius)}");
        Shape2DValidation.RequireCapsuleHeight(radius, height, $"{nameof(CapsuleShape2D)}.{nameof(Height)}");
        this.radius = radius;
        this.height = height;
    }

    /// <summary>
    /// Gets or sets the capsule radius.
    /// </summary>
    ///
    /// <value>
    /// A positive finite radius that fits within <see cref="Height" />.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public float Radius
    {
        get
        {
            ThrowIfFreed();
            return radius;
        }
        set
        {
            ThrowIfFreed();
            Shape2DValidation.RequirePositive(value, $"{nameof(CapsuleShape2D)}.{nameof(Radius)}");
            Shape2DValidation.RequireCapsuleHeight(value, height, $"{nameof(CapsuleShape2D)}.{nameof(Radius)}");
            radius = value;
        }
    }

    /// <summary>
    /// Gets or sets the full capsule height.
    /// </summary>
    ///
    /// <value>
    /// A positive finite height greater than the capsule diameter.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public float Height
    {
        get
        {
            ThrowIfFreed();
            return height;
        }
        set
        {
            ThrowIfFreed();
            Shape2DValidation.RequireCapsuleHeight(radius, value, $"{nameof(CapsuleShape2D)}.{nameof(Height)}");
            height = value;
        }
    }

    /// <inheritdoc />
    protected override Rid CreatePhysicsRid()
    {
        return PhysicsServer2D.CapsuleShapeCreate();
    }
}
