namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<RID> _ownedTextureRIDs = [];

    internal RID ViewportGetTextureCore(RID viewport) { EnsureOwner(); return ServerViewport(viewport).GetTexture().GetRID(); }

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
        if (destination is DrawableTexture drawableTarget && replacement is DrawableTexture drawableSource)
        {
            if (texture == byTexture) return;
            drawableTarget.TakeStorage(drawableSource); RedirectTextureAliases(byTexture, texture); FreeRIDCore(byTexture); drawableTarget.NotifyStorageChanged(); return;
        }
        if (destination is ServerTextureArray array && replacement is ServerTextureArray byArray)
        {
            if (texture == byTexture) return;
            ValidateReplacementStorage(array, array.DeviceTexture?.TextureRDRID ?? default,
                byArray.DeviceTexture?.TextureRDRID ?? default, array: true);
            var retireUpload = array.DeviceTexture is null && byArray.DeviceTexture is not null;
            array.TakeStorage(byArray);
            if (retireUpload && _backend is GPUCanvasBackend gpu) gpu.RetireReplacedUpload(array);
            RedirectTextureAliases(byTexture, texture);
            FreeRIDCore(byTexture);
            return;
        }
        if (destination is not ServerTexture { IsProxy: false } target ||
            replacement is not ServerTexture { IsProxy: false } source)
            throw new InvalidOperationException("Replacement requires matching concrete texture roles.");
        if (texture == byTexture) return;

        ValidateReplacementStorage(target, target.DeviceTexture?.TextureRDRID ?? default,
            source.DeviceTexture?.TextureRDRID ?? default, array: false);
        var retireOrdinaryUpload = target.DeviceTexture is null && source.DeviceTexture is not null;
        target.TakeStorage(source);
        if (retireOrdinaryUpload && _backend is GPUCanvasBackend backend) backend.RetireReplacedUpload(target);
        RedirectTextureAliases(byTexture, texture);
        FreeRIDCore(byTexture);
    }

    private void ValidateReplacementStorage(Resource target, RID previous, RID incoming, bool array)
    {
        if (!previous.IsValid() && !incoming.IsValid()) return;
        var device = GetRenderingDeviceCore() ?? throw new InvalidOperationException("A GPU renderer is required.");
        if (incoming.IsValid()) device.ValidateCanvasReplacement(incoming, array, target);
        else device.ValidateCanvasMutation();
    }
    private void RedirectTextureAliases(RID source, RID target)
    {
        foreach (var rid in _ownedTextureRIDs)
        {
            var resource = RenderingTextureRegistry.OwnedResource(rid, this);
            if (resource is ServerTexture { IsProxy: true } proxy && proxy.ProxyTarget == source) proxy.RedirectProxy(target);
            else if (resource is LayeredTextureProxy layered && layered.ProxyTarget == source) layered.SetTarget(target);
        }
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
        return source is DrawableTexture or TextureRD or ViewportTexture or ServerTexture { DeviceTexture: not null } ? source.GetImage() : (source.CapturePixels() ?? RenderingTextureRegistry.PlaceholderPixels).CopyImage();
    }

    internal void Texture2DUpdateCore(RID texture, Image image, int layer = 0)
    {
        EnsureTextureChange();
        var resource = RenderingTextureRegistry.OwnedResource(texture, this);
        if (resource is ServerTextureArray array) { array.UpdateLayer(image, layer); return; }
        if (layer != 0) throw new ArgumentOutOfRangeException(nameof(layer));
        var target = resource as ServerTexture ?? throw new InvalidOperationException("Update the concrete image-array source of a proxy.");
        if (target.IsProxy || target.DeviceTexture is not null) throw new InvalidOperationException("Update the concrete pixel source of this texture.");
        var pixels = TexturePixels.FromImage(image);
        var old = target.Pixels.Source; var next = pixels.Source;
        if (old.Width != next.Width || old.Height != next.Height || old.Format != next.Format || old.HasMIPMaps != next.HasMIPMaps)
            throw new ArgumentException("Texture updates require matching dimensions, format and mipmap state.", nameof(image));
        pixels.Allocation = target.Pixels.Allocation;
        target.Pixels = pixels;
    }

    internal Image.Format TextureGetFormatCore(RID texture)
    {
        EnsureOwner(); var resource = RenderingTextureRegistry.ResolveResource(texture);
        if (resource is TextureLayered array) return array.GetFormat();
        var source = (Texture)resource;
        return source is DrawableTexture drawable ? drawable.PixelFormat : source is TextureRD deviceTexture ? deviceTexture.PixelFormat : source is ServerTexture serverTexture ? serverTexture.PixelFormat : source.CapturePixels()?.Source.Format ?? Image.Format.Rgba8;
    }

    internal void TextureSetSizeOverrideCore(RID texture, int width, int height)
    {
        EnsureTextureChange();
        if (width is < 1 or > 16384) throw new ArgumentOutOfRangeException(nameof(width));
        if (height is < 1 or > 16384) throw new ArgumentOutOfRangeException(nameof(height));
        var resource = RenderingTextureRegistry.OwnedResource(texture, this);
        if (resource is DrawableTexture drawable) drawable.DisplaySize = new(width, height);
        else (resource as ServerTexture ?? throw new ArgumentException("A size override requires an ordinary texture.", nameof(texture))).SetDisplaySize(new(width, height));
    }

    internal void TextureSetPathCore(RID texture, string path)
    {
        EnsureTextureChange(); ArgumentNullException.ThrowIfNull(path);
        var resource = RenderingTextureRegistry.OwnedResource(texture, this);
        if (resource is DrawableTexture drawable) drawable.ServerPath = path; else if (resource is TextureLayered array) array.ServerPath = path; else ((ServerTexture)resource).Path = path;
    }

    internal string TextureGetPathCore(RID texture)
    {
        EnsureOwner(); var target = RenderingTextureRegistry.ResolveResource(texture);
        return target is DrawableTexture { ServerOwner: not null } drawable ? drawable.ServerPath : target is ServerTexture owned ? owned.Path : target is TextureLayered { ServerOwner: not null } array ? array.ServerPath : target is CompressedTextureLayered imported ? imported.RenderingPath : target is CompressedTexture compressed ? compressed.RenderingPath : target.ResourcePath;
    }

    internal void FreeRIDCore(RID rid)
    {
        EnsureTextureChange(); if (ReleaseParticleRID(rid) || ReleaseViewportRID(rid) || ReleaseLightingRID(rid) || ReleaseProgramRID(rid) || ReleaseCanvasRID(rid)) return; if (RenderingSkeletonRegistry.Contains(rid)) { RenderingSkeletonRegistry.Owned(rid, this).EnsureWritable(); _ownedSkeletonRIDs.Remove(rid); RenderingSkeletonRegistry.Remove(rid); return; }
        if (RenderingMultiMeshRegistry.Contains(rid)) { var resource = RenderingMultiMeshRegistry.Owned(rid, this); _ownedMultiMeshRIDs.Remove(rid); RenderingMultiMeshRegistry.Remove(rid); resource.Dispose(); return; }
        if (RenderingMeshRegistry.Contains(rid)) { var mesh = RenderingMeshRegistry.Owned(rid, this); _ownedMeshRIDs.Remove(rid); RenderingMeshRegistry.Remove(rid); mesh.Dispose(); return; }
        var texture = RenderingTextureRegistry.OwnedResource(rid, this);
        if (texture is DrawableTexture writable) writable.EnsureCanRelease();
        _ownedTextureRIDs.Remove(rid); RenderingTextureRegistry.Remove(rid);
        if (rid == _testTextureRID) _testTextureRID = default;
        if (rid == _whiteTextureRID) _whiteTextureRID = default;
        if (texture is DrawableTexture drawable) drawable.ServerReleased = true; else if (texture is ServerTexture ordinary) ordinary.Released = true; else if (texture is TextureLayered layered) layered.ServerReleased = true;
        texture.Dispose();
    }

    private void EnsureTextureChange()
    {
        EnsureOwner();
        if (_submittingTextures || _closing) throw new InvalidOperationException("Texture resources cannot change during submission or shutdown.");
        MarkChanged();
    }

    private void ReleaseOwnedTextures()
    {
        List<Exception>? errors = null;
        while (_ownedTextureRIDs.Count > 0)
        {
            var rid = _ownedTextureRIDs[^1]; _ownedTextureRIDs.RemoveAt(_ownedTextureRIDs.Count - 1);
            var texture = RenderingTextureRegistry.OwnedResource(rid, this);
            RenderingTextureRegistry.Remove(rid);
            if (texture is DrawableTexture drawable) drawable.ServerReleased = true; else if (texture is ServerTexture ordinary) ordinary.Released = true; else if (texture is TextureLayered layered) layered.ServerReleased = true;
            try { texture.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        }
        if (errors is not null) throw new AggregateException("Texture cleanup failed.", errors);
    }
}
