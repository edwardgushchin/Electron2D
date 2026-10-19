namespace Electron2D;

/// <summary>Describes an ordered color/depth pass, its resolve destinations, inputs and preserved attachments.</summary>
public sealed class RDFramebufferPass : ElectronObject
{
    /// <summary>Creates the default configuration.</summary>
    public RDFramebufferPass() { }
    /// <summary>Marks an unused depth attachment, resolve slot or input slot.</summary>
    public const int AttachmentUnused = -1;
    private int[] _colorAttachments = [];
    /// <summary>Selects color attachment indices in shader output order.</summary>
    /// <value>Initially []; copied when the device consumes this configuration.</value>
    public int[] ColorAttachments { get { ThrowIfDisposed(); return _colorAttachments.ToArray(); } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _colorAttachments = value.ToArray(); } }
    private int _depthAttachment = -1;
    /// <summary>Selects the depth/stencil attachment index, or AttachmentUnused.</summary>
    /// <value>Initially -1; copied when the device consumes this configuration.</value>
    public int DepthAttachment { get { ThrowIfDisposed(); return _depthAttachment; } set { ThrowIfDisposed(); _depthAttachment = value; } }
    private int[] _resolveAttachments = [];
    /// <summary>Selects one resolve destination index per color slot, or AttachmentUnused.</summary>
    /// <value>Initially []; copied when the device consumes this configuration.</value>
    public int[] ResolveAttachments { get { ThrowIfDisposed(); return _resolveAttachments.ToArray(); } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _resolveAttachments = value.ToArray(); } }
    private int[] _inputAttachments = [];
    /// <summary>Selects input attachment indices in shader input-attachment-index order.</summary>
    /// <value>Initially empty. Reads and writes copy; AttachmentUnused leaves an unbound input slot.</value>
    public int[] InputAttachments { get { ThrowIfDisposed(); return _inputAttachments.ToArray(); } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _inputAttachments = value.ToArray(); } }
    private int[] _preserveAttachments = [];
    /// <summary>Selects inactive attachments whose contents must survive this pass.</summary>
    /// <value>Initially empty. Reads and writes copy; indices refer to the framebuffer attachment array.</value>
    public int[] PreserveAttachments { get { ThrowIfDisposed(); return _preserveAttachments.ToArray(); } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _preserveAttachments = value.ToArray(); } }
    /// <summary>Projects inherited metadata followed by the executing configuration fields.</summary>
    /// <returns>Typed configuration descriptors with their default values.</returns>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<RDFramebufferPass, int[]>(nameof(ColorAttachments), p => p.ColorAttachments, (p, v) => p.ColorAttachments = v, _ => []),
        new PropertyDescriptor<RDFramebufferPass, int>(nameof(DepthAttachment), p => p.DepthAttachment, (p, v) => p.DepthAttachment = v, _ => -1),
        new PropertyDescriptor<RDFramebufferPass, int[]>(nameof(InputAttachments), p => p.InputAttachments, (p, v) => p.InputAttachments = v, _ => []),
        new PropertyDescriptor<RDFramebufferPass, int[]>(nameof(PreserveAttachments), p => p.PreserveAttachments, (p, v) => p.PreserveAttachments = v, _ => []),
        new PropertyDescriptor<RDFramebufferPass, int[]>(nameof(ResolveAttachments), p => p.ResolveAttachments, (p, v) => p.ResolveAttachments = v, _ => []),
    ]);
}
