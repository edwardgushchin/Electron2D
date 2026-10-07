namespace Electron2D;

public sealed partial class RenderingServer
{
    internal Image ReadbackBackBuffer(Viewport viewport) { EnsureOwner(); return _backend.Readback(_backend.FindTarget(viewport) ?? throw new InvalidOperationException("No viewport target is available."), true); }
    private static bool IsCompositor(CanvasItem item) => item is CanvasGroup || item.ClipChildren != ClipChildrenMode.Disabled && item.HasCanvasCommands;
    private static CanvasItem? ParentGroup(CanvasItem item)
    {
        // ponytail: ancestry lookup is O(depth) per item; cache on reparenting if large-scene profiling warrants it.
        for (var parent = item.RenderParent; parent is not null; parent = parent.RenderParent)
            if (IsCompositor(parent)) return parent;
        return null;
    }
    private void ComposeFrame(Vector2i pixels)
    {
        CanvasItem? active = null; var begin = 0; var first = 0; Rect2? notifierBounds = null;
        foreach (var item in _order)
        {
            var owner = item.Group is { } group && group.RenderZ == item.Z ? group : null;
            if (IsCompositor(item.Node) && owner is not null)
                throw new NotSupportedException("Nested same-Z canvas groups and masks require independent nested backbuffer storage.");
            if (owner is not null && active is null)
            { active = owner; begin = _batches.Count; first = _vertices.Count; notifierBounds = null; _batches.Add(new(0, 0, null, Operation: owner is CanvasGroup ? CanvasOperation.GroupBegin : CanvasOperation.MaskBegin)); }
            if (active is not null && ReferenceEquals(item.Node, active))
            {
                if (active is not CanvasGroup)
                {
                    var maskBatch = _batches.Count;
                    _batches.Add(new(0, 0, null, Operation: CanvasOperation.MaskEnd));
                    AppendOrderedCanvas(item, pixels);
                    var end = _batches.Count;
                    var maskBounds = VertexBounds(first, _vertices.Count, notifierBounds);
                    var region = maskBounds is { } rect ? PixelRegion(rect, pixels, GetClip(active, pixels)) : default;
                    var maskBuiltIn = active.ClipChildren == ClipChildrenMode.AndDraw || active.CanvasMaterial is null;
                    if (active.ClipChildren == ClipChildrenMode.AndDraw)
                    {
                        // Reuse the uploaded owner vertices for its earlier color pass.
                        for (var i = maskBatch + 1; i < end; i++)
                        {
                            var batch = _batches[i];
                            if (batch.Material?.Program.UsesScreenTexture == true) throw new NotSupportedException("A mask's early draw cannot sample its writable screen backbuffer.");
                        }
                        // ponytail: insertion shifts child batches per owner batch; rotate a prepared span if profiling warrants it.
                        for (var i = end - 1; i > maskBatch; i--) _batches.Insert(begin + 1, _batches[end - 1]);
                        var inserted = end - maskBatch - 1; maskBatch += inserted; end += inserted;
                    }
                    for (var i = maskBatch + 1; i < end; i++)
                        _batches[i] = _batches[i] with { Operation = CanvasOperation.MaskEnd, MaskShader = maskBuiltIn, Material = maskBuiltIn ? null : _batches[i].Material, Blend = maskBuiltIn ? BlendMode.Mix : _batches[i].Blend };
                    _batches[maskBatch] = _batches[maskBatch] with { MaskShader = maskBuiltIn };
                    _batches[begin] = new(0, 0, null, Operation: CanvasOperation.MaskBegin, Region: region);
                    _batches.Add(new(0, 0, null, Operation: CanvasOperation.MaskFinish, Region: region, MaskShader: maskBuiltIn));
                    active = null;
                    continue;
                }
                var canvasGroup = (CanvasGroup)active;
                var bounds = VertexBounds(first, _vertices.Count, notifierBounds);
                if (bounds is null) { _batches.RemoveAt(begin); active = null; continue; }
                var fit = bounds.Value == default ? new Rect2(bounds.Value.Position, Vector2.One) : bounds.Value;
                var clip = GetClip(active, pixels);
                _batches.Add(new(0, 0, null, Operation: CanvasOperation.GroupEnd, Mipmaps: canvasGroup.UseMipmaps));
                var ownFirst = _vertices.Count; var ownBatch = _batches.Count;
                if (active.HasCanvasCommands) AppendOrderedCanvas(item, pixels);
                else
                {
                    var material = active.CanvasMaterial;
                    var color = active.RenderInheritedModulate * active.RenderSelfModulate * (active.RenderCanvas()?.Modulate ?? Colors.White);
                    fit = GrowFinite(fit, canvasGroup.FitMargin);
                    var localFit = item.Transform.AffineInverse() * fit;
                    var drawFirst = _vertices.Count; Quad(localFit, color, pixels, item.Transform);
                    _batches.Add(new(drawFirst, 6, material?.GetCanvasState(), Blend: material?.GetCanvasBlendMode() ?? BlendMode.Mix, Clip: clip));
                }
                var builtIn = active.CanvasMaterial is null;
                for (var i = ownBatch; i < _batches.Count; i++)
                    _batches[i] = _batches[i] with { Operation = CanvasOperation.GroupEnd, GroupShader = builtIn };
                if (builtIn) for (var i = ownFirst; i < _vertices.Count; i++)
                    {
                        var v = _vertices[i]; var c = v.Color;
                        _vertices[i] = v with { UV = v.Position / new Vector2(pixels.X, pixels.Y), Color = new(c.R * c.A, c.G * c.A, c.B * c.A, c.A) };
                    }
                var clearBounds = active.HasCanvasCommands ? VertexBounds(ownFirst, _vertices.Count, bounds)!.Value : fit;
                var clear = PixelRegion(GrowFinite(clearBounds, canvasGroup.ClearMargin), pixels, clip);
                var clearFirst = _vertices.Count; Quad(new(clear.Position, clear.Size), default, pixels);
                _batches[begin] = new(clearFirst, 6, null, Clip: clear, Operation: CanvasOperation.GroupBegin, Region: clear);
                active = null;
                continue;
            }
            if (item.Node is CanvasGroup) continue;
            if (active is not null && owner is not null && !ReferenceEquals(owner, active))
                throw new NotSupportedException("Overlapping canvas-group ranges require separate nested backbuffers.");
            if (item.Node is BackBufferCopy copy && copy.CopyMode != BackBufferCopyMode.Disabled && active is null)
            {
                var rect = copy.CopyMode == BackBufferCopyMode.Viewport || copy.Rect == default ? new Rect2(Vector2.Zero, new(pixels.X, pixels.Y)) : item.Transform * copy.Rect;
                _batches.Add(new(0, 0, null, Operation: CanvasOperation.Copy, Region: PixelRegion(rect, pixels, GetClip(copy, pixels))));
            }
            var startBatch = _batches.Count;
            AppendOrderedCanvas(item, pixels);
            if (active is not null)
            {
                for (var i = Math.Max(begin + 1, startBatch - 1); i < _batches.Count; i++)
                    if (_batches[i].Material?.Program.UsesScreenTexture == true)
                        throw new NotSupportedException("A group child cannot sample its active writable screen backbuffer.");
                if (item.Node is VisibleOnScreenNotifier notifier)
                {
                    var rect = item.Transform * notifier.Rect;
                    notifierBounds = notifierBounds is { } current ? current.Merge(rect) : rect;
                }
            }
        }
        if (active is not null) throw new InvalidOperationException("Canvas group boundaries must finish within their ordered canvas range.");
    }
    private Rect2? VertexBounds(int first, int end, Rect2? initial)
    {
        for (var i = first; i < end; i++)
        { var p = _vertices[i].Position; initial = initial is { } rect ? rect.Expand(p) : new Rect2(p, Vector2.Zero); }
        return initial;
    }
    private static Rect2 GrowFinite(Rect2 rect, float margin)
    {
        rect = rect.Grow(margin);
        if (!rect.IsFinite()) throw new InvalidOperationException("Canvas group margins overflowed finite bounds.");
        return rect;
    }
    private static Rect2i PixelRegion(Rect2 rect, Vector2i size, Rect2i? clip)
    {
        if (!rect.IsFinite()) throw new InvalidOperationException("Canvas copy bounds overflowed finite coordinates.");
        rect = rect.Abs().Intersection(new(Vector2.Zero, new(size.X, size.Y)));
        if (clip is { } c) rect = rect.Intersection(new(c.Position, c.Size));
        if (!rect.HasArea()) return default;
        var lo = rect.Position.Floor(); var hi = rect.End.Ceil();
        return new((int)lo.X, (int)lo.Y, (int)(hi.X - lo.X), (int)(hi.Y - lo.Y));
    }
    private void Quad(Rect2 rect, Color color, Vector2i pixels, Transform? transform = null)
    {
        var a = rect.Position; var b = new Vector2(rect.End.X, a.Y); var c = rect.End; var d = new Vector2(a.X, c.Y); var size = new Vector2(pixels.X, pixels.Y);
        if (transform is { } t) { a = t * a; b = t * b; c = t * c; d = t * d; }
        _vertices.Add(new(a, color, a / size)); _vertices.Add(new(b, color, b / size)); _vertices.Add(new(c, color, c / size));
        _vertices.Add(new(a, color, a / size)); _vertices.Add(new(c, color, c / size)); _vertices.Add(new(d, color, d / size));
    }
}
