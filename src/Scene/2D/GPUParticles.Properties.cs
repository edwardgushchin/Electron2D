namespace Electron2D;

public partial class GPUParticles
{
    /// <summary>Gets or sets particle capacity.</summary>
    /// <value>8 initially.</value>
    public int Amount { get => Read(ref _amount); set { if (value < 1 || ((long)value + 1) * 96 > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(value)); Assign(ref _amount, value, RenderingServer.ParticlesSetAmount); } }
    /// <summary>Gets or sets positive emission-cycle duration in seconds.</summary>
    /// <value>1 initially.</value>
    public double Lifetime { get => Read(ref _lifetime); set { Number(value, true); Assign(ref _lifetime, value, static (rid, v) => RenderingServer.ParticlesSetLifetime(rid, (float)v)); } }
    /// <summary>Gets or sets restart preprocessing time in seconds.</summary>
    /// <value>0 initially.</value>
    public double Preprocess { get => Read(ref _preprocess); set { Number(value); Assign(ref _preprocess, value, static (rid, v) => RenderingServer.ParticlesSetPreProcessTime(rid, (float)v)); } }
    /// <summary>Gets or sets fraction of capacity used for automatic births.</summary>
    /// <value>1 initially.</value>
    public float AmountRatio { get => Read(ref _amountRatio); set { value = Ratio(value); Assign(ref _amountRatio, value, RenderingServer.ParticlesSetAmountRatio); } }
    /// <summary>Gets or sets emission phase compression.</summary>
    /// <value>0 initially.</value>
    public float Explosiveness { get => Read(ref _explosiveness); set { value = Ratio(value); Assign(ref _explosiveness, value, RenderingServer.ParticlesSetExplosivenessRatio); } }
    /// <summary>Gets or sets cycle/index emission-time randomness.</summary>
    /// <value>0 initially.</value>
    public float Randomness { get => Read(ref _randomness); set { value = Ratio(value); Assign(ref _randomness, value, RenderingServer.ParticlesSetRandomnessRatio); } }
    /// <summary>Gets or sets fixed simulation frequency; nonpositive uses variable steps.</summary>
    /// <value>30 initially.</value>
    public int FixedFPS { get => Read(ref _fixedFPS); set { Assign(ref _fixedFPS, value, RenderingServer.ParticlesSetFixedFPS); } }
    /// <summary>Gets or sets fractional newborn simulation delta.</summary>
    /// <value>true initially.</value>
    public bool FractDelta { get => Read(ref _fractDelta); set { Assign(ref _fractDelta, value, RenderingServer.ParticlesSetFractionalDelta); } }
    /// <summary>Gets or sets fixed-step presentation interpolation.</summary>
    /// <value>true initially.</value>
    public bool Interpolate { get => Read(ref _interpolate); set { Assign(ref _interpolate, value, RenderingServer.ParticlesSetInterpolate); } }
    /// <summary>Gets or sets process interpolation-to-end ratio.</summary>
    /// <value>0 initially.</value>
    public float InterpToEnd { get => Read(ref _interpToEnd); set { value = Ratio(value); Assign(ref _interpToEnd, value, RenderingServer.ParticlesSetInterpToEnd); } }
    /// <summary>Gets or sets seed for future native births.</summary>
    /// <value>a random value initially.</value>
    public uint Seed { get => Read(ref _seed); set { Assign(ref _seed, value, static (rid, seed) => RenderingServer.Service!.SetParticleSeed(rid, seed)); } }
    /// <summary>Gets or sets particle quad ordering.</summary>
    /// <value>ParticlesDrawOrder.Lifetime initially.</value>
    public ParticlesDrawOrder DrawOrder { get => Read(ref _drawOrder); set { if (value < ParticlesDrawOrder.Index || value > ParticlesDrawOrder.ReverseLifetime) throw new ArgumentOutOfRangeException(nameof(value)); Assign(ref _drawOrder, value, RenderingServer.ParticlesSetDrawOrder); } }
    /// <summary>Gets or sets emitter-local visibility rectangle.</summary>
    /// <value>(-100, -100, 200, 200) initially.</value>
    public Rect2 VisibilityRect { get => Read(ref _visibilityRect); set { if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); Assign(ref _visibilityRect, value, RenderingServer.ParticlesSetCustomAABB); } }
    /// <summary>Gets or sets number of trail history sections.</summary>
    /// <value>8 initially.</value>
    public int TrailSections { get => Read(ref _trailSections); set { if (value is < 2 or > 128) throw new ArgumentOutOfRangeException(nameof(value)); Assign(ref _trailSections, value, null, mesh: true); } }
    /// <summary>Gets or sets mesh segments per trail section.</summary>
    /// <value>4 initially.</value>
    public int TrailSectionSubdivisions { get => Read(ref _trailSectionSubdivisions); set { if (value is < 1 or > 1024) throw new ArgumentOutOfRangeException(nameof(value)); Assign(ref _trailSectionSubdivisions, value, null, mesh: true); } }
}
