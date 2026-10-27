namespace Electron2D;

/// <summary>Reuses copied multipass framebuffers belonging to the active GPU rendering session.</summary>
/// <remarks>Attachments are borrowed. Their retirement, explicit framebuffer FreeRID and renderer shutdown evict
/// cached identities. Local-device identities cannot enter this process-wide service.</remarks>
public sealed class FramebufferCacheRD : ElectronObject
{
    private readonly RenderingDevice _device;
    private readonly Dictionary<int, List<Cached>> _buckets = [];
    private bool _retiring;
    private sealed record Pass(int Depth, int[] Colors, int[] Resolves, int[] Inputs, int[] Preserves);
    private sealed record Cached(RID RID, RID[] Textures, int Views, Pass[] Passes);
    private FramebufferCacheRD(RenderingDevice device) => _device = device;
    internal static FramebufferCacheRD Create(RenderingDevice device) => new(device);

    /// <summary>Creates or returns an intact framebuffer with the same ordered attachments, pass contents and views.</summary>
    /// <param name="textures">Target/resolve/input textures of the active renderer device, copied on a miss. Empty RID slots must remain unused by every pass.</param>
    /// <param name="passes">Ordered copied descriptions; empty selects the ordinary inferred single pass.</param>
    /// <param name="views">One through 256 ordered image layers.</param>
    /// <returns>A cache-managed framebuffer RID. Description edits and disposal do not alter previous results.</returns>
    /// <exception cref="InvalidOperationException">No active GPU renderer exists, a list is active, or device/thread/callback access is invalid.</exception>
    /// <exception cref="ArgumentException">An attachment, pass, identity or layout is invalid.</exception>
    /// <exception cref="NotSupportedException">The native format/sample profile is unavailable.</exception>
    public static RID GetCacheMultipass(ReadOnlySpan<RID> textures, ReadOnlySpan<RDFramebufferPass> passes, int views)
    {
        var device = RenderingServer.GetRenderingDevice() ?? throw new InvalidOperationException("An active GPU rendering session is required.");
        return device.FramebufferCache.Get(textures, passes, views);
    }
    private RID Get(ReadOnlySpan<RID> textures, ReadOnlySpan<RDFramebufferPass> passes, int views)
    {
        _device.CacheAccess(true);
        var hash = new HashCode(); hash.Add(views); hash.Add(textures.Length); foreach (var id in textures) hash.Add(id);
        hash.Add(passes.Length);
        foreach (var pass in passes)
        {
            ArgumentNullException.ThrowIfNull(pass); hash.Add(pass.DepthAttachment);
            Add(ref hash, pass.BorrowColors); Add(ref hash, pass.BorrowResolves); Add(ref hash, pass.BorrowInputs); Add(ref hash, pass.BorrowPreserves);
        }
        var key = hash.ToHashCode();
        if (_buckets.TryGetValue(key, out var bucket))
            foreach (var cached in bucket)
                if (cached.Views == views && textures.SequenceEqual(cached.Textures) && Matches(cached.Passes, passes) && _device.CachedFramebufferIsValid(cached.RID)) return cached.RID;
        var saved = new Pass[passes.Length];
        for (var i = 0; i < saved.Length; i++)
            saved[i] = new(passes[i].DepthAttachment, passes[i].BorrowColors.ToArray(), passes[i].BorrowResolves.ToArray(), passes[i].BorrowInputs.ToArray(), passes[i].BorrowPreserves.ToArray());
        var copied = textures.ToArray();
        var rid = passes.IsEmpty ? _device.FramebufferCreate(textures, viewCount: views) : _device.FramebufferCreateMultipass(textures, passes, viewCount: views);
        try
        {
            if (!_buckets.TryGetValue(key, out bucket)) _buckets.Add(key, bucket = []);
            bucket.Add(new(rid, copied, views, saved));
        }
        catch { _device.ReleaseCachedRID(rid); throw; }
        return rid;
    }
    private static void Add(ref HashCode hash, ReadOnlySpan<int> values) { hash.Add(values.Length); foreach (var value in values) hash.Add(value); }
    private static bool Matches(Pass[] saved, ReadOnlySpan<RDFramebufferPass> values)
    {
        if (saved.Length != values.Length) return false;
        for (var i = 0; i < saved.Length; i++)
        {
            var a = saved[i]; var b = values[i];
            if (a.Depth != b.DepthAttachment || !b.BorrowColors.SequenceEqual(a.Colors) || !b.BorrowResolves.SequenceEqual(a.Resolves) || !b.BorrowInputs.SequenceEqual(a.Inputs) || !b.BorrowPreserves.SequenceEqual(a.Preserves)) return false;
        }
        return true;
    }
    internal void Evict(RID removed = default)
    {
        foreach (var pair in _buckets)
        {
            var values = pair.Value;
            for (var i = values.Count - 1; i >= 0; i--)
                if (values[i].RID == removed || !_device.CachedFramebufferIsValid(values[i].RID))
                { _device.ReleaseCachedRID(values[i].RID); values.RemoveAt(i); }
            if (values.Count == 0) _buckets.Remove(pair.Key);
        }
    }
    internal void Retire() { _retiring = true; Dispose(); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (!_retiring) throw new InvalidOperationException("The rendering session owns this cache."); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) _buckets.Clear(); base.Dispose(disposing); }
}
