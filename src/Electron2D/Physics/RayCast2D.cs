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
/// Provides an Electron2D 2D raycast node.
/// </summary>
///
/// <remarks>
/// The 0.1-preview baseline stores raycast query settings. Real physics
/// query execution is introduced by the later raycast and shape query task, so
/// result methods return an empty collision state.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate nodes on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
public class RayCast2D : Node2D, ISceneTreeLifecycleHandler
{

    /// <summary>
    /// Initializes a new instance of the RayCast2D type.
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
    /// <seealso cref="RayCast2D" />
    ///
    public RayCast2D()
    {
    }

    private bool colliding;
    private ElectronObject? collider;
    private Rid colliderRid;
    private int colliderShape;
    private Vector2 collisionPoint;
    private Vector2 collisionNormal;

    /// <summary>
    /// Gets or sets whether this raycast is enabled.
    /// </summary>
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
    /// <value>
    /// The current enabled value.
    /// </value>
    ///
    /// <seealso cref="RayCast2D" />
    ///
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the ray target position in the node's local coordinates.
    /// </summary>
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
    /// <value>
    /// The current target position value.
    /// </value>
    ///
    /// <seealso cref="RayCast2D" />
    ///
    public Vector2 TargetPosition { get; set; } = new(0f, 50f);

    /// <summary>
    /// Gets or sets whether the direct parent should be excluded from results.
    /// </summary>
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
    /// <value>
    /// The current exclude parent value.
    /// </value>
    ///
    /// <seealso cref="RayCast2D" />
    ///
    public bool ExcludeParent { get; set; } = true;

    /// <summary>
    /// Gets or sets whether hits are allowed when the ray starts inside a shape.
    /// </summary>
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
    /// <value>
    /// The current hit from inside value.
    /// </value>
    ///
    /// <seealso cref="RayCast2D" />
    ///
    public bool HitFromInside { get; set; }

    /// <summary>
    /// Gets or sets whether areas should be considered by the raycast.
    /// </summary>
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
    /// <value>
    /// The current collide with areas value.
    /// </value>
    ///
    /// <seealso cref="RayCast2D" />
    ///
    public bool CollideWithAreas { get; set; }

    /// <summary>
    /// Gets or sets whether bodies should be considered by the raycast.
    /// </summary>
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
    /// <value>
    /// The current collide with bodies value.
    /// </value>
    ///
    /// <seealso cref="RayCast2D" />
    ///
    public bool CollideWithBodies { get; set; } = true;

    /// <summary>
    /// Gets or sets the collision mask used by the raycast.
    /// </summary>
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
    /// <value>
    /// The current collision mask value.
    /// </value>
    ///
    /// <seealso cref="RayCast2D" />
    ///
    public uint CollisionMask { get; set; } = 1u;

    /// <summary>
    /// Updates the cached raycast result immediately.
    /// </summary>
    ///
    /// <remarks>
    /// Real query execution is not part of the current baseline, so this method
    /// keeps the result in an empty state.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <seealso cref="RayCast2D" />
    ///
    public void ForceRaycastUpdate()
    {
        ThrowIfFreed();
        if (!Enabled || !IsInsideTree())
        {
            ClearResult();
            return;
        }

        var query = new PhysicsRayQueryParameters2D
        {
            From = GlobalPosition,
            To = ToGlobal(TargetPosition),
            CollisionMask = CollisionMask,
            CollideWithBodies = CollideWithBodies,
            CollideWithAreas = CollideWithAreas,
            HitFromInside = HitFromInside,
            Exclude = CreateExcludeList()
        };

        ApplyResult(GetWorld2D().DirectSpaceState.IntersectRay(query));
    }

    /// <summary>
    /// Checks whether the raycast currently collides with an object.
    /// </summary>
    /// <returns><c>false</c> until the query backend is implemented.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="RayCast2D" />
    ///
    public bool IsColliding()
    {
        ThrowIfFreed();
        return colliding;
    }

    /// <summary>
    /// Gets the ElectronObject hit by the raycast.
    /// </summary>
    /// <returns><c>null</c> until the query backend is implemented.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="RayCast2D" />
    ///
    public ElectronObject? GetCollider()
    {
        ThrowIfFreed();
        return collider;
    }

    /// <summary>
    /// Gets the RID of the ElectronObject hit by the raycast.
    /// </summary>
    /// <returns>The default empty <see cref="Rid" /> until the query backend is implemented.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="RayCast2D" />
    ///
    public Rid GetColliderRid()
    {
        ThrowIfFreed();
        return colliderRid;
    }

    /// <summary>
    /// Gets the shape index hit by the raycast.
    /// </summary>
    /// <returns><c>0</c> until the query backend is implemented.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="RayCast2D" />
    ///
    public int GetColliderShape()
    {
        ThrowIfFreed();
        return colliderShape;
    }

    /// <summary>
    /// Gets the collision point reported by the raycast.
    /// </summary>
    /// <returns><see cref="Vector2.Zero" /> until the query backend is implemented.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="RayCast2D" />
    ///
    public Vector2 GetCollisionPoint()
    {
        ThrowIfFreed();
        return collisionPoint;
    }

    /// <summary>
    /// Gets the collision normal reported by the raycast.
    /// </summary>
    /// <returns><see cref="Vector2.Zero" /> until the query backend is implemented.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="RayCast2D" />
    ///
    public Vector2 GetCollisionNormal()
    {
        ThrowIfFreed();
        return collisionNormal;
    }

    void ISceneTreeLifecycleHandler.OnEnterTree()
    {
    }

    void ISceneTreeLifecycleHandler.OnPhysicsProcess(double delta)
    {
        _ = delta;
        if (Enabled)
        {
            ForceRaycastUpdate();
            return;
        }

        ClearResult();
    }

    void ISceneTreeLifecycleHandler.OnExitTree()
    {
        ClearResult();
    }

    private Rid[] CreateExcludeList()
    {
        if (!ExcludeParent || GetParent() is not CollisionObject2D parent)
        {
            return [];
        }

        var parentRid = parent.GetRid();
        return parentRid.IsValid() ? [parentRid] : [];
    }

    private void ApplyResult(Collections.Dictionary result)
    {
        if (result.Count == 0)
        {
            ClearResult();
            return;
        }

        colliding = true;
        collider = result[Variant.CreateFrom("collider")].Obj as ElectronObject;
        colliderRid = result[Variant.CreateFrom("rid")].Obj is Rid rid ? rid : default;
        colliderShape = result[Variant.CreateFrom("shape")].Obj is long shapeIndex ? checked((int)shapeIndex) : 0;
        collisionPoint = result[Variant.CreateFrom("position")].Obj is Vector2 point ? point : Vector2.Zero;
        collisionNormal = result[Variant.CreateFrom("normal")].Obj is Vector2 normal ? normal : Vector2.Zero;
    }

    private void ClearResult()
    {
        colliding = false;
        collider = null;
        colliderRid = default;
        colliderShape = 0;
        collisionPoint = Vector2.Zero;
        collisionNormal = Vector2.Zero;
    }
}
