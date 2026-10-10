namespace Electron2D;

/// <summary>Retains image-array dimensions and layer count without image data.</summary>
/// <remarks>Metadata may be used by a headless consumer. It does not guarantee shader-coordinate behavior;
/// native sampling uses a single diagnostic layer. Size writes emit Changed, including equal writes; layer writes do not.</remarks>
public abstract class PlaceholderTextureLayered : TextureLayered
{
    private readonly object _gate = new();
    private Vector2i _size = Vector2i.One;
    private int _layers = 1;
    /// <summary>Initializes one metadata layer of size one by one.</summary>
    protected PlaceholderTextureLayered() { }
    /// <summary>Gets or replaces authored dimensions in pixels, without allocating images.</summary>
    /// <value>The requested integer dimensions, including zero or negative metadata.</value>
    public Vector2i Size { get { lock (_gate) { ThrowIfDisposed(); return _size; } } set { lock (_gate) { ThrowIfDisposed(); _size = value; } EmitChanged(); } }
    /// <summary>Gets or replaces the authored layer count, without allocating images or notifying Changed.</summary>
    /// <value>The requested integer count; initially one.</value>
    public int Layers { get { lock (_gate) { ThrowIfDisposed(); return _layers; } } set { lock (_gate) { ThrowIfDisposed(); _layers = value; } } }
    /// <inheritdoc />
    public override Image.Format GetFormat() { ThrowIfDisposed(); return Image.Format.Rgb8; }
    /// <inheritdoc />
    public override int GetWidth() => Size.X;
    /// <inheritdoc />
    public override int GetHeight() => Size.Y;
    /// <inheritdoc />
    public override int GetLayers() => Layers;
    /// <inheritdoc />
    public override bool HasMipmaps() { ThrowIfDisposed(); return false; }
    /// <inheritdoc />
    public override Image? GetLayerData(int layerIndex) { ThrowIfDisposed(); return null; }
    internal override LayeredTexturePixels? CaptureLayers() { ThrowIfDisposed(); return null; }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var p in base.GetPropertyDescriptors()) yield return p;
        yield return new PropertyDescriptor<PlaceholderTextureLayered, Vector2i>(nameof(Size), t => t.Size, (t, v) => t.Size = v, _ => Vector2i.One, stored: true);
        yield return new PropertyDescriptor<PlaceholderTextureLayered, int>(nameof(Layers), t => t.Layers, (t, v) => t.Layers = v, _ => 1, stored: true);
    }
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (PlaceholderTextureLayered)target; lock (_gate) { ThrowIfDisposed(); lock (copy._gate) { copy._size = _size; copy._layers = _layers; } }
    }
}
