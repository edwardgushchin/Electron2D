namespace Electron2D;

internal sealed partial class CanvasPolygon
{
    private bool _antialiased;
    private readonly Dictionary<(int A, int B), int> _edgeCounts = [];
    private readonly List<(int A, int B)> _edges = [];
    private Vector2[] _contour = [], _normals = [], _edgeNormals = [];
    private int[] _contourTriangles = [], _sourceMap = [], _skinSource = [];
    private CanvasVertex[] _transformed = [];

    private void SetContours(ReadOnlySpan<Vector2> points, int[][] contours)
    {
        _hasContourMapping = true;
        if (_transformed.Length < VertexCount) Array.Resize(ref _transformed, VertexCount);
        Vertices.AsSpan(0, VertexCount).CopyTo(_transformed);
        if (_sourceMap.Length < VertexCount) Array.Resize(ref _sourceMap, VertexCount);
        if (_skinSource.Length < VertexCount) Array.Resize(ref _skinSource, VertexCount);
        Array.Fill(_sourceMap, -1, 0, VertexCount); VertexCount = IndexCount = 0;
        foreach (var contour in contours)
        {
            if (contour.Length < 3) continue;
            if (_contour.Length < contour.Length) Array.Resize(ref _contour, contour.Length);
            if (_remaining.Length < contour.Length) Array.Resize(ref _remaining, contour.Length);
            var count = checked((contour.Length - 2) * 3);
            if (_contourTriangles.Length < count) Array.Resize(ref _contourTriangles, count);
            for (var i = 0; i < contour.Length; i++)
            {
                if ((uint)contour[i] >= (uint)points.Length) throw new ArgumentOutOfRangeException(nameof(contours), "A contour index is outside Vertices.");
                var source = contour[i]; _contour[i] = points[source];
                if (_sourceMap[source] < 0) { _sourceMap[source] = VertexCount; _skinSource[VertexCount] = source; Vertices[VertexCount++] = _transformed[source]; }
            }
            if (!Geometry.TryTriangulatePolygon(_contour.AsSpan(0, contour.Length), _remaining, _contourTriangles))
                throw new ArgumentException("The polygon contour cannot be triangulated.", nameof(contours));
            var end = checked(IndexCount + count); if (Indices.Length < end) Array.Resize(ref Indices, end);
            for (var i = 0; i < count; i++) Indices[IndexCount + i] = _sourceMap[contour[_contourTriangles[i]]];
            IndexCount = end;
        }
    }

    internal void EnableAntialias(ReadOnlySpan<Vector2> invertedInner)
    {
        _antialiased = true; _edges.Clear(); _edgeCounts.Clear();
        if (!invertedInner.IsEmpty)
        {
            // The triangulation bridge is a numerical slit, not a visible boundary.
            var bounds = new Rect2(Vertices[0].Position, Vector2.Zero);
            for (var i = 1; i < VertexCount; i++) bounds = bounds.Expand(Vertices[i].Position);
            Span<Vector2> outer = [bounds.Position, new(bounds.End.X, bounds.Position.Y), bounds.End, new(bounds.Position.X, bounds.End.Y)];
            AddLoop(outer, positive: true); AddLoop(invertedInner, positive: false);
        }
        else
        {
            for (var i = 0; i < IndexCount; i += 3)
            {
                AddEdge(Indices[i], Indices[i + 1]); AddEdge(Indices[i + 1], Indices[i + 2]); AddEdge(Indices[i + 2], Indices[i]);
            }
            foreach (var (edge, count) in _edgeCounts) for (var i = 0; i < count; i++) _edges.Add(edge);
        }
        if (_transformed.Length < VertexCount) Array.Resize(ref _transformed, VertexCount);
        if (_normals.Length < VertexCount) Array.Resize(ref _normals, VertexCount);
        if (_edgeNormals.Length < _edges.Count) Array.Resize(ref _edgeNormals, _edges.Count);
    }

    private void AddEdge(int a, int b)
    {
        if (_edgeCounts.TryGetValue((b, a), out var reverse) && reverse > 0) _edgeCounts[(b, a)] = reverse - 1;
        else _edgeCounts[(a, b)] = _edgeCounts.GetValueOrDefault((a, b)) + 1;
    }

    private void AddLoop(ReadOnlySpan<Vector2> points, bool positive)
    {
        double area = 0;
        for (var i = 0; i < points.Length; i++) { var a = points[i]; var b = points[(i + 1) % points.Length]; area += (double)a.X * b.Y - (double)a.Y * b.X; }
        for (var i = 0; i < points.Length; i++)
        {
            var a = FindPoint(points[i]); var b = FindPoint(points[(i + 1) % points.Length]);
            _edges.Add((area > 0 == positive) ? (a, b) : (b, a));
        }
    }

    private int FindPoint(Vector2 point)
    {
        for (var i = 0; i < VertexCount; i++) if (Vertices[i].Position == point) return i;
        throw new InvalidOperationException("The inverted boundary is absent from its triangulated contour.");
    }

    private void AppendAntialiased(List<CanvasVertex> output, Transform transform, Color modulation, bool snap, bool linear, bool skinned, BlendMode blend)
    {
        if (blend == BlendMode.Mul) throw new NotSupportedException("Polygon alpha-fringe smoothing requires an alpha-aware blend mode.");
        for (var i = 0; i < VertexCount; i++)
            _transformed[i] = TransformVertex(skinned ? Vertices[i] with { Position = _skin!.Position(i) } : Vertices[i], transform, modulation, snap, linear);
        for (var i = 0; i < IndexCount; i++) output.Add(_transformed[Indices[i]]);
        Array.Clear(_normals, 0, VertexCount); double area = 0;
        foreach (var (a, b) in _edges) { var from = _transformed[a].Position; var to = _transformed[b].Position; area += (double)from.X * to.Y - (double)from.Y * to.X; }
        if (area == 0) return;
        var sign = area > 0 ? 1 : -1;
        for (var i = 0; i < _edges.Count; i++)
        {
            var (a, b) = _edges[i]; var from = _transformed[a].Position; var to = _transformed[b].Position;
            var dx = (double)to.X - from.X; var dy = (double)to.Y - from.Y; var length = Math.Sqrt(dx * dx + dy * dy);
            var normal = length == 0 ? Vector2.Zero : new Vector2((float)(dy / length * sign), (float)(-dx / length * sign));
            _edgeNormals[i] = normal; _normals[a] += normal; _normals[b] += normal;
        }
        for (var i = 0; i < _edges.Count; i++)
        {
            var (from, to) = _edges[i]; var normal = _edgeNormals[i]; if (normal == Vector2.Zero) continue;
            var a = _transformed[from]; var b = _transformed[to];
            var c = b with { Position = b.Position + FringeOffset(_normals[to], normal), Color = blend == BlendMode.PremultAlpha ? new Color(0, 0, 0, 0) : b.Color with { A = 0 } };
            var d = a with { Position = a.Position + FringeOffset(_normals[from], normal), Color = blend == BlendMode.PremultAlpha ? new Color(0, 0, 0, 0) : a.Color with { A = 0 } };
            if (!c.Position.IsFinite() || !d.Position.IsFinite()) throw new InvalidOperationException("Polygon antialias geometry overflowed finite coordinates.");
            output.Add(a); output.Add(b); output.Add(c); output.Add(a); output.Add(c); output.Add(d);
        }
    }

    private static Vector2 FringeOffset(Vector2 sum, Vector2 normal)
    {
        var length = sum.Length(); var denominator = sum.Dot(normal);
        if (length == 0 || denominator <= 0) return normal;
        return sum * (denominator * 4 >= length ? 1 / denominator : 4 / length);
    }
}
