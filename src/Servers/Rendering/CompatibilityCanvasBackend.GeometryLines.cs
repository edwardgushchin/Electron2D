using SDL3;

namespace Electron2D;

internal sealed partial class CompatibilityCanvasBackend
{
    // Adapted SDL geometry-line endpoint squares and connecting triangles; see licence/SDL-LICENSE.txt.
    private int DrawGeometryAttributeLines(nint renderer, nint texture, ReadOnlySpan<SDL.Vertex> vertices, Mesh.PrimitiveType primitive, bool snap)
    {
        var capacity = checked(vertices.Length * 18);
        if (_attributeTriangles.Length < capacity) Array.Resize(ref _attributeTriangles, Math.Max(capacity, _attributeTriangles.Length * 2));
        var connected = primitive == Mesh.PrimitiveType.LineStrip; var count = 0;
        var first = vertices[0]; first.Position = LinePoint(first.Position, snap);
        if (connected && !EqualPoint(first.Position, LinePoint(vertices[^1].Position, snap)))
            AppendGeometryLine(first, first, [0, 1, 2, 0, 2, 3], ref count);
        for (var i = 1; i < vertices.Length; i += connected ? 1 : 2)
        {
            var a = vertices[i - 1]; var b = vertices[i];
            a.Position = LinePoint(a.Position, snap); b.Position = LinePoint(b.Position, snap);
            if (EqualPoint(a.Position, b.Position)) continue;
            if (!connected) AppendGeometryLine(a, a, [0, 1, 2, 0, 2, 3], ref count);
            AppendGeometryLine(b, b, [0, 1, 2, 0, 2, 3], ref count);
            ReadOnlySpan<byte> indices;
            if (a.Position.Y == b.Position.Y)
                indices = a.Position.X < b.Position.X ? [1, 4, 7, 1, 7, 2] : [5, 0, 3, 5, 3, 6];
            else if (a.Position.X == b.Position.X)
                indices = a.Position.Y < b.Position.Y ? [2, 5, 4, 2, 4, 3] : [6, 1, 0, 6, 0, 7];
            else if (a.Position.Y < b.Position.Y)
                indices = a.Position.X < b.Position.X ? [1, 5, 4, 1, 4, 2, 2, 4, 7, 2, 7, 3] : [4, 0, 5, 5, 0, 3, 5, 3, 6, 6, 3, 2];
            else
                indices = a.Position.X < b.Position.X ? [0, 4, 7, 0, 7, 1, 1, 7, 6, 1, 6, 2] : [6, 5, 1, 6, 1, 0, 7, 6, 0, 7, 0, 3];
            AppendGeometryLine(a, b, indices, ref count);
        }
        if (count == 0) return 0;
        Check(SDL.RenderGeometry(renderer, texture, _attributeTriangles.AsSpan(0, count), count, 0, 0), "draw compatibility geometry attribute lines");
        return 1;
    }

    private void AppendGeometryLine(SDL.Vertex a, SDL.Vertex b, ReadOnlySpan<byte> indices, ref int count)
    {
        foreach (var index in indices)
        {
            var vertex = index < 4 ? a : b; var corner = index % 4;
            if (corner is 1 or 2) vertex.Position.X++;
            if (corner is 2 or 3) vertex.Position.Y++;
            _attributeTriangles[count++] = vertex;
        }
    }
}
