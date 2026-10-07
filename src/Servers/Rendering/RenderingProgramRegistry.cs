namespace Electron2D;

internal static class RenderingProgramRegistry<T> where T : Resource
{
    // ponytail: two cold resource kinds share one typed registry policy; partition only after measured contention.
    private static readonly object Gate = new();
    internal sealed class Entry(T resource, RenderingServer? owner)
    {
        internal readonly WeakReference<T> Weak = new(resource);
        internal readonly T? Retained = owner is null ? null : resource;
        internal readonly RenderingServer? Owner = owner;
        internal string PathHint = "";
    }
    private static readonly Dictionary<RID, Entry> Entries = [];
    private static readonly List<RID> Stale = [];
    private static int _registrations;
    internal static RID Register(T resource, RenderingServer? owner = null)
    {
        var rid = RID.Allocate();
        lock (Gate)
        {
            if (++_registrations == 256) { _registrations = 0; foreach (var pair in Entries) if (pair.Value.Owner is null && (!pair.Value.Weak.TryGetTarget(out var target) || target.IsDisposed)) Stale.Add(pair.Key); foreach (var old in Stale) Entries.Remove(old); Stale.Clear(); }
            Entries.Add(rid, new(resource, owner));
        }
        return rid;
    }
    internal static bool Contains(RID rid) { lock (Gate) return Entries.ContainsKey(rid); }
    internal static T? ResolveOrNull(RID rid) { lock (Gate) { if (Entries.TryGetValue(rid, out var entry) && entry.Weak.TryGetTarget(out var resource) && !resource.IsDisposed) return resource; if (entry?.Owner is null) Entries.Remove(rid); return null; } }
    internal static T Resolve(RID rid) => ResolveOrNull(rid) ?? throw new ArgumentException("The rendering program identity is absent or disposed.", nameof(rid));
    internal static Entry OwnedEntry(RID rid, RenderingServer owner)
    {
        lock (Gate) { if (!Entries.TryGetValue(rid, out var entry)) throw new ArgumentException("The rendering program identity is absent.", nameof(rid)); if (!ReferenceEquals(entry.Owner, owner)) throw new InvalidOperationException("Rendering program mutation/free requires the owning renderer."); return entry; }
    }
    internal static T Owned(RID rid, RenderingServer owner) => OwnedEntry(rid, owner).Retained!;
    internal static void Remove(RID rid) { lock (Gate) Entries.Remove(rid); }
}
