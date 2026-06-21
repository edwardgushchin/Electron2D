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
/// Stores parameters for <see cref="PhysicsDirectSpaceState2D.IntersectRay(PhysicsRayQueryParameters2D)" />.
/// </summary>
///
/// <threadsafety>
/// This type is not synchronized. Mutate it on the main scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
    /// <remarks>
    /// This type is part of the Electron2D 0.1.0 Preview public API.
    /// </remarks>
    ///
public sealed class PhysicsRayQueryParameters2D : RefCounted
{

    /// <summary>
    /// Initializes a new instance of the PhysicsRayQueryParameters2D type.
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
    /// <seealso cref="PhysicsRayQueryParameters2D" />
    ///
    public PhysicsRayQueryParameters2D()
    {
    }

    /// <summary>
    /// Gets or sets the ray start point in world coordinates.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current from value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsRayQueryParameters2D" />
    ///
    public Vector2 From { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the ray end point in world coordinates.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current to value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsRayQueryParameters2D" />
    ///
    public Vector2 To { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the collision layers scanned by this query.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current collision mask value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsRayQueryParameters2D" />
    ///
    public uint CollisionMask { get; set; } = uint.MaxValue;

    /// <summary>
    /// Gets or sets whether body objects are included.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current collide with bodies value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsRayQueryParameters2D" />
    ///
    public bool CollideWithBodies { get; set; } = true;

    /// <summary>
    /// Gets or sets whether area objects are included.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current collide with areas value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsRayQueryParameters2D" />
    ///
    public bool CollideWithAreas { get; set; }

    /// <summary>
    /// Gets or sets whether a ray starting inside a shape reports a hit.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current hit from inside value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsRayQueryParameters2D" />
    ///
    public bool HitFromInside { get; set; }

    /// <summary>
    /// Gets or sets RIDs excluded from the query.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current exclude value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsRayQueryParameters2D" />
    ///
    public Rid[] Exclude { get; set; } = [];
}

/// <summary>
/// Stores parameters for <see cref="PhysicsDirectSpaceState2D.IntersectPoint(PhysicsPointQueryParameters2D, int)" />.
/// </summary>
///
/// <threadsafety>
/// This type is not synchronized. Mutate it on the main scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
    /// <remarks>
    /// This type is part of the Electron2D 0.1.0 Preview public API.
    /// </remarks>
    ///
public sealed class PhysicsPointQueryParameters2D : RefCounted
{

    /// <summary>
    /// Initializes a new instance of the PhysicsPointQueryParameters2D type.
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
    /// <seealso cref="PhysicsPointQueryParameters2D" />
    ///
    public PhysicsPointQueryParameters2D()
    {
    }

    /// <summary>
    /// Gets or sets the query point in world coordinates.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current position value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsPointQueryParameters2D" />
    ///
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the collision layers scanned by this query.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current collision mask value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsPointQueryParameters2D" />
    ///
    public uint CollisionMask { get; set; } = uint.MaxValue;

    /// <summary>
    /// Gets or sets whether body objects are included.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current collide with bodies value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsPointQueryParameters2D" />
    ///
    public bool CollideWithBodies { get; set; } = true;

    /// <summary>
    /// Gets or sets whether area objects are included.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current collide with areas value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsPointQueryParameters2D" />
    ///
    public bool CollideWithAreas { get; set; }

    /// <summary>
    /// Gets or sets RIDs excluded from the query.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current exclude value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsPointQueryParameters2D" />
    ///
    public Rid[] Exclude { get; set; } = [];
}

/// <summary>
/// Stores parameters for <see cref="PhysicsDirectSpaceState2D.IntersectShape(PhysicsShapeQueryParameters2D, int)" />.
/// </summary>
///
/// <threadsafety>
/// This type is not synchronized. Mutate it on the main scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
    /// <remarks>
    /// This type is part of the Electron2D 0.1.0 Preview public API.
    /// </remarks>
    ///
public sealed class PhysicsShapeQueryParameters2D : RefCounted
{

    /// <summary>
    /// Initializes a new instance of the PhysicsShapeQueryParameters2D type.
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
    /// <seealso cref="PhysicsShapeQueryParameters2D" />
    ///
    public PhysicsShapeQueryParameters2D()
    {
    }

    /// <summary>
    /// Gets or sets the shape resource used by the query.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current shape value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsShapeQueryParameters2D" />
    ///
    public Shape2D? Shape { get; set; }

    /// <summary>
    /// Gets or sets the shape RID used by future production backends.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current shape rid value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsShapeQueryParameters2D" />
    ///
    public Rid ShapeRid { get; set; }

    /// <summary>
    /// Gets or sets the query shape transform in world coordinates.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current transform value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsShapeQueryParameters2D" />
    ///
    public Transform2D Transform { get; set; } = Transform2D.Identity;

    /// <summary>
    /// Gets or sets extra motion swept into the query bounds.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current motion value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsShapeQueryParameters2D" />
    ///
    public Vector2 Motion { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets an extra margin added around the query bounds.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current margin value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsShapeQueryParameters2D" />
    ///
    public float Margin { get; set; }

    /// <summary>
    /// Gets or sets the collision layers scanned by this query.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current collision mask value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsShapeQueryParameters2D" />
    ///
    public uint CollisionMask { get; set; } = uint.MaxValue;

    /// <summary>
    /// Gets or sets whether body objects are included.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current collide with bodies value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsShapeQueryParameters2D" />
    ///
    public bool CollideWithBodies { get; set; } = true;

    /// <summary>
    /// Gets or sets whether area objects are included.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current collide with areas value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsShapeQueryParameters2D" />
    ///
    public bool CollideWithAreas { get; set; }

    /// <summary>
    /// Gets or sets RIDs excluded from the query.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current exclude value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsShapeQueryParameters2D" />
    ///
    public Rid[] Exclude { get; set; } = [];
}
