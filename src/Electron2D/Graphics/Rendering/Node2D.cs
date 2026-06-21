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
/// Provides the Electron2D 2D transform node used by sprites and future 2D nodes.
/// </summary>
///
/// <remarks>
/// `Node2D` combines position, rotation and scale into a local
/// <see cref="Transform2D" />. A direct `Node2D` parent contributes to
/// <see cref="GlobalTransform" />; a non-`Node2D` parent breaks the transform
/// chain for the 0.1.0 Preview subset.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate nodes on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="CanvasItem" />
public class Node2D : CanvasItem
{

    /// <summary>
    /// Initializes a new instance of the Node2D type.
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
    /// <seealso cref="Node2D" />
    ///
    public Node2D()
    {
    }

    /// <summary>
    /// Gets or sets the local position relative to the direct `Node2D` parent.
    /// </summary>
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
    /// <value>
    /// The current position value.
    /// </value>
    ///
    /// <seealso cref="Node2D" />
    ///
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the local rotation in radians.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RotationDegrees" />
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current rotation value.
    /// </value>
    ///
    public float Rotation { get; set; }

    /// <summary>
    /// Gets or sets the local rotation in degrees.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Rotation" />
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current rotation degrees value.
    /// </value>
    ///
    public float RotationDegrees
    {
        get
        {
            ThrowIfFreed();
            return Mathf.RadToDeg(Rotation);
        }
        set
        {
            ThrowIfFreed();
            Rotation = Mathf.DegToRad(value);
        }
    }

    /// <summary>
    /// Gets or sets the local 2D scale.
    /// </summary>
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
    /// <value>
    /// The current scale value.
    /// </value>
    ///
    /// <seealso cref="Node2D" />
    ///
    public Vector2 Scale { get; set; } = Vector2.One;

    /// <summary>
    /// Gets or sets the local transform built from position, rotation and scale.
    /// </summary>
    ///
    /// <remarks>
    /// The setter decomposes position, rotation and scale for transforms without
    /// skew. Skew is outside the Electron2D 0.1.0 Preview subset.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <value>
    /// The current transform value.
    /// </value>
    ///
    /// <seealso cref="Node2D" />
    ///
    public Transform2D Transform
    {
        get
        {
            ThrowIfFreed();
            return CreateTransform(Rotation, Scale, Position);
        }
        set
        {
            ThrowIfFreed();
            ApplyTransform(value);
        }
    }

    /// <summary>
    /// Gets or sets the global 2D position.
    /// </summary>
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
    /// <value>
    /// The current global position value.
    /// </value>
    ///
    /// <seealso cref="Node2D" />
    ///
    public Vector2 GlobalPosition
    {
        get
        {
            ThrowIfFreed();
            return GlobalTransform.Origin;
        }
        set
        {
            ThrowIfFreed();
            var transform = GlobalTransform;
            transform.Origin = value;
            GlobalTransform = transform;
        }
    }

    /// <summary>
    /// Gets or sets the global rotation in radians.
    /// </summary>
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
    /// <value>
    /// The current global rotation value.
    /// </value>
    ///
    /// <seealso cref="Node2D" />
    ///
    public float GlobalRotation
    {
        get
        {
            ThrowIfFreed();
            return DecomposeRotation(GlobalTransform);
        }
        set
        {
            ThrowIfFreed();
            GlobalTransform = CreateTransform(value, GlobalScale, GlobalPosition);
        }
    }

    /// <summary>
    /// Gets or sets the global rotation in degrees.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GlobalRotation" />
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current global rotation degrees value.
    /// </value>
    ///
    public float GlobalRotationDegrees
    {
        get
        {
            ThrowIfFreed();
            return Mathf.RadToDeg(GlobalRotation);
        }
        set
        {
            ThrowIfFreed();
            GlobalRotation = Mathf.DegToRad(value);
        }
    }

    /// <summary>
    /// Gets or sets the global 2D scale.
    /// </summary>
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
    /// <value>
    /// The current global scale value.
    /// </value>
    ///
    /// <seealso cref="Node2D" />
    ///
    public Vector2 GlobalScale
    {
        get
        {
            ThrowIfFreed();
            return DecomposeScale(GlobalTransform);
        }
        set
        {
            ThrowIfFreed();
            GlobalTransform = CreateTransform(GlobalRotation, value, GlobalPosition);
        }
    }

    /// <summary>
    /// Gets or sets the transform relative to the global 2D canvas.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Transform" />
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current global transform value.
    /// </value>
    ///
    public Transform2D GlobalTransform
    {
        get
        {
            ThrowIfFreed();
            return GetParent() is Node2D parent ? parent.GlobalTransform * Transform : Transform;
        }
        set
        {
            ThrowIfFreed();
            var local = GetParent() is Node2D parent ? parent.GlobalTransform.AffineInverse() * value : value;
            ApplyTransform(local);
        }
    }

    /// <summary>
    /// Multiplies the current local scale by a ratio.
    /// </summary>
    ///
    /// <param name="ratio">The X and Y scale ratio to multiply into <see cref="Scale" />.</param>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Node2D" />
    ///
    public void ApplyScale(Vector2 ratio)
    {
        ThrowIfFreed();
        Scale *= ratio;
    }

    /// <summary>
    /// Translates the node in global coordinates.
    /// </summary>
    ///
    /// <param name="offset">The global offset to add to <see cref="GlobalPosition" />.</param>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Node2D" />
    ///
    public void GlobalTranslate(Vector2 offset)
    {
        ThrowIfFreed();
        GlobalPosition += offset;
    }

    /// <summary>
    /// Adds a rotation in radians to the local transform.
    /// </summary>
    ///
    /// <param name="radians">The angle in radians to add to <see cref="Rotation" />.</param>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Node2D" />
    ///
    public void Rotate(float radians)
    {
        ThrowIfFreed();
        Rotation += radians;
    }

    /// <summary>
    /// Transforms a local point into global coordinates.
    /// </summary>
    ///
    /// <param name="localPoint">The point in this node's local coordinate space.</param>
    /// <returns>The transformed point in global coordinate space.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ToLocal" />
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public Vector2 ToGlobal(Vector2 localPoint)
    {
        ThrowIfFreed();
        return GlobalTransform.Xform(localPoint);
    }

    /// <summary>
    /// Transforms a global point into this node's local coordinates.
    /// </summary>
    ///
    /// <param name="globalPoint">The point in global coordinate space.</param>
    /// <returns>The transformed point in this node's local coordinate space.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ToGlobal" />
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public Vector2 ToLocal(Vector2 globalPoint)
    {
        ThrowIfFreed();
        return GlobalTransform.AffineInverse().Xform(globalPoint);
    }

    /// <summary>
    /// Translates the node in local coordinates.
    /// </summary>
    ///
    /// <param name="offset">The local offset to add to <see cref="Position" />.</param>
    ///
    /// <remarks>
    /// This is equivalent to `Position += offset`.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <seealso cref="Node2D" />
    ///
    public void Translate(Vector2 offset)
    {
        ThrowIfFreed();
        Position += offset;
    }

    internal static Transform2D CreateTransform(float rotation, Vector2 scale, Vector2 origin)
    {
        var sine = MathF.Sin(rotation);
        var cosine = MathF.Cos(rotation);
        return new Transform2D(
            new Vector2(cosine * scale.X, sine * scale.X),
            new Vector2(-sine * scale.Y, cosine * scale.Y),
            origin);
    }

    internal static float DecomposeRotation(Transform2D transform)
    {
        if (transform.X.IsZeroApprox())
        {
            return 0f;
        }

        var x = Mathf.IsZeroApprox(transform.X.X) ? 0f : transform.X.X;
        var y = Mathf.IsZeroApprox(transform.X.Y) ? 0f : transform.X.Y;
        return new Vector2(x, y).Angle();
    }

    internal static Vector2 DecomposeScale(Transform2D transform)
    {
        var scale = new Vector2(transform.X.Length(), transform.Y.Length());
        return transform.Determinant() < 0f ? new Vector2(scale.X, -scale.Y) : scale;
    }

    private void ApplyTransform(Transform2D transform)
    {
        Position = transform.Origin;
        Rotation = DecomposeRotation(transform);
        Scale = DecomposeScale(transform);
    }
}
