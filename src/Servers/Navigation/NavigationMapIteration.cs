namespace Electron2D;

internal sealed class NavigationMapIteration
{
    internal sealed record Cell(RID Region, uint Layers, float EnterCost, float TravelCost, bool Connect, Vector2[] Vertices, Vector2 Center, bool IsLink = false);
    internal readonly record struct Portal(int Target, Vector2 A, Vector2 B);
    internal static readonly NavigationMapIteration Empty = new([], []);
    internal readonly Cell[] Cells;
    internal readonly Portal[][] Edges;
    private NavigationMapIteration(Cell[] cells, Portal[][] edges) { Cells = cells; Edges = edges; }
    internal static NavigationMapIteration BuildRegion(NavigationRegionState region)
    {
        var cells = new List<Cell>();
        foreach (var polygon in region.Geometry.Polygons)
        {
            var points = new Vector2[polygon.Length]; var center = Vector2.Zero;
            for (var i = 0; i < points.Length; i++) { points[i] = region.Transform * region.Geometry.Vertices[polygon[i]]; if (!points[i].IsFinite()) throw new ArgumentException("Navigation transform produces nonfinite geometry."); center += points[i] / points.Length; }
            NavigationPolygon.ValidatePolygon(points, Enumerable.Range(0, points.Length).ToArray());
            cells.Add(new(region.RID, region.Layers, region.EnterCost, region.TravelCost, region.UseEdgeConnections, points, center));
        }
        return new(cells.ToArray(), []);
    }
    internal static NavigationMapIteration Build(NavigationMapState map, IEnumerable<NavigationRegionState> regions, Dictionary<RID, NavigationMapIteration> pending, IEnumerable<NavigationLinkState> links)
    {
        if (!map.Active) return Empty;
        var cells = new List<Cell>();
        foreach (var region in regions)
            if (region.Map == map.RID && region.Enabled) cells.AddRange(pending.GetValueOrDefault(region.RID, region.Iteration).Cells);
        var ownership = new Dictionary<(Vector2, Vector2), int>();
        foreach (var cell in cells) for (var i = 0; i < cell.Vertices.Length; i++)
            {
                var key = EdgeKey(cell.Vertices[i], cell.Vertices[(i + 1) % cell.Vertices.Length]);
                ownership.TryGetValue(key, out var count); ownership[key] = count + 1;
            }
        if (ownership.Values.Any(count => count > 2)) throw new ArgumentException("Navigation edges cannot have more than two authored owners.");
        var edges = new List<Portal>[cells.Count]; for (var i = 0; i < edges.Length; i++) edges[i] = [];
        // ponytail: cold topology compares edges quadratically; spatial edge indexing follows measured large-map need.
        for (var i = 0; i < cells.Count; i++) for (var j = i + 1; j < cells.Count; j++)
            {
                var a = cells[i]; var b = cells[j]; var same = a.Region == b.Region;
                var nearConnections = !same && map.UseEdgeConnections && a.Connect && b.Connect;
                for (var ai = 0; ai < a.Vertices.Length; ai++) for (var bi = 0; bi < b.Vertices.Length; bi++)
                    {
                        var a0 = a.Vertices[ai]; var a1 = a.Vertices[(ai + 1) % a.Vertices.Length]; var b0 = b.Vertices[bi]; var b1 = b.Vertices[(bi + 1) % b.Vertices.Length];
                        if (a0 == b1 && a1 == b0) Add(a0, a1, b1, b0);
                        else if (a0 == b0 && a1 == b1) Add(a0, a1, b0, b1);
                        else if (nearConnections && ownership[EdgeKey(a0, a1)] == 1 && ownership[EdgeKey(b0, b1)] == 1)
                        {
                            var dx = (double)a1.X - a0.X; var dy = (double)a1.Y - a0.Y; var length = dx * dx + dy * dy;
                            var r0 = (((double)b0.X - a0.X) * dx + ((double)b0.Y - a0.Y) * dy) / length; var r1 = (((double)b1.X - a0.X) * dx + ((double)b1.Y - a0.Y) * dy) / length;
                            if ((r0 < 0 && r1 < 0) || (r0 > 1 && r1 > 1) || r0 == r1) continue;
                            var p = Interpolate(a0, a1, Math.Clamp(r0, 0, 1)); var q = Interpolate(a0, a1, Math.Clamp(r1, 0, 1));
                            var r = r0 is >= 0 and <= 1 ? b0 : Interpolate(b0, b1, (Math.Clamp(r0, 0, 1) - r0) / (r1 - r0)); var t = r1 is >= 0 and <= 1 ? b1 : Interpolate(b0, b1, (Math.Clamp(r1, 0, 1) - r0) / (r1 - r0));
                            if (Near(p, r, map.EdgeMargin) && Near(q, t, map.EdgeMargin) && p != q) Add(p, q, r, t);
                        }
                        void Add(Vector2 p, Vector2 q, Vector2 r, Vector2 s) { var start = p * .5f + r * .5f; var end = q * .5f + s * .5f; edges[i].Add(new(j, start, end)); edges[j].Add(new(i, start, end)); }
                    }
            }
        var allEdges = edges.ToList();
        var regionCount = cells.Count;
        foreach (var link in links)
        {
            if (!link.Enabled || link.Map != map.RID) continue;
            var start = Attach(link.Start, out var from); var end = Attach(link.End, out var to);
            if (start < 0 || end < 0) continue;
            var index = cells.Count;
            cells.Add(new(link.RID, link.Layers, link.EnterCost, link.TravelCost, false, [from, from, to, to], from * .5f + to * .5f, IsLink: true));
            allEdges.Add([]);
            allEdges[start].Add(new(index, from, from)); allEdges[index].Add(new(end, to, to));
            if (link.Bidirectional) { allEdges[end].Add(new(index, to, to)); allEdges[index].Add(new(start, from, from)); }
        }
        return new(cells.ToArray(), allEdges.Select(e => e.ToArray()).ToArray());
        int Attach(Vector2 point, out Vector2 projected)
        {
            var index = -1; projected = default; var distance = (double)map.LinkRadius * map.LinkRadius;
            for (var i = 0; i < regionCount; i++) { var closest = Closest(cells[i], point); var d = DistanceSquared(point, closest); if (d < distance) { distance = d; projected = closest; index = i; } }
            return index;
        }
    }
    private static (Vector2, Vector2) EdgeKey(Vector2 a, Vector2 b) => a.X < b.X || a.X == b.X && a.Y <= b.Y ? (a, b) : (b, a);
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
        for (var i = 0; i < Cells.Length; i++) { if (Cells[i].IsLink || (Cells[i].Layers & layers) == 0) continue; var p = Closest(Cells[i], point); var d = DistanceSquared(p, point); if (d < distance) { distance = d; best = i; projected = p; } }
        return best;
    }
    internal (Vector2 Point, RID Owner) Closest(Vector2 point) { var index = Project(point, uint.MaxValue, out var p); return (p, index < 0 ? default : Cells[index].Region); }
    internal Vector2[] Path(Vector2 origin, Vector2 destination, bool optimize, uint layers)
    {
        var start = Project(origin, layers, out var from); var finish = Project(destination, layers, out var to);
        if (start < 0 || finish < 0) return [];
        if (start == finish) return from == to ? [from] : [from, to];
        var distance = new double[Cells.Length]; Array.Fill(distance, double.PositiveInfinity); distance[start] = 0;
        var parent = new int[Cells.Length]; Array.Fill(parent, -1); var entry = new Vector2[Cells.Length]; entry[start] = from; var previous = new Portal[Cells.Length];
        var queue = new PriorityQueue<(int Cell, double Cost), (double Total, double NegativeCost, int Order)>(); queue.Enqueue((start, 0), (Math.Sqrt(DistanceSquared(from, to)) * Cells[start].TravelCost, 0, start));
        var best = start; var remaining = DistanceSquared(Closest(Cells[start], to), to);
        while (queue.TryDequeue(out var current, out _))
        {
            var index = current.Cell; if (current.Cost != distance[index]) continue;
            var d = Cells[index].IsLink ? double.PositiveInfinity : DistanceSquared(Closest(Cells[index], to), to); if (d < remaining) { remaining = d; best = index; }
            if (index == finish) { best = finish; break; }
            foreach (var portal in Edges[index])
            {
                var next = portal.Target; if ((Cells[next].Layers & layers) == 0) continue;
                var point = ClosestSegment(entry[index], portal.A, portal.B);
                var cost = current.Cost + Math.Sqrt(DistanceSquared(entry[index], point)) * Cells[index].TravelCost + (Cells[index].Region != Cells[next].Region ? Cells[next].EnterCost : 0);
                if (cost >= distance[next]) continue;
                distance[next] = cost; entry[next] = point; parent[next] = index; previous[next] = portal; queue.Enqueue((next, cost), (cost + Math.Sqrt(DistanceSquared(point, to)) * Cells[next].TravelCost, -cost, next));
            }
        }
        to = Closest(Cells[best], to); if (best == start) return from == to ? [from] : [from, to];
        var corridor = new List<(Vector2 Left, Vector2 Right)>(); var at = best;
        while (at != start)
        {
            var edge = previous[at]; var mid = edge.A * .5f + edge.B * .5f; var fromCenter = Cells[parent[at]].Center; var toCenter = Cells[at].Center;
            corridor.Add((((double)toCenter.X - fromCenter.X) * ((double)edge.A.Y - mid.Y) - ((double)toCenter.Y - fromCenter.Y) * ((double)edge.A.X - mid.X)) >= 0 ? (edge.A, edge.B) : (edge.B, edge.A)); at = parent[at];
        }
        corridor.Reverse();
        if (!optimize) { var path = new List<Vector2> { from }; foreach (var p in corridor) AddDistinct(path, p.Left * .5f + p.Right * .5f); AddDistinct(path, to); return path.ToArray(); }
        return Funnel(from, to, corridor);
    }
    private static void AddDistinct(List<Vector2> path, Vector2 point) { if (path.Count == 0 || path[^1] != point) path.Add(point); }
    private static Vector2[] Funnel(Vector2 from, Vector2 to, List<(Vector2 Left, Vector2 Right)> portals)
    {
        portals.Add((to, to)); var result = new List<Vector2> { from }; var apex = from; var left = from; var right = from; var apexIndex = 0; var leftIndex = 0; var rightIndex = 0;
        for (var i = 0; i < portals.Count; i++)
        {
            var nextLeft = portals[i].Left; var nextRight = portals[i].Right;
            if (-Area(apex, right, nextRight) <= 0)
            {
                if (apex == right || -Area(apex, left, nextRight) > 0) { right = nextRight; rightIndex = i; }
                else { AddDistinct(result, left); apex = left; apexIndex = leftIndex; left = right = apex; leftIndex = rightIndex = apexIndex; i = apexIndex; continue; }
            }
            if (-Area(apex, left, nextLeft) >= 0)
            {
                if (apex == left || -Area(apex, right, nextLeft) < 0) { left = nextLeft; leftIndex = i; }
                else { AddDistinct(result, right); apex = right; apexIndex = rightIndex; left = right = apex; leftIndex = rightIndex = apexIndex; i = apexIndex; }
            }
        }
        AddDistinct(result, to); return result.ToArray();
    }
    internal Rect2 Bounds(RID region)
    {
        var found = false; var minimum = Vector2.Zero; var maximum = Vector2.Zero;
        foreach (var cell in Cells) if (cell.Region == region) foreach (var point in cell.Vertices) { if (!found) { minimum = maximum = point; found = true; } else { minimum = minimum.Min(point); maximum = maximum.Max(point); } }
        return found ? new(minimum, maximum - minimum) : default;
    }
}
