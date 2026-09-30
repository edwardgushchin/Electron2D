namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<RID> _ownedTextureRIDs = [];

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
        if (texture == byTexture) return;
        target.Pixels = source.Pixels; target.Size = source.Size; target.Path = source.Path;
        FreeRID(byTexture);
    }

    /// <summary>Returns an independent image copy of a live rendering texture.</summary>
    /// <param name="texture">A caller-owned or resource-owned texture RID.</param>
    /// <returns>A caller-owned image, including checkerboard pixels for an uninitialized resource texture.</returns>
    /// <remarks>Uses original backing pixels, including the full source of an atlas view. Does not stall the GPU.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The renderer is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    public Image Texture2DGet(RID texture)
    {
        EnsureOwner();
        return (RenderingTextureRegistry.Resolve(texture).CapturePixels() ?? RenderingTextureRegistry.PlaceholderPixels).CopyImage();
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
    public Image.Format TextureGetFormat(RID texture) { EnsureOwner(); return RenderingTextureRegistry.Resolve(texture).CapturePixels()?.Source.Format ?? Image.Format.Rgba8; }

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
        target.Size = new(width, height);
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

    /// <summary>Releases a caller-owned rendering texture identity.</summary>
    /// <param name="rid">A live texture RID owned by this renderer.</param>
    /// <remarks>Resource-owned texture RIDs must be released by their resource. Retained commands stop drawing the freed texture.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The owner is different or the renderer is off-owner/submitting.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    public void FreeRID(RID rid)
    {
        EnsureTextureChange(); var texture = RenderingTextureRegistry.Owned(rid, this);
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
