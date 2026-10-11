namespace Electron2D;

/// <summary>Describes copied depth testing, depth writes and eight-bit two-sided stencil operations.</summary>
/// <remarks>Both visible faces must have equal effective masks and references on the current backend.
/// Culling allows different unused-face values. Depth writes require an enabled depth test.
/// Depth-bounds testing requires a separate native adapter and is not exposed as inert configuration.</remarks>
public sealed class RDPipelineDepthStencilState : ElectronObject
{
    /// <summary>Creates the default disabled tests and zeroing stencil operations.</summary>
    public RDPipelineDepthStencilState() { }
    private bool _enableDepthTest = false;
    /// <summary>Enables comparison of incoming depth against stored depth.</summary>
    /// <value>Initially false; validated and copied at pipeline creation.</value>
    public bool EnableDepthTest { get { ThrowIfDisposed(); return _enableDepthTest; } set { ThrowIfDisposed(); _enableDepthTest = value; } }
    private bool _enableDepthWrite = false;
    /// <summary>Enables depth writes for fragments passing an enabled depth test.</summary>
    /// <value>Initially false; validated and copied at pipeline creation.</value>
    public bool EnableDepthWrite { get { ThrowIfDisposed(); return _enableDepthWrite; } set { ThrowIfDisposed(); _enableDepthWrite = value; } }
    private RenderingDevice.CompareOperator _depthCompareOperator = RenderingDevice.CompareOperator.Always;
    /// <summary>Selects the depth comparison.</summary>
    /// <value>Initially RenderingDevice.CompareOperator.Always; validated and copied at pipeline creation.</value>
    public RenderingDevice.CompareOperator DepthCompareOperator { get { ThrowIfDisposed(); return _depthCompareOperator; } set { ThrowIfDisposed(); _depthCompareOperator = value; } }
    private bool _enableStencil = false;
    /// <summary>Enables front and back stencil tests.</summary>
    /// <value>Initially false; validated and copied at pipeline creation.</value>
    public bool EnableStencil { get { ThrowIfDisposed(); return _enableStencil; } set { ThrowIfDisposed(); _enableStencil = value; } }
    private RenderingDevice.CompareOperator _frontOpCompare = RenderingDevice.CompareOperator.Always;
    /// <summary>Selects the front-face comparison.</summary>
    /// <value>Initially RenderingDevice.CompareOperator.Always; validated and copied at pipeline creation.</value>
    public RenderingDevice.CompareOperator FrontOpCompare { get { ThrowIfDisposed(); return _frontOpCompare; } set { ThrowIfDisposed(); _frontOpCompare = value; } }
    private int _frontOpCompareMask = 0;
    /// <summary>Selects the front-face comparison mask; only its low eight bits are used.</summary>
    /// <value>Initially 0; validated and copied at pipeline creation.</value>
    public int FrontOpCompareMask { get { ThrowIfDisposed(); return _frontOpCompareMask; } set { ThrowIfDisposed(); _frontOpCompareMask = value; } }
    private RenderingDevice.StencilOperation _frontOpDepthFail = RenderingDevice.StencilOperation.Zero;
    /// <summary>Selects the front-face operation when depth fails.</summary>
    /// <value>Initially RenderingDevice.StencilOperation.Zero; validated and copied at pipeline creation.</value>
    public RenderingDevice.StencilOperation FrontOpDepthFail { get { ThrowIfDisposed(); return _frontOpDepthFail; } set { ThrowIfDisposed(); _frontOpDepthFail = value; } }
    private RenderingDevice.StencilOperation _frontOpFail = RenderingDevice.StencilOperation.Zero;
    /// <summary>Selects the front-face operation when stencil fails.</summary>
    /// <value>Initially RenderingDevice.StencilOperation.Zero; validated and copied at pipeline creation.</value>
    public RenderingDevice.StencilOperation FrontOpFail { get { ThrowIfDisposed(); return _frontOpFail; } set { ThrowIfDisposed(); _frontOpFail = value; } }
    private RenderingDevice.StencilOperation _frontOpPass = RenderingDevice.StencilOperation.Zero;
    /// <summary>Selects the front-face operation when both tests pass.</summary>
    /// <value>Initially RenderingDevice.StencilOperation.Zero; validated and copied at pipeline creation.</value>
    public RenderingDevice.StencilOperation FrontOpPass { get { ThrowIfDisposed(); return _frontOpPass; } set { ThrowIfDisposed(); _frontOpPass = value; } }
    private int _frontOpReference = 0;
    /// <summary>Selects the front-face reference from zero through 255.</summary>
    /// <value>Initially 0; validated and copied at pipeline creation.</value>
    public int FrontOpReference { get { ThrowIfDisposed(); return _frontOpReference; } set { ThrowIfDisposed(); _frontOpReference = value; } }
    private int _frontOpWriteMask = 0;
    /// <summary>Selects the front-face write mask; only its low eight bits are used.</summary>
    /// <value>Initially 0; validated and copied at pipeline creation.</value>
    public int FrontOpWriteMask { get { ThrowIfDisposed(); return _frontOpWriteMask; } set { ThrowIfDisposed(); _frontOpWriteMask = value; } }
    private RenderingDevice.CompareOperator _backOpCompare = RenderingDevice.CompareOperator.Always;
    /// <summary>Selects the back-face comparison.</summary>
    /// <value>Initially RenderingDevice.CompareOperator.Always; validated and copied at pipeline creation.</value>
    public RenderingDevice.CompareOperator BackOpCompare { get { ThrowIfDisposed(); return _backOpCompare; } set { ThrowIfDisposed(); _backOpCompare = value; } }
    private int _backOpCompareMask = 0;
    /// <summary>Selects the back-face comparison mask; only its low eight bits are used.</summary>
    /// <value>Initially 0; validated and copied at pipeline creation.</value>
    public int BackOpCompareMask { get { ThrowIfDisposed(); return _backOpCompareMask; } set { ThrowIfDisposed(); _backOpCompareMask = value; } }
    private RenderingDevice.StencilOperation _backOpDepthFail = RenderingDevice.StencilOperation.Zero;
    /// <summary>Selects the back-face operation when depth fails.</summary>
    /// <value>Initially RenderingDevice.StencilOperation.Zero; validated and copied at pipeline creation.</value>
    public RenderingDevice.StencilOperation BackOpDepthFail { get { ThrowIfDisposed(); return _backOpDepthFail; } set { ThrowIfDisposed(); _backOpDepthFail = value; } }
    private RenderingDevice.StencilOperation _backOpFail = RenderingDevice.StencilOperation.Zero;
    /// <summary>Selects the back-face operation when stencil fails.</summary>
    /// <value>Initially RenderingDevice.StencilOperation.Zero; validated and copied at pipeline creation.</value>
    public RenderingDevice.StencilOperation BackOpFail { get { ThrowIfDisposed(); return _backOpFail; } set { ThrowIfDisposed(); _backOpFail = value; } }
    private RenderingDevice.StencilOperation _backOpPass = RenderingDevice.StencilOperation.Zero;
    /// <summary>Selects the back-face operation when both tests pass.</summary>
    /// <value>Initially RenderingDevice.StencilOperation.Zero; validated and copied at pipeline creation.</value>
    public RenderingDevice.StencilOperation BackOpPass { get { ThrowIfDisposed(); return _backOpPass; } set { ThrowIfDisposed(); _backOpPass = value; } }
    private int _backOpReference = 0;
    /// <summary>Selects the back-face reference from zero through 255.</summary>
    /// <value>Initially 0; validated and copied at pipeline creation.</value>
    public int BackOpReference { get { ThrowIfDisposed(); return _backOpReference; } set { ThrowIfDisposed(); _backOpReference = value; } }
    private int _backOpWriteMask = 0;
    /// <summary>Selects the back-face write mask; only its low eight bits are used.</summary>
    /// <value>Initially 0; validated and copied at pipeline creation.</value>
    public int BackOpWriteMask { get { ThrowIfDisposed(); return _backOpWriteMask; } set { ThrowIfDisposed(); _backOpWriteMask = value; } }
    /// <summary>Projects inherited metadata followed by the copied depth/stencil configuration properties.</summary>
    /// <returns>Typed configuration descriptors with the pinned defaults.</returns>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<RDPipelineDepthStencilState, bool>(nameof(EnableDepthTest), p => p.EnableDepthTest, (p, v) => p.EnableDepthTest = v, _ => false),
        new PropertyDescriptor<RDPipelineDepthStencilState, bool>(nameof(EnableDepthWrite), p => p.EnableDepthWrite, (p, v) => p.EnableDepthWrite = v, _ => false),
        new PropertyDescriptor<RDPipelineDepthStencilState, RenderingDevice.CompareOperator>(nameof(DepthCompareOperator), p => p.DepthCompareOperator, (p, v) => p.DepthCompareOperator = v, _ => RenderingDevice.CompareOperator.Always),
        new PropertyDescriptor<RDPipelineDepthStencilState, bool>(nameof(EnableStencil), p => p.EnableStencil, (p, v) => p.EnableStencil = v, _ => false),
        new PropertyDescriptor<RDPipelineDepthStencilState, RenderingDevice.CompareOperator>(nameof(FrontOpCompare), p => p.FrontOpCompare, (p, v) => p.FrontOpCompare = v, _ => RenderingDevice.CompareOperator.Always),
        new PropertyDescriptor<RDPipelineDepthStencilState, int>(nameof(FrontOpCompareMask), p => p.FrontOpCompareMask, (p, v) => p.FrontOpCompareMask = v, _ => 0),
        new PropertyDescriptor<RDPipelineDepthStencilState, RenderingDevice.StencilOperation>(nameof(FrontOpDepthFail), p => p.FrontOpDepthFail, (p, v) => p.FrontOpDepthFail = v, _ => RenderingDevice.StencilOperation.Zero),
        new PropertyDescriptor<RDPipelineDepthStencilState, RenderingDevice.StencilOperation>(nameof(FrontOpFail), p => p.FrontOpFail, (p, v) => p.FrontOpFail = v, _ => RenderingDevice.StencilOperation.Zero),
        new PropertyDescriptor<RDPipelineDepthStencilState, RenderingDevice.StencilOperation>(nameof(FrontOpPass), p => p.FrontOpPass, (p, v) => p.FrontOpPass = v, _ => RenderingDevice.StencilOperation.Zero),
        new PropertyDescriptor<RDPipelineDepthStencilState, int>(nameof(FrontOpReference), p => p.FrontOpReference, (p, v) => p.FrontOpReference = v, _ => 0),
        new PropertyDescriptor<RDPipelineDepthStencilState, int>(nameof(FrontOpWriteMask), p => p.FrontOpWriteMask, (p, v) => p.FrontOpWriteMask = v, _ => 0),
        new PropertyDescriptor<RDPipelineDepthStencilState, RenderingDevice.CompareOperator>(nameof(BackOpCompare), p => p.BackOpCompare, (p, v) => p.BackOpCompare = v, _ => RenderingDevice.CompareOperator.Always),
        new PropertyDescriptor<RDPipelineDepthStencilState, int>(nameof(BackOpCompareMask), p => p.BackOpCompareMask, (p, v) => p.BackOpCompareMask = v, _ => 0),
        new PropertyDescriptor<RDPipelineDepthStencilState, RenderingDevice.StencilOperation>(nameof(BackOpDepthFail), p => p.BackOpDepthFail, (p, v) => p.BackOpDepthFail = v, _ => RenderingDevice.StencilOperation.Zero),
        new PropertyDescriptor<RDPipelineDepthStencilState, RenderingDevice.StencilOperation>(nameof(BackOpFail), p => p.BackOpFail, (p, v) => p.BackOpFail = v, _ => RenderingDevice.StencilOperation.Zero),
        new PropertyDescriptor<RDPipelineDepthStencilState, RenderingDevice.StencilOperation>(nameof(BackOpPass), p => p.BackOpPass, (p, v) => p.BackOpPass = v, _ => RenderingDevice.StencilOperation.Zero),
        new PropertyDescriptor<RDPipelineDepthStencilState, int>(nameof(BackOpReference), p => p.BackOpReference, (p, v) => p.BackOpReference = v, _ => 0),
        new PropertyDescriptor<RDPipelineDepthStencilState, int>(nameof(BackOpWriteMask), p => p.BackOpWriteMask, (p, v) => p.BackOpWriteMask = v, _ => 0),
    ]);
}
