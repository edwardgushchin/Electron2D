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
    private bool _discardPrimitives;
    /// <summary>Gets or sets whether vertex processing executes without rasterization or fragment execution.</summary>
    /// <value>False initially. Copied at pipeline creation; required for a vertex-only shader.</value>
    public bool DiscardPrimitives { get { ThrowIfDisposed(); return _discardPrimitives; } set { ThrowIfDisposed(); _discardPrimitives = value; } }
    private float _lineWidth = 1;
    /// <summary>Gets or sets the rasterized line width in pixels, including polygon outlines.</summary>
    /// <value>One initially. Must be finite and positive; nonunit widths require the enabled native feature.</value>
    /// <remarks>Copied at pipeline creation. The native range clamps the width before raster precision quantization.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned width is nonfinite or not positive.</exception>
    public float LineWidth { get { ThrowIfDisposed(); return _lineWidth; } set { ThrowIfDisposed(); if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); _lineWidth = value; } }
    private bool _wireframe = false;
    /// <summary>Gets or sets outline rather than filled rasterization.</summary>
    /// <value>Initially false; validated and copied by its device consumer.</value>
    public bool Wireframe { get { ThrowIfDisposed(); return _wireframe; } set { ThrowIfDisposed(); _wireframe = value; } }
    private bool _enableDepthClamp = false;
    /// <summary>Gets or sets depth clamping rather than clip-depth rejection.</summary>
    /// <value>Initially false; validated and copied by its device consumer.</value>
    public bool EnableDepthClamp { get { ThrowIfDisposed(); return _enableDepthClamp; } set { ThrowIfDisposed(); _enableDepthClamp = value; } }
    private bool _depthBiasEnabled = false;
    /// <summary>Enables constant and slope-dependent depth bias.</summary>
    /// <value>Initially false; copied at resource creation.</value>
    public bool DepthBiasEnabled { get { ThrowIfDisposed(); return _depthBiasEnabled; } set { ThrowIfDisposed(); _depthBiasEnabled = value; } }
    private float _depthBiasConstantFactor = 0;
    /// <summary>Scales the minimum representable depth increment.</summary>
    /// <value>Initially 0; copied at resource creation.</value>
    public float DepthBiasConstantFactor { get { ThrowIfDisposed(); return _depthBiasConstantFactor; } set { ThrowIfDisposed(); _depthBiasConstantFactor = RDTextureConfiguration.Finite(value); } }
    private float _depthBiasClamp = 0;
    /// <summary>Bounds depth bias: positive values cap it, negative values floor it, and zero disables clamping.</summary>
    /// <value>Initially 0; copied at resource creation.</value>
    public float DepthBiasClamp { get { ThrowIfDisposed(); return _depthBiasClamp; } set { ThrowIfDisposed(); _depthBiasClamp = RDTextureConfiguration.Finite(value); } }
    private float _depthBiasSlopeFactor = 0;
    /// <summary>Scales the maximum primitive depth slope.</summary>
    /// <value>Initially 0; copied at resource creation.</value>
    public float DepthBiasSlopeFactor { get { ThrowIfDisposed(); return _depthBiasSlopeFactor; } set { ThrowIfDisposed(); _depthBiasSlopeFactor = RDTextureConfiguration.Finite(value); } }
    /// <summary>Projects the caller-owned configuration through inherited typed property metadata.</summary>
    /// <returns>Inherited descriptors followed by this description's properties; configuration is copied when consumed.</returns>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<RDPipelineRasterizationState, bool>(nameof(DepthBiasEnabled), p => p.DepthBiasEnabled, (p, v) => p.DepthBiasEnabled = v, _ => false),
        new PropertyDescriptor<RDPipelineRasterizationState, float>(nameof(DepthBiasConstantFactor), p => p.DepthBiasConstantFactor, (p, v) => p.DepthBiasConstantFactor = v, _ => 0),
        new PropertyDescriptor<RDPipelineRasterizationState, float>(nameof(DepthBiasClamp), p => p.DepthBiasClamp, (p, v) => p.DepthBiasClamp = v, _ => 0),
        new PropertyDescriptor<RDPipelineRasterizationState, float>(nameof(DepthBiasSlopeFactor), p => p.DepthBiasSlopeFactor, (p, v) => p.DepthBiasSlopeFactor = v, _ => 0),

        new PropertyDescriptor<RDPipelineRasterizationState, RenderingDevice.PolygonCullMode>(nameof(CullMode), p => p.CullMode, (p, v) => p.CullMode = v, _ => RenderingDevice.PolygonCullMode.Disabled),
        new PropertyDescriptor<RDPipelineRasterizationState, RenderingDevice.PolygonFrontFace>(nameof(FrontFace), p => p.FrontFace, (p, v) => p.FrontFace = v, _ => RenderingDevice.PolygonFrontFace.Clockwise),
        new PropertyDescriptor<RDPipelineRasterizationState, bool>(nameof(DiscardPrimitives), n => n.DiscardPrimitives, (n, v) => n.DiscardPrimitives = v, _ => false),
        new PropertyDescriptor<RDPipelineRasterizationState, float>(nameof(LineWidth), p => p.LineWidth, (p, v) => p.LineWidth = v, _ => 1),
        new PropertyDescriptor<RDPipelineRasterizationState, bool>(nameof(Wireframe), p => p.Wireframe, (p, v) => p.Wireframe = v, _ => false),
        new PropertyDescriptor<RDPipelineRasterizationState, bool>(nameof(EnableDepthClamp), p => p.EnableDepthClamp, (p, v) => p.EnableDepthClamp = v, _ => false),
    ]);
}
