namespace Electron2D;

/// <summary>Describes a two-dimensional texture with a logical size and readable image data.</summary>
/// <remarks>Custom implementations provide GetWidth, GetHeight and GetImage, and emit Changed when their pixels change.
/// The renderer caches a copied image until Changed. Consumers borrow textures; GPU resources belong to the renderer.</remarks>
public abstract class Texture : Resource
{
    private readonly object _snapshotGate = new();
    private TexturePixels? _snapshot;

    /// <summary>Initializes the texture's pixel-cache invalidation.</summary>
    protected Texture() { Changed += InvalidatePixels; }

    /// <summary>Gets the logical width used for drawing.</summary>
    /// <returns>Width in pixels, as defined by the concrete texture.</returns>
    public abstract int GetWidth();
    /// <summary>Gets the logical height used for drawing.</summary>
    /// <returns>Height in pixels, as defined by the concrete texture.</returns>
    public abstract int GetHeight();
    /// <summary>Gets the logical drawing size.</summary>
    /// <returns>The width and height as a floating-point vector.</returns>
    public virtual Vector2 GetSize() => new(GetWidth(), GetHeight());
    /// <summary>Gets the original image's pixel format.</summary>
    /// <value>L8 for an uninitialized texture.</value>
    public virtual Image.Format PixelFormat => CapturePixels()?.Source.Format ?? Image.Format.L8;
    /// <summary>Gets whether the original format contains an alpha channel.</summary>
    /// <value>False for formats without alpha or for an uninitialized texture.</value>
    public virtual bool HasAlpha => CapturePixels() is { } pixels && Image.TextureHasAlpha(pixels.Source.Format);
    /// <summary>Gets whether the texture contains a mipmap chain.</summary>
    /// <value>False for an uninitialized texture.</value>
    public virtual bool HasMipmaps => CapturePixels()?.Source.HasMipmaps ?? false;
    /// <summary>Gets the number of mip levels after the base image.</summary>
    /// <value>Zero when no mipmaps are stored.</value>
    public virtual int MipmapCount => (CapturePixels()?.Levels ?? 1) - 1;

    /// <summary>Returns a caller-owned copy of the texture's original image.</summary>
    /// <returns>An independent image, or null when the texture has no readable pixels.</returns>
    /// <remarks>The base implementation represents an uninitialized texture. A renderable custom implementation
    /// must return readable image data and notify Changed when that data changes.</remarks>
    public virtual Image? GetImage() { ThrowIfDisposed(); return null; }

    /// <summary>Draws this texture at its logical size during the target item's canvas recording.</summary>
    /// <param name="canvasItem">The node recording the command; it borrows this texture.</param>
    /// <param name="position">The finite local top-left position.</param>
    /// <param name="modulate">The finite color multiplier, or null for white.</param>
    /// <param name="transpose">Whether to exchange the texture axes and drawing dimensions.</param>
    /// <exception cref="ArgumentException">Geometry or modulation is not finite.</exception>
    /// <exception cref="ArgumentNullException">The target node is null.</exception>
    /// <exception cref="InvalidOperationException">The target is not recording canvas commands on its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The texture or target node is disposed.</exception>
    public virtual void Draw(CanvasItem canvasItem, Vector2 position, Color? modulate = null, bool transpose = false)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(canvasItem);
        canvasItem.RecordTexture(this, new Rect(position, GetSize()), null, modulate ?? Colors.White, false, transpose, false);
    }

    /// <summary>Stretches or tiles this texture over a local rectangle during canvas recording.</summary>
    /// <param name="canvasItem">The node recording the command; it borrows this texture.</param>
    /// <param name="rect">The finite destination. Negative dimensions flip the image without moving its origin.</param>
    /// <param name="tile">Whether to repeat at logical pixel size instead of stretching.</param>
    /// <param name="modulate">The finite color multiplier, or null for white.</param>
    /// <param name="transpose">Whether to exchange texture axes and destination dimensions.</param>
    /// <remarks>A zero-area destination or uninitialized texture records no geometry. Pixel changes are visible
    /// without recording this command again. The node does not own or dispose this resource.</remarks>
    /// <exception cref="ArgumentException">Geometry or modulation is not finite.</exception>
    /// <exception cref="ArgumentNullException">The target node is null.</exception>
    /// <exception cref="InvalidOperationException">The target is not recording canvas commands on its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The texture or target node is disposed.</exception>
    public virtual void DrawRect(CanvasItem canvasItem, Rect rect, bool tile, Color? modulate = null, bool transpose = false)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(canvasItem);
        canvasItem.RecordTexture(this, rect, null, modulate ?? Colors.White, tile, transpose, false);
    }

    /// <summary>Stretches a source region over a local rectangle during canvas recording.</summary>
    /// <param name="canvasItem">The node recording the command; it borrows this texture.</param>
    /// <param name="rect">The finite destination. Negative dimensions flip without moving the origin.</param>
    /// <param name="sourceRect">The finite source in logical texture pixels. Negative dimensions toggle the corresponding flip.</param>
    /// <param name="modulate">The finite color multiplier, or null for white.</param>
    /// <param name="transpose">Whether to exchange texture axes and destination dimensions.</param>
    /// <param name="clipUV">Whether to constrain sampling to texel centers inside the source region.</param>
    /// <remarks>Sampling outside the full texture clamps to its edges. Zero-area regions draw nothing.</remarks>
    /// <exception cref="ArgumentException">Geometry or modulation is not finite.</exception>
    /// <exception cref="ArgumentNullException">The target node is null.</exception>
    /// <exception cref="InvalidOperationException">The target is not recording canvas commands on its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The texture or target node is disposed.</exception>
    public virtual void DrawRectRegion(CanvasItem canvasItem, Rect rect, Rect sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(canvasItem);
        canvasItem.RecordTexture(this, rect, sourceRect, modulate ?? Colors.White, false, transpose, clipUV);
    }

    /// <summary>Tests alpha at a logical pixel coordinate, clamped to the texture edges.</summary>
    /// <param name="x">The logical horizontal coordinate.</param>
    /// <param name="y">The logical vertical coordinate.</param>
    /// <returns>True when alpha exceeds 0.1, or when the texture has no image.</returns>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public virtual bool IsPixelOpaque(int x, int y)
    {
        var pixels = CapturePixels();
        return SampleOpacity(pixels, new Vector2i(GetWidth(), GetHeight()), x, y);
    }

    private protected static bool SampleOpacity(TexturePixels? pixels, Vector2i size, int x, int y)
    {
        if (pixels is null || size.X <= 0 || size.Y <= 0) return true;
        x = (int)Math.Clamp((long)x * pixels.Source.Width / size.X, 0, pixels.Source.Width - 1);
        y = (int)Math.Clamp((long)y * pixels.Source.Height / size.Y, 0, pixels.Source.Height - 1);
        return Image.ReadTexturePixel(pixels.Source, x, y).A > 0.1f;
    }

    private void InvalidatePixels(Resource _) { lock (_snapshotGate) _snapshot = null; }

    internal virtual TexturePixels? CapturePixels()
    {
        lock (_snapshotGate)
        {
            ThrowIfDisposed();
            if (_snapshot is not null) return _snapshot;
            using var image = GetImage();
            return _snapshot = image is null ? null : TexturePixels.FromImage(image);
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<Texture, Image.Format>(nameof(PixelFormat), t => t.PixelFormat),
        new PropertyDescriptor<Texture, bool>(nameof(HasAlpha), t => t.HasAlpha),
        new PropertyDescriptor<Texture, bool>(nameof(HasMipmaps), t => t.HasMipmaps),
        new PropertyDescriptor<Texture, int>(nameof(MipmapCount), t => t.MipmapCount),
    ]);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) { Changed -= InvalidatePixels; lock (_snapshotGate) _snapshot = null; }
        base.Dispose(disposing);
    }
}

internal sealed class TexturePixels
{
    internal readonly Image.State Source;
    internal readonly Image.State Upload;
    internal readonly int Levels;
    internal object Allocation = new();
    internal int BytesPerPixel => Upload.Format == Image.Format.Rgba8 ? 4 : 16;

    private TexturePixels(Image.State source)
    {
        if (source.Width is <= 0 or > 16384 || source.Height is <= 0 or > 16384 || source.Data.Length == 0)
            throw new ArgumentException("A texture requires a nonempty image no larger than 16384 pixels per axis.");
        if (source.Format is >= Image.Format.Dxt1 and <= Image.Format.Astc8X8Hdr ||
            source.Format is Image.Format.R16I or Image.Format.Rg16I or Image.Format.Rgb16I or Image.Format.Rgba16I)
            throw new NotSupportedException("Compressed and integer-sampled texture formats are not integrated yet.");
        Source = source;
        // Preserve high-precision/HDR values; 8-bit formats can use the portable RGBA8 sampling path.
        var format = source.Format is Image.Format.L8 or Image.Format.La8 or Image.Format.R8 or Image.Format.Rg8 or Image.Format.Rgb8 or Image.Format.Rgba8
            ? Image.Format.Rgba8 : Image.Format.Rgbaf;
        Upload = Image.ConvertPixels(source, format);
        Levels = 1;
        if (source.HasMipmaps)
            for (int width = source.Width, height = source.Height; width > 1 || height > 1; width = Math.Max(1, width / 2), height = Math.Max(1, height / 2)) Levels++;
    }

    internal static TexturePixels FromImage(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        return new(image.CopyPixels());
    }

    internal Image CopyImage() => Image.CreateFromData(Source.Width, Source.Height, Source.HasMipmaps, Source.Format, Source.Data);
}
