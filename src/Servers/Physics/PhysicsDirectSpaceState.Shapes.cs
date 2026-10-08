using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

/// <summary>A typed collider result from a direct shape-overlap query.</summary>
public readonly struct PhysicsShapeResult
{
    internal PhysicsShapeResult(RID rid, CollisionObject? collider, int shapeIndex)
    {
        ColliderRID = rid;
        Collider = collider;
        ShapeIndex = shapeIndex;
    }

    /// <summary>Gets the stable RID of the intersecting collider.</summary>
    /// <value>A nonempty server identity.</value>
    public RID ColliderRID { get; }

    /// <summary>Gets the scene collider, or null for a server-only collider.</summary>
    /// <value>The live scene object when one exists.</value>
    public CollisionObject? Collider { get; }

    /// <summary>Gets the scene object instance ID, or zero for a server-only collider.</summary>
    /// <value>A managed instance ID or zero.</value>
    public ulong ColliderID => Collider?.InstanceID ?? 0;

    /// <summary>Gets the stable direct shape-owner index.</summary>
    /// <value>An index unchanged by compound fixture rebuilds.</value>
    public int ShapeIndex { get; }
}

public sealed partial class PhysicsDirectSpaceState
{
    private readonly List<B2ShapeProxy> _queryProxies = [];
    private readonly List<ShapeCandidate> _shapeCandidates = [];
    private readonly List<PhysicsShapeResult> _shapeHits = [];
    private bool? _queryRaySlide;
    private float _queryMargin;

    private readonly record struct ShapeCandidate(B2ShapeId ShapeID, PhysicsFixtureTag Tag);

    /// <summary>Finds collider shape owners intersected by a query shape or its motion.</summary>
    /// <param name="parameters">A live Shape resource or shape RID, pose, margin and filters.</param>
    /// <param name="maxResults">Maximum RID/index-ordered results; 32 by default.</param>
    /// <returns>A caller-owned array of unique collider shape-owner results.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The maximum count is negative.</exception>
    public PhysicsShapeResult[] IntersectShape(PhysicsShapeQueryParameters parameters, int maxResults = 32)
    {
        if (maxResults < 0) throw new ArgumentOutOfRangeException(nameof(maxResults));
        var hits = CollectShapeHits(parameters);
        var count = Math.Min(maxResults, hits.Count);
        if (count == 0) return [];
        var output = new PhysicsShapeResult[count];
        hits.CopyTo(0, output, 0, count);
        return output;
    }

    /// <summary>Copies ordered unique collider shape-owner hits into caller-owned storage.</summary>
    /// <param name="parameters">The live shape, pose, motion, margin and filters.</param>
    /// <param name="results">Destination storage; its length is the maximum result count.</param>
    /// <returns>The written count. Remaining elements are unchanged; excess hits are truncated in RID/index order.</returns>
    /// <remarks>Reuses query scratch capacity after preparation; no output array is created. Ownership and query errors match the array overload.</remarks>
    /// <exception cref="ArgumentNullException">Parameters are null.</exception>
    /// <exception cref="ArgumentException">The shape RID is invalid or query geometry is invalid.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or the space is stepping.</exception>
    /// <exception cref="ObjectDisposedException">The view or required shape/parameters are disposed.</exception>
    public int IntersectShape(PhysicsShapeQueryParameters parameters, Span<PhysicsShapeResult> results)
    {
        var hits = CollectShapeHits(parameters);
        var count = Math.Min(results.Length, hits.Count);
        for (var i = 0; i < count; i++) results[i] = hits[i];
        return count;
    }

    private List<PhysicsShapeResult> CollectShapeHits(PhysicsShapeQueryParameters parameters)
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
            var other = b2MakeShapeDistanceProxy(backendShape);
            var otherTransform = b2Body_GetTransform(b2Shape_GetBody(candidate.ShapeID));
            for (var piece = 0; piece < _queryProxies.Count; piece++)
            {
                var query = _queryProxies[piece];
                if (_queryRaySlide is not null || candidate.Tag.SeparationRay is not null)
                {
                    if (PhysicsSeparationRay.PairContact(query, _queryRaySlide, other, otherTransform,
                        candidate.Tag.SeparationRay, motion, _queryMargin).pointCount == 0) continue;
                }
                else if (!Overlaps(query, other, otherTransform) &&
                         !SweepsInto(query, other, otherTransform, motion, 1f)) continue;
                hits.Add(new(candidate.Tag.ColliderRID,
                    PhysicsServer.Service.ResolveSceneObject(candidate.Tag.ColliderRID),
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

    /// <summary>Finds safe and unsafe fractions of a shape's requested global motion.</summary>
    /// <param name="parameters">A live query shape, pose, motion, margin and filters.</param>
    /// <returns>(1, 1) when no new collision occurs; initial overlaps are ignored.</returns>
    public (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters parameters)
    {
        var space = PrepareShapeQuery(parameters);
        var motion = PhysicsShapeBackend.ToBackend(parameters.Motion);
        if ((motion.X == 0 && motion.Y == 0) || _queryProxies.Count == 0 || _shapeCandidates.Count == 0)
            return (1, 1);
        var world = b2GetWorldFromId(space.WorldID);
        var bestSafe = 1f;
        var bestUnsafe = 1f;
        foreach (var candidate in _shapeCandidates)
        {
            var backendShape = b2GetShape(world, candidate.ShapeID);
            var other = b2MakeShapeDistanceProxy(backendShape);
            var otherTransform = b2Body_GetTransform(b2Shape_GetBody(candidate.ShapeID));
            for (var piece = 0; piece < _queryProxies.Count; piece++)
            {
                var query = _queryProxies[piece];
                if (_queryRaySlide is not null || candidate.Tag.SeparationRay is not null)
                {
                    if (PhysicsSeparationRay.PairContact(query, _queryRaySlide, other, otherTransform,
                            candidate.Tag.SeparationRay, default, _queryMargin).pointCount != 0) continue;
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
                if (Overlaps(query, other, otherTransform)) continue;
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

    private PhysicsSpace PrepareShapeQuery(PhysicsShapeQueryParameters parameters)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(parameters);
        var space = PhysicsServer.Service.GetSceneSpace(_spaceRID);
        space.PrepareForQuery();
        var shape = parameters.Shape ?? PhysicsServer.Service.GetShapeGeometry(parameters.ShapeRID);
        _queryRaySlide = (shape as SeparationRayShape)?.SlideOnSlope;
        _queryProxies.Clear();
        PhysicsShapeBackend.AppendQueryProxies(shape, _queryProxies);
        var transform = parameters.Transform;
        var backendTransform = new B2Transform(PhysicsShapeBackend.ToBackend(transform.Origin), b2MakeRot(transform.Rotation));
        var margin = parameters.Margin * PhysicsSpace.MetersPerUnit;
        _queryMargin = margin;
        for (var index = 0; index < _queryProxies.Count; index++)
        {
            var proxy = _queryProxies[index];
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
        return space;
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
