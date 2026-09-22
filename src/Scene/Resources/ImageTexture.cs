namespace Electron2D;

/// <summary>A two-dimensional texture owning a snapshot of an Image's pixel data.</summary>
/// <remarks>Construction is independent of an active renderer. GPU data is uploaded when first used. Image inputs
/// and outputs are copies; modifying the original Image never changes this texture. SetImage replaces allocation
/// parameters; Update preserves them. Compressed and integer-sampled formats are not integrated yet.</remarks>
public sealed class ImageTexture : Texture
{
    private readonly object _gate = new();
    private TexturePixels? _pixels;
    private Vector2I _size;

    /// <summary>Creates an uninitialized texture with zero size and no image.</summary>
    public ImageTexture() { }

    /// <inheritdoc />
    public override int Width { get { lock (_gate) { ThrowIfDisposed(); return _size.X; } } }
    /// <inheritdoc />
    public override int Height { get { lock (_gate) { ThrowIfDisposed(); return _size.Y; } } }
    /// <inheritdoc />
    public override Vector2 Size { get { lock (_gate) { ThrowIfDisposed(); return new(_size.X, _size.Y); } } }
    /// <inheritdoc />
    public override bool IsPixelOpaque(int x, int y) { lock (_gate) { ThrowIfDisposed(); return SampleOpacity(_pixels, _size, x, y); } }

    /// <summary>Creates a texture from a copied, nonempty image.</summary>
    /// <param name="image">A live image in a supported sampling format, at most 16384 pixels per axis.</param>
    /// <returns>An independent texture; the supplied image remains caller-owned.</returns>
    /// <exception cref="ArgumentException">The image is empty or its dimensions are unsupported.</exception>
    /// <exception cref="NotSupportedException">The image requires an unimplemented texture format.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public static ImageTexture CreateFromImage(Image image)
    {
        var pixels = TexturePixels.FromImage(image);
        return new ImageTexture { _pixels = pixels, _size = new(pixels.Source.Width, pixels.Source.Height) };
    }

    /// <summary>Replaces the image and resets the logical size to its pixel dimensions.</summary>
    /// <param name="image">The live, nonempty image to copy.</param>
    /// <remarks>Validation and copying precede mutation; failure preserves the previous texture. A successful
    /// replacement emits Changed and is uploaded before a later draw.</remarks>
    /// <exception cref="ArgumentException">The image is empty or too large.</exception>
    /// <exception cref="NotSupportedException">The image format is not integrated.</exception>
    /// <exception cref="ObjectDisposedException">The texture or image is disposed.</exception>
    public void SetImage(Image image)
    {
        ThrowIfDisposed();
        var pixels = TexturePixels.FromImage(image);
        lock (_gate) { ThrowIfDisposed(); _pixels = pixels; _size = new(pixels.Source.Width, pixels.Source.Height); }
        EmitChanged();
    }

    /// <summary>Replaces pixels while retaining the original image dimensions, format and mipmap configuration.</summary>
    /// <param name="image">A live image matching the original pixel configuration.</param>
    /// <remarks>Logical size overrides do not change the required pixel dimensions. Input is copied and failure
    /// preserves the previous pixels. Compatible GPU allocation is reused, with native cycling for queued frames.</remarks>
    /// <exception cref="ArgumentException">The image configuration differs from the original.</exception>
    /// <exception cref="InvalidOperationException">The texture has not been initialized.</exception>
    /// <exception cref="NotSupportedException">The image format is not integrated.</exception>
    /// <exception cref="ObjectDisposedException">The texture or image is disposed.</exception>
    public void Update(Image image)
    {
        ThrowIfDisposed();
        var pixels = TexturePixels.FromImage(image);
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_pixels is null) throw new InvalidOperationException("Initialize the texture before updating it.");
            var old = _pixels.Source;
            var current = pixels.Source;
            if (old.Width != current.Width || old.Height != current.Height || old.Format != current.Format || old.HasMipmaps != current.HasMipmaps)
                throw new ArgumentException("Texture updates require matching pixel dimensions, format and mipmap configuration.", nameof(image));
            pixels.Allocation = _pixels.Allocation;
            _pixels = pixels;
        }
        EmitChanged();
    }

    /// <summary>Changes the logical drawing size without reallocating or resampling pixel data.</summary>
    /// <param name="size">New dimensions; zero retains that axis's current value.</param>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is negative.</exception>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public void SetSizeOverride(Vector2I size)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (size.X < 0 || size.Y < 0) throw new ArgumentOutOfRangeException(nameof(size));
            var next = new Vector2I(size.X == 0 ? _size.X : size.X, size.Y == 0 ? _size.Y : size.Y);
            if (next == _size) return;
            _size = next;
        }
        EmitChanged();
    }

    /// <inheritdoc />
    public override Image? GetImage() => CapturePixels()?.CopyImage();

    internal override TexturePixels? CapturePixels() { lock (_gate) { ThrowIfDisposed(); return _pixels; } }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new ImageTexture();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        TexturePixels? pixels;
        Vector2I size;
        lock (_gate) { ThrowIfDisposed(); pixels = _pixels; size = _size; }
        var copy = (ImageTexture)target;
        lock (copy._gate) { copy._pixels = pixels; copy._size = size; }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) { _pixels = null; _size = default; }
        base.Dispose(disposing);
    }
}
