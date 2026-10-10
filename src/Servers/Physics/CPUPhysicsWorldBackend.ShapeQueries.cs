using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class CPUPhysicsWorldBackend
{
    private readonly List<B2ShapeProxy> _queryProxies = [];
    private readonly List<ShapeCandidate> _shapeCandidates = [];
    private readonly List<PhysicsShapeResult> _shapeHits = [];
    private bool? _queryRaySlide;
    private bool _queryCompoundConvex;
    private float _queryMargin;

    private readonly record struct ShapeCandidate(B2ShapeId ShapeID, PhysicsFixtureTag Tag);

    internal override List<PhysicsShapeResult> CollectShapeHits(PhysicsShapeQueryParameters parameters)
    {
        var space = PrepareShapeQuery(parameters);
        var hits = _shapeHits;
        hits.Clear();
        if (_queryProxies.Count == 0 || _shapeCandidates.Count == 0) return hits;
        var world = b2GetWorldFromId(space.WorldID);
        var motion = PhysicsShapeBackend.ToBackend(parameters.Motion);
        foreach (var candidate in _shapeCandidates)
        {
            var backendShape = b2GetShape(world, candidate.ShapeID);
            var other = PhysicsShapeBackend.GetQueryProxy(backendShape);
            var otherTransform = b2Body_GetTransform(b2Shape_GetBody(candidate.ShapeID));
            for (var piece = 0; piece < _queryProxies.Count; piece++)
            {
                var query = _queryProxies[piece];
                if (query.isBoundary || other.isBoundary) query = BoundaryQuery(query);
                if (!query.isBoundary && !other.isBoundary && (_queryRaySlide is not null || candidate.Tag.SeparationRay is not null))
                {
                    if (PhysicsSeparationRay.PairContact(query, _queryRaySlide, other, otherTransform,
                        candidate.Tag.SeparationRay, motion, _queryMargin).pointCount == 0) continue;
                }
                else if (!Overlaps(query, other, otherTransform) &&
                         !SweepsInto(query, other, otherTransform, motion, 1f)) continue;
                hits.Add(new(candidate.Tag.ColliderRID,
                    PhysicsServer.Service.ResolveSceneObject(candidate.Tag.ColliderRID), candidate.Tag.ObjectIdentity,
                    candidate.Tag.ShapeIndex));
                break;
            }
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

    internal override (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters parameters)
    {
        var space = PrepareShapeQuery(parameters);
        var motion = PhysicsShapeBackend.ToBackend(parameters.Motion);
        if ((motion.X == 0 && motion.Y == 0) || _queryProxies.Count == 0 || _shapeCandidates.Count == 0)
            return (1, 1);
        var world = b2GetWorldFromId(space.WorldID);
        RemoveInitiallyOverlappingSlots(world);
        var bestSafe = 1f;
        var bestUnsafe = 1f;
        foreach (var candidate in _shapeCandidates)
        {
            var backendShape = b2GetShape(world, candidate.ShapeID);
            var other = PhysicsShapeBackend.GetQueryProxy(backendShape);
            var otherTransform = b2Body_GetTransform(b2Shape_GetBody(candidate.ShapeID));
            for (var piece = 0; piece < _queryProxies.Count; piece++)
            {
                var query = _queryProxies[piece];
                if (query.isBoundary || other.isBoundary) query = BoundaryQuery(query);
                if (!query.isBoundary && !other.isBoundary && (_queryRaySlide is not null || candidate.Tag.SeparationRay is not null))
                {
                    if (PhysicsSeparationRay.PairContact(query, _queryRaySlide, other, otherTransform,
                            candidate.Tag.SeparationRay, motion * bestSafe, _queryMargin).pointCount == 0) continue;
                    var lowRay = 0f;
                    var highRay = bestSafe;
                    for (var step = 0; step < 8; step++)
                    {
                        var middle = (lowRay + highRay) * 0.5f;
                        if (PhysicsSeparationRay.PairContact(query, _queryRaySlide, other, otherTransform,
                                candidate.Tag.SeparationRay, motion * middle, _queryMargin).pointCount != 0)
                            highRay = middle;
                        else lowRay = middle;
                    }
                    bestSafe = lowRay;
                    bestUnsafe = highRay;
                    continue;
                }
                if (!SweepsInto(query, other, otherTransform, motion, bestSafe)) continue;
                var low = 0f;
                var high = bestSafe;
                for (var step = 0; step < 8; step++)
                {
                    var middle = (low + high) * 0.5f;
                    if (SweepsInto(query, other, otherTransform, motion, middle)) high = middle;
                    else low = middle;
                }
                if (low < bestSafe)
                {
                    bestSafe = low;
                    bestUnsafe = high;
                }
            }
        }
        return (bestSafe, bestUnsafe);
    }

    private int CandidateGroupEnd(int first)
    {
        var tag = _shapeCandidates[first].Tag; var end = first + 1;
        while (end < _shapeCandidates.Count && _shapeCandidates[end].Tag.ColliderRID == tag.ColliderRID && _shapeCandidates[end].Tag.ShapeIndex == tag.ShapeIndex) end++;
        return end;
    }

    private void RemoveInitiallyOverlappingSlots(B2World world)
    {
        var kept = 0;
        for (var first = 0; first < _shapeCandidates.Count;)
        {
            var end = CandidateGroupEnd(first);
            var overlaps = false;
            for (var i = first; i < end && !overlaps; i++)
            {
                var candidate = _shapeCandidates[i]; var other = PhysicsShapeBackend.GetQueryProxy(b2GetShape(world, candidate.ShapeID));
                var pose = b2Body_GetTransform(b2Shape_GetBody(candidate.ShapeID));
                foreach (var query in _queryProxies)
                {
                    overlaps = query.isBoundary || other.isBoundary ? Overlaps(BoundaryQuery(query), other, pose) :
                        _queryRaySlide is not null || candidate.Tag.SeparationRay is not null
                        ? PhysicsSeparationRay.PairContact(query, _queryRaySlide, other, pose, candidate.Tag.SeparationRay, default, _queryMargin).pointCount != 0
                        : Overlaps(query, other, pose);
                    if (overlaps) break;
                }
            }
            // A logical shape's internal decomposition seams must not stop an already-overlapping query.
            if (!overlaps) for (var i = first; i < end; i++) _shapeCandidates[kept++] = _shapeCandidates[i];
            first = end;
        }
        if (kept < _shapeCandidates.Count) _shapeCandidates.RemoveRange(kept, _shapeCandidates.Count - kept);
    }

    private void RemoveContainedRaySlots(B2Vec2 motion)
    {
        if (_queryRaySlide is null && !_queryCompoundConvex || _queryProxies.Count == 0) return;
        var kept = 0;
        for (var first = 0; first < _shapeCandidates.Count;)
        {
            var tag = _shapeCandidates[first].Tag; var end = CandidateGroupEnd(first);
            var inside = false;
            if (_queryRaySlide is not null && end - first > 1 && b2Shape_GetType(_shapeCandidates[first].ShapeID) == B2ShapeType.b2_polygonShape)
                for (var i = first; i < end && !inside; i++) inside = b2Shape_TestPoint(_shapeCandidates[i].ShapeID, _queryProxies[0].points[0]);
            if (_queryCompoundConvex && tag.SeparationRay is { } ray)
            {
                var pose = b2Body_GetTransform(b2Shape_GetBody(_shapeCandidates[first].ShapeID));
                var origin = PhysicsSeparationRay.WorldProxy(ray, pose, default).points[0];
                foreach (var query in _queryProxies) if (PhysicsSeparationRay.ContainsSweptRegion(query, origin, motion)) { inside = true; break; }
            }
            if (!inside) for (var i = first; i < end; i++) _shapeCandidates[kept++] = _shapeCandidates[i];
            first = end;
        }
        if (kept < _shapeCandidates.Count) _shapeCandidates.RemoveRange(kept, _shapeCandidates.Count - kept);
    }

    private PhysicsSpace PrepareShapeQuery(PhysicsShapeQueryParameters parameters)
    {
        var space = Space;
        var shape = parameters.Shape ?? PhysicsServer.Service.GetShapeGeometry(parameters.ShapeRID);
        _queryRaySlide = (shape as SeparationRayShape)?.SlideOnSlope;
        _queryProxies.Clear();
        PhysicsShapeBackend.AppendQueryProxies(shape, _queryProxies);
        if (shape is SeparationRayShape { Length: 0 }) _queryProxies.Clear();
        _queryCompoundConvex = shape is ConvexPolygonShape && _queryProxies.Count > 1;
        var transform = parameters.Transform;
        var backendTransform = new B2Transform(PhysicsShapeBackend.ToBackend(transform.Origin), b2MakeRot(transform.Rotation));
        var margin = parameters.Margin * PhysicsSpace.MetersPerUnit;
        _queryMargin = margin;
        for (var index = 0; index < _queryProxies.Count; index++)
        {
            var proxy = _queryProxies[index];
            if (proxy.isBoundary) proxy.boundary = B2Boundaries.Transform(proxy.boundary, backendTransform);
            if (_queryRaySlide is null) proxy.radius += margin;
            if (!float.IsFinite(proxy.radius)) throw new ArgumentOutOfRangeException(nameof(parameters));
            for (var pointIndex = 0; pointIndex < proxy.count; pointIndex++)
            {
                var point = b2TransformPoint(backendTransform, proxy.points[pointIndex]);
                if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
                    throw new ArgumentOutOfRangeException(nameof(parameters), "Query geometry exceeds the finite range.");
                proxy.points[pointIndex] = point;
            }
            _queryProxies[index] = proxy;
        }
        _shapeCandidates.Clear();
        var excluded = parameters.ExclusionsArray;
        var mask = parameters.CollisionMask;
        if (parameters.CollideWithBodies)
            for (var index = 0; index < space.Bodies.Count; index++)
                AddCandidates(space.Bodies[index].BackendShapes, mask, excluded);
        if (parameters.CollideWithAreas)
            for (var index = 0; index < space.Areas.Count; index++)
                AddCandidates(space.Areas[index].BackendShapes, mask, excluded);
        for (var index = 0; index < space.ServerColliders.Count; index++)
        {
            var collider = space.ServerColliders[index];
            if (collider.IsArea ? parameters.CollideWithAreas : parameters.CollideWithBodies)
                AddCandidates(collider.BackendShapes, mask, excluded);
        }
        RemoveContainedRaySlots(PhysicsShapeBackend.ToBackend(parameters.Motion));
        return space;
    }

    private B2ShapeProxy BoundaryQuery(B2ShapeProxy query)
    {
        if (_queryRaySlide is not null && query.count == 2)
            query.points[1] += _queryMargin * b2Normalize(query.points[1] - query.points[0]);
        return query;
    }

    private void AddCandidates(IReadOnlyList<B2ShapeId> shapes, uint mask, RID[] excluded)
    {
        for (var index = 0; index < shapes.Count; index++)
        {
            var shape = shapes[index];
            if (Eligible(shape, mask, excluded, out var tag, includeSeparationRays: true))
                _shapeCandidates.Add(new(shape, tag));
        }
    }

    internal static bool Overlaps(in B2ShapeProxy query, in B2ShapeProxy other, in B2Transform otherTransform)
    {
        var input = new B2DistanceInput
        {
            proxyA = query,
            proxyB = other,
            transformA = b2Transform_identity,
            transformB = otherTransform,
            useRadii = true
        };
        var cache = new B2SimplexCache();
        return b2ShapeDistance(ref input, ref cache, null, 0).distance <= 0.1f * B2_LINEAR_SLOP;
    }

    private static bool SweepsInto(in B2ShapeProxy query, in B2ShapeProxy other,
        in B2Transform otherTransform, in B2Vec2 motion, float maxFraction)
    {
        if ((motion.X == 0 && motion.Y == 0) || maxFraction <= 0) return false;
        return Cast(query, other, otherTransform, motion, maxFraction).hit;
    }

    internal static B2CastOutput Cast(in B2ShapeProxy query, in B2ShapeProxy other,
        in B2Transform otherTransform, in B2Vec2 motion, float maxFraction)
    {
        var cast = new B2ShapeCastPairInput
        {
            proxyA = other,
            proxyB = query,
            transformA = otherTransform,
            transformB = b2Transform_identity,
            translationB = motion,
            maxFraction = maxFraction
        };
        return b2ShapeCast(cast);
    }
}
