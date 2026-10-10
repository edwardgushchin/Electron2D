namespace Electron2D;

/// <summary>A sampled array of equally sized image layers, independent of ordinary canvas texture drawing.</summary>
/// <remarks>Resources own copied pixels or supply them through typed getters. Custom producers emit Changed after
/// mutation; prepared rendering reuses immutable snapshots. Native allocations belong to the active renderer.</remarks>
public abstract class TextureLayered : Resource
{
    /// <summary>Identifies the applicable layered sampling role.</summary>
    public enum LayeredType
    {
        /// <summary>An array of independent two-dimensional image layers.</summary>
        Array = 0,
    }
    private readonly object _snapshotGate = new();
    private LayeredTexturePixels? _snapshot;
    private RID _rid;
    internal RenderingServer? ServerOwner;
    internal bool ServerReleased;
    internal string ServerPath = string.Empty;
    internal bool RetainRendererCache => ServerOwner is not null && !IsDisposed;

    /// <summary>Initializes cached pixel invalidation for a custom layered producer.</summary>
    protected TextureLayered() { Changed += InvalidateSnapshot; }
    private void InvalidateSnapshot(Resource _) { lock (_snapshotGate) _snapshot = null; }
    /// <summary>Gets the source pixel format.</summary>
    /// <returns>The stored image format, without exposing upload conversion.</returns>
    public abstract Image.Format GetFormat();
    /// <summary>Gets the pixel width of one layer.</summary>
    /// <returns>The authored width; zero for uninitialized image arrays.</returns>
    public abstract int GetWidth();
    /// <summary>Gets the pixel height of one layer.</summary>
    /// <returns>The authored height; zero for uninitialized image arrays.</returns>
    public abstract int GetHeight();
    /// <summary>Gets the number of image layers.</summary>
    /// <returns>The authored count; zero for uninitialized image arrays.</returns>
    public abstract int GetLayers();
    /// <summary>Gets the sampling role.</summary>
    /// <returns>The independent image-array role.</returns>
    public virtual LayeredType GetLayeredType() { ThrowIfDisposed(); return LayeredType.Array; }
    /// <summary>Reports whether each image stores a mipmap chain.</summary>
    /// <returns>True when stored layers contain mipmaps.</returns>
    public abstract bool HasMipmaps();
    /// <summary>Copies one layer into a caller-owned image.</summary>
    /// <param name="layerIndex">The zero-based image layer.</param>
    /// <returns>An independent image, or null for a metadata-only placeholder.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An image-array layer is outside its stored range.</exception>
    public abstract Image? GetLayerData(int layerIndex);
    /// <inheritdoc />
    public override RID GetRID()
    {
        lock (_snapshotGate) { ThrowIfDisposed(); return _rid.IsValid() ? _rid : _rid = RenderingTextureRegistry.Register(this, ServerOwner); }
    }
    internal virtual LayeredTexturePixels? CaptureLayers()
    {
        lock (_snapshotGate)
        {
            ThrowIfDisposed(); if (GetLayeredType() != LayeredType.Array) throw new NotSupportedException("Only independent image-array sampling is applicable."); if (_snapshot is not null) return _snapshot;
            var count = GetLayers(); if (count == 0) return null;
            if (count < 0 || count > LayeredTexturePixels.MaximumBytes / IntPtr.Size) throw new InvalidOperationException("The custom image-array layer count is outside its bounded range.");
            var layers = new TexturePixels[count]; var remaining = LayeredTexturePixels.MaximumBytes;
            for (var i = 0; i < count; i++) { using var image = GetLayerData(i); if (image is null) return null; layers[i] = TexturePixels.FromImage(image, remaining); remaining -= layers[i].Source.Data.Length + layers[i].Upload.Data.Length; }
            var snapshot = new LayeredTexturePixels(layers);
            if (snapshot.First.Source.Width != GetWidth() || snapshot.First.Source.Height != GetHeight() || snapshot.First.Source.Format != GetFormat() || snapshot.First.Source.HasMipmaps != HasMipmaps())
                throw new InvalidOperationException("Custom layered metadata does not match its readable images.");
            return _snapshot = snapshot;
        }
    }
    /// <inheritdoc />
    protected override void ValidateDisposal()
    {
        if (ServerOwner is not null && !ServerReleased) throw new InvalidOperationException("The rendering server owns this texture array.");
        base.ValidateDisposal();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_snapshotGate) { RenderingTextureRegistry.Remove(_rid); _rid = default; _snapshot = null; ServerPath = string.Empty; }
        base.Dispose(disposing);
    }
}

internal sealed class LayeredTexturePixels
{
    internal const int MaximumBytes = 256 * 1024 * 1024;
    internal readonly TexturePixels[] Layers;
    internal TexturePixels First => Layers[0];
    internal object Allocation = new();
    internal readonly int UploadBytes;
    internal LayeredTexturePixels(TexturePixels[] layers)
    {
        if (layers.Length == 0) throw new ArgumentException("Texture arrays require at least one image.", nameof(layers));
        long sourceBytes = 0, uploadBytes = 0;
        foreach (var p in layers)
        {
            var a = layers[0].Source; var b = p.Source;
            if (a.Width != b.Width || a.Height != b.Height || a.Format != b.Format || a.HasMipmaps != b.HasMipmaps)
                throw new ArgumentException("Array layers require equal dimensions, formats and mipmap configuration.", nameof(layers));
            sourceBytes += b.Data.Length; uploadBytes += p.Upload.Data.Length;
            if (sourceBytes + uploadBytes > MaximumBytes) throw new ArgumentException("Texture array source and upload pixels exceed 256 MiB.", nameof(layers));
        }
        Layers = layers; UploadBytes = checked((int)uploadBytes);
    }
}
