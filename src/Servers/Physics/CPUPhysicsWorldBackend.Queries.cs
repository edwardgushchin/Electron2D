using Box2D.NET;
using static Box2D.NET.B2Shapes;

namespace Electron2D;

internal sealed partial class CPUPhysicsWorldBackend
{
    private readonly List<PhysicsPointResult> _pointHits = [];

    internal override PhysicsRayResult? IntersectRay(Vector2 from, Vector2 to, uint mask, RID[] excluded,
        bool collideWithAreas, bool collideWithBodies, bool hitFromInside)
    {
        var space = Space;
        var motion = to - from;
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

    internal override List<PhysicsPointResult> CollectPointHits(PhysicsPointQueryParameters parameters)
    {
        var space = Space;
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
                best = new PhysicsRayResult(tag.ColliderRID, PhysicsServer.Service.ResolveSceneObject(tag.ColliderRID), tag.ObjectIdentity,
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
            hits.Add(new(tag.ColliderRID, PhysicsServer.Service.ResolveSceneObject(tag.ColliderRID), tag.ObjectIdentity, tag.ShapeIndex));
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
