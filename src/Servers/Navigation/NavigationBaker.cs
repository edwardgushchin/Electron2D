using Clipper2Lib;

namespace Electron2D;

internal static class NavigationBaker
{
    private const int Precision = 2;
    private const double CoordinateLimit = 1e15;
    private static void Check(double coordinate) { if (!double.IsFinite(coordinate) || Math.Abs(coordinate) > CoordinateLimit) throw new ArgumentOutOfRangeException(nameof(coordinate), "Navigation baking exceeds its clipping coordinate domain."); }
    private static PathD Path(ReadOnlySpan<Vector2> points, bool positive = false)
    {
        var result = new PathD(points.Length); foreach (var p in points) { Check(p.X); Check(p.Y); result.Add(new(p.X, p.Y)); }
        if (positive && !Clipper.IsPositive(result)) result.Reverse(); return result;
    }
    private static PathsD Paths(Vector2[][] outlines) { var result = new PathsD(outlines.Length); foreach (var item in outlines) if (item.Length >= 3) result.Add(Path(item)); return result; }
    internal static NavigationPolygonData? Bake(NavigationBakeSettings settings, Vector2[][] outlines, NavigationSourceGeometrySnapshot source)
    {
        if (outlines.Length == 0 && source.Traversable.Length == 0) return null;
        long inputPoints = 0; foreach (var outline in outlines) inputPoints += outline.Length; foreach (var outline in source.Traversable) inputPoints += outline.Length; foreach (var outline in source.Obstruction) inputPoints += outline.Length; foreach (var item in source.Projected) inputPoints += item.Points.Length; if (inputPoints > NavigationGeometryStorage.Limit) throw new ArgumentException("Combined navigation input exceeds its preparation budget.");
        Check(settings.AgentRadius); Check(settings.BorderSize);
        var traversable = Paths(outlines); traversable.AddRange(Paths(source.Traversable)); var obstruction = Paths(source.Obstruction);
        foreach (var item in source.Projected) if (!item.Carve && item.Points.Length >= 3) obstruction.Add(Path(item.Points, true));
        RectD? bounds = null;
        if (settings.BakingRect.HasArea())
        {
            var rect = settings.BakingRect; var offset = settings.BakingRectOffset;
            double x = (double)rect.Position.X + offset.X, y = (double)rect.Position.Y + offset.Y, right = x + rect.Size.X, bottom = y + rect.Size.Y;
            Check(x); Check(y); Check(right); Check(bottom);
            // Keep fractional authoring bounds rather than truncating them through integer coordinates.
            bounds = new RectD(x, y, right, bottom);
            traversable = Clipper.RectClip(bounds.Value, traversable, Precision); obstruction = Clipper.RectClip(bounds.Value, obstruction, Precision);
        }
        traversable = Clipper.Union(traversable, FillRule.NonZero);
        obstruction = Clipper.Union(obstruction, FillRule.NonZero);
        var solution = Clipper.Difference(traversable, obstruction, FillRule.NonZero, Precision);
        if (settings.AgentRadius > 0) solution = Clipper.InflatePaths(solution, -settings.AgentRadius, JoinType.Miter, EndType.Polygon, precision: Precision);
        var carve = new PathsD(); foreach (var item in source.Projected) if (item.Carve && item.Points.Length >= 3) carve.Add(Path(item.Points, true));
        if (carve.Count != 0) solution = Clipper.Difference(solution, carve, FillRule.NonZero, Precision);
        if (bounds is { } border && settings.BorderSize > 0)
        { border.left += settings.BorderSize; border.top += settings.BorderSize; border.right -= settings.BorderSize; border.bottom -= settings.BorderSize; solution = Clipper.RectClip(border, solution, Precision); }
        if (solution.Count == 0) return new([], [], settings.CellSize);
        PathsD partition;
        if (settings.SamplePartition == NavigationPolygon.SamplePartitionType.ConvexPartition && solution.All(p => Clipper.IsPositive(p) && IsConvex(p))) partition = solution;
        else
        {
            var status = Clipper.Triangulate(solution, Precision, out partition, useDelaunay: false);
            if (status != TriangulateResult.success) throw new InvalidDataException("Navigation surface triangulation failed.");
        }
        var vertices = new List<Vector2>(); var indices = new Dictionary<Vector2, int>(); var polygons = new List<int[]>();
        foreach (var contour in partition)
        {
            var polygon = new int[contour.Count];
            for (var i = 0; i < contour.Count; i++)
            {
                var point = new Vector2((float)contour[i].x, (float)contour[i].y); if (!point.IsFinite()) throw new InvalidDataException("Baked point exceeds the finite planar domain.");
                if (!indices.TryGetValue(point, out var index)) { index = vertices.Count; indices.Add(point, index); vertices.Add(point); }
                polygon[i] = index;
            }
            if (vertices.Count > NavigationGeometryStorage.Limit || polygons.Count >= NavigationGeometryStorage.Limit) throw new InvalidDataException("Baked navigation output exceeds its preparation budget.");
            polygons.Add(polygon);
        }
        var vertexArray = vertices.ToArray(); foreach (var polygon in polygons) NavigationPolygon.ValidatePolygon(vertexArray, polygon);
        if (settings.SamplePartition == NavigationPolygon.SamplePartitionType.ConvexPartition) polygons = MergeConvex(vertexArray, polygons);
        return new(vertexArray, polygons.ToArray(), settings.CellSize);
    }
    private static bool IsConvex(PathD polygon)
    {
        if (polygon.Count < 3) return false; var sign = 0;
        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i]; var b = polygon[(i + 1) % polygon.Count]; var c = polygon[(i + 2) % polygon.Count];
            var cross = (b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x);
            if (cross == 0) continue; var current = Math.Sign(cross); if (sign != 0 && sign != current) return false; sign = current;
        }
        return sign != 0;
    }
    private static List<int[]> MergeConvex(Vector2[] vertices, List<int[]> input)
    {
        var polygons = input.Cast<int[]?>().ToArray(); var edges = new Dictionary<(int, int), List<int>>(); var pending = new Queue<(int, int)>();
        static (int, int) Key(int a, int b) => a < b ? (a, b) : (b, a);
        void Add(int index)
        {
            var p = polygons[index]!; for (var i = 0; i < p.Length; i++)
            { var key = Key(p[i], p[(i + 1) % p.Length]); if (!edges.TryGetValue(key, out var owners)) edges[key] = owners = []; owners.Add(index); if (owners.Count == 2) pending.Enqueue(key); }
        }
        void Remove(int index)
        { var p = polygons[index]!; for (var i = 0; i < p.Length; i++) { var key = Key(p[i], p[(i + 1) % p.Length]); if (edges.TryGetValue(key, out var owners)) { owners.Remove(index); if (owners.Count == 0) edges.Remove(key); } } }
        for (var i = 0; i < polygons.Length; i++) Add(i);
        while (pending.TryDequeue(out var edge))
        {
            if (!edges.TryGetValue(edge, out var owners) || owners.Count != 2) continue;
            var ai = owners[0]; var bi = owners[1]; var a = polygons[ai]!; var b = polygons[bi]!;
            int at = -1, bt = -1;
            for (var i = 0; i < a.Length; i++) if (Key(a[i], a[(i + 1) % a.Length]) == edge) { at = i; break; }
            for (var i = 0; i < b.Length; i++) if (b[i] == a[(at + 1) % a.Length] && b[(i + 1) % b.Length] == a[at]) { bt = i; break; }
            if (bt < 0) continue;
            var merged = new int[a.Length + b.Length - 2]; var count = 0;
            for (var i = 0; i < a.Length - 1; i++) merged[count++] = a[(at + 1 + i) % a.Length];
            for (var i = 0; i < b.Length - 1; i++) merged[count++] = b[(bt + 1 + i) % b.Length];
            try { NavigationPolygon.ValidatePolygon(vertices, merged); } catch (ArgumentException) { continue; }
            Remove(ai); Remove(bi); polygons[ai] = merged; polygons[bi] = null; Add(ai);
        }
        return polygons.Where(p => p is not null).Select(p => p!).ToList();
    }
}
