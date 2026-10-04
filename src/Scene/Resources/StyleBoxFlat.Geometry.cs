namespace Electron2D;

public partial class StyleBoxFlat
{
    private void BuildGeometry(Rect2 rect, float recordingFactor = 1)
    {
        _triangles.Clear();
        var aaSize = _antiAliasingSize / recordingFactor;
        var border = _borders[0] > 0 || _borders[1] > 0 || _borders[2] > 0 || _borders[3] > 0;
        var shadow = _shadowSize > 0;
        if (!border && !_drawCenter && !shadow) return;
        var style = rect.GrowIndividual(_expansion[0], _expansion[1], _expansion[2], _expansion[3]);
        if (!style.IsFinite()) throw new InvalidOperationException("Style expansion exceeded finite geometry.");
        if (Mathf.IsZeroApprox(style.Size.X) || Mathf.IsZeroApprox(style.Size.Y)) return;
        var rounded = _radii[0] > 0 || _radii[1] > 0 || _radii[2] > 0 || _radii[3] > 0;
        var aa = (rounded || !_skew.IsZeroApprox()) && _antiAliasing;
        var blend = _borderBlend && border;
        var transparentBorder = _borderColor with { A = 0 };
        var blendedBorder = _drawCenter ? _background : transparentBorder;
        var innerBorder = blend ? blendedBorder : _borderColor;
        var width = MathF.Max(style.Size.X, 0); var height = MathF.Max(style.Size.Y, 0);
        Span<float> borders = stackalloc float[4] { 1_000_000, 1_000_000, 1_000_000, 1_000_000 };
        Span<float> radii = stackalloc float[4] { 1_000_000, 1_000_000, 1_000_000, 1_000_000 };
        ReadOnlySpan<float> requestedBorders = stackalloc float[4] { _borders[0], _borders[1], _borders[2], _borders[3] };
        ReadOnlySpan<float> requestedRadii = stackalloc float[4] { _radii[0], _radii[1], _radii[2], _radii[3] };
        Adapt(1, 3, borders, requestedBorders, height, height, height);
        Adapt(0, 2, borders, requestedBorders, width, width, width);
        Adapt(1, 2, radii, requestedRadii, height, height - borders[3], height - borders[1]);
        Adapt(0, 3, radii, requestedRadii, height, height - borders[3], height - borders[1]);
        Adapt(0, 1, radii, requestedRadii, width, width - borders[2], width - borders[0]);
        Adapt(3, 2, radii, requestedRadii, width, width - borders[2], width - borders[0]);
        var infill = style.GrowIndividual(-borders[0], -borders[1], -borders[2], -borders[3]);
        var borderStyle = style;
        if (aa)
            for (var side = 0; side < 4; side++)
                if (_borders[side] > 0) borderStyle = borderStyle.GrowSide((Side)side, -aaSize);
        if (shadow)
        {
            var shadowInner = new Rect2(style.Position + _shadowOffset, style.Size);
            var shadowOuter = new Rect2(style.Grow(_shadowSize).Position + _shadowOffset, style.Grow(_shadowSize).Size);
            Rounded(shadowInner, radii, shadowOuter, shadowInner, _shadowColor, _shadowColor with { A = 0 });
            if (_drawCenter) Rounded(shadowInner, radii, shadowInner, shadowInner, _shadowColor, _shadowColor, true);
        }
        if (border && !aa) Rounded(borderStyle, radii, borderStyle, infill, innerBorder, _borderColor);
        if (_drawCenter && (!aa || blend)) Rounded(borderStyle, radii, infill, infill, _background, _background, true);
        if (aa)
        {
            Span<float> aaBorder = stackalloc float[4]; Span<float> aaFill = stackalloc float[4];
            for (var side = 0; side < 4; side++)
            {
                var hasBorder = border && _borders[side] > 0;
                aaBorder[side] = hasBorder ? aaSize : 0;
                aaFill[side] = hasBorder ? 0 : aaSize;
            }
            if (_drawCenter)
            {
                var transparent = Grow(infill, aaFill, .5f);
                var colored = Grow(transparent, aaFill, -1);
                if (!blend) Rounded(borderStyle, radii, colored, colored, _background, _background, true);
                if (!blend || !border) Rounded(borderStyle, radii, transparent, colored, _background, _background with { A = 0 });
            }
            if (border)
            {
                var innerColored = Grow(infill, aaBorder, .5f);
                var innerTransparent = Grow(innerColored, aaBorder, -1);
                var outerTransparent = Grow(style, aaBorder, .5f);
                var outerColored = Grow(borderStyle, aaBorder, .5f);
                Rounded(borderStyle, radii, outerColored, blend ? infill : innerColored, innerBorder, _borderColor);
                if (!blend) Rounded(borderStyle, radii, innerColored, innerTransparent, blendedBorder, _borderColor);
                Rounded(borderStyle, radii, outerTransparent, outerColored, _borderColor, transparentBorder);
            }
        }
        var uvRect = style.Grow(aa ? aaSize : 0);
        for (var index = 0; index < _triangles.Count; index++)
        {
            var vertex = _triangles[index];
            var uv = (vertex.Position - uvRect.Position) / uvRect.Size;
            if (!vertex.Position.IsFinite() || !uv.IsFinite()) throw new InvalidOperationException("Style tessellation exceeded finite geometry.");
            _triangles[index] = vertex with { UV = uv };
        }
    }

    private static Rect2 Grow(Rect2 rect, ReadOnlySpan<float> widths, float factor) =>
        rect.GrowIndividual(widths[0] * factor, widths[1] * factor, widths[2] * factor, widths[3] * factor);

    private static void Adapt(int a, int b, Span<float> adapted, ReadOnlySpan<float> requested, float extent, float maxA, float maxB)
    {
        // Ordered comparisons retain the finite fallback when a zero-size axis produces 0/0.
        static float Min(float first, float second) => first < second ? first : second;
        var factor = Min(1, extent / (requested[a] + requested[b]));
        adapted[a] = Min(Min(requested[a] * factor, maxA), adapted[a]);
        adapted[b] = Min(Min(requested[b] * factor, maxB), adapted[b]);
    }

    private static void InnerRadii(Rect2 style, Rect2 inner, ReadOnlySpan<float> radii, Span<float> result)
    {
        var left = inner.Position.X - style.Position.X; var top = inner.Position.Y - style.Position.Y;
        var right = style.Size.X - inner.Size.X - left; var bottom = style.Size.Y - inner.Size.Y - top;
        result[0] = MathF.Max(radii[0] - MathF.Min(top, left), 0);
        result[1] = MathF.Max(radii[1] - MathF.Min(top, right), 0);
        result[2] = MathF.Max(radii[2] - MathF.Min(bottom, right), 0);
        result[3] = MathF.Max(radii[3] - MathF.Min(bottom, left), 0);
    }

    private static void CornerScales(Rect2 style, Rect2 inner, ReadOnlySpan<float> radii, Span<Vector2> result)
    {
        var left = inner.Position.X - style.Position.X; var top = inner.Position.Y - style.Position.Y;
        var right = style.Size.X - inner.Size.X - left; var bottom = style.Size.Y - inner.Size.Y - top;
        var horizontal = left + right; var vertical = top + bottom;
        var leftRatio = horizontal > 0 ? left / horizontal : 0; var rightRatio = horizontal > 0 ? right / horizontal : 0;
        var topRatio = vertical > 0 ? top / vertical : 0; var bottomRatio = vertical > 0 ? bottom / vertical : 0;
        var overflowLeft = -MathF.Min(0, inner.Size.Y - radii[0] - radii[3]);
        var overflowTop = -MathF.Min(0, inner.Size.X - radii[0] - radii[1]);
        var overflowRight = -MathF.Min(0, inner.Size.Y - radii[1] - radii[2]);
        var overflowBottom = -MathF.Min(0, inner.Size.X - radii[3] - radii[2]);
        Span<Vector2> reduction = stackalloc Vector2[4]
        {
            new(overflowTop * leftRatio, overflowLeft * topRatio), new(overflowTop * rightRatio, overflowRight * topRatio),
            new(overflowBottom * rightRatio, overflowRight * bottomRatio), new(overflowBottom * leftRatio, overflowLeft * bottomRatio)
        };
        Span<Vector2> leftovers = stackalloc Vector2[4]; Span<Vector2> distributed = stackalloc Vector2[4];
        for (var index = 0; index < 4; index++) leftovers[index] = -(new Vector2(radii[index], radii[index]) - reduction[index]).Min(Vector2.Zero);
        for (var index = 0; index < 4; index++)
            distributed[index] = (new Vector2(radii[index], radii[index]) - reduction[index] - leftovers[(index + 3) % 4] - leftovers[(index + 1) % 4]).Max(Vector2.Zero);
        for (var index = 0; index < 4; index++)
        {
            var unshrinkable = (leftovers[(index + 1) % 4] + leftovers[(index + 3) % 4] - distributed[index]).Max(Vector2.Zero);
            result[index] = distributed[index] / (new Vector2(radii[index], radii[index]) - unshrinkable).Max(1.1920929e-7f);
        }
    }

    private static void Centers(Rect2 rect, ReadOnlySpan<float> radii, ReadOnlySpan<Vector2> scale, Span<Vector2> result)
    {
        result[0] = rect.Position + new Vector2(radii[0], radii[0]) * scale[0];
        result[1] = new(rect.Position.X + rect.Size.X - radii[1] * scale[1].X, rect.Position.Y + radii[1] * scale[1].Y);
        result[2] = rect.Position + rect.Size - new Vector2(radii[2], radii[2]) * scale[2];
        result[3] = new(rect.Position.X + radii[3] * scale[3].X, rect.Position.Y + rect.Size.Y - radii[3] * scale[3].Y);
    }

    private void Rounded(Rect2 style, ReadOnlySpan<float> radii, Rect2 ring, Rect2 inner, Color innerColor, Color outerColor, bool filled = false)
    {
        var detailCount = radii[0] > 0 || radii[1] > 0 || radii[2] > 0 || radii[3] > 0 ? _cornerDetail : 1;
        Span<float> ringRadii = stackalloc float[4]; Span<float> innerRadii = stackalloc float[4];
        InnerRadii(style, ring, radii, ringRadii); InnerRadii(style, inner, radii, innerRadii);
        Span<Vector2> ringScale = stackalloc Vector2[4]; Span<Vector2> innerScale = stackalloc Vector2[4];
        CornerScales(style, ring, ringRadii, ringScale); CornerScales(style, inner, innerRadii, innerScale);
        Span<Vector2> ringCenters = stackalloc Vector2[4]; Span<Vector2> innerCenters = stackalloc Vector2[4];
        Centers(ring, ringRadii, ringScale, ringCenters); Centers(inner, innerRadii, innerScale, innerCenters);
        var count = (detailCount + 1) * (filled ? 4 : 8);
        Span<CanvasVertex> vertices = stackalloc CanvasVertex[168];
        var center = style.GetCenter(); var quarterArc = (float)(Math.PI / 2.0);
        for (var corner = 0; corner < 4; corner++)
            for (var step = 0; step <= detailCount; step++)
            {
                var index = ((detailCount + 1) * corner + step) * (filled ? 1 : 2);
                var angle = (float)((corner + step / (double)detailCount) * quarterArc + Math.PI);
                var cosine = MathF.Cos(angle); var sine = MathF.Sin(angle);
                var point = new Vector2(innerRadii[corner] * cosine * innerScale[corner].X + innerCenters[corner].X,
                    innerRadii[corner] * sine * innerScale[corner].Y + innerCenters[corner].Y);
                vertices[index] = new(Shear(point, center), innerColor, Vector2.Zero);
                if (!filled)
                {
                    point = new(ringRadii[corner] * cosine * ringScale[corner].X + ringCenters[corner].X,
                        ringRadii[corner] * sine * ringScale[corner].Y + ringCenters[corner].Y);
                    vertices[index + 1] = new(Shear(point, center), outerColor, Vector2.Zero);
                }
            }
        if (!filled)
            for (var index = 0; index < count; index++)
            {
                _triangles.Add(vertices[index]); _triangles.Add(vertices[(index + 2) % count]); _triangles.Add(vertices[(index + 1) % count]);
            }
        else
            for (var index = 0; index < count / 2 - 1; index++)
            {
                _triangles.Add(vertices[index]); _triangles.Add(vertices[count - index - 2]); _triangles.Add(vertices[index + 1]);
                _triangles.Add(vertices[index]); _triangles.Add(vertices[count - index - 1]); _triangles.Add(vertices[count - index - 2]);
            }
    }

    private Vector2 Shear(Vector2 point, Vector2 center) => new(point.X - _skew.X * (point.Y - center.Y), point.Y - _skew.Y * (point.X - center.X));
}
