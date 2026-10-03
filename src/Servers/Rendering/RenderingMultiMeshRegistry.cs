namespace Electron2D;

internal static class RenderingMultiMeshRegistry
{
    // ponytail: one gate serializes logical identities; split by renderer only if contention is measured.
    private static readonly object Gate = new();
    private sealed record Entry(WeakReference<MultiMesh> Reference, RenderingServer? Owner, MultiMesh? Owned);
    private static readonly Dictionary<RID, Entry> Entries = [];
    private static readonly List<RID> Stale = [];
    private static readonly List<MultiMesh> TickTargets = [];
    private static int _registrations;
    private static bool _ticking;
    internal static RID Register(MultiMesh value, RenderingServer? owner = null)
    {
        lock (Gate)
        {
            if (++_registrations % 256 == 0)
            {
                foreach (var pair in Entries) if (!pair.Value.Reference.TryGetTarget(out var target) || target.IsDisposed) Stale.Add(pair.Key);
                foreach (var staleRID in Stale) Entries.Remove(staleRID);
                Stale.Clear();
            }
            var rid = RID.Allocate(); Entries.Add(rid, new(new(value), owner, owner is null ? null : value));
            TickTargets.EnsureCapacity(Entries.Count); return rid;
        }
    }
    internal static bool Contains(RID rid) { lock (Gate) return Entries.ContainsKey(rid); }
    internal static MultiMesh Resolve(RID rid)
    {
        lock (Gate)
        {
            if (Entries.TryGetValue(rid, out var entry) && entry.Reference.TryGetTarget(out var target) && !target.IsDisposed) return target;
            Entries.Remove(rid); throw new ArgumentException("The instance resource identity is absent or disposed.", nameof(rid));
        }
    }
    internal static MultiMesh Owned(RID rid, RenderingServer owner)
    {
        lock (Gate)
        {
            if (!Entries.TryGetValue(rid, out var entry) || !entry.Reference.TryGetTarget(out var resource) || resource.IsDisposed) throw new ArgumentException("The instance resource identity is absent or disposed.", nameof(rid));
            if (entry.Owned is null || !ReferenceEquals(owner, entry.Owner)) throw new InvalidOperationException("Instance storage belongs to another renderer.");
            return entry.Owned;
        }
    }
    internal static void Remove(RID rid) { lock (Gate) Entries.Remove(rid); }
    internal static void PhysicsTick(bool start)
    {
        lock (Gate)
        {
            if (_ticking) throw new InvalidOperationException("Instance interpolation capture cannot be reentered.");
            _ticking = true; TickTargets.Clear();
            foreach (var entry in Entries.Values) if (entry.Reference.TryGetTarget(out var target) && !target.IsDisposed) TickTargets.Add(target);
        }
        try { foreach (var target in TickTargets) target.PhysicsTick(start); }
        finally { lock (Gate) { TickTargets.Clear(); _ticking = false; } }
    }
}
