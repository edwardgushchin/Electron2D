namespace Electron2D;

/// <summary>Selects the blend operation of a built-in writable-texture fragment.</summary>
public enum BlitBlendMode
{
    /// <summary>Source alpha over destination.</summary>
    Mix = 0,
    /// <summary>Adds the alpha-weighted source.</summary>
    Add = 1,
    /// <summary>Subtracts the alpha-weighted source from the destination.</summary>
    Sub = 2,
    /// <summary>Multiplies source and destination channels.</summary>
    Mul = 3,
    /// <summary>Replaces destination channels.</summary>
    Disabled = 4,
}

/// <summary>Copies and modulates up to four source textures into writable targets.</summary>
/// <remarks>Each target receives its corresponding complete source, scaled into the destination rectangle.
/// Missing sources use opaque white. This material requires the GPU backend and cannot shade canvas items.</remarks>
public sealed class BlitMaterial : Material
{
    private BlitBlendMode _blend;
    /// <summary>Creates a material using source-alpha mixing.</summary>
    public BlitMaterial() { }
    /// <summary>Gets or sets the operation applied to all blit outputs.</summary>
    /// <value>Mix by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The material is disposed.</exception>
    public BlitBlendMode BlendMode
    {
        get { ThrowIfDisposed(); return _blend; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_blend == value) return; _blend = value; EmitChanged(); }
    }
    internal override MaterialState? GetCanvasState() { ThrowIfDisposed(); return null; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new BlitMaterial();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    { ((BlitMaterial)target)._blend = _blend; }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<BlitMaterial, BlitBlendMode>(nameof(BlendMode), p => p.BlendMode, (p, v) => p.BlendMode = v, _ => BlitBlendMode.Mix, stored: true)]);
}
