namespace Electron2D;

internal sealed class RenderingCanvasRuntime
{
    internal readonly RID RID;
    internal readonly RenderingServer? Owner;
    internal RenderingServer? SessionOwner;
    internal readonly WeakReference<CanvasLayer>? Layer;
    internal readonly WeakReference<Viewport>? DefaultViewport;
    internal Color Modulate = Colors.White;
    internal readonly Dictionary<RID, Attachment> Attachments = [];
    internal readonly Dictionary<RID, Vector2> Mirroring = [];
    internal sealed class Attachment { internal bool Attached = true; internal Transform? Transform; internal int? Layer, Sublayer; }
    internal RenderingCanvasRuntime(RID rid, RenderingServer? owner = null, CanvasLayer? layer = null, Viewport? viewport = null) { RID = rid; Owner = owner; if (layer is not null) Layer = new(layer); if (viewport is not null) DefaultViewport = new(viewport); }
    internal bool Alive => Owner is not null || Layer is not null && Layer.TryGetTarget(out var layer) && !layer.IsDisposed || DefaultViewport is not null && DefaultViewport.TryGetTarget(out var viewport) && !viewport.IsDisposed;
    internal bool View(Viewport viewport, out Transform transform, out int layer, out int sublayer, float fraction = 1f)
    {
        var rid = viewport.GetViewportRID(); Attachments.TryGetValue(rid, out var attachment); var sceneLayer = Layer is not null && Layer.TryGetTarget(out var l) && !l.IsDisposed ? l : null;
        var primary = sceneLayer is not null ? ReferenceEquals(sceneLayer.CanvasViewport, viewport) : DefaultViewport is not null && DefaultViewport.TryGetTarget(out var v) && ReferenceEquals(v, viewport);
        transform = attachment?.Transform is { } custom ? viewport.GetFinalTransform() * custom : (primary ? viewport.GetCanvasRenderTransform(sceneLayer, fraction) : viewport.GetFinalTransform());
        layer = attachment?.Layer ?? (primary ? sceneLayer?.Layer ?? 0 : 0); sublayer = attachment?.Sublayer ?? (primary ? sceneLayer?.GetIndex(includeInternal: true) ?? 0 : 0);
        return Alive && (attachment?.Attached ?? primary);
    }
    internal void PublishLayer(bool transform) { if (Attachments.Count == 0 || Layer is null || !Layer.TryGetTarget(out var layer) || layer.CanvasViewport is not { } viewport || !Attachments.TryGetValue(viewport.GetViewportRID(), out var view)) return; if (transform) view.Transform = null; else { view.Layer = null; view.Sublayer = null; } }
}

internal static class RenderingCanvasRegistry
{
    // ponytail: cold weak scene and retained owned identities share one gate; partition after measured contention.
    private static readonly object Gate = new();
    private static readonly Dictionary<RID, RenderingCanvasRuntime> Items = [];
    private static readonly List<RID> Stale = [];
    private static int _registrations;
    internal static RenderingCanvasRuntime Register(RenderingServer? owner = null, CanvasLayer? layer = null, Viewport? viewport = null)
    {
        var rid = RID.Allocate(); var value = new RenderingCanvasRuntime(rid, owner, layer, viewport);
        lock (Gate) { if (++_registrations == 256) { _registrations = 0; foreach (var pair in Items) if (!pair.Value.Alive) Stale.Add(pair.Key); foreach (var old in Stale) Items.Remove(old); Stale.Clear(); } Items.Add(rid, value); }
        return value;
    }
    internal static bool Contains(RID rid) { lock (Gate) return Items.ContainsKey(rid); }
    internal static RenderingCanvasRuntime? ResolveOrNull(RID rid) { lock (Gate) { if (Items.TryGetValue(rid, out var value) && value.Alive) return value; Items.Remove(rid); return null; } }
    internal static RenderingCanvasRuntime Resolve(RID rid) => ResolveOrNull(rid) ?? throw new ArgumentException("The canvas identity is absent or disposed.", nameof(rid));
    internal static void Remove(RID rid) { lock (Gate) Items.Remove(rid); }
}

internal sealed class RenderingCanvasItemState
{
    internal readonly RenderingServer Owner;
    internal readonly bool Owned;
    internal RID Parent;
    internal bool ParentAssigned;
    internal Transform? Transform;
    internal Color? Modulate, SelfModulate;
    internal bool? Visible, Behind, ZRelative, YSort, Clip;
    internal int? Z;
    internal TextureFilter? Filter;
    internal TextureRepeat? Repeat;
    internal Rect2? CustomRect;
    internal CanvasItem? Commands;
    internal readonly List<WeakReference<CanvasItem>> Children = [];
    internal RenderingCanvasItemState(RenderingServer owner, bool owned) { Owner = owner; Owned = owned; }
    internal void AddChild(CanvasItem child) { foreach (var weak in Children) if (weak.TryGetTarget(out var old) && ReferenceEquals(old, child)) return; Children.Add(new(child)); }
}
