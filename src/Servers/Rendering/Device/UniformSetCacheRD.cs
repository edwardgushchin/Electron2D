namespace Electron2D;

/// <summary>Reuses descriptor sets belonging to the active GPU rendering session.</summary>
/// <remarks>Cached identities borrow their shader and bound resources. Dependency retirement, explicit FreeRID and
/// renderer shutdown evict them. Local-device identities cannot enter this process-wide service.</remarks>
public sealed class UniformSetCacheRD : ElectronObject
{
    private readonly RenderingDevice _device;
    private readonly Dictionary<int, List<Cached>> _buckets = [];
    private bool _retiring;
    private sealed record Binding(int Slot, RenderingDevice.UniformType Type, RID[] IDs);
    private sealed record Cached(RID RID, RID Shader, uint Set, Binding[] Bindings);
    private UniformSetCacheRD(RenderingDevice device) => _device = device;
    internal static UniformSetCacheRD Create(RenderingDevice device) => new(device);

    /// <summary>Creates or returns an intact set with the same ordered binding contents.</summary>
    /// <param name="shader">A live shader of the active renderer device.</param>
    /// <param name="set">The shader descriptor-set number.</param>
    /// <param name="uniforms">Copied on a miss; later description edits and disposal do not alter the cached set.</param>
    /// <returns>A cache-managed set RID. Explicitly freeing it evicts it; a later request creates another identity.</returns>
    /// <exception cref="InvalidOperationException">No active GPU renderer exists, or device/thread/callback access is invalid.</exception>
    /// <exception cref="ArgumentException">A description, shader, resource identity or layout is invalid.</exception>
    public static RID GetCache(RID shader, uint set, ReadOnlySpan<RDUniform> uniforms)
    {
        var device = RenderingServer.GetRenderingDevice() ?? throw new InvalidOperationException("An active GPU rendering session is required.");
        return device.UniformCache.Get(shader, set, uniforms);
    }
    private RID Get(RID shader, uint set, ReadOnlySpan<RDUniform> uniforms)
    {
        _device.CacheAccess(false);
        var hash = new HashCode(); hash.Add(shader); hash.Add(set); hash.Add(uniforms.Length);
        foreach (var uniform in uniforms)
        {
            ArgumentNullException.ThrowIfNull(uniform);
            hash.Add(uniform.Binding); hash.Add(uniform.UniformType);
            var ids = uniform.BorrowIDs; hash.Add(ids.Length); foreach (var id in ids) hash.Add(id);
        }
        var key = hash.ToHashCode();
        if (_buckets.TryGetValue(key, out var bucket))
            foreach (var cached in bucket)
                if (cached.Shader == shader && cached.Set == set && Matches(cached.Bindings, uniforms) && _device.CachedUniformIsValid(cached.RID)) return cached.RID;
        var bindings = new Binding[uniforms.Length];
        for (var i = 0; i < bindings.Length; i++) bindings[i] = new(uniforms[i].Binding, uniforms[i].UniformType, uniforms[i].BorrowIDs.ToArray());
        var rid = _device.UniformSetCreate(uniforms, shader, set);
        try
        {
            if (!_buckets.TryGetValue(key, out bucket)) _buckets.Add(key, bucket = []);
            bucket.Add(new(rid, shader, set, bindings));
        }
        catch { _device.ReleaseCachedRID(rid); throw; }
        return rid;
    }
    private static bool Matches(Binding[] saved, ReadOnlySpan<RDUniform> values)
    {
        if (saved.Length != values.Length) return false;
        for (var i = 0; i < saved.Length; i++)
            if (saved[i].Slot != values[i].Binding || saved[i].Type != values[i].UniformType || !values[i].BorrowIDs.SequenceEqual(saved[i].IDs)) return false;
        return true;
    }
    internal void Evict(RID removed = default)
    {
        foreach (var pair in _buckets)
        {
            var values = pair.Value;
            for (var i = values.Count - 1; i >= 0; i--)
                if (values[i].RID == removed || !_device.CachedUniformIsValid(values[i].RID))
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
