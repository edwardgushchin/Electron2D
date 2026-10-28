namespace Electron2D;

/// <summary>Describes a copied initial or shared image view.</summary>
/// <remarks>Texture creation copies each sampled component source, including identity, zero and one.
/// Sampling maps components after format conversion and filtering; raw transfers retain stored channels.
/// Storage-image descriptors and framebuffer attachments require identity mappings.
/// Alternate pixel formats use native shared image storage and must be declared shareable.</remarks>
public sealed class RDTextureView : ElectronObject
{
    /// <summary>Creates the default TextureView configuration.</summary>
    public RDTextureView() { }
    private RenderingDevice.DataFormat _formatOverride = RenderingDevice.DataFormat.Max;
    /// <summary>Gets or sets the view pixel format, or Max to retain the allocation format.</summary>
    /// <value>Initially RenderingDevice.DataFormat.Max; copied when the device creates its resource.</value>
    public RenderingDevice.DataFormat FormatOverride { get { ThrowIfDisposed(); return _formatOverride; } set { ThrowIfDisposed(); _formatOverride = RDTextureConfiguration.ViewFormat(value); } }
    private RenderingDevice.TextureSwizzle _swizzleR = RenderingDevice.TextureSwizzle.R;
    /// <summary>Gets or sets the source of the sampled red component.</summary>
    /// <value>Initially RenderingDevice.TextureSwizzle.R; copied when the device creates its resource.</value>
    public RenderingDevice.TextureSwizzle SwizzleR { get { ThrowIfDisposed(); return _swizzleR; } set { ThrowIfDisposed(); _swizzleR = RDTextureConfiguration.Swizzle(value); } }
    private RenderingDevice.TextureSwizzle _swizzleG = RenderingDevice.TextureSwizzle.G;
    /// <summary>Gets or sets the source of the sampled green component.</summary>
    /// <value>Initially RenderingDevice.TextureSwizzle.G; copied when the device creates its resource.</value>
    public RenderingDevice.TextureSwizzle SwizzleG { get { ThrowIfDisposed(); return _swizzleG; } set { ThrowIfDisposed(); _swizzleG = RDTextureConfiguration.Swizzle(value); } }
    private RenderingDevice.TextureSwizzle _swizzleB = RenderingDevice.TextureSwizzle.B;
    /// <summary>Gets or sets the source of the sampled blue component.</summary>
    /// <value>Initially RenderingDevice.TextureSwizzle.B; copied when the device creates its resource.</value>
    public RenderingDevice.TextureSwizzle SwizzleB { get { ThrowIfDisposed(); return _swizzleB; } set { ThrowIfDisposed(); _swizzleB = RDTextureConfiguration.Swizzle(value); } }
    private RenderingDevice.TextureSwizzle _swizzleA = RenderingDevice.TextureSwizzle.A;
    /// <summary>Gets or sets the source of the sampled alpha component.</summary>
    /// <value>Initially RenderingDevice.TextureSwizzle.A; copied when the device creates its resource.</value>
    public RenderingDevice.TextureSwizzle SwizzleA { get { ThrowIfDisposed(); return _swizzleA; } set { ThrowIfDisposed(); _swizzleA = RDTextureConfiguration.Swizzle(value); } }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [
            new PropertyDescriptor<RDTextureView, RenderingDevice.DataFormat>(nameof(FormatOverride), p => p.FormatOverride, (p, v) => p.FormatOverride = v, _ => RenderingDevice.DataFormat.Max),
            new PropertyDescriptor<RDTextureView, RenderingDevice.TextureSwizzle>(nameof(SwizzleR), p => p.SwizzleR, (p, v) => p.SwizzleR = v, _ => RenderingDevice.TextureSwizzle.R),
            new PropertyDescriptor<RDTextureView, RenderingDevice.TextureSwizzle>(nameof(SwizzleG), p => p.SwizzleG, (p, v) => p.SwizzleG = v, _ => RenderingDevice.TextureSwizzle.G),
            new PropertyDescriptor<RDTextureView, RenderingDevice.TextureSwizzle>(nameof(SwizzleB), p => p.SwizzleB, (p, v) => p.SwizzleB = v, _ => RenderingDevice.TextureSwizzle.B),
            new PropertyDescriptor<RDTextureView, RenderingDevice.TextureSwizzle>(nameof(SwizzleA), p => p.SwizzleA, (p, v) => p.SwizzleA = v, _ => RenderingDevice.TextureSwizzle.A)
        ]);
}
