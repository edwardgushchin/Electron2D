namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<RID> _ownedCanvasRIDs = [];
    private readonly List<RenderingCanvasRuntime> _serverSceneCanvases = [];
    private readonly List<CanvasItem> _ownedCanvasItems = [];
    private readonly List<WeakReference<CanvasItem>> _serverSceneItems = [];
    private readonly HashSet<CanvasItem> _capturedCanvasItems = [];
    private readonly List<(CanvasItem Item, int Depth)> _canvasDepthScratch = [];
    internal Viewport CurrentCanvasViewport => _viewport;
    internal SceneTree? CanvasSceneTree => _window.Tree;
    private void CheckCanvasItemOwner(CanvasItem item)
    {
        if (item.Tree is not null && !ReferenceEquals(item.Tree, _window.Tree)) throw new InvalidOperationException("Canvas item belongs to another scene owner.");
        if (item.ServerState is { } state && !ReferenceEquals(state.Owner, this)) throw new InvalidOperationException("Canvas state belongs to another rendering session.");
    }
    private RenderingCanvasItemState CanvasState(CanvasItem item)
    {
        CheckCanvasItemOwner(item);
        if (item.ServerState is { } existing) return existing;
        _serverSceneItems.Add(new(item)); return item.ServerState = new(this, false);
    }
    private RenderingCanvasItemState Change(RID rid, out CanvasItem item) { EnsureTextureChange(); item = RenderingCanvasItemRegistry.Resolve(rid); return CanvasState(item); }
    private RenderingCanvasRuntime CanvasChange(RID rid) { EnsureTextureChange(); var canvas = RenderingCanvasRegistry.Resolve(rid); if (canvas.Owner is not null && !ReferenceEquals(canvas.Owner, this) || canvas.Layer is not null && canvas.Layer.TryGetTarget(out var layer) && layer.Tree is not null && !ReferenceEquals(layer.Tree, _window.Tree)) throw new InvalidOperationException("Canvas belongs to another renderer or scene owner."); if (canvas.World is not null && canvas.World.TryGetTarget(out var world) && world.SceneOwner is { } worldOwner && !ReferenceEquals(worldOwner, _window.Tree)) throw new InvalidOperationException("World canvas belongs to another scene owner."); if (canvas.Owner is null) { if (canvas.SessionOwner is not null && !ReferenceEquals(canvas.SessionOwner, this)) throw new InvalidOperationException("Canvas overrides belong to another session."); if (canvas.SessionOwner is null) { canvas.SessionOwner = this; _serverSceneCanvases.Add(canvas); } } return canvas; }
    private RenderingCanvasRuntime.Attachment ViewChange(RID viewport, RID canvas, out RenderingCanvasRuntime data)
    {
        data = CanvasChange(canvas); var view = Viewport.ResolveViewportRID(viewport); if (!view.IsInsideTree || !ReferenceEquals(view.Tree, _window.Tree)) throw new InvalidOperationException("Viewport must be active in this rendering tree.");
        if (!data.Attachments.TryGetValue(viewport, out var attachment)) { attachment = new() { Attached = data.View(view, out _, out _, out _) }; data.Attachments.Add(viewport, attachment); }
        return attachment;
    }
    internal RID CanvasCreateCore() { EnsureTextureChange(); var value = RenderingCanvasRegistry.Register(owner: this); _ownedCanvasRIDs.Add(value.RID); return value.RID; }
    internal RID CanvasItemCreateCore() { EnsureTextureChange(); var item = new RenderingServerCanvasItem { ServerState = new(this, true) }; item.ServerState.ParentAssigned = true; var rid = RenderingCanvasItemRegistry.RegisterOwned(item, this); _ownedCanvasItems.Add(item); return rid; }
    internal void CanvasItemSetParentCore(RID rid, RID parent)
    {
        var state = Change(rid, out var item); CanvasItem? target = null;
        if (parent.IsValid()) { if (RenderingCanvasRegistry.ResolveOrNull(parent) is not null) CanvasChange(parent); else target = RenderingCanvasItemRegistry.Resolve(parent); }
        var depth = 0; for (var current = target; current is not null; current = current.RenderParent) { if (ReferenceEquals(current, item)) throw new ArgumentException("Canvas parenting cannot form a cycle.", nameof(parent)); if (++depth > 1024) throw new InvalidOperationException("Canvas parent depth exceeds 1024."); }
        CheckCanvasDepth(item, depth);
        if (target is not null) CanvasState(target).AddChild(item);
        state.Parent = parent; state.ParentAssigned = true;
    }
    private void CheckCanvasDepth(CanvasItem item, int parentDepth)
    {
        _canvasDepthScratch.Clear(); _canvasDepthScratch.Add((item, parentDepth));
        try
        {
            while (_canvasDepthScratch.Count != 0)
            {
                var entry = _canvasDepthScratch[^1]; _canvasDepthScratch.RemoveAt(_canvasDepthScratch.Count - 1);
                if (entry.Depth > 1024) throw new InvalidOperationException("Canvas parent depth exceeds 1024.");
                for (var i = 0; i < entry.Item.GetChildCount(includeInternal: true); i++)
                    if (entry.Item.GetChild(i, includeInternal: true) is CanvasItem child && ReferenceEquals(child.RenderParent, entry.Item)) _canvasDepthScratch.Add((child, entry.Depth + 1));
                if (entry.Item.ServerState is { } state) foreach (var weak in state.Children)
                        if (weak.TryGetTarget(out var child) && !child.IsDisposed && ReferenceEquals(child.RenderParent, entry.Item) && !ReferenceEquals(child.GetParentItem(), entry.Item)) _canvasDepthScratch.Add((child, entry.Depth + 1));
            }
        }
        finally { _canvasDepthScratch.Clear(); }
    }
    internal void CanvasItemSetTransformCore(RID item, Transform transform) { if (!transform.IsFinite()) throw new ArgumentException("Canvas transforms must be finite.", nameof(transform)); Change(item, out _).Transform = transform; }
    internal void CanvasItemSetVisibleCore(RID item, bool visible) => Change(item, out _).Visible = visible;
    internal void CanvasItemSetModulateCore(RID item, Color color) { if (!color.IsFinite()) throw new ArgumentException("Canvas colors must be finite.", nameof(color)); Change(item, out _).Modulate = color; }
    internal void CanvasItemSetSelfModulateCore(RID item, Color color) { if (!color.IsFinite()) throw new ArgumentException("Canvas colors must be finite.", nameof(color)); Change(item, out _).SelfModulate = color; }
    internal void CanvasItemSetDrawBehindParentCore(RID item, bool enabled) => Change(item, out _).Behind = enabled;
    internal void CanvasItemSetSortChildrenByYCore(RID item, bool enabled) => Change(item, out _).YSort = enabled;
    internal void CanvasItemSetZIndexCore(RID item, int z) { if (z < CanvasItem.MinimumZIndex || z > CanvasItem.MaximumZIndex) throw new ArgumentOutOfRangeException(nameof(z)); Change(item, out _).Z = z; }
    internal void CanvasItemSetZAsRelativeToParentCore(RID item, bool enabled) => Change(item, out _).ZRelative = enabled;
    internal void CanvasItemSetClipCore(RID item, bool clip) => Change(item, out _).Clip = clip;
    internal void CanvasItemSetCustomRectCore(RID item, bool use, Rect2 rect) { if (!rect.IsFinite() || !rect.End.IsFinite()) throw new ArgumentException("Custom canvas bounds must be finite.", nameof(rect)); Change(item, out _).CustomRect = use ? rect : null; }
    internal Rect2 DebugCanvasItemGetRectCore(RID rid) { EnsureOwner(); var item = RenderingCanvasItemRegistry.Resolve(rid); CheckCanvasItemOwner(item); return item.RenderBounds(); }
    internal void CanvasItemSetDefaultTextureFilterCore(RID item, TextureFilter filter) { if ((uint)filter >= (uint)TextureFilter.Max) throw new ArgumentOutOfRangeException(nameof(filter)); Change(item, out _).Filter = filter; }
    internal void CanvasItemSetDefaultTextureRepeatCore(RID item, TextureRepeat repeat) { if ((uint)repeat >= (uint)TextureRepeat.Max) throw new ArgumentOutOfRangeException(nameof(repeat)); Change(item, out _).Repeat = repeat; }
    internal void CanvasItemClearCore(RID rid) { var state = Change(rid, out var item); item.ServerCommandTarget(state).ClearServerCommands(); }
    internal void CanvasItemAddMeshCore(RID rid, RID mesh, Transform transform, Color modulate, RID texture)
    {
        var state = Change(rid, out var item); var geometry = RenderingMeshRegistry.Resolve(mesh); var image = texture.IsValid() ? RenderingTextureRegistry.Resolve(texture) : null;
        if (!transform.IsFinite() || !modulate.IsFinite()) throw new ArgumentException("Mesh command values must be finite."); item.ServerCommandTarget(state).AppendServerMesh(geometry, image, transform, modulate);
    }
    internal void CanvasItemAddMultiMeshCore(RID rid, RID mesh, RID texture) { var state = Change(rid, out var item); var geometry = RenderingMultiMeshRegistry.Resolve(mesh); var image = texture.IsValid() ? RenderingTextureRegistry.Resolve(texture) : null; item.ServerCommandTarget(state).AppendServerMultiMesh(geometry, image); }
    internal void CanvasItemAddNinePatchCore(RID rid, Rect2 rect, Rect2 source, RID texture, Vector2 begin, Vector2 end, AxisStretchMode horizontal, AxisStretchMode vertical, bool center, Color color)
    {
        if (!rect.IsFinite() || !rect.End.IsFinite() || !source.IsFinite() || !source.End.IsFinite() || !begin.IsFinite() || !end.IsFinite() || !color.IsFinite()) throw new ArgumentException("Nine-patch geometry, margins and colors must be finite.");
        if ((uint)horizontal > (uint)AxisStretchMode.TileFit || (uint)vertical > (uint)AxisStretchMode.TileFit) throw new ArgumentOutOfRangeException(nameof(horizontal));
        var state = Change(rid, out var item); var image = texture.IsValid() ? RenderingTextureRegistry.Resolve(texture) : RenderingTextureRegistry.WhiteTexture;
        item.ServerCommandTarget(state).AppendServerNinePatch(image, rect, source, new(begin, end, horizontal, vertical, center), color);
    }
    internal void CanvasSetModulateCore(RID rid, Color color) { if (!color.IsFinite()) throw new ArgumentException("Canvas colors must be finite.", nameof(color)); CanvasChange(rid).Modulate = color; }
    internal void CanvasSetItemMirroringCore(RID rid, RID item, Vector2 mirroring)
    { if (!mirroring.IsFinite()) throw new ArgumentException("Canvas mirroring must be finite.", nameof(mirroring)); var canvas = CanvasChange(rid); var target = RenderingCanvasItemRegistry.Resolve(item); if (target.RenderParent is not null || !ReferenceEquals(target.RenderCanvas(), canvas)) throw new ArgumentException("Mirroring requires a direct canvas child.", nameof(item)); canvas.Mirroring[item] = mirroring; }
    internal void ViewportAttachCanvasCore(RID viewport, RID canvas) => ViewChange(viewport, canvas, out _).Attached = true;
    internal void ViewportRemoveCanvasCore(RID viewport, RID canvas) => ViewChange(viewport, canvas, out _).Attached = false;
    internal void ViewportSetCanvasTransformCore(RID viewport, RID canvas, Transform transform) { if (!transform.IsFinite()) throw new ArgumentException("Canvas view transforms must be finite.", nameof(transform)); ViewChange(viewport, canvas, out _).Transform = transform; }
    internal void ViewportSetCanvasStackingCore(RID viewport, RID canvas, int layer, int sublayer) { var view = ViewChange(viewport, canvas, out _); view.Layer = layer; view.Sublayer = sublayer; }
    private bool ReleaseCanvasRID(RID rid)
    {
        if (RenderingCanvasRegistry.ResolveOrNull(rid) is { } canvas)
        {
            if (!ReferenceEquals(canvas.Owner, this)) throw new InvalidOperationException("Scene canvases cannot be freed by the renderer.");
            _ownedCanvasRIDs.Remove(rid); RenderingCanvasRegistry.Remove(rid); return true;
        }
        if (!RenderingCanvasItemRegistry.Contains(rid)) return false;
        if (!RenderingCanvasItemRegistry.IsOwned(rid, this)) throw new InvalidOperationException("Scene canvas items cannot be freed by the renderer.");
        var item = RenderingCanvasItemRegistry.Resolve(rid); _ownedCanvasItems.Remove(item); foreach (var canvasID in _ownedCanvasRIDs) RenderingCanvasRegistry.Resolve(canvasID).Mirroring.Remove(rid); foreach (var sceneCanvas in _serverSceneCanvases) sceneCanvas.Mirroring.Remove(rid); item.Dispose(); return true;
    }
    private void ReleaseOwnedCanvasGraph()
    {
        foreach (var item in _ownedCanvasItems) { RenderingCanvasItemRegistry.Remove(item.GetCanvasItem()); item.Dispose(); }
        _ownedCanvasItems.Clear();
        foreach (var rid in _ownedCanvasRIDs) RenderingCanvasRegistry.Remove(rid); _ownedCanvasRIDs.Clear();
        foreach (var canvas in _serverSceneCanvases) { canvas.Modulate = Colors.White; canvas.Attachments.Clear(); canvas.Mirroring.Clear(); canvas.SessionOwner = null; }
        _serverSceneCanvases.Clear();
        foreach (var weak in _serverSceneItems) if (weak.TryGetTarget(out var item) && !item.IsDisposed && ReferenceEquals(item.ServerState?.Owner, this)) { item.ServerState?.Commands?.Dispose(); item.ServerState = null; }
        _serverSceneItems.Clear(); _capturedCanvasItems.Clear();
    }
    private void CaptureCanvasScene(Node root)
    {
        _capturedCanvasItems.Clear(); Capture(root);
        foreach (var item in _ownedCanvasItems) if (!item.IsDisposed && _capturedCanvasItems.Add(item)) _nodes.Add(item);
        foreach (var weak in _serverSceneItems) if (weak.TryGetTarget(out var item) && !item.IsDisposed && !ReferenceEquals(item.Tree, root.Tree) && _capturedCanvasItems.Add(item) && item.ServerState is { ParentAssigned: true }) _nodes.Add(item);
    }
}
