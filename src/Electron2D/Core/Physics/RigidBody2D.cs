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
/// Provides a Godot-like rigid 2D physics body.
/// </summary>
///
/// <remarks>
/// The 0.1.0 Preview baseline stores body properties and synchronizes the node
/// transform to the physics server. Dynamic simulation is added by later physics
/// tasks.
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
public class RigidBody2D : PhysicsBody2D
{
    /// <summary>
    /// Identifies how a frozen rigid body should interact with the simulation.
    /// </summary>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    public enum FreezeModeEnum
    {
        /// <summary>
        /// Treat the frozen body like a static body.
        /// </summary>
        Static = 0,

        /// <summary>
        /// Treat the frozen body like a kinematic body.
        /// </summary>
        Kinematic = 1
    }

    /// <summary>
    /// Identifies how the body's center of mass should be selected.
    /// </summary>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    public enum CenterOfMassModeEnum
    {
        /// <summary>
        /// Calculate the center of mass from shapes.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Use <see cref="RigidBody2D.CenterOfMass" />.
        /// </summary>
        Custom = 1
    }

    /// <summary>
    /// Gets or sets the body mass.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public float Mass { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the body inertia.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public float Inertia { get; set; }

    /// <summary>
    /// Gets or sets the custom center of mass.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Vector2 CenterOfMass { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets how the center of mass should be selected.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public CenterOfMassModeEnum CenterOfMassMode { get; set; } = CenterOfMassModeEnum.Auto;

    /// <summary>
    /// Gets or sets the gravity multiplier for this body.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public float GravityScale { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the current linear velocity.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Vector2 LinearVelocity { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the current angular velocity.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public float AngularVelocity { get; set; }

    /// <summary>
    /// Gets or sets whether this rigid body is frozen.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool Freeze { get; set; }

    /// <summary>
    /// Gets or sets how this body behaves when frozen.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public FreezeModeEnum FreezeMode { get; set; } = FreezeModeEnum.Static;

    /// <summary>
    /// Gets or sets whether the body is sleeping.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool Sleeping { get; set; }

    /// <summary>
    /// Gets or sets whether this body is allowed to sleep.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool CanSleep { get; set; } = true;

    /// <summary>
    /// Gets or sets whether rotation is locked.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool LockRotation { get; set; }

    protected override Rid CreatePhysicsRid()
    {
        return PhysicsServer2D.BodyCreate(PhysicsBodyKind.Rigid);
    }
}
