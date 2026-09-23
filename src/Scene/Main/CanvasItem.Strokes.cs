namespace Electron2D;

public abstract partial class CanvasItem
{
    private List<CanvasStroke>? _strokes;
    private int _strokeCount;
    private Vector2[] _strokePoints = [];

    /// <summary>Records a joined line strip.</summary>
    /// <param name="points">At least two finite local points.</param>
    /// <param name="color">The finite uniform color.</param>
    /// <param name="width">Finite local width; negative values produce one-pixel lines, zero produces no geometry.</param>
    /// <param name="antialiased">Whether to add local feather geometry. Ignored for negative-width polylines and multilines.</param>
    /// <remarks>Input values are copied. Positive-width joins use a bisector limited to three half-widths; an approximately repeated endpoint closes the strip.</remarks>
    /// <exception cref="ArgumentException">Used coordinates, colors or width are nonfinite, or input counts are invalid.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void DrawPolyline(ReadOnlySpan<Vector2> points, Color color, float width = -1f, bool antialiased = false)
    {
        RecordStroke(points, [color], width, antialiased, connected: true);
    }

    /// <summary>Records a joined line strip.</summary>
    /// <param name="points">At least two finite local points.</param>
    /// <param name="colors">Finite vertex colors; empty uses white, missing entries repeat the last supplied color, extra entries are ignored.</param>
    /// <param name="width">Finite local width; negative values produce one-pixel lines, zero produces no geometry.</param>
    /// <param name="antialiased">Whether to add local feather geometry. Ignored for negative-width polylines and multilines.</param>
    /// <remarks>Input values are copied. Positive-width joins use a bisector limited to three half-widths; an approximately repeated endpoint closes the strip.</remarks>
    /// <exception cref="ArgumentException">Used coordinates, colors or width are nonfinite, or input counts are invalid.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void DrawPolylineColors(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width = -1f, bool antialiased = false)
    {
        RecordStroke(points, colors, width, antialiased, connected: true);
    }

    /// <summary>Records independent line segments.</summary>
    /// <param name="points">A nonempty even-length sequence of finite endpoint pairs.</param>
    /// <param name="color">The finite uniform color.</param>
    /// <param name="width">Finite local width; negative values produce one-pixel lines, zero produces no geometry.</param>
    /// <param name="antialiased">Whether to add local feather geometry. Ignored for negative-width polylines and multilines.</param>
    /// <remarks>Each pair is independent and has flat caps. Colors belong to segments, not endpoints.</remarks>
    /// <exception cref="ArgumentException">Used coordinates, colors or width are nonfinite, or input counts are invalid.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void DrawMultiline(ReadOnlySpan<Vector2> points, Color color, float width = -1f, bool antialiased = false)
    {
        RecordStroke(points, [color], width, antialiased, connected: false);
    }

    /// <summary>Records independent line segments.</summary>
    /// <param name="points">A nonempty even-length sequence of finite endpoint pairs.</param>
    /// <param name="colors">One finite color for every segment, or one uniform color.</param>
    /// <param name="width">Finite local width; negative values produce one-pixel lines, zero produces no geometry.</param>
    /// <param name="antialiased">Whether to add local feather geometry. Ignored for negative-width polylines and multilines.</param>
    /// <remarks>Each pair is independent and has flat caps. Colors belong to segments, not endpoints.</remarks>
    /// <exception cref="ArgumentException">Used coordinates, colors or width are nonfinite, or input counts are invalid.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void DrawMultilineColors(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width = -1f, bool antialiased = false)
    {
        RecordStroke(points, colors, width, antialiased, connected: false);
    }

    /// <summary>Records evenly spaced dash segments along a local line.</summary>
    /// <param name="from">Finite local start.</param>
    /// <param name="to">Finite local end.</param>
    /// <param name="color">Finite uniform color.</param>
    /// <param name="width">Finite local width; negative values produce one-pixel lines, zero produces no geometry.</param>
    /// <param name="dash">Positive finite dash length in local units; defaults to two.</param>
    /// <param name="aligned">Whether to fit partial end dashes symmetrically to both endpoints.</param>
    /// <param name="antialiased">Whether to add local feather geometry. Ignored for negative-width polylines and multilines.</param>
    /// <exception cref="ArgumentOutOfRangeException">Dash length is nonpositive/nonfinite or requires more than the supported array capacity.</exception>
    /// <remarks>An unaligned line may leave an undrawn tail. A line shorter than dash uses DrawLine, including its antialias policy.</remarks>
    /// <exception cref="ArgumentException">Used coordinates, colors or width are nonfinite, or input counts are invalid.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void DrawDashedLine(Vector2 from, Vector2 to, Color color, float width = -1f, float dash = 2f, bool aligned = true, bool antialiased = false)
    {
        ValidateStroke([from, to], [color], width, false);
        if (!float.IsFinite(dash) || dash <= 0) throw new ArgumentOutOfRangeException(nameof(dash));
        var dx = (double)to.X - from.X; var dy = (double)to.Y - from.Y; var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < dash || length == 0) { DrawLine(from, to, color, width, antialiased); return; }
        var rawSteps = aligned ? Math.Ceiling(length / dash) : Math.Floor(length / dash);
        if (rawSteps > int.MaxValue - 1) throw new ArgumentOutOfRangeException(nameof(dash), "The dash count exceeds array capacity.");
        var steps = (int)rawSteps; if ((steps & 1) == 0) steps--;
        var points = StrokePoints(steps + 1);
        var direction = new Vector2((float)(dx / length), (float)(dy / length)); var step = direction * dash;
        var offset = aligned ? from + direction * (float)((length - steps * (double)dash) / 2) : from;
        for (var i = 0; i < steps; i += 2)
        {
            points[i] = i == 0 ? from : offset;
            points[i + 1] = aligned && i == steps - 1 ? to : offset + step;
            offset += step * 2;
        }
        RecordStroke(points, [color], width, antialiased, false);
    }

    /// <summary>Records a sampled circular arc.</summary>
    /// <param name="center">Finite local center.</param>
    /// <param name="radius">Finite signed radius.</param>
    /// <param name="startAngle">Finite start angle in radians.</param>
    /// <param name="endAngle">Finite end angle in radians; the sweep is clamped to plus or minus one full turn.</param>
    /// <param name="pointCount">At least two samples, including both endpoints.</param>
    /// <param name="color">Finite uniform color.</param>
    /// <param name="width">Finite local width; negative values produce one-pixel lines, zero produces no geometry.</param>
    /// <param name="antialiased">Whether to add local feather geometry. Ignored for negative-width polylines and multilines.</param>
    /// <exception cref="ArgumentOutOfRangeException">Point count is less than two.</exception>
    /// <remarks>Uses the same joined strip contract as DrawPolyline; clockwise and counterclockwise sweeps are supported.</remarks>
    /// <exception cref="ArgumentException">Used coordinates, colors or width are nonfinite, or input counts are invalid.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void DrawArc(Vector2 center, float radius, float startAngle, float endAngle, int pointCount, Color color, float width = -1f, bool antialiased = false)
    {
        DrawEllipseArc(center, radius, radius, startAngle, endAngle, pointCount, color, width, antialiased);
    }

    /// <summary>Records a sampled elliptical arc.</summary>
    /// <param name="center">Finite local center.</param>
    /// <param name="major">Finite signed horizontal radius.</param>
    /// <param name="minor">Finite signed vertical radius.</param>
    /// <param name="startAngle">Finite start angle in radians.</param>
    /// <param name="endAngle">Finite end angle in radians; the sweep is clamped to plus or minus one full turn.</param>
    /// <param name="pointCount">At least two samples, including both endpoints.</param>
    /// <param name="color">Finite uniform color.</param>
    /// <param name="width">Finite local width; negative values produce one-pixel lines, zero produces no geometry.</param>
    /// <param name="antialiased">Whether to add local feather geometry. Ignored for negative-width polylines and multilines.</param>
    /// <exception cref="ArgumentOutOfRangeException">Point count is less than two.</exception>
    /// <remarks>Sample positions are retained until redraw. Width, joins and feathering follow DrawPolyline.</remarks>
    /// <exception cref="ArgumentException">Used coordinates, colors or width are nonfinite, or input counts are invalid.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void DrawEllipseArc(Vector2 center, float major, float minor, float startAngle, float endAngle, int pointCount, Color color, float width = -1f, bool antialiased = false)
    {
        EnsureDrawing();
        if (!center.IsFinite() || !float.IsFinite(major) || !float.IsFinite(minor) || !float.IsFinite(startAngle) || !float.IsFinite(endAngle))
            throw new ArgumentException("Arc coordinates must be finite.");
        if (pointCount < 2) throw new ArgumentOutOfRangeException(nameof(pointCount));
        ValidateStroke([center, center], [color], width, true);
        var points = StrokePoints(pointCount); var sweep = (float)Math.Clamp((double)endAngle - startAngle, -Mathf.Tau, Mathf.Tau);
        for (var i = 0; i < pointCount; i++)
        {
            var angle = i / (pointCount - 1f) * sweep + startAngle;
            points[i] = center + new Vector2(major * MathF.Cos(angle), minor * MathF.Sin(angle));
        }
        RecordStroke(points, [color], width, antialiased, true);
    }

    /// <summary>Records a filled circle or circular outline.</summary>
    /// <param name="position">Finite local center.</param>
    /// <param name="radius">Finite signed radius.</param>
    /// <param name="color">Finite uniform color.</param>
    /// <param name="filled">Whether to fill the circle; true by default.</param>
    /// <param name="width">Finite local outline width; ignored when filled. Negative values produce one-pixel lines, zero produces no outline geometry.</param>
    /// <param name="antialiased">Whether to add local feather geometry. Ignored for negative-width outlines.</param>
    /// <remarks>Uses 64 segments. Filled geometry ignores width. An outline wider than its diameter becomes an expanded filled circle.</remarks>
    /// <exception cref="ArgumentException">Used coordinates, colors or width are nonfinite, or input counts are invalid.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void DrawCircle(Vector2 position, float radius, Color color, bool filled = true, float width = -1f, bool antialiased = false)
    {
        DrawEllipse(position, radius, radius, color, filled, width, antialiased);
    }

    /// <summary>Records a filled ellipse or elliptical outline.</summary>
    /// <param name="position">Finite local center.</param>
    /// <param name="major">Finite signed horizontal radius.</param>
    /// <param name="minor">Finite signed vertical radius.</param>
    /// <param name="color">Finite uniform color.</param>
    /// <param name="filled">Whether to fill the ellipse; true by default.</param>
    /// <param name="width">Finite local outline width; ignored when filled. Negative values produce one-pixel lines, zero produces no outline geometry.</param>
    /// <param name="antialiased">Whether to add local feather geometry. Ignored for negative-width outlines.</param>
    /// <remarks>Uses 64 segments. Filled geometry ignores width. An outline at least as wide as the larger diameter becomes an expanded filled ellipse. Feather widths are local and scale with the item.</remarks>
    /// <exception cref="ArgumentException">Used coordinates, colors or width are nonfinite, or input counts are invalid.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void DrawEllipse(Vector2 position, float major, float minor, Color color, bool filled = true, float width = -1f, bool antialiased = false)
    {
        ValidateStroke([position, position], [color], width, true);
        if (!float.IsFinite(major) || !float.IsFinite(minor)) throw new ArgumentException("Ellipse radii must be finite.");
        if (filled || width >= 2 * (double)MathF.Max(major, minor))
        {
            if (!filled) { major += width * 0.5f; minor += width * 0.5f; }
            var stroke = NextStroke(); stroke.SetEllipse(position, major, minor, color, antialiased); CommitStroke(stroke);
        }
        else
        {
            Span<Vector2> points = stackalloc Vector2[65];
            for (var i = 0; i < 64; i++) { var angle = i * (Mathf.Tau / 64); points[i] = position + new Vector2(major * MathF.Cos(angle), minor * MathF.Sin(angle)); }
            points[64] = points[0]; RecordStroke(points, [color], width, antialiased, true);
        }
    }

    private void RecordStroke(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width, bool antialiased, bool connected)
    {
        ValidateStroke(points, colors, width, connected);
        var stroke = NextStroke(); stroke.Set(points, colors, width, antialiased, connected); CommitStroke(stroke);
    }

    private void ValidateStroke(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width, bool connected)
    {
        EnsureDrawing();
        if (points.Length < 2 || !connected && (points.Length % 2 != 0 || colors.Length != 1 && colors.Length != points.Length / 2))
            throw new ArgumentException("Invalid stroke point or color count.");
        if (!float.IsFinite(width)) throw new ArgumentException("Stroke width must be finite.", nameof(width));
        foreach (var point in points) if (!point.IsFinite()) throw new ArgumentException("Stroke points must be finite.", nameof(points));
        foreach (var color in colors[..Math.Min(colors.Length, points.Length)]) ValidateCanvasColor(color);
    }

    private Span<Vector2> StrokePoints(int count)
    {
        if (_strokePoints.Length < count) Array.Resize(ref _strokePoints, count);
        return _strokePoints.AsSpan(0, count);
    }

    private CanvasStroke NextStroke()
    {
        _strokes ??= [];
        if (_strokeCount == _strokes.Count) _strokes.Add(new());
        return _strokes[_strokeCount];
    }

    private void CommitStroke(CanvasStroke stroke)
    {
        (_canvasCommands ??= []).Add(new CanvasCommand(false, default, default, Colors.White, true, 0, false, _drawTransform, Stroke: stroke));
        _strokeCount++;
    }
}
