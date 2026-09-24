using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed class CanvasStroke
{
    private const float Feather = 1.25f;
    private readonly List<CanvasVertex> _triangles = [];
    private readonly List<CanvasVertex> _thin = [];
    private readonly List<CanvasVertex> _strip = [];
    private bool _connected;

    internal void Set(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width, bool antialiased, bool connected)
    {
        _triangles.Clear(); _thin.Clear(); _connected = connected;
        if (width < 0)
        {
            for (var i = 0; i < points.Length; i++) _thin.Add(new(points[i], ColorAt(colors, connected ? i : i / 2)));
            return;
        }
        if (!connected)
        {
            for (var i = 0; i < points.Length; i += 2)
                AppendLine(_triangles, points[i], points[i + 1], ColorAt(colors, i / 2), width, antialiased, Transform.Identity, false);
            return;
        }
        if (antialiased) width = CompensatedWidth(width);
        if (width == 0) return;
        var loop = points[0].IsEqualApprox(points[^1]);
        var first = Vector2.Zero; var last = Vector2.Zero;
        for (var i = 1; i < points.Length && first.IsZeroApprox(); i++) first = Direction(points[i - 1], points[i]);
        for (var i = points.Length - 1; i > 0 && last.IsZeroApprox(); i--) last = Direction(points[i - 1], points[i]);
        var border = antialiased ? Feather * Mathf.Min(width, 1) : 0;
        // Body, left feather, right feather: preserve strip order at intersections and translucent joins.
        for (var layer = 0; layer < (antialiased ? 3 : 1); layer++)
        {
            _strip.Clear(); var previous = Vector2.Zero;
            for (var i = 0; i < points.Length; i++)
            {
                var direction = i == points.Length - 1 ? previous : Direction(points[i], points[i + 1]);
                if (direction.IsZeroApprox()) direction = previous;
                if (loop && i == 0) previous = last;
                else if (loop && i == points.Length - 1) previous = first;
                var offset = !loop && i == 0 ? first.Orthogonal() : !loop && i == points.Length - 1 ? last.Orthogonal() : Edge(direction, previous);
                var point = points[i]; var color = ColorAt(colors, i); var clear = color with { A = 0 };
                var inner = point + offset * (width * 0.5f * (layer == 2 ? -1 : 1));
                var outer = layer == 0 ? point - offset * (width * 0.5f) : inner + offset * (border * (layer == 2 ? -1 : 1));
                if (i == 0 && antialiased && !loop)
                {
                    var cap = -direction * border;
                    _strip.Add(new(inner + cap, clear)); _strip.Add(new(outer + cap, clear));
                }
                _strip.Add(new(inner, color)); _strip.Add(new(outer, layer == 0 ? color : clear));
                if (i == points.Length - 1 && antialiased && !loop)
                {
                    var cap = previous * border;
                    if (layer == 0) { _strip.Add(new(inner + cap, clear)); _strip.Add(new(outer + cap, clear)); }
                    else { _strip.Add(new(inner, color)); _strip.Add(new(outer + cap, clear)); _strip.Add(new(inner + cap, clear)); }
                }
                previous = direction;
            }
            for (var i = 2; i < _strip.Count; i++)
                Triangle(_triangles, _strip[i - 2], _strip[i - 1], _strip[i]);
        }
    }

    internal void SetRect(Rect2 rect, Color color, bool filled, float width, bool antialiased)
    {
        rect = rect.Abs();
        if (!rect.IsFinite() || !rect.End.IsFinite()) throw new ArgumentException("Rectangle coordinates overflowed.");
        if (!filled && width < rect.Size.X && width < rect.Size.Y)
        {
            Set([rect.Position, new(rect.End.X, rect.Position.Y), rect.End, new(rect.Position.X, rect.End.Y), rect.Position],
                [color], width, antialiased, true);
            return;
        }
        _triangles.Clear(); _thin.Clear();
        if (!filled) rect = rect.Grow(width * 0.5f);
        if (antialiased) rect = rect.Grow(-Feather * 0.25f);
        var a = rect.Position; var b = new Vector2(rect.End.X, rect.Position.Y);
        var c = rect.End; var d = new Vector2(rect.Position.X, rect.End.Y);
        Quad(a, b, c, d, color, color, color, color);
        if (!antialiased) return;
        var size = Mathf.Min(rect.Size.X, rect.Size.Y);
        var border = Feather * (size >= 0 && size < 1 ? size : 1);
        var x = new Vector2(border, 0); var y = new Vector2(0, border); var clear = color with { A = 0 };
        Quad(a, a - y, b - y, b, color, clear, clear, color);
        Quad(d, d + y, c + y, c, color, clear, clear, color);
        Quad(a, a - x, d - x, d, color, clear, clear, color);
        Quad(b, b + x, c + x, c, color, clear, clear, color);
        Quad(a, a - x, a - x - y, a - y, color, clear, clear, clear);
        Quad(d, d - x, d - x + y, d + y, color, clear, clear, clear);
        Quad(b, b + x, b + x - y, b - y, color, clear, clear, clear);
        Quad(c, c + x, c + x + y, c + y, color, clear, clear, clear);

        void Quad(Vector2 p, Vector2 q, Vector2 r, Vector2 t, Color cp, Color cq, Color cr, Color ct)
        {
            Triangle(_triangles, new(p, cp), new(q, cq), new(r, cr));
            Triangle(_triangles, new(p, cp), new(r, cr), new(t, ct));
        }
    }

    internal void SetEllipse(Vector2 center, float major, float minor, Color color, bool antialiased)
    {
        _triangles.Clear(); _thin.Clear();
        if (antialiased) { major = Mathf.Max(0, major - Feather * 0.25f); minor = Mathf.Max(0, minor - Feather * 0.25f); }
        var border = Feather; var diameter = Mathf.Max(major, minor) * 2;
        if (diameter >= 0 && diameter < 1) border *= diameter * 0.5f;
        for (var layer = 0; layer < (antialiased ? 2 : 1); layer++)
        {
            var previous = new Vector2(major, 0); var previousOuter = new Vector2(major + border, 0);
            for (var i = 1; i <= 64; i++)
            {
                var angle = i * (Mathf.Tau / 64); var unit = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var next = unit * new Vector2(major, minor); var nextOuter = unit * new Vector2(major + border, minor + border);
                if (layer == 0) Triangle(_triangles, new(center, color), new(center + previous, color), new(center + next, color));
                else
                {
                    var clear = color with { A = 0 };
                    StripQuad(_triangles, new(center + previous, color), new(center + previousOuter, clear), new(center + next, color), new(center + nextOuter, clear));
                }
                previous = next; previousOuter = nextOuter;
            }
        }
    }

    internal void Append(List<CanvasVertex> output, Transform transform, Color modulation, bool snap)
    {
        foreach (var vertex in CollectionsMarshal.AsSpan(_triangles)) output.Add(TransformVertex(vertex, transform, modulation, snap));
        for (var i = 1; i < _thin.Count; i += _connected ? 1 : 2)
            AppendThinLine(output, TransformVertex(_thin[i - 1], transform, modulation, false), TransformVertex(_thin[i], transform, modulation, false), snap);
    }

    internal static void AppendLine(List<CanvasVertex> output, Vector2 from, Vector2 to, Color color, float width, bool antialiased, Transform transform, bool snap)
    {
        if (from == to || width == 0) return;
        if (antialiased) width = CompensatedWidth(width);
        var direction = Direction(from, to); var normal = direction.Orthogonal();
        var edge = normal * (width < 0 ? 0 : width * 0.5f);
        if (width < 0) AppendThinLine(output, TransformVertex(new(from, color), transform, Colors.White, false), TransformVertex(new(to, color), transform, Colors.White, false), snap);
        else AddQuad(from + edge, from - edge, to + edge, to - edge, color, color, color, color);
        if (!antialiased) return;
        var amount = Feather * (width >= 0 && width < 1 ? width : 1);
        var border = normal * amount; var cap = direction * amount; var clear = color with { A = 0 };
        // Side strips and flat caps remain in local units, including their feather widths.
        AddQuad(from + edge, from + edge + border, to + edge, to + edge + border, color, clear, color, clear);
        AddQuad(from - edge, from - edge - border, to - edge, to - edge - border, color, clear, color, clear);
        AddQuad(from + edge, from + edge - cap, from - edge, from - edge - cap, color, clear, color, clear);
        AddQuad(to + edge, to + edge + cap, to - edge, to - edge + cap, color, clear, color, clear);
        for (var end = 0; end < 2; end++)
            for (var side = -1; side <= 1; side += 2)
            {
                var corner = (end == 0 ? from : to) + edge * side;
                var capOffset = cap * (end == 0 ? -1 : 1); var sideOffset = border * side;
                // Both triangles share the opaque corner to avoid a transparent diagonal seam.
                AddQuad(corner + capOffset, corner, corner + capOffset + sideOffset, corner + sideOffset, clear, color, clear, clear);
            }

        void AddQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ca, Color cb, Color cc, Color cd) =>
            StripQuad(output, TransformVertex(new(a, ca), transform, Colors.White, snap), TransformVertex(new(b, cb), transform, Colors.White, snap),
                TransformVertex(new(c, cc), transform, Colors.White, snap), TransformVertex(new(d, cd), transform, Colors.White, snap));
    }

    internal static void AppendThinLine(List<CanvasVertex> output, CanvasVertex a, CanvasVertex b, bool snap)
    {
        if (a.Position == b.Position) return;
        var first = output.Count;
        var offset = Direction(a.Position, b.Position).Orthogonal() * 0.5f;
        StripQuad(output, a with { Position = a.Position + offset }, a with { Position = a.Position - offset },
            b with { Position = b.Position + offset }, b with { Position = b.Position - offset });
        if (snap) for (var i = first; i < output.Count; i++) output[i] = output[i] with { Position = CanvasGeometry.Snap(output[i].Position) };
    }

    private static Color ColorAt(ReadOnlySpan<Color> colors, int i) => colors.IsEmpty ? Colors.White : colors[Math.Min(i, colors.Length - 1)];
    private static float CompensatedWidth(float width) => width <= 0 ? width : width <= 2.5f + 0.00001f ? width * 0.5f :
        width <= 5 + 0.00001f ? Mathf.Lerp(width * 0.5f, width - 0.625f, (width - 2.5f) / 2.5f) : width - 0.625f;
    private static Vector2 Direction(Vector2 from, Vector2 to)
    {
        var x = (double)to.X - from.X; var y = (double)to.Y - from.Y; var length = Math.Sqrt(x * x + y * y);
        return length == 0 ? Vector2.Zero : new((float)(x / length), (float)(y / length));
    }
    private static Vector2 Edge(Vector2 direction, Vector2 previous)
    {
        var bisector = (previous * direction.Length() - direction * previous.Length()).Normalized();
        var sine = Mathf.Sin(Mathf.Atan2(bisector.Cross(previous), bisector.Dot(previous)));
        var length = 1f;
        if (!Mathf.IsZeroApprox(sine) && !direction.IsEqualApprox(previous)) length = Math.Clamp(1 / sine, -3, 3);
        else bisector = direction.Orthogonal();
        if (bisector.IsZeroApprox()) bisector = direction.Orthogonal();
        return bisector * length;
    }
    private static CanvasVertex TransformVertex(CanvasVertex vertex, Transform transform, Color modulation, bool snap)
    {
        var position = transform * vertex.Position; var color = vertex.Color * modulation;
        if (!position.IsFinite() || !color.IsFinite()) throw new InvalidOperationException("Canvas stroke geometry overflowed.");
        return vertex with { Position = snap ? CanvasGeometry.Snap(position) : position, Color = color };
    }
    private static void StripQuad(List<CanvasVertex> output, CanvasVertex a, CanvasVertex b, CanvasVertex c, CanvasVertex d)
    { Triangle(output, a, b, c); Triangle(output, b, c, d); }
    private static void Triangle(List<CanvasVertex> output, CanvasVertex a, CanvasVertex b, CanvasVertex c)
    {
        if (!a.Position.IsFinite() || !b.Position.IsFinite() || !c.Position.IsFinite()) throw new ArgumentException("Stroke coordinates overflowed.");
        output.Add(a); output.Add(b); output.Add(c);
    }
}
