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
/// Provides the base node for physical 2D bodies that participate in collision queries.
/// </summary>
///
/// <remarks>
/// <para>
/// <see cref="PhysicsBody2D"/> is the shared base for <see cref="StaticBody2D"/>,
/// <see cref="RigidBody2D"/> and <see cref="CharacterBody2D"/>. It exposes
/// body-level movement helpers without exposing backend-specific handles.
/// </para>
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
/// <seealso cref="CollisionObject2D"/>
/// <seealso cref="CharacterBody2D"/>
public abstract class PhysicsBody2D : CollisionObject2D
{
    private PhysicsMaterial? physicsMaterialOverride;

    /// <summary>
    /// Gets or sets the physics material overriding this body's default collision material.
    /// </summary>
    ///
    /// <value>
    /// The override material, or <c>null</c> to use the default body material.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The 0.1.0 Preview baseline stores this value in the internal body-state
    /// snapshot so future physics backends can combine material properties.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsMaterial"/>
    public PhysicsMaterial? PhysicsMaterialOverride
    {
        get
        {
            ThrowIfFreed();
            return physicsMaterialOverride;
        }
        set
        {
            ThrowIfFreed();
            physicsMaterialOverride = value;
        }
    }

    /// <summary>
    /// Moves this body along a motion vector and stops when it hits another body.
    /// </summary>
    ///
    /// <param name="motion">
    /// The relative motion to test and, unless <paramref name="testOnly" /> is
    /// <c>true</c>, apply to this body.
    /// </param>
    /// <param name="testOnly">
    /// When <c>true</c>, the collision is reported without changing the body position.
    /// </param>
    /// <param name="safeMargin">
    /// Extra separation margin used by the managed AABB baseline when sweeping the body.
    /// </param>
    /// <param name="recoveryAsCollision">
    /// Whether recovery movement should be reported as a collision. The 0.1.0 Preview
    /// AABB baseline stores the value for API compatibility but does not run a separate
    /// recovery phase yet.
    /// </param>
    /// <returns>
    /// A <see cref="KinematicCollision2D"/> when the movement hit another body;
    /// otherwise, <c>null</c>.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The 0.1.0 Preview baseline performs a managed AABB sweep against
    /// <see cref="StaticBody2D"/> targets whose collision layers match this
    /// body's <see cref="CollisionObject2D.CollisionMask"/>.
    /// </para>
    /// <para>
    /// When <paramref name="testOnly"/> is <c>false</c>, the body position is
    /// advanced by either the full <paramref name="motion"/> or the collision
    /// travel reported by <see cref="KinematicCollision2D.GetTravel"/>.
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
    /// <seealso cref="CharacterBody2D.MoveAndSlide"/>
    /// <seealso cref="KinematicCollision2D"/>
    public KinematicCollision2D? MoveAndCollide(
        Vector2 motion,
        bool testOnly = false,
        float safeMargin = 0.08f,
        bool recoveryAsCollision = false)
    {
        ThrowIfFreed();
        return PhysicsBody2DMotion.MoveAndCollide(this, motion, testOnly, safeMargin, recoveryAsCollision);
    }

    /// <inheritdoc />
    protected override void SynchronizePhysicsState(Rid rid)
    {
        PhysicsServer2D.BodySetState(rid, CapturePhysicsBodyState());
    }

    internal virtual PhysicsBody2DState CapturePhysicsBodyState()
    {
        return new PhysicsBody2DState(PhysicsMaterialState.From(PhysicsMaterialOverride), RigidBody: null);
    }
}
