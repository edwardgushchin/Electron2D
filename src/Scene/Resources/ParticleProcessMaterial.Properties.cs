namespace Electron2D;

public sealed partial class ParticleProcessMaterial
{
    private Vector2 _direction = new(1, 0);
    /// <summary>Gets or sets the local emission direction.</summary>
    /// <value>new(1, 0) initially.</value>
    public Vector2 Direction { get => Read(ref _direction); set { Set(ref _direction, Finite(value)); } }
    private Vector2 _gravity = new(0, -9.8f);
    /// <summary>Gets or sets constant acceleration in simulation coordinates.</summary>
    /// <value>new(0, -9.8f) initially.</value>
    public Vector2 Gravity { get => Read(ref _gravity); set { Set(ref _gravity, Finite(value)); } }
    private float _spread = 45;
    /// <summary>Gets or sets emission spread in degrees on either side of Direction.</summary>
    /// <value>45 initially.</value>
    public float Spread { get => Read(ref _spread); set { Set(ref _spread, Finite(value)); } }
    private float _lifetimeRandomness = 0;
    /// <summary>Gets or sets the fraction of lifetime removed by each particle random sample.</summary>
    /// <value>0 initially.</value>
    public float LifetimeRandomness { get => Read(ref _lifetimeRandomness); set { Set(ref _lifetimeRandomness, Ratio(value)); } }
    private float _inheritVelocityRatio = 0;
    /// <summary>Gets or sets the emitter velocity multiplier added at birth.</summary>
    /// <value>0 initially.</value>
    public float InheritVelocityRatio { get => Read(ref _inheritVelocityRatio); set { Set(ref _inheritVelocityRatio, Finite(value)); } }
    private ParticlesEmissionShape _emissionShape = ParticlesEmissionShape.Point;
    /// <summary>Gets or sets the emission distribution.</summary>
    /// <value>ParticlesEmissionShape.Point initially.</value>
    public ParticlesEmissionShape EmissionShape { get => Read(ref _emissionShape); set { if (value < 0 || value >= ParticlesEmissionShape.Max) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref _emissionShape, value); } }
    private Vector2 _emissionShapeOffset = Vector2.Zero;
    /// <summary>Gets or sets the local distribution offset.</summary>
    /// <value>Vector2.Zero initially.</value>
    public Vector2 EmissionShapeOffset { get => Read(ref _emissionShapeOffset); set { Set(ref _emissionShapeOffset, Finite(value)); } }
    private Vector2 _emissionShapeScale = Vector2.One;
    /// <summary>Gets or sets the local distribution scale.</summary>
    /// <value>Vector2.One initially.</value>
    public Vector2 EmissionShapeScale { get => Read(ref _emissionShapeScale); set { Set(ref _emissionShapeScale, Finite(value)); } }
    private Vector2 _emissionRectExtents = Vector2.One;
    /// <summary>Gets or sets the rectangle half extents.</summary>
    /// <value>Vector2.One initially.</value>
    public Vector2 EmissionRectExtents { get => Read(ref _emissionRectExtents); set { Set(ref _emissionRectExtents, Finite(value)); } }
    private float _emissionSphereRadius = 1;
    /// <summary>Gets or sets the radius of the projected sphere distribution.</summary>
    /// <value>1 initially.</value>
    public float EmissionSphereRadius { get => Read(ref _emissionSphereRadius); set { Set(ref _emissionSphereRadius, Nonnegative(value)); } }
    private float _emissionRingRadius = 1;
    /// <summary>Gets or sets the outer planar ring radius.</summary>
    /// <value>1 initially.</value>
    public float EmissionRingRadius { get => Read(ref _emissionRingRadius); set { Set(ref _emissionRingRadius, Nonnegative(value)); } }
    private float _emissionRingInnerRadius = 0;
    /// <summary>Gets or sets the inner planar ring radius.</summary>
    /// <value>0 initially.</value>
    public float EmissionRingInnerRadius { get => Read(ref _emissionRingInnerRadius); set { Set(ref _emissionRingInnerRadius, Nonnegative(value)); } }
    private int _emissionPointCount = 1;
    /// <summary>Gets or sets the number of texels used from the emission point texture.</summary>
    /// <value>1 initially.</value>
    public int EmissionPointCount { get => Read(ref _emissionPointCount); set { Set(ref _emissionPointCount, Nonnegative(value)); } }
    private Texture? _emissionPointTexture = null;
    /// <summary>Gets or sets the borrowed texture containing planar emission positions in red and green.</summary>
    /// <value>null initially.</value>
    public Texture? EmissionPointTexture { get => Read(ref _emissionPointTexture); set { if (value is not null) ObjectDisposedException.ThrowIf(value.IsDisposed, value); Set(ref _emissionPointTexture, value); } }
    private Texture? _emissionNormalTexture = null;
    /// <summary>Gets or sets the borrowed texture containing directed-point normals in red and green.</summary>
    /// <value>null initially.</value>
    public Texture? EmissionNormalTexture { get => Read(ref _emissionNormalTexture); set { if (value is not null) ObjectDisposedException.ThrowIf(value.IsDisposed, value); Set(ref _emissionNormalTexture, value); } }
    private Texture? _emissionColorTexture = null;
    /// <summary>Gets or sets the borrowed texture containing per-point colors.</summary>
    /// <value>null initially.</value>
    public Texture? EmissionColorTexture { get => Read(ref _emissionColorTexture); set { if (value is not null) ObjectDisposedException.ThrowIf(value.IsDisposed, value); Set(ref _emissionColorTexture, value); } }
    private Color _color = Colors.White;
    /// <summary>Gets or sets the base particle color.</summary>
    /// <value>Colors.White initially.</value>
    public Color Color { get => Read(ref _color); set { Set(ref _color, Finite(value)); } }
    private Vector2 _velocityPivot = Vector2.Zero;
    /// <summary>Gets or sets the local pivot for controlled radial velocity.</summary>
    /// <value>Vector2.Zero initially.</value>
    public Vector2 VelocityPivot { get => Read(ref _velocityPivot); set { Set(ref _velocityPivot, Finite(value)); } }
    private bool _turbulenceEnabled = false;
    /// <summary>Gets or sets whether procedural turbulence affects motion.</summary>
    /// <value>false initially.</value>
    public bool TurbulenceEnabled { get => Read(ref _turbulenceEnabled); set { Set(ref _turbulenceEnabled, value); } }
    private float _turbulenceNoiseStrength = 1;
    /// <summary>Gets or sets the turbulence noise contrast.</summary>
    /// <value>1 initially.</value>
    public float TurbulenceNoiseStrength { get => Read(ref _turbulenceNoiseStrength); set { Set(ref _turbulenceNoiseStrength, Finite(value)); } }
    private float _turbulenceNoiseScale = 9;
    /// <summary>Gets or sets the nonnegative turbulence scale control.</summary>
    /// <value>9 initially.</value>
    public float TurbulenceNoiseScale { get => Read(ref _turbulenceNoiseScale); set { Set(ref _turbulenceNoiseScale, Nonnegative(value)); } }
    private Vector3 _turbulenceNoiseSpeed = Vector3.Zero;
    /// <summary>Gets or sets the three numeric noise-domain animation rates.</summary>
    /// <value>Vector3.Zero initially.</value>
    public Vector3 TurbulenceNoiseSpeed { get => Read(ref _turbulenceNoiseSpeed); set { Set(ref _turbulenceNoiseSpeed, Finite(value)); } }
    private float _turbulenceNoiseSpeedRandom = .2f;
    /// <summary>Gets or sets the fourth noise-domain animation rate.</summary>
    /// <value>.2f initially.</value>
    public float TurbulenceNoiseSpeedRandom { get => Read(ref _turbulenceNoiseSpeedRandom); set { Set(ref _turbulenceNoiseSpeedRandom, Finite(value)); } }
    private CollisionModeEnum _collisionMode = CollisionModeEnum.Disabled;
    /// <summary>Gets or sets the canvas distance-field collision policy.</summary>
    /// <value>CollisionModeEnum.Disabled initially.</value>
    public CollisionModeEnum CollisionMode { get => Read(ref _collisionMode); set { if (value < 0 || value >= CollisionModeEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref _collisionMode, value); } }
    private float _collisionFriction = 0;
    /// <summary>Gets or sets the fraction of tangential physical velocity removed at contact.</summary>
    /// <value>0 initially.</value>
    public float CollisionFriction { get => Read(ref _collisionFriction); set { Set(ref _collisionFriction, Finite(value)); } }
    private float _collisionBounce = 0;
    /// <summary>Gets or sets the rigid-contact bounce multiplier.</summary>
    /// <value>0 initially.</value>
    public float CollisionBounce { get => Read(ref _collisionBounce); set { Set(ref _collisionBounce, Finite(value)); } }
    private bool _collisionUseScale = false;
    /// <summary>Gets or sets whether particle scale changes the collision radius.</summary>
    /// <value>false initially.</value>
    public bool CollisionUseScale { get => Read(ref _collisionUseScale); set { Set(ref _collisionUseScale, value); } }
    private SubEmitterModeEnum _subEmitterMode = SubEmitterModeEnum.Disabled;
    /// <summary>Gets or sets the subemission timing policy.</summary>
    /// <value>SubEmitterModeEnum.Disabled initially.</value>
    public SubEmitterModeEnum SubEmitterMode { get => Read(ref _subEmitterMode); set { if (value < 0 || value >= SubEmitterModeEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref _subEmitterMode, value); } }
    private float _subEmitterFrequency = 4;
    /// <summary>Gets or sets the constant subemission frequency in births per second.</summary>
    /// <value>4 initially.</value>
    public float SubEmitterFrequency { get => Read(ref _subEmitterFrequency); set { Set(ref _subEmitterFrequency, Positive(value)); } }
    private int _subEmitterAmountAtStart = 1;
    /// <summary>Gets or sets the child birth count at parent birth.</summary>
    /// <value>1 initially.</value>
    public int SubEmitterAmountAtStart { get => Read(ref _subEmitterAmountAtStart); set { Set(ref _subEmitterAmountAtStart, Nonnegative(value)); } }
    private int _subEmitterAmountAtEnd = 1;
    /// <summary>Gets or sets the child birth count at the end of parent life.</summary>
    /// <value>1 initially.</value>
    public int SubEmitterAmountAtEnd { get => Read(ref _subEmitterAmountAtEnd); set { Set(ref _subEmitterAmountAtEnd, Nonnegative(value)); } }
    private int _subEmitterAmountAtCollision = 1;
    /// <summary>Gets or sets the child birth count at a parent contact.</summary>
    /// <value>1 initially.</value>
    public int SubEmitterAmountAtCollision { get => Read(ref _subEmitterAmountAtCollision); set { Set(ref _subEmitterAmountAtCollision, Nonnegative(value)); } }
    private bool _subEmitterKeepVelocity = false;
    /// <summary>Gets or sets whether subemission supplies the parent velocity.</summary>
    /// <value>false initially.</value>
    public bool SubEmitterKeepVelocity { get => Read(ref _subEmitterKeepVelocity); set { Set(ref _subEmitterKeepVelocity, value); } }
    /// <summary>Gets or sets the minimum InitialVelocity value; preserves ordered bounds.</summary>
    public float InitialVelocityMin { get => GetParamMin(Parameter.InitialLinearVelocity); set => SetParamMin(Parameter.InitialLinearVelocity, value); }
    /// <summary>Gets or sets the maximum InitialVelocity value; preserves ordered bounds.</summary>
    public float InitialVelocityMax { get => GetParamMax(Parameter.InitialLinearVelocity); set => SetParamMax(Parameter.InitialLinearVelocity, value); }
    /// <summary>Gets or sets the minimum AngularVelocity value; preserves ordered bounds.</summary>
    public float AngularVelocityMin { get => GetParamMin(Parameter.AngularVelocity); set => SetParamMin(Parameter.AngularVelocity, value); }
    /// <summary>Gets or sets the maximum AngularVelocity value; preserves ordered bounds.</summary>
    public float AngularVelocityMax { get => GetParamMax(Parameter.AngularVelocity); set => SetParamMax(Parameter.AngularVelocity, value); }
    /// <summary>Gets or sets the borrowed lifetime texture for AngularVelocity.</summary>
    public Texture? AngularVelocityCurve { get => GetParamTexture(Parameter.AngularVelocity); set => SetParamTexture(Parameter.AngularVelocity, value); }
    /// <summary>Gets or sets the minimum OrbitVelocity value; preserves ordered bounds.</summary>
    public float OrbitVelocityMin { get => GetParamMin(Parameter.OrbitVelocity); set => SetParamMin(Parameter.OrbitVelocity, value); }
    /// <summary>Gets or sets the maximum OrbitVelocity value; preserves ordered bounds.</summary>
    public float OrbitVelocityMax { get => GetParamMax(Parameter.OrbitVelocity); set => SetParamMax(Parameter.OrbitVelocity, value); }
    /// <summary>Gets or sets the borrowed lifetime texture for OrbitVelocity.</summary>
    public Texture? OrbitVelocityCurve { get => GetParamTexture(Parameter.OrbitVelocity); set => SetParamTexture(Parameter.OrbitVelocity, value); }
    /// <summary>Gets or sets the minimum LinearAccel value; preserves ordered bounds.</summary>
    public float LinearAccelMin { get => GetParamMin(Parameter.LinearAccel); set => SetParamMin(Parameter.LinearAccel, value); }
    /// <summary>Gets or sets the maximum LinearAccel value; preserves ordered bounds.</summary>
    public float LinearAccelMax { get => GetParamMax(Parameter.LinearAccel); set => SetParamMax(Parameter.LinearAccel, value); }
    /// <summary>Gets or sets the borrowed lifetime texture for LinearAccel.</summary>
    public Texture? LinearAccelCurve { get => GetParamTexture(Parameter.LinearAccel); set => SetParamTexture(Parameter.LinearAccel, value); }
    /// <summary>Gets or sets the minimum RadialAccel value; preserves ordered bounds.</summary>
    public float RadialAccelMin { get => GetParamMin(Parameter.RadialAccel); set => SetParamMin(Parameter.RadialAccel, value); }
    /// <summary>Gets or sets the maximum RadialAccel value; preserves ordered bounds.</summary>
    public float RadialAccelMax { get => GetParamMax(Parameter.RadialAccel); set => SetParamMax(Parameter.RadialAccel, value); }
    /// <summary>Gets or sets the borrowed lifetime texture for RadialAccel.</summary>
    public Texture? RadialAccelCurve { get => GetParamTexture(Parameter.RadialAccel); set => SetParamTexture(Parameter.RadialAccel, value); }
    /// <summary>Gets or sets the minimum TangentialAccel value; preserves ordered bounds.</summary>
    public float TangentialAccelMin { get => GetParamMin(Parameter.TangentialAccel); set => SetParamMin(Parameter.TangentialAccel, value); }
    /// <summary>Gets or sets the maximum TangentialAccel value; preserves ordered bounds.</summary>
    public float TangentialAccelMax { get => GetParamMax(Parameter.TangentialAccel); set => SetParamMax(Parameter.TangentialAccel, value); }
    /// <summary>Gets or sets the borrowed lifetime texture for TangentialAccel.</summary>
    public Texture? TangentialAccelCurve { get => GetParamTexture(Parameter.TangentialAccel); set => SetParamTexture(Parameter.TangentialAccel, value); }
    /// <summary>Gets or sets the minimum Damping value; preserves ordered bounds.</summary>
    public float DampingMin { get => GetParamMin(Parameter.Damping); set => SetParamMin(Parameter.Damping, value); }
    /// <summary>Gets or sets the maximum Damping value; preserves ordered bounds.</summary>
    public float DampingMax { get => GetParamMax(Parameter.Damping); set => SetParamMax(Parameter.Damping, value); }
    /// <summary>Gets or sets the borrowed lifetime texture for Damping.</summary>
    public Texture? DampingCurve { get => GetParamTexture(Parameter.Damping); set => SetParamTexture(Parameter.Damping, value); }
    /// <summary>Gets or sets the minimum Angle value; preserves ordered bounds.</summary>
    public float AngleMin { get => GetParamMin(Parameter.Angle); set => SetParamMin(Parameter.Angle, value); }
    /// <summary>Gets or sets the maximum Angle value; preserves ordered bounds.</summary>
    public float AngleMax { get => GetParamMax(Parameter.Angle); set => SetParamMax(Parameter.Angle, value); }
    /// <summary>Gets or sets the borrowed lifetime texture for Angle.</summary>
    public Texture? AngleCurve { get => GetParamTexture(Parameter.Angle); set => SetParamTexture(Parameter.Angle, value); }
    /// <summary>Gets or sets the minimum Scale value; preserves ordered bounds.</summary>
    public float ScaleMin { get => GetParamMin(Parameter.Scale); set => SetParamMin(Parameter.Scale, value); }
    /// <summary>Gets or sets the maximum Scale value; preserves ordered bounds.</summary>
    public float ScaleMax { get => GetParamMax(Parameter.Scale); set => SetParamMax(Parameter.Scale, value); }
    /// <summary>Gets or sets the borrowed lifetime texture for Scale.</summary>
    public Texture? ScaleCurve { get => GetParamTexture(Parameter.Scale); set => SetParamTexture(Parameter.Scale, value); }
    /// <summary>Gets or sets the minimum HueVariation value; preserves ordered bounds.</summary>
    public float HueVariationMin { get => GetParamMin(Parameter.HueVariation); set => SetParamMin(Parameter.HueVariation, value); }
    /// <summary>Gets or sets the maximum HueVariation value; preserves ordered bounds.</summary>
    public float HueVariationMax { get => GetParamMax(Parameter.HueVariation); set => SetParamMax(Parameter.HueVariation, value); }
    /// <summary>Gets or sets the borrowed lifetime texture for HueVariation.</summary>
    public Texture? HueVariationCurve { get => GetParamTexture(Parameter.HueVariation); set => SetParamTexture(Parameter.HueVariation, value); }
    /// <summary>Gets or sets the minimum AnimSpeed value; preserves ordered bounds.</summary>
    public float AnimSpeedMin { get => GetParamMin(Parameter.AnimSpeed); set => SetParamMin(Parameter.AnimSpeed, value); }
    /// <summary>Gets or sets the maximum AnimSpeed value; preserves ordered bounds.</summary>
    public float AnimSpeedMax { get => GetParamMax(Parameter.AnimSpeed); set => SetParamMax(Parameter.AnimSpeed, value); }
    /// <summary>Gets or sets the borrowed lifetime texture for AnimSpeed.</summary>
    public Texture? AnimSpeedCurve { get => GetParamTexture(Parameter.AnimSpeed); set => SetParamTexture(Parameter.AnimSpeed, value); }
    /// <summary>Gets or sets the minimum AnimOffset value; preserves ordered bounds.</summary>
    public float AnimOffsetMin { get => GetParamMin(Parameter.AnimOffset); set => SetParamMin(Parameter.AnimOffset, value); }
    /// <summary>Gets or sets the maximum AnimOffset value; preserves ordered bounds.</summary>
    public float AnimOffsetMax { get => GetParamMax(Parameter.AnimOffset); set => SetParamMax(Parameter.AnimOffset, value); }
    /// <summary>Gets or sets the borrowed lifetime texture for AnimOffset.</summary>
    public Texture? AnimOffsetCurve { get => GetParamTexture(Parameter.AnimOffset); set => SetParamTexture(Parameter.AnimOffset, value); }
    /// <summary>Gets or sets the minimum TurbulenceInfluence value; preserves ordered bounds.</summary>
    public float TurbulenceInfluenceMin { get => GetParamMin(Parameter.TurbVelocityInfluence); set => SetParamMin(Parameter.TurbVelocityInfluence, value); }
    /// <summary>Gets or sets the maximum TurbulenceInfluence value; preserves ordered bounds.</summary>
    public float TurbulenceInfluenceMax { get => GetParamMax(Parameter.TurbVelocityInfluence); set => SetParamMax(Parameter.TurbVelocityInfluence, value); }
    /// <summary>Gets or sets the minimum TurbulenceInitialDisplacement value; preserves ordered bounds.</summary>
    public float TurbulenceInitialDisplacementMin { get => GetParamMin(Parameter.TurbInitialDisplacement); set => SetParamMin(Parameter.TurbInitialDisplacement, value); }
    /// <summary>Gets or sets the maximum TurbulenceInitialDisplacement value; preserves ordered bounds.</summary>
    public float TurbulenceInitialDisplacementMax { get => GetParamMax(Parameter.TurbInitialDisplacement); set => SetParamMax(Parameter.TurbInitialDisplacement, value); }
    /// <summary>Gets or sets the minimum RadialVelocity value; preserves ordered bounds.</summary>
    public float RadialVelocityMin { get => GetParamMin(Parameter.RadialVelocity); set => SetParamMin(Parameter.RadialVelocity, value); }
    /// <summary>Gets or sets the maximum RadialVelocity value; preserves ordered bounds.</summary>
    public float RadialVelocityMax { get => GetParamMax(Parameter.RadialVelocity); set => SetParamMax(Parameter.RadialVelocity, value); }
    /// <summary>Gets or sets the borrowed lifetime texture for RadialVelocity.</summary>
    public Texture? RadialVelocityCurve { get => GetParamTexture(Parameter.RadialVelocity); set => SetParamTexture(Parameter.RadialVelocity, value); }
    /// <summary>Gets or sets the minimum DirectionalVelocity value; preserves ordered bounds.</summary>
    public float DirectionalVelocityMin { get => GetParamMin(Parameter.DirectionalVelocity); set => SetParamMin(Parameter.DirectionalVelocity, value); }
    /// <summary>Gets or sets the maximum DirectionalVelocity value; preserves ordered bounds.</summary>
    public float DirectionalVelocityMax { get => GetParamMax(Parameter.DirectionalVelocity); set => SetParamMax(Parameter.DirectionalVelocity, value); }
    /// <summary>Gets or sets the borrowed lifetime texture for DirectionalVelocity.</summary>
    public Texture? DirectionalVelocityCurve { get => GetParamTexture(Parameter.DirectionalVelocity); set => SetParamTexture(Parameter.DirectionalVelocity, value); }
    /// <summary>Gets or sets the minimum ScaleOverVelocity value; preserves ordered bounds.</summary>
    public float ScaleOverVelocityMin { get => GetParamMin(Parameter.ScaleOverVelocity); set => SetParamMin(Parameter.ScaleOverVelocity, value); }
    /// <summary>Gets or sets the maximum ScaleOverVelocity value; preserves ordered bounds.</summary>
    public float ScaleOverVelocityMax { get => GetParamMax(Parameter.ScaleOverVelocity); set => SetParamMax(Parameter.ScaleOverVelocity, value); }
    /// <summary>Gets or sets the borrowed lifetime texture for ScaleOverVelocity.</summary>
    public Texture? ScaleOverVelocityCurve { get => GetParamTexture(Parameter.ScaleOverVelocity); set => SetParamTexture(Parameter.ScaleOverVelocity, value); }
    /// <summary>Gets or sets the borrowed AlphaCurve texture.</summary>
    public Texture? AlphaCurve { get => ReadCurve(18); set => SetCurve(18, value); }
    /// <summary>Gets or sets the borrowed EmissionCurve texture.</summary>
    public Texture? EmissionCurve { get => ReadCurve(19); set => SetCurve(19, value); }
    /// <summary>Gets or sets the borrowed ColorRamp texture.</summary>
    public Texture? ColorRamp { get => ReadCurve(20); set => SetCurve(20, value); }
    /// <summary>Gets or sets the borrowed ColorInitialRamp texture.</summary>
    public Texture? ColorInitialRamp { get => ReadCurve(21); set => SetCurve(21, value); }
    /// <summary>Gets or sets the borrowed VelocityLimitCurve texture.</summary>
    public Texture? VelocityLimitCurve { get => ReadCurve(22); set => SetCurve(22, value); }
    /// <summary>Gets or sets the borrowed TurbulenceInfluenceOverLife texture.</summary>
    public Texture? TurbulenceInfluenceOverLife { get => ReadCurve(12); set => SetCurve(12, value); }
    /// <summary>Gets or sets the AlignYToVelocity policy.</summary>
    public bool ParticleFlagAlignY { get => GetParticleFlag(ParticleFlags.AlignYToVelocity); set => SetParticleFlag(ParticleFlags.AlignYToVelocity, value); }
    /// <summary>Gets or sets the DampingAsFriction policy.</summary>
    public bool ParticleFlagDampingAsFriction { get => GetParticleFlag(ParticleFlags.DampingAsFriction); set => SetParticleFlag(ParticleFlags.DampingAsFriction, value); }
    /// <summary>Gets or sets the InheritEmitterScale policy.</summary>
    public bool ParticleFlagInheritEmitterScale { get => GetParticleFlag(ParticleFlags.InheritEmitterScale); set => SetParticleFlag(ParticleFlags.InheritEmitterScale, value); }
}
