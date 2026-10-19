namespace Electron2D;

public partial class GPUParticles
{
    private static readonly PropertyDescriptor[] ParticleProperties =
    [
        new PropertyDescriptor<GPUParticles, int>(nameof(Amount), p => p.Amount, (p, v) => p.Amount = v, _ => 8, stored: true),
        new PropertyDescriptor<GPUParticles, float>(nameof(AmountRatio), p => p.AmountRatio, (p, v) => p.AmountRatio = v, _ => 1f, stored: true),
        new PropertyDescriptor<GPUParticles, float>(nameof(CollisionBaseSize), p => p.CollisionBaseSize, (p, v) => p.CollisionBaseSize = v, _ => 1f, stored: true),
        new PropertyDescriptor<GPUParticles, ParticlesDrawOrder>(nameof(DrawOrder), p => p.DrawOrder, (p, v) => p.DrawOrder = v, _ => ParticlesDrawOrder.Lifetime, stored: true),
        new PropertyDescriptor<GPUParticles, bool>(nameof(Emitting), p => p.Emitting, (p, v) => p.Emitting = v, _ => true, stored: true),
        new PropertyDescriptor<GPUParticles, float>(nameof(Explosiveness), p => p.Explosiveness, (p, v) => p.Explosiveness = v, _ => 0f, stored: true),
        new PropertyDescriptor<GPUParticles, int>(nameof(FixedFPS), p => p.FixedFPS, (p, v) => p.FixedFPS = v, _ => 30, stored: true),
        new PropertyDescriptor<GPUParticles, bool>(nameof(FractDelta), p => p.FractDelta, (p, v) => p.FractDelta = v, _ => true, stored: true),
        new PropertyDescriptor<GPUParticles, float>(nameof(InterpToEnd), p => p.InterpToEnd, (p, v) => p.InterpToEnd = v, _ => 0f, stored: true),
        new PropertyDescriptor<GPUParticles, bool>(nameof(Interpolate), p => p.Interpolate, (p, v) => p.Interpolate = v, _ => true, stored: true),
        new PropertyDescriptor<GPUParticles, double>(nameof(Lifetime), p => p.Lifetime, (p, v) => p.Lifetime = v, _ => 1d, stored: true),
        new PropertyDescriptor<GPUParticles, bool>(nameof(LocalCoords), p => p.LocalCoords, (p, v) => p.LocalCoords = v, _ => false, stored: true),
        new PropertyDescriptor<GPUParticles, bool>(nameof(OneShot), p => p.OneShot, (p, v) => p.OneShot = v, _ => false, stored: true),
        new PropertyDescriptor<GPUParticles, double>(nameof(Preprocess), p => p.Preprocess, (p, v) => p.Preprocess = v, _ => 0d, stored: true),
        new PropertyDescriptor<GPUParticles, Material?>(nameof(ProcessMaterial), p => p.ProcessMaterial, (p, v) => p.ProcessMaterial = v, _ => null, stored: true),
        new PropertyDescriptor<GPUParticles, float>(nameof(Randomness), p => p.Randomness, (p, v) => p.Randomness = v, _ => 0f, stored: true),
        new PropertyDescriptor<GPUParticles, uint>(nameof(Seed), p => p.Seed, (p, v) => p.Seed = v, _ => 0u, stored: true),
        new PropertyDescriptor<GPUParticles, double>(nameof(SpeedScale), p => p.SpeedScale, (p, v) => p.SpeedScale = v, _ => 1d, stored: true),
        new PropertyDescriptor<GPUParticles, string>(nameof(SubEmitter), p => p.SubEmitter, (p, v) => p.SubEmitter = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<GPUParticles, Texture?>(nameof(Texture), p => p.Texture, (p, v) => p.Texture = v, _ => null, stored: true),
        new PropertyDescriptor<GPUParticles, bool>(nameof(TrailEnabled), p => p.TrailEnabled, (p, v) => p.TrailEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<GPUParticles, double>(nameof(TrailLifetime), p => p.TrailLifetime, (p, v) => p.TrailLifetime = v, _ => .3d, stored: true),
        new PropertyDescriptor<GPUParticles, int>(nameof(TrailSectionSubdivisions), p => p.TrailSectionSubdivisions, (p, v) => p.TrailSectionSubdivisions = v, _ => 4, stored: true),
        new PropertyDescriptor<GPUParticles, int>(nameof(TrailSections), p => p.TrailSections, (p, v) => p.TrailSections = v, _ => 8, stored: true),
        new PropertyDescriptor<GPUParticles, bool>(nameof(UseFixedSeed), p => p.UseFixedSeed, (p, v) => p.UseFixedSeed = v, _ => false, stored: true),
        new PropertyDescriptor<GPUParticles, Rect2>(nameof(VisibilityRect), p => p.VisibilityRect, (p, v) => p.VisibilityRect = v, _ => new(-100, -100, 200, 200), stored: true),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(ParticleProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(GPUParticles) ? CreateParticles : base.CreateSceneInstanceFactory();
    private static Node CreateParticles() => new GPUParticles();
}
