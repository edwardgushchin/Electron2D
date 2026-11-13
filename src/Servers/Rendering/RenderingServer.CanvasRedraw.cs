namespace Electron2D;

public sealed partial class RenderingServer
{
    private double _debugRedrawDuration, _debugRedrawClock;
    private ulong _debugRedrawEpoch;
    private Color _debugRedrawColor;
    private List<(Rect2 Bounds, Transform Transform, Rect2i? Clip, Color Color)>? _debugRedrawHighlights;

    private void ConfigureCanvasRedraw()
    {
        foreach (var argument in Environment.GetCommandLineArgs().AsSpan(1))
        {
            if (argument is "--" or "++") break;
            if (argument != "--debug-canvas-item-redraw") continue;
            _debugRedrawColor = ProjectSettings.GetWithOverride(ProjectSettings.DebugRedrawColor);
            if (_debugRedrawColor.A > 0f)
                _debugRedrawDuration = ProjectSettings.GetWithOverride(ProjectSettings.DebugRedrawTime);
            break;
        }
    }

    private void AdvanceCanvasRedraw(double step)
    {
        if (step >= _debugRedrawDuration || !double.IsFinite(_debugRedrawClock + step))
        {
            _debugRedrawClock = 0d;
            _debugRedrawEpoch++;
        }
        else _debugRedrawClock += step;
    }

    internal void MarkCanvasRedraw(CanvasItem item)
    {
        if (_debugRedrawDuration == 0d) return;
        item.DebugRedrawRenderer = InstanceID;
        item.DebugRedrawTime = double.NaN;
    }

    private void CaptureCanvasRedraw(CanvasItem item, Transform transform, Rect2i? clip, Vector2i pixels)
    {
        if (item.DebugRedrawRenderer != InstanceID || ClearMode(_viewport) != ViewportClearMode.Always) return;
        var pending = double.IsNaN(item.DebugRedrawTime);
        var elapsed = pending ? 0d : _debugRedrawClock - item.DebugRedrawTime;
        if (!pending && (item.DebugRedrawEpoch != _debugRedrawEpoch || elapsed >= _debugRedrawDuration)) return;
        var bounds = item.RenderCommandBounds().Abs();
        var transformed = transform * bounds;
        if (!bounds.HasArea() || !transformed.IsFinite() || !transformed.End.IsFinite() ||
            !PixelRegion(transformed, pixels, clip).HasArea()) return;
        if (pending)
        {
            item.DebugRedrawEpoch = _debugRedrawEpoch;
            item.DebugRedrawTime = _debugRedrawClock;
        }
        var color = _debugRedrawColor with { A = _debugRedrawColor.A * (float)(1d - elapsed / _debugRedrawDuration) };
        (_debugRedrawHighlights ??= []).Add((bounds, transform, clip, color));
        _continuousCanvas = true;
    }

    private void AppendCanvasRedrawHighlights(Vector2i pixels)
    {
        if (_debugRedrawHighlights is not { } highlights) return;
        foreach (var highlight in highlights)
        {
            var first = _vertices.Count;
            Quad(highlight.Bounds, highlight.Color, pixels, highlight.Transform);
            _batches.Add(new(first, 6, null, Clip: highlight.Clip));
        }
        highlights.Clear();
    }
}
