namespace Electron2D;

/// <summary>Describes copied AttachmentFormat configuration for graphics resources.</summary>
public sealed class RDAttachmentFormat : ElectronObject
{
    /// <summary>Creates the default configuration.</summary>
    public RDAttachmentFormat() { }
    private RenderingDevice.DataFormat _format = RenderingDevice.DataFormat.R8G8B8A8UNORM;
    /// <summary>Gets or sets the color or depth/stencil attachment pixel format.</summary>
    /// <value>Initially RenderingDevice.DataFormat.R8G8B8A8UNORM; validated and copied by its device consumer.</value>
    public RenderingDevice.DataFormat Format { get { ThrowIfDisposed(); return _format; } set { ThrowIfDisposed(); _format = value; } }
    private RenderingDevice.TextureSamples _samples = RenderingDevice.TextureSamples.Samples1;
    /// <summary>Gets or sets the exact attachment sample count.</summary>
    /// <value>Initially RenderingDevice.TextureSamples.Samples1; validated and copied by its device consumer.</value>
    public RenderingDevice.TextureSamples Samples { get { ThrowIfDisposed(); return _samples; } set { ThrowIfDisposed(); _samples = value; } }
    private RenderingDevice.TextureUsageBits _usageFlags = 0;
    /// <summary>Gets or sets the required attachment allocation and transfer roles.</summary>
    /// <value>Initially 0; validated and copied by its device consumer.</value>
    public RenderingDevice.TextureUsageBits UsageFlags { get { ThrowIfDisposed(); return _usageFlags; } set { ThrowIfDisposed(); _usageFlags = value; } }
    /// <summary>Projects the caller-owned configuration through inherited typed property metadata.</summary>
    /// <returns>Inherited descriptors followed by this description's properties; configuration is copied when consumed.</returns>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<RDAttachmentFormat, RenderingDevice.DataFormat>(nameof(Format), p => p.Format, (p, v) => p.Format = v, _ => RenderingDevice.DataFormat.R8G8B8A8UNORM),
        new PropertyDescriptor<RDAttachmentFormat, RenderingDevice.TextureSamples>(nameof(Samples), p => p.Samples, (p, v) => p.Samples = v, _ => RenderingDevice.TextureSamples.Samples1),
        new PropertyDescriptor<RDAttachmentFormat, RenderingDevice.TextureUsageBits>(nameof(UsageFlags), p => p.UsageFlags, (p, v) => p.UsageFlags = v, _ => 0),
    ]);
}
