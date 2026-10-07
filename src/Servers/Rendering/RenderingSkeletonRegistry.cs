namespace Electron2D;

internal static class RenderingSkeletonRegistry
{
    // ponytail: one gate protects cold weak identities; partition only after measured contention.
    private static readonly object Gate = new();
    private static readonly Dictionary<RID, WeakReference<Skeleton>> Items = [];
    internal sealed class Palette { internal Transform[] Transforms = []; internal Transform Base = Transform.Identity; internal int ReplayReaders; internal void EnsureWritable() { if (ReplayReaders != 0) throw new InvalidOperationException("Palette storage cannot change during mesh replay callbacks."); } }
    private static readonly Dictionary<RID, (RenderingServer Owner, Palette Data)> OwnedItems = [];
    internal static RID RegisterOwned(RenderingServer owner) { var rid = RID.Allocate(); lock (Gate) OwnedItems.Add(rid, (owner, new())); return rid; }
    internal static Palette Owned(RID rid, RenderingServer owner) { lock (Gate) { if (!OwnedItems.TryGetValue(rid, out var value)) { if (Items.ContainsKey(rid)) throw new InvalidOperationException("Scene palettes cannot be mutated or freed by the renderer."); throw new ArgumentException("The palette identity is absent.", nameof(rid)); } if (!ReferenceEquals(value.Owner, owner)) throw new InvalidOperationException("Palette ownership belongs to another renderer."); return value.Data; } }
    internal static ReadOnlySpan<Transform> Read(RID rid) { lock (Gate) { if (OwnedItems.TryGetValue(rid, out var value)) return value.Data.Transforms; } if (Resolve(rid) is { } scene) return scene.PrepareSkinPalette(); throw new ArgumentException("The palette identity is absent or disposed.", nameof(rid)); }
    internal static bool Prepare(RID rid, CanvasItem consumer, out ReadOnlySpan<Transform> transforms, out Transform basis, out Palette? lease)
    {
        lease = null; lock (Gate) { if (OwnedItems.TryGetValue(rid, out var value)) { if (value.Data.Transforms.Length != 0) { lease = value.Data; lease.ReplayReaders++; } transforms = value.Data.Transforms; basis = value.Data.Base; return transforms.Length != 0; } }
        if (Resolve(rid) is { IsInsideTree: true } scene && ReferenceEquals(scene.Tree, consumer.Tree ?? consumer.ServerState?.Owner?.CanvasSceneTree) && ReferenceEquals(scene.RenderCanvas(), consumer.RenderCanvas())) { transforms = scene.PrepareSkinPalette(); basis = scene.RenderGlobal((float)Engine.PhysicsInterpolationFraction); return transforms.Length != 0; }
        transforms = default; basis = default; return false;
    }
    private static int _registrations;
    private static readonly List<RID> Stale = [];
    internal static RID Register(Skeleton skeleton) { var rid = RID.Allocate(); lock (Gate) { if (++_registrations == 256) { _registrations = 0; foreach (var pair in Items) if (!pair.Value.TryGetTarget(out var target) || target.IsDisposed) Stale.Add(pair.Key); foreach (var old in Stale) Items.Remove(old); Stale.Clear(); } Items.Add(rid, new(skeleton)); } return rid; }
    internal static bool Contains(RID rid) { lock (Gate) return Items.ContainsKey(rid) || OwnedItems.ContainsKey(rid); }
    internal static void Remove(RID rid) { lock (Gate) { Items.Remove(rid); OwnedItems.Remove(rid); } }
    internal static Skeleton? Resolve(RID rid) { lock (Gate) { if (Items.TryGetValue(rid, out var weak) && weak.TryGetTarget(out var target) && !target.IsDisposed) return target; Items.Remove(rid); return null; } }
}
