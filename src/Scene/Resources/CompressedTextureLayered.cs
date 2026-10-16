namespace Electron2D;

/// <summary>Loads copied homogeneous image layers from a compressed array asset.</summary>
/// <remarks>Loading validates every layer before publication. Sources are bounded to sixty-four MiB;
/// source and prepared sampling pixels share the existing layered storage budget. Rendering owns native
/// allocations. Compatible file replacement retains this resource's RID, while borrowed device storage may expire.</remarks>
public abstract class CompressedTextureLayered : TextureLayered
{
    private readonly object _gate = new();
    private LayeredTexturePixels? _pixels;
    private string _loadPath = "";
    private string _renderPath = "";

    /// <summary>Initializes an empty file-backed image-array resource.</summary>
    protected CompressedTextureLayered() { }

    /// <summary>Gets the last successfully loaded asset path, or loads a replacement asset.</summary>
    /// <value>Empty until the first successful load. An assignment performs the same operation as Load.</value>
    /// <exception cref="ArgumentException">The path is empty.</exception>
    /// <exception cref="IOException">The asset cannot be read or decoded.</exception>
    /// <exception cref="InvalidDataException">The source is malformed or exceeds image limits.</exception>
    /// <exception cref="Exception">A subscriber throws after publication.</exception>
    /// <exception cref="NotSupportedException">The asset role or pixel layout is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public string LoadPath { get { lock (_gate) { ThrowIfDisposed(); return _loadPath; } } set => Load(value); }

    /// <summary>Atomically loads a complete independent image-array asset.</summary>
    /// <param name="path">Operating-system, res:// or user:// asset path.</param>
    /// <remarks>Supports raw, PNG/WebP mip payloads and Basis LDR/HDR. Every layer must agree in size,
    /// source format and complete mip state. Failure before publication preserves pixels, path and RID.
    /// PropertyListChanged precedes Changed after publication; subscriber failure retains the new data.</remarks>
    /// <exception cref="ArgumentException">The path is empty.</exception>
    /// <exception cref="InvalidDataException">The container is malformed, heterogeneous or exceeds its bounds.</exception>
    /// <exception cref="IOException">The source cannot be read or the decoder cannot allocate working memory.</exception>
    /// <exception cref="NotSupportedException">The layered role or required pixel decoder is unsupported.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    /// <exception cref="Exception">A subscriber throws after publication.</exception>
    public void Load(string path)
    {
        ThrowIfDisposed(); ArgumentException.ThrowIfNullOrEmpty(path);
        var pixels = CompressedArraySource.Read(path);
        var visiblePath = ResourcePath;
        lock (_gate) { ThrowIfDisposed(); _pixels = pixels; _loadPath = path; _renderPath = visiblePath.Length == 0 ? path : visiblePath; }
        NotifyPropertyListChanged(); EmitChanged();
    }

    /// <inheritdoc />
    public override Image.Format GetFormat() { lock (_gate) { ThrowIfDisposed(); return _pixels?.First.Source.Format ?? Image.Format.L8; } }
    /// <inheritdoc />
    public override int GetWidth() { lock (_gate) { ThrowIfDisposed(); return _pixels?.First.Source.Width ?? 0; } }
    /// <inheritdoc />
    public override int GetHeight() { lock (_gate) { ThrowIfDisposed(); return _pixels?.First.Source.Height ?? 0; } }
    /// <inheritdoc />
    public override int GetLayers() { lock (_gate) { ThrowIfDisposed(); return _pixels?.Layers.Length ?? 0; } }
    /// <inheritdoc />
    public override bool HasMIPMaps() { lock (_gate) { ThrowIfDisposed(); return _pixels?.First.Source.HasMIPMaps ?? false; } }
    /// <inheritdoc />
    public override Image? GetLayerData(int layerIndex)
    {
        lock (_gate) { ThrowIfDisposed(); if ((uint)layerIndex >= (uint)(_pixels?.Layers.Length ?? 0)) throw new ArgumentOutOfRangeException(nameof(layerIndex)); return _pixels!.Layers[layerIndex].CopyImage(); }
    }

    internal override LayeredTexturePixels? CaptureLayers() { lock (_gate) { ThrowIfDisposed(); return _pixels; } }
    internal string RenderingPath { get { lock (_gate) { ThrowIfDisposed(); return _renderPath; } } }
    internal (LayeredTexturePixels? Pixels, string Path) CaptureFileState() { lock (_gate) { ThrowIfDisposed(); return (_pixels, _loadPath); } }
    internal void RestoreFileState(LayeredTexturePixels? pixels, string path)
    {
        var visiblePath = ResourcePath;
        if (pixels is null && path.Length != 0) throw new InvalidDataException("An uninitialized compressed array cannot have a loaded path.");
        lock (_gate) { ThrowIfDisposed(); _pixels = pixels; _loadPath = path; _renderPath = visiblePath.Length == 0 ? path : visiblePath; }
    }
    /// <inheritdoc />
    protected override void OnResourcePathChanged(string path) { lock (_gate) _renderPath = path; base.OnResourcePathChanged(path); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<CompressedTextureLayered, string>(nameof(LoadPath), texture => texture.LoadPath, (texture, value) => texture.LoadPath = value, _ => "", stored: true)]);
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var state = CaptureFileState(); var copy = (CompressedTextureLayered)target;
        copy.RestoreFileState(state.Pixels, state.Path); copy.NotifyPropertyListChanged();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) { _pixels = null; _loadPath = ""; _renderPath = ""; }
        base.Dispose(disposing);
    }
}
