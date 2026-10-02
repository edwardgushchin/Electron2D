namespace Electron2D;

internal static class RenderingMeshRegistry
{
    // ponytail: one gate serializes mesh identity lookup; partition by renderer if measured contention warrants it.
    private static readonly object Gate = new();
    private static readonly Dictionary<RID, Entry> Meshes = [];
    private static readonly List<RID> Stale = [];
    private static int _registrationsSinceSweep;
    private sealed record Entry(WeakReference<Mesh> Reference, RenderingServer? Owner, ArrayMesh? Owned);
    internal static RID Register(Mesh mesh, RenderingServer? owner = null)
    {
        var rid = RID.Allocate();
        lock (Gate)
        {
            if (++_registrationsSinceSweep >= 256)
            {
                _registrationsSinceSweep = 0;
                foreach (var pair in Meshes)
                    if (!pair.Value.Reference.TryGetTarget(out var target) || target.IsDisposed) Stale.Add(pair.Key);
                foreach (var stale in Stale) Meshes.Remove(stale);
                Stale.Clear();
            }
            Meshes.Add(rid, new(new(mesh), owner, owner is null ? null : (ArrayMesh)mesh));
        }
        return rid;
    }
    internal static ArrayMesh Owned(RID rid, RenderingServer owner) { lock (Gate) { if (!Meshes.TryGetValue(rid, out var entry) || !entry.Reference.TryGetTarget(out var mesh) || mesh.IsDisposed) throw new ArgumentException("The mesh identity is absent.", nameof(rid)); if (!ReferenceEquals(entry.Owner, owner) || entry.Owned is null) throw new InvalidOperationException("Mesh ownership does not belong to this renderer."); return entry.Owned; } }
    internal static bool Contains(RID rid) { lock (Gate) return Meshes.ContainsKey(rid); }
    internal static Mesh Resolve(RID rid)
    {
        lock (Gate)
        {
            if (Meshes.TryGetValue(rid, out var reference) && reference.Reference.TryGetTarget(out var mesh) && !mesh.IsDisposed) return mesh;
            Meshes.Remove(rid); throw new ArgumentException("The mesh identity is absent or disposed.", nameof(rid));
        }
    }
    internal static void Remove(RID rid) { lock (Gate) Meshes.Remove(rid); }
}
