namespace Electron2D;

/// <summary>Selects ambient and light contribution for canvas drawing.</summary>
public enum LightMode
{
    /// <summary>Combines ambient canvas modulation with illumination.</summary>
    Normal = 0,
    /// <summary>Uses item color independently of ambient modulation and lights.</summary>
    Unshaded = 1,
    /// <summary>Uses illumination without ambient contribution.</summary>
    LightOnly = 2,
}

/// <summary>Defines how source color and alpha combine with the canvas target.</summary>
public enum BlendMode
{
    /// <summary>Normal source-over alpha blending.</summary>
    Mix = 0,
    /// <summary>Adds source color weighted by its alpha.</summary>
    Add = 1,
    /// <summary>Subtracts source color weighted by its alpha from the target.</summary>
    Sub = 2,
    /// <summary>Multiplies source and target components.</summary>
    Mul = 3,
    /// <summary>Blends source color that is already multiplied by its alpha.</summary>
    PremultAlpha = 4,
}

/// <summary>Applies a fixed blend mode to canvas geometry and textures.</summary>
/// <remarks>Nodes borrow this resource. The active mode is read each frame without re-recording draw commands.</remarks>
public sealed partial class CanvasItemMaterial : Material
{
    private readonly object _gate = new();
    private BlendMode _blendMode;
    private LightMode _lightMode;

    /// <summary>Creates a material with normal alpha blending.</summary>
    public CanvasItemMaterial() { }

    /// <summary>Gets or sets ambient and light-pass participation.</summary>
    /// <value>Normal initially; existing commands read the mode each frame.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is undefined.</exception>
    public LightMode LightMode
    {
        get { lock (_gate) { ThrowIfDisposed(); return _lightMode; } }
        set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_lightMode == value) return; _lightMode = value; } EmitChanged(); }
    }

    /// <summary>Gets or sets the canvas blend mode.</summary>
    /// <value><see cref="BlendMode.Mix"/> by default.</value>
    /// <remarks>Successful assignments raise Changed after committing the value. Existing geometry uses the new value on the next frame.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a defined blend mode.</exception>
    /// <exception cref="ObjectDisposedException">The material is disposed.</exception>
    public BlendMode BlendMode
    {
        get { lock (_gate) { ThrowIfDisposed(); return _blendMode; } }
        set
        {
            if (value is < BlendMode.Mix or > BlendMode.PremultAlpha) throw new ArgumentOutOfRangeException(nameof(value));
            lock (_gate) { ThrowIfDisposed(); _blendMode = value; }
            EmitChanged();
        }
    }

    internal override MaterialState? GetCanvasState() { ThrowIfDisposed(); return null; }
    internal override BlendMode GetCanvasBlendMode() => BlendMode;

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CanvasItemMaterial();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) =>
        CopyMaterial((CanvasItemMaterial)target);

    private void CopyMaterial(CanvasItemMaterial target)
    {
        lock (_gate) { ThrowIfDisposed(); target._blendMode = _blendMode; target._lightMode = _lightMode; target._particlesAnimation = _particlesAnimation; target._particlesAnimHFrames = _particlesAnimHFrames; target._particlesAnimVFrames = _particlesAnimVFrames; target._particlesAnimLoop = _particlesAnimLoop; }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(ParticleProperties).Concat(
        [new PropertyDescriptor<CanvasItemMaterial, LightMode>(nameof(LightMode), m => m.LightMode, (m, v) => m.LightMode = v, _ => LightMode.Normal, stored: true),
        new PropertyDescriptor<CanvasItemMaterial, BlendMode>(nameof(BlendMode), m => m.BlendMode,
            (m, v) => m.BlendMode = v, _ => BlendMode.Mix, stored: true)]);
}
