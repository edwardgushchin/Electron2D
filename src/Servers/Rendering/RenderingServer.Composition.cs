namespace Electron2D;

public sealed partial class RenderingServer
{
    internal Image ReadbackBackBuffer(Viewport viewport) { EnsureOwner(); return _backend.Readback(_backend.FindTarget(viewport) ?? throw new InvalidOperationException("No viewport target is available."), true); }
    private static bool IsCompositor(CanvasItem item) { var group = GroupSettings(item); return group.Mode != CanvasGroupMode.Disabled && (group.Mode == CanvasGroupMode.Transparent || item.HasCanvasCommands || group.FitEmpty); }
    private static CanvasItem? ParentGroup(CanvasItem item)
    {
        // ponytail: ancestry lookup is O(depth) per item; cache on reparenting if large-scene profiling warrants it.
        for (var parent = item.RenderParent; parent is not null; parent = parent.RenderParent)
            if (IsCompositor(parent)) return parent;
        return null;
    }
    private void ComposeFrame(Vector2i pixels)
    {
        var linear = ViewportHDR(_viewport);
        CanvasItem? active = null; var begin = 0; var first = 0; Rect2? notifierBounds = null;
        foreach (var item in _order)
        {
            var owner = item.Group is { } group && group.RenderZ == item.Z ? group : null;
            if (IsCompositor(item.Node) && owner is not null)
                throw new NotSupportedException("Nested same-Z canvas groups and masks require independent nested backbuffer storage.");
            if (owner is not null && active is null)
            { active = owner; begin = _batches.Count; first = _vertices.Count; notifierBounds = null; _batches.Add(new(0, 0, null, Operation: GroupSettings(owner).Mode == CanvasGroupMode.Transparent ? CanvasOperation.GroupBegin : CanvasOperation.MaskBegin)); }
            if (active is not null && ReferenceEquals(item.Node, active))
            {
                var settings = GroupSettings(active);
                var childBounds = VertexBounds(first, _vertices.Count, notifierBounds);
                if (settings.Mode != CanvasGroupMode.Transparent)
                {
                    var maskBatch = _batches.Count;
                    _batches.Add(new(0, 0, null, Operation: CanvasOperation.MaskEnd));
                    AppendGroupOwner(item, pixels, settings, childBounds);
                    var end = _batches.Count;
                    var maskBounds = VertexBounds(first, _vertices.Count, notifierBounds);
                    var region = maskBounds is { } rect ? PixelRegion(GrowFinite(rect, settings.ClearMargin), pixels, GetClip(active, pixels)) : default;
                    var maskBuiltIn = settings.Mode == CanvasGroupMode.ClipAndDraw || active.CanvasMaterial is null;
                    if (settings.Mode == CanvasGroupMode.ClipAndDraw)
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
                        _batches[i] = _batches[i] with { Operation = CanvasOperation.MaskEnd, MaskShader = maskBuiltIn, Material = maskBuiltIn && !_batches[i].Procedural ? null : _batches[i].Material, Blend = maskBuiltIn ? BlendMode.Mix : _batches[i].Blend, CanvasModulation = maskBuiltIn ? null : _batches[i].CanvasModulation, Lighting = maskBuiltIn ? null : _batches[i].Lighting };
                    _batches[maskBatch] = _batches[maskBatch] with { MaskShader = maskBuiltIn, MIPMaps = settings.BlurMIPMaps };
                    _batches[begin] = new(0, 0, null, Operation: CanvasOperation.MaskBegin, Region: region);
                    _batches.Add(new(0, 0, null, Operation: CanvasOperation.MaskFinish, Region: region, MaskShader: maskBuiltIn));
                    active = null;
                    continue;
                }
                var bounds = childBounds;
                if (bounds is null) { _batches.RemoveAt(begin); active = null; continue; }
                var fit = bounds.Value == default ? new Rect2(bounds.Value.Position, Vector2.One) : bounds.Value;
                var clip = GetClip(active, pixels);
                _batches.Add(new(0, 0, null, Operation: CanvasOperation.GroupEnd, MIPMaps: settings.BlurMIPMaps));
                var ownFirst = _vertices.Count; var ownBatch = _batches.Count;
                AppendGroupOwner(item, pixels, settings, bounds);
                var builtIn = active.CanvasMaterial is null;
                for (var i = ownBatch; i < _batches.Count; i++)
                {
                    var batch = _batches[i];
                    if (builtIn && batch.Glyph.Mode == CanvasGlyphMode.LCD)
                        for (var vertex = batch.First; vertex < batch.First + batch.Count; vertex++)
                        {
                            var value = _vertices[vertex]; var tint = batch.GlyphModulation;
                            _vertices[vertex] = value with { Color = new(tint.R, tint.G, tint.B, value.Color.A) };
                        }
                    if (builtIn && linear && !batch.LinearColors)
                        for (var vertex = batch.First; vertex < batch.First + batch.Count; ++vertex)
                            _vertices[vertex] = _vertices[vertex] with { Color = CanvasGeometry.LinearColor(_vertices[vertex].Color) };
                    _batches[i] = batch with { Operation = CanvasOperation.GroupEnd, GroupShader = builtIn, Glyph = builtIn ? default : batch.Glyph, LinearColors = builtIn && linear || batch.LinearColors };
                }
                if (builtIn) for (var i = ownBatch; i < _batches.Count; i++)
                    {
                        var batch = _batches[i];
                        if (batch.Particles is not null) continue;
                        for (var vertex = batch.First; vertex < batch.First + batch.Count; vertex++)
                        {
                            var value = _vertices[vertex]; var color = value.Color;
                            _vertices[vertex] = value with { UV = batch.Procedural ? value.UV : value.Position / new Vector2(pixels.X, pixels.Y), Color = new(color.R * color.A, color.G * color.A, color.B * color.A, color.A), Glyph = batch.Procedural ? value.Glyph : default };
                        }
                    }
                var clearBounds = active.HasCanvasCommands || settings.FitEmpty ? VertexBounds(ownFirst, _vertices.Count, bounds)!.Value : fit;
                var clear = PixelRegion(GrowFinite(clearBounds, settings.ClearMargin), pixels, clip);
                var clearFirst = _vertices.Count; Quad(new(clear.Position, clear.Size), default, pixels);
                _batches[begin] = new(clearFirst, 6, null, Clip: clear, Operation: CanvasOperation.GroupBegin, Region: clear);
                active = null;
                continue;
            }
            if ((item.Node.ServerState?.Group is not null || item.Node is CanvasGroup) && IsCompositor(item.Node) && GroupSettings(item.Node).Mode != CanvasGroupMode.ClipAndDraw) continue;
            if (active is not null && owner is not null && !ReferenceEquals(owner, active))
                throw new NotSupportedException("Overlapping canvas-group ranges require separate nested backbuffers.");
            if (item.Node.RenderCopyRect is { } copyRect && active is null)
            {
                var rect = copyRect == default ? new Rect2(Vector2.Zero, new(pixels.X, pixels.Y)) : item.CopyTransform * copyRect;
                _batches.Add(new(0, 0, null, Operation: CanvasOperation.Copy, Region: PixelRegion(rect, pixels, GetClip(item.Node, pixels))));
            }
            var startBatch = _batches.Count;
            _appendedInstanceBounds = null;
            AppendOrderedCanvas(item, pixels);
            if (active is not null)
            {
                if (_appendedInstanceBounds is { } instanceBounds)
                    notifierBounds = notifierBounds is { } previous ? previous.Merge(instanceBounds) : instanceBounds;
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
    private void AppendGroupOwner(RenderEntry item, Vector2i pixels, CanvasGroupSettings settings, Rect2? childBounds)
    {
        if (item.Node.HasCanvasCommands) { AppendOrderedCanvas(item, pixels, unshaded: item.Node.CanvasMaterial is null && settings.Mode != CanvasGroupMode.ClipAndDraw); return; }
        if (!settings.FitEmpty || childBounds is null) return;
        var fit = childBounds.Value == default ? new Rect2(childBounds.Value.Position, Vector2.One) : childBounds.Value;
        fit = GrowFinite(fit, settings.FitMargin);
        var material = item.Node.CanvasMaterial;
        var color = item.Node.RenderInheritedModulate * item.Node.RenderSelfModulate;
        var localFit = item.Transform.AffineInverse() * fit;
        var first = _vertices.Count; Quad(localFit, color, pixels, item.Transform);
        var state = material?.RequireCanvasState();
        _batches.Add(new(first, 6, state, Blend: material?.GetCanvasBlendMode() ?? BlendMode.Mix, Clip: GetClip(item.Node, pixels), InstanceOwner: state?.Program.HasInstanceUniforms == true ? item.Node : null, CanvasModulation: material is null or CanvasItemMaterial { LightMode: LightMode.Unshaded } ? null : item.Node.RenderCanvas()?.Modulate ?? Colors.White));
    }
    private Rect2? VertexBounds(int first, int end, Rect2? initial)
    {
        foreach (var batch in _batches)
        {
            if (batch.First >= end || batch.First + batch.Count <= first) continue;
            if (batch.VisibilityBounds is { } manual)
            { initial = initial is { } bounds ? bounds.Merge(manual) : manual; continue; }
            // Instance base vertices are local geometry; their transformed bounds were accumulated during publication.
            if (batch.InstanceCount > 0) continue;
            var stop = Math.Min(end, batch.First + batch.Count);
            for (var i = Math.Max(first, batch.First); i < stop; i++)
            {
                var footprint = new Rect2(_vertices[i].Position, Vector2.Zero).Grow(batch.RasterMargin);
                if (batch.RasterMargin > 0) footprint = new(footprint.Position.Floor(), footprint.End.Ceil() - footprint.Position.Floor());
                initial = initial is { } rect ? rect.Merge(footprint) : footprint;
            }
        }
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
