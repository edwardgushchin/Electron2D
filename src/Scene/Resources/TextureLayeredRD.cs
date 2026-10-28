namespace Electron2D;

/// <summary>Base resource for sampled device image arrays with a borrowed native identity.</summary>
/// <remarks>Only independent two-dimensional array layers are applicable. The resource owns no device pixels;
/// rendering uses the active device directly, while GetLayerData is an explicit readback.</remarks>
public abstract class TextureLayeredRD : TextureLayered
{
    private RenderingDevice? _device;
    private RID _texture;
    /// <summary>Initializes an unbound independent image-array view.</summary>
    protected TextureLayeredRD() { }
    /// <summary>Gets or sets a sampled image-array identity of the active rendering device.</summary>
    /// <value>An empty identity initially. Bound arrays contain one or more independent image layers.</value>
    /// <remarks>Device identities remain borrowed and transient; archives do not store this binding.</remarks>
    public RID TextureRDRID
    {
        get { ThrowIfDisposed(); return _texture; }
        set
        {
            ThrowIfDisposed(); ServerOwner?.EnsureDeviceAccess(); if (_device is { IsDisposed: false }) _device.ValidateCanvasMutation();
            var device = value.IsValid() ? RenderingServer.GetRenderingDevice() ?? throw new InvalidOperationException("A GPU renderer is required.") : null;
            if (device is not null) device.ValidateCanvasImage(value, true);
            _texture = value; _device = device; EmitChanged();
        }
    }
    internal RenderingDevice.TextureInfo? DeviceInfo { get { ThrowIfDisposed(); return _device is { IsDisposed: false } ? _device.CanvasInfo(_texture) : null; } }
    internal nint NativeTexture(nint device) => DeviceInfo is null ? 0 : _device!.CanvasHandle(_texture, device);
    /// <inheritdoc />
    public override int GetWidth() => (int)(DeviceInfo?.Width ?? 0);
    /// <inheritdoc />
    public override int GetHeight() => (int)(DeviceInfo?.Height ?? 0);
    /// <inheritdoc />
    public override int GetLayers() => (int)(DeviceInfo?.Layers ?? 0);
    /// <inheritdoc />
    public override Image.Format GetFormat() => DeviceInfo is { } info ? RenderingDevice.CanvasImageFormat(info.Format) ?? Image.Format.Rgbaf : Image.Format.L8;
    /// <inheritdoc />
    public override bool HasMIPMaps() => DeviceInfo?.Mips > 1;
    /// <summary>Copies every mip of one readable array layer into a caller-owned image.</summary>
    /// <param name="layerIndex">Zero-based layer; invalid layers reject even for unbound resources.</param>
    /// <returns>An independent image, or null when the selected device image is not CPU readable. Integer lanes convert to raw-valued float32 RGBA with float32 precision.</returns>
    public override Image? GetLayerData(int layerIndex)
    {
        var info = DeviceInfo; if ((uint)layerIndex >= (info?.Layers ?? 0)) throw new ArgumentOutOfRangeException(nameof(layerIndex));
        return _device!.CanvasImage(_texture, (uint)layerIndex);
    }
    internal override LayeredTexturePixels? CaptureLayers() { ThrowIfDisposed(); return null; }
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    { var copy = (TextureLayeredRD)target; copy._device = _device; copy._texture = _texture; }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<TextureLayeredRD, RID>(nameof(TextureRDRID), p => p.TextureRDRID, (p, v) => p.TextureRDRID = v, _ => default)]);
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (_device is { IsDisposed: false }) _device.ValidateCanvasDisposal(); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _device = null; _texture = default; } base.Dispose(disposing); }
}
