using Box2D.NET;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2MathFunction;

namespace Electron2D;

internal static class PhysicsShapeCollision
{
    [ThreadStatic] private static Buffers? s_buffers;

    private sealed class Buffers
    {
        internal readonly List<B2ShapeProxy> First = [];
        internal readonly List<B2ShapeProxy> Second = [];
        internal Vector2[] Sorted = [];
        internal Vector2[] FirstHull = [];
        internal Vector2[] SecondHull = [];
    }

    internal static bool Evaluate(Shape first, Transform firstPose, Vector2 firstMotion, Shape second,
        Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count)
    {
        ArgumentNullException.ThrowIfNull(second);
        if (first.IsDisposed) throw new ObjectDisposedException(first.GetType().Name);
        if (second.IsDisposed) throw new ObjectDisposedException(second.GetType().Name);
        Validate(firstPose, firstMotion);
        Validate(secondPose, secondMotion);
        count = 0;
        if (first is ConcavePolygonShape && second is ConcavePolygonShape ||
            first is SeparationRayShape && second is SeparationRayShape) return false;
        var buffers = s_buffers ??= new();
        var fullFirst = Prepare(first, buffers.First);
        var fullSecond = Prepare(second, buffers.Second);
        var firstCount = first is ConvexPolygonShape ? fullFirst.IsEmpty ? 0 : 1 : buffers.First.Count;
        var secondCount = second is ConvexPolygonShape ? fullSecond.IsEmpty ? 0 : 1 : buffers.Second.Count;
        var firstRay = first as SeparationRayShape;
        var secondRay = second as SeparationRayShape;
        if (first is ConcavePolygonShape || secondRay is not null) firstMotion = Vector2.Zero;
        if (second is ConcavePolygonShape || firstRay is not null) secondMotion = Vector2.Zero;
        var hit = false;
        var rayDepth = -1d;
        for (var a = 0; a < firstCount; a++)
        {
            var proxyA = first is ConvexPolygonShape ? default : buffers.First[a];
            var hullA = BuildHull(proxyA, fullFirst, firstPose, firstRay is null ? firstMotion : Vector2.Zero,
                buffers, ref buffers.FirstHull);
            var radiusA = proxyA.radius;
            for (var b = 0; b < secondCount; b++)
            {
                var proxyB = second is ConvexPolygonShape ? default : buffers.Second[b];
                var hullB = BuildHull(proxyB, fullSecond, secondPose, secondRay is null ? secondMotion : Vector2.Zero,
                    buffers, ref buffers.SecondHull);
                var radiusB = proxyB.radius;
                if (firstRay is not null || secondRay is not null)
                {
                    var rayIsFirst = firstRay is not null;
                    var rayProxy = rayIsFirst ? WorldProxy(proxyA, firstPose) : WorldProxy(proxyB, secondPose);
                    var otherHull = rayIsFirst ? hullB : hullA;
                    var otherRadius = rayIsFirst ? radiusB : radiusA;
                    if (!RayContact(rayProxy, (firstRay ?? secondRay)!.SlideOnSlope,
                        otherHull, otherRadius, rayIsFirst ? firstMotion : secondMotion,
                        out var rayPoint, out var otherPoint)) continue;
                    hit = true;
                    if (contacts.IsEmpty) return true;
                    var depth = DistanceSquared(rayPoint, otherPoint);
                    if (depth <= rayDepth) continue;
                    rayDepth = depth;
                    contacts[0] = rayIsFirst ? rayPoint : otherPoint;
                    contacts[1] = rayIsFirst ? otherPoint : rayPoint;
                    count = 1;
                    continue;
                }
                if (!Intersect(hullA, radiusA, hullB, radiusB, out var away, out var depthAmount)) continue;
                hit = true;
                if (contacts.IsEmpty) return true;
                AddContacts(hullA, radiusA, hullB, radiusB, away, depthAmount, contacts, ref count);
            }
        }
        return hit;
    }

    private static void Validate(Transform pose, Vector2 motion)
    {
        if (!pose.IsFinite() || !pose.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(pose.Skew))
            throw new ArgumentException("Shape collision requires a finite unit-scale, zero-skew pose.", nameof(pose));
        if (!motion.IsFinite()) throw new ArgumentOutOfRangeException(nameof(motion));
    }

    private static ReadOnlySpan<Vector2> Prepare(Shape shape, List<B2ShapeProxy> proxies)
    {
        proxies.Clear();
        var geometry = shape.GetGeometry();
        switch (geometry.Kind)
        {
            case PhysicsShapeGeometry.ShapeKind.ConvexPolygon:
                return geometry.Points;
            case PhysicsShapeGeometry.ShapeKind.Circle:
                proxies.Add(b2MakeProxy(default(B2Vec2), 1, geometry.Radius));
                break;
            case PhysicsShapeGeometry.ShapeKind.Capsule:
                proxies.Add(geometry.A == geometry.B ? b2MakeProxy(default(B2Vec2), 1, geometry.Radius) :
                    b2MakeProxy(new B2Vec2(geometry.A.X, geometry.A.Y), new B2Vec2(geometry.B.X, geometry.B.Y), 2, geometry.Radius));
                break;
            case PhysicsShapeGeometry.ShapeKind.Rectangle:
                Span<B2Vec2> corners = stackalloc B2Vec2[4]
                { new(geometry.A.X, geometry.A.Y), new(geometry.B.X, geometry.A.Y),
                    new(geometry.B.X, geometry.B.Y), new(geometry.A.X, geometry.B.Y) };
                proxies.Add(b2MakeProxy(corners, 4, 0));
                break;
            case PhysicsShapeGeometry.ShapeKind.SeparationRay:
                proxies.Add(b2MakeProxy(default, new B2Vec2(geometry.B.X, geometry.B.Y), 2, 0));
                break;
            case PhysicsShapeGeometry.ShapeKind.Segment:
                proxies.Add(SegmentProxy(geometry.A, geometry.B));
                break;
            case PhysicsShapeGeometry.ShapeKind.ConcavePolygon:
                for (var index = 0; index < geometry.Points.Length; index += 2)
                    proxies.Add(SegmentProxy(geometry.Points[index], geometry.Points[index + 1]));
                break;
            default: throw new NotSupportedException("The collision resource has no standalone geometry integration.");
        }
        return [];
    }

    private static B2ShapeProxy SegmentProxy(Vector2 a, Vector2 b) =>
        DistanceSquared(a, b) <= 0.25 ? b2MakeProxy(new B2Vec2(a.X + (b.X - a.X) * 0.5f,
            a.Y + (b.Y - a.Y) * 0.5f), 1, 0) :
        b2MakeProxy(new B2Vec2(a.X, a.Y), new B2Vec2(b.X, b.Y), 2, 0);

    private static ReadOnlySpan<Vector2> BuildHull(in B2ShapeProxy proxy, ReadOnlySpan<Vector2> full,
        Transform pose, Vector2 motion, Buffers buffers, ref Vector2[] output)
    {
        var sourceCount = full.IsEmpty ? proxy.count : full.Length;
        var size = checked(sourceCount * 2);
        if (buffers.Sorted.Length < size) Array.Resize(ref buffers.Sorted, size);
        if (output.Length < checked(size * 2)) Array.Resize(ref output, checked(size * 2));
        var sorted = buffers.Sorted.AsSpan(0, size);
        for (var index = 0; index < sourceCount; index++)
        {
            var local = full.IsEmpty ? new Vector2(proxy.points[index].X, proxy.points[index].Y) : full[index];
            var point = pose * local;
            var end = point + motion;
            if (!point.IsFinite() || !end.IsFinite() ||
                !new Vector2(MathF.Abs(point.X) + proxy.radius, MathF.Abs(point.Y) + proxy.radius).IsFinite() ||
                !new Vector2(MathF.Abs(end.X) + proxy.radius, MathF.Abs(end.Y) + proxy.radius).IsFinite())
                throw new ArgumentOutOfRangeException(nameof(pose), "Transformed collision geometry exceeds the finite range.");
            sorted[index] = point;
            sorted[index + sourceCount] = end;
        }
        var count = Geometry.FillConvexHull(sorted, output);
        if (count > 1 && output[0] == output[count - 1]) count--;
        return output.AsSpan(0, count);
    }

    private static bool Intersect(ReadOnlySpan<Vector2> a, float radiusA, ReadOnlySpan<Vector2> b,
        float radiusB, out Vector2 away, out double depth)
    {
        away = Vector2.Down;
        depth = double.PositiveInfinity;
        // ponytail: Scan edge and rounded-corner axes directly; add a support-map distance accelerator if large contour profiling warrants it.
        for (var index = 0; index < a.Length; index++)
            if (!TestAxis((a[(index + 1) % a.Length] - a[index]).Orthogonal(), a, radiusA, b, radiusB, ref away, ref depth)) return false;
        for (var index = 0; index < b.Length; index++)
            if (!TestAxis((b[(index + 1) % b.Length] - b[index]).Orthogonal(), a, radiusA, b, radiusB, ref away, ref depth)) return false;
        if (radiusA > 0 || radiusB > 0)
            for (var first = 0; first < a.Length; first++)
                for (var second = 0; second < b.Length; second++)
                    if (!TestAxis(b[second] - a[first], a, radiusA, b, radiusB, ref away, ref depth)) return false;
        return double.IsFinite(depth);
    }

    private static bool TestAxis(Vector2 axis, ReadOnlySpan<Vector2> a, float radiusA,
        ReadOnlySpan<Vector2> b, float radiusB, ref Vector2 away, ref double depth)
    {
        if (!axis.IsFinite()) throw new ArgumentOutOfRangeException(nameof(axis), "Collision extent exceeds the finite calculation range.");
        var length = Math.Sqrt(Dot(axis, axis));
        axis = length == 0 ? Vector2.Down : new((float)(axis.X / length), (float)(axis.Y / length));
        Project(a, axis, radiusA, out var minA, out var maxA);
        Project(b, axis, radiusB, out var minB, out var maxB);
        var negative = maxA - minB;
        var positive = maxB - minA;
        if (negative < 0 || positive < 0) return false;
        var current = Math.Min(negative, positive);
        if (current < depth) { depth = current; away = positive < negative ? axis : -axis; }
        return true;
    }

    private static void Project(ReadOnlySpan<Vector2> points, Vector2 axis, float radius, out double min, out double max)
    {
        min = max = Dot(points[0], axis);
        for (var index = 1; index < points.Length; index++)
        {
            var value = Dot(points[index], axis);
            min = Math.Min(min, value); max = Math.Max(max, value);
        }
        min -= radius; max += radius;
    }

    private static int Support(ReadOnlySpan<Vector2> hull, float radius, Vector2 direction, Span<Vector2> result)
    {
        Project(hull, direction, 0, out var min, out var max);
        var tangent = direction.Orthogonal();
        var minT = double.PositiveInfinity;
        var maxT = double.NegativeInfinity;
        for (var index = 0; index < hull.Length; index++)
        {
            var point = hull[index];
            if (max - Dot(point, direction) > 1e-5 * Math.Max(1, max - min)) continue;
            var projection = Dot(point, tangent);
            if (projection < minT) { minT = projection; result[0] = point + direction * radius; }
            if (projection > maxT) { maxT = projection; result[1] = point + direction * radius; }
        }
        return maxT - minT > 1e-5 ? 2 : 1;
    }

    private static void AddContacts(ReadOnlySpan<Vector2> a, float radiusA, ReadOnlySpan<Vector2> b,
        float radiusB, Vector2 away, double depth, Span<Vector2> result, ref int count)
    {
        Span<Vector2> first = stackalloc Vector2[2];
        Span<Vector2> second = stackalloc Vector2[2];
        var countA = Support(a, radiusA, -away, first);
        var countB = Support(b, radiusB, away, second);
        if (countA == 1 || countB == 1)
        {
            var pointA = countA == 1 ? first[0] : ProjectOnEdge(second[0], first[0], first[1]);
            var pointB = countB == 1 ? second[0] : ProjectOnEdge(first[0], second[0], second[1]);
            Store(pointA, pointB, result, ref count);
            return;
        }
        if (depth <= 1e-5) return;
        var tangent = away.Orthogonal();
        var a0 = Dot(first[0], tangent); var a1 = Dot(first[1], tangent);
        var b0 = Dot(second[0], tangent); var b1 = Dot(second[1], tangent);
        var low = Math.Max(Math.Min(a0, a1), Math.Min(b0, b1));
        var high = Math.Min(Math.Max(a0, a1), Math.Max(b0, b1));
        if (low > high) return;
        Store(AlongEdge(first, tangent, low), AlongEdge(second, tangent, low), result, ref count);
        if (high > low) Store(AlongEdge(first, tangent, high), AlongEdge(second, tangent, high), result, ref count);
    }

    private static Vector2 AlongEdge(ReadOnlySpan<Vector2> edge, Vector2 tangent, double projection) =>
        Lerp(edge[0], edge[1], (projection - Dot(edge[0], tangent)) / Dot(edge[1] - edge[0], tangent));

    private static Vector2 ProjectOnEdge(Vector2 point, Vector2 a, Vector2 b)
    {
        var axis = b - a;
        return Lerp(a, b, Dot(point - a, axis) / Dot(axis, axis));
    }

    private static Vector2 Lerp(Vector2 a, Vector2 b, double weight) =>
        new((float)(a.X + ((double)b.X - a.X) * weight), (float)(a.Y + ((double)b.Y - a.Y) * weight));

    private static void Store(Vector2 a, Vector2 b, Span<Vector2> result, ref int count)
    {
        if (!a.IsFinite() || !b.IsFinite()) throw new ArgumentOutOfRangeException(nameof(result), "Contact points exceed the finite range.");
        var index = count;
        if (count == result.Length / 2)
        {
            var least = double.PositiveInfinity;
            for (var pair = 0; pair < count; pair++)
            {
                var depth = DistanceSquared(result[pair * 2], result[pair * 2 + 1]);
                if (depth < least) { least = depth; index = pair; }
            }
            if (DistanceSquared(a, b) < least) return;
        }
        else count++;
        result[index * 2] = a; result[index * 2 + 1] = b;
    }

    private static bool RayContact(in B2ShapeProxy ray, bool slide, ReadOnlySpan<Vector2> other,
        float radius, Vector2 motion, out Vector2 pointA, out Vector2 pointB)
    {
        pointA = pointB = default;
        if (other.Length <= 8)
        {
            Span<B2Vec2> points = stackalloc B2Vec2[8];
            for (var index = 0; index < other.Length; index++) points[index] = PhysicsShapeBackend.ToBackend(other[index]);
            var proxy = b2MakeProxy(points, other.Length, radius * PhysicsSpace.MetersPerUnit);
            var contact = PhysicsSeparationRay.Contact(ray, slide, proxy, b2Transform_identity, PhysicsShapeBackend.ToBackend(motion));
            if (contact.pointCount == 0) return false;
            var value = contact.points[0];
            pointA = ToScene(value.point - contact.normal * (value.separation * 0.5f));
            pointB = ToScene(value.point + contact.normal * (value.separation * 0.5f));
            return true;
        }
        // Full convex contours exceed the native hull limit, so clip against their complete boundary.
        var from = ToScene(ray.points[0]);
        var axis = ToScene(ray.points[1] - ray.points[0]);
        var length = Math.Sqrt(Dot(axis, axis));
        if (length == 0) return false;
        var direction = new Vector2((float)(axis.X / length), (float)(axis.Y / length));
        var displacement = direction * (float)(length + Math.Max(0, Dot(direction, motion)));
        var entry = 0d; var exit = 1d; var normal = Vector2.Zero;
        for (var index = 0; index < other.Length; index++)
        {
            var edge = other[(index + 1) % other.Length] - other[index];
            var n = new Vector2(edge.Y, -edge.X);
            var distance = Dot(n, other[index] - from);
            var speed = Dot(n, displacement);
            if (speed == 0) { if (distance < 0) return false; continue; }
            var fraction = distance / speed;
            if (speed < 0 && fraction > entry) { entry = fraction; normal = n; }
            else if (speed > 0) exit = Math.Min(exit, fraction);
            if (entry > exit) return false;
        }
        if (normal == Vector2.Zero || entry > 1) return false;
        var normalLength = Math.Sqrt(Dot(normal, normal));
        normal = new((float)(normal.X / normalLength), (float)(normal.Y / normalLength));
        if (Dot(normal, -displacement) * PhysicsSpace.MetersPerUnit < PhysicsSeparationRay.MinimumFacingProjection) return false;
        pointA = from + displacement;
        var hit = from + displacement * (float)entry;
        pointB = slide ? pointA + normal * (float)Math.Sqrt(DistanceSquared(pointA, hit)) : hit;
        return true;
    }

    private static B2ShapeProxy WorldProxy(B2ShapeProxy proxy, Transform pose)
    {
        for (var index = 0; index < proxy.count; index++)
            proxy.points[index] = PhysicsShapeBackend.ToBackend(pose * new Vector2(proxy.points[index].X, proxy.points[index].Y));
        return proxy;
    }

    private static Vector2 ToScene(B2Vec2 point) => new(point.X * PhysicsSpace.UnitsPerMeter, point.Y * PhysicsSpace.UnitsPerMeter);
    private static double Dot(Vector2 a, Vector2 b) => (double)a.X * b.X + (double)a.Y * b.Y;
    private static double DistanceSquared(Vector2 a, Vector2 b) =>
        ((double)a.X - b.X) * ((double)a.X - b.X) + ((double)a.Y - b.Y) * ((double)a.Y - b.Y);
}
