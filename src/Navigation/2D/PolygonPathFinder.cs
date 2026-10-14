namespace Electron2D;

/// <summary>Finds weighted visibility paths inside a stored even-odd polygon area.</summary>
/// <remarks>Closed boundary edges may describe concave contours, holes and separate islands. Boundary
/// points belong to the area; outside path endpoints project to its nearest boundary. Setup copies its
/// inputs and resets penalties. Queries synchronize per resource. Returned arrays belong to the caller;
/// prepared scalar queries allocate nothing. Geometry preparation is limited to 2,048 points and 4,096
/// unique edges. No scene, renderer or native host is required.</remarks>
public sealed class PolygonPathFinder : Resource
{
    private readonly object _gate = new();
    private readonly record struct Edge(int A, int B);
    private readonly record struct Point(double X, double Y)
    {
        internal Point(Vector2 value) : this(value.X, value.Y) { }
        internal Vector2 Export() => new((float)X, (float)Y);
    }
    private sealed record Graph(Vector2[] Points, Edge[] Edges, int[][] Neighbors, Rect2 Bounds);
    private static readonly Graph Empty = new([], [], [], default);
    private Graph _graph = Empty;
    private float[] _penalties = [];
    private readonly List<double> _cuts = [];
    private double[] _distances = [];
    private int[] _previous = [];
    private bool[] _closed = [], _fromVisible = [], _toVisible = [];

    /// <summary>Creates an empty polygon path resource.</summary>
    public PolygonPathFinder() { }

    /// <summary>Atomically copies boundary geometry and prepares its visibility graph.</summary>
    /// <param name="points">Finite authored vertices, including optional unconnected waypoints.</param>
    /// <param name="connections">Pairs of vertex indices defining undirected boundary edges.</param>
    /// <remarks>Reversed duplicate edges collapse. Boundary vertices must have even degree. Empty input
    /// clears geometry. Invalid input leaves existing geometry and penalties intact; callbacks run after publication.</remarks>
    /// <exception cref="ArgumentException">Input is nonfinite, too large, odd-sized, open or has a zero-length edge.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An edge index is outside the point array.</exception>
    /// <exception cref="OverflowException">Bounds cannot be represented by finite Rect2 dimensions.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void Setup(ReadOnlySpan<Vector2> points, ReadOnlySpan<int> connections)
    {
        ThrowIfDisposed(); var graph = Build(points, connections); var penalties = new float[graph.Points.Length];
        lock (_gate) { ThrowIfDisposed(); _graph = graph; _penalties = penalties; }
        EmitChanged();
    }

    /// <summary>Returns a minimum-cost path after projecting outside endpoints to the boundary.</summary>
    /// <param name="from">Finite starting position.</param><param name="to">Finite destination.</param>
    /// <returns>Caller-owned endpoints and visibility waypoints, or an empty array when no route exists.</returns>
    /// <remarks>Cost is Euclidean length plus each entered authored point's nonnegative penalty.
    /// Equal-cost choices follow point order. A direct route retains both endpoints, including equal endpoints.</remarks>
    /// <exception cref="ArgumentException">An endpoint is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Vector2[] FindPath(Vector2 from, Vector2 to)
    {
        Validate(from); Validate(to);
        lock (_gate)
        {
            ThrowIfDisposed(); var graph = _graph; if (graph.Edges.Length == 0) return [];
            var start = Inside(graph, new(from)) ? new Point(from) : Closest(graph, new(from));
            var end = Inside(graph, new(to)) ? new Point(to) : Closest(graph, new(to));
            EnsureWorkspace(graph.Points.Length + 2, graph.Edges.Length);
            if (Visible(graph, start, end, _cuts)) return [start.Export(), end.Export()];
            var count = graph.Points.Length; var first = count; var last = count + 1;
            Array.Fill(_distances, double.PositiveInfinity); Array.Fill(_previous, -1); Array.Clear(_closed);
            for (var i = 0; i < count; i++)
            { var point = new Point(graph.Points[i]); _fromVisible[i] = Visible(graph, start, point, _cuts); _toVisible[i] = Visible(graph, end, point, _cuts); }
            _distances[first] = 0;
            Point Position(int index) => index == first ? start : index == last ? end : new(graph.Points[index]);
            while (true)
            {
                var current = -1; var best = double.PositiveInfinity;
                for (var i = 0; i < count + 2; i++)
                {
                    if (_closed[i] || double.IsPositiveInfinity(_distances[i])) continue;
                    var estimate = _distances[i] + Distance(Position(i), end);
                    if (estimate < best) { best = estimate; current = i; }
                }
                if (current < 0) return [];
                if (current == last) break;
                _closed[current] = true;
                void Relax(int next)
                {
                    if (_closed[next]) return;
                    var cost = _distances[current] + Distance(Position(current), Position(next)) + (next < count ? _penalties[next] : 0);
                    if (cost < _distances[next]) { _distances[next] = cost; _previous[next] = current; }
                }
                if (current == first) { for (var i = 0; i < count; i++) if (_fromVisible[i]) Relax(i); }
                else { foreach (var next in graph.Neighbors[current]) Relax(next); if (_toVisible[current]) Relax(last); }
            }
            var length = 1; for (var at = last; at != first; at = _previous[at]) length++;
            var result = new Vector2[length]; var index = length - 1;
            for (var at = last; ; at = _previous[at]) { result[index--] = Position(at).Export(); if (at == first) break; }
            return result;
        }
    }

    /// <summary>Returns bounds enclosing every authored point, including isolated waypoints.</summary>
    /// <returns>Zero bounds for empty geometry.</returns>
    public Rect2 GetBounds() { lock (_gate) { ThrowIfDisposed(); return _graph.Bounds; } }
    /// <summary>Returns the closest point on the boundary, even when the query lies inside.</summary>
    /// <param name="point">Finite query point.</param><returns>First-edge nearest point, or zero for empty geometry.</returns>
    /// <exception cref="ArgumentException">The query is nonfinite.</exception>
    public Vector2 GetClosestPoint(Vector2 point) { Validate(point); lock (_gate) { ThrowIfDisposed(); return Closest(_graph, new(point)).Export(); } }
    /// <summary>Tests the closed even-odd area, including its boundary.</summary>
    /// <param name="point">Finite query point.</param><returns>False for empty geometry or a hole interior.</returns>
    /// <exception cref="ArgumentException">The query is nonfinite.</exception>
    public bool IsPointInside(Vector2 point) { Validate(point); lock (_gate) { ThrowIfDisposed(); return Inside(_graph, new(point)); } }
    /// <summary>Returns unique segment crossings per edge in first-authored edge order.</summary>
    /// <param name="from">Finite segment start.</param><param name="to">Finite segment end.</param>
    /// <returns>Caller-owned hits; shared-vertex hits from separate edges remain duplicated. Collinear overlaps have no unique hit.</returns>
    /// <exception cref="ArgumentException">An endpoint is nonfinite.</exception>
    public Vector2[] GetIntersections(Vector2 from, Vector2 to)
    {
        Validate(from); Validate(to); lock (_gate)
        {
            ThrowIfDisposed(); List<Vector2>? hits = null; var a = new Point(from); var b = new Point(to);
            foreach (var edge in _graph.Edges)
                if (Crossing(a, b, new(_graph.Points[edge.A]), new(_graph.Points[edge.B]), out var t))
                    (hits ??= []).Add(Lerp(a, b, t).Export());
            return hits?.ToArray() ?? [];
        }
    }
    /// <summary>Reads one authored waypoint's additional entry cost.</summary>
    /// <param name="index">Zero-based authored point index.</param><returns>Zero after Setup until changed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The point is absent.</exception>
    public float GetPointPenalty(int index) { lock (_gate) { ThrowIfDisposed(); CheckIndex(index); return _penalties[index]; } }
    /// <summary>Sets one authored waypoint's nonnegative entry cost.</summary>
    /// <param name="index">Zero-based authored point index.</param><param name="penalty">Finite nonnegative cost.</param>
    /// <exception cref="ArgumentOutOfRangeException">The point is absent or penalty is invalid.</exception>
    public void SetPointPenalty(int index, float penalty)
    {
        if (!float.IsFinite(penalty) || penalty < 0) throw new ArgumentOutOfRangeException(nameof(penalty));
        lock (_gate) { ThrowIfDisposed(); CheckIndex(index); if (_penalties[index] == penalty) return; _penalties[index] = penalty; }
        EmitChanged();
    }
    private void CheckIndex(int index) { if ((uint)index >= (uint)_penalties.Length) throw new ArgumentOutOfRangeException(nameof(index)); }
    private static void Validate(Vector2 point) { if (!point.IsFinite()) throw new ArgumentException("Polygon points must be finite."); }
    private void EnsureWorkspace(int count, int edges)
    {
        if (_distances.Length != count) { _distances = new double[count]; _previous = new int[count]; _closed = new bool[count]; _fromVisible = new bool[count]; _toVisible = new bool[count]; }
        _cuts.EnsureCapacity(edges * 2 + 2);
    }
    private static Graph Build(ReadOnlySpan<Vector2> points, ReadOnlySpan<int> connections)
    {
        if (points.Length > 2048 || (connections.Length & 1) != 0) throw new ArgumentException("Polygon point budget exceeded or connections are odd-sized.");
        var copied = points.ToArray(); foreach (var point in copied) Validate(point);
        var degrees = new int[copied.Length]; var seen = new HashSet<Edge>(); var edges = new List<Edge>();
        for (var i = 0; i < connections.Length; i += 2)
        {
            var a = connections[i]; var b = connections[i + 1];
            if ((uint)a >= (uint)copied.Length || (uint)b >= (uint)copied.Length) throw new ArgumentOutOfRangeException(nameof(connections));
            if (a == b || copied[a] == copied[b]) throw new ArgumentException("Boundary edges must have nonzero length.", nameof(connections));
            var edge = new Edge(Math.Min(a, b), Math.Max(a, b)); if (!seen.Add(edge)) continue;
            edges.Add(edge); degrees[a]++; degrees[b]++;
            if (edges.Count > 4096) throw new ArgumentException("Polygon edge budget exceeded.", nameof(connections));
        }
        foreach (var degree in degrees) if ((degree & 1) != 0) throw new ArgumentException("Boundary edges must form closed contours.", nameof(connections));
        Rect2 bounds = default;
        if (copied.Length > 0)
        {
            double minX = copied[0].X, maxX = minX, minY = copied[0].Y, maxY = minY;
            foreach (var point in copied) { minX = Math.Min(minX, point.X); maxX = Math.Max(maxX, point.X); minY = Math.Min(minY, point.Y); maxY = Math.Max(maxY, point.Y); }
            if (maxX - minX > float.MaxValue || maxY - minY > float.MaxValue) throw new OverflowException("Polygon bounds exceed Rect2 dimensions.");
            bounds = new((float)minX, (float)minY, (float)(maxX - minX), (float)(maxY - minY));
        }
        var graph = new Graph(copied, edges.ToArray(), new int[copied.Length][], bounds);
        var neighbors = new List<int>[copied.Length]; for (var i = 0; i < neighbors.Length; i++) neighbors[i] = [];
        var cuts = new List<double>(edges.Count * 2 + 2);
        for (var a = 0; a < copied.Length; a++) for (var b = a + 1; b < copied.Length; b++)
                if (Visible(graph, new(copied[a]), new(copied[b]), cuts)) { neighbors[a].Add(b); neighbors[b].Add(a); }
        for (var i = 0; i < neighbors.Length; i++) graph.Neighbors[i] = neighbors[i].ToArray();
        return graph;
    }
    private static double Cross(double ax, double ay, double bx, double by) => ax * by - ay * bx;
    private static double Distance(Point a, Point b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    private static Point Lerp(Point a, Point b, double t) => t == 0 ? a : t == 1 ? b : new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
    private static bool OnSegment(Point point, Point a, Point b)
    {
        if (point.X < Math.Min(a.X, b.X) || point.X > Math.Max(a.X, b.X) || point.Y < Math.Min(a.Y, b.Y) || point.Y > Math.Max(a.Y, b.Y)) return false;
        var dx = b.X - a.X; var dy = b.Y - a.Y; var px = point.X - a.X; var py = point.Y - a.Y;
        var cross = Cross(dx, dy, px, py); var tolerance = 4e-15 * (Math.Abs(dx * py) + Math.Abs(dy * px));
        if (Math.Abs(cross) > tolerance) return false;
        var dot = px * dx + py * dy; return dot >= 0 && dot <= dx * dx + dy * dy;
    }
    private static bool Inside(Graph graph, Point point)
    {
        var inside = false;
        foreach (var edge in graph.Edges)
        {
            var a = new Point(graph.Points[edge.A]); var b = new Point(graph.Points[edge.B]);
            if (OnSegment(point, a, b)) return true;
            if ((a.Y > point.Y) != (b.Y > point.Y) && point.X < a.X + (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y)) inside = !inside;
        }
        return inside;
    }
    private static Point Closest(Graph graph, Point point)
    {
        var result = new Point(0, 0); var best = double.PositiveInfinity;
        foreach (var edge in graph.Edges)
        {
            var a = new Point(graph.Points[edge.A]); var b = new Point(graph.Points[edge.B]); var dx = b.X - a.X; var dy = b.Y - a.Y;
            var t = Math.Clamp(((point.X - a.X) * dx + (point.Y - a.Y) * dy) / (dx * dx + dy * dy), 0, 1);
            var candidate = Lerp(a, b, t); var distance = (point.X - candidate.X) * (point.X - candidate.X) + (point.Y - candidate.Y) * (point.Y - candidate.Y);
            if (distance < best) { best = distance; result = candidate; }
        }
        return result;
    }
    private static bool Crossing(Point a, Point b, Point c, Point d, out double t)
    {
        var rx = b.X - a.X; var ry = b.Y - a.Y; var sx = d.X - c.X; var sy = d.Y - c.Y;
        var denominator = Cross(rx, ry, sx, sy); t = 0; if (denominator == 0) return false;
        t = Cross(c.X - a.X, c.Y - a.Y, sx, sy) / denominator;
        var u = Cross(c.X - a.X, c.Y - a.Y, rx, ry) / denominator;
        return t >= 0 && t <= 1 && u >= 0 && u <= 1;
    }
    private static bool Visible(Graph graph, Point a, Point b, List<double> cuts)
    {
        if (!Inside(graph, a) || !Inside(graph, b)) return false;
        cuts.Clear(); cuts.Add(0); cuts.Add(1);
        var dx = b.X - a.X; var dy = b.Y - a.Y; var length = dx * dx + dy * dy;
        foreach (var edge in graph.Edges)
        {
            var c = new Point(graph.Points[edge.A]); var d = new Point(graph.Points[edge.B]);
            if (Crossing(a, b, c, d, out var t)) cuts.Add(t);
            else if (length > 0 && Cross(dx, dy, c.X - a.X, c.Y - a.Y) == 0 && Cross(dx, dy, d.X - a.X, d.Y - a.Y) == 0)
            { cuts.Add(Math.Clamp(((c.X - a.X) * dx + (c.Y - a.Y) * dy) / length, 0, 1)); cuts.Add(Math.Clamp(((d.X - a.X) * dx + (d.Y - a.Y) * dy) / length, 0, 1)); }
        }
        cuts.Sort();
        for (var i = 1; i < cuts.Count; i++) if (cuts[i] > cuts[i - 1] && !Inside(graph, Lerp(a, b, (cuts[i] + cuts[i - 1]) * .5))) return false;
        return true;
    }
    private byte[] SaveData()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
            writer.Write(1); writer.Write(_graph.Points.Length); writer.Write(_graph.Edges.Length);
            for (var i = 0; i < _graph.Points.Length; i++) { writer.Write(_graph.Points[i].X); writer.Write(_graph.Points[i].Y); writer.Write(_penalties[i]); }
            foreach (var edge in _graph.Edges) { writer.Write(edge.A); writer.Write(edge.B); }
            return stream.ToArray();
        }
    }
    private void LoadData(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes); ThrowIfDisposed();
        if (bytes.Length == 0) { Setup([], []); return; }
        Graph graph; float[] penalties;
        try
        {
            using var stream = new MemoryStream(bytes, false); using var reader = new BinaryReader(stream);
            if (reader.ReadInt32() != 1) throw new InvalidDataException("Unsupported polygon path data version.");
            var count = reader.ReadInt32(); var edges = reader.ReadInt32();
            if ((uint)count > 2048 || (uint)edges > 4096 || bytes.Length != 12 + count * 12 + edges * 8) throw new InvalidDataException("Invalid polygon path data counts or length.");
            var points = new Vector2[count]; penalties = new float[count]; var connections = new int[edges * 2];
            for (var i = 0; i < count; i++) { points[i] = new(reader.ReadSingle(), reader.ReadSingle()); penalties[i] = reader.ReadSingle(); if (!float.IsFinite(penalties[i]) || penalties[i] < 0) throw new InvalidDataException("Invalid polygon point penalty."); }
            for (var i = 0; i < connections.Length; i++) connections[i] = reader.ReadInt32();
            graph = Build(points, connections);
        }
        catch (EndOfStreamException error) { throw new InvalidDataException("Truncated polygon path data.", error); }
        catch (ArgumentException error) { throw new InvalidDataException("Invalid polygon path geometry.", error); }
        catch (OverflowException error) { throw new InvalidDataException("Invalid polygon path bounds.", error); }
        lock (_gate) { ThrowIfDisposed(); _graph = graph; _penalties = penalties; }
        EmitChanged();
    }
    private static readonly PropertyDescriptor[] DataProperties = [new PropertyDescriptor<PolygonPathFinder, byte[]>("_polygon_path", p => p.SaveData(), (p, v) => p.LoadData(v), _ => [], stored: true)];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(DataProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new PolygonPathFinder();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        Graph graph; float[] penalties; lock (_gate) { ThrowIfDisposed(); graph = _graph; penalties = (float[])_penalties.Clone(); }
        var copy = (PolygonPathFinder)target; lock (copy._gate) { copy.ThrowIfDisposed(); copy._graph = graph; copy._penalties = penalties; }
    }
    /// <inheritdoc />
    protected override void OnResetState()
    { lock (_gate) { ThrowIfDisposed(); _distances = []; _previous = []; _closed = []; _fromVisible = []; _toVisible = []; _cuts.Clear(); _cuts.Capacity = 0; } base.OnResetState(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    { lock (_gate) { _graph = Empty; _penalties = []; _distances = []; _previous = []; _closed = []; _fromVisible = []; _toVisible = []; _cuts.Clear(); } base.Dispose(disposing); }
}
