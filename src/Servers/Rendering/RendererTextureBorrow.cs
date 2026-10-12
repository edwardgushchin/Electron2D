namespace Electron2D;

internal sealed class RendererTextureBorrow : IDisposable
{
    internal readonly Resource Source;
    internal readonly GPUTexture? Pixels;
    internal readonly CanvasRenderTarget? Target;
    private readonly RenderHandle _first;
    private readonly RenderHandle? _second;
    private readonly Viewport? _viewport;
    private bool _firstHeld, _secondHeld, _disposed;
    internal RendererTextureBorrow(Resource source, GPUTexture pixels)
    {
        Source = source; Pixels = pixels; _first = pixels.Allocation;
        try { _first.DangerousAddRef(ref _firstHeld); if (source is Texture texture) texture.AcquireRendererCacheResidency(); else if (source is TextureLayered layered) layered.AcquireRendererCacheResidency(); }
        catch { if (_firstHeld) _first.DangerousRelease(); throw; }
    }
    internal RendererTextureBorrow(ViewportTexture source, CanvasRenderTarget target)
    {
        Source = source; Target = target; _viewport = source.Bound; _first = target.Current; _second = target.Next;
        try { _first.DangerousAddRef(ref _firstHeld); _second.DangerousAddRef(ref _secondHeld); }
        catch { if (_secondHeld) _second.DangerousRelease(); if (_firstHeld) _first.DangerousRelease(); throw; }
    }
    internal bool Alive
    {
        get
        {
            if (_disposed || Source.IsDisposed) return false;
            if (Target is not null) return !Target.IsDisposed && _viewport is { IsDisposed: false } && ReferenceEquals(((ViewportTexture)Source).Bound, _viewport);
            if (Pixels is null || Pixels.IsDisposed) return false;
            return Source switch { Texture texture => texture.CapturePixels() is { } p && ReferenceEquals(p.Allocation, Pixels.Pixels.Allocation), TextureLayered layered => layered.CaptureLayers() is { } p && ReferenceEquals(p.Allocation, Pixels.ArrayPixels!.Allocation), _ => false };
        }
    }
    internal nint Native => Target?.Current.DangerousGetHandle() ?? Pixels!.Handle;
    internal void RefreshPixels()
    {
        if (Pixels is null) return;
        if (Source is Texture texture) Pixels.Pixels = texture.CapturePixels()!;
        else if (Source is TextureLayered layered) { Pixels.ArrayPixels = layered.CaptureLayers()!; Pixels.Pixels = Pixels.ArrayPixels.First; }
    }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        try { if (Pixels is not null) { if (Source is Texture texture) texture.ReleaseRendererCacheResidency(); else if (Source is TextureLayered layered) layered.ReleaseRendererCacheResidency(); } }
        finally { try { if (_secondHeld) _second!.DangerousRelease(); } finally { if (_firstHeld) _first.DangerousRelease(); } }
    }
}
