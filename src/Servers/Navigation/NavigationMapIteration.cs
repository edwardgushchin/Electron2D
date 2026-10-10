namespace Electron2D;

internal sealed partial class NavigationMapIteration
{
    internal sealed record Cell(RID Region, uint Layers, float EnterCost, float TravelCost, bool Connect, Vector2[] Vertices, Vector2 Center, bool IsLink = false, ulong OwnerID = 0, RID LinkStartRegion = default, RID LinkEndRegion = default, bool Enabled = true);
    internal readonly record struct Portal(int Target, Vector2 A, Vector2 B, Vector2? Centered = null);
    internal static readonly NavigationMapIteration Empty = new([], []);
    internal readonly Cell[] Cells;
    internal readonly Portal[][] Edges;
    private NavigationMapIteration(Cell[] cells, Portal[][] edges) { Cells = cells; Edges = edges; (_samplingRegions, _surfaceAreas) = PrepareSampling(cells); }
    internal static NavigationMapIteration BuildRegion(NavigationRegionState region)
    {
        var cells = new List<Cell>();
        foreach (var polygon in region.Geometry.Polygons)
        {
            var points = new Vector2[polygon.Length]; var center = Vector2.Zero;
            for (var i = 0; i < points.Length; i++) { points[i] = region.Transform * region.Geometry.Vertices[polygon[i]]; if (!points[i].IsFinite()) throw new ArgumentException("Navigation transform produces nonfinite geometry."); center += points[i] / points.Length; }
            NavigationPolygon.ValidatePolygon(points, Enumerable.Range(0, points.Length).ToArray());
            cells.Add(new(region.RID, region.Layers, region.EnterCost, region.TravelCost, region.UseEdgeConnections, points, center, OwnerID: region.OwnerID, Enabled: region.Enabled));
        }
        return new(cells.ToArray(), []);
    }
    internal static NavigationMapIteration Build(NavigationMapState map, IEnumerable<NavigationRegionState> regions, Dictionary<RID, NavigationMapIteration> pending, IEnumerable<NavigationLinkState> links)
    {
        if (!map.Active) return Empty;
        var cells = new List<Cell>();
        foreach (var region in regions)
            if (region.Map == map.RID) cells.AddRange(pending.GetValueOrDefault(region.RID, region.Iteration).Cells);
        var topology = BuildTopology(map, cells);
        var edges = topology.Edges;
        var allEdges = edges.ToList();
        var regionCount = cells.Count;
        foreach (var link in links)
        {
            if (!link.Enabled || link.Map != map.RID) continue;
            var start = Attach(link.Start, out var from); var end = Attach(link.End, out var to);
            if (start < 0 || end < 0) continue;
            var index = cells.Count;
            cells.Add(new(link.RID, link.Layers, link.EnterCost, link.TravelCost, false, [from, from, to, to], from * .5f + to * .5f, IsLink: true, OwnerID: link.OwnerID, LinkStartRegion: cells[start].Region, LinkEndRegion: cells[end].Region));
            allEdges.Add([]);
            allEdges[start].Add(new(index, from, from)); allEdges[index].Add(new(end, to, to));
            if (link.Bidirectional) { allEdges[end].Add(new(index, to, to)); allEdges[index].Add(new(start, from, from)); }
        }
        return new(cells.ToArray(), allEdges.Select(e => e.ToArray()).ToArray()) { Profile = topology.Profile, Connections = topology.Connections };
        int Attach(Vector2 point, out Vector2 projected)
        {
            var index = -1; projected = default; var distance = (double)map.LinkRadius * map.LinkRadius;
            for (var i = 0; i < regionCount; i++) { if (!cells[i].Enabled) continue; var closest = Closest(cells[i], point); var d = DistanceSquared(point, closest); if (d < distance) { distance = d; projected = closest; index = i; } }
            return index;
        }
    }
    private static bool Near(Vector2 a, Vector2 b, float margin) => DistanceSquared(a, b) <= (double)margin * margin;
    internal static Vector2 Closest(Cell cell, Vector2 point)
    {
        var sign = 0; var inside = true;
        for (var i = 0; i < cell.Vertices.Length; i++) { var area = Area(cell.Vertices[i], cell.Vertices[(i + 1) % cell.Vertices.Length], point); if (area == 0) continue; var next = Math.Sign(area); if (sign != 0 && sign != next) { inside = false; break; } sign = next; }
        if (inside) return point;
        var best = cell.Vertices[0]; var distance = double.PositiveInfinity;
        for (var i = 0; i < cell.Vertices.Length; i++) { var p = ClosestSegment(point, cell.Vertices[i], cell.Vertices[(i + 1) % cell.Vertices.Length]); var d = DistanceSquared(point, p); if (d < distance) { distance = d; best = p; } }
        return best;
    }
    private static Vector2 Interpolate(Vector2 a, Vector2 b, double t) => new((float)(a.X + ((double)b.X - a.X) * t), (float)(a.Y + ((double)b.Y - a.Y) * t));
    private static Vector2 ClosestSegment(Vector2 point, Vector2 a, Vector2 b)
    { var dx = (double)b.X - a.X; var dy = (double)b.Y - a.Y; var length = dx * dx + dy * dy; return length == 0 ? a : Interpolate(a, b, Math.Clamp((((double)point.X - a.X) * dx + ((double)point.Y - a.Y) * dy) / length, 0, 1)); }
    private static double Area(Vector2 origin, Vector2 a, Vector2 b) => ((double)a.X - origin.X) * ((double)b.Y - origin.Y) - ((double)a.Y - origin.Y) * ((double)b.X - origin.X);
    private static double DistanceSquared(Vector2 a, Vector2 b) { var x = (double)a.X - b.X; var y = (double)a.Y - b.Y; return x * x + y * y; }
    private int Project(Vector2 point, uint layers, out Vector2 projected)
    {
        var best = -1; projected = Vector2.Zero; var distance = double.PositiveInfinity;
        for (var i = 0; i < Cells.Length; i++) { if (!Cells[i].Enabled || Cells[i].IsLink || (Cells[i].Layers & layers) == 0) continue; var p = Closest(Cells[i], point); var d = DistanceSquared(p, point); if (d < distance) { distance = d; best = i; projected = p; } }
        return best;
    }
    internal (Vector2 Point, RID Owner) Closest(Vector2 point) { var index = Project(point, uint.MaxValue, out var p); return (p, index < 0 ? default : Cells[index].Region); }
    internal Rect2 Bounds(RID region)
    {
        var found = false; var minimum = Vector2.Zero; var maximum = Vector2.Zero;
        foreach (var cell in Cells) if (cell.Region == region) foreach (var point in cell.Vertices) { if (!found) { minimum = maximum = point; found = true; } else { minimum = minimum.Min(point); maximum = maximum.Max(point); } }
        return found ? new(minimum, maximum - minimum) : default;
    }
}
