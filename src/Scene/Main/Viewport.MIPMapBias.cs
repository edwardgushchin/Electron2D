namespace Electron2D;

public abstract partial class Viewport
{
    private float _textureMIPMapBias;
    private bool _textureMIPMapBiasAssigned;
    internal void ApplyProjectMIPMapBias()
    { if (!_textureMIPMapBiasAssigned) _textureMIPMapBias = ProjectSettings.GetWithOverride(ProjectSettings.TextureMIPMapBias); }

    /// <summary>Gets or sets the texture mip-level offset used by this viewport's canvas.</summary>
    /// <value>Zero initially. An unassigned root window samples ProjectSettings.TextureMIPMapBias at startup.</value>
    /// <remarks>Negative offsets sharpen minified mipmapped textures; positive offsets select coarser levels.
    /// The next submitted image uses the value without replacing its target. Imported image policies, ordinary
    /// canvas images and normalized explicit mip samplers share the offset; explicit sampler LODBias is added.
    /// Base-level filters and unnormalized samplers retain their original policy. Valid assignments, including
    /// equal values, republish renderer overrides. Compatibility and Metal reject nonzero offsets; other GPU
    /// drivers may reject a combined offset beyond their native sampler limit. Explicit shader LOD operations
    /// retain native sampling semantics. Independent device and particle-process samplers are unaffected.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is not finite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during capture/submission.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public float TextureMIPMapBias
    {
        get { CheckTransformQuery(); return _textureMIPMapBias; }
        set
        {
            EnsureMutable(); RenderingOwner?.EnsureServerViewportChange();
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            _textureMIPMapBias = value; _textureMIPMapBiasAssigned = true;
            RenderingOwner?.PublishViewportMIPMapBias(this);
        }
    }

    private static readonly PropertyDescriptor[] MIPMapBiasProperties =
    [new PropertyDescriptor<Viewport, float>(nameof(TextureMIPMapBias), n => n.TextureMIPMapBias,
        (n, v) => n.TextureMIPMapBias = v, _ => 0, (_, v) => float.IsFinite(v), stored: true)];
}
