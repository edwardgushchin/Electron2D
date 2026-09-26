using System.Runtime.InteropServices;

namespace Electron2D;

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct CanvasVertex(Vector2 Position, Color Color, Vector2 UV = default);

internal readonly record struct CanvasCommand(bool Line, Vector2 A, Vector2 B, Color Color,
    float Width, bool Antialiased, Transform Transform, Texture? Texture = null, Rect2 Source = default,
    bool Transpose = false, bool ClipUV = false, bool Tile = false, CanvasPolygon? Polygon = null, CanvasStroke? Stroke = null, bool SetTransform = false, CanvasAnimationSlice? AnimationSlice = null, CanvasNinePatch? NinePatch = null, bool ConstantSource = false);

internal readonly record struct CanvasAnimationSlice(double Length, double Begin, double End, double Offset)
{
    internal bool Includes(double time)
    {
        var local = Mathf.PosMod(time - Offset, Length);
        return local >= Begin && local < End;
    }
}

internal static class CanvasGeometry
{
    internal static void Append(List<CanvasVertex> output, CanvasCommand command, Transform transform, Color modulation, bool snapVertices = false)
    {
        if (command.NinePatch is { } ninePatch) { ninePatch.Append(output, command, transform, modulation, snapVertices); return; }
        if (command.Stroke is { } stroke) { stroke.Append(output, transform, modulation, snapVertices); return; }
        if (command.Polygon is { } polygon) { polygon.Append(output, transform, modulation, snapVertices); return; }
        var color = command.Color * modulation;
        if (!color.IsFinite()) throw new InvalidOperationException("Canvas modulation overflowed finite colors.");
        if (command.Texture is not null) { AppendTexture(output, command, transform, color, snapVertices); return; }
        if (command.Line) { CanvasStroke.AppendLine(output, command.A, command.B, color, command.Width, command.Antialiased, transform, snapVertices); return; }
        throw new InvalidOperationException("The canvas command has no geometry.");
    }

    internal static Vector2 Snap(Vector2 point) => (point + new Vector2(0.5f, 0.5f)).Floor();

    private static void AppendTexture(List<CanvasVertex> output, CanvasCommand command, Transform transform, Color color, bool snapVertices)
    {
        var pixels = command.Texture!.CapturePixels() ?? throw new InvalidOperationException("The drawn texture has no readable image.");
        var size = command.B.Abs();
        var source = new Rect2(command.Source.Position, command.Source.Size.Abs());
        if (size.X == 0 || size.Y == 0 || !source.HasArea() && !command.ConstantSource) return;
        if (command.Transpose) size = new Vector2(size.Y, size.X);
        var flipX = (command.B.X < 0) != (command.Source.Size.X < 0);
        var flipY = (command.B.Y < 0) != (command.Source.Size.Y < 0);
        var halfPixel = new Vector2(0.5f / pixels.Source.Width, 0.5f / pixels.Source.Height);
        var border = halfPixel / source.Size;
        if (command.Transpose) border = new Vector2(border.Y, border.X);
        Span<float> xs = stackalloc float[4];
        Span<float> ys = stackalloc float[4];
        var nx = Cuts(xs, command.ClipUV ? border.X : 0);
        var ny = Cuts(ys, command.ClipUV ? border.Y : 0);
        if (snapVertices)
        {
            AppendSnappedTexture(output, command, transform, color, source, size, halfPixel, flipX, flipY, xs[..nx], ys[..ny]);
            return;
        }
        Span<CanvasVertex> quad = stackalloc CanvasVertex[4];
        Span<Vector2> points = stackalloc Vector2[4];
        for (var y = 0; y < ny - 1; y++)
            for (var x = 0; x < nx - 1; x++)
            {
                for (var i = 0; i < 4; i++)
                {
                    var u = xs[x + (i is 1 or 2 ? 1 : 0)];
                    var v = ys[y + (i >= 2 ? 1 : 0)];
                    var uv = source.Position + source.Size * (command.Transpose ? new Vector2(v, u) : new Vector2(u, v));
                    if (command.ClipUV)
                        uv = new Vector2(Mathf.Min(Mathf.Max(uv.X, source.Position.X + halfPixel.X), source.End.X - halfPixel.X),
                            Mathf.Min(Mathf.Max(uv.Y, source.Position.Y + halfPixel.Y), source.End.Y - halfPixel.Y));
                    if (!uv.IsFinite()) throw new InvalidOperationException("Texture coordinates overflowed.");
                    points[i] = transform * (command.A + size * new Vector2(flipX ? 1 - u : u, flipY ? 1 - v : v));
                    quad[i] = new(points[i], color, uv);
                }
                if (!ValidQuad(points)) continue;
                output.Add(quad[0]); output.Add(quad[1]); output.Add(quad[2]);
                output.Add(quad[0]); output.Add(quad[2]); output.Add(quad[3]);
            }
    }

    private static void AppendSnappedTexture(List<CanvasVertex> output, CanvasCommand command, Transform transform,
        Color color, Rect2 source, Vector2 size, Vector2 halfPixel, bool flipX, bool flipY, ReadOnlySpan<float> xs, ReadOnlySpan<float> ys)
    {
        Span<Vector2> corners = stackalloc Vector2[4];
        for (var i = 0; i < 4; i++)
        {
            var u = i is 1 or 2 ? 1f : 0f; var v = i >= 2 ? 1f : 0f;
            var point = transform * (command.A + size * new Vector2(flipX ? 1 - u : u, flipY ? 1 - v : v));
            if (!point.IsFinite()) throw new InvalidOperationException("Canvas transforms overflowed finite coordinates.");
            corners[i] = Snap(point);
        }
        Span<Vector2> cell = stackalloc Vector2[4];
        Span<Vector2> polygon = stackalloc Vector2[5];
        Span<CanvasVertex> vertices = stackalloc CanvasVertex[5];
        for (var y = 0; y < ys.Length - 1; y++)
            for (var x = 0; x < xs.Length - 1; x++)
            {
                cell[0] = new(xs[x], ys[y]); cell[1] = new(xs[x + 1], ys[y]);
                cell[2] = new(xs[x + 1], ys[y + 1]); cell[3] = new(xs[x], ys[y + 1]);
                // Clipping cuts are interpolation points, not primitive corners. Keep them on the two snapped triangles.
                for (var side = -1; side <= 1; side += 2)
                {
                    var count = 0;
                    for (var i = 0; i < 4; i++)
                    {
                        var a = cell[i]; var b = cell[(i + 1) % 4];
                        var da = side * (a.X - a.Y); var db = side * (b.X - b.Y);
                        if (da >= 0) polygon[count++] = a;
                        if (da > 0 && db < 0 || da < 0 && db > 0) polygon[count++] = a.Lerp(b, da / (da - db));
                    }
                    for (var i = 0; i < count; i++)
                    {
                        var p = polygon[i];
                        var position = side > 0
                            ? corners[0] * (1 - p.X) + corners[1] * (p.X - p.Y) + corners[2] * p.Y
                            : corners[0] * (1 - p.Y) + corners[2] * p.X + corners[3] * (p.Y - p.X);
                        var uv = source.Position + source.Size * (command.Transpose ? new Vector2(p.Y, p.X) : p) + new Vector2(0.00001f, 0.00001f);
                        if (command.ClipUV)
                            uv = new(Mathf.Min(Mathf.Max(uv.X, source.Position.X + halfPixel.X), source.End.X - halfPixel.X),
                                Mathf.Min(Mathf.Max(uv.Y, source.Position.Y + halfPixel.Y), source.End.Y - halfPixel.Y));
                        if (!uv.IsFinite() || !position.IsFinite()) throw new InvalidOperationException("Texture coordinates overflowed.");
                        vertices[i] = new(position, color, uv);
                    }
                    for (var i = 1; i + 1 < count; i++)
                    {
                        if (Mathf.Abs((vertices[i].Position - vertices[0].Position).Cross(vertices[i + 1].Position - vertices[0].Position)) <= 0.000001f) continue;
                        output.Add(vertices[0]); output.Add(vertices[i]); output.Add(vertices[i + 1]);
                    }
                }
            }
    }

    // Piecewise interpolation preserves the interior UVs while clamping only the half-texel border.
    private static int Cuts(Span<float> values, float border)
    {
        values[0] = 0;
        if (border > 0 && border < 0.5f)
        {
            values[1] = border; values[2] = 1 - border; values[3] = 1;
            return 4;
        }
        values[1] = 1;
        return 2;
    }

    private static bool ValidQuad(ReadOnlySpan<Vector2> q)
    {
        foreach (var p in q) if (!p.IsFinite()) throw new InvalidOperationException("Canvas transforms overflowed finite coordinates.");
        return Mathf.Abs((q[1] - q[0]).Cross(q[3] - q[0])) > 0.000001f;
    }

}
