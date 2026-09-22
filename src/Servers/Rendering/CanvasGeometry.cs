using System.Runtime.InteropServices;

namespace Electron2D;

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct CanvasVertex(Vector2 Position, Color Color, Vector2 UV = default);

internal readonly record struct CanvasCommand(bool Line, Vector2 A, Vector2 B, Color Color, bool Filled,
    float Width, bool Antialiased, Transform Transform, Texture? Texture = null, Rect Source = default,
    bool Transpose = false, bool ClipUV = false, bool Tile = false);

internal static class CanvasGeometry
{
    internal static void Append(List<CanvasVertex> output, CanvasCommand command, Transform transform, Color modulation)
    {
        var color = command.Color * modulation;
        if (!color.IsFinite()) throw new InvalidOperationException("Canvas modulation overflowed finite colors.");
        if (command.Texture is not null) { AppendTexture(output, command, transform, color); return; }
        Span<Vector2> outer = stackalloc Vector2[4];
        Span<Vector2> inner = stackalloc Vector2[4];
        if (command.Line)
        {
            if (command.Width == 0f || command.A == command.B) return;
            var a = command.A;
            var b = command.B;
            if (command.Width < 0) { a = transform * a; b = transform * b; transform = Transform.Identity; }
            var direction = (b - a).Normalized();
            var normal = new Vector2(-direction.Y, direction.X) * (command.Width < 0 ? 0.5f : command.Width / 2);
            outer[0] = transform * (a + normal); outer[1] = transform * (b + normal);
            outer[2] = transform * (b - normal); outer[3] = transform * (a - normal);
            if (!ValidQuad(outer)) return;
            Quad(output, outer, color);
        }
        else
        {
            var rect = new Rect(command.A, command.B).Abs();
            if (!rect.HasArea() || !command.Filled && command.Width == 0f) return;
            SetQuad(outer, rect, transform);
            if (!ValidQuad(outer)) return;
            if (command.Filled) Quad(output, outer, color);
            else
            {
                if (command.Width < 0)
                {
                    Offset(outer, inner, -0.5f);
                    Span<Vector2> expanded = stackalloc Vector2[4];
                    Offset(outer, expanded, 0.5f);
                    expanded.CopyTo(outer);
                }
                else
                {
                    var half = command.Width / 2;
                    SetQuad(outer, rect.Grow(half), transform);
                    var hole = rect.Grow(-half);
                    if (hole.Size.X <= 0 || hole.Size.Y <= 0)
                        inner.Fill(transform * rect.GetCenter());
                    else SetQuad(inner, hole, transform);
                }
                Ring(output, outer, inner, color, color);
                if (command.Antialiased && ValidQuad(inner))
                {
                    Span<Vector2> inset = stackalloc Vector2[4];
                    Offset(inner, inset, -1f);
                    Ring(output, inner, inset, color, new Color(color.R, color.G, color.B, 0));
                }
            }
        }
        if (command.Antialiased)
        {
            Span<Vector2> expanded = stackalloc Vector2[4];
            Offset(outer, expanded, 1f);
            Ring(output, expanded, outer, new Color(color.R, color.G, color.B, 0), color);
        }
    }

    private static void AppendTexture(List<CanvasVertex> output, CanvasCommand command, Transform transform, Color color)
    {
        var pixels = command.Texture!.CapturePixels() ?? throw new InvalidOperationException("The drawn texture has no readable image.");
        var size = command.B.Abs();
        var source = new Rect(command.Source.Position, command.Source.Size.Abs());
        if (size.X == 0 || size.Y == 0 || !source.HasArea()) return;
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
                        uv = new Vector2(MathF.Min(MathF.Max(uv.X, source.Position.X + halfPixel.X), source.End.X - halfPixel.X),
                            MathF.Min(MathF.Max(uv.Y, source.Position.Y + halfPixel.Y), source.End.Y - halfPixel.Y));
                    if (!uv.IsFinite()) throw new InvalidOperationException("Texture coordinates overflowed.");
                    points[i] = transform * (command.A + size * new Vector2(flipX ? 1 - u : u, flipY ? 1 - v : v));
                    quad[i] = new(points[i], color, uv);
                }
                if (!ValidQuad(points)) continue;
                output.Add(quad[0]); output.Add(quad[1]); output.Add(quad[2]);
                output.Add(quad[0]); output.Add(quad[2]); output.Add(quad[3]);
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

    private static void SetQuad(Span<Vector2> points, Rect rect, Transform transform)
    {
        points[0] = transform * rect.Position;
        points[1] = transform * new Vector2(rect.End.X, rect.Position.Y);
        points[2] = transform * rect.End;
        points[3] = transform * new Vector2(rect.Position.X, rect.End.Y);
    }

    private static bool ValidQuad(ReadOnlySpan<Vector2> q)
    {
        foreach (var p in q) if (!p.IsFinite()) throw new InvalidOperationException("Canvas transforms overflowed finite coordinates.");
        return MathF.Abs((q[1] - q[0]).Cross(q[3] - q[0])) > 0.000001f;
    }

    private static void Offset(ReadOnlySpan<Vector2> source, Span<Vector2> target, float distance)
    {
        var sign = MathF.Sign((source[1] - source[0]).Cross(source[3] - source[0]));
        for (var i = 0; i < 4; i++)
        {
            var before = (source[i] - source[(i + 3) % 4]).Normalized();
            var after = (source[(i + 1) % 4] - source[i]).Normalized();
            var n1 = new Vector2(before.Y, -before.X) * sign;
            var n2 = new Vector2(after.Y, -after.X) * sign;
            var divisor = 1f + n1.Dot(n2);
            target[i] = source[i] + (n1 + n2) * (distance / MathF.Max(divisor, 0.000001f));
        }
    }

    private static void Quad(List<CanvasVertex> output, ReadOnlySpan<Vector2> q, Color color)
    {
        Triangle(output, q[0], q[1], q[2], color, color, color);
        Triangle(output, q[0], q[2], q[3], color, color, color);
    }

    private static void Ring(List<CanvasVertex> output, ReadOnlySpan<Vector2> outer, ReadOnlySpan<Vector2> inner, Color outerColor, Color innerColor)
    {
        for (var i = 0; i < 4; i++)
        {
            var j = (i + 1) % 4;
            Triangle(output, outer[i], outer[j], inner[j], outerColor, outerColor, innerColor);
            Triangle(output, outer[i], inner[j], inner[i], outerColor, innerColor, innerColor);
        }
    }

    private static void Triangle(List<CanvasVertex> output, Vector2 a, Vector2 b, Vector2 c, Color ca, Color cb, Color cc)
    {
        output.Add(new(a, ca)); output.Add(new(b, cb)); output.Add(new(c, cc));
    }
}
