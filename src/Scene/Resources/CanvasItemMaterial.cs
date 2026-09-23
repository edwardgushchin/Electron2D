namespace Electron2D;

/// <summary>Applies a fixed blend mode to canvas geometry and textures.</summary>
/// <remarks>Nodes borrow this resource. The active mode is read each frame without re-recording draw commands.</remarks>
public sealed class CanvasItemMaterial : Material
{
    /// <summary>Defines how source color and alpha combine with the canvas target.</summary>
    public enum BlendModeEnum
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

    private readonly object _gate = new();
    private BlendModeEnum _blendMode;

    /// <summary>Creates a material with normal alpha blending.</summary>
    public CanvasItemMaterial() { }

    /// <summary>Gets or sets the canvas blend mode.</summary>
    /// <value><see cref="BlendModeEnum.Mix"/> by default.</value>
    /// <remarks>Successful assignments raise Changed after committing the value. Existing geometry uses the new value on the next frame.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a defined blend mode.</exception>
    /// <exception cref="ObjectDisposedException">The material is disposed.</exception>
    public BlendModeEnum BlendMode
    {
        get { lock (_gate) { ThrowIfDisposed(); return _blendMode; } }
        set
        {
            if (value is < BlendModeEnum.Mix or > BlendModeEnum.PremultAlpha) throw new ArgumentOutOfRangeException(nameof(value));
            lock (_gate) { ThrowIfDisposed(); _blendMode = value; }
            EmitChanged();
        }
    }

    internal override MaterialState? GetCanvasState() { ThrowIfDisposed(); return null; }
    internal override BlendModeEnum GetCanvasBlendMode() => BlendMode;

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CanvasItemMaterial();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) =>
        ((CanvasItemMaterial)target).BlendMode = BlendMode;

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<CanvasItemMaterial, BlendModeEnum>(nameof(BlendMode), m => m.BlendMode,
            (m, v) => m.BlendMode = v, _ => BlendModeEnum.Mix, stored: true)]);
}
