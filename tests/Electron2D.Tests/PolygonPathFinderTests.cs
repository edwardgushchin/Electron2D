using Electron2D;

internal static class PolygonPathFinderTests
{
    internal static readonly Vector2[] Points = [new(0, 0), new(100, 0), new(100, 100), new(0, 100), new(40, 30), new(60, 30), new(60, 70), new(40, 70)];
    internal static readonly int[] Edges = [0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4];
    internal static void Run()
    {
        using var polygon = new PolygonPathFinder();
        Check(polygon.GetBounds() == default && polygon.GetClosestPoint(new(5, 5)) == default && !polygon.IsPointInside(default), "Empty scalar queries.");
        Check(polygon.FindPath(default, new(1, 1)).Length == 0 && polygon.GetIntersections(default, new(1, 1)).Length == 0, "Empty path/crossings.");
        var input = (Vector2[])Points.Clone(); var edges = (int[])Edges.Clone(); polygon.Setup(input, edges); input[0] = new(-100, -100); edges[0] = 99;
        Check(polygon.GetBounds() == new Rect2(0, 0, 100, 100), "Inputs are copied.");
        Check(polygon.IsPointInside(new(10, 10)) && !polygon.IsPointInside(new(50, 50)) && !polygon.IsPointInside(new(110, 50)), "Outer area/hole/outside.");
        Check(polygon.IsPointInside(new(40, 50)) && polygon.IsPointInside(new(0, 0)), "Closed boundary membership.");
        Check(polygon.GetClosestPoint(new(50, 50)) == new Vector2(60, 50) && polygon.GetClosestPoint(new(-5, 50)) == new Vector2(0, 50), "Nearest boundary and first-edge tie.");
        Check(polygon.GetIntersections(new(-10, 50), new(110, 50)).SequenceEqual([new Vector2(100, 50), new(0, 50), new(60, 50), new(40, 50)]), "Authored edge intersection order.");
        Check(polygon.GetIntersections(new(-10, -10), default).SequenceEqual([default(Vector2), default]), "Shared-vertex crossings retain duplicates.");
        Check(polygon.GetIntersections(new(1, 0), new(99, 0)).Length == 0 && polygon.GetIntersections(new(50, 50), new(50, 50)).Length == 0, "Collinear and zero-length have no unique crossing.");
        var direct = polygon.FindPath(new(10, 10), new(90, 10)); Check(direct.SequenceEqual([new Vector2(10, 10), new(90, 10)]), "Direct path.");
        Check(polygon.FindPath(new(10, 10), new(10, 10)).Length == 2, "Equal endpoints retain both entries.");
        var top = polygon.FindPath(new(10, 50), new(90, 50)); Check(top.SequenceEqual([new Vector2(10, 50), new(40, 30), new(60, 30), new(90, 50)]), "Hole route and deterministic tie.");
        var projected = polygon.FindPath(new(-10, 50), new(110, 50)); Check(projected[0] == new Vector2(0, 50) && projected[^1] == new Vector2(100, 50), "Outside endpoints project.");
        var holeStart = polygon.FindPath(new(50, 50), new(90, 50)); Check(holeStart[0] == new Vector2(60, 50), "Hole endpoint projects to first nearest boundary.");
        polygon.SetPointPenalty(4, 100); var bottom = polygon.FindPath(new(10, 50), new(90, 50)); Check(bottom[1] == Points[7] && bottom[2] == Points[6], "Entry penalty changes executable route.");
        Check(top[1] == Points[4], "Returned arrays survive later edits.");
        PenaltyOracle(polygon);
        polygon.Setup(Points, Edges); Check(polygon.GetPointPenalty(4) == 0, "Setup clears penalties.");
        Reject<ArgumentException>(() => polygon.Setup(Points, [0])); Reject<ArgumentOutOfRangeException>(() => polygon.Setup(Points, [0, 99]));
        Reject<ArgumentException>(() => polygon.Setup(Points, [0, 1])); Reject<ArgumentException>(() => polygon.Setup(Points, [0, 0]));
        Reject<ArgumentException>(() => polygon.Setup([new(float.NaN, 0)], [])); Reject<OverflowException>(() => polygon.Setup([new(-float.MaxValue, 0), new(float.MaxValue, 0)], []));
        Reject<ArgumentException>(() => polygon.Setup(new Vector2[2049], []));
        Reject<ArgumentOutOfRangeException>(() => polygon.SetPointPenalty(-1, 0)); Reject<ArgumentOutOfRangeException>(() => polygon.GetPointPenalty(8));
        Reject<ArgumentOutOfRangeException>(() => polygon.SetPointPenalty(0, -1)); Reject<ArgumentOutOfRangeException>(() => polygon.SetPointPenalty(0, float.NaN));
        Reject<ArgumentException>(() => polygon.FindPath(new(float.PositiveInfinity, 0), default));
        Check(polygon.GetBounds() == new Rect2(0, 0, 100, 100) && polygon.FindPath(new(10, 50), new(90, 50)).Length == 4, "Rejected setup leaves live graph intact.");
        polygon.Setup(Points, [.. Edges, 1, 0, 5, 4]); Check(polygon.GetIntersections(new(-10, 50), new(110, 50)).Length == 4, "Reversed duplicate edges collapse.");
        DisconnectedAndConcave(); Persistence(polygon); FailureAndConcurrency(polygon); NumericBoundaries();
        polygon.Setup(Points, Edges); for (var i = 0; i < 128; i++) ScalarQueries(polygon);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) ScalarQueries(polygon);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared scalar polygon queries allocate zero bytes.");
        var disposed = new PolygonPathFinder(); disposed.Dispose(); Reject<ObjectDisposedException>(() => disposed.GetBounds()); Reject<ObjectDisposedException>(() => disposed.Setup([], []));
        Console.WriteLine("PolygonPathFinder: closed areas/holes/concavity/islands, projections/crossings, 128 independent weighted shortest-path oracles, atomic inputs, typed persistence/copy/reset/errors/concurrency and 128 warm scalar cycles=0 managed bytes passed.");
    }
    private static void ScalarQueries(PolygonPathFinder polygon)
    { _ = polygon.GetBounds(); _ = polygon.GetClosestPoint(new(50, 50)); _ = polygon.IsPointInside(new(20, 20)); _ = polygon.GetPointPenalty(0); }
    private static void PenaltyOracle(PolygonPathFinder polygon)
    {
        var random = new Random(10206); Vector2[] all = [new(10, 50), new(90, 50), .. Points]; var weights = new float[8];
        for (var iteration = 0; iteration < 128; iteration++)
        {
            for (var i = 0; i < 8; i++) { weights[i] = random.Next(0, 300); polygon.SetPointPenalty(i, weights[i]); }
            var distance = Enumerable.Repeat(double.PositiveInfinity, all.Length).ToArray(); var visited = new bool[all.Length]; distance[0] = 0;
            for (var step = 0; step < all.Length; step++)
            {
                var at = -1; for (var i = 0; i < all.Length; i++) if (!visited[i] && (at < 0 || distance[i] < distance[at])) at = i;
                visited[at] = true;
                for (var next = 0; next < all.Length; next++)
                {
                    if (visited[next] || CrossesOpenHole(all[at], all[next])) continue;
                    var candidate = distance[at] + Length(all[at], all[next]) + (next >= 2 ? weights[next - 2] : 0);
                    distance[next] = Math.Min(distance[next], candidate);
                }
            }
            var path = polygon.FindPath(all[0], all[1]); double actual = 0;
            for (var i = 1; i < path.Length; i++) { Check(!CrossesOpenHole(path[i - 1], path[i]), "Path stays out of hole."); actual += Length(path[i - 1], path[i]); if (i < path.Length - 1) actual += weights[Array.IndexOf(Points, path[i])]; }
            Check(Math.Abs(actual - distance[1]) < 1e-5, "Weighted optimal route " + iteration + ": " + actual + "/" + distance[1]);
        }
    }
    private static bool CrossesOpenHole(Vector2 a, Vector2 b)
    {
        double low = 0, high = 1;
        bool Clip(double start, double delta, double min, double max)
        {
            if (delta == 0) return start > min && start < max;
            var t0 = (min - start) / delta; var t1 = (max - start) / delta; low = Math.Max(low, Math.Min(t0, t1)); high = Math.Min(high, Math.Max(t0, t1)); return low < high;
        }
        return Clip(a.X, b.X - a.X, 40, 60) && Clip(a.Y, b.Y - a.Y, 30, 70) && low < high;
    }
    private static double Length(Vector2 a, Vector2 b) => Math.Sqrt(((double)a.X - b.X) * (a.X - b.X) + ((double)a.Y - b.Y) * (a.Y - b.Y));
    private static void DisconnectedAndConcave()
    {
        using var polygon = new PolygonPathFinder();
        polygon.Setup([new(0, 0), new(20, 0), new(20, 20), new(0, 20), new(40, 0), new(60, 0), new(60, 20), new(40, 20)], Edges);
        Check(polygon.FindPath(new(10, 10), new(50, 10)).Length == 0, "Disconnected islands have no route.");
        polygon.Setup([new(0, 0), new(40, 0), new(40, 10), new(10, 10), new(10, 40), new(0, 40)], [0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 0]);
        Check(polygon.FindPath(new(30, 5), new(5, 30)).SequenceEqual([new Vector2(30, 5), new(10, 10), new(5, 30)]), "Concave corner route.");
        polygon.Setup([new(0, 0), new(10, 10), new(0, 10), new(10, 0)], [0, 1, 1, 2, 2, 3, 3, 0]);
        Check(polygon.IsPointInside(new(5, 2)) && polygon.IsPointInside(new(5, 8)) && polygon.FindPath(new(5, 2), new(5, 8)).Length == 2, "Crossing closed boundaries use even-odd area and touching passage.");
    }
    private static void Persistence(PolygonPathFinder polygon)
    {
        polygon.Setup(Points, Edges); polygon.SetPointPenalty(4, 100);
        using var copy = (PolygonPathFinder)polygon.DuplicateDeep(); copy.SetPointPenalty(4, 0); Check(polygon.GetPointPenalty(4) == 100 && copy.GetPointPenalty(4) == 0, "Copied penalties are independent.");
        using var target = new PolygonPathFinder(); target.CopyFromResource(polygon); Check(target.FindPath(new(10, 50), new(90, 50))[1] == Points[7], "CopyFromResource preserves geometry/penalties.");
        var descriptor = (PropertyDescriptor<PolygonPathFinder, byte[]>)polygon.GetPropertyList().Single(p => p.Name == "_polygon_path"); var bytes = descriptor.GetValue(polygon);
        Reject<InvalidDataException>(() => descriptor.SetValue(polygon, bytes[..^1])); Reject<InvalidDataException>(() => descriptor.SetValue(polygon, [.. bytes, 0]));
        var invalid = (byte[])bytes.Clone(); invalid[0] = 99; Reject<InvalidDataException>(() => descriptor.SetValue(polygon, invalid));
        Check(polygon.GetPointPenalty(4) == 100, "Rejected typed file state is atomic.");
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-polygon-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = System.IO.Path.Combine(folder, "polygon.e2dres"); ResourceSaver.Save(polygon, path, SaverFlags.Compress | SaverFlags.SaveBigEndian);
            using var loaded = ResourceLoader.Load<PolygonPathFinder>(path, ResourceLoader.CacheMode.Ignore); Check(loaded.FindPath(new(10, 50), new(90, 50))[1] == Points[7], "Nonempty compressed big-endian graph loads and executes.");
            using var root = new Node(); var holder = new ResourcePreloader { Name = "Routes" }; root.AddChild(holder); holder.Owner = root; holder.AddResource("area", polygon);
            using var scene = new PackedScene(); scene.Pack(root); var scenePath = System.IO.Path.Combine(folder, "route.e2dscene"); ResourceSaver.Save(scene, scenePath);
            using var reopened = ResourceLoader.Load<PackedScene>(scenePath, ResourceLoader.CacheMode.Ignore); using var instance = reopened.Instantiate();
            var restored = (PolygonPathFinder)((ResourcePreloader)instance.GetNode("Routes")).GetResource("area"); Check(restored.FindPath(new(10, 50), new(90, 50))[1] == Points[7], "Scene resource route survives archive.");
            target.ResetState(); Check(target.GetPointPenalty(4) == 100 && target.FindPath(new(10, 50), new(90, 50))[1] == Points[7], "Reset retires transient workspace and preserves stored geometry/penalties.");
            target.Setup([], []); Check(target.GetBounds() == default && target.FindPath(default, new(1, 1)).Length == 0, "Empty setup clears geometry.");
        }
        finally { Directory.Delete(folder, true); }
    }
    private static void FailureAndConcurrency(PolygonPathFinder polygon)
    {
        polygon.Setup(Points, Edges); Action<Resource> failing = _ => throw new ApplicationException("observer"); polygon.Changed += failing;
        Reject<ApplicationException>(() => polygon.SetPointPenalty(4, 10)); Check(polygon.GetPointPenalty(4) == 10, "Observer error occurs after publication."); polygon.Changed -= failing;
        var descriptor = (PropertyDescriptor<PolygonPathFinder, byte[]>)polygon.GetPropertyList().Single(p => p.Name == "_polygon_path"); var bytes = descriptor.GetValue(polygon);
        Action<Resource> typedFailure = _ => throw new ArgumentException("observer type"); polygon.Changed += typedFailure; Reject<ArgumentException>(() => descriptor.SetValue(polygon, bytes)); polygon.Changed -= typedFailure; Check(polygon.GetPointPenalty(4) == 10, "File setter preserves observer exception identity after valid publication.");
        Action<Resource> reentry = _ => Check(polygon.GetBounds().Size == new Vector2(100, 100), "Observers can query committed geometry."); polygon.Changed += reentry; polygon.Setup(Points, Edges); polygon.Changed -= reentry;
        Task.WaitAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() => { for (var i = 0; i < 64; i++) { Check(polygon.FindPath(new(10, 50), new(90, 50)).Length == 4, "Serialized concurrent path workspace."); Check(polygon.IsPointInside(new(10, 10)), "Concurrent scalar predicate."); } })).ToArray());
    }
    private static void NumericBoundaries()
    {
        using var polygon = new PolygonPathFinder();
        polygon.Setup([new(1e30f, 0), new(1, 0), new(1, 10), new(1e30f, 10)], [0, 1, 1, 2, 2, 3, 3, 0]);
        Check(polygon.GetClosestPoint(new(0, -1)) == new Vector2(1, 0) && !polygon.IsPointInside(new(0, 0)), "Extreme-scale endpoint projection and exact boundary bounds.");
        var triangle = new Vector2[] { new(-3.25f, -2.125f), new(11.875f, 1.625f), new(2.375f, 13.75f) }; polygon.Setup(triangle, [0, 1, 1, 2, 2, 0]);
        var random = new Random(106);
        for (var i = 0; i < 64; i++) { var a = (float)random.NextDouble() * .4f; var b = (float)random.NextDouble() * .4f; var point = triangle[0] * a + triangle[1] * b + triangle[2] * (1 - a - b); Check(polygon.IsPointInside(point) && polygon.FindPath(point, new(2, 3)).Length == 2, "Deterministic convex membership/direct route."); }
        polygon.Setup([new(0, 0), new(float.Epsilon * 8, 0), new(float.Epsilon * 8, float.Epsilon * 8), new(0, float.Epsilon * 8)], [0, 1, 1, 2, 2, 3, 3, 0]);
        Check(polygon.IsPointInside(new(float.Epsilon * 4, float.Epsilon * 4)), "Subnormal finite geometry.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
