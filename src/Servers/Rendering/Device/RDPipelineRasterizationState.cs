namespace Electron2D;

/// <summary>Describes copied PipelineRasterizationState configuration for graphics resources.</summary>
public sealed class RDPipelineRasterizationState : ElectronObject
{
    /// <summary>Creates the default configuration.</summary>
    public RDPipelineRasterizationState() { }
    private RenderingDevice.PolygonCullMode _cullMode = RenderingDevice.PolygonCullMode.Disabled;
    /// <summary>Gets or sets the face culling selection.</summary>
    /// <value>Initially RenderingDevice.PolygonCullMode.Disabled; validated and copied by its device consumer.</value>
    public RenderingDevice.PolygonCullMode CullMode { get { ThrowIfDisposed(); return _cullMode; } set { ThrowIfDisposed(); _cullMode = value; } }
    private RenderingDevice.PolygonFrontFace _frontFace = RenderingDevice.PolygonFrontFace.Clockwise;
    /// <summary>Gets or sets the winding treated as front-facing.</summary>
    /// <value>Initially RenderingDevice.PolygonFrontFace.Clockwise; validated and copied by its device consumer.</value>
    public RenderingDevice.PolygonFrontFace FrontFace { get { ThrowIfDisposed(); return _frontFace; } set { ThrowIfDisposed(); _frontFace = value; } }
    private bool _wireframe = false;
    /// <summary>Gets or sets outline rather than filled rasterization.</summary>
    /// <value>Initially false; validated and copied by its device consumer.</value>
    public bool Wireframe { get { ThrowIfDisposed(); return _wireframe; } set { ThrowIfDisposed(); _wireframe = value; } }
    private bool _enableDepthClamp = false;
    /// <summary>Gets or sets depth clamping rather than clip-depth rejection.</summary>
    /// <value>Initially false; validated and copied by its device consumer.</value>
    public bool EnableDepthClamp { get { ThrowIfDisposed(); return _enableDepthClamp; } set { ThrowIfDisposed(); _enableDepthClamp = value; } }
    /// <summary>Projects the caller-owned configuration through inherited typed property metadata.</summary>
    /// <returns>Inherited descriptors followed by this description's properties; configuration is copied when consumed.</returns>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<RDPipelineRasterizationState, RenderingDevice.PolygonCullMode>(nameof(CullMode), p => p.CullMode, (p, v) => p.CullMode = v, _ => RenderingDevice.PolygonCullMode.Disabled),
        new PropertyDescriptor<RDPipelineRasterizationState, RenderingDevice.PolygonFrontFace>(nameof(FrontFace), p => p.FrontFace, (p, v) => p.FrontFace = v, _ => RenderingDevice.PolygonFrontFace.Clockwise),
        new PropertyDescriptor<RDPipelineRasterizationState, bool>(nameof(Wireframe), p => p.Wireframe, (p, v) => p.Wireframe = v, _ => false),
        new PropertyDescriptor<RDPipelineRasterizationState, bool>(nameof(EnableDepthClamp), p => p.EnableDepthClamp, (p, v) => p.EnableDepthClamp = v, _ => false),
    ]);
}
