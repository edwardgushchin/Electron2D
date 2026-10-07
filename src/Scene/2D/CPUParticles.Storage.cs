namespace Electron2D;

public partial class CPUParticles
{
    private static readonly PropertyDescriptor[] ParticleProperties =
    [
        new PropertyDescriptor<CPUParticles, Curve?>("_initial_velocity_curve", p => p.GetParamCurve(Parameter.InitialLinearVelocity), (p, v) => p.SetParamCurve(Parameter.InitialLinearVelocity, v), _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, int>(nameof(Amount), p => p.Amount, (p, v) => p.Amount = v, _ => 8, stored: true),
        new PropertyDescriptor<CPUParticles, bool>(nameof(Emitting), p => p.Emitting, (p, v) => p.Emitting = v, _ => true, stored: true),
        new PropertyDescriptor<CPUParticles, double>(nameof(Lifetime), p => p.Lifetime, (p, v) => p.Lifetime = v, _ => 1d, stored: true),
        new PropertyDescriptor<CPUParticles, int>(nameof(FixedFPS), p => p.FixedFPS, (p, v) => p.FixedFPS = v, _ => 0, stored: true),
        new PropertyDescriptor<CPUParticles, DrawOrderMode>(nameof(DrawOrder), p => p.DrawOrder, (p, v) => p.DrawOrder = v, _ => DrawOrderMode.Index, stored: true),
        new PropertyDescriptor<CPUParticles, EmissionShapeMode>(nameof(EmissionShape), p => p.EmissionShape, (p, v) => p.EmissionShape = v, _ => EmissionShapeMode.Point, stored: true),
        new PropertyDescriptor<CPUParticles, Texture?>(nameof(Texture), p => p.Texture, (p, v) => p.Texture = v, _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, Vector2[]>(nameof(EmissionPoints), p => p.EmissionPoints, (p, v) => p.EmissionPoints = v, _ => [], stored: true),
        new PropertyDescriptor<CPUParticles, Vector2[]>(nameof(EmissionNormals), p => p.EmissionNormals, (p, v) => p.EmissionNormals = v, _ => [], stored: true),
        new PropertyDescriptor<CPUParticles, Color[]>(nameof(EmissionColors), p => p.EmissionColors, (p, v) => p.EmissionColors = v, _ => [], stored: true),
        new PropertyDescriptor<CPUParticles, bool>(nameof(OneShot), p => p.OneShot, (p, v) => p.OneShot = v, _ => false, stored: true),
        new PropertyDescriptor<CPUParticles, bool>(nameof(LocalCoords), p => p.LocalCoords, (p, v) => p.LocalCoords = v, _ => false, stored: true),
        new PropertyDescriptor<CPUParticles, bool>(nameof(FractDelta), p => p.FractDelta, (p, v) => p.FractDelta = v, _ => true, stored: true),
        new PropertyDescriptor<CPUParticles, bool>(nameof(UseFixedSeed), p => p.UseFixedSeed, (p, v) => p.UseFixedSeed = v, _ => false, stored: true),
        new PropertyDescriptor<CPUParticles, bool>(nameof(SplitScale), p => p.SplitScale, (p, v) => p.SplitScale = v, _ => false, stored: true),
        new PropertyDescriptor<CPUParticles, bool>(nameof(ParticleFlagAlignY), p => p.ParticleFlagAlignY, (p, v) => p.ParticleFlagAlignY = v, _ => false, stored: true),
        new PropertyDescriptor<CPUParticles, uint>(nameof(Seed), p => p.Seed, (p, v) => p.Seed = v, _ => 0u, stored: true),
        new PropertyDescriptor<CPUParticles, Vector2>(nameof(Direction), p => p.Direction, (p, v) => p.Direction = v, _ => new Vector2(1,0), stored: true),
        new PropertyDescriptor<CPUParticles, Vector2>(nameof(Gravity), p => p.Gravity, (p, v) => p.Gravity = v, _ => new Vector2(0,980), stored: true),
        new PropertyDescriptor<CPUParticles, Vector2>(nameof(EmissionRectExtents), p => p.EmissionRectExtents, (p, v) => p.EmissionRectExtents = v, _ => Vector2.One, stored: true),
        new PropertyDescriptor<CPUParticles, Color>(nameof(Color), p => p.Color, (p, v) => p.Color = v, _ => Colors.White, stored: true),
        new PropertyDescriptor<CPUParticles, Gradient?>(nameof(ColorRamp), p => p.ColorRamp, (p, v) => p.ColorRamp = v, _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, Gradient?>(nameof(ColorInitialRamp), p => p.ColorInitialRamp, (p, v) => p.ColorInitialRamp = v, _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, Curve?>(nameof(ScaleCurveX), p => p.ScaleCurveX, (p, v) => p.ScaleCurveX = v, _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, Curve?>(nameof(ScaleCurveY), p => p.ScaleCurveY, (p, v) => p.ScaleCurveY = v, _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, double>(nameof(Preprocess), p => p.Preprocess, (p, v) => p.Preprocess = v, _ => 0d, stored: true),
        new PropertyDescriptor<CPUParticles, double>(nameof(SpeedScale), p => p.SpeedScale, (p, v) => p.SpeedScale = v, _ => 1d, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(Explosiveness), p => p.Explosiveness, (p, v) => p.Explosiveness = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(Randomness), p => p.Randomness, (p, v) => p.Randomness = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(LifetimeRandomness), p => p.LifetimeRandomness, (p, v) => p.LifetimeRandomness = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(Spread), p => p.Spread, (p, v) => p.Spread = v, _ => 45, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(EmissionSphereRadius), p => p.EmissionSphereRadius, (p, v) => p.EmissionSphereRadius = v, _ => 1f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(EmissionRingRadius), p => p.EmissionRingRadius, (p, v) => p.EmissionRingRadius = v, _ => 1f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(EmissionRingInnerRadius), p => p.EmissionRingInnerRadius, (p, v) => p.EmissionRingInnerRadius = v, _ => .8f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(InitialVelocityMin), p => p.InitialVelocityMin, (p, v) => p.InitialVelocityMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(InitialVelocityMax), p => p.InitialVelocityMax, (p, v) => p.InitialVelocityMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(AngularVelocityMin), p => p.AngularVelocityMin, (p, v) => p.AngularVelocityMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(AngularVelocityMax), p => p.AngularVelocityMax, (p, v) => p.AngularVelocityMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, Curve?>(nameof(AngularVelocityCurve), p => p.AngularVelocityCurve, (p, v) => p.AngularVelocityCurve = v, _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(OrbitVelocityMin), p => p.OrbitVelocityMin, (p, v) => p.OrbitVelocityMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(OrbitVelocityMax), p => p.OrbitVelocityMax, (p, v) => p.OrbitVelocityMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, Curve?>(nameof(OrbitVelocityCurve), p => p.OrbitVelocityCurve, (p, v) => p.OrbitVelocityCurve = v, _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(LinearAccelMin), p => p.LinearAccelMin, (p, v) => p.LinearAccelMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(LinearAccelMax), p => p.LinearAccelMax, (p, v) => p.LinearAccelMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, Curve?>(nameof(LinearAccelCurve), p => p.LinearAccelCurve, (p, v) => p.LinearAccelCurve = v, _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(RadialAccelMin), p => p.RadialAccelMin, (p, v) => p.RadialAccelMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(RadialAccelMax), p => p.RadialAccelMax, (p, v) => p.RadialAccelMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, Curve?>(nameof(RadialAccelCurve), p => p.RadialAccelCurve, (p, v) => p.RadialAccelCurve = v, _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(TangentialAccelMin), p => p.TangentialAccelMin, (p, v) => p.TangentialAccelMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(TangentialAccelMax), p => p.TangentialAccelMax, (p, v) => p.TangentialAccelMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, Curve?>(nameof(TangentialAccelCurve), p => p.TangentialAccelCurve, (p, v) => p.TangentialAccelCurve = v, _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(DampingMin), p => p.DampingMin, (p, v) => p.DampingMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(DampingMax), p => p.DampingMax, (p, v) => p.DampingMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, Curve?>(nameof(DampingCurve), p => p.DampingCurve, (p, v) => p.DampingCurve = v, _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(AngleMin), p => p.AngleMin, (p, v) => p.AngleMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(AngleMax), p => p.AngleMax, (p, v) => p.AngleMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, Curve?>(nameof(AngleCurve), p => p.AngleCurve, (p, v) => p.AngleCurve = v, _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(ScaleAmountMin), p => p.ScaleAmountMin, (p, v) => p.ScaleAmountMin = v, _ => 1f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(ScaleAmountMax), p => p.ScaleAmountMax, (p, v) => p.ScaleAmountMax = v, _ => 1f, stored: true),
        new PropertyDescriptor<CPUParticles, Curve?>(nameof(ScaleAmountCurve), p => p.ScaleAmountCurve, (p, v) => p.ScaleAmountCurve = v, _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(HueVariationMin), p => p.HueVariationMin, (p, v) => p.HueVariationMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(HueVariationMax), p => p.HueVariationMax, (p, v) => p.HueVariationMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, Curve?>(nameof(HueVariationCurve), p => p.HueVariationCurve, (p, v) => p.HueVariationCurve = v, _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(AnimSpeedMin), p => p.AnimSpeedMin, (p, v) => p.AnimSpeedMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(AnimSpeedMax), p => p.AnimSpeedMax, (p, v) => p.AnimSpeedMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, Curve?>(nameof(AnimSpeedCurve), p => p.AnimSpeedCurve, (p, v) => p.AnimSpeedCurve = v, _ => null, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(AnimOffsetMin), p => p.AnimOffsetMin, (p, v) => p.AnimOffsetMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, float>(nameof(AnimOffsetMax), p => p.AnimOffsetMax, (p, v) => p.AnimOffsetMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<CPUParticles, Curve?>(nameof(AnimOffsetCurve), p => p.AnimOffsetCurve, (p, v) => p.AnimOffsetCurve = v, _ => null, stored: true),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(ParticleProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(CPUParticles) ? CreateParticles : base.CreateSceneInstanceFactory();
    private static Node CreateParticles() => new CPUParticles();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) { if (_texture is not null) _texture.Changed -= TextureChanged; _texture = null; _ramp = _initialRamp = null; _scaleX = _scaleY = null; Array.Clear(_curves); Finished = null; _particles = _scratch = []; _points = _normals = []; _colors = []; _order = []; _rng.Dispose(); }
        base.Dispose(disposing);
    }
}
