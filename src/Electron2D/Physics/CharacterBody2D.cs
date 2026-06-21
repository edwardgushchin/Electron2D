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
/// Provides a 2D character body moved directly by user code.
/// </summary>
///
/// <remarks>
/// <para>
/// <see cref="CharacterBody2D"/> is not moved by forces in the 0.1.0 Preview
/// baseline. Assign <see cref="Velocity"/> in <see cref="Node._PhysicsProcess"/>
/// and call <see cref="MoveAndSlide"/> to move with managed AABB collision
/// response.
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
/// <seealso cref="PhysicsBody2D"/>
/// <seealso cref="KinematicCollision2D"/>
public class CharacterBody2D : PhysicsBody2D
{
    private const double FixedDelta = 1d / 60d;

    private readonly List<KinematicCollision2D> slideCollisions = new();
    private Vector2 upDirection = Vector2.Up;
    private float floorSnapLength = 1f;
    private float floorMaxAngle = Mathf.Pi / 4f;
    private int maxSlides = 4;
    private float safeMargin = 0.08f;
    private float wallMinSlideAngle = Mathf.Pi / 12f;
    private Vector2 floorNormal = Vector2.Zero;
    private Vector2 wallNormal = Vector2.Zero;
    private Vector2 lastMotion = Vector2.Zero;
    private Vector2 positionDelta = Vector2.Zero;
    private Vector2 realVelocity = Vector2.Zero;
    private Vector2 platformVelocity = Vector2.Zero;
    private bool onFloor;
    private bool onWall;
    private bool onCeiling;

    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterBody2D" /> class.
    /// </summary>
    ///
    /// <remarks>
    /// New instances start with zero velocity, upward floor classification and
    /// the managed axis-aligned bounding-box movement baseline.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Create nodes on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <seealso cref="CharacterBody2D" />
    ///
    public CharacterBody2D()
    {
    }

    /// <summary>
    /// Identifies how collisions are classified during slide movement.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The mode changes how <see cref="MoveAndSlide"/> interprets collision
    /// normals when updating <see cref="IsOnFloor"/>, <see cref="IsOnWall"/>
    /// and <see cref="IsOnCeiling"/>.
    /// </para>
    /// </remarks>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="MotionMode"/>
    public enum MotionModeEnum
    {
        /// <summary>
        /// Classifies collisions as floor, wall or ceiling using <see cref="UpDirection"/>.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// Use this mode for platformers and other grounded characters.
        /// </para>
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="MotionModeEnum" />
        ///
        Grounded = 0,

        /// <summary>
        /// Classifies all slide collisions as walls.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// Use this mode for top-down movement where floor and ceiling
        /// classification should be ignored.
        /// </para>
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="MotionModeEnum" />
        ///
        Floating = 1
    }

    /// <summary>
    /// Identifies how platform velocity should be applied when leaving a platform.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The 0.1.0 Preview baseline exposes this policy as part of the public
    /// movement contract. Complex leave behavior is reserved for a later solver
    /// pass.
    /// </para>
    /// </remarks>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PlatformOnLeave"/>
    public enum PlatformOnLeaveEnum
    {
        /// <summary>
        /// Add the last platform velocity on leave.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This is the default policy.
        /// </para>
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="PlatformOnLeaveEnum" />
        ///
        AddVelocity = 0,

        /// <summary>
        /// Add upward platform velocity while ignoring downward platform velocity.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This policy is intended for moving platforms that should not pull the
        /// character downward after contact ends.
        /// </para>
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="PlatformOnLeaveEnum" />
        ///
        AddUpwardVelocity = 1,

        /// <summary>
        /// Do not add platform velocity on leave.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// Use this policy when platform velocity should only be reported
        /// through <see cref="GetPlatformVelocity"/>.
        /// </para>
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="PlatformOnLeaveEnum" />
        ///
        DoNothing = 2
    }

    /// <summary>
    /// Gets or sets the velocity used by <see cref="MoveAndSlide"/>.
    /// </summary>
    ///
    /// <value>
    /// The velocity in units per second.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// <see cref="MoveAndSlide"/> consumes this value using the fixed physics
    /// tick of <c>1/60</c> seconds and updates it after slide collisions.
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
    /// <seealso cref="MoveAndSlide"/>
    /// <seealso cref="GetRealVelocity"/>
    public Vector2 Velocity { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the up direction used to classify floor, wall and ceiling collisions.
    /// </summary>
    ///
    /// <value>
    /// A finite non-zero vector. The setter stores the normalized direction.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Floor, wall and ceiling classification compares collision normals against
    /// this direction and <see cref="FloorMaxAngle"/>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is zero or contains non-finite components.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="FloorMaxAngle"/>
    /// <seealso cref="GetFloorAngle"/>
    public Vector2 UpDirection
    {
        get
        {
            ThrowIfFreed();
            return upDirection;
        }
        set
        {
            ThrowIfFreed();
            if (value.IsZeroApprox() || !value.IsFinite())
            {
                throw new ArgumentOutOfRangeException(nameof(UpDirection), "UpDirection must be finite and non-zero.");
            }

            upDirection = value.Normalized();
        }
    }

    /// <summary>
    /// Gets or sets the floor snapping distance used by <see cref="MoveAndSlide"/>.
    /// </summary>
    ///
    /// <value>
    /// The non-negative snap distance in world units.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// When the body is not moving upward and no floor collision happened during
    /// the main slide pass, <see cref="MoveAndSlide"/> attempts a snap movement
    /// opposite <see cref="UpDirection"/>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is negative or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ApplyFloorSnap"/>
    /// <seealso cref="IsOnFloor"/>
    public float FloorSnapLength
    {
        get
        {
            ThrowIfFreed();
            return floorSnapLength;
        }
        set
        {
            ThrowIfFreed();
            floorSnapLength = ValidateNonNegativeFinite(value, nameof(FloorSnapLength));
        }
    }

    /// <summary>
    /// Gets or sets the maximum angle in radians that still counts as floor or ceiling.
    /// </summary>
    ///
    /// <value>
    /// The non-negative angle in radians.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Collision normals whose angle from <see cref="UpDirection"/> is less than
    /// or equal to this value are treated as floor. The opposite direction is
    /// treated as ceiling.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is negative or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetFloorAngle"/>
    /// <seealso cref="UpDirection"/>
    public float FloorMaxAngle
    {
        get
        {
            ThrowIfFreed();
            return floorMaxAngle;
        }
        set
        {
            ThrowIfFreed();
            floorMaxAngle = ValidateNonNegativeFinite(value, nameof(FloorMaxAngle));
        }
    }

    /// <summary>
    /// Gets or sets whether a standing body should stop on slopes.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> when slope stop behavior is requested; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The 0.1.0 Preview baseline stores this setting for API completeness. The
    /// simple AABB solver does not yet implement full slope stop behavior.
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
    /// <seealso cref="FloorConstantSpeed"/>
    public bool FloorStopOnSlope { get; set; } = true;

    /// <summary>
    /// Gets or sets whether floor movement should preserve constant speed on slopes.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> when constant floor speed behavior is requested; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The 0.1.0 Preview baseline stores this setting for API completeness. The
    /// simple AABB solver does not yet implement full constant-speed slope
    /// movement.
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
    /// <seealso cref="FloorStopOnSlope"/>
    public bool FloorConstantSpeed { get; set; }

    /// <summary>
    /// Gets or sets whether floor movement should avoid walking on walls.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> when wall blocking is requested for floor movement; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The 0.1.0 Preview baseline stores this setting for API completeness. Wall
    /// classification still uses <see cref="FloorMaxAngle"/> and
    /// <see cref="UpDirection"/>.
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
    /// <seealso cref="IsOnWall"/>
    public bool FloorBlockOnWall { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the body should slide along ceilings.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to allow ceiling slide response; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// When this property is <c>false</c>, a ceiling collision clears the
    /// vertical component of <see cref="Velocity"/> in the current baseline.
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
    /// <seealso cref="IsOnCeiling"/>
    public bool SlideOnCeiling { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of slide iterations for <see cref="MoveAndSlide"/>.
    /// </summary>
    ///
    /// <value>
    /// A positive number of slide iterations.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Each iteration consumes the remaining motion after a collision by sliding
    /// it along the collision normal.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is less than or equal to zero.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="MoveAndSlide"/>
    public int MaxSlides
    {
        get
        {
            ThrowIfFreed();
            return maxSlides;
        }
        set
        {
            ThrowIfFreed();
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(MaxSlides), "MaxSlides must be greater than zero.");
            }

            maxSlides = value;
        }
    }

    /// <summary>
    /// Gets or sets the motion mode used by <see cref="MoveAndSlide"/>.
    /// </summary>
    ///
    /// <value>
    /// The current <see cref="MotionModeEnum"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// <see cref="MotionModeEnum.Grounded"/> classifies collisions into floor,
    /// wall and ceiling. <see cref="MotionModeEnum.Floating"/> treats slide
    /// collisions as walls.
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
    /// <seealso cref="IsOnFloor"/>
    /// <seealso cref="IsOnWall"/>
    public MotionModeEnum MotionMode { get; set; } = MotionModeEnum.Grounded;

    /// <summary>
    /// Gets or sets the safe margin used by <see cref="MoveAndSlide"/>.
    /// </summary>
    ///
    /// <value>
    /// The non-negative collision margin in world units.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The managed AABB solver expands target bounds by this margin before
    /// sweeping the body.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is negative or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsBody2D.MoveAndCollide"/>
    public float SafeMargin
    {
        get
        {
            ThrowIfFreed();
            return safeMargin;
        }
        set
        {
            ThrowIfFreed();
            safeMargin = ValidateNonNegativeFinite(value, nameof(SafeMargin));
        }
    }

    /// <summary>
    /// Gets or sets the minimum slide angle for walls.
    /// </summary>
    ///
    /// <value>
    /// The non-negative angle in radians.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The 0.1.0 Preview baseline stores this setting for API completeness. The
    /// simple AABB solver does not yet implement wall minimum slide angle
    /// filtering.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is negative or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="IsOnWall"/>
    public float WallMinSlideAngle
    {
        get
        {
            ThrowIfFreed();
            return wallMinSlideAngle;
        }
        set
        {
            ThrowIfFreed();
            wallMinSlideAngle = ValidateNonNegativeFinite(value, nameof(WallMinSlideAngle));
        }
    }

    /// <summary>
    /// Gets or sets the layers treated as moving floor platforms.
    /// </summary>
    ///
    /// <value>
    /// A bit mask matched against <see cref="CollisionObject2D.CollisionLayer"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// When a floor collision occurs with a <see cref="StaticBody2D"/> whose
    /// collision layer is included in this mask, <see cref="GetPlatformVelocity"/>
    /// returns that body's <see cref="StaticBody2D.ConstantLinearVelocity"/>.
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
    /// <seealso cref="GetPlatformVelocity"/>
    /// <seealso cref="PlatformWallLayers"/>
    public uint PlatformFloorLayers { get; set; } = uint.MaxValue;

    /// <summary>
    /// Gets or sets the layers treated as moving wall platforms.
    /// </summary>
    ///
    /// <value>
    /// A bit mask matched against <see cref="CollisionObject2D.CollisionLayer"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// When a wall collision occurs and the collider layer is included in this
    /// mask, <see cref="GetPlatformVelocity"/> returns the collider velocity.
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
    /// <seealso cref="GetPlatformVelocity"/>
    /// <seealso cref="PlatformFloorLayers"/>
    public uint PlatformWallLayers { get; set; }

    /// <summary>
    /// Gets or sets how platform velocity should be handled when leaving a platform.
    /// </summary>
    ///
    /// <value>
    /// The selected <see cref="PlatformOnLeaveEnum"/> policy.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The 0.1.0 Preview baseline stores this setting for API completeness.
    /// Complex platform leave velocity application is reserved for a later
    /// solver pass.
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
    /// <seealso cref="GetPlatformVelocity"/>
    public PlatformOnLeaveEnum PlatformOnLeave { get; set; } = PlatformOnLeaveEnum.AddVelocity;

    /// <summary>
    /// Applies floor snapping immediately using <see cref="FloorSnapLength"/>.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// This method moves the body opposite <see cref="UpDirection"/> by
    /// <see cref="FloorSnapLength"/> when a floor collision is found. It is
    /// useful when code changes velocity or position before the next
    /// <see cref="MoveAndSlide"/> call.
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
    /// <seealso cref="FloorSnapLength"/>
    /// <seealso cref="MoveAndSlide"/>
    public void ApplyFloorSnap()
    {
        ThrowIfFreed();
        ApplySnap();
    }

    /// <summary>
    /// Moves the body using <see cref="Velocity"/> and slides along collisions.
    /// </summary>
    /// <returns><c>true</c> when at least one collision happened; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The method consumes <see cref="Velocity"/> over the fixed physics tick,
    /// applies at most <see cref="MaxSlides"/> slide iterations, updates
    /// floor/wall/ceiling state and stores collision data for
    /// <see cref="GetSlideCollision"/>.
    /// </para>
    /// <para>
    /// The 0.1.0 Preview baseline uses managed AABB sweeps against
    /// <see cref="StaticBody2D"/> nodes. It is a gameplay baseline, not a final
    /// narrow-phase solver.
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
    /// <seealso cref="Velocity"/>
    /// <seealso cref="PhysicsBody2D.MoveAndCollide"/>
    /// <seealso cref="GetSlideCollision"/>
    public bool MoveAndSlide()
    {
        ThrowIfFreed();
        ResetSlideState();

        var startPosition = Position;
        var remainingMotion = Velocity * (float)FixedDelta;
        for (var slide = 0; slide < MaxSlides && !remainingMotion.IsZeroApprox(); slide++)
        {
            var collision = MoveAndCollide(remainingMotion, testOnly: false, SafeMargin);
            if (collision is null)
            {
                lastMotion += remainingMotion;
                break;
            }

            slideCollisions.Add(collision);
            RegisterCollision(collision);
            lastMotion += collision.GetTravel();
            Velocity = Slide(Velocity, collision.GetNormal());
            remainingMotion = Slide(collision.GetRemainder(), collision.GetNormal());
        }

        if (!onFloor && ShouldApplyFloorSnap())
        {
            ApplySnap();
        }

        positionDelta = Position - startPosition;
        realVelocity = positionDelta / (float)FixedDelta;
        return slideCollisions.Count > 0;
    }

    /// <summary>
    /// Gets the angle of the last floor collision.
    /// </summary>
    /// <param name="upDirection">Optional up direction used as reference.</param>
    /// <returns>The floor angle in radians, or <c>0</c> when no floor collision exists.</returns>
    ///
    /// <remarks>
    /// <para>
    /// When <paramref name="upDirection"/> is <c>null</c>, the current
    /// <see cref="UpDirection"/> is used. If the body is not on a floor after the
    /// last <see cref="MoveAndSlide"/> call, the method returns <c>0</c>.
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
    /// <seealso cref="GetFloorNormal"/>
    /// <seealso cref="IsOnFloor"/>
    public float GetFloorAngle(Vector2? upDirection = null)
    {
        ThrowIfFreed();
        if (!onFloor)
        {
            return 0f;
        }

        var up = NormalizeUpDirection(upDirection ?? UpDirection);
        return MathF.Acos(Mathf.Clamp(floorNormal.Dot(up), -1f, 1f));
    }

    /// <summary>
    /// Gets the normal of the last floor collision.
    /// </summary>
    /// <returns>The floor normal, or <see cref="Vector2.Zero"/> when not on floor.</returns>
    ///
    /// <remarks>
    /// <para>
    /// This value is reset at the start of each <see cref="MoveAndSlide"/> call.
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
    /// <seealso cref="GetFloorAngle"/>
    /// <seealso cref="IsOnFloor"/>
    public Vector2 GetFloorNormal()
    {
        ThrowIfFreed();
        return floorNormal;
    }

    /// <summary>
    /// Gets the last motion applied by <see cref="MoveAndSlide"/>.
    /// </summary>
    /// <returns>The last slide motion.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The value is reset at the start of each <see cref="MoveAndSlide"/> call
    /// and accumulates the travel applied by slide and snap movement.
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
    /// <seealso cref="GetPositionDelta"/>
    public Vector2 GetLastMotion()
    {
        ThrowIfFreed();
        return lastMotion;
    }

    /// <summary>
    /// Gets the last slide collision.
    /// </summary>
    /// <returns>The last slide collision, or <c>null</c> when there are no slide collisions.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The returned collision object belongs to the data collected during the
    /// most recent <see cref="MoveAndSlide"/> call.
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
    /// <seealso cref="GetSlideCollision"/>
    /// <seealso cref="GetSlideCollisionCount"/>
    public KinematicCollision2D? GetLastSlideCollision()
    {
        ThrowIfFreed();
        return slideCollisions.Count == 0 ? null : slideCollisions[^1];
    }

    /// <summary>
    /// Gets the velocity of the platform currently treated as floor or wall.
    /// </summary>
    /// <returns>The platform velocity, or <see cref="Vector2.Zero"/> when none is tracked.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The baseline reads <see cref="StaticBody2D.ConstantLinearVelocity"/> from
    /// floor or wall colliders whose layer is included in the relevant platform
    /// mask.
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
    /// <seealso cref="PlatformFloorLayers"/>
    /// <seealso cref="PlatformWallLayers"/>
    public Vector2 GetPlatformVelocity()
    {
        ThrowIfFreed();
        return platformVelocity;
    }

    /// <summary>
    /// Gets the position delta applied by the last <see cref="MoveAndSlide"/> call.
    /// </summary>
    /// <returns>The last position delta.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The value is the difference between <see cref="Node2D.Position"/> before
    /// and after the last slide call.
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
    /// <seealso cref="GetRealVelocity"/>
    /// <seealso cref="GetLastMotion"/>
    public Vector2 GetPositionDelta()
    {
        ThrowIfFreed();
        return positionDelta;
    }

    /// <summary>
    /// Gets the effective velocity from the last applied position delta.
    /// </summary>
    /// <returns>The last real velocity.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The value is <see cref="GetPositionDelta"/> divided by the fixed physics
    /// tick used by the 0.1.0 Preview baseline.
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
    /// <seealso cref="Velocity"/>
    /// <seealso cref="GetPositionDelta"/>
    public Vector2 GetRealVelocity()
    {
        ThrowIfFreed();
        return realVelocity;
    }

    /// <summary>
    /// Gets a slide collision by index.
    /// </summary>
    /// <param name="slideIdx">The zero-based slide collision index.</param>
    /// <returns>The slide collision, or <c>null</c> when the index is outside the current range.</returns>
    ///
    /// <remarks>
    /// <para>
    /// Valid indices are in the range <c>0</c> through
    /// <c>GetSlideCollisionCount() - 1</c>. Out-of-range indices return
    /// <c>null</c> instead of throwing.
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
    /// <seealso cref="GetSlideCollisionCount"/>
    /// <seealso cref="GetLastSlideCollision"/>
    public KinematicCollision2D? GetSlideCollision(int slideIdx)
    {
        ThrowIfFreed();
        return slideIdx < 0 || slideIdx >= slideCollisions.Count ? null : slideCollisions[slideIdx];
    }

    /// <summary>
    /// Gets the number of slide collisions from the last <see cref="MoveAndSlide"/> call.
    /// </summary>
    /// <returns>The slide collision count.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The count is reset at the start of each <see cref="MoveAndSlide"/> call.
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
    /// <seealso cref="GetSlideCollision"/>
    public int GetSlideCollisionCount()
    {
        ThrowIfFreed();
        return slideCollisions.Count;
    }

    /// <summary>
    /// Gets the normal of the last wall collision.
    /// </summary>
    /// <returns>The wall normal, or <see cref="Vector2.Zero"/> when not on wall.</returns>
    ///
    /// <remarks>
    /// <para>
    /// This value is reset at the start of each <see cref="MoveAndSlide"/> call.
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
    /// <seealso cref="IsOnWall"/>
    public Vector2 GetWallNormal()
    {
        ThrowIfFreed();
        return wallNormal;
    }

    /// <summary>
    /// Checks whether the last slide movement touched a ceiling.
    /// </summary>
    /// <returns><c>true</c> when on ceiling; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The value describes the most recent <see cref="MoveAndSlide"/> call and
    /// is reset before the next slide pass.
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
    /// <seealso cref="IsOnCeilingOnly"/>
    /// <seealso cref="SlideOnCeiling"/>
    public bool IsOnCeiling()
    {
        ThrowIfFreed();
        return onCeiling;
    }

    /// <summary>
    /// Checks whether the last slide movement touched only a ceiling.
    /// </summary>
    /// <returns><c>true</c> when only ceiling state is active; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>
    /// <para>
    /// This method returns <c>false</c> if the last slide movement also touched
    /// a floor or wall.
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
    /// <seealso cref="IsOnCeiling"/>
    public bool IsOnCeilingOnly()
    {
        ThrowIfFreed();
        return onCeiling && !onFloor && !onWall;
    }

    /// <summary>
    /// Checks whether the last slide movement touched a floor.
    /// </summary>
    /// <returns><c>true</c> when on floor; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>
    /// <para>
    /// Floor state is classified with <see cref="UpDirection"/> and
    /// <see cref="FloorMaxAngle"/> when <see cref="MotionMode"/> is
    /// <see cref="MotionModeEnum.Grounded"/>.
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
    /// <seealso cref="IsOnFloorOnly"/>
    /// <seealso cref="GetFloorNormal"/>
    public bool IsOnFloor()
    {
        ThrowIfFreed();
        return onFloor;
    }

    /// <summary>
    /// Checks whether the last slide movement touched only a floor.
    /// </summary>
    /// <returns><c>true</c> when only floor state is active; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>
    /// <para>
    /// This method returns <c>false</c> if the last slide movement also touched
    /// a wall or ceiling.
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
    /// <seealso cref="IsOnFloor"/>
    public bool IsOnFloorOnly()
    {
        ThrowIfFreed();
        return onFloor && !onWall && !onCeiling;
    }

    /// <summary>
    /// Checks whether the last slide movement touched a wall.
    /// </summary>
    /// <returns><c>true</c> when on wall; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>
    /// <para>
    /// Wall state is set for slide collisions that are neither floor nor
    /// ceiling in grounded mode, and for all slide collisions in floating mode.
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
    /// <seealso cref="IsOnWallOnly"/>
    /// <seealso cref="GetWallNormal"/>
    public bool IsOnWall()
    {
        ThrowIfFreed();
        return onWall;
    }

    /// <summary>
    /// Checks whether the last slide movement touched only a wall.
    /// </summary>
    /// <returns><c>true</c> when only wall state is active; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>
    /// <para>
    /// This method returns <c>false</c> if the last slide movement also touched
    /// a floor or ceiling.
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
    /// <seealso cref="IsOnWall"/>
    public bool IsOnWallOnly()
    {
        ThrowIfFreed();
        return onWall && !onFloor && !onCeiling;
    }

    protected override Rid CreatePhysicsRid()
    {
        return PhysicsServer2D.BodyCreate(PhysicsBodyKind.Character);
    }

    private void ResetSlideState()
    {
        slideCollisions.Clear();
        floorNormal = Vector2.Zero;
        wallNormal = Vector2.Zero;
        lastMotion = Vector2.Zero;
        positionDelta = Vector2.Zero;
        realVelocity = Vector2.Zero;
        platformVelocity = Vector2.Zero;
        onFloor = false;
        onWall = false;
        onCeiling = false;
    }

    private bool ShouldApplyFloorSnap()
    {
        return FloorSnapLength > 0f && Velocity.Dot(UpDirection) <= 0f;
    }

    private void ApplySnap()
    {
        if (FloorSnapLength <= 0f)
        {
            return;
        }

        var snapMotion = -UpDirection * FloorSnapLength;
        var collision = MoveAndCollide(snapMotion, testOnly: false, SafeMargin, recoveryAsCollision: true);
        if (collision is null)
        {
            return;
        }

        lastMotion += collision.GetTravel();
        slideCollisions.Add(collision);
        RegisterCollision(collision);
    }

    private void RegisterCollision(KinematicCollision2D collision)
    {
        var normal = collision.GetNormal();
        if (MotionMode == MotionModeEnum.Floating)
        {
            onWall = true;
            wallNormal = normal;
            UpdatePlatformVelocity(collision, PlatformWallLayers);
            return;
        }

        var upDot = normal.Dot(UpDirection);
        var floorThreshold = MathF.Cos(FloorMaxAngle);
        if (upDot >= floorThreshold)
        {
            onFloor = true;
            floorNormal = normal;
            UpdatePlatformVelocity(collision, PlatformFloorLayers);
            return;
        }

        if (upDot <= -floorThreshold)
        {
            onCeiling = true;
            if (!SlideOnCeiling)
            {
                Velocity = new Vector2(Velocity.X, 0f);
            }

            return;
        }

        onWall = true;
        wallNormal = normal;
        UpdatePlatformVelocity(collision, PlatformWallLayers);
    }

    private void UpdatePlatformVelocity(KinematicCollision2D collision, uint layerMask)
    {
        if (collision.GetCollider() is not CollisionObject2D collisionObject ||
            (layerMask & collisionObject.CollisionLayer) == 0u)
        {
            return;
        }

        platformVelocity = collision.GetColliderVelocity();
    }

    private static Vector2 Slide(Vector2 vector, Vector2 normal)
    {
        return vector - (normal * vector.Dot(normal));
    }

    private static float ValidateNonNegativeFinite(float value, string parameterName)
    {
        if (value < 0f || !float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, $"{parameterName} must be finite and non-negative.");
        }

        return value;
    }

    private static Vector2 NormalizeUpDirection(Vector2 direction)
    {
        return direction.IsZeroApprox() || !direction.IsFinite() ? Vector2.Up : direction.Normalized();
    }
}
