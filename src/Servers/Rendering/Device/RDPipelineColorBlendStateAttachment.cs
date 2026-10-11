namespace Electron2D;

/// <summary>Describes copied PipelineColorBlendStateAttachment configuration for graphics resources.</summary>
public sealed class RDPipelineColorBlendStateAttachment : ElectronObject
{
    /// <summary>Creates the default configuration.</summary>
    public RDPipelineColorBlendStateAttachment() { }
    private bool _enableBlend = false;
    /// <summary>Gets or sets whether source and destination pixels blend.</summary>
    /// <value>Initially false; validated and copied by its device consumer.</value>
    public bool EnableBlend { get { ThrowIfDisposed(); return _enableBlend; } set { ThrowIfDisposed(); _enableBlend = value; } }
    private RenderingDevice.BlendFactor _srcColorBlendFactor = RenderingDevice.BlendFactor.Zero;
    /// <summary>Gets or sets the source RGB factor.</summary>
    /// <value>Initially RenderingDevice.BlendFactor.Zero; validated and copied by its device consumer.</value>
    public RenderingDevice.BlendFactor SrcColorBlendFactor { get { ThrowIfDisposed(); return _srcColorBlendFactor; } set { ThrowIfDisposed(); _srcColorBlendFactor = value; } }
    private RenderingDevice.BlendFactor _dstColorBlendFactor = RenderingDevice.BlendFactor.Zero;
    /// <summary>Gets or sets the destination RGB factor.</summary>
    /// <value>Initially RenderingDevice.BlendFactor.Zero; validated and copied by its device consumer.</value>
    public RenderingDevice.BlendFactor DstColorBlendFactor { get { ThrowIfDisposed(); return _dstColorBlendFactor; } set { ThrowIfDisposed(); _dstColorBlendFactor = value; } }
    private RenderingDevice.BlendOperation _colorBlendOp = RenderingDevice.BlendOperation.Add;
    /// <summary>Gets or sets the RGB combination operation.</summary>
    /// <value>Initially RenderingDevice.BlendOperation.Add; validated and copied by its device consumer.</value>
    public RenderingDevice.BlendOperation ColorBlendOp { get { ThrowIfDisposed(); return _colorBlendOp; } set { ThrowIfDisposed(); _colorBlendOp = value; } }
    private RenderingDevice.BlendFactor _srcAlphaBlendFactor = RenderingDevice.BlendFactor.Zero;
    /// <summary>Gets or sets the source alpha factor.</summary>
    /// <value>Initially RenderingDevice.BlendFactor.Zero; validated and copied by its device consumer.</value>
    public RenderingDevice.BlendFactor SrcAlphaBlendFactor { get { ThrowIfDisposed(); return _srcAlphaBlendFactor; } set { ThrowIfDisposed(); _srcAlphaBlendFactor = value; } }
    private RenderingDevice.BlendFactor _dstAlphaBlendFactor = RenderingDevice.BlendFactor.Zero;
    /// <summary>Gets or sets the destination alpha factor.</summary>
    /// <value>Initially RenderingDevice.BlendFactor.Zero; validated and copied by its device consumer.</value>
    public RenderingDevice.BlendFactor DstAlphaBlendFactor { get { ThrowIfDisposed(); return _dstAlphaBlendFactor; } set { ThrowIfDisposed(); _dstAlphaBlendFactor = value; } }
    private RenderingDevice.BlendOperation _alphaBlendOp = RenderingDevice.BlendOperation.Add;
    /// <summary>Gets or sets the alpha combination operation.</summary>
    /// <value>Initially RenderingDevice.BlendOperation.Add; validated and copied by its device consumer.</value>
    public RenderingDevice.BlendOperation AlphaBlendOp { get { ThrowIfDisposed(); return _alphaBlendOp; } set { ThrowIfDisposed(); _alphaBlendOp = value; } }
    private bool _writeR = true;
    /// <summary>Gets or sets whether red writes are enabled.</summary>
    /// <value>Initially true; validated and copied by its device consumer.</value>
    public bool WriteR { get { ThrowIfDisposed(); return _writeR; } set { ThrowIfDisposed(); _writeR = value; } }
    private bool _writeG = true;
    /// <summary>Gets or sets whether green writes are enabled.</summary>
    /// <value>Initially true; validated and copied by its device consumer.</value>
    public bool WriteG { get { ThrowIfDisposed(); return _writeG; } set { ThrowIfDisposed(); _writeG = value; } }
    private bool _writeB = true;
    /// <summary>Gets or sets whether blue writes are enabled.</summary>
    /// <value>Initially true; validated and copied by its device consumer.</value>
    public bool WriteB { get { ThrowIfDisposed(); return _writeB; } set { ThrowIfDisposed(); _writeB = value; } }
    private bool _writeA = true;
    /// <summary>Gets or sets whether alpha writes are enabled.</summary>
    /// <value>Initially true; validated and copied by its device consumer.</value>
    public bool WriteA { get { ThrowIfDisposed(); return _writeA; } set { ThrowIfDisposed(); _writeA = value; } }
    /// <summary>Configures ordinary source-alpha color mixing with accumulated alpha.</summary>
    public void SetAsMix() { EnableBlend = true; SrcColorBlendFactor = RenderingDevice.BlendFactor.SrcAlpha; DstColorBlendFactor = RenderingDevice.BlendFactor.OneMinusSrcAlpha; SrcAlphaBlendFactor = RenderingDevice.BlendFactor.One; DstAlphaBlendFactor = RenderingDevice.BlendFactor.OneMinusSrcAlpha; ColorBlendOp = AlphaBlendOp = RenderingDevice.BlendOperation.Add; }
    /// <summary>Projects the caller-owned configuration through inherited typed property metadata.</summary>
    /// <returns>Inherited descriptors followed by this description's properties; configuration is copied when consumed.</returns>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<RDPipelineColorBlendStateAttachment, bool>(nameof(EnableBlend), p => p.EnableBlend, (p, v) => p.EnableBlend = v, _ => false),
        new PropertyDescriptor<RDPipelineColorBlendStateAttachment, RenderingDevice.BlendFactor>(nameof(SrcColorBlendFactor), p => p.SrcColorBlendFactor, (p, v) => p.SrcColorBlendFactor = v, _ => RenderingDevice.BlendFactor.Zero),
        new PropertyDescriptor<RDPipelineColorBlendStateAttachment, RenderingDevice.BlendFactor>(nameof(DstColorBlendFactor), p => p.DstColorBlendFactor, (p, v) => p.DstColorBlendFactor = v, _ => RenderingDevice.BlendFactor.Zero),
        new PropertyDescriptor<RDPipelineColorBlendStateAttachment, RenderingDevice.BlendOperation>(nameof(ColorBlendOp), p => p.ColorBlendOp, (p, v) => p.ColorBlendOp = v, _ => RenderingDevice.BlendOperation.Add),
        new PropertyDescriptor<RDPipelineColorBlendStateAttachment, RenderingDevice.BlendFactor>(nameof(SrcAlphaBlendFactor), p => p.SrcAlphaBlendFactor, (p, v) => p.SrcAlphaBlendFactor = v, _ => RenderingDevice.BlendFactor.Zero),
        new PropertyDescriptor<RDPipelineColorBlendStateAttachment, RenderingDevice.BlendFactor>(nameof(DstAlphaBlendFactor), p => p.DstAlphaBlendFactor, (p, v) => p.DstAlphaBlendFactor = v, _ => RenderingDevice.BlendFactor.Zero),
        new PropertyDescriptor<RDPipelineColorBlendStateAttachment, RenderingDevice.BlendOperation>(nameof(AlphaBlendOp), p => p.AlphaBlendOp, (p, v) => p.AlphaBlendOp = v, _ => RenderingDevice.BlendOperation.Add),
        new PropertyDescriptor<RDPipelineColorBlendStateAttachment, bool>(nameof(WriteR), p => p.WriteR, (p, v) => p.WriteR = v, _ => true),
        new PropertyDescriptor<RDPipelineColorBlendStateAttachment, bool>(nameof(WriteG), p => p.WriteG, (p, v) => p.WriteG = v, _ => true),
        new PropertyDescriptor<RDPipelineColorBlendStateAttachment, bool>(nameof(WriteB), p => p.WriteB, (p, v) => p.WriteB = v, _ => true),
        new PropertyDescriptor<RDPipelineColorBlendStateAttachment, bool>(nameof(WriteA), p => p.WriteA, (p, v) => p.WriteA = v, _ => true),
    ]);
}
