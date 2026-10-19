namespace Electron2D;

public sealed partial class ParticleProcessMaterial
{
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<ParticleProcessMaterial, Vector2>(nameof(Direction), p => p.Direction, (p, v) => p.Direction = v, _ => new(1, 0), stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Vector2>(nameof(Gravity), p => p.Gravity, (p, v) => p.Gravity = v, _ => new(0, -9.8f), stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(Spread), p => p.Spread, (p, v) => p.Spread = v, _ => 45, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(LifetimeRandomness), p => p.LifetimeRandomness, (p, v) => p.LifetimeRandomness = v, _ => 0, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(InheritVelocityRatio), p => p.InheritVelocityRatio, (p, v) => p.InheritVelocityRatio = v, _ => 0, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, ParticlesEmissionShape>(nameof(EmissionShape), p => p.EmissionShape, (p, v) => p.EmissionShape = v, _ => ParticlesEmissionShape.Point, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Vector2>(nameof(EmissionShapeOffset), p => p.EmissionShapeOffset, (p, v) => p.EmissionShapeOffset = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Vector2>(nameof(EmissionShapeScale), p => p.EmissionShapeScale, (p, v) => p.EmissionShapeScale = v, _ => Vector2.One, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Vector2>(nameof(EmissionRectExtents), p => p.EmissionRectExtents, (p, v) => p.EmissionRectExtents = v, _ => Vector2.One, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(EmissionSphereRadius), p => p.EmissionSphereRadius, (p, v) => p.EmissionSphereRadius = v, _ => 1, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(EmissionRingRadius), p => p.EmissionRingRadius, (p, v) => p.EmissionRingRadius = v, _ => 1, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(EmissionRingInnerRadius), p => p.EmissionRingInnerRadius, (p, v) => p.EmissionRingInnerRadius = v, _ => 0, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, int>(nameof(EmissionPointCount), p => p.EmissionPointCount, (p, v) => p.EmissionPointCount = v, _ => 1, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(EmissionPointTexture), p => p.EmissionPointTexture, (p, v) => p.EmissionPointTexture = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(EmissionNormalTexture), p => p.EmissionNormalTexture, (p, v) => p.EmissionNormalTexture = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(EmissionColorTexture), p => p.EmissionColorTexture, (p, v) => p.EmissionColorTexture = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Color>(nameof(Color), p => p.Color, (p, v) => p.Color = v, _ => Colors.White, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Vector2>(nameof(VelocityPivot), p => p.VelocityPivot, (p, v) => p.VelocityPivot = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, bool>(nameof(TurbulenceEnabled), p => p.TurbulenceEnabled, (p, v) => p.TurbulenceEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(TurbulenceNoiseStrength), p => p.TurbulenceNoiseStrength, (p, v) => p.TurbulenceNoiseStrength = v, _ => 1, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(TurbulenceNoiseScale), p => p.TurbulenceNoiseScale, (p, v) => p.TurbulenceNoiseScale = v, _ => 9, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Vector3>(nameof(TurbulenceNoiseSpeed), p => p.TurbulenceNoiseSpeed, (p, v) => p.TurbulenceNoiseSpeed = v, _ => Vector3.Zero, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(TurbulenceNoiseSpeedRandom), p => p.TurbulenceNoiseSpeedRandom, (p, v) => p.TurbulenceNoiseSpeedRandom = v, _ => .2f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, CollisionModeEnum>(nameof(CollisionMode), p => p.CollisionMode, (p, v) => p.CollisionMode = v, _ => CollisionModeEnum.Disabled, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(CollisionFriction), p => p.CollisionFriction, (p, v) => p.CollisionFriction = v, _ => 0, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(CollisionBounce), p => p.CollisionBounce, (p, v) => p.CollisionBounce = v, _ => 0, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, bool>(nameof(CollisionUseScale), p => p.CollisionUseScale, (p, v) => p.CollisionUseScale = v, _ => false, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, SubEmitterModeEnum>(nameof(SubEmitterMode), p => p.SubEmitterMode, (p, v) => p.SubEmitterMode = v, _ => SubEmitterModeEnum.Disabled, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(SubEmitterFrequency), p => p.SubEmitterFrequency, (p, v) => p.SubEmitterFrequency = v, _ => 4, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, int>(nameof(SubEmitterAmountAtStart), p => p.SubEmitterAmountAtStart, (p, v) => p.SubEmitterAmountAtStart = v, _ => 1, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, int>(nameof(SubEmitterAmountAtEnd), p => p.SubEmitterAmountAtEnd, (p, v) => p.SubEmitterAmountAtEnd = v, _ => 1, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, int>(nameof(SubEmitterAmountAtCollision), p => p.SubEmitterAmountAtCollision, (p, v) => p.SubEmitterAmountAtCollision = v, _ => 1, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, bool>(nameof(SubEmitterKeepVelocity), p => p.SubEmitterKeepVelocity, (p, v) => p.SubEmitterKeepVelocity = v, _ => false, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(InitialVelocityMin), p => p.InitialVelocityMin, (p, v) => p.InitialVelocityMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(InitialVelocityMax), p => p.InitialVelocityMax, (p, v) => p.InitialVelocityMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(AngularVelocityMin), p => p.AngularVelocityMin, (p, v) => p.AngularVelocityMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(AngularVelocityMax), p => p.AngularVelocityMax, (p, v) => p.AngularVelocityMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(AngularVelocityCurve), p => p.AngularVelocityCurve, (p, v) => p.AngularVelocityCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(OrbitVelocityMin), p => p.OrbitVelocityMin, (p, v) => p.OrbitVelocityMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(OrbitVelocityMax), p => p.OrbitVelocityMax, (p, v) => p.OrbitVelocityMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(OrbitVelocityCurve), p => p.OrbitVelocityCurve, (p, v) => p.OrbitVelocityCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(LinearAccelMin), p => p.LinearAccelMin, (p, v) => p.LinearAccelMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(LinearAccelMax), p => p.LinearAccelMax, (p, v) => p.LinearAccelMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(LinearAccelCurve), p => p.LinearAccelCurve, (p, v) => p.LinearAccelCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(RadialAccelMin), p => p.RadialAccelMin, (p, v) => p.RadialAccelMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(RadialAccelMax), p => p.RadialAccelMax, (p, v) => p.RadialAccelMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(RadialAccelCurve), p => p.RadialAccelCurve, (p, v) => p.RadialAccelCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(TangentialAccelMin), p => p.TangentialAccelMin, (p, v) => p.TangentialAccelMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(TangentialAccelMax), p => p.TangentialAccelMax, (p, v) => p.TangentialAccelMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(TangentialAccelCurve), p => p.TangentialAccelCurve, (p, v) => p.TangentialAccelCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(DampingMin), p => p.DampingMin, (p, v) => p.DampingMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(DampingMax), p => p.DampingMax, (p, v) => p.DampingMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(DampingCurve), p => p.DampingCurve, (p, v) => p.DampingCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(AngleMin), p => p.AngleMin, (p, v) => p.AngleMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(AngleMax), p => p.AngleMax, (p, v) => p.AngleMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(AngleCurve), p => p.AngleCurve, (p, v) => p.AngleCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(ScaleMin), p => p.ScaleMin, (p, v) => p.ScaleMin = v, _ => 1f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(ScaleMax), p => p.ScaleMax, (p, v) => p.ScaleMax = v, _ => 1f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(ScaleCurve), p => p.ScaleCurve, (p, v) => p.ScaleCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(HueVariationMin), p => p.HueVariationMin, (p, v) => p.HueVariationMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(HueVariationMax), p => p.HueVariationMax, (p, v) => p.HueVariationMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(HueVariationCurve), p => p.HueVariationCurve, (p, v) => p.HueVariationCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(AnimSpeedMin), p => p.AnimSpeedMin, (p, v) => p.AnimSpeedMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(AnimSpeedMax), p => p.AnimSpeedMax, (p, v) => p.AnimSpeedMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(AnimSpeedCurve), p => p.AnimSpeedCurve, (p, v) => p.AnimSpeedCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(AnimOffsetMin), p => p.AnimOffsetMin, (p, v) => p.AnimOffsetMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(AnimOffsetMax), p => p.AnimOffsetMax, (p, v) => p.AnimOffsetMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(AnimOffsetCurve), p => p.AnimOffsetCurve, (p, v) => p.AnimOffsetCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(TurbulenceInfluenceMin), p => p.TurbulenceInfluenceMin, (p, v) => p.TurbulenceInfluenceMin = v, _ => .1f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(TurbulenceInfluenceMax), p => p.TurbulenceInfluenceMax, (p, v) => p.TurbulenceInfluenceMax = v, _ => .1f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(TurbulenceInitialDisplacementMin), p => p.TurbulenceInitialDisplacementMin, (p, v) => p.TurbulenceInitialDisplacementMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(TurbulenceInitialDisplacementMax), p => p.TurbulenceInitialDisplacementMax, (p, v) => p.TurbulenceInitialDisplacementMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(RadialVelocityMin), p => p.RadialVelocityMin, (p, v) => p.RadialVelocityMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(RadialVelocityMax), p => p.RadialVelocityMax, (p, v) => p.RadialVelocityMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(RadialVelocityCurve), p => p.RadialVelocityCurve, (p, v) => p.RadialVelocityCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(DirectionalVelocityMin), p => p.DirectionalVelocityMin, (p, v) => p.DirectionalVelocityMin = v, _ => 1f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(DirectionalVelocityMax), p => p.DirectionalVelocityMax, (p, v) => p.DirectionalVelocityMax = v, _ => 1f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(DirectionalVelocityCurve), p => p.DirectionalVelocityCurve, (p, v) => p.DirectionalVelocityCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(ScaleOverVelocityMin), p => p.ScaleOverVelocityMin, (p, v) => p.ScaleOverVelocityMin = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, float>(nameof(ScaleOverVelocityMax), p => p.ScaleOverVelocityMax, (p, v) => p.ScaleOverVelocityMax = v, _ => 0f, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(ScaleOverVelocityCurve), p => p.ScaleOverVelocityCurve, (p, v) => p.ScaleOverVelocityCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(AlphaCurve), p => p.AlphaCurve, (p, v) => p.AlphaCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(EmissionCurve), p => p.EmissionCurve, (p, v) => p.EmissionCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(ColorRamp), p => p.ColorRamp, (p, v) => p.ColorRamp = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(ColorInitialRamp), p => p.ColorInitialRamp, (p, v) => p.ColorInitialRamp = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(VelocityLimitCurve), p => p.VelocityLimitCurve, (p, v) => p.VelocityLimitCurve = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, Texture?>(nameof(TurbulenceInfluenceOverLife), p => p.TurbulenceInfluenceOverLife, (p, v) => p.TurbulenceInfluenceOverLife = v, _ => null, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, bool>(nameof(ParticleFlagAlignY), p => p.ParticleFlagAlignY, (p, v) => p.ParticleFlagAlignY = v, _ => false, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, bool>(nameof(ParticleFlagDampingAsFriction), p => p.ParticleFlagDampingAsFriction, (p, v) => p.ParticleFlagDampingAsFriction = v, _ => false, stored: true),
        new PropertyDescriptor<ParticleProcessMaterial, bool>(nameof(ParticleFlagInheritEmitterScale), p => p.ParticleFlagInheritEmitterScale, (p, v) => p.ParticleFlagInheritEmitterScale = v, _ => false, stored: true),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new ParticleProcessMaterial();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            foreach (var property in Properties)
                property.RestoreStoredValue(target, property.CaptureStoredValue(this), resource => deep ? duplicateSubresource(resource)! : resource);
        }
    }
}
