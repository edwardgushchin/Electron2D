namespace Electron2D;

internal static class RenderingCanvasItemRegistry
{
    // ponytail: a cold weak identity gate; partition only after measured contention.
    private static readonly object Gate = new();
    private static readonly Dictionary<RID, WeakReference<CanvasItem>> Items = [];
    private static int _registrations;
    private static readonly List<RID> Stale = [];
    internal static RID Register(CanvasItem item)
    {
        var rid = RID.Allocate(); lock (Gate) { if (++_registrations == 256) { _registrations = 0; foreach (var pair in Items) if (!pair.Value.TryGetTarget(out var target) || target.IsDisposed) Stale.Add(pair.Key); foreach (var old in Stale) Items.Remove(old); Stale.Clear(); } Items.Add(rid, new(item)); }
        return rid;
    }
    internal static bool Contains(RID rid) { lock (Gate) return Items.ContainsKey(rid); }
    internal static void Remove(RID rid) { lock (Gate) Items.Remove(rid); }
    internal static CanvasItem Resolve(RID rid) { lock (Gate) { if (Items.TryGetValue(rid, out var weak) && weak.TryGetTarget(out var item) && !item.IsDisposed) return item; Items.Remove(rid); throw new ArgumentException("The canvas item identity is absent or disposed.", nameof(rid)); } }
}
