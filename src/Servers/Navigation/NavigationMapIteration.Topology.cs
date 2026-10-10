using System.Numerics;

namespace Electron2D;

internal sealed partial class NavigationMapIteration
{
    internal readonly record struct TopologyProfile(int Polygons, int Edges, int Merges, int Connections, int FreeEdges);
    private readonly record struct PointKey(BigInteger X, BigInteger Y) : IComparable<PointKey>
    {
        public int CompareTo(PointKey other) { var y = Y.CompareTo(other.Y); return y != 0 ? y : X.CompareTo(other.X); }
    }
    private readonly record struct RasterEdge(PointKey A, PointKey B);
    private readonly record struct Boundary(int Cell, Vector2 A, Vector2 B);
    private sealed class Owners(Boundary first) { internal readonly Boundary First = first; internal Boundary? Second; }
    internal TopologyProfile Profile { get; private init; }
    private Dictionary<RID, (Vector2 Start, Vector2 End)[]> Connections { get; init; } = [];
    internal int ConnectionCount(RID region) => Connections.TryGetValue(region, out var value) ? value.Length : 0;
    internal (Vector2 Start, Vector2 End) Connection(RID region, int index)
    {
        if (!Connections.TryGetValue(region, out var value) || (uint)index >= (uint)value.Length) throw new ArgumentOutOfRangeException(nameof(index));
        return value[index];
    }
    private static RasterEdge RasterKey(Vector2 a, Vector2 b, float dimension)
    {
        PointKey Point(Vector2 p) => new(Coordinate(p.X), Coordinate(p.Y));
        BigInteger Coordinate(float value)
        {
            var ratio = value / dimension;
            return new(float.IsFinite(ratio) ? MathF.Floor(ratio) : Math.Floor((double)value / dimension));
        }
        var p = Point(a); var q = Point(b); return p.CompareTo(q) <= 0 ? new(p, q) : new(q, p);
    }
    private static void AddOwner(Dictionary<RasterEdge, Owners> owners, RasterEdge key, Boundary edge)
    {
        if (!owners.TryGetValue(key, out var pair)) owners.Add(key, new(edge));
        else if (pair.Second is null) pair.Second = edge;
    }
    private static (List<Portal>[] Edges, TopologyProfile Profile, Dictionary<RID, (Vector2 Start, Vector2 End)[]> Connections) BuildTopology(NavigationMapState map, List<Cell> cells)
    {
        var edges = new List<Portal>[cells.Count]; for (var i = 0; i < edges.Length; i++) edges[i] = [];
        var regional = new Dictionary<RID, Dictionary<RasterEdge, Owners>>();
        var external = new Dictionary<RasterEdge, Owners>();
        var dimension = map.CellSize * map.RasterScale;
        for (var i = 0; i < cells.Count; i++)
        {
            var cell = cells[i]; if (!regional.TryGetValue(cell.Region, out var owners)) regional.Add(cell.Region, owners = []);
            for (var j = 0; j < cell.Vertices.Length; j++)
            { var a = cell.Vertices[j]; var b = cell.Vertices[(j + 1) % cell.Vertices.Length]; AddOwner(owners, RasterKey(a, b, dimension), new(i, a, b)); }
        }
        var edgeCount = 0; var merged = 0;
        foreach (var owners in regional.Values)
        {
            edgeCount = checked(edgeCount + owners.Count);
            foreach (var pair in owners)
            {
                if (pair.Value.Second is { } second) { Connect(pair.Value.First, second); merged++; }
                else AddOwner(external, pair.Key, pair.Value.First);
            }
        }
        var free = new List<Boundary>(); var connected = 0;
        foreach (var pair in external.Values)
        {
            if (pair.Second is { } second) { Connect(pair.First, second); connected++; }
            else if (map.UseEdgeConnections && cells[pair.First.Cell].Connect) free.Add(pair.First);
        }
        var pathways = new Dictionary<RID, List<(Vector2 Start, Vector2 End)>>();
        for (var i = 0; i < free.Count; i++) for (var j = 0; j < free.Count; j++)
            {
                var a = free[i]; var b = free[j]; if (i == j || cells[a.Cell].Region == cells[b.Cell].Region) continue;
                var dx = (double)a.B.X - a.A.X; var dy = (double)a.B.Y - a.A.Y; var length = dx * dx + dy * dy;
                if (length == 0) continue;
                var r0 = (((double)b.A.X - a.A.X) * dx + ((double)b.A.Y - a.A.Y) * dy) / length;
                var r1 = (((double)b.B.X - a.A.X) * dx + ((double)b.B.Y - a.A.Y) * dy) / length;
                if (r0 < 0 && r1 < 0 || r0 > 1 && r1 > 1) continue;
                var c0 = Math.Clamp(r0, 0, 1); var c1 = Math.Clamp(r1, 0, 1);
                var p = Interpolate(a.A, a.B, c0); var q = Interpolate(a.A, a.B, c1);
                var r = c0 == r0 ? b.A : Interpolate(b.A, b.B, (c0 - r0) / (r1 - r0));
                var s = c1 == r1 ? b.B : Interpolate(b.A, b.B, (c1 - r0) / (r1 - r0));
                if (!Near(p, r, map.EdgeMargin) || !Near(q, s, map.EdgeMargin)) continue;
                var start = p * .5f + r * .5f; var end = q * .5f + s * .5f;
                if (!start.IsFinite() || !end.IsFinite()) throw new ArgumentException("Navigation connection produces nonfinite geometry.");
                edges[a.Cell].Add(new(b.Cell, start, end, b.A * .5f + b.B * .5f)); connected = checked(connected + 1);
                if (!pathways.TryGetValue(cells[a.Cell].Region, out var values)) pathways.Add(cells[a.Cell].Region, values = []);
                values.Add((start, end));
            }
        return (edges, new(cells.Count, edgeCount, merged, connected, free.Count), pathways.ToDictionary(p => p.Key, p => p.Value.ToArray()));
        void Connect(Boundary a, Boundary b) { edges[a.Cell].Add(new(b.Cell, b.A, b.B)); edges[b.Cell].Add(new(a.Cell, a.A, a.B)); }
    }
}
