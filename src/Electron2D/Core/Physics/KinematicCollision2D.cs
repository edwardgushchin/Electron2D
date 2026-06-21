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
/// Holds collision data returned by 2D body movement methods.
/// </summary>
///
/// <remarks>
/// <para>
/// The 0.1.0 Preview baseline stores collision information produced by the
/// managed AABB kinematic solver. It does not expose backend-specific handles.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Read it on the main scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="PhysicsBody2D.MoveAndCollide"/>
/// <seealso cref="CharacterBody2D.MoveAndSlide"/>
public class KinematicCollision2D : RefCounted
{
    internal KinematicCollision2D(
        Vector2 position,
        Vector2 normal,
        Vector2 travel,
        Vector2 remainder,
        Object? collider,
        Rid colliderRid,
        Object? colliderShape,
        int colliderShapeIndex,
        Vector2 colliderVelocity,
        Object? localShape,
        float depth)
    {
        Position = position;
        Normal = normal;
        Travel = travel;
        Remainder = remainder;
        Collider = collider;
        ColliderRid = colliderRid;
        ColliderShape = colliderShape;
        ColliderShapeIndex = colliderShapeIndex;
        ColliderVelocity = colliderVelocity;
        LocalShape = localShape;
        Depth = depth;
    }

    private Vector2 Position { get; }

    private Vector2 Normal { get; }

    private Vector2 Travel { get; }

    private Vector2 Remainder { get; }

    private Object? Collider { get; }

    private Rid ColliderRid { get; }

    private Object? ColliderShape { get; }

    private int ColliderShapeIndex { get; }

    private Vector2 ColliderVelocity { get; }

    private Object? LocalShape { get; }

    private float Depth { get; }

    /// <summary>
    /// Gets the collision angle relative to an up direction.
    /// </summary>
    ///
    /// <param name="upDirection">
    /// The up direction used as the reference vector. When omitted,
    /// <see cref="Vector2.Up" /> is used.
    /// </param>
    /// <returns>The positive angle in radians between the collision normal and the up direction.</returns>
    ///
    /// <remarks>
    /// <para>
    /// When <paramref name="upDirection"/> is <c>null</c>,
    /// <see cref="Vector2.Up"/> is used as the reference direction.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetNormal"/>
    public float GetAngle(Vector2? upDirection = null)
    {
        ThrowIfFreed();
        var up = NormalizeUpDirection(upDirection ?? Vector2.Up);
        return MathF.Acos(Mathf.Clamp(Normal.Dot(up), -1f, 1f));
    }

    /// <summary>
    /// Gets the object collided with by the moving body.
    /// </summary>
    /// <returns>The collider object, or <c>null</c> when the collision has no collider.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The current movement baseline reports <see cref="StaticBody2D"/>
    /// colliders.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetColliderRid"/>
    /// <seealso cref="GetColliderShape"/>
    public Object? GetCollider()
    {
        ThrowIfFreed();
        return Collider;
    }

    /// <summary>
    /// Gets the instance ID of the object collided with by the moving body.
    /// </summary>
    /// <returns>The collider instance ID, or <c>0</c> when the collision has no collider.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The value matches <see cref="Object.GetInstanceId"/> for the object
    /// returned by <see cref="GetCollider"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetCollider"/>
    public long GetColliderId()
    {
        ThrowIfFreed();
        return Collider is null ? 0L : (long)Collider.GetInstanceId();
    }

    /// <summary>
    /// Gets the physics server RID of the collider.
    /// </summary>
    /// <returns>The collider RID, or the empty RID when the collision has no collider.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The RID is an opaque identifier. It should be passed only to APIs that
    /// explicitly accept <see cref="Rid"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetCollider"/>
    public Rid GetColliderRid()
    {
        ThrowIfFreed();
        return ColliderRid;
    }

    /// <summary>
    /// Gets the collider shape object involved in the collision.
    /// </summary>
    /// <returns>The collider shape object, or <c>null</c> when unavailable.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The returned object is the <see cref="CollisionShape2D"/> that provided
    /// the target bounds for the sweep.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetColliderShapeIndex"/>
    public Object? GetColliderShape()
    {
        ThrowIfFreed();
        return ColliderShape;
    }

    /// <summary>
    /// Gets the collider shape index involved in the collision.
    /// </summary>
    /// <returns>The collider shape index.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The 0.1.0 Preview baseline reports the index within the collider's active
    /// collision shapes collected for the movement query.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetColliderShape"/>
    public int GetColliderShapeIndex()
    {
        ThrowIfFreed();
        return ColliderShapeIndex;
    }

    /// <summary>
    /// Gets the velocity reported for the collider.
    /// </summary>
    /// <returns>The collider velocity.</returns>
    ///
    /// <remarks>
    /// <para>
    /// For <see cref="StaticBody2D"/> colliders this value is
    /// <see cref="StaticBody2D.ConstantLinearVelocity"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="CharacterBody2D.GetPlatformVelocity"/>
    public Vector2 GetColliderVelocity()
    {
        ThrowIfFreed();
        return ColliderVelocity;
    }

    /// <summary>
    /// Gets the overlap depth along the collision normal.
    /// </summary>
    /// <returns>The overlap depth. The AABB baseline returns <c>0</c> for swept collisions.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The current movement baseline reports swept collisions rather than
    /// persistent overlap recovery, so this value is <c>0</c>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetNormal"/>
    public float GetDepth()
    {
        ThrowIfFreed();
        return Depth;
    }

    /// <summary>
    /// Gets the moving body's local shape object involved in the collision.
    /// </summary>
    /// <returns>The local shape object, or <c>null</c> when unavailable.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The returned object is the moving body's <see cref="CollisionShape2D"/>
    /// used for the sweep.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetColliderShape"/>
    public Object? GetLocalShape()
    {
        ThrowIfFreed();
        return LocalShape;
    }

    /// <summary>
    /// Gets the collision normal.
    /// </summary>
    /// <returns>The collision normal.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The normal points away from the collider and is used by
    /// <see cref="CharacterBody2D.MoveAndSlide"/> to remove the velocity
    /// component that moves into the collision.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetAngle"/>
    /// <seealso cref="GetTravel"/>
    public Vector2 GetNormal()
    {
        ThrowIfFreed();
        return Normal;
    }

    /// <summary>
    /// Gets the collision position in global coordinates.
    /// </summary>
    /// <returns>The collision position.</returns>
    ///
    /// <remarks>
    /// <para>
    /// In the managed AABB baseline this is the swept center point on the
    /// expanded bounds used to stop the moving body.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetTravel"/>
    public Vector2 GetPosition()
    {
        ThrowIfFreed();
        return Position;
    }

    /// <summary>
    /// Gets the remaining motion after the collision.
    /// </summary>
    /// <returns>The motion that remained after travel was consumed.</returns>
    ///
    /// <remarks>
    /// <para>
    /// <see cref="CharacterBody2D.MoveAndSlide"/> slides this value along the
    /// collision normal to continue movement after a hit.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetTravel"/>
    /// <seealso cref="GetNormal"/>
    public Vector2 GetRemainder()
    {
        ThrowIfFreed();
        return Remainder;
    }

    /// <summary>
    /// Gets the motion completed before the collision.
    /// </summary>
    /// <returns>The travel motion before the body stopped or slid.</returns>
    ///
    /// <remarks>
    /// <para>
    /// For non-test movement, the moving body has already been advanced by this
    /// vector when the collision is returned.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetRemainder"/>
    /// <seealso cref="PhysicsBody2D.MoveAndCollide"/>
    public Vector2 GetTravel()
    {
        ThrowIfFreed();
        return Travel;
    }

    private static Vector2 NormalizeUpDirection(Vector2 upDirection)
    {
        if (upDirection.IsZeroApprox() || !upDirection.IsFinite())
        {
            return Vector2.Up;
        }

        return upDirection.Normalized();
    }
}
