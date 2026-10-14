using Electron2D;

public static partial class BackendChecks
{
    private static Part GeometryPart(ShapeGeometry geometry, Transform transform, int index = 0, int element = 0)
    {
        if (geometry.Type == PhysicsServer.ShapeType.SeparationRay)
        {
            var data = geometry.GetSeparationRayParameters(); var edge = transform.Y * (data.Length / 2);
            return new(index, transform.Origin + edge, 0, edge.Length(), EdgeY: edge, Directed: true, Slide: data.SlideOnSlope);
        }
        if (geometry.Type == PhysicsServer.ShapeType.WorldBoundary)
        {
            var plane = geometry.GetWorldBoundaryPlane();
            var determinant = CrossEdges(transform.X, transform.Y);
            var normal = new Vector2(transform.Y.Y * plane.Normal.X - transform.X.Y * plane.Normal.Y,
                transform.X.X * plane.Normal.Y - transform.Y.X * plane.Normal.X) / determinant;
            return new(index, transform * (plane.Normal * plane.Distance), 0, 0, PlaneNormal: normal.Normalized());
        }
        if (geometry.Type == PhysicsServer.ShapeType.ConvexPolygon)
        {
            var polygon = Polygons.GetValue(geometry, static data => new Polygon(data));
            return new(index, transform * polygon.Center, 0, polygon.Radius, transform.X, transform.Y, 0, false, false, polygon);
        }
        if (geometry.Type == PhysicsServer.ShapeType.Rectangle)
        {
            var half = geometry.GetRectangleSize() / 2;
            var x = transform.X * half.X; var y = transform.Y * half.Y;
            var radius = MathF.Max((x + y).Length(), (x - y).Length());
            if (!x.IsFinite() || !y.IsFinite() || !float.IsFinite(radius) || CrossEdges(x, y) == 0)
                throw new NotSupportedException("The rectangle requires finite nondegenerate transformed edges.");
            return new(index, transform.Origin, 0, radius, x, y);
        }
        if (geometry.Type == PhysicsServer.ShapeType.ConcavePolygon)
        {
            var points = geometry.GetConcavePolygonSegments(); var a = transform * points[2 * element]; var b = transform * points[2 * element + 1];
            var edge = (b - a) / 2; var length = edge.Length();
            return new(index, (a + b) / 2, 0, length <= .25f ? 0 : length, default,
                length <= .25f ? default : edge, 0, false, true, null, true);
        }
        if (geometry.Type == PhysicsServer.ShapeType.Segment)
        {
            var endpoints = geometry.GetSegmentEndpoints(); var a = transform * endpoints.A; var b = transform * endpoints.B;
            var edge = (b - a) / 2;
            return new(index, (a + b) / 2, 0, edge.Length(), default, edge, 0, false, true);
        }
        var scale = transform.Scale.Abs();
        if (!Mathf.IsEqualApprox(scale.X, scale.Y) || !Mathf.IsZeroApprox(transform.Skew))
            throw new NotSupportedException("Round fixtures require uniform local scale.");
        if (geometry.Type == PhysicsServer.ShapeType.Capsule)
        {
            var capsule = geometry.GetCapsuleDimensions();
            var r = capsule.Radius * scale.X;
            var y = transform.Y * MathF.Max(0, capsule.Height / 2 - capsule.Radius);
            return new(index, transform.Origin, 0, y.Length() + r, default, y, r, true);
        }
        var dimensions = Annulus.Dimensions(geometry);
        return new(index, transform.Origin, dimensions.Inner * scale.X, dimensions.Outer * scale.X);
    }
    private static float CrossEdges(Vector2 x, Vector2 y) => x.X * y.Y - x.Y * y.X;
    private static Vector2 PartAt(in Part part, Transform pose) => pose * part.Offset;
    private static Vector2 EdgeX(in Part part, Transform pose) => pose.BasisXform(part.EdgeX);
    private static Vector2 EdgeY(in Part part, Transform pose) => pose.BasisXform(part.EdgeY);
    private static Vector2 Coefficients(Vector2 point, Vector2 x, Vector2 y) =>
        new(CrossEdges(point, y) / CrossEdges(x, y), CrossEdges(x, point) / CrossEdges(x, y));
    private static bool ContainsPart(in Part part, Transform pose, Vector2 point)
    {
        var center = PartAt(part, pose);
        if (part.Directed) return false;
        if (part.Boundary) return (point - center).Dot(BoundaryNormal(part, pose)) <= 0;
        if (part.Polygon is not null) return PolygonContains(part, pose, point);
        if (part.Segment) return false;
        if (part.Capsule) return point.DistanceSquaredTo(ClosestContour(part, pose, point)) <= part.Inflation * part.Inflation;
        if (!part.Contour) return Annulus.Contains(point, center, part.Inner, part.Outer);
        var v = Coefficients(point - center, EdgeX(part, pose), EdgeY(part, pose));
        return MathF.Abs(v.X) <= 1 && MathF.Abs(v.Y) <= 1;
    }
    private static void Corners(in Part part, Transform pose, Span<Vector2> output)
    {
        var center = PartAt(part, pose); var x = EdgeX(part, pose); var y = EdgeY(part, pose);
        output[0] = center - x - y; output[1] = center + x - y;
        output[2] = center + x + y; output[3] = center - x + y;
    }
    private static Vector2 ClosestEdge(Vector2 point, Vector2 a, Vector2 b)
    {
        var edge = b - a; return edge == Vector2.Zero ? a : a + edge * Math.Clamp((point - a).Dot(edge) / edge.LengthSquared(), 0, 1);
    }
    private static Vector2 ClosestContour(in Part part, Transform pose, Vector2 point)
    {
        if (part.Polygon is not null)
        {
            var nearest = PartAt(part, pose); var distanceToEdge = float.PositiveInfinity;
            for (var i = 0; i < CoreCount(part); i++)
            {
                var candidate = ClosestEdge(point, CoreVertex(part, pose, i), CoreVertex(part, pose, (i + 1) % CoreCount(part)));
                var squared = candidate.DistanceSquaredTo(point);
                if (squared < distanceToEdge) { nearest = candidate; distanceToEdge = squared; }
            }
            return nearest;
        }
        Span<Vector2> corners = stackalloc Vector2[4]; Corners(part, pose, corners);
        var best = corners[0]; var distance = float.PositiveInfinity;
        for (var i = 0; i < 4; i++)
        {
            var candidate = ClosestEdge(point, corners[i], corners[(i + 1) % 4]); var d = candidate.DistanceSquaredTo(point);
            if (d < distance) { distance = d; best = candidate; }
        }
        return best;
    }
    private static float ProjectionRadius(in Part part, Transform pose, Vector2 axis) => part.Contour
        ? MathF.Abs(EdgeX(part, pose).Dot(axis)) + MathF.Abs(EdgeY(part, pose).Dot(axis)) + part.Inflation
        : part.Outer;
    private static Vector2 LeadingPart(in Part part, Transform pose, Vector2 direction)
    {
        if (part.Polygon is not null)
        {
            var point = PartAt(part, pose); var maximum = float.NegativeInfinity;
            for (var i = 0; i < CoreCount(part); i++)
            { var vertex = CoreVertex(part, pose, i); var along = vertex.Dot(direction); if (along > maximum) { maximum = along; point = vertex; } }
            return point + direction * part.Inflation;
        }
        if (!part.Contour) return PartAt(part, pose) + direction * part.Outer;
        var x = EdgeX(part, pose); var y = EdgeY(part, pose);
        return PartAt(part, pose) + x * MathF.Sign(x.Dot(direction)) + y * MathF.Sign(y.Dot(direction)) + direction * part.Inflation;
    }
    private static void SupportRange(in Part part, Transform pose, Vector2 normal, Vector2 tangent, out float minimum, out float maximum)
    {
        if (part.Polygon is not null) { PolygonSupport(part, pose, normal, tangent, out minimum, out maximum); return; }
        var center = PartAt(part, pose);
        if (!part.Contour) { minimum = maximum = center.Dot(tangent); return; }
        var x = EdgeX(part, pose); var y = EdgeY(part, pose);
        var sx = x.Dot(normal); var sy = y.Dot(normal);
        var tx = .000001f * MathF.Max(1, x.Length()); var ty = .000001f * MathF.Max(1, y.Length());
        var along = center.Dot(tangent) + (MathF.Abs(sx) > tx ? x.Dot(tangent) * MathF.Sign(sx) : 0) +
            (MathF.Abs(sy) > ty ? y.Dot(tangent) * MathF.Sign(sy) : 0);
        var extent = (MathF.Abs(sx) <= tx ? MathF.Abs(x.Dot(tangent)) : 0) +
            (MathF.Abs(sy) <= ty ? MathF.Abs(y.Dot(tangent)) : 0);
        minimum = along - extent; maximum = along + extent;
    }
    private static float ContourGap(in Part first, Transform a, in Part second, Transform b,
        out Vector2 normal, out Vector2 pointA, out Vector2 pointB)
    {
        if (first.Polygon is not null || second.Polygon is not null) return PolygonGap(first, a, second, b, out normal, out pointA, out pointB);
        Span<Vector2> axes = stackalloc Vector2[40]; var count = 0;
        if (first.Contour) { var x = EdgeX(first, a); var y = EdgeY(first, a); axes[count++] = new(-x.Y, x.X); axes[count++] = new(-y.Y, y.X); }
        if (second.Contour) { var x = EdgeX(second, b); var y = EdgeY(second, b); axes[count++] = new(-x.Y, x.X); axes[count++] = new(-y.Y, y.X); }
        var ca = PartAt(first, a); var cb = PartAt(second, b);
        axes[count++] = ca == cb ? Vector2.Right : cb - ca;
        if (!first.Contour) axes[count++] = ca - ClosestContour(second, b, ca);
        else if (!second.Contour) axes[count++] = cb - ClosestContour(first, a, cb);
        else if (first.Inflation + second.Inflation > 0)
        {
            Span<Vector2> ac = stackalloc Vector2[4]; Span<Vector2> bc = stackalloc Vector2[4];
            Corners(first, a, ac); Corners(second, b, bc);
            for (var i = 0; i < 4; i++) for (var j = 0; j < 4; j++)
                {
                    axes[count++] = bc[j] - ClosestEdge(bc[j], ac[i], ac[(i + 1) % 4]);
                    axes[count++] = ac[i] - ClosestEdge(ac[i], bc[j], bc[(j + 1) % 4]);
                }
        }
        var gap = float.NegativeInfinity; normal = Vector2.Right;
        for (var i = 0; i < count; i++)
        {
            if (axes[i].LengthSquared() < 1e-16f) continue;
            var axis = axes[i].Normalized(); var difference = (cb - ca).Dot(axis);
            var separation = MathF.Abs(difference) - ProjectionRadius(first, a, axis) - ProjectionRadius(second, b, axis);
            if (separation > gap) { gap = separation; normal = difference >= 0 ? axis : -axis; }
        }
        var tangent = new Vector2(-normal.Y, normal.X);
        SupportRange(first, a, normal, tangent, out var alo, out var ahi);
        SupportRange(second, b, -normal, tangent, out var blo, out var bhi);
        var lo = MathF.Max(alo, blo); var hi = MathF.Min(ahi, bhi); var t = (lo + hi) / 2;
        pointA = normal * (ca.Dot(normal) + ProjectionRadius(first, a, normal)) + tangent * Math.Clamp(t, alo, ahi);
        pointB = normal * (cb.Dot(normal) - ProjectionRadius(second, b, normal)) + tangent * Math.Clamp(t, blo, bhi);
        return gap;
    }
    private static float PartGap(in Part first, Transform a, in Part second, Transform b,
        out Vector2 normal, out Vector2 pointA, out Vector2 pointB)
    {
        if (first.Boundary || second.Boundary) return BoundaryGap(first, a, second, b, out normal, out pointA, out pointB);
        if (first.Directed || second.Directed)
        {
            if (DirectedContact(first, a, Vector2.Zero, second, b, Vector2.Zero, out normal, out pointA, out pointB, out var depth)) return -depth;
            normal = pointA = pointB = default; return float.PositiveInfinity;
        }
        if (CoreCount(first) == 0 || CoreCount(second) == 0) { normal = pointA = pointB = default; return float.PositiveInfinity; }
        if (first.Contour || second.Contour)
        {
            if (first.Inner > 0 || second.Inner > 0)
                return ContourRingGap(first, a, second, b, out normal, out pointA, out pointB);
            return ContourGap(first, a, second, b, out normal, out pointA, out pointB);
        }
        var ca = PartAt(first, a); var cb = PartAt(second, b); var delta = cb - ca;
        var distance = delta.Length(); normal = distance > 0 ? delta / distance : Vector2.Right;
        var outer = distance - first.Outer - second.Outer;
        var inner = MathF.Max(first.Inner - second.Outer, second.Inner - first.Outer) - distance;
        if (inner > outer)
        {
            normal = -normal;
            pointA = ca + normal * (first.Inner > 0 ? -first.Inner : first.Outer);
            pointB = cb - normal * (second.Inner > 0 ? -second.Inner : second.Outer);
            return inner;
        }
        pointA = ca + normal * first.Outer; pointB = cb - normal * second.Outer; return outer;
    }
    private static float ContourRingGap(in Part first, Transform a, in Part second, Transform b,
        out Vector2 normal, out Vector2 pointA, out Vector2 pointB)
    {
        var box = first.Contour ? first : second; var boxPose = first.Contour ? a : b;
        var ring = first.Contour ? second : first; var ringPose = first.Contour ? b : a;
        var center = PartAt(ring, ringPose); var closest = ClosestContour(box, boxPose, center);
        var farthest = CoreVertex(box, boxPose, 0);
        for (var i = 1; i < CoreCount(box); i++)
        { var vertex = CoreVertex(box, boxPose, i); if (vertex.DistanceSquaredTo(center) > farthest.DistanceSquaredTo(center)) farthest = vertex; }
        var inner = ring.Inner - farthest.DistanceTo(center) - box.Inflation;
        var outer = (ContainsPart(box, boxPose, center) ? 0 : closest.DistanceTo(center)) - ring.Outer - box.Inflation;
        if (inner > outer)
        {
            var n = (farthest - center).Normalized(); normal = first.Contour ? n : -n;
            pointA = first.Contour ? farthest + n * box.Inflation : center + n * ring.Inner;
            pointB = first.Contour ? center + n * ring.Inner : farthest + n * box.Inflation;
            return inner;
        }
        return ContourGap(first with { Inner = 0 }, a, second with { Inner = 0 }, b, out normal, out pointA, out pointB);
    }
    private static bool SweepParts(in Part first, Transform a, Vector2 motion, in Part second, Transform b, out float fraction)
    {
        fraction = 0;
        if (first.Boundary || second.Boundary) return SweepBoundary(first, a, motion, second, b, out fraction);
        if (first.Directed || second.Directed) return SweepDirected(first, a, motion, second, b, out fraction);
        if (CoreCount(first) == 0 || CoreCount(second) == 0) return false;
        var gap = PartGap(first, a, second, b, out _, out _, out _);
        if (gap <= 0) return true;
        var speed = motion.Length(); if (speed == 0) return false;
        var current = a;
        for (var iteration = 0; iteration < 256; iteration++)
        {
            gap = PartGap(first, current, second, b, out var normal, out _, out _);
            if (gap <= .00001f) return true;
            // A hollow inner boundary can close later even when its initial radial speed is zero.
            var closing = first.Inner > 0 || second.Inner > 0 ? speed : motion.Dot(normal);
            if (closing <= 0) return false;
            var advance = gap / closing;
            if (!float.IsFinite(advance) || advance <= 0) throw new InvalidOperationException("A contour sweep cannot advance.");
            fraction += advance; if (fraction > 1) return false;
            current.Origin = a.Origin + motion * fraction;
        }
        throw new InvalidOperationException("A contour sweep exceeded its advancement budget.");
    }
    internal static bool CollideContourGeometry(ShapeGeometry first, Transform firstPose, Vector2 firstMotion,
        ShapeGeometry second, Transform secondPose, Vector2 secondMotion, Span<Vector2> output, out int count)
    {
        var a = GeometryPart(first, firstPose); var b = GeometryPart(second, secondPose);
        return CollidePartsRegions(a, firstMotion, b, secondMotion, output, out count);
    }
    private static bool CollidePartsRegions(in Part a, Vector2 firstMotion, in Part b, Vector2 secondMotion, Span<Vector2> output, out int count)
    {
        if (a.Polygon is not null || b.Polygon is not null) return CollidePolygonRegions(a, firstMotion, b, secondMotion, output, out count);
        Span<Vector2> av = stackalloc Vector2[8]; Span<Vector2> bv = stackalloc Vector2[8];
        var ac = Vertices(a, firstMotion, av); var bc = Vertices(b, secondMotion, bv);
        Span<Vector2> axes = stackalloc Vector2[584]; var axisCount = 1; axes[0] = Vector2.Right;
        if (a.Contour) { axes[axisCount++] = new(-a.EdgeX.Y, a.EdgeX.X); axes[axisCount++] = new(-a.EdgeY.Y, a.EdgeY.X); }
        if (b.Contour) { axes[axisCount++] = new(-b.EdgeX.Y, b.EdgeX.X); axes[axisCount++] = new(-b.EdgeY.Y, b.EdgeY.X); }
        if (firstMotion != Vector2.Zero) axes[axisCount++] = new(-firstMotion.Y, firstMotion.X);
        if (secondMotion != Vector2.Zero) axes[axisCount++] = new(-secondMotion.Y, secondMotion.X);
        if (a.Inflation + b.Inflation > 0 || !a.Contour || !b.Contour)
        {
            for (var i = 0; i < ac; i++) for (var j = 0; j < bc; j++) for (var k = j; k < bc; k++)
                        axes[axisCount++] = av[i] - (j == k ? bv[j] : ClosestEdge(av[i], bv[j], bv[k]));
            for (var i = 0; i < bc; i++) for (var j = 0; j < ac; j++) for (var k = j; k < ac; k++)
                        axes[axisCount++] = bv[i] - (j == k ? av[j] : ClosestEdge(bv[i], av[j], av[k]));
        }
        var best = float.NegativeInfinity; var normal = Vector2.Right;
        for (var i = 0; i < axisCount; i++)
        {
            if (axes[i].LengthSquared() < 1e-16f) continue;
            var n = axes[i].Normalized();
            var ar = ProjectionRadius(a, Transform.Identity, n); var br = ProjectionRadius(b, Transform.Identity, n);
            var amin = a.Offset.Dot(n) - ar + MathF.Min(0, firstMotion.Dot(n));
            var amax = a.Offset.Dot(n) + ar + MathF.Max(0, firstMotion.Dot(n));
            var bmin = b.Offset.Dot(n) - br + MathF.Min(0, secondMotion.Dot(n));
            var bmax = b.Offset.Dot(n) + br + MathF.Max(0, secondMotion.Dot(n));
            var forward = bmin - amax; var backward = amin - bmax; var gap = MathF.Max(forward, backward);
            if (gap > best) { best = gap; normal = forward >= backward ? n : -n; }
        }
        count = 0; if (best > 0) return false;
        var tangent = new Vector2(-normal.Y, normal.X);
        SweptRange(a, firstMotion, normal, tangent, out var alo, out var ahi);
        SweptRange(b, secondMotion, -normal, tangent, out var blo, out var bhi);
        var t = (MathF.Max(alo, blo) + MathF.Min(ahi, bhi)) / 2;
        var pointA = normal * (a.Offset.Dot(normal) + ProjectionRadius(a, Transform.Identity, normal) + MathF.Max(0, firstMotion.Dot(normal))) + tangent * Math.Clamp(t, alo, ahi);
        var pointB = normal * (b.Offset.Dot(normal) - ProjectionRadius(b, Transform.Identity, normal) + MathF.Min(0, secondMotion.Dot(normal))) + tangent * Math.Clamp(t, blo, bhi);
        if (a.Inner > 0 || b.Inner > 0)
        {
            var ring = a.Inner > 0 ? a : b; var ringMotion = a.Inner > 0 ? firstMotion : secondMotion;
            var box = a.Inner > 0 ? bv[..bc] : av[..ac];
            var distance = 0f; var vertex = box[0]; var center = ring.Offset;
            foreach (var corner in box)
                for (var end = 0; end < 2; end++)
                {
                    var endpoint = ring.Offset + ringMotion * end; var value = corner.DistanceTo(endpoint);
                    if (value > distance) { distance = value; vertex = corner; center = endpoint; }
                }
            var rounded = a.Inner > 0 ? b.Inflation : a.Inflation;
            var innerGap = ring.Inner - distance - rounded;
            if (innerGap > 0) return false;
            if (innerGap > best)
            {
                var boundary = center + (vertex - center).Normalized() * ring.Inner;
                var tip = vertex + (vertex - center).Normalized() * rounded;
                pointA = a.Contour ? tip : boundary; pointB = a.Contour ? boundary : tip;
            }
        }
        if (output.Length >= 2) { output[0] = pointA; output[1] = pointB; count = 1; }
        return true;
        static int Vertices(in Part part, Vector2 motion, Span<Vector2> vertices)
        {
            if (!part.Contour) { vertices[0] = part.Offset; vertices[1] = part.Offset + motion; return 2; }
            Corners(part, Transform.Identity, vertices);
            for (var i = 0; i < 4; i++) vertices[i + 4] = vertices[i] + motion;
            return 8;
        }
        static void SweptRange(in Part part, Vector2 motion, Vector2 n, Vector2 t, out float lo, out float hi)
        {
            SupportRange(part, Transform.Identity, n, t, out lo, out hi);
            if (MathF.Abs(motion.Dot(n)) < .000001f) { lo += MathF.Min(0, motion.Dot(t)); hi += MathF.Max(0, motion.Dot(t)); }
            else if (motion.Dot(n) > 0) { lo += motion.Dot(t); hi += motion.Dot(t); }
        }
    }
    private static bool SweptContour(Body a, in Part first, Body b, in Part second, float duration, bool full,
        float spinA, float spinB, out float fraction)
    {
        var initialA = a.Pose;
        var linearA = MotionForContour(a); var linearB = MotionForContour(b);
        var armA = first.Offset - (a.Dynamic ? a.LocalCenter : Vector2.Zero);
        var armB = second.Offset - (b.Dynamic ? b.LocalCenter : Vector2.Zero);
        var speed = (linearA - linearB).Length() + MathF.Abs(spinA) * (armA.Length() + (first.Contour ? first.Outer : 0)) +
            MathF.Abs(spinB) * (armB.Length() + (second.Contour ? second.Outer : 0));
        var ray = full ? first : new Part(first.Index, LeadingPart(first, initialA, (linearA - linearB).Normalized()), 0, 0);
        var tolerance = .0001f * MathF.Min(first.Thickness, second.Thickness);
        var time = 0f;
        for (var iteration = 0; iteration < 1024; iteration++)
        {
            var poseA = MovingPose(a, linearA, spinA, time); var poseB = MovingPose(b, linearB, spinB, time);
            var gap = PartGap(ray, full ? poseA : Transform.Identity.Translated(linearA * time), second, poseB, out var axis, out _, out _);
            if (gap <= tolerance) { fraction = time / duration; return true; }
            if (!(speed > 0)) { fraction = 1; return false; }
            var advance = gap / (double)speed;
            if (first.Inner == 0 && second.Inner == 0)
            {
                var rayPose = full ? poseA : Transform.Identity.Translated(linearA * time);
                var ac = CoreCount(ray); var bc = CoreCount(second); advance = double.PositiveInfinity;
                var radius = (ray.Contour ? ray.Inflation : ray.Outer) + (second.Contour ? second.Inflation : second.Outer);
                var centerA = a.Center + linearA * time; var centerB = b.Center + linearB * time;
                for (var i = 0; i < ac; i++) for (var j = 0; j < bc; j++)
                    {
                        var ap = CoreVertex(ray, rayPose, i); var bp = CoreVertex(second, poseB, j);
                        var ra = ap - centerA; var rb = bp - centerB;
                        var wa = full ? spinA : 0;
                        var va = linearA + new Vector2(-ra.Y, ra.X) * wa;
                        var vb = linearB + new Vector2(-rb.Y, rb.X) * spinB;
                        var separation = MathF.Max(0, (bp - ap).Dot(axis) - radius);
                        var closing = (va - vb).Dot(axis);
                        var acceleration = (double)wa * wa * ra.Length() + (double)spinB * spinB * rb.Length();
                        double bound;
                        if (acceleration == 0) bound = closing > 0 ? separation / (double)closing : double.PositiveInfinity;
                        else
                        {
                            var root = Math.Sqrt((double)closing * closing + 2 * acceleration * separation);
                            bound = closing >= 0 ? 2 * separation / (closing + root) : (root - closing) / acceleration;
                        }
                        advance = Math.Min(advance, bound);
                    }
            }
            var next = time + (float)advance;
            if (next > duration) { fraction = 1; return false; }
            if (!(next > time)) throw new InvalidOperationException("A rotating contour sweep cannot advance.");
            time = next;
        }
        throw new InvalidOperationException("A rotating contour sweep exceeded its advancement budget.");
        static Vector2 MotionForContour(Body body) => body.IsArea || body.Mode == PhysicsServer.BodyMode.Static || body.Dynamic && !body.Awake ? Vector2.Zero : body.Velocity;
        static Transform MovingPose(Body body, Vector2 linear, float spin, float time)
        {
            var rotation = body.Rotation + spin * time; var center = body.Dynamic ? body.LocalCenter : Vector2.Zero;
            return new(rotation, body.Position + linear * time + center.Rotated(body.Rotation) - center.Rotated(rotation));
        }
    }
    private static bool RayPart(in Part part, Transform pose, Vector2 from, Vector2 motion, out float fraction, out Vector2 normal)
    {
        if (part.Directed) { fraction = 1; normal = default; return false; }
        if (part.Boundary) return RayBoundary(part, pose, from, motion, out fraction, out normal);
        if (part.Polygon is not null) return RayPolygon(part, pose, from, motion, out fraction, out normal);
        if (part.Segment && part.EdgeY != Vector2.Zero) return RaySegment(part, pose, from, motion, out fraction, out normal);
        if (part.Capsule || part.Segment) return RayCapsule(part, pose, from, motion, out fraction, out normal);
        if (!part.Contour)
        { var hit = Annulus.Sweep(from, motion, PartAt(part, pose), part.Inner, part.Outer, out fraction); normal = Solver.PartNormal(from + motion * fraction, PartAt(part, pose), part); return hit; }
        var x = EdgeX(part, pose); var y = EdgeY(part, pose); var start = Coefficients(from - PartAt(part, pose), x, y);
        var delta = Coefficients(motion, x, y); var near = 0f; var far = 1f; normal = Vector2.Zero;
        for (var axis = 0; axis < 2; axis++)
        {
            var origin = axis == 0 ? start.X : start.Y; var d = axis == 0 ? delta.X : delta.Y;
            if (d == 0) { if (MathF.Abs(origin) > 1) { fraction = 1; return false; } continue; }
            var low = (-1 - origin) / d; var high = (1 - origin) / d;
            if (low > high) (low, high) = (high, low);
            if (low > near)
            {
                near = low; var outward = axis == 0 ? new Vector2(y.Y, -y.X) : new Vector2(-x.Y, x.X);
                normal = outward.Normalized() * MathF.Sign(CrossEdges(x, y)) * (d > 0 ? -1 : 1);
            }
            far = MathF.Min(far, high); if (near > far) { fraction = 1; return false; }
        }
        fraction = near; return near <= 1 && far >= 0;
    }
}
