namespace Electron2D;

/// <summary>A color gradient texture with linear, radial, square or conic fill.</summary>
/// <remarks>Pixel work is coalesced until image access or renderer use. Source edits invalidate pixels without
/// emitting this texture's Changed event. Texture setting notifications are synchronous after mutation.
/// Input and output resources remain caller-owned; copies follow the Resource graph policy. Native floats
/// require backend support. Coordinate multi-call edits, resource copying and disposal across threads.</remarks>
public sealed class GradientTexture : Texture
{
    /// <summary>Selects how UV positions map to gradient offsets.</summary>
    public enum FillEnum
    {
        /// <summary>Signed projection onto the start-to-end line.</summary>
        Linear = 0,
        /// <summary>Distance from the start relative to the start-to-end radius.</summary>
        Radial = 1,
        /// <summary>Maximum axis distance relative to the maximum start-to-end axis distance.</summary>
        Square = 2,
        /// <summary>Signed angle wrapped into one full turn.</summary>
        Conic = 3,
    }
    /// <summary>Selects how fill offsets outside the unit interval repeat.</summary>
    public enum RepeatEnum
    {
        /// <summary>Clamp to the endpoint colors.</summary>
        None = 0,
        /// <summary>Repeat the same unit interval in both directions.</summary>
        Repeat = 1,
        /// <summary>Reflect alternating unit intervals.</summary>
        Mirror = 2,
    }

    private readonly GradientTextureData _data;

    /// <summary>Creates an uninitialized 64-by-64 linear gradient texture with a null source and UseHDR false.</summary>
    public GradientTexture() { _data = new(this, ramp: false); }

    /// <summary>Gets or sets horizontal sample count, from 1 through 16384. Every assignment invalidates pixels and emits Changed.</summary>
    /// <value>64 by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The width is outside 1..16384.</exception>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public int Width { get => _data.Width; set => _data.Width = value; }

    /// <summary>Gets or sets vertical sample count, from 1 through 16384. Every assignment invalidates pixels and emits Changed.</summary>
    /// <value>64 by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The height is outside 1..16384.</exception>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public int Height { get => _data.Height; set => _data.Height = value; }

    /// <summary>Gets or sets borrowed color source. A changed reference emits Changed; removing it preserves the last generated pixels.</summary>
    /// <value>null by default.</value>
    /// <exception cref="ObjectDisposedException">This texture or the supplied gradient is disposed.</exception>
    public Gradient? Gradient { get => _data.Gradient; set => _data.Gradient = value; }

    /// <summary>Gets or sets whether to use RGBA float storage; false uses clamped RGBA8. Actual changes invalidate and emit Changed.</summary>
    /// <value>false by default.</value>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public bool UseHDR { get => _data.UseHDR; set => _data.UseHDR = value; }

    /// <summary>Gets or sets fill pattern. Every assignment invalidates pixels and emits Changed.</summary>
    /// <value>Linear by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public FillEnum Fill { get => _data.Fill; set => _data.Fill = value; }

    /// <summary>Gets or sets start in UV coordinates; finite coordinates outside the unit square are allowed. Every assignment notifies.</summary>
    /// <value>(0, 0) by default.</value>
    /// <exception cref="ArgumentException">The coordinates are nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public Vector2 FillFrom { get => _data.FillFrom; set => _data.FillFrom = value; }

    /// <summary>Gets or sets end in UV coordinates. Equal endpoints sample offset zero; every assignment notifies.</summary>
    /// <value>(1, 0) by default.</value>
    /// <exception cref="ArgumentException">The coordinates are nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public Vector2 FillTo { get => _data.FillTo; set => _data.FillTo = value; }

    /// <summary>Gets or sets fill repetition policy, independent of the canvas sampler. Every assignment invalidates and emits Changed.</summary>
    /// <value>None by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public RepeatEnum Repeat { get => _data.Repeat; set => _data.Repeat = value; }

    /// <inheritdoc />
    public override int GetWidth() => Width;
    /// <inheritdoc />
    public override int GetHeight() => Height;
    /// <inheritdoc />
    public override Vector2 GetSize() => _data.GetSize();
    /// <inheritdoc />
    /// <value>Always true, including before initialization.</value>
    public override bool HasAlpha { get { ThrowIfDisposed(); return true; } }
    /// <inheritdoc />
    /// <remarks>Flushes pending work and returns copied pixels. A null gradient preserves the last generated image.
    /// Checked buffer sizing or allocation can fail for very large dimensions; failure preserves the previous payload.</remarks>
    public override Image? GetImage() => CapturePixels()?.CopyImage();
    internal override TexturePixels? CapturePixels() => _data.CapturePixels();

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<GradientTexture, int>(nameof(Width), t => t.Width, (t, v) => t.Width = v, _ => 64),
        new PropertyDescriptor<GradientTexture, int>(nameof(Height), t => t.Height, (t, v) => t.Height = v, _ => 64),
        new PropertyDescriptor<GradientTexture, Gradient?>(nameof(Gradient), t => t.Gradient, (t, v) => t.Gradient = v, _ => null),
        new PropertyDescriptor<GradientTexture, bool>(nameof(UseHDR), t => t.UseHDR, (t, v) => t.UseHDR = v, _ => false),
        new PropertyDescriptor<GradientTexture, FillEnum>(nameof(Fill), t => t.Fill, (t, v) => t.Fill = v, _ => FillEnum.Linear),
        new PropertyDescriptor<GradientTexture, Vector2>(nameof(FillFrom), t => t.FillFrom, (t, v) => t.FillFrom = v, _ => new Vector2(0, 0)),
        new PropertyDescriptor<GradientTexture, Vector2>(nameof(FillTo), t => t.FillTo, (t, v) => t.FillTo = v, _ => new Vector2(1, 0)),
        new PropertyDescriptor<GradientTexture, RepeatEnum>(nameof(Repeat), t => t.Repeat, (t, v) => t.Repeat = v, _ => RepeatEnum.None),
    ]);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new GradientTexture();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) =>
        _data.CopyTo(((GradientTexture)target)._data, deep, duplicateSubresource);
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _data.Dispose();
        base.Dispose(disposing);
    }
}
