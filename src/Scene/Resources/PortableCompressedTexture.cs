namespace Electron2D;

/// <summary>A texture created at runtime from a portable compressed image payload.</summary>
/// <remarks>Creation prepares all mip levels before publication. Drawing borrows immutable pixels;
/// image reads return independent copies. Retaining encoded buffers is optional and required for saving
/// populated resources. Duplication remains available without a retained encoded buffer.</remarks>
public sealed class PortableCompressedTexture : Texture
{
    /// <summary>The portable image storage encoding.</summary>
    public enum CompressionMode
    {
        /// <summary>Lossless WebP, with PNG for dimensions outside WebP's range.</summary>
        Lossless = 0,
        /// <summary>Lossy WebP with preserved alpha.</summary>
        Lossy = 1,
        /// <summary>UASTC LDR or RGB HDR in a portable KTX2 container.</summary>
        BasisUniversal = 2,
        /// <summary>BC1, BC3, BC4 or BC5 blocks selected from image channels.</summary>
        S3TC = 3,
        /// <summary>ETC2 or EAC blocks selected from image channels.</summary>
        ETC2 = 4,
        /// <summary>BC7 LDR or BC6H HDR blocks.</summary>
        BPTC = 5,
        /// <summary>ASTC 4 by 4 blocks with LDR or HDR channels.</summary>
        ASTC = 6,
    }
    private readonly object _gate = new();
    private static bool _keepAll;
    private TexturePixels? _pixels;
    private byte[] _encoded = [];
    private CompressionMode _mode;
    private Vector2 _sizeOverride;
    private bool _keep, _hasAlpha;
    private int _basisLevel = 2;
    private float _basisRDO;

    /// <summary>Creates an empty resource with no image or encoded payload.</summary>
    public PortableCompressedTexture() { }

    /// <summary>Gets or changes whether future creations retain their encoded payload.</summary>
    /// <value>False by default. Setting false clears the current buffer, including a globally retained buffer.
    /// Setting true cannot recover a discarded buffer.</value>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public bool KeepCompressedBuffer
    {
        get { lock (_gate) { ThrowIfDisposed(); return _keep; } }
        set { lock (_gate) { ThrowIfDisposed(); _keep = value; if (!value) _encoded = []; } }
    }
    /// <summary>Gets or changes rendering pixel dimensions used for source regions and tiling.</summary>
    /// <value>Finite dimensions from zero through 16384; zero uses the source dimension. Fractions are
    /// truncated for rendering. Creation resets this value to the image size. Image and width/height queries
    /// retain the original dimensions.</value>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is negative, nonfinite or exceeds 16384.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Vector2 SizeOverride
    {
        get { lock (_gate) { ThrowIfDisposed(); return _sizeOverride; } }
        set
        {
            if (!value.IsFinite() || value.X < 0 || value.Y < 0 || value.X > 16384 || value.Y > 16384) throw new ArgumentOutOfRangeException(nameof(value));
            lock (_gate) { ThrowIfDisposed(); if (_sizeOverride == value) return; _sizeOverride = value; }
            EmitChanged();
        }
    }
    /// <summary>Reports the process-wide policy for retaining buffers on subsequent creations and loads.</summary>
    /// <returns>False by default.</returns>
    public static bool IsKeepingAllCompressedBuffers() => Volatile.Read(ref _keepAll);
    /// <summary>Changes buffer retention for subsequent creations and loads.</summary>
    /// <param name="keep">Whether to retain newly created payloads regardless of their resource policy.</param>
    /// <remarks>Existing resources are not visited or changed.</remarks>
    public static void SetKeepAllCompressedBuffers(bool keep) => Volatile.Write(ref _keepAll, keep);
    /// <summary>Changes Basis encoder settings used by the next creation.</summary>
    /// <param name="uastcLevel">Encoder effort from zero through four, or -1 for the default level two.</param>
    /// <param name="rdoQualityLoss">LDR rate-distortion loss from zero through fifty, or -1 for the default zero.
    /// Zero disables rate-distortion optimization. HDR uses the effort setting and does not use LDR RDO.</param>
    /// <exception cref="ArgumentOutOfRangeException">A setting is nonfinite or outside its supported range.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetBASISUCompressorParams(int uastcLevel, float rdoQualityLoss)
    {
        if (uastcLevel is < -1 or > 4) throw new ArgumentOutOfRangeException(nameof(uastcLevel));
        if (!float.IsFinite(rdoQualityLoss) || rdoQualityLoss != -1 && (rdoQualityLoss < 0 || rdoQualityLoss > 50)) throw new ArgumentOutOfRangeException(nameof(rdoQualityLoss));
        lock (_gate) { ThrowIfDisposed(); _basisLevel = uastcLevel == -1 ? 2 : uastcLevel; _basisRDO = rdoQualityLoss == -1 ? 0 : rdoQualityLoss; }
    }
    /// <summary>Encodes a copied image, then atomically replaces this texture's pixels and storage mode.</summary>
    /// <param name="image">A live nonempty image, no more than 16384 pixels per axis.</param>
    /// <param name="compressionMode">The complete portable encoding mode.</param>
    /// <param name="normalMap">Whether Basis should optimize linear normal channels.</param>
    /// <param name="lossyQuality">Finite WebP quality from zero through one.</param>
    /// <remarks>All mip levels are retained. Matching precompressed block images are accepted without
    /// recompression; other compressed inputs are rejected for block/Basis encoding. Lossless/lossy encoding
    /// uses RGBA8 precision. Basis HDR stores RGB, with opaque alpha. Failures before publication preserve
    /// pixels, size, mode and RID; notifications run outside the state lock after publication.</remarks>
    /// <exception cref="ArgumentException">The image is empty or its compressed format does not match.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Mode or quality is invalid.</exception>
    /// <exception cref="InvalidDataException">Pixels or encoded data exceed the bounded image profile.</exception>
    /// <exception cref="InvalidOperationException">Codec work exceeds its memory budget.</exception>
    /// <exception cref="IOException">A codec cannot allocate or encode the image.</exception>
    /// <exception cref="ObjectDisposedException">This texture or the image is disposed.</exception>
    /// <exception cref="Exception">A notification subscriber throws after publication.</exception>
    public void CreateFromImage(Image image, CompressionMode compressionMode, bool normalMap = false, float lossyQuality = 0.8f)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (!Enum.IsDefined(compressionMode)) throw new ArgumentOutOfRangeException(nameof(compressionMode));
        if (!float.IsFinite(lossyQuality) || lossyQuality < 0 || lossyQuality > 1) throw new ArgumentOutOfRangeException(nameof(lossyQuality));
        int level; float rdo; lock (_gate) { ThrowIfDisposed(); level = _basisLevel; rdo = _basisRDO; }
        var result = PortableTextureData.Encode(image, compressionMode, normalMap, lossyQuality, level, rdo);
        Publish(result.Pixels, compressionMode, result.Data, result.HasAlpha);
    }
    /// <summary>Gets the last successful creation's storage mode.</summary>
    /// <returns>Lossless before creation.</returns>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public CompressionMode GetCompressionMode() { lock (_gate) { ThrowIfDisposed(); return _mode; } }
    private void Publish(TexturePixels pixels, CompressionMode mode, byte[] encoded, bool hasAlpha)
    {
        lock (_gate) { ThrowIfDisposed(); _pixels = pixels; _mode = mode; _hasAlpha = hasAlpha; _sizeOverride = new(pixels.Source.Width, pixels.Source.Height); _encoded = _keep || IsKeepingAllCompressedBuffers() ? encoded : []; }
        NotifyPropertyListChanged(); EmitChanged();
    }
    /// <inheritdoc />
    public override int GetWidth() { lock (_gate) { ThrowIfDisposed(); return _pixels?.Source.Width ?? 0; } }
    /// <inheritdoc />
    public override int GetHeight() { lock (_gate) { ThrowIfDisposed(); return _pixels?.Source.Height ?? 0; } }
    /// <inheritdoc />
    public override Vector2 GetSize() { lock (_gate) { ThrowIfDisposed(); return new(_pixels?.Source.Width ?? 0, _pixels?.Source.Height ?? 0); } }
    /// <inheritdoc />
    public override Image? GetImage() { lock (_gate) { ThrowIfDisposed(); return _pixels?.CopyImage(); } }
    /// <summary>Reports whether the encoded source format is LA8 or RGBA8.</summary>
    /// <value>False for block formats, RGB-only Basis sources and an empty texture; opacity queries still inspect decoded pixels.</value>
    public override bool HasAlpha { get { lock (_gate) { ThrowIfDisposed(); return _hasAlpha; } } }
    /// <inheritdoc />
    public override bool IsPixelOpaque(int x, int y) { lock (_gate) { ThrowIfDisposed(); return SampleOpacity(_pixels, new(_pixels?.Source.Width ?? 0, _pixels?.Source.Height ?? 0), x, y); } }
    internal override TexturePixels? CapturePixels() { lock (_gate) { ThrowIfDisposed(); return _pixels; } }
    internal override Vector2 DrawingSize { get { lock (_gate) { ThrowIfDisposed(); return _pixels is { } pixels ? new((int)_sizeOverride.X > 0 ? (int)_sizeOverride.X : pixels.Source.Width, (int)_sizeOverride.Y > 0 ? (int)_sizeOverride.Y : pixels.Source.Height) : Vector2.Zero; } } }
    private byte[] StoredData()
    {
        lock (_gate) { ThrowIfDisposed(); if (_pixels is not null && _encoded.Length == 0) throw new InvalidOperationException("Saving a populated portable texture requires a retained compressed buffer. Enable retention before creating it."); return (byte[])_encoded.Clone(); }
    }
    private void Restore(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data); ThrowIfDisposed();
        if (data.Length == 0) { lock (_gate) { ThrowIfDisposed(); _pixels = null; _encoded = []; _hasAlpha = false; _mode = default; _sizeOverride = default; } return; }
        var result = PortableTextureData.Decode(data); Publish(result.Pixels, result.Mode, (byte[])data.Clone(), result.HasAlpha);
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<PortableCompressedTexture, bool>(nameof(KeepCompressedBuffer), p => p.KeepCompressedBuffer, (p, v) => p.KeepCompressedBuffer = v, _ => false, stored: true),
        new PropertyDescriptor<PortableCompressedTexture, byte[]>("portable_data", p => p.StoredData(), (p, v) => p.Restore(v), _ => [], stored: true),
        new PropertyDescriptor<PortableCompressedTexture, Vector2>(nameof(SizeOverride), p => p.SizeOverride, (p, v) => p.SizeOverride = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<PortableCompressedTexture, int>("basis_level", p => p._basisLevel, (p, v) => p.SetBASISUCompressorParams(v, p._basisRDO), _ => 2, stored: true),
        new PropertyDescriptor<PortableCompressedTexture, float>("basis_rdo", p => p._basisRDO, (p, v) => p.SetBASISUCompressorParams(p._basisLevel, v), _ => 0, stored: true),
    ]);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new PortableCompressedTexture();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (PortableCompressedTexture)target;
        lock (_gate) { ThrowIfDisposed(); copy._pixels = _pixels?.CopySnapshot(); copy._encoded = (byte[])_encoded.Clone(); copy._mode = _mode; copy._hasAlpha = _hasAlpha; copy._sizeOverride = _sizeOverride; copy._keep = _keep; copy._basisLevel = _basisLevel; copy._basisRDO = _basisRDO; }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) lock (_gate) { _pixels = null; _encoded = []; } base.Dispose(disposing); }
}
