namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private UniformSetCacheRD? _uniformCache;
    private FramebufferCacheRD? _framebufferCache;
    internal UniformSetCacheRD UniformCache => _uniformCache ??= UniformSetCacheRD.Create(this);
    internal FramebufferCacheRD FramebufferCache => _framebufferCache ??= FramebufferCacheRD.Create(this);
    internal void CacheAccess(bool framebuffer) { if (framebuffer) IdleList(); else Access(); }
    internal void ReleaseCachedRID(RID rid) => _entries.Remove(rid);
    private void EvictResourceCaches(RID removed = default) { _uniformCache?.Evict(removed); _framebufferCache?.Evict(removed); }
    private void RetireResourceCaches() { _uniformCache?.Retire(); _framebufferCache?.Retire(); }
}
