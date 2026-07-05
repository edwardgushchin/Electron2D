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
/// Provides an Electron2D rigid 2D physics body.
/// </summary>
///
/// <remarks>
/// The 0.1-preview baseline stores body properties and synchronizes the node
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
/// This type is available since Electron2D 0.1-preview.
/// </since>
public class RigidBody2D : PhysicsBody2D
{

    /// <summary>
    /// Initializes a new instance of the RigidBody2D type.
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
    /// <seealso cref="RigidBody2D" />
    ///
    public RigidBody2D()
    {
    }

    private float gravityScale = 1f;
    private bool sleeping;
    private bool canSleep = true;

    /// <summary>
    /// Identifies how a frozen rigid body should interact with the simulation.
    /// </summary>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This type is part of the Electron2D 0.1-preview public API.
    /// </remarks>
    ///
    public enum FreezeModeEnum
    {
        /// <summary>
        /// Treat the frozen body like a static body.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept FreezeModeEnum.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="FreezeModeEnum" />
        ///
        Static = 0,

        /// <summary>
        /// Treat the frozen body like a kinematic body.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept FreezeModeEnum.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="FreezeModeEnum" />
        ///
        Kinematic = 1
    }

    /// <summary>
    /// Identifies how the body's center of mass should be selected.
    /// </summary>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This type is part of the Electron2D 0.1-preview public API.
    /// </remarks>
    ///
    public enum CenterOfMassModeEnum
    {
        /// <summary>
        /// Calculate the center of mass from shapes.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept CenterOfMassModeEnum.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="CenterOfMassModeEnum" />
        ///
        Auto = 0,

        /// <summary>
        /// Use <see cref="RigidBody2D.CenterOfMass" />.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept CenterOfMassModeEnum.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="CenterOfMassModeEnum" />
        ///
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current mass value.
    /// </value>
    ///
    /// <seealso cref="RigidBody2D" />
    ///
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current inertia value.
    /// </value>
    ///
    /// <seealso cref="RigidBody2D" />
    ///
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current center of mass value.
    /// </value>
    ///
    /// <seealso cref="RigidBody2D" />
    ///
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current center of mass mode value.
    /// </value>
    ///
    /// <seealso cref="RigidBody2D" />
    ///
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current gravity scale value.
    /// </value>
    ///
    /// <seealso cref="RigidBody2D" />
    ///
    public float GravityScale
    {
        get
        {
            ThrowIfFreed();
            return gravityScale;
        }
        set
        {
            ThrowIfFreed();
            if (!float.IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(nameof(GravityScale), "GravityScale must be finite.");
            }

            gravityScale = value;
        }
    }

    /// <summary>
    /// Gets or sets the current linear velocity.
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
    /// The current linear velocity value.
    /// </value>
    ///
    /// <seealso cref="RigidBody2D" />
    ///
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current angular velocity value.
    /// </value>
    ///
    /// <seealso cref="RigidBody2D" />
    ///
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current freeze value.
    /// </value>
    ///
    /// <seealso cref="RigidBody2D" />
    ///
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current freeze mode value.
    /// </value>
    ///
    /// <seealso cref="RigidBody2D" />
    ///
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current sleeping value.
    /// </value>
    ///
    /// <seealso cref="RigidBody2D" />
    ///
    public bool Sleeping
    {
        get
        {
            ThrowIfFreed();
            return sleeping;
        }
        set
        {
            ThrowIfFreed();
            sleeping = value;
        }
    }

    /// <summary>
    /// Gets or sets whether this body is allowed to sleep.
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
    /// The current can sleep value.
    /// </value>
    ///
    /// <seealso cref="RigidBody2D" />
    ///
    public bool CanSleep
    {
        get
        {
            ThrowIfFreed();
            return canSleep;
        }
        set
        {
            ThrowIfFreed();
            canSleep = value;
        }
    }

    /// <summary>
    /// Gets or sets whether rotation is locked.
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
    /// The current lock rotation value.
    /// </value>
    ///
    /// <seealso cref="RigidBody2D" />
    ///
    public bool LockRotation { get; set; }

    protected override Rid CreatePhysicsRid()
    {
        return PhysicsServer2D.BodyCreate(PhysicsBodyKind.Rigid);
    }

    internal override void PhysicsStep(double delta)
    {
        if (Freeze || Sleeping || delta <= 0d || !double.IsFinite(delta))
        {
            return;
        }

        var step = (float)delta;
        var motion = LinearVelocity * step;
        if (!motion.IsZeroApprox())
        {
            var resolvedMotion = RigidBody2DMotion.ResolveMotion(this, motion, out var collisionNormal);
            Position += resolvedMotion;
            LinearVelocity = RemoveVelocityIntoCollision(LinearVelocity, collisionNormal);
        }

        if (!LockRotation && !Mathf.IsZeroApprox(AngularVelocity))
        {
            Rotation += AngularVelocity * step;
        }
    }

    internal override PhysicsBody2DState CapturePhysicsBodyState()
    {
        return new PhysicsBody2DState(
            PhysicsMaterialState.From(PhysicsMaterialOverride),
            new PhysicsRigidBody2DState(GravityScale, Sleeping, CanSleep));
    }

    private static Vector2 RemoveVelocityIntoCollision(Vector2 velocity, Vector2 collisionNormal)
    {
        if (collisionNormal.IsZeroApprox())
        {
            return velocity;
        }

        var result = velocity;
        if (!Mathf.IsZeroApprox(collisionNormal.X))
        {
            result.X = 0f;
        }

        if (!Mathf.IsZeroApprox(collisionNormal.Y))
        {
            result.Y = 0f;
        }

        return result;
    }
}
