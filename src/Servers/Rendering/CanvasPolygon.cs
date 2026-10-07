namespace Electron2D;

// Retained command storage is reused across redraws; callers never own these arrays.
internal sealed class CanvasPolygon
{
    internal CanvasVertex[] Vertices = [];
    internal int[] Indices = [];
    internal int VertexCount;
    internal int IndexCount;
    private int[] _remaining = [];
    private CanvasSkeletonSkin? _skin;
    private bool _skinEnabled;
    internal void SetSkin(Polygon owner, ReadOnlySpan<int> source) { (_skin ??= new()).Set(owner, source, VertexCount); _skinEnabled = true; }

    internal Rect2 GetLocalBounds() { var bounds = VertexCount == 0 ? default : new Rect2(Vertices[0].Position, Vector2.Zero); for (var i = 1; i < VertexCount; i++) bounds = bounds.Expand(Vertices[i].Position); return bounds; }
    internal void Set(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, bool primitive)
    {
        _skinEnabled = false;
        if (Vertices.Length < points.Length) Array.Resize(ref Vertices, points.Length);
        VertexCount = points.Length;
        for (var i = 0; i < points.Length; i++)
            Vertices[i] = new(points[i], colors.IsEmpty ? Colors.White : colors[i < colors.Length ? i : 0], i < uvs.Length ? uvs[i] : Vector2.Zero);
        IndexCount = primitive ? Math.Max(0, points.Length - 2) * 3 : checked((points.Length - 2) * 3);
        if (Indices.Length < IndexCount) Array.Resize(ref Indices, IndexCount);
        if (primitive)
        {
            if (points.Length >= 3) { Indices[0] = 0; Indices[1] = 1; Indices[2] = 2; }
            if (points.Length == 4) { Indices[3] = 0; Indices[4] = 2; Indices[5] = 3; }
            return;
        }
        Triangulate(points);
    }

    internal void SetTriangles(ReadOnlySpan<CanvasVertex> triangles)
    {
        _skinEnabled = false;
        if (Vertices.Length < triangles.Length) Array.Resize(ref Vertices, triangles.Length);
        if (Indices.Length < triangles.Length) Array.Resize(ref Indices, triangles.Length);
        VertexCount = IndexCount = triangles.Length;
        triangles.CopyTo(Vertices);
        for (var i = 0; i < triangles.Length; i++) Indices[i] = i;
    }

    private void Triangulate(ReadOnlySpan<Vector2> points)
    {
        if (_remaining.Length < points.Length) Array.Resize(ref _remaining, points.Length);
        if (!Geometry.TryTriangulatePolygon(points, _remaining, Indices))
            throw new ArgumentException("The polygon cannot be triangulated.", nameof(points));
    }

    internal void RemapUV(Rect2 mapping)
    {
        for (var i = 0; i < VertexCount; i++)
        {
            var uv = mapping.Position + Vertices[i].UV * mapping.Size;
            if (!uv.IsFinite()) throw new ArgumentException("Atlas texture coordinates overflowed.");
            Vertices[i] = Vertices[i] with { UV = uv };
        }
    }

    internal void Append(List<CanvasVertex> output, Transform transform, Color modulation, bool snap)
    {
        var skinned = _skinEnabled && _skin?.Prepare(Vertices, VertexCount) == true;
        if (VertexCount >= 3)
        {
            for (var i = 0; i < IndexCount; i++) output.Add(TransformVertex(skinned ? Vertices[Indices[i]] with { Position = _skin!.Position(Indices[i]) } : Vertices[Indices[i]], transform, modulation, snap));
            return;
        }
        var a = TransformVertex(Vertices[0], transform, modulation, snap);
        var b = VertexCount == 1 ? a : TransformVertex(Vertices[1], transform, modulation, snap);
        if (VertexCount == 2 && a.Position == b.Position) return;
        var dx = (double)b.Position.X - a.Position.X; var dy = (double)b.Position.Y - a.Position.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        var normal = VertexCount == 1 ? new Vector2(0.5f, 0) : new Vector2((float)(dy / length * 0.5), (float)(-dx / length * 0.5));
        if (VertexCount == 1) { a = a with { Position = a.Position - new Vector2(0, 0.5f) }; b = b with { Position = b.Position + new Vector2(0, 0.5f) }; }
        var q0 = a with { Position = a.Position + normal }; var q1 = b with { Position = b.Position + normal };
        var q2 = b with { Position = b.Position - normal }; var q3 = a with { Position = a.Position - normal };
        output.Add(q0); output.Add(q1); output.Add(q2); output.Add(q0); output.Add(q2); output.Add(q3);
    }

    private static CanvasVertex TransformVertex(CanvasVertex vertex, Transform transform, Color modulation, bool snap)
    {
        var position = transform * vertex.Position; var color = vertex.Color * modulation;
        if (!position.IsFinite() || !color.IsFinite()) throw new InvalidOperationException("Canvas geometry overflowed finite values.");
        return vertex with { Position = snap ? CanvasGeometry.Snap(position) : position, Color = color };
    }
}
