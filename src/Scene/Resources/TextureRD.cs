namespace Electron2D;

/// <summary>A canvas texture borrowing a sampled image from the active rendering device.</summary>
/// <remarks>The device image remains caller-owned. Drawing samples native GPU storage directly; GetImage
/// performs explicit readback. Integer sampling requires nearest filtering; snapshots convert raw integer lanes to float32 RGBA with float32 precision. Device identities are transient and are not saved in resource archives.</remarks>
public sealed class TextureRD : Texture
{
    private RenderingDevice? _device;
    private RID _texture;
    internal RenderingServer? ServerOwner;
    internal bool ServerReleased;
    internal string ServerPath = string.Empty;
    internal Vector2i? DisplaySize;
    /// <summary>Creates an unbound device texture with zero logical dimensions.</summary>
    public TextureRD() { }
    /// <summary>Gets or sets the borrowed image identity of the current rendering device.</summary>
    /// <value>An empty identity until bound; local-device and incompatible image identities reject.</value>
    /// <remarks>Validation precedes publication; a committed assignment emits Changed without owning the image.</remarks>
    public RID TextureRDRID
    {
        get { ThrowIfDisposed(); return _texture; }
        set
        {
            ThrowIfDisposed(); ServerOwner?.EnsureDeviceAccess(); if (_device is { IsDisposed: false }) _device.ValidateCanvasMutation();
            var device = value.IsValid() ? RenderingServer.GetRenderingDevice() ?? throw new InvalidOperationException("A GPU renderer is required.") : null;
            if (device is not null) device.ValidateCanvasImage(value, false);
            _texture = value; _device = device; EmitChanged();
        }
    }
    internal RenderingDevice.TextureInfo? DeviceInfo { get { ThrowIfDisposed(); return _device is { IsDisposed: false } ? _device.CanvasInfo(_texture) : null; } }
    internal nint NativeTexture(nint device) => DeviceInfo is null ? 0 : _device!.CanvasHandle(_texture, device);
    /// <inheritdoc />
    public override int GetWidth() => DeviceInfo is { } info ? DisplaySize?.X ?? (int)info.Width : 0;
    /// <inheritdoc />
    public override int GetHeight() => DeviceInfo is { } info ? DisplaySize?.Y ?? (int)info.Height : 0;
    /// <inheritdoc />
    public override Image.Format PixelFormat => DeviceInfo is { } info ? RenderingDevice.CanvasImageFormat(info.Format) ?? Image.Format.Rgbaf : Image.Format.L8;
    /// <inheritdoc />
    public override bool HasAlpha { get { ThrowIfDisposed(); return false; } }
    /// <inheritdoc />
    public override bool HasMipmaps => DeviceInfo?.Mips > 1;
    /// <inheritdoc />
    public override int MipmapCount => (int)(DeviceInfo?.Mips ?? 1) - 1;
    /// <summary>Returns an independent encoded image snapshot, or null for an unbound/unreadable source.</summary>
    /// <returns>A caller-owned image containing all readable mip levels; integer sources use raw component values in float32 RGBA.</returns>
    /// <remarks>This cold readback synchronizes pending device work. Formats without a direct Image representation
    /// convert on the GPU into floating-point RGBA; ordinary canvas drawing performs no CPU pixel copy.</remarks>
    public override Image? GetImage() => DeviceInfo is null ? null : _device!.CanvasImage(_texture, 0);
    internal override TexturePixels? CapturePixels() { ThrowIfDisposed(); return null; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new TextureRD();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    { var copy = (TextureRD)target; copy._device = _device; copy._texture = _texture; }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<TextureRD, RID>(nameof(TextureRDRID), p => p.TextureRDRID, (p, v) => p.TextureRDRID = v, _ => default)]);
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (ServerOwner is not null && !ServerReleased) throw new InvalidOperationException("The rendering server owns this texture."); if (_device is { IsDisposed: false }) _device.ValidateCanvasDisposal(); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _texture = default; _device = null; } base.Dispose(disposing); }
}
