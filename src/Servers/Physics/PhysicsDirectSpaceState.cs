using Box2D.NET;
using static Box2D.NET.B2Shapes;

namespace Electron2D;

/// <summary>A typed result for the nearest ray hit in a two-dimensional physics space.</summary>
public readonly struct PhysicsRayResult
{
    internal PhysicsRayResult(RID rid, CollisionObject? collider, int shapeIndex, Vector2 position, Vector2 normal)
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
public readonly struct PhysicsPointResult
{
    internal PhysicsPointResult(RID rid, CollisionObject? collider, int shapeIndex)
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
    private readonly List<PhysicsPointResult> _pointHits = [];

    internal PhysicsDirectSpaceState(RID spaceRID) => _spaceRID = spaceRID;

    /// <summary>Finds the nearest eligible collider along a ray.</summary>
    /// <param name="parameters">Global endpoints, filtering and exclusion options.</param>
    /// <returns>The nearest typed hit, or null when the ray finds none.</returns>
    /// <remarks>Origin containment applies to the complete logical shape, including compound polygon pieces.
    /// Inside hits are skipped unless enabled; enabled inside hits use the origin and a zero normal.</remarks>
    /// <exception cref="ArgumentNullException">Parameters are null.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or the world is stepping.</exception>
    /// <exception cref="ObjectDisposedException">The view has been disposed.</exception>
    public PhysicsRayResult? IntersectRay(PhysicsRayQueryParameters parameters)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(parameters);
        return IntersectRay(parameters.From, parameters.To, parameters.CollisionMask, parameters.ExclusionsArray,
            parameters.CollideWithAreas, parameters.CollideWithBodies, parameters.HitFromInside);
    }

    internal PhysicsRayResult? IntersectRay(Vector2 from, Vector2 to, uint mask, RID[] excluded,
        bool collideWithAreas, bool collideWithBodies, bool hitFromInside)
    {
        ThrowIfDisposed();
        if (!from.IsFinite() || !to.IsFinite())
            throw new ArgumentOutOfRangeException(nameof(from), "Ray endpoints must be finite.");
        var space = PhysicsServer.Service.GetSceneSpace(_spaceRID);
        space.PrepareForQuery();
        var motion = to - from;
        if (!motion.IsFinite()) throw new ArgumentOutOfRangeException(nameof(to), "Ray span exceeds the finite range.");
        if (motion == Vector2.Zero || mask == 0 || !collideWithBodies && !collideWithAreas) return null;

        var input = new B2RayCastInput(PhysicsShapeBackend.ToBackend(from), PhysicsShapeBackend.ToBackend(motion), 1);
        PhysicsRayResult? best = null;
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
    public PhysicsPointResult[] IntersectPoint(PhysicsPointQueryParameters parameters, int maxResults = 32)
    {
        if (maxResults < 0) throw new ArgumentOutOfRangeException(nameof(maxResults));
        var hits = CollectPointHits(parameters);
        var count = Math.Min(maxResults, hits.Count);
        if (count == 0) return [];
        var output = new PhysicsPointResult[count];
        hits.CopyTo(0, output, 0, count);
        return output;
    }

    /// <summary>Copies ordered unique point-query hits into caller-owned storage.</summary>
    /// <param name="parameters">The global point and collider filters.</param>
    /// <param name="results">Destination storage; its length is the maximum result count.</param>
    /// <returns>The written count. Remaining elements are unchanged; excess hits are truncated in RID/index order.</returns>
    /// <remarks>Reuses prepared query scratch capacity and creates no output array.</remarks>
    /// <exception cref="ArgumentNullException">Parameters are null.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or the world is stepping.</exception>
    /// <exception cref="ObjectDisposedException">The view has been disposed.</exception>
    public int IntersectPoint(PhysicsPointQueryParameters parameters, Span<PhysicsPointResult> results)
    {
        var hits = CollectPointHits(parameters);
        var count = Math.Min(results.Length, hits.Count);
        for (var i = 0; i < count; i++) results[i] = hits[i];
        return count;
    }

    private List<PhysicsPointResult> CollectPointHits(PhysicsPointQueryParameters parameters)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(parameters);
        var space = PhysicsServer.Service.GetSceneSpace(_spaceRID);
        space.PrepareForQuery();
        var hits = _pointHits;
        hits.Clear();
        if (parameters.CollisionMask == 0 || !parameters.CollideWithBodies && !parameters.CollideWithAreas) return hits;
        var point = PhysicsShapeBackend.ToBackend(parameters.Position);
        var excluded = parameters.ExclusionsArray;
        var mask = parameters.CollisionMask;
        if (parameters.CollideWithBodies)
            for (var index = 0; index < space.Bodies.Count; index++)
                ScanPointShapes(space.Bodies[index].Backend, point, mask, parameters.CanvasInstanceID, excluded, hits);
        if (parameters.CollideWithAreas)
            for (var index = 0; index < space.Areas.Count; index++)
                ScanPointShapes(space.Areas[index].Backend, point, mask, parameters.CanvasInstanceID, excluded, hits);
        for (var index = 0; index < space.ServerColliders.Count; index++)
        {
            var collider = space.ServerColliders[index];
            if (collider.IsArea ? parameters.CollideWithAreas : parameters.CollideWithBodies)
                ScanPointShapes(collider.Backend, point, mask, parameters.CanvasInstanceID, excluded, hits);
        }
        hits.Sort(static (left, right) =>
        {
            var order = left.ColliderRID.CompareTo(right.ColliderRID);
            return order != 0 ? order : left.ShapeIndex.CompareTo(right.ShapeIndex);
        });
        var used = 0;
        for (var i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            if (used > 0 && hit.ColliderRID == hits[used - 1].ColliderRID && hit.ShapeIndex == hits[used - 1].ShapeIndex) continue;
            hits[used++] = hit;
        }
        if (used < hits.Count) hits.RemoveRange(used, hits.Count - used);
        return hits;
    }

    private static void ScanRayShapes(IReadOnlyList<B2ShapeId> shapes, B2RayCastInput input, Vector2 from,
        uint mask, RID[] excluded, bool hitFromInside, ref PhysicsRayResult? best, ref float bestFraction)
    {
        // ponytail: Scene fixture scans are linear; use a query broad phase when large-world profiling needs it.
        for (var index = 0; index < shapes.Count; index++)
        {
            var shape = shapes[index];
            if (!Eligible(shape, mask, excluded, out var tag)) continue;
            // The backend appends each logical slot's pieces contiguously. An internal seam is not a new shape.
            var first = index;
            while (index + 1 < shapes.Count && b2Shape_GetUserData(shapes[index + 1]).GetRef<PhysicsFixtureTag>() is { } next &&
                   next.ColliderRID == tag.ColliderRID && next.ShapeIndex == tag.ShapeIndex) index++;
            var startsInside = b2Shape_TestPoint(shape, input.origin);
            for (var piece = first + 1; piece <= index && !startsInside; piece++)
                startsInside = b2Shape_TestPoint(shapes[piece], input.origin);
            if (startsInside && !hitFromInside) continue;
            for (var piece = first; piece <= (startsInside ? first : index); piece++)
            {
                var output = startsInside ? default : b2Shape_RayCast(shapes[piece], input);
                if (!startsInside && !output.hit) continue;
                var fraction = startsInside ? 0 : output.fraction;
                if (fraction > bestFraction || fraction == bestFraction && best is { } prior &&
                    (tag.ColliderRID > prior.ColliderRID || tag.ColliderRID == prior.ColliderRID && tag.ShapeIndex >= prior.ShapeIndex))
                    continue;
                bestFraction = fraction;
                var point = startsInside ? from : new Vector2(output.point.X * PhysicsSpace.UnitsPerMeter,
                    output.point.Y * PhysicsSpace.UnitsPerMeter);
                var normal = startsInside ? Vector2.Zero : new Vector2(output.normal.X, output.normal.Y);
                best = new PhysicsRayResult(tag.ColliderRID, PhysicsServer.Service.ResolveSceneObject(tag.ColliderRID),
                    tag.ShapeIndex, point, normal);
            }
        }
    }

    private static void ScanPointShapes(PhysicsColliderBackend backend, B2Vec2 point, uint mask,
        ulong canvasInstanceID, RID[] excluded, List<PhysicsPointResult> hits)
    {
        if (backend.CanvasInstanceID != canvasInstanceID) return;
        var shapes = backend.Shapes;
        for (var index = 0; index < shapes.Count; index++)
        {
            var shape = shapes[index];
            if (!Eligible(shape, mask, excluded, out var tag) || !b2Shape_TestPoint(shape, point)) continue;
            hits.Add(new(tag.ColliderRID, PhysicsServer.Service.ResolveSceneObject(tag.ColliderRID), tag.ShapeIndex));
        }
    }

    private static bool Eligible(B2ShapeId shape, uint mask, RID[] excluded, out PhysicsFixtureTag tag,
        bool includeSeparationRays = false)
    {
        tag = b2Shape_GetUserData(shape).GetRef<PhysicsFixtureTag>()!;
        if (tag is null || tag.SeparationRay is { } empty && empty.From.X == empty.To.X && empty.From.Y == empty.To.Y ||
            !includeSeparationRays && tag.SeparationRay is not null ||
            (b2Shape_GetFilter(shape).categoryBits & mask) == 0) return false;
        foreach (var rid in excluded) if (rid == tag.ColliderRID) return false;
        return true;
    }
}
