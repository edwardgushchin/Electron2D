using Electron2D;

internal static class GeometryTests
{
    internal static void Run()
    {
        Check(Geometry.BresenhamLine(new(0, 0), new(3, 1)).SequenceEqual(
            [new(0, 0), new(1, 0), new(2, 1), new(3, 1)]), "Shallow raster line and endpoints");
        Check(Geometry.BresenhamLine(new(1, 3), new(0, 0)).SequenceEqual(
            [new(1, 3), new(1, 2), new(0, 1), new(0, 0)]), "Steep reverse raster line");
        Check(Geometry.BresenhamLine(new(int.MaxValue, int.MaxValue), new(int.MaxValue, int.MaxValue)).Length == 1,
            "Degenerate line at integer boundary");
        Reject<ArgumentOutOfRangeException>(() => Geometry.BresenhamLine(new(int.MinValue, 0), new(int.MaxValue, 0)));

        var a = new Vector2(0, 0);
        var b = new Vector2(2, 0);
        Check(Geometry.GetClosestPointToSegment(new(3, 1), a, b) == b, "Capped nearest endpoint");
        Check(Geometry.GetClosestPointToSegmentUncapped(new(3, 1), a, b) == new Vector2(3, 0), "Uncapped projection");
        Check(Geometry.GetClosestPointToSegment(new(3, 1), a, a) == a &&
            Geometry.GetClosestPointToSegmentUncapped(new(3, 1), a, a) == a, "Degenerate projection");
        var pair = Geometry.GetClosestPointsBetweenSegments(new(0, 0), new(2, 0), new(1, -1), new(1, 1));
        Check(pair.Length == 2 && Near(pair[0], new(1, 0)) && Near(pair[1], new(1, 0)), "Crossing segments nearest pair");
        pair = Geometry.GetClosestPointsBetweenSegments(new(0, 0), new(0, 0), new(2, 0), new(4, 0));
        Check(pair[0] == a && pair[1] == b, "Degenerate first segment nearest pair");
        Check(Geometry.IsPointInCircle(new(1, 0), a, 1) &&
            !Geometry.IsPointInCircle(new(2, 0), a, 1) &&
            Geometry.IsPointInCircle(new(1, 0), a, -1), "Circle boundary, outside and negative radius");

        Check(Near(Geometry.LineIntersectsLine(new(0, 0), new(1, 0), new(1, -1), new(0, 1)), new(1, 0)),
            "Infinite line intersection uses directions");
        Check(Geometry.LineIntersectsLine(new(0, 0), new(1, 0), new(0, 1), new(1, 0)) is null,
            "Parallel lines have no unique intersection");
        Check(Near(Geometry.SegmentIntersectsSegment(new(0, 0), new(2, 0), new(1, -1), new(1, 1)), new(1, 0)),
            "Crossing segments");
        Check(Near(Geometry.SegmentIntersectsSegment(new(0, 0), new(2, 0), new(2, 0), new(2, 1)), new(2, 0)),
            "Touching endpoint");
        Check(Geometry.SegmentIntersectsSegment(new(0, 0), new(2, 0), new(1, 0), new(3, 0)) is null &&
            Geometry.SegmentIntersectsSegment(new(0, 0), new(0, 0), new(0, -1), new(0, 1)) is null,
            "Collinear and degenerate segments have no unique intersection");

        Vector2[] square = [new(0, 0), new(4, 0), new(4, 4), new(0, 4)];
        Check(Geometry.IsPointInPolygon(new(2, 2), square) &&
            Geometry.IsPointInPolygon(new(0, 2), square) && Geometry.IsPointInPolygon(new(4, 4), square) &&
            !Geometry.IsPointInPolygon(new(5, 2), square) &&
            !Geometry.IsPointInPolygon(new(0, 0), square.AsSpan(0, 2)),
            "Polygon interior, edge, vertex, exterior and insufficient vertices");
        Vector2[] concave = [new(0, 0), new(4, 0), new(4, 4), new(2, 2), new(0, 4)];
        Check(!Geometry.IsPointInPolygon(new(2, 3), concave) && Geometry.IsPointInPolygon(new(2, 2), concave),
            "Concave indentation and boundary");
        Check(!Geometry.IsPolygonClockwise(square) && Geometry.IsPolygonClockwise(square.Reverse().ToArray()) &&
            !Geometry.IsPolygonClockwise(square.AsSpan(0, 2)), "Cartesian winding and small polygon");
        Check(Geometry.PointIsInsideTriangle(new(1, 1), new(0, 0), new(3, 0), new(0, 3)) &&
            !Geometry.PointIsInsideTriangle(new(3, 3), new(0, 0), new(3, 0), new(0, 3)),
            "Triangle interior and exterior");

        Vector2[] hullInput = [new(1, 1), new(0, 0), new(1, 0), new(0, 1), new(0.5f, 0.5f)];
        Check(Geometry.ConvexHull(hullInput).SequenceEqual(
            [new(0, 0), new(1, 0), new(1, 1), new(0, 1), new(0, 0)]) && hullInput[0] == new Vector2(1, 1),
            "Convex hull ordering, closure, interior removal and input ownership");
        Check(Geometry.ConvexHull([]).Length == 0 && Geometry.ConvexHull([new(2, 3)]).SequenceEqual([new(2, 3)]) &&
            Geometry.ConvexHull([new(0, 0), new(1, 0), new(2, 0)]).SequenceEqual([new(0, 0), new(2, 0), new(0, 0)]),
            "Convex hull empty, single and collinear inputs");
        Check(Geometry.SegmentIntersectsCircle(new(-2, 0), new(2, 0), a, 1f) == 0.25f &&
            Geometry.SegmentIntersectsCircle(new(0, 0), new(2, 0), a, 1f) == 0.5f &&
            Geometry.SegmentIntersectsCircle(new(-1, 1), new(1, 1), a, 1f) == 0.5f &&
            Geometry.SegmentIntersectsCircle(new(-1, 2), new(1, 2), a, 1f) == -1f &&
            Geometry.SegmentIntersectsCircle(a, a, a, 1f) == -1f,
            "Circle entry, exit, tangent, miss and degenerate segment");
        Console.WriteLine("Geometry primitive geometry checks passed.");
    }

    private static bool Near(Vector2? actual, Vector2 expected) => actual is Vector2 value &&
        System.MathF.Abs(value.X - expected.X) < 0.00001f && System.MathF.Abs(value.Y - expected.Y) < 0.00001f;
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
