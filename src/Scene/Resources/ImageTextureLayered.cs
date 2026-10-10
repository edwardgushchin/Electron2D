namespace Electron2D;

/// <summary>Owns immutable copied image layers for an array texture.</summary>
/// <remarks>Creation and updates validate before publication. Callbacks run after releasing the state lock;
/// callback exceptions retain committed pixels. Image inputs and outputs remain caller-owned.</remarks>
public abstract class ImageTextureLayered : TextureLayered
{
    private readonly object _gate = new();
    private LayeredTexturePixels? _pixels;
    /// <summary>Initializes an empty image-layer resource.</summary>
    protected ImageTextureLayered() { }
    /// <inheritdoc />
    public override Image.Format GetFormat() { lock (_gate) { ThrowIfDisposed(); return _pixels?.First.Source.Format ?? Image.Format.L8; } }
    /// <inheritdoc />
    public override int GetWidth() { lock (_gate) { ThrowIfDisposed(); return _pixels?.First.Source.Width ?? 0; } }
    /// <inheritdoc />
    public override int GetHeight() { lock (_gate) { ThrowIfDisposed(); return _pixels?.First.Source.Height ?? 0; } }
    /// <inheritdoc />
    public override int GetLayers() { lock (_gate) { ThrowIfDisposed(); return _pixels?.Layers.Length ?? 0; } }
    /// <inheritdoc />
    public override bool HasMipmaps() { lock (_gate) { ThrowIfDisposed(); return _pixels?.First.Source.HasMipmaps ?? false; } }
    /// <inheritdoc />
    public override Image? GetLayerData(int layerIndex)
    {
        lock (_gate) { ThrowIfDisposed(); if ((uint)layerIndex >= (uint)(_pixels?.Layers.Length ?? 0)) throw new ArgumentOutOfRangeException(nameof(layerIndex)); return _pixels!.Layers[layerIndex].CopyImage(); }
    }
    /// <summary>Atomically replaces this array with independent copies of homogeneous images.</summary>
    /// <param name="images">At least one live, nonempty image, with equal size, format and mipmap state.</param>
    /// <remarks>Failure preserves the previous layers and RID. Native upload occurs before subsequent drawing.</remarks>
    /// <exception cref="ArgumentException">The array is empty, heterogeneous, too large, or contains an empty image.</exception>
    /// <exception cref="NotSupportedException">A pixel format is unsupported by the existing sampling conversion.</exception>
    /// <exception cref="ObjectDisposedException">This resource or an input image is disposed.</exception>
    public void CreateFromImages(ReadOnlySpan<Image> images)
    {
        ThrowIfDisposed(); if (images.IsEmpty) throw new ArgumentException("Texture arrays require at least one image.", nameof(images));
        if (images.Length > LayeredTexturePixels.MaximumBytes / IntPtr.Size) throw new ArgumentException("Array layer metadata exceeds bounded storage.", nameof(images));
        var layers = new TexturePixels[images.Length]; var remaining = LayeredTexturePixels.MaximumBytes;
        for (var i = 0; i < layers.Length; i++)
        {
            layers[i] = TexturePixels.FromImage(images[i], remaining);
            remaining -= layers[i].Source.Data.Length + layers[i].Upload.Data.Length;
        }
        var pixels = new LayeredTexturePixels(layers);
        lock (_gate) { ThrowIfDisposed(); _pixels = pixels; }
        EmitChanged();
    }
    /// <summary>Replaces one layer while retaining all allocation parameters.</summary>
    /// <param name="image">A copied image matching the array dimensions, source format and mipmap state.</param>
    /// <param name="layer">The zero-based layer to replace.</param>
    /// <remarks>Other layers remain unchanged. Validation failure preserves the array; compatible native allocation is reused.</remarks>
    /// <exception cref="InvalidOperationException">The array is uninitialized.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The layer does not exist.</exception>
    /// <exception cref="ArgumentException">Image configuration does not match.</exception>
    /// <exception cref="ObjectDisposedException">This resource or the image is disposed.</exception>
    public void UpdateLayer(Image image, int layer)
    {
        ThrowIfDisposed(); var pixels = TexturePixels.FromImage(image, LayeredTexturePixels.MaximumBytes);
        lock (_gate)
        {
            ThrowIfDisposed(); if (_pixels is null) throw new InvalidOperationException("Initialize the texture array before updating it.");
            if ((uint)layer >= (uint)_pixels.Layers.Length) throw new ArgumentOutOfRangeException(nameof(layer));
            var layers = (TexturePixels[])_pixels.Layers.Clone(); layers[layer] = pixels;
            _pixels = new LayeredTexturePixels(layers) { Allocation = _pixels.Allocation };
        }
        EmitChanged();
    }
    internal override LayeredTexturePixels? CaptureLayers() { lock (_gate) { ThrowIfDisposed(); return _pixels; } }
    internal void ReplaceLayers(LayeredTexturePixels? pixels) { lock (_gate) { ThrowIfDisposed(); _pixels = pixels; } }
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (ImageTextureLayered)target; lock (_gate) { ThrowIfDisposed(); lock (copy._gate) copy._pixels = _pixels; }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) lock (_gate) _pixels = null; base.Dispose(disposing); }
}
