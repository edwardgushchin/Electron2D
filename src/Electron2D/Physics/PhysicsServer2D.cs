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
/// Provides the Electron2D server boundary for low-level 2D physics objects.
/// </summary>
///
/// <remarks>
/// <para>
/// `PhysicsServer2D` creates and manipulates physics resources through opaque
/// <see cref="Rid"/> values. The public API does not expose Box2D.NET or any
/// other concrete backend handle.
/// </para>
///
/// <para>
/// Electron2D 0.1-preview implements the server resource boundary only.
/// Physical bodies, shape data, contacts and queries are added by later physics
/// tasks.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Calls are serialized through an internal lock. Backend replacement is an
/// internal startup/test operation and should not be performed while physics is
/// stepping.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
public static class PhysicsServer2D
{
    private static readonly object BackendLock = new();
    private static IPhysicsServer2DBackend backend = new ManagedPhysicsServer2DBackend();

    /// <summary>
    /// Identifies a configurable parameter of a physics space.
    /// </summary>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This type is part of the Electron2D 0.1-preview public API.
    /// </remarks>
    ///
    public enum SpaceParameter
    {
        /// <summary>
        /// Contact recycle radius.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept SpaceParameter.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="SpaceParameter" />
        ///
        ContactRecycleRadius = 0,

        /// <summary>
        /// Maximum separation still considered for contact persistence.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept SpaceParameter.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="SpaceParameter" />
        ///
        ContactMaxSeparation = 1,

        /// <summary>
        /// Maximum allowed penetration before solver correction.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept SpaceParameter.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="SpaceParameter" />
        ///
        ContactMaxAllowedPenetration = 2,

        /// <summary>
        /// Default contact solver bias.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept SpaceParameter.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="SpaceParameter" />
        ///
        ContactDefaultBias = 3,

        /// <summary>
        /// Linear velocity threshold below which a body can sleep.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept SpaceParameter.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="SpaceParameter" />
        ///
        BodyLinearVelocitySleepThreshold = 4,

        /// <summary>
        /// Angular velocity threshold below which a body can sleep.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept SpaceParameter.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="SpaceParameter" />
        ///
        BodyAngularVelocitySleepThreshold = 5,

        /// <summary>
        /// Time a low-activity body must remain below thresholds before sleeping.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept SpaceParameter.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="SpaceParameter" />
        ///
        BodyTimeToSleep = 6,

        /// <summary>
        /// Default constraint solver bias.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept SpaceParameter.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="SpaceParameter" />
        ///
        ConstraintDefaultBias = 7,

        /// <summary>
        /// Number of solver iterations used by the space.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept SpaceParameter.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="SpaceParameter" />
        ///
        SolverIterations = 8
    }

    /// <summary>
    /// Identifies the shape resource kind stored by the physics server.
    /// </summary>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This type is part of the Electron2D 0.1-preview public API.
    /// </remarks>
    ///
    public enum ShapeType
    {
        /// <summary>
        /// Infinite world boundary line shape.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ShapeType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="ShapeType" />
        ///
        WorldBoundary = 0,

        /// <summary>
        /// Separation ray shape used by character-style controllers.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ShapeType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="ShapeType" />
        ///
        SeparationRay = 1,

        /// <summary>
        /// Finite segment shape.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ShapeType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="ShapeType" />
        ///
        Segment = 2,

        /// <summary>
        /// Circle shape.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ShapeType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="ShapeType" />
        ///
        Circle = 3,

        /// <summary>
        /// Rectangle shape.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ShapeType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="ShapeType" />
        ///
        Rectangle = 4,

        /// <summary>
        /// Capsule shape.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ShapeType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="ShapeType" />
        ///
        Capsule = 5,

        /// <summary>
        /// Convex polygon shape.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ShapeType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="ShapeType" />
        ///
        ConvexPolygon = 6,

        /// <summary>
        /// Concave polygon shape.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ShapeType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="ShapeType" />
        ///
        ConcavePolygon = 7,

        /// <summary>
        /// Reserved custom shape value.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ShapeType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="ShapeType" />
        ///
        Custom = 8
    }

    /// <summary>
    /// Identifies a physics process statistic that can be queried from the server.
    /// </summary>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This type is part of the Electron2D 0.1-preview public API.
    /// </remarks>
    ///
    public enum ProcessInfo
    {
        /// <summary>
        /// Number of active physics objects.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ProcessInfo.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="ProcessInfo" />
        ///
        ActiveObjects = 0,

        /// <summary>
        /// Number of possible collision pairs.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ProcessInfo.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="ProcessInfo" />
        ///
        CollisionPairs = 1,

        /// <summary>
        /// Number of active simulation islands.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ProcessInfo.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="ProcessInfo" />
        ///
        IslandCount = 2
    }

    /// <summary>
    /// Activates or deactivates physics processing in the active backend.
    /// </summary>
    /// <param name="active">Whether the physics server should process physics objects.</param>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static void SetActive(bool active)
    {
        lock (BackendLock)
        {
            backend.SetActive(active);
        }
    }

    /// <summary>
    /// Creates a new 2D physics space.
    /// </summary>
    /// <returns>A <see cref="Rid"/> identifying the created space.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static Rid SpaceCreate()
    {
        lock (BackendLock)
        {
            return backend.SpaceCreate();
        }
    }

    /// <summary>
    /// Sets whether a physics space is active.
    /// </summary>
    /// <param name="space">The space to update.</param>
    /// <param name="active">Whether the space should be active.</param>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static void SpaceSetActive(Rid space, bool active)
    {
        lock (BackendLock)
        {
            backend.SpaceSetActive(space, active);
        }
    }

    /// <summary>
    /// Checks whether a physics space is active.
    /// </summary>
    /// <param name="space">The space to query.</param>
    /// <returns><c>true</c> when the space is active; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static bool SpaceIsActive(Rid space)
    {
        lock (BackendLock)
        {
            return backend.SpaceIsActive(space);
        }
    }

    /// <summary>
    /// Sets a numeric parameter on a physics space.
    /// </summary>
    /// <param name="space">The space to update.</param>
    /// <param name="param">The parameter to update.</param>
    /// <param name="value">The new parameter value.</param>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static void SpaceSetParam(Rid space, SpaceParameter param, float value)
    {
        lock (BackendLock)
        {
            backend.SpaceSetParam(space, param, value);
        }
    }

    /// <summary>
    /// Gets a numeric parameter from a physics space.
    /// </summary>
    /// <param name="space">The space to query.</param>
    /// <param name="param">The parameter to read.</param>
    /// <returns>The stored parameter value.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static float SpaceGetParam(Rid space, SpaceParameter param)
    {
        lock (BackendLock)
        {
            return backend.SpaceGetParam(space, param);
        }
    }

    /// <summary>
    /// Creates a new 2D area object.
    /// </summary>
    /// <returns>A <see cref="Rid"/> identifying the created area.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static Rid AreaCreate()
    {
        lock (BackendLock)
        {
            return backend.AreaCreate();
        }
    }

    /// <summary>
    /// Creates a new 2D body object.
    /// </summary>
    /// <returns>A <see cref="Rid"/> identifying the created body.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static Rid BodyCreate()
    {
        return BodyCreate(PhysicsBodyKind.Generic);
    }

    internal static Rid BodyCreate(PhysicsBodyKind bodyKind)
    {
        lock (BackendLock)
        {
            return backend.BodyCreate(bodyKind);
        }
    }

    /// <summary>
    /// Creates a new 2D joint object.
    /// </summary>
    /// <returns>A <see cref="Rid"/> identifying the created joint.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static Rid JointCreate()
    {
        lock (BackendLock)
        {
            return backend.JointCreate();
        }
    }

    /// <summary>
    /// Creates a world boundary shape.
    /// </summary>
    /// <returns>A <see cref="Rid"/> identifying the created shape.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static Rid WorldBoundaryShapeCreate()
    {
        return ShapeCreate(ShapeType.WorldBoundary);
    }

    /// <summary>
    /// Creates a separation ray shape.
    /// </summary>
    /// <returns>A <see cref="Rid"/> identifying the created shape.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static Rid SeparationRayShapeCreate()
    {
        return ShapeCreate(ShapeType.SeparationRay);
    }

    /// <summary>
    /// Creates a segment shape.
    /// </summary>
    /// <returns>A <see cref="Rid"/> identifying the created shape.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static Rid SegmentShapeCreate()
    {
        return ShapeCreate(ShapeType.Segment);
    }

    /// <summary>
    /// Creates a circle shape.
    /// </summary>
    /// <returns>A <see cref="Rid"/> identifying the created shape.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static Rid CircleShapeCreate()
    {
        return ShapeCreate(ShapeType.Circle);
    }

    /// <summary>
    /// Creates a rectangle shape.
    /// </summary>
    /// <returns>A <see cref="Rid"/> identifying the created shape.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static Rid RectangleShapeCreate()
    {
        return ShapeCreate(ShapeType.Rectangle);
    }

    /// <summary>
    /// Creates a capsule shape.
    /// </summary>
    /// <returns>A <see cref="Rid"/> identifying the created shape.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static Rid CapsuleShapeCreate()
    {
        return ShapeCreate(ShapeType.Capsule);
    }

    /// <summary>
    /// Creates a convex polygon shape.
    /// </summary>
    /// <returns>A <see cref="Rid"/> identifying the created shape.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static Rid ConvexPolygonShapeCreate()
    {
        return ShapeCreate(ShapeType.ConvexPolygon);
    }

    /// <summary>
    /// Creates a concave polygon shape.
    /// </summary>
    /// <returns>A <see cref="Rid"/> identifying the created shape.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static Rid ConcavePolygonShapeCreate()
    {
        return ShapeCreate(ShapeType.ConcavePolygon);
    }

    /// <summary>
    /// Gets the Electron2D shape type stored for a physics shape RID.
    /// </summary>
    /// <param name="shape">The shape to query.</param>
    /// <returns>The type of the shape.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static ShapeType ShapeGetType(Rid shape)
    {
        lock (BackendLock)
        {
            return backend.ShapeGetType(shape);
        }
    }

    /// <summary>
    /// Frees a physics server RID.
    /// </summary>
    /// <param name="rid">The RID to free.</param>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static void FreeRid(Rid rid)
    {
        lock (BackendLock)
        {
            backend.FreeRid(rid);
        }
    }

    /// <summary>
    /// Gets a physics process statistic.
    /// </summary>
    /// <param name="processInfo">The statistic to read.</param>
    /// <returns>The current statistic value.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
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
    /// <seealso cref="PhysicsServer2D" />
    ///
    public static int GetProcessInfo(ProcessInfo processInfo)
    {
        lock (BackendLock)
        {
            return backend.GetProcessInfo(processInfo);
        }
    }

    internal static string CurrentBackendName
    {
        get
        {
            lock (BackendLock)
            {
                return backend.Name;
            }
        }
    }

    internal static void SetBackend(IPhysicsServer2DBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        lock (BackendLock)
        {
            PhysicsServer2D.backend = backend;
        }
    }

    internal static void CollisionObjectSetTransform(Rid rid, Transform2D transform)
    {
        lock (BackendLock)
        {
            backend.CollisionObjectSetTransform(rid, transform);
        }
    }

    internal static void CollisionObjectSetCollisionFilter(Rid rid, PhysicsCollisionFilter filter)
    {
        lock (BackendLock)
        {
            backend.CollisionObjectSetCollisionFilter(rid, filter);
        }
    }

    internal static void BodySetState(Rid rid, PhysicsBody2DState state)
    {
        lock (BackendLock)
        {
            backend.BodySetState(rid, state);
        }
    }

    private static Rid ShapeCreate(ShapeType type)
    {
        lock (BackendLock)
        {
            return backend.ShapeCreate(type);
        }
    }
}
