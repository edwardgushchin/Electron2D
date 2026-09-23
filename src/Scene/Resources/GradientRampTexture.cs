namespace Electron2D;

/// <summary>A single row of samples from a borrowed color gradient.</summary>
/// <remarks>Pixel work is coalesced until image access or renderer use. Source edits invalidate pixels without
/// emitting this texture's Changed event. Texture setting notifications are synchronous after mutation.
/// Input and output resources remain caller-owned; copies follow the Resource graph policy. Native floats
/// require backend support. Coordinate multi-call edits, resource copying and disposal across threads.</remarks>
public sealed class GradientRampTexture : Texture
{
    private readonly GradientTextureData _data;

    /// <summary>Creates an uninitialized 256-by-1 gradient ramp with a null source and UseHDR false.</summary>
    public GradientRampTexture() { _data = new(this, ramp: true); }

    /// <summary>Gets or sets horizontal sample count, from 1 through 16384. Every assignment invalidates pixels and emits Changed.</summary>
    /// <value>256 by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The width is outside 1..16384.</exception>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public int Width { get => _data.Width; set => _data.Width = value; }

    /// <summary>Gets or sets borrowed color source. A changed reference emits Changed; removing it preserves the last generated pixels.</summary>
    /// <value>null by default.</value>
    /// <exception cref="ObjectDisposedException">This texture or the supplied gradient is disposed.</exception>
    public Gradient? Gradient { get => _data.Gradient; set => _data.Gradient = value; }

    /// <summary>Gets or sets whether to use RGBA float storage; false uses clamped RGBA8. Actual changes invalidate and emit Changed.</summary>
    /// <value>false by default.</value>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public bool UseHDR { get => _data.UseHDR; set => _data.UseHDR = value; }

    /// <inheritdoc />
    public override int GetWidth() => Width;
    /// <inheritdoc />
    public override int GetHeight() { ThrowIfDisposed(); return 1; }
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
        new PropertyDescriptor<GradientRampTexture, int>(nameof(Width), t => t.Width, (t, v) => t.Width = v, _ => 256),
        new PropertyDescriptor<GradientRampTexture, Gradient?>(nameof(Gradient), t => t.Gradient, (t, v) => t.Gradient = v, _ => null),
        new PropertyDescriptor<GradientRampTexture, bool>(nameof(UseHDR), t => t.UseHDR, (t, v) => t.UseHDR = v, _ => false),
    ]);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new GradientRampTexture();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) =>
        _data.CopyTo(((GradientRampTexture)target)._data, deep, duplicateSubresource);
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _data.Dispose();
        base.Dispose(disposing);
    }
}
