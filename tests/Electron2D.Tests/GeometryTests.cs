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
        var convexParts = Geometry.DecomposePolygonInConvex(square);
        Check(convexParts.Length == 1 && convexParts[0].SequenceEqual(square) &&
            !ReferenceEquals(convexParts[0], square), "Convex polygon stays whole and the result is caller-owned");
        convexParts = Geometry.DecomposePolygonInConvex(square.Reverse().ToArray());
        Check(convexParts.Length == 1 && convexParts[0].SequenceEqual(square),
            "Clockwise convex input yields a counterclockwise contour");
        Vector2[] lShape = [new(0, 0), new(4, 0), new(4, 1), new(1, 1), new(1, 4), new(0, 4)];
        convexParts = Geometry.DecomposePolygonInConvex(lShape);
        var partArea = 0f;
        foreach (var part in convexParts)
        {
            Check(part.Length >= 3, "Convex decomposition parts have at least three vertices");
            for (var i = 0; i < part.Length; i++)
            {
                var current = part[i];
                var next = part[(i + 1) % part.Length];
                var following = part[(i + 2) % part.Length];
                partArea += current.Cross(next) * 0.5f;
                Check((next - current).Cross(following - next) >= 0f,
                    "Every decomposed part is counterclockwise and convex");
            }
        }
        Check(convexParts.Length == 2 && partArea == 7f && lShape[3] == new Vector2(1, 1),
            "Concave L contour partitions into two convex parts without lost area or input mutation");
        Check(Geometry.DecomposePolygonInConvex(lShape.Reverse().ToArray()).Length == 2,
            "Clockwise concave contour is decomposed into the same number of convex parts");
        Check(Geometry.DecomposePolygonInConvex([]).Length == 0 &&
            Geometry.DecomposePolygonInConvex([Vector2.Zero, Vector2.One]).Length == 0 &&
            Geometry.DecomposePolygonInConvex([new(0, 0), new(1, 0), new(2, 0)]).Length == 0 &&
            Geometry.DecomposePolygonInConvex([new(0, 0), new(4, 4), new(0, 4), new(4, 0)]).Length == 0,
            "Insufficient, zero-area and crossing contours do not produce convex parts");
        Check(Geometry.SegmentIntersectsCircle(new(-2, 0), new(2, 0), a, 1f) == 0.25f &&
            Geometry.SegmentIntersectsCircle(new(0, 0), new(2, 0), a, 1f) == 0.5f &&
            Geometry.SegmentIntersectsCircle(new(-1, 1), new(1, 1), a, 1f) == 0.5f &&
            Geometry.SegmentIntersectsCircle(new(-1, 2), new(1, 2), a, 1f) == -1f &&
            Geometry.SegmentIntersectsCircle(a, a, a, 1f) == -1f,
            "Circle entry, exit, tangent, miss and degenerate segment");
        Check(Geometry.TriangulatePolygon(square).SequenceEqual([3, 0, 1, 1, 2, 3]) &&
            Geometry.TriangulatePolygon([new(0, 0), new(0, 4), new(4, 0)]).SequenceEqual([0, 2, 1]),
            "Polygon triangulation preserves input indices and counterclockwise output for either winding");
        Check(Geometry.TriangulatePolygon([new(0, 0), new(1e30f, 0), new(1e30f, 1e30f), new(0, 1e30f)])
            .SequenceEqual([3, 0, 1, 1, 2, 3]), "Finite large-coordinate contours avoid float-area overflow");
        var triangles = Geometry.TriangulatePolygon(concave);
        float triangleArea = 0f;
        for (var i = 0; i < triangles.Length; i += 3)
        {
            var signedTwiceArea = (concave[triangles[i + 1]] - concave[triangles[i]]).Cross(concave[triangles[i + 2]] - concave[triangles[i]]);
            Check(signedTwiceArea > 0f, "Triangulation emits counterclockwise triangles");
            triangleArea += signedTwiceArea * 0.5f;
        }
        Check(triangles.Length == 9 && triangleArea == 12f, "Concave polygon triangulation covers its area exactly");
        Check(Geometry.TriangulatePolygon([]).Length == 0 &&
            Geometry.TriangulatePolygon([Vector2.Zero, Vector2.One]).Length == 0 &&
            Geometry.TriangulatePolygon([new(0, 0), new(4, 4), new(0, 4), new(4, 0)]).Length == 0,
            "Insufficient and crossing contours return an empty triangulation");

        Vector2[] overlapping = [new(2, 2), new(6, 2), new(6, 6), new(2, 6)];
        Check(Area(Geometry.IntersectPolygons(square, overlapping)) == 4d &&
            Area(Geometry.MergePolygons(square, overlapping)) == 28d &&
            Area(Geometry.ClipPolygons(square, overlapping)) == 12d &&
            Area(Geometry.ExcludePolygons(square, overlapping)) == 24d,
            "Polygon intersection, union, difference and XOR preserve region areas");
        Vector2[] inner = [new(1, 1), new(3, 1), new(3, 3), new(1, 3)];
        var withHole = Geometry.ClipPolygons(square, inner);
        Check(withHole.Length == 2 && Area(withHole) == 12d &&
            SignedArea(withHole[0]) * SignedArea(withHole[1]) < 0d,
            "Nested difference emits an oppositely wound hole");
        Check(Geometry.IntersectPolygons(square, [new(8, 8), new(9, 8), new(9, 9), new(8, 9)]).Length == 0 &&
            Geometry.ClipPolygons(square, square).Length == 0,
            "Disjoint intersection and identical difference are empty");
        Vector2[] crossingLine = [new(-1, 2), new(5, 2)];
        var inside = Geometry.IntersectPolylineWithPolygon(crossingLine, square);
        var outside = Geometry.ClipPolylineWithPolygon(crossingLine, square);
        Check(inside.Length == 1 && inside[0].Length == 2 &&
            inside[0].Contains(new Vector2(0, 2)) && inside[0].Contains(new Vector2(4, 2)) &&
            outside.Length == 2 && outside.All(part => part.Length == 2) &&
            outside.SelectMany(part => part).Contains(new Vector2(-1, 2)) &&
            outside.SelectMany(part => part).Contains(new Vector2(5, 2)),
            "Open line clipping retains inside and both outside portions");
        Check(Area(Geometry.OffsetPolygon(square, 1f, Geometry.PolyJoinType.Miter)) == 36d &&
            Area(Geometry.OffsetPolygon(square, -1f, Geometry.PolyJoinType.Miter)) == 4d &&
            Geometry.OffsetPolygon(square, 1f, Geometry.PolyJoinType.Round).Length > 0 &&
            Geometry.OffsetPolygon(square, 1f).Length > 0,
            "Polygon offsets expand and contract with each corner style");
        Vector2[] horizontalLine = [new(0, 0), new(4, 0)];
        Check(Area(Geometry.OffsetPolyline(horizontalLine, 1f, endType: Geometry.PolyEndType.Butt)) == 8d &&
            Area(Geometry.OffsetPolyline(horizontalLine, 1f, endType: Geometry.PolyEndType.Square)) == 12d &&
            Geometry.OffsetPolyline(horizontalLine, 1f, endType: Geometry.PolyEndType.Round).Length > 0 &&
            Geometry.OffsetPolyline(horizontalLine, -1f).Length == 0 &&
            Geometry.OffsetPolyline(horizontalLine, 1f, endType: Geometry.PolyEndType.Polygon).Length == 0,
            "Polyline caps, negative offsets and rejected polygon end type");
        Reject<ArgumentOutOfRangeException>(() => Geometry.ClipPolygons([new(float.NaN, 0)], square));
        Reject<ArgumentOutOfRangeException>(() => Geometry.MergePolygons([new(float.MaxValue, 0)], square));
        Reject<ArgumentOutOfRangeException>(() => Geometry.OffsetPolygon(square, float.NaN));
        Reject<ArgumentOutOfRangeException>(() => Geometry.OffsetPolygon(square, 1f, (Geometry.PolyJoinType)99));
        Reject<ArgumentOutOfRangeException>(() => Geometry.OffsetPolyline(horizontalLine, 1f, endType: (Geometry.PolyEndType)99));

        Check(Geometry.TriangulateDelaunay([]).Length == 0 &&
            Geometry.TriangulateDelaunay([Vector2.Zero, Vector2.One]).Length == 0 &&
            Geometry.TriangulateDelaunay([new(0, 0), new(1, 0), new(2, 0)]).Length == 0,
            "Delaunay triangulation omits insufficient and collinear point sets");
        var delaunayTriangle = Geometry.TriangulateDelaunay([new(0, 0), new(4, 0), new(0, 4)]);
        Check(delaunayTriangle.Length == 3 && delaunayTriangle.Order().SequenceEqual([0, 1, 2]),
            "Delaunay output uses original input indices");
        Vector2[] delaunayPoints = [new(0, 0), new(4, 0), new(4, 4), new(0, 4), new(2, 2)];
        var delaunay = Geometry.TriangulateDelaunay(delaunayPoints);
        float delaunayArea = 0f;
        for (var i = 0; i < delaunay.Length; i += 3)
        {
            var first = delaunay[i]; var second = delaunay[i + 1]; var third = delaunay[i + 2];
            Check(first >= 0 && first < delaunayPoints.Length && second >= 0 && second < delaunayPoints.Length &&
                third >= 0 && third < delaunayPoints.Length && first != second && second != third && third != first,
                "Delaunay triangles use distinct in-range indices");
            delaunayArea += System.MathF.Abs((delaunayPoints[second] - delaunayPoints[first])
                .Cross(delaunayPoints[third] - delaunayPoints[first])) * 0.5f;
        }
        Check(delaunay.Length == 12 && delaunayArea == 16f && delaunay.Count(index => index == 4) == 4,
            "Interior point splits the square into four triangles without gaps or overlap");
        delaunay = Geometry.TriangulateDelaunay([new(0, 0), new(4, 0), new(4, 4), new(0, 3)]);
        Check(delaunay.Length == 6 &&
            delaunay.Chunk(3).Select(indices => string.Join(',', indices.Order())).Order()
                .SequenceEqual(new[] { "0,1,3", "1,2,3" }),
            "Delaunay chooses the empty-circumcircle diagonal of an asymmetric quadrilateral");

        var packed = Geometry.MakeAtlas([new(2, 2), new(2, 2)]);
        Check(packed.Size == new Vector2I(2, 4) && packed.Points.SequenceEqual([new(0, 0), new(0, 2)]),
            "Atlas chooses the first equal-aspect strip and returns positions in input order");
        packed = Geometry.MakeAtlas([new(1, 3), new(3, 1)]);
        Check(packed.Size == new Vector2I(4, 3) && packed.Points.SequenceEqual([new(3, 0), new(0, 0)]),
            "Atlas sorts by width but restores original tile order");
        packed = Geometry.MakeAtlas([new(3.9f, 2.9f)]);
        Check(packed.Size == new Vector2I(3, 2) && packed.Points.SequenceEqual([Vector2.Zero]),
            "Atlas truncates fractional tile dimensions to pixels");
        packed = Geometry.MakeAtlas([new(1, 2147483520f), new(1, 2147483520f)]);
        Check(packed.Size == new Vector2I(2, 2147483520) && packed.Points.SequenceEqual([new(0, 0), new(1, 0)]),
            "Atlas skips an overflowing narrow strip when a wider layout fits");
        Reject<ArgumentException>(() => Geometry.MakeAtlas([]));
        Reject<ArgumentOutOfRangeException>(() => Geometry.MakeAtlas([new(0, 1)]));
        Reject<ArgumentOutOfRangeException>(() => Geometry.MakeAtlas([new(4097, 1)]));
        Reject<ArgumentOutOfRangeException>(() => Geometry.MakeAtlas([new(float.NaN, 1)]));
        Console.WriteLine("Geometry primitive geometry checks passed.");
    }

    private static bool Near(Vector2? actual, Vector2 expected) => actual is Vector2 value &&
        System.MathF.Abs(value.X - expected.X) < 0.00001f && System.MathF.Abs(value.Y - expected.Y) < 0.00001f;
    private static double Area(Vector2[][] contours) => contours.Sum(SignedArea);
    private static double SignedArea(Vector2[] contour)
    {
        var twiceArea = 0d;
        for (var i = 0; i < contour.Length; i++)
        {
            var next = contour[(i + 1) % contour.Length];
            twiceArea += (double)contour[i].X * next.Y - (double)next.X * contour[i].Y;
        }
        return twiceArea / 2d;
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
