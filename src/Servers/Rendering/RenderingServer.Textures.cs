namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<RID> _ownedTextureRIDs = [];

    /// <summary>Returns the borrowed live texture identity associated with a scene viewport.</summary>
    /// <param name="viewport">A live identity returned by Viewport.GetViewportRID.</param>
    /// <returns>The stable resource texture RID.</returns>
    /// <exception cref="ArgumentException">The identity does not resolve to a live viewport.</exception>
    /// <exception cref="InvalidOperationException">The call is off the renderer owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    public RID ViewportGetTexture(RID viewport) { EnsureOwner(); return Viewport.ResolveViewportRID(viewport).GetTexture().GetRID(); }

    /// <summary>Creates a caller-owned two-dimensional rendering texture from copied image pixels.</summary>
    /// <param name="image">A live nonempty image in a supported sampling format.</param>
    /// <returns>A stable texture RID valid until FreeRID or renderer shutdown.</returns>
    /// <exception cref="ArgumentNullException">The image is null.</exception>
    /// <exception cref="ObjectDisposedException">The image or renderer is disposed.</exception>
    /// <exception cref="ArgumentException">The image is empty or exceeds supported texture dimensions.</exception>
    /// <exception cref="NotSupportedException">The image's sampling format is not integrated.</exception>
    /// <exception cref="InvalidOperationException">The renderer is off-owner, submitting geometry or shutting down.</exception>
    public RID Texture2DCreate(Image image)
    {
        EnsureTextureChange();
        var texture = new ServerTexture(TexturePixels.FromImage(image));
        var rid = RenderingTextureRegistry.Register(texture, this);
        texture.Bind(rid); _ownedTextureRIDs.Add(rid);
        return rid;
    }

    /// <summary>Creates an owned four-by-four RGBA8 magenta/black checkerboard texture.</summary>
    /// <returns>A texture RID that can be drawn, replaced and freed like an ordinary two-dimensional texture.</returns>
    /// <exception cref="InvalidOperationException">The renderer is off-owner, submitting geometry or shutting down.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    public RID Texture2DPlaceholderCreate()
    {
        EnsureTextureChange();
        using var image = RenderingTextureRegistry.PlaceholderPixels.CopyImage();
        return Texture2DCreate(image);
    }

    /// <summary>Creates an owned alias of an existing rendering texture.</summary>
    /// <param name="baseTexture">A live resource-owned or server-owned texture RID, including another proxy.</param>
    /// <returns>A stable owned proxy RID; its source pixels remain borrowed.</returns>
    /// <remarks>Retained commands sample the current source without copying pixels or native texture storage.
    /// Freeing the source or an intermediate proxy leaves this identity alive but with no image/draw output.
    /// Freeing the proxy never frees its source. Empty source resources use their rendering placeholder.</remarks>
    /// <exception cref="ArgumentException">The source RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The renderer is off-owner, submitting or shutting down.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    public RID TextureProxyCreate(RID baseTexture)
    {
        EnsureTextureChange();
        var source = RenderingTextureRegistry.Resolve(baseTexture);
        var texture = new ServerTexture(baseTexture, source);
        var rid = RenderingTextureRegistry.Register(texture, this);
        texture.Bind(rid); _ownedTextureRIDs.Add(rid);
        return rid;
    }

    /// <summary>Retargets an owned proxy to a live non-proxy texture without changing its identity.</summary>
    /// <param name="texture">A live proxy RID owned by this renderer.</param>
    /// <param name="proxyTo">A live non-proxy resource-owned or server-owned source RID.</param>
    /// <remarks>Validation precedes publication. Current pixels, logical size, format and diagnostic path follow
    /// the new source; existing retained destination geometry stays fixed. Cyclic/proxy targets are rejected.</remarks>
    /// <exception cref="ArgumentException">A RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The destination is not an owned proxy, the source is a proxy,
    /// or the renderer is off-owner, submitting or shutting down.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    public void TextureProxyUpdate(RID texture, RID proxyTo)
    {
        EnsureTextureChange();
        var target = RenderingTextureRegistry.Owned(texture, this);
        var source = RenderingTextureRegistry.Resolve(proxyTo);
        if (!target.IsProxy || source is ServerTexture { IsProxy: true })
            throw new InvalidOperationException("Proxy updates require an owned proxy and a non-proxy source.");
        target.SetProxyTarget(proxyTo, source);
    }

    /// <summary>Transfers an owned texture's pixels and metadata into another stable owned identity.</summary>
    /// <param name="texture">The destination RID whose identity and retained draw references survive.</param>
    /// <param name="byTexture">The replacement RID, consumed after the transfer.</param>
    /// <remarks>Both RIDs must belong to this renderer. Replacing a texture with itself does nothing.
    /// Different dimensions, formats and mipmap state are allowed; old recorded geometry remains fixed.</remarks>
    /// <exception cref="ArgumentException">A RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">A RID is resource-owned or the renderer is off-owner/submitting.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    public void TextureReplace(RID texture, RID byTexture)
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
        FreeRID(byTexture);
    }

    /// <summary>Returns an independent image copy of a live rendering texture.</summary>
    /// <param name="texture">A caller-owned or resource-owned texture RID.</param>
    /// <returns>A caller-owned image, including checkerboard pixels for an uninitialized resource texture;
    /// null for a live proxy whose source has been released.</returns>
    /// <remarks>Uses original backing pixels, including the full source of an atlas view. Does not stall the GPU.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The renderer is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    public Image? Texture2DGet(RID texture)
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

    /// <summary>Updates an owned two-dimensional texture while preserving its allocation parameters.</summary>
    /// <param name="texture">A live texture RID owned by this rendering server.</param>
    /// <param name="image">A live image with matching dimensions, format and mipmap state.</param>
    /// <param name="layer">Zero for the current two-dimensional texture profile.</param>
    /// <remarks>Input is copied before commit; subsequent retained draws see the new pixels.</remarks>
    /// <exception cref="ArgumentException">The RID or image configuration is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The layer is not zero.</exception>
    /// <exception cref="NotSupportedException">The image requires an unsupported format.</exception>
    /// <exception cref="ArgumentNullException">The image is null.</exception>
    /// <exception cref="ObjectDisposedException">The image or renderer is disposed.</exception>
    /// <exception cref="InvalidOperationException">The RID is resource-owned, or the renderer is off-owner/submitting.</exception>
    public void Texture2DUpdate(RID texture, Image image, int layer = 0)
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

    /// <summary>Returns a texture's source pixel format.</summary>
    /// <param name="texture">A live caller-owned or resource-owned texture RID.</param>
    /// <returns>The backing format, or RGBA8 for an uninitialized resource's rendering placeholder.</returns>
    /// <exception cref="ArgumentException">The RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The renderer is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    public Image.Format TextureGetFormat(RID texture)
    {
        EnsureOwner(); var source = RenderingTextureRegistry.Resolve(texture);
        return source is ServerTexture { IsProxy: true } proxy ? proxy.PixelFormat : source.CapturePixels()?.Source.Format ?? Image.Format.Rgba8;
    }

    /// <summary>Changes an owned texture's logical drawing size without resampling pixels.</summary>
    /// <param name="texture">A live server-owned texture RID.</param>
    /// <param name="width">Logical width from one through 16384 pixels.</param>
    /// <param name="height">Logical height from one through 16384 pixels.</param>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is outside the supported range.</exception>
    /// <exception cref="ArgumentException">The RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The texture is resource-owned or the renderer is off-owner/submitting.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    public void TextureSetSizeOverride(RID texture, int width, int height)
    {
        EnsureTextureChange();
        if (width is < 1 or > 16384) throw new ArgumentOutOfRangeException(nameof(width));
        if (height is < 1 or > 16384) throw new ArgumentOutOfRangeException(nameof(height));
        var target = RenderingTextureRegistry.Owned(texture, this);
        target.SetDisplaySize(new(width, height));
    }

    /// <summary>Sets diagnostic path metadata for an owned rendering texture.</summary>
    /// <param name="texture">A live server-owned texture RID.</param>
    /// <param name="path">Diagnostic path; it does not load an image.</param>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="ArgumentException">The RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The texture is resource-owned or the renderer is off-owner/submitting.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    public void TextureSetPath(RID texture, string path)
    {
        EnsureTextureChange(); ArgumentNullException.ThrowIfNull(path);
        RenderingTextureRegistry.Owned(texture, this).Path = path;
    }

    /// <summary>Returns texture path metadata.</summary>
    /// <param name="texture">A live owned or resource-owned texture RID.</param>
    /// <returns>Owned diagnostic path, or the resource's existing ResourcePath.</returns>
    /// <exception cref="ArgumentException">The RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The renderer is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    public string TextureGetPath(RID texture)
    {
        EnsureOwner(); var target = RenderingTextureRegistry.Resolve(texture);
        return target is ServerTexture owned ? owned.Path : target.ResourcePath;
    }

    /// <summary>Releases a caller-owned rendering texture or mesh identity.</summary>
    /// <param name="rid">A live texture or mesh RID owned by this renderer.</param>
    /// <remarks>Resource-owned identities must be released by their resource. Retained commands stop drawing freed
    /// server textures; drawing a disposed mesh reports the borrowed-resource lifetime error.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or not a supported rendering resource.</exception>
    /// <exception cref="InvalidOperationException">The owner is different or the renderer is off-owner/submitting.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    public void FreeRID(RID rid)
    {
        EnsureTextureChange(); if (RenderingMultiMeshRegistry.Contains(rid)) { var resource = RenderingMultiMeshRegistry.Owned(rid, this); _ownedMultiMeshRIDs.Remove(rid); RenderingMultiMeshRegistry.Remove(rid); resource.Dispose(); return; }
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
