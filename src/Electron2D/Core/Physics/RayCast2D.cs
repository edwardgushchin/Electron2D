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
/// Provides a Godot-like 2D raycast node.
/// </summary>
///
/// <remarks>
/// The 0.1.0 Preview baseline stores raycast query settings. Real physics
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
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
public class RayCast2D : Node2D
{
    /// <summary>
    /// Gets or sets whether this raycast is enabled.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
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
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
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
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
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
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
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
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
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
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
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
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
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
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public void ForceRaycastUpdate()
    {
        ThrowIfFreed();
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
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool IsColliding()
    {
        ThrowIfFreed();
        return false;
    }

    /// <summary>
    /// Gets the object hit by the raycast.
    /// </summary>
    /// <returns><c>null</c> until the query backend is implemented.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Object? GetCollider()
    {
        ThrowIfFreed();
        return null;
    }

    /// <summary>
    /// Gets the RID of the object hit by the raycast.
    /// </summary>
    /// <returns>The default empty <see cref="Rid" /> until the query backend is implemented.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Rid GetColliderRid()
    {
        ThrowIfFreed();
        return default;
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
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public int GetColliderShape()
    {
        ThrowIfFreed();
        return 0;
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
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Vector2 GetCollisionPoint()
    {
        ThrowIfFreed();
        return Vector2.Zero;
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
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Vector2 GetCollisionNormal()
    {
        ThrowIfFreed();
        return Vector2.Zero;
    }
}
