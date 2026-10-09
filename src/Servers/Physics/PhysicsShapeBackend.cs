using System.Runtime.CompilerServices;
using Box2D.NET;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using Kind = Electron2D.PhysicsShapeGeometry.ShapeKind;

namespace Electron2D;

/// <summary>Compiles borrowed scene geometry into the current CPU backend's fixtures and query proxies.</summary>
internal static class PhysicsShapeBackend
{
    // The compiled cache neither owns nor keeps a resource alive. Points remain the authoring source.
    private static readonly ConditionalWeakTable<ConvexPolygonShape, B2Hull[]> PolygonHulls = new();

    internal static B2Vec2 ToBackend(Vector2 value) => new(value.X * PhysicsSpace.MetersPerUnit, value.Y * PhysicsSpace.MetersPerUnit);

    internal static void ValidateAndCachePolygon(ConvexPolygonShape shape, ReadOnlySpan<Vector2> points) =>
        PolygonHulls.AddOrUpdate(shape, BuildHulls(points));

    internal static B2Hull[] GetHulls(Shape shape, ReadOnlySpan<Vector2> points)
    {
        var polygon = (ConvexPolygonShape)shape;
        if (PolygonHulls.TryGetValue(polygon, out var hulls)) return hulls;
        hulls = BuildHulls(points);
        PolygonHulls.AddOrUpdate(polygon, hulls);
        return hulls;
    }

    internal static void AppendToBody(Shape shape, B2BodyId bodyID, Vector2 localPosition, float localRotation,
        in B2ShapeDef definition, List<B2ShapeId> fixtures)
    {
        var geometry = shape.GetGeometry();
        var radius = geometry.Radius * PhysicsSpace.MetersPerUnit;
        switch (geometry.Kind)
        {
            case Kind.Circle:
                fixtures.Add(b2CreateCircleShape(bodyID, definition, new B2Circle { center = ToBackend(localPosition), radius = radius }));
                break;
            case Kind.Capsule:
                var circle = new B2Circle { center = ToBackend(localPosition), radius = radius };
                if ((geometry.B.Y - geometry.A.Y) * PhysicsSpace.MetersPerUnit <= B2_LINEAR_SLOP)
                {
                    fixtures.Add(b2CreateCircleShape(bodyID, definition, circle));
                    break;
                }
                var offset = geometry.B.Rotated(localRotation);
                var capsule = new B2Capsule(ToBackend(localPosition - offset), ToBackend(localPosition + offset), radius);
                var id = b2CreateCapsuleShape(bodyID, definition, capsule);
                fixtures.Add(id.index1 == 0 ? b2CreateCircleShape(bodyID, definition, circle) : id);
                break;
            case Kind.Segment:
                fixtures.Add(CreateSegmentOrPoint(bodyID, definition,
                    ToBackend(localPosition + geometry.A.Rotated(localRotation)), ToBackend(localPosition + geometry.B.Rotated(localRotation))));
                break;
            case Kind.Rectangle:
                var box = b2MakeOffsetBox(geometry.B.X * PhysicsSpace.MetersPerUnit,
                    geometry.B.Y * PhysicsSpace.MetersPerUnit, ToBackend(localPosition), new B2Rot(MathF.Cos(localRotation), MathF.Sin(localRotation)));
                fixtures.Add(b2CreatePolygonShape(bodyID, definition, box));
                break;
            case Kind.ConvexPolygon:
                var position = ToBackend(localPosition);
                var rotation = new B2Rot(MathF.Cos(localRotation), MathF.Sin(localRotation));
                var hulls = GetHulls(shape, geometry.Points);
                for (var i = 0; i < hulls.Length; i++)
                {
                    var pieceDefinition = definition;
                    if (definition.userData.GetRef<PhysicsFixtureTag>() is { Compound: not null } compoundTag)
                        pieceDefinition.userData = new B2UserData(compoundTag with { CompoundPiece = i });
                    fixtures.Add(b2CreatePolygonShape(bodyID, pieceDefinition, b2MakeOffsetPolygon(hulls[i], position, rotation)));
                }
                break;
            case Kind.ConcavePolygon:
                for (var index = 0; index < geometry.Points.Length; index += 2)
                    fixtures.Add(CreateSegmentOrPoint(bodyID, definition,
                        ToBackend(localPosition + geometry.Points[index].Rotated(localRotation)),
                        ToBackend(localPosition + geometry.Points[index + 1].Rotated(localRotation))));
                break;
            case Kind.WorldBoundary:
                var normal = geometry.A.Rotated(localRotation);
                var plane = new B2Plane(new(normal.X, normal.Y), (geometry.Radius + normal.Dot(localPosition)) * PhysicsSpace.MetersPerUnit);
                fixtures.Add(b2CreateBoundaryShape(bodyID, definition, plane));
                break;
            case Kind.SeparationRay:
                var from = ToBackend(localPosition);
                var to = ToBackend(localPosition + geometry.B.Rotated(localRotation));
                if (!float.IsFinite(to.X) || !float.IsFinite(to.Y)) throw new ArgumentOutOfRangeException(nameof(localPosition));
                var tag = definition.userData.GetRef<PhysicsFixtureTag>() ??
                    throw new InvalidOperationException("A separation ray requires a tagged collision owner.");
                var settings = definition;
                settings.userData = new B2UserData(tag with { SeparationRay = new(from, to, geometry.SlideOnSlope) });
                settings.density = 0; settings.manifoldOverride = PhysicsSeparationRay.SolverContact;
                fixtures.Add(CreateSegmentOrPoint(bodyID, settings, from, to));
                break;
            default: throw new NotSupportedException("The shape has no CPU geometry integration.");
        }
    }

    internal static B2ShapeProxy GetQueryProxy(B2Shape shape) => shape.userData.GetRef<PhysicsFixtureTag>()?.SeparationRay is { } ray
        ? b2MakeProxy(ray.From, ray.To, 2, 0) : b2MakeShapeDistanceProxy(shape);

    internal static void AppendQueryProxies(Shape shape, List<B2ShapeProxy> proxies)
    {
        var geometry = shape.GetGeometry();
        var radius = geometry.Radius * PhysicsSpace.MetersPerUnit;
        switch (geometry.Kind)
        {
            case Kind.Circle:
                proxies.Add(b2MakeProxy(default(B2Vec2), 1, radius));
                break;
            case Kind.Capsule:
                proxies.Add((geometry.B.Y - geometry.A.Y) * PhysicsSpace.MetersPerUnit <= B2_LINEAR_SLOP
                    ? b2MakeProxy(default(B2Vec2), 1, radius)
                    : b2MakeProxy(ToBackend(geometry.A), ToBackend(geometry.B), 2, radius));
                break;
            case Kind.Segment:
                proxies.Add(SegmentProxy(ToBackend(geometry.A), ToBackend(geometry.B)));
                break;
            case Kind.Rectangle:
                var a = ToBackend(geometry.A); var b = ToBackend(geometry.B);
                Span<B2Vec2> corners = stackalloc B2Vec2[4] { a, new(b.X, a.Y), b, new(a.X, b.Y) };
                proxies.Add(b2MakeProxy(corners, 4, 0));
                break;
            case Kind.ConvexPolygon:
                foreach (ref readonly var hull in GetHulls(shape, geometry.Points).AsSpan())
                    proxies.Add(b2MakeProxy(hull.points.AsSpan(), hull.count, 0));
                break;
            case Kind.ConcavePolygon:
                for (var index = 0; index < geometry.Points.Length; index += 2)
                    proxies.Add(SegmentProxy(ToBackend(geometry.Points[index]), ToBackend(geometry.Points[index + 1])));
                break;
            case Kind.WorldBoundary:
                proxies.Add(B2Boundaries.Proxy(new(new(geometry.A.X, geometry.A.Y), geometry.Radius * PhysicsSpace.MetersPerUnit)));
                break;
            case Kind.SeparationRay:
                proxies.Add(b2MakeProxy(default, ToBackend(geometry.B), 2, 0));
                break;
            default: throw new NotSupportedException("The shape has no CPU query geometry integration.");
        }
    }

    private static B2ShapeProxy SegmentProxy(B2Vec2 a, B2Vec2 b) =>
        b2DistanceSquared(a, b) <= B2_LINEAR_SLOP * B2_LINEAR_SLOP
            ? b2MakeProxy(new B2Vec2(a.X + (b.X - a.X) * 0.5f, a.Y + (b.Y - a.Y) * 0.5f), 1, 0)
            : b2MakeProxy(a, b, 2, 0);

    private static B2ShapeId CreateSegmentOrPoint(B2BodyId bodyID, in B2ShapeDef definition, B2Vec2 a, B2Vec2 b)
    {
        if (b2DistanceSquared(a, b) <= B2_LINEAR_SLOP * B2_LINEAR_SLOP)
            return b2CreateCircleShape(bodyID, definition, new B2Circle { center = new(a.X + (b.X - a.X) * 0.5f, a.Y + (b.Y - a.Y) * 0.5f), radius = 0 });
        return b2CreateSegmentShape(bodyID, definition, new B2Segment(a, b));
    }
    private static B2Hull[] BuildHulls(ReadOnlySpan<Vector2> points)
    {
        if (points.Length == 0) return [];
        var min = points[0];
        var max = min;
        foreach (var point in points)
        {
            if (!point.IsFinite()) throw new ArgumentException("Polygon coordinates must be finite.", nameof(points));
            min = new(MathF.Min(min.X, point.X), MathF.Min(min.Y, point.Y));
            max = new(MathF.Max(max.X, point.X), MathF.Max(max.Y, point.Y));
        }
        if (!(max - min).IsFinite()) throw new ArgumentException("Polygon bounds exceed the finite range.", nameof(points));
        var count = points.Length;
        if (count > 1 && points[0] == points[^1]) count--;
        if (count < 3) throw new ArgumentException("A convex polygon needs three distinct perimeter points.", nameof(points));
        // ponytail: Pairwise checks are quadratic; index edges if large authoring contours show a measured cost.
        for (var first = 0; first < count; first++)
            for (var second = first + 2; second < count; second++)
            {
                if (first == 0 && second == count - 1) continue;
                if (Geometry.SegmentIntersectsSegment(points[first], points[(first + 1) % count],
                        points[second], points[(second + 1) % count]) is not null)
                    throw new ArgumentException("Polygon edges must not cross.", nameof(points));
            }

        double sign = 0;
        for (var index = 0; index < count; index++)
        {
            var first = points[index];
            var second = points[(index + 1) % count];
            var third = points[(index + 2) % count];
            var cross = ((double)second.X - first.X) * (third.Y - second.Y) -
                ((double)second.Y - first.Y) * (third.X - second.X);
            if (cross == 0) continue;
            if (sign != 0 && Math.Sign(cross) != Math.Sign(sign))
                throw new ArgumentException("Points must follow a convex perimeter.", nameof(points));
            sign = cross;
        }
        if (sign == 0) throw new ArgumentException("Polygon vertices must span a finite area.", nameof(points));

        var hulls = new List<B2Hull>();
        Span<B2Vec2> piece = stackalloc B2Vec2[B2_MAX_POLYGON_VERTICES];
        for (var start = 1; start < count - 1;)
        {
            var end = Math.Min(start + B2_MAX_POLYGON_VERTICES - 2, count - 1);
            piece[0] = ToBackend(points[0]);
            for (var index = start; index <= end; index++) piece[index - start + 1] = ToBackend(points[index]);
            var hull = b2ComputeHull(piece, end - start + 2);
            if (hull.count < 3)
                throw new ArgumentException("A polygon piece is below the physics solver's size tolerance.", nameof(points));
            hulls.Add(hull);
            start = end;
        }
        return hulls.ToArray();
    }
}
