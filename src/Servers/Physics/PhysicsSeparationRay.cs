using Box2D.NET;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;

namespace Electron2D;

internal static class PhysicsSeparationRay
{
    // Receives body-local fixture geometry and returns world-space normals/points with origin-relative anchors.
    internal static B2Manifold SolverContact(B2Shape a, in B2Transform poseA, B2Shape b, in B2Transform poseB, ref B2SimplexCache cache)
        => SolverContact(a, poseA, b, poseB, 0);

    internal static B2Manifold SolverContact(B2Shape a, in B2Transform poseA, B2Shape b, in B2Transform poseB, float margin)
    {
        var tagA = a.userData.GetRef<PhysicsFixtureTag>(); var tagB = b.userData.GetRef<PhysicsFixtureTag>();
        var rayA = tagA?.SeparationRay; var rayB = tagB?.SeparationRay;
        if ((rayA is null) == (rayB is null)) return default;
        var flip = rayA is null; var ray = (rayA ?? rayB)!.Value;
        var rayPose = flip ? poseB : poseA; var otherPose = flip ? poseA : poseB;
        var other = flip ? a : b; var tag = flip ? tagA : tagB;
        var worldRay = WorldProxy(ray, rayPose, default);
        if (other.type == B2ShapeType.b2_boundaryShape)
        {
            if (b2LengthSquared(ray.To - ray.From) == 0) return default;
            var result = B2Boundaries.Contact(worldRay, b2Transform_identity, B2Shapes.b2MakeShapeDistanceProxy(other), otherPose, margin);
            if (flip) result.normal = -result.normal;
            for (var i = 0; i < result.pointCount; i++)
            {
                ref var p = ref result.points[i]; p.anchorA = p.point - poseA.p; p.anchorB = p.point - poseB.p;
            }
            return result;
        }
        B2Manifold contact;
        if (tag is { Compound: { } contour })
        {
            if (!contour.Source.TryGetTarget(out var source)) throw new ObjectDisposedException(nameof(ConvexPolygonShape));
            var local = contour.LocalPose;
            var pose = b2MulTransforms(otherPose, new B2Transform(PhysicsShapeBackend.ToBackend(local.Origin), new B2Rot(MathF.Cos(local.Rotation), MathF.Sin(local.Rotation))));
            var scenePose = new Transform(new(pose.q.c, pose.q.s), new(-pose.q.s, pose.q.c), new(pose.p.X * PhysicsSpace.UnitsPerMeter, pose.p.Y * PhysicsSpace.UnitsPerMeter));
            if (PhysicsShapeCollision.FullMotionRegionContains(contour.Points, scenePose, Vector2.Zero, 0, worldRay.points[0])) return default;
            // ponytail: scan cached partitions to select one outer entry; add a contour cast if measured large-hull cost warrants it.
            var hulls = PhysicsShapeBackend.GetHulls(source, contour.Points);
            contact = default; var selected = -1;
            for (var i = 0; i < hulls.Length; i++)
            {
                var candidate = Contact(worldRay, ray.SlideOnSlope, new B2ShapeProxy { points = hulls[i].points, count = hulls[i].count }, pose, default, margin);
                if (candidate.pointCount != 0 && (selected < 0 || candidate.points[0].separation < contact.points[0].separation))
                { contact = candidate; selected = i; }
            }
            if (selected != tag.CompoundPiece) return default;
        }
        else contact = Contact(worldRay, ray.SlideOnSlope, B2Shapes.b2MakeShapeDistanceProxy(other), otherPose, default, margin);
        if (contact.pointCount == 0) return contact;
        if (flip) contact.normal = -contact.normal;
        ref var point = ref contact.points[0];
        point.anchorA = point.point - poseA.p; point.anchorB = point.point - poseB.p;
        return contact;
    }

    internal const float MinimumFacingProjection = 1e-6f;
    internal static B2Manifold PairContact(in B2ShapeProxy query, bool? queryRay,
        in B2ShapeProxy other, in B2Transform otherTransform, SeparationRayData? otherRay,
        in B2Vec2 motion, float margin)
    {
        if (query.isBoundary || other.isBoundary)
        {
            var target = otherRay is { } data ? WorldProxy(data, otherTransform, default) : other;
            var targetPose = otherRay is not null ? b2Transform_identity : otherTransform;
            if (queryRay is not null && b2LengthSquared(query.points[1] - query.points[0]) == 0 ||
                otherRay is { } empty && b2LengthSquared(empty.To - empty.From) == 0) return default;
            var moved = query;
            if (queryRay is not null) moved.points[1] += margin * b2Normalize(moved.points[1] - moved.points[0]);
            for (var i = 0; i < moved.count; i++) moved.points[i] += motion;
            if (moved.isBoundary) moved.boundary.offset += b2Dot(moved.boundary.normal, motion);
            return B2Boundaries.Contact(moved, b2Transform_identity, target, targetPose, 0);
        }
        if (queryRay is { } slide)
            return otherRay is null ? Contact(query, slide, other, otherTransform, motion, margin) : default;
        if (otherRay is not { } ray) return default;
        var worldRay = WorldProxy(ray, otherTransform, default);
        var manifold = SweptContact(worldRay, ray.SlideOnSlope, query, motion);
        manifold.normal = -manifold.normal;
        return manifold;
    }

    private static B2Manifold SweptContact(in B2ShapeProxy ray, bool slide, in B2ShapeProxy query,
        in B2Vec2 motion)
    {
        if (motion.X == 0 && motion.Y == 0) return Contact(ray, slide, query, b2Transform_identity, default);
        Span<B2ShapeProxy> pieces = stackalloc B2ShapeProxy[10];
        var count = SweptPieces(query, motion, pieces);
        var best = default(B2Manifold);
        for (var index = 0; index < count; index++)
        {
            var piece = pieces[index];
            // A composite cast must reject containment in any piece before selecting another piece's entry.
            if (Contains(piece, ray.points[0])) return default;
            var contact = Contact(ray, slide, piece, b2Transform_identity, default);
            if (contact.pointCount != 0 && (best.pointCount == 0 ||
                contact.points[0].separation < best.points[0].separation)) best = contact;
        }
        return best;
    }

    internal static bool ContainsSweptRegion(in B2ShapeProxy query, in B2Vec2 point, in B2Vec2 motion)
    {
        if (motion.X == 0 && motion.Y == 0) return Contains(query, point);
        Span<B2ShapeProxy> pieces = stackalloc B2ShapeProxy[10];
        var count = SweptPieces(query, motion, pieces);
        for (var i = 0; i < count; i++) if (Contains(pieces[i], point)) return true;
        return false;
    }

    private static int SweptPieces(in B2ShapeProxy query, in B2Vec2 motion, Span<B2ShapeProxy> pieces)
    {
        // The swept convex region is the union of the initial/final shape and each swept edge.
        // Reuse native primitives without exceeding their eight-vertex hull capacity.
        var count = 0;
        if (query.count == 1)
            pieces[count++] = b2MakeProxy(query.points[0], query.points[0] + motion, 2, query.radius);
        else
        {
            pieces[count++] = query;
            var end = query;
            for (var index = 0; index < end.count; index++) end.points[index] += motion;
            pieces[count++] = end;
            var edgeCount = query.count == 2 ? 1 : query.count;
            for (var index = 0; index < edgeCount; index++)
                pieces[count++] = SweptEdge(query.points[index], query.points[(index + 1) % query.count], motion, query.radius);
        }
        return count;
    }

    private static B2ShapeProxy SweptEdge(in B2Vec2 a, in B2Vec2 b, in B2Vec2 motion, float radius)
    {
        var cross = b2Cross(b - a, motion);
        if (cross == 0)
        {
            var from = a;
            var to = b;
            var axis = b - a;
            if (b2Dot(axis, motion) < 0) from += motion;
            else to += motion;
            return b2MakeProxy(from, to, 2, radius);
        }
        Span<B2Vec2> vertices = stackalloc B2Vec2[4];
        vertices[0] = a;
        vertices[1] = cross > 0 ? b : a + motion;
        vertices[2] = b + motion;
        vertices[3] = cross > 0 ? a + motion : b;
        return b2MakeProxy(vertices, 4, radius);
    }

    private static bool Contains(in B2ShapeProxy proxy, in B2Vec2 point)
    {
        if (proxy.count == 1)
            return b2PointInCircle(new B2Circle { center = proxy.points[0], radius = proxy.radius }, point);
        if (proxy.count == 2)
            return b2PointInCapsule(new B2Capsule(proxy.points[0], proxy.points[1], proxy.radius), point);
        var polygon = b2MakePolygon(new B2Hull { points = proxy.points, count = proxy.count }, proxy.radius);
        return b2PointInPolygon(ref polygon, point);
    }

    internal static B2ShapeProxy WorldProxy(in SeparationRayData ray, in B2Transform transform, in B2Vec2 offset) =>
        b2MakeProxy(b2TransformPoint(transform, ray.From) + offset,
            b2TransformPoint(transform, ray.To) + offset, 2, 0);

    internal static B2Manifold Contact(in B2ShapeProxy ray, bool slideOnSlope, in B2ShapeProxy other,
        in B2Transform otherTransform, in B2Vec2 motion, float margin = 0)
    {
        var from = ray.points[0];
        var direction = ray.points[1] - from;
        var length = b2Length(direction);
        if (length == 0) return default;
        direction *= 1f / length;
        var endpoint = from + direction * (length + margin + MathF.Max(0, b2Dot(direction, motion)));
        var translation = endpoint - from;
        if (!float.IsFinite(endpoint.X) || !float.IsFinite(endpoint.Y) || !float.IsFinite(b2LengthSquared(translation)))
            throw new ArgumentOutOfRangeException(nameof(motion), "Directed query geometry exceeds the backend squared-distance range.");
        var input = new B2RayCastInput
        {
            origin = b2InvTransformPoint(otherTransform, from),
            translation = b2InvRotateVector(otherTransform.q, translation),
            maxFraction = 1f
        };
        B2CastOutput hit;
        if (other.count == 1)
            hit = b2RayCastCircle(new B2Circle { center = other.points[0], radius = other.radius }, input);
        else if (other.count == 2)
            hit = other.radius > 0
                ? b2RayCastCapsule(new B2Capsule(other.points[0], other.points[1], other.radius), input)
                : b2RayCastSegment(new B2Segment(other.points[0], other.points[1]), input, oneSided: false);
        else
        {
            var polygon = b2MakePolygon(new B2Hull { points = other.points, count = other.count }, other.radius);
            hit = b2RayCastPolygon(ref polygon, input);
        }
        if (!hit.hit) return default;
        var normal = b2RotateVector(otherTransform.q, hit.normal);
        if (b2Dot(normal, from - endpoint) < MinimumFacingProjection) return default;
        var point = b2TransformPoint(otherTransform, hit.point);
        var depth = b2Length(endpoint - point);
        var contactPoint = slideOnSlope ? endpoint + depth * normal : point;
        var away = slideOnSlope ? normal : -direction;
        var manifold = new B2Manifold { normal = -away, pointCount = 1 };
        manifold.points[0].point = (endpoint + contactPoint) * 0.5f;
        manifold.points[0].separation = -depth;
        return manifold;
    }
}
