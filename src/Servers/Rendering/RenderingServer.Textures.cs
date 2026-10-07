namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<RID> _ownedTextureRIDs = [];

    internal RID ViewportGetTextureCore(RID viewport) { EnsureOwner(); return Viewport.ResolveViewportRID(viewport).GetTexture().GetRID(); }

    internal RID Texture2DCreateCore(Image image)
    {
        EnsureTextureChange();
        var texture = new ServerTexture(TexturePixels.FromImage(image));
        var rid = RenderingTextureRegistry.Register(texture, this);
        texture.Bind(rid); _ownedTextureRIDs.Add(rid);
        return rid;
    }

    internal RID Texture2DPlaceholderCreateCore()
    {
        EnsureTextureChange();
        using var image = RenderingTextureRegistry.PlaceholderPixels.CopyImage();
        return Texture2DCreateCore(image);
    }

    internal RID TextureProxyCreateCore(RID baseTexture)
    {
        EnsureTextureChange();
        var source = RenderingTextureRegistry.Resolve(baseTexture);
        var texture = new ServerTexture(baseTexture, source);
        var rid = RenderingTextureRegistry.Register(texture, this);
        texture.Bind(rid); _ownedTextureRIDs.Add(rid);
        return rid;
    }

    internal void TextureProxyUpdateCore(RID texture, RID proxyTo)
    {
        EnsureTextureChange();
        var target = RenderingTextureRegistry.Owned(texture, this);
        var source = RenderingTextureRegistry.Resolve(proxyTo);
        if (!target.IsProxy || source is ServerTexture { IsProxy: true })
            throw new InvalidOperationException("Proxy updates require an owned proxy and a non-proxy source.");
        target.SetProxyTarget(proxyTo, source);
    }

    internal void TextureReplaceCore(RID texture, RID byTexture)
    {
        EnsureTextureChange();
        var target = RenderingTextureRegistry.Owned(texture, this);
        var source = RenderingTextureRegistry.Owned(byTexture, this);
        if (target.IsProxy || source.IsProxy) throw new InvalidOperationException("A proxy cannot be replaced or consumed as replacement pixels.");
        if (texture == byTexture) return;
        target.Pixels = source.Pixels; target.Size = source.Size; target.Path = source.Path;
        foreach (var rid in _ownedTextureRIDs)
        {
            var proxy = RenderingTextureRegistry.Owned(rid, this);
            if (proxy.IsProxy && proxy.ProxyTarget == byTexture) proxy.RedirectProxy(texture);
        }
        FreeRIDCore(byTexture);
    }

    internal Image? Texture2DGetCore(RID texture)
    {
        EnsureOwner();
        var source = RenderingTextureRegistry.Resolve(texture);
        if (source is ServerTexture { IsProxy: true } proxy)
        {
            source = RenderingTextureRegistry.ResolveProxySource(proxy.ProxyTarget);
            if (source is null) return null;
        }
        return source is ViewportTexture view ? view.GetImage() : (source.CapturePixels() ?? RenderingTextureRegistry.PlaceholderPixels).CopyImage();
    }

    internal void Texture2DUpdateCore(RID texture, Image image, int layer = 0)
    {
        EnsureTextureChange();
        if (layer != 0) throw new ArgumentOutOfRangeException(nameof(layer));
        var target = RenderingTextureRegistry.Owned(texture, this);
        if (target.IsProxy) throw new InvalidOperationException("Update the source pixels of a proxy texture.");
        var pixels = TexturePixels.FromImage(image);
        var old = target.Pixels.Source; var next = pixels.Source;
        if (old.Width != next.Width || old.Height != next.Height || old.Format != next.Format || old.HasMipmaps != next.HasMipmaps)
            throw new ArgumentException("Texture updates require matching dimensions, format and mipmap state.", nameof(image));
        pixels.Allocation = target.Pixels.Allocation;
        target.Pixels = pixels;
    }

    internal Image.Format TextureGetFormatCore(RID texture)
    {
        EnsureOwner(); var source = RenderingTextureRegistry.Resolve(texture);
        return source is ServerTexture { IsProxy: true } proxy ? proxy.PixelFormat : source.CapturePixels()?.Source.Format ?? Image.Format.Rgba8;
    }

    internal void TextureSetSizeOverrideCore(RID texture, int width, int height)
    {
        EnsureTextureChange();
        if (width is < 1 or > 16384) throw new ArgumentOutOfRangeException(nameof(width));
        if (height is < 1 or > 16384) throw new ArgumentOutOfRangeException(nameof(height));
        var target = RenderingTextureRegistry.Owned(texture, this);
        target.SetDisplaySize(new(width, height));
    }

    internal void TextureSetPathCore(RID texture, string path)
    {
        EnsureTextureChange(); ArgumentNullException.ThrowIfNull(path);
        RenderingTextureRegistry.Owned(texture, this).Path = path;
    }

    internal string TextureGetPathCore(RID texture)
    {
        EnsureOwner(); var target = RenderingTextureRegistry.Resolve(texture);
        return target is ServerTexture owned ? owned.Path : target.ResourcePath;
    }

    internal void FreeRIDCore(RID rid)
    {
        EnsureTextureChange(); if (ReleaseProgramRID(rid) || ReleaseCanvasRID(rid)) return; if (RenderingSkeletonRegistry.Contains(rid)) { RenderingSkeletonRegistry.Owned(rid, this).EnsureWritable(); _ownedSkeletonRIDs.Remove(rid); RenderingSkeletonRegistry.Remove(rid); return; }
        if (RenderingMultiMeshRegistry.Contains(rid)) { var resource = RenderingMultiMeshRegistry.Owned(rid, this); _ownedMultiMeshRIDs.Remove(rid); RenderingMultiMeshRegistry.Remove(rid); resource.Dispose(); return; }
        if (RenderingMeshRegistry.Contains(rid)) { var mesh = RenderingMeshRegistry.Owned(rid, this); _ownedMeshRIDs.Remove(rid); RenderingMeshRegistry.Remove(rid); mesh.Dispose(); return; }
        var texture = RenderingTextureRegistry.Owned(rid, this);
        _ownedTextureRIDs.Remove(rid); RenderingTextureRegistry.Remove(rid); texture.Released = true; texture.Dispose();
    }

    private void EnsureTextureChange()
    {
        EnsureOwner();
        if (_submittingTextures || _closing) throw new InvalidOperationException("Texture resources cannot change during submission or shutdown.");
    }

    private void ReleaseOwnedTextures()
    {
        List<Exception>? errors = null;
        while (_ownedTextureRIDs.Count > 0)
        {
            var rid = _ownedTextureRIDs[^1]; _ownedTextureRIDs.RemoveAt(_ownedTextureRIDs.Count - 1);
            var texture = RenderingTextureRegistry.Owned(rid, this);
            RenderingTextureRegistry.Remove(rid); texture.Released = true;
            try { texture.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        }
        if (errors is not null) throw new AggregateException("Texture cleanup failed.", errors);
    }
}
