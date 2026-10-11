namespace Electron2D;

/// <summary>Selects the canvas distance-field area around a viewport.</summary>
public enum CanvasSDFOversize
{
    /// <summary>Covers the viewport.</summary>
    Percent100 = 0,
    /// <summary>Covers 120 percent of each viewport axis.</summary>
    Percent120 = 1,
    /// <summary>Covers 150 percent of each viewport axis.</summary>
    Percent150 = 2,
    /// <summary>Covers twice each viewport axis.</summary>
    Percent200 = 3,
    /// <summary>Nonselectable bound.</summary>
    Max = 4,
}
/// <summary>Selects distance-field resolution relative to its canvas area.</summary>
public enum CanvasSDFScale
{
    /// <summary>Uses full resolution.</summary>
    Percent100 = 0,
    /// <summary>Uses half resolution per axis.</summary>
    Percent50 = 1,
    /// <summary>Uses quarter resolution per axis.</summary>
    Percent25 = 2,
    /// <summary>Nonselectable bound.</summary>
    Max = 3,
}

public partial class Viewport
{
    private CanvasSDFOversize _sdfOversize = CanvasSDFOversize.Percent120;
    private CanvasSDFScale _sdfScale = CanvasSDFScale.Percent50;
    /// <summary>Gets or sets the area sampled by shaders using the canvas distance field.</summary>
    /// <value>Percent120 initially; native distance-field axes are bounded to 16384 pixels.</value>
    public CanvasSDFOversize SDFOversize { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _sdfOversize; } set { EnsureMutable(); if (value is < CanvasSDFOversize.Percent100 or >= CanvasSDFOversize.Max) throw new ArgumentOutOfRangeException(nameof(value)); _sdfOversize = value; } }
    /// <summary>Gets or sets canvas distance-field resolution independently of canvas drawing.</summary>
    /// <value>Percent50 initially; distances remain expressed in framebuffer pixels at every resolution.</value>
    public CanvasSDFScale SDFScale { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _sdfScale; } set { EnsureMutable(); if (value is < CanvasSDFScale.Percent100 or >= CanvasSDFScale.Max) throw new ArgumentOutOfRangeException(nameof(value)); _sdfScale = value; } }
    private static readonly PropertyDescriptor[] SDFProperties =
    [
        new PropertyDescriptor<Viewport, CanvasSDFOversize>(nameof(SDFOversize), n => n.SDFOversize, (n, v) => n.SDFOversize = v, _ => CanvasSDFOversize.Percent120, stored: true),
        new PropertyDescriptor<Viewport, CanvasSDFScale>(nameof(SDFScale), n => n.SDFScale, (n, v) => n.SDFScale = v, _ => CanvasSDFScale.Percent50, stored: true),
    ];
}
