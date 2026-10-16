namespace Electron2D;

/// <summary>Loads a copied image and logical drawing size from an imported texture asset.</summary>
/// <remarks>Loading prepares every stored mip before publication. Image reads retain the original format
/// and bytes. Rendering owns native storage; replacing the file retains this resource's RID while borrowed
/// device storage may expire. Inputs and source/upload snapshots use the bounded image storage profile.
/// UInt16 sources preserve original bytes and use raw unsigned GPU sampling; nearest filtering is required.
/// Compatibility rejects integer drawing without changing the loaded resource.</remarks>
public sealed class CompressedTexture : Texture
{
    private readonly object _gate = new();
    private TexturePixels? _pixels;
    private Vector2i _size;
    private string _loadPath = "";
    private string _renderPath = "";

    /// <summary>Creates an uninitialized texture with zero logical size and no source image.</summary>
    public CompressedTexture() { }

    /// <summary>Gets the last successfully loaded asset path, or loads a replacement asset.</summary>
    /// <value>Empty before loading. Assignment performs the same operation as <see cref="Load"/>.</value>
    /// <exception cref="ArgumentException">The path is empty.</exception>
    /// <exception cref="InvalidDataException">The asset is malformed or exceeds the image storage limits.</exception>
    /// <exception cref="IOException">The source cannot be read or a decoder cannot allocate working memory.</exception>
    /// <exception cref="InvalidOperationException">Block expansion exceeds its codec work budget.</exception>
    /// <exception cref="NotSupportedException">The source sampling format is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">This texture is disposed.</exception>
    /// <exception cref="Exception">A subscriber throws after publication.</exception>
    public string LoadPath { get { lock (_gate) { ThrowIfDisposed(); return _loadPath; } } set => Load(value); }

    /// <summary>Atomically loads an imported image texture, retaining its logical size and complete mip chain.</summary>
    /// <param name="path">An operating-system, res:// or user:// asset path.</param>
    /// <remarks>Accepts raw, PNG/WebP mip payloads and Basis LDR/HDR. Logical dimensions can differ from
    /// stored image dimensions. Failure before publication preserves pixels, size, path and RID.
    /// PropertyListChanged precedes Changed outside the state lock; subscriber failure retains new state.
    /// Source bytes are bounded to sixty-four MiB; copied source/upload storage is bounded to 256 MiB.
    /// Native format limits are checked before subsequent drawing.</remarks>
    /// <exception cref="ArgumentException">The path is empty.</exception>
    /// <exception cref="InvalidDataException">The asset is malformed or exceeds image storage limits.</exception>
    /// <exception cref="IOException">The source cannot be read or a decoder cannot allocate working memory.</exception>
    /// <exception cref="InvalidOperationException">Block expansion exceeds its codec work budget.</exception>
    /// <exception cref="NotSupportedException">The source sampling format is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">This texture is disposed.</exception>
    /// <exception cref="Exception">A subscriber throws after publication.</exception>
    public void Load(string path)
    {
        ThrowIfDisposed(); ArgumentException.ThrowIfNullOrEmpty(path);
        var state = CompressedTextureSource.ReadTexture(path); var visiblePath = ResourcePath;
        lock (_gate) { ThrowIfDisposed(); _pixels = state.Pixels; _size = state.Size; _loadPath = path; _renderPath = visiblePath.Length == 0 ? path : visiblePath; }
        NotifyPropertyListChanged(); EmitChanged();
    }

    /// <inheritdoc />
    public override int GetWidth() { lock (_gate) { ThrowIfDisposed(); return _size.X; } }
    /// <inheritdoc />
    public override int GetHeight() { lock (_gate) { ThrowIfDisposed(); return _size.Y; } }
    /// <inheritdoc />
    public override Vector2 GetSize() { lock (_gate) { ThrowIfDisposed(); return new(_size.X, _size.Y); } }
    /// <summary>Gets the alpha-reporting flag for this imported texture resource.</summary>
    /// <value>False, including for stored alpha-bearing images. Per-pixel opacity remains available.</value>
    /// <exception cref="ObjectDisposedException">This texture is disposed.</exception>
    public override bool HasAlpha { get { ThrowIfDisposed(); return false; } }
    /// <inheritdoc />
    public override Image? GetImage() { lock (_gate) { ThrowIfDisposed(); return _pixels?.CopyImage(); } }
    /// <inheritdoc />
    public override bool IsPixelOpaque(int x, int y) { lock (_gate) { ThrowIfDisposed(); return SampleOpacity(_pixels, _size, x, y); } }

    internal override TexturePixels? CapturePixels() { lock (_gate) { ThrowIfDisposed(); return _pixels; } }
    internal string RenderingPath { get { lock (_gate) { ThrowIfDisposed(); return _renderPath; } } }
    internal (TexturePixels? Pixels, Vector2i Size, string Path) CaptureFileState() { lock (_gate) { ThrowIfDisposed(); return (_pixels, _size, _loadPath); } }
    internal void RestoreFileState(TexturePixels? pixels, Vector2i size, string path)
    {
        if (pixels is null ? size != Vector2i.Zero || path.Length != 0 : size.X <= 0 || size.Y <= 0)
            throw new InvalidDataException("Stored imported texture pixels, size and path are inconsistent.");
        var visiblePath = ResourcePath;
        lock (_gate) { ThrowIfDisposed(); _pixels = pixels; _size = size; _loadPath = path; _renderPath = visiblePath.Length == 0 ? path : visiblePath; }
    }
    /// <inheritdoc />
    protected override void OnResourcePathChanged(string path) { lock (_gate) _renderPath = path; base.OnResourcePathChanged(path); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<CompressedTexture, string>(nameof(LoadPath), texture => texture.LoadPath, (texture, value) => texture.LoadPath = value, _ => "", stored: true)]);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CompressedTexture();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var state = CaptureFileState(); var copy = (CompressedTexture)target;
        copy.RestoreFileState(state.Pixels, state.Size, state.Path); copy.NotifyPropertyListChanged();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) { _pixels = null; _size = default; _loadPath = ""; _renderPath = ""; }
        base.Dispose(disposing);
    }
}
