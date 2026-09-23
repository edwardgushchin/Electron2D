namespace Electron2D;

// Retained command storage is reused across redraws; callers never own these arrays.
internal sealed class CanvasPolygon
{
    internal CanvasVertex[] Vertices = [];
    internal int[] Indices = [];
    internal int VertexCount;
    internal int IndexCount;
    private int[] _remaining = [];

    internal void Set(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, bool primitive)
    {
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

    private void Triangulate(ReadOnlySpan<Vector2> points)
    {
        // ponytail: ear clipping is cubic in the worst case; replace with indexed spatial search if large redraws need it.
        if (_remaining.Length < points.Length) Array.Resize(ref _remaining, points.Length);
        double area = 0;
        for (var i = 0; i < points.Length; i++)
        {
            var a = points[i]; var b = points[(i + 1) % points.Length];
            area += (double)a.X * b.Y - (double)a.Y * b.X;
        }
        for (var i = 0; i < points.Length; i++) _remaining[i] = area > 0 ? i : points.Length - 1 - i;
        var count = points.Length; var cursor = count - 1; var attempts = 2L * count; var relaxed = false; var output = 0;
        while (count > 2)
        {
            if (attempts-- == 0)
            {
                if (relaxed) throw new ArgumentException("The polygon cannot be triangulated.", nameof(points));
                relaxed = true; attempts = 2L * count;
            }
            var previous = cursor % count; cursor = (previous + 1) % count; var next = (cursor + 1) % count;
            var a = points[_remaining[previous]]; var b = points[_remaining[cursor]]; var c = points[_remaining[next]];
            if (Cross(a, b, c) < (relaxed ? -0.00001 : 0.00001)) continue;
            var contains = false;
            for (var i = 0; i < count; i++)
            {
                if (i == previous || i == cursor || i == next) continue;
                var point = points[_remaining[i]];
                var ab = Cross(a, b, point); var bc = Cross(b, c, point); var ca = Cross(c, a, point);
                if (relaxed ? ab > 0 && bc > 0 && ca > 0 : ab >= 0 && bc >= 0 && ca >= 0) { contains = true; break; }
            }
            if (contains) continue;
            Indices[output++] = _remaining[previous]; Indices[output++] = _remaining[cursor]; Indices[output++] = _remaining[next];
            _remaining.AsSpan(cursor + 1, count - cursor - 1).CopyTo(_remaining.AsSpan(cursor));
            count--; attempts = 2L * count;
        }
    }

    private static double Cross(Vector2 a, Vector2 b, Vector2 c) =>
        ((double)b.X - a.X) * ((double)c.Y - a.Y) - ((double)b.Y - a.Y) * ((double)c.X - a.X);

    internal void RemapUV(Rect mapping)
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
        if (VertexCount >= 3)
        {
            for (var i = 0; i < IndexCount; i++) output.Add(TransformVertex(Vertices[Indices[i]], transform, modulation, snap));
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
