using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Constants;

namespace Box2D.NET;

// Analytic half-planes never enter the bounded support-map iterations.
internal static class B2Boundaries
{
    internal static bool Overlap(B2World world, B2Shape a, B2Shape b)
    {
        if (a.type != B2ShapeType.b2_boundaryShape && b.type != B2ShapeType.b2_boundaryShape)
            return b2AABB_Overlaps(a.fatAABB, b.fatAABB);
        if (a.type == b.type) return false;
        var source = a.type == B2ShapeType.b2_boundaryShape ? a : b; var other = ReferenceEquals(source, a) ? b : a;
        var plane = Transform(source.us.boundary, B2Bodies.b2GetBodyTransform(world, source.bodyId));
        var box = other.fatAABB;
        var nearest = new B2Vec2(plane.normal.X >= 0 ? box.lowerBound.X : box.upperBound.X,
            plane.normal.Y >= 0 ? box.lowerBound.Y : box.upperBound.Y);
        return b2Dot(plane.normal, nearest) <= plane.offset;
    }
    internal static B2Plane Transform(B2Plane plane, in B2Transform pose)
    {
        var normal = b2RotateVector(pose.q, plane.normal);
        return new(normal, plane.offset + b2Dot(normal, pose.p));
    }
    internal static B2ShapeProxy Proxy(B2Plane plane) => new() { count = 1, isBoundary = true, boundary = plane };
    internal static bool Contains(B2Plane plane, B2Vec2 point) => b2Dot(plane.normal, point) <= plane.offset;
    internal static B2CastOutput Ray(B2Plane plane, in B2RayCastInput input)
    {
        var gap = b2Dot(plane.normal, input.origin) - plane.offset;
        var rate = b2Dot(plane.normal, input.translation);
        if (gap < 0 || rate >= 0) return default;
        var fraction = -gap / rate;
        return fraction <= input.maxFraction ? new() { hit = true, fraction = fraction, normal = plane.normal,
            point = input.origin + fraction * input.translation } : default;
    }
    internal static float Separation(in B2ShapeProxy a, in B2Transform poseA, in B2ShapeProxy b, in B2Transform poseB,
        bool radii, out B2Vec2 pointA, out B2Vec2 pointB, out B2Vec2 normal)
    {
        if (a.isBoundary && b.isBoundary) { pointA = pointB = normal = default; return float.MaxValue; }
        var flip = b.isBoundary; var planeProxy = flip ? b : a; var other = flip ? a : b;
        var plane = Transform(planeProxy.boundary, flip ? poseB : poseA); var pose = flip ? poseA : poseB;
        var offset = plane.offset + (radii ? planeProxy.radius : 0);
        var point = b2TransformPoint(pose, other.points[0]); var minimum = b2Dot(plane.normal, point);
        for (var i = 1; i < other.count; i++)
        {
            var p = b2TransformPoint(pose, other.points[i]); var projection = b2Dot(plane.normal, p);
            if (projection < minimum) { minimum = projection; point = p; }
        }
        point -= (radii ? other.radius : 0) * plane.normal;
        var separation = minimum - (radii ? other.radius : 0) - offset;
        var onPlane = point - separation * plane.normal;
        pointA = flip ? point : onPlane; pointB = flip ? onPlane : point; normal = flip ? -plane.normal : plane.normal;
        return separation;
    }
    internal static B2DistanceOutput Distance(in B2DistanceInput input)
    {
        var gap = Separation(input.proxyA, input.transformA, input.proxyB, input.transformB, input.useRadii,
            out var a, out var b, out var normal);
        if (gap < 0) a = b = input.proxyA.isBoundary ? b : a;
        return new() { pointA = a, pointB = b, normal = normal, distance = MathF.Max(0, gap) };
    }
    internal static B2CastOutput Cast(in B2ShapeCastPairInput input)
    {
        if (input.proxyA.isBoundary && input.proxyB.isBoundary) return default;
        var gap = Separation(input.proxyA, input.transformA, input.proxyB, input.transformB, true, out _, out _, out var normal);
        var rate = b2Dot(normal, input.translationB);
        if (gap <= 0 || rate >= 0) return default;
        var fraction = -gap / rate; if (fraction > input.maxFraction) return default;
        var pose = input.transformB; pose.p += fraction * input.translationB;
        Separation(input.proxyA, input.transformA, input.proxyB, pose, true, out var point, out _, out normal);
        return new() { hit = true, fraction = fraction, point = point, normal = normal };
    }
    internal static B2Manifold Contact(in B2ShapeProxy a, in B2Transform poseA, in B2ShapeProxy b, in B2Transform poseB, float limit = float.NaN)
    {
        if (a.isBoundary && b.isBoundary) return default;
        if (float.IsNaN(limit)) limit = B2_SPECULATIVE_DISTANCE;
        var flip = b.isBoundary; var other = flip ? a : b; var pose = flip ? poseA : poseB;
        var planeProxy = flip ? b : a; var plane = Transform(planeProxy.boundary, flip ? poseB : poseA);
        plane.offset += planeProxy.radius;
        var normal = flip ? -plane.normal : plane.normal;
        var minimum = float.MaxValue;
        for (var i = 0; i < other.count; i++) minimum = MathF.Min(minimum, b2Dot(plane.normal, b2TransformPoint(pose, other.points[i])));
        if (minimum - plane.offset - other.radius > limit) return default;
        var manifold = new B2Manifold { normal = normal };
        var first = -1; var last = -1; var lo = float.MaxValue; var hi = -float.MaxValue;
        var tangent = new B2Vec2(-plane.normal.Y, plane.normal.X);
        for (var i = 0; i < other.count; i++)
        {
            var p = b2TransformPoint(pose, other.points[i]);
            if (b2Dot(plane.normal, p) > minimum + B2_LINEAR_SLOP) continue;
            var along = b2Dot(tangent, p);
            if (along < lo) { lo = along; first = i; } if (along > hi) { hi = along; last = i; }
        }
        for (var slot = 0; slot < (hi - lo > B2_LINEAR_SLOP ? 2 : 1); slot++)
        {
            var vertex = slot == 0 ? first : last;
            var p = b2TransformPoint(pose, other.points[vertex]) - other.radius * plane.normal;
            var separation = b2Dot(plane.normal, p) - plane.offset;
            var midpoint = p - .5f * separation * plane.normal;
            manifold.points[slot] = new() { point = midpoint, anchorA = midpoint - poseA.p, anchorB = midpoint - poseB.p,
                separation = separation, id = (ushort)vertex };
            manifold.pointCount++;
        }
        return manifold;
    }
    internal static float SweepRate(in B2TOIInput input)
    {
        var flip = input.proxyB.isBoundary; var planeSweep = flip ? input.sweepB : input.sweepA;
        var otherSweep = flip ? input.sweepA : input.sweepB; var other = flip ? input.proxyA : input.proxyB;
        var radius = 0f;
        for (var i = 0; i < other.count; i++) radius = MathF.Max(radius, b2Length(other.points[i] - otherSweep.localCenter));
        var wp = 2 * MathF.Abs(MathF.Tan(.5f * b2RelativeAngle(planeSweep.q2, planeSweep.q1)));
        var wo = 2 * MathF.Abs(MathF.Tan(.5f * b2RelativeAngle(otherSweep.q2, otherSweep.q1)));
        var rel0 = otherSweep.c1 - planeSweep.c1; var rel1 = otherSweep.c2 - planeSweep.c2;
        var n = b2RotateVector(planeSweep.q1, (flip ? input.proxyB : input.proxyA).boundary.normal);
        return (wp == 0 ? MathF.Max(0, -b2Dot(n, rel1 - rel0)) : b2Length(rel1 - rel0)) +
            wo * radius + wp * (MathF.Max(b2Length(rel0), b2Length(rel1)) + radius);
    }

    internal static B2TOIOutput TimeOfImpact(in B2TOIInput input)
    {
        var result = new B2TOIOutput { state = B2TOIState.b2_toiStateSeparated, fraction = input.maxFraction };
        if (input.proxyA.isBoundary && input.proxyB.isBoundary) return result;
        var rate = SweepRate(input);
        if (!float.IsFinite(rate)) return new() { state = B2TOIState.b2_toiStateFailed };
        var t = 0f;
        for (var iteration = 0; iteration < 256; iteration++)
        {
            var a = B2Distances.b2GetSweepTransform(input.sweepA, t); var b = B2Distances.b2GetSweepTransform(input.sweepB, t);
            var gap = Separation(input.proxyA, a, input.proxyB, b, true, out _, out _, out var normal);
            if (!float.IsFinite(gap)) return new() { state = B2TOIState.b2_toiStateFailed };
            if (gap <= B2_LINEAR_SLOP)
                return new() { state = t == 0 && gap < 0 ? B2TOIState.b2_toiStateOverlapped : B2TOIState.b2_toiStateHit, fraction = t, normal = normal };
            if (rate == 0 || gap - B2_LINEAR_SLOP > rate * (input.maxFraction - t)) return result;
            var next = t + .9f * (gap - .5f * B2_LINEAR_SLOP) / rate;
            if (next >= input.maxFraction) return result;
            if (!(next > t)) return new() { state = B2TOIState.b2_toiStateFailed };
            t = next;
        }
        return new() { state = B2TOIState.b2_toiStateFailed };
    }

}
