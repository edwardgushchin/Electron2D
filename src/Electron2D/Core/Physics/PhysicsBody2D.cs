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
/// Provides the Godot-like base node for physical 2D bodies.
/// </summary>
///
/// <remarks>
/// `PhysicsBody2D` is the shared base for `StaticBody2D` and `RigidBody2D`.
/// It does not add behavior beyond <see cref="CollisionObject2D" /> in the
/// 0.1.0 Preview baseline.
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
public abstract class PhysicsBody2D : CollisionObject2D
{
    private PhysicsMaterial? physicsMaterialOverride;

    /// <summary>
    /// Gets or sets the physics material overriding this body's default collision material.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
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
