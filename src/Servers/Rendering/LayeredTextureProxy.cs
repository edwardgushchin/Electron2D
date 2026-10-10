namespace Electron2D;

internal sealed class LayeredTextureProxy : TextureLayered
{
    internal RID ProxyTarget { get; private set; }
    private TextureLayered Source => RenderingTextureRegistry.ResolveLayeredProxySource(ProxyTarget)
        ?? throw new InvalidOperationException("The sampled layered proxy has no live source.");
    internal LayeredTextureProxy(RID target) { SetTarget(target); }
    internal void SetTarget(RID target) { RenderingTextureRegistry.ResolveLayered(target); ProxyTarget = target; }
    public override Image.Format GetFormat() { ThrowIfDisposed(); return Source.GetFormat(); }
    public override int GetWidth() { ThrowIfDisposed(); return Source.GetWidth(); }
    public override int GetHeight() { ThrowIfDisposed(); return Source.GetHeight(); }
    public override int GetLayers() { ThrowIfDisposed(); return Source.GetLayers(); }
    public override bool HasMipmaps() { ThrowIfDisposed(); return Source.HasMipmaps(); }
    public override Image? GetLayerData(int layerIndex) { ThrowIfDisposed(); return Source.GetLayerData(layerIndex); }
    internal override LayeredTexturePixels? CaptureLayers() { ThrowIfDisposed(); return Source.CaptureLayers(); }
}
