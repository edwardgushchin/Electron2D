using Box2D.NET;
using static Box2D.NET.B2Shapes;

namespace Electron2D;

/// <summary>A typed result for the nearest ray hit in a two-dimensional physics space.</summary>
public readonly struct PhysicsRayResult2D
{
    internal PhysicsRayResult2D(RID rid, CollisionObject? collider, int shapeIndex, Vector2 position, Vector2 normal)
    {
        ColliderRID = rid;
        Collider = collider;
        ShapeIndex = shapeIndex;
        Position = position;
        Normal = normal;
    }

    /// <summary>Gets the stable identity of the collider, including server-only colliders.</summary>
    /// <value>A nonempty RID from the owning physics server.</value>
    public RID ColliderRID { get; }
    /// <summary>Gets the scene collider, or null for a server-only collider.</summary>
    /// <value>The scene object when one exists.</value>
    public CollisionObject? Collider { get; }
    /// <summary>Gets the scene object's instance ID, or zero for a server-only collider.</summary>
    /// <value>A managed scene instance ID or zero.</value>
    public ulong ColliderID => Collider?.InstanceID ?? 0;
    /// <summary>Gets the collider's stable shape-owner index.</summary>
    /// <value>The direct owner slot, unchanged by fixture rebuild.</value>
    public int ShapeIndex { get; }
    /// <summary>Gets the hit position in global scene units.</summary>
    /// <value>The first eligible contact point along the ray.</value>
    public Vector2 Position { get; }
    /// <summary>Gets the outward contact normal, or zero when hit from inside.</summary>
    /// <value>A world-space direction or zero.</value>
    public Vector2 Normal { get; }
}

/// <summary>A typed collider and shape-owner result for a point query.</summary>
public readonly struct PhysicsPointResult2D
{
    internal PhysicsPointResult2D(RID rid, CollisionObject? collider, int shapeIndex)
    {
        ColliderRID = rid;
        Collider = collider;
        ShapeIndex = shapeIndex;
    }

    /// <summary>Gets the stable identity of the collider, including server-only colliders.</summary>
    /// <value>A nonempty RID from the owning physics server.</value>
    public RID ColliderRID { get; }
    /// <summary>Gets the scene collider, or null for a server-only collider.</summary>
    /// <value>The scene object when one exists.</value>
    public CollisionObject? Collider { get; }
    /// <summary>Gets the scene object's instance ID, or zero for a server-only collider.</summary>
    /// <value>A managed scene instance ID or zero.</value>
    public ulong ColliderID => Collider?.InstanceID ?? 0;
    /// <summary>Gets the collider's stable shape-owner index.</summary>
    /// <value>The direct owner slot, unchanged by fixture rebuild.</value>
    public int ShapeIndex { get; }
}

/// <summary>Queries the live solver state of one two-dimensional physics space.</summary>
/// <remarks>A view becomes unusable when its owning space is freed. Queries require that space's owner thread
/// and cannot run during its solver step.</remarks>
public sealed partial class PhysicsDirectSpaceState : ElectronObject
{
    private readonly RID _spaceRID;

    internal PhysicsDirectSpaceState(RID spaceRID) => _spaceRID = spaceRID;

    /// <summary>Finds the nearest eligible collider along a ray.</summary>
    /// <param name="parameters">Global endpoints, filtering and exclusion options.</param>
    /// <returns>The nearest typed hit, or null when the ray finds none.</returns>
    /// <exception cref="ArgumentNullException">Parameters are null.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or the world is stepping.</exception>
    /// <exception cref="ObjectDisposedException">The view has been disposed.</exception>
    public PhysicsRayResult2D? IntersectRay(PhysicsRayQueryParameters2D parameters)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(parameters);
        return IntersectRay(parameters.From, parameters.To, parameters.CollisionMask, parameters.Exclude,
            parameters.CollideWithAreas, parameters.CollideWithBodies, parameters.HitFromInside);
    }

    internal PhysicsRayResult2D? IntersectRay(Vector2 from, Vector2 to, uint mask, RID[] excluded,
        bool collideWithAreas, bool collideWithBodies, bool hitFromInside)
    {
        ThrowIfDisposed();
        if (!from.IsFinite() || !to.IsFinite())
            throw new ArgumentOutOfRangeException(nameof(from), "Ray endpoints must be finite.");
        var space = PhysicsServer.Instance.GetSceneSpace(_spaceRID);
        space.PrepareForQuery();
        var motion = to - from;
        if (!motion.IsFinite()) throw new ArgumentOutOfRangeException(nameof(to), "Ray span exceeds the finite range.");
        if (motion == Vector2.Zero || mask == 0 || !collideWithBodies && !collideWithAreas) return null;

        var input = new B2RayCastInput(Shape.ToBackend(from), Shape.ToBackend(motion), 1);
        PhysicsRayResult2D? best = null;
        var bestFraction = float.PositiveInfinity;
        if (collideWithBodies)
            for (var index = 0; index < space.Bodies.Count; index++)
                ScanRayShapes(space.Bodies[index].BackendShapes, input, from, mask, excluded, hitFromInside,
                    ref best, ref bestFraction);
        if (collideWithAreas)
            for (var index = 0; index < space.Areas.Count; index++)
                ScanRayShapes(space.Areas[index].BackendShapes, input, from, mask, excluded, hitFromInside,
                    ref best, ref bestFraction);
        for (var index = 0; index < space.ServerColliders.Count; index++)
        {
            var collider = space.ServerColliders[index];
            if (collider.IsArea ? collideWithAreas : collideWithBodies)
                ScanRayShapes(collider.BackendShapes, input, from, mask, excluded, hitFromInside, ref best, ref bestFraction);
        }
        return best;
    }

    /// <summary>Finds filled shapes containing a global point.</summary>
    /// <param name="parameters">Point, filtering and exclusion options.</param>
    /// <param name="maxResults">Maximum results after stable RID/shape-index ordering; 32 by default.</param>
    /// <returns>A caller-owned array of typed collider hits, empty when none qualify.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The maximum result count is negative.</exception>
    /// <exception cref="ArgumentNullException">Parameters are null.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or the world is stepping.</exception>
    /// <exception cref="ObjectDisposedException">The view has been disposed.</exception>
    public PhysicsPointResult2D[] IntersectPoint(PhysicsPointQueryParameters2D parameters, int maxResults = 32)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(parameters);
        if (maxResults < 0) throw new ArgumentOutOfRangeException(nameof(maxResults));
        var space = PhysicsServer.Instance.GetSceneSpace(_spaceRID);
        space.PrepareForQuery();
        if (maxResults == 0 || parameters.CollisionMask == 0 ||
            !parameters.CollideWithBodies && !parameters.CollideWithAreas) return [];

        var point = Shape.ToBackend(parameters.Position);
        var excluded = parameters.Exclude;
        var mask = parameters.CollisionMask;
        var hits = new List<PhysicsPointResult2D>();
        if (parameters.CollideWithBodies)
            for (var index = 0; index < space.Bodies.Count; index++)
                ScanPointShapes(space.Bodies[index].BackendShapes, point, mask, excluded, hits);
        if (parameters.CollideWithAreas)
            for (var index = 0; index < space.Areas.Count; index++)
                ScanPointShapes(space.Areas[index].BackendShapes, point, mask, excluded, hits);
        for (var index = 0; index < space.ServerColliders.Count; index++)
        {
            var collider = space.ServerColliders[index];
            if (collider.IsArea ? parameters.CollideWithAreas : parameters.CollideWithBodies)
                ScanPointShapes(collider.BackendShapes, point, mask, excluded, hits);
        }
        hits.Sort(static (left, right) =>
        {
            var order = left.ColliderRID.CompareTo(right.ColliderRID);
            return order != 0 ? order : left.ShapeIndex.CompareTo(right.ShapeIndex);
        });
        var output = new List<PhysicsPointResult2D>(Math.Min(maxResults, hits.Count));
        foreach (var hit in hits)
        {
            if (output.Count != 0 && hit.ColliderRID == output[^1].ColliderRID &&
                hit.ShapeIndex == output[^1].ShapeIndex) continue;
            output.Add(hit);
            if (output.Count == maxResults) break;
        }
        return output.ToArray();
    }

    private static void ScanRayShapes(IReadOnlyList<B2ShapeId> shapes, B2RayCastInput input, Vector2 from,
        uint mask, RID[] excluded, bool hitFromInside, ref PhysicsRayResult2D? best, ref float bestFraction)
    {
        // ponytail: Scene fixture scans are linear; use a query broad phase when large-world profiling needs it.
        for (var index = 0; index < shapes.Count; index++)
        {
            var shape = shapes[index];
            if (!Eligible(shape, mask, excluded, out var tag)) continue;
            var startsInside = b2Shape_TestPoint(shape, input.origin);
            if (startsInside && !hitFromInside) continue;
            var output = startsInside ? default : b2Shape_RayCast(shape, input);
            if (!startsInside && !output.hit) continue;
            var fraction = startsInside ? 0 : output.fraction;
            if (fraction > bestFraction || fraction == bestFraction && best is { } prior &&
                (tag.ColliderRID > prior.ColliderRID || tag.ColliderRID == prior.ColliderRID && tag.ShapeIndex >= prior.ShapeIndex))
                continue;
            bestFraction = fraction;
            var point = startsInside ? from : new Vector2(output.point.X * PhysicsSpace.UnitsPerMeter,
                output.point.Y * PhysicsSpace.UnitsPerMeter);
            var normal = startsInside ? Vector2.Zero : new Vector2(output.normal.X, output.normal.Y);
            best = new PhysicsRayResult2D(tag.ColliderRID, PhysicsServer.Instance.ResolveSceneObject(tag.ColliderRID),
                tag.ShapeIndex, point, normal);
        }
    }

    private static void ScanPointShapes(IReadOnlyList<B2ShapeId> shapes, B2Vec2 point, uint mask,
        RID[] excluded, List<PhysicsPointResult2D> hits)
    {
        for (var index = 0; index < shapes.Count; index++)
        {
            var shape = shapes[index];
            if (!Eligible(shape, mask, excluded, out var tag) || !b2Shape_TestPoint(shape, point)) continue;
            hits.Add(new(tag.ColliderRID, PhysicsServer.Instance.ResolveSceneObject(tag.ColliderRID), tag.ShapeIndex));
        }
    }

    private static bool Eligible(B2ShapeId shape, uint mask, RID[] excluded, out PhysicsFixtureTag tag,
        bool includeSeparationRays = false)
    {
        tag = b2Shape_GetUserData(shape).GetRef<PhysicsFixtureTag>()!;
        if (tag is null || !includeSeparationRays && tag.SeparationRay is not null ||
            (b2Shape_GetFilter(shape).categoryBits & mask) == 0) return false;
        foreach (var rid in excluded) if (rid == tag.ColliderRID) return false;
        return true;
    }
}
