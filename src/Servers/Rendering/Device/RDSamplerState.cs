namespace Electron2D;

/// <summary>Describes filtering, normalized-coordinate addressing and mip selection for device-owned samplers and copied shader bindings.</summary>
/// <remarks>The current profile supports repeat, mirrored repeat and clamp-to-edge. Other addressing
/// modes fail during creation. Integer image sampling requires nearest filters without effective anisotropy. A limit of one disables anisotropy.
/// Nonzero LOD bias fails on a backend that cannot apply it.</remarks>
public sealed class RDSamplerState : ElectronObject
{
    /// <summary>Creates the default SamplerState configuration.</summary>
    public RDSamplerState() { }
    private RenderingDevice.SamplerFilter _minFilter = RenderingDevice.SamplerFilter.Nearest;
    /// <summary>Gets or sets the within-mip filter used for minification.</summary>
    /// <value>Initially RenderingDevice.SamplerFilter.Nearest; copied when a device sampler or shader binding is configured.</value>
    public RenderingDevice.SamplerFilter MinFilter { get { ThrowIfDisposed(); return _minFilter; } set { ThrowIfDisposed(); _minFilter = RDTextureConfiguration.Filter(value); } }
    private RenderingDevice.SamplerFilter _magFilter = RenderingDevice.SamplerFilter.Nearest;
    /// <summary>Gets or sets the within-mip filter used for magnification.</summary>
    /// <value>Initially RenderingDevice.SamplerFilter.Nearest; copied when a device sampler or shader binding is configured.</value>
    public RenderingDevice.SamplerFilter MagFilter { get { ThrowIfDisposed(); return _magFilter; } set { ThrowIfDisposed(); _magFilter = RDTextureConfiguration.Filter(value); } }
    private RenderingDevice.SamplerFilter _mipFilter = RenderingDevice.SamplerFilter.Nearest;
    /// <summary>Gets or sets how adjacent mip levels are selected or blended.</summary>
    /// <value>Initially RenderingDevice.SamplerFilter.Nearest; copied when a device sampler or shader binding is configured.</value>
    public RenderingDevice.SamplerFilter MIPFilter { get { ThrowIfDisposed(); return _mipFilter; } set { ThrowIfDisposed(); _mipFilter = RDTextureConfiguration.Filter(value); } }
    private RenderingDevice.SamplerRepeatMode _repeatU = RenderingDevice.SamplerRepeatMode.ClampToEdge;
    /// <summary>Gets or sets horizontal normalized-coordinate addressing.</summary>
    /// <value>Initially RenderingDevice.SamplerRepeatMode.ClampToEdge; copied when a device sampler or shader binding is configured.</value>
    public RenderingDevice.SamplerRepeatMode RepeatU { get { ThrowIfDisposed(); return _repeatU; } set { ThrowIfDisposed(); _repeatU = RDTextureConfiguration.Repeat(value); } }
    private RenderingDevice.SamplerRepeatMode _repeatV = RenderingDevice.SamplerRepeatMode.ClampToEdge;
    /// <summary>Gets or sets vertical normalized-coordinate addressing.</summary>
    /// <value>Initially RenderingDevice.SamplerRepeatMode.ClampToEdge; copied when a device sampler or shader binding is configured.</value>
    public RenderingDevice.SamplerRepeatMode RepeatV { get { ThrowIfDisposed(); return _repeatV; } set { ThrowIfDisposed(); _repeatV = RDTextureConfiguration.Repeat(value); } }
    private float _lODBias = 0;
    /// <summary>Gets or sets the bias added to the implicit mip selection.</summary>
    /// <value>Initially 0; copied when a device sampler or shader binding is configured.</value>
    public float LODBias { get { ThrowIfDisposed(); return _lODBias; } set { ThrowIfDisposed(); _lODBias = RDTextureConfiguration.Finite(value); } }
    private float _minLOD = 0;
    /// <summary>Gets or sets the minimum mip level, with zero identifying the base level.</summary>
    /// <value>Initially 0; copied when a device sampler or shader binding is configured.</value>
    public float MinLOD { get { ThrowIfDisposed(); return _minLOD; } set { ThrowIfDisposed(); _minLOD = RDTextureConfiguration.Finite(value); } }
    private float _maxLOD = 1e20f;
    /// <summary>Gets or sets the maximum mip level used for sampling.</summary>
    /// <value>Initially 1e20f; copied when a device sampler or shader binding is configured.</value>
    public float MaxLOD { get { ThrowIfDisposed(); return _maxLOD; } set { ThrowIfDisposed(); _maxLOD = RDTextureConfiguration.Finite(value); } }
    private float _anisotropyMax = 1;
    /// <summary>Gets or sets the anisotropic filtering limit from one through sixteen.</summary>
    /// <value>Initially 1; copied when a device sampler or shader binding is configured.</value>
    public float AnisotropyMax { get { ThrowIfDisposed(); return _anisotropyMax; } set { ThrowIfDisposed(); _anisotropyMax = RDTextureConfiguration.Anisotropy(value); } }
    private bool _useAnisotropy = false;
    /// <summary>Gets or sets whether the sampler uses anisotropic filtering.</summary>
    /// <value>Initially false; copied when a device sampler or shader binding is configured.</value>
    public bool UseAnisotropy { get { ThrowIfDisposed(); return _useAnisotropy; } set { ThrowIfDisposed(); _useAnisotropy = value; } }
    private bool _enableCompare = false;
    /// <summary>Enables comparison sampling for a depth image.</summary>
    /// <value>Initially false; available only for device depth images. High-level shaders reject enabled comparison.</value>
    public bool EnableCompare { get { ThrowIfDisposed(); return _enableCompare; } set { ThrowIfDisposed(); _enableCompare = value; } }
    private RenderingDevice.CompareOperator _compareOp = RenderingDevice.CompareOperator.Always;
    /// <summary>Selects comparison of a sample reference against stored depth.</summary>
    /// <value>Initially RenderingDevice.CompareOperator.Always; copied when a device sampler or shader binding is configured.</value>
    public RenderingDevice.CompareOperator CompareOp { get { ThrowIfDisposed(); return _compareOp; } set { ThrowIfDisposed(); _compareOp = value; } }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [
        new PropertyDescriptor<RDSamplerState, bool>(nameof(EnableCompare), p => p.EnableCompare, (p, v) => p.EnableCompare = v, _ => false),
        new PropertyDescriptor<RDSamplerState, RenderingDevice.CompareOperator>(nameof(CompareOp), p => p.CompareOp, (p, v) => p.CompareOp = v, _ => RenderingDevice.CompareOperator.Always),

            new PropertyDescriptor<RDSamplerState, RenderingDevice.SamplerFilter>(nameof(MinFilter), p => p.MinFilter, (p, v) => p.MinFilter = v, _ => RenderingDevice.SamplerFilter.Nearest),
            new PropertyDescriptor<RDSamplerState, RenderingDevice.SamplerFilter>(nameof(MagFilter), p => p.MagFilter, (p, v) => p.MagFilter = v, _ => RenderingDevice.SamplerFilter.Nearest),
            new PropertyDescriptor<RDSamplerState, RenderingDevice.SamplerFilter>(nameof(MIPFilter), p => p.MIPFilter, (p, v) => p.MIPFilter = v, _ => RenderingDevice.SamplerFilter.Nearest),
            new PropertyDescriptor<RDSamplerState, RenderingDevice.SamplerRepeatMode>(nameof(RepeatU), p => p.RepeatU, (p, v) => p.RepeatU = v, _ => RenderingDevice.SamplerRepeatMode.ClampToEdge),
            new PropertyDescriptor<RDSamplerState, RenderingDevice.SamplerRepeatMode>(nameof(RepeatV), p => p.RepeatV, (p, v) => p.RepeatV = v, _ => RenderingDevice.SamplerRepeatMode.ClampToEdge),
            new PropertyDescriptor<RDSamplerState, float>(nameof(LODBias), p => p.LODBias, (p, v) => p.LODBias = v, _ => 0),
            new PropertyDescriptor<RDSamplerState, float>(nameof(MinLOD), p => p.MinLOD, (p, v) => p.MinLOD = v, _ => 0),
            new PropertyDescriptor<RDSamplerState, float>(nameof(MaxLOD), p => p.MaxLOD, (p, v) => p.MaxLOD = v, _ => 1e20f),
            new PropertyDescriptor<RDSamplerState, float>(nameof(AnisotropyMax), p => p.AnisotropyMax, (p, v) => p.AnisotropyMax = v, _ => 1),
            new PropertyDescriptor<RDSamplerState, bool>(nameof(UseAnisotropy), p => p.UseAnisotropy, (p, v) => p.UseAnisotropy = v, _ => false)
        ]);
}
