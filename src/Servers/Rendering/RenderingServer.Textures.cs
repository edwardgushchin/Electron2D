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
        var resource = RenderingTextureRegistry.ResolveResource(baseTexture);
        if (resource is TextureLayered) return OwnLayered(new LayeredTextureProxy(baseTexture));
        var source = (Texture)resource;
        var texture = new ServerTexture(baseTexture, source);
        var rid = RenderingTextureRegistry.Register(texture, this);
        texture.Bind(rid); _ownedTextureRIDs.Add(rid);
        return rid;
    }

    internal void TextureProxyUpdateCore(RID texture, RID proxyTo)
    {
        EnsureTextureChange();
        var owned = RenderingTextureRegistry.OwnedResource(texture, this);
        var sampled = RenderingTextureRegistry.ResolveResource(proxyTo);
        if (owned is LayeredTextureProxy layered)
        {
            if (sampled is not TextureLayered) throw new ArgumentException("Proxy source shapes do not match.", nameof(proxyTo));
            if (sampled is LayeredTextureProxy) throw new InvalidOperationException("Layered proxy updates require a non-proxy source.");
            layered.SetTarget(proxyTo); return;
        }
        var target = owned as ServerTexture ?? throw new InvalidOperationException("The destination is not a proxy.");
        var source = sampled as Texture ?? throw new ArgumentException("Proxy source shapes do not match.", nameof(proxyTo));
        if (!target.IsProxy || source is ServerTexture { IsProxy: true })
            throw new InvalidOperationException("Proxy updates require an owned proxy and a non-proxy source.");
        target.SetProxyTarget(proxyTo, source);
    }

    internal void TextureReplaceCore(RID texture, RID byTexture)
    {
        EnsureTextureChange();
        var destination = RenderingTextureRegistry.OwnedResource(texture, this);
        var replacement = RenderingTextureRegistry.OwnedResource(byTexture, this);
        if (destination is TextureArray array && replacement is TextureArray byArray)
        {
            if (texture == byTexture) return;
            array.ReplaceLayers(byArray.CaptureLayers()); array.ServerPath = byArray.ServerPath;
            foreach (var rid in _ownedTextureRIDs)
                if (RenderingTextureRegistry.OwnedResource(rid, this) is LayeredTextureProxy proxy && proxy.ProxyTarget == byTexture) proxy.SetTarget(texture);
            FreeRIDCore(byTexture); return;
        }
        var target = destination as ServerTexture ?? throw new InvalidOperationException("Replacement requires matching concrete texture roles.");
        var source = replacement as ServerTexture ?? throw new InvalidOperationException("Replacement requires matching concrete texture roles.");
        if (target.IsProxy || source.IsProxy) throw new InvalidOperationException("A proxy cannot be replaced or consumed as replacement pixels.");
        if (texture == byTexture) return;
        target.Pixels = source.Pixels; target.Size = source.Size; target.Path = source.Path;
        foreach (var rid in _ownedTextureRIDs)
        {
            var proxy = RenderingTextureRegistry.OwnedResource(rid, this) as ServerTexture;
            if (proxy is { IsProxy: true } && proxy.ProxyTarget == byTexture) proxy.RedirectProxy(texture);
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
        var resource = RenderingTextureRegistry.OwnedResource(texture, this);
        if (resource is TextureArray array) { array.UpdateLayer(image, layer); return; }
        if (layer != 0) throw new ArgumentOutOfRangeException(nameof(layer));
        var target = resource as ServerTexture ?? throw new InvalidOperationException("Update the concrete image-array source of a proxy.");
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
        EnsureOwner(); var resource = RenderingTextureRegistry.ResolveResource(texture);
        if (resource is TextureLayered array) return array.GetFormat();
        var source = (Texture)resource;
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
        var resource = RenderingTextureRegistry.OwnedResource(texture, this);
        if (resource is TextureLayered array) array.ServerPath = path; else ((ServerTexture)resource).Path = path;
    }

    internal string TextureGetPathCore(RID texture)
    {
        EnsureOwner(); var target = RenderingTextureRegistry.ResolveResource(texture);
        return target is ServerTexture owned ? owned.Path : target is TextureLayered { ServerOwner: not null } array ? array.ServerPath : target.ResourcePath;
    }

    internal void FreeRIDCore(RID rid)
    {
        EnsureTextureChange(); if (ReleaseProgramRID(rid) || ReleaseCanvasRID(rid)) return; if (RenderingSkeletonRegistry.Contains(rid)) { RenderingSkeletonRegistry.Owned(rid, this).EnsureWritable(); _ownedSkeletonRIDs.Remove(rid); RenderingSkeletonRegistry.Remove(rid); return; }
        if (RenderingMultiMeshRegistry.Contains(rid)) { var resource = RenderingMultiMeshRegistry.Owned(rid, this); _ownedMultiMeshRIDs.Remove(rid); RenderingMultiMeshRegistry.Remove(rid); resource.Dispose(); return; }
        if (RenderingMeshRegistry.Contains(rid)) { var mesh = RenderingMeshRegistry.Owned(rid, this); _ownedMeshRIDs.Remove(rid); RenderingMeshRegistry.Remove(rid); mesh.Dispose(); return; }
        var texture = RenderingTextureRegistry.OwnedResource(rid, this);
        _ownedTextureRIDs.Remove(rid); RenderingTextureRegistry.Remove(rid);
        if (texture is ServerTexture ordinary) ordinary.Released = true; else ((TextureLayered)texture).ServerReleased = true;
        texture.Dispose();
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
            var texture = RenderingTextureRegistry.OwnedResource(rid, this);
            RenderingTextureRegistry.Remove(rid);
            if (texture is ServerTexture ordinary) ordinary.Released = true; else ((TextureLayered)texture).ServerReleased = true;
            try { texture.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        }
        if (errors is not null) throw new AggregateException("Texture cleanup failed.", errors);
    }
}
