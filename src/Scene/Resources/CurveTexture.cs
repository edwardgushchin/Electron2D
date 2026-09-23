namespace Electron2D;

/// <summary>A one-row floating-point texture generated from a borrowed scalar curve.</summary>
/// <remarks>Texel i samples Curve.SampleBaked(i / Width); the endpoint at one is not sampled.
/// Curve changes synchronously replace the pixels before Changed. Values are not clamped or color-converted.
/// CPU access needs no renderer; drawing requires native floating-point texture support. State is serialized;
/// events run outside its lock. Coordinate multi-call edits, copying and disposal with other threads.</remarks>
public sealed class CurveTexture : Texture
{
    /// <summary>Selects the stored channels for the scalar samples.</summary>
    public enum TextureModeEnum
    {
        /// <summary>Repeats the scalar value in red, green and blue.</summary>
        RGB = 0,
        /// <summary>Stores the scalar in red, with zero green and blue when sampled.</summary>
        Red = 1,
    }

    private readonly CurveTextureData _data;

    /// <summary>Creates an uninitialized 256-by-1 RGB texture with no curve or image.</summary>
    public CurveTexture() { _data = new(this, single: true); }

    /// <summary>Gets or sets the number of horizontal samples.</summary>
    /// <value>256 by default; valid values are 32 through 4096 inclusive.</value>
    /// <remarks>An actual change rebakes and emits Changed. Equal assignments do nothing.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The width is outside the supported range.</exception>
    /// <exception cref="ObjectDisposedException">This texture or a borrowed curve is disposed.</exception>
    public int Width { get => _data.Width; set => _data.Width = value; }

    /// <summary>Gets or sets the borrowed curve supplying every stored channel.</summary>
    /// <value>Null by default. A null curve supplies zero after the texture is initialized.</value>
    /// <remarks>Assigning a different curve rebakes and emits Changed. The texture never disposes the curve.</remarks>
    /// <exception cref="ObjectDisposedException">This texture or a supplied curve is disposed.</exception>
    public Curve? Curve { get => _data.GetCurve(0); set => _data.SetCurve(0, value); }

    /// <summary>Gets or sets whether to store three identical channels or red only.</summary>
    /// <value>RGB by default; uses Image.Format.Rgbf or Image.Format.Rf respectively.</value>
    /// <remarks>An actual change rebakes and emits Changed. GPU uploads currently expand both formats to RGBA32Float.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a defined mode.</exception>
    /// <exception cref="ObjectDisposedException">This texture or its curve is disposed.</exception>
    public TextureModeEnum TextureMode { get => _data.Mode; set => _data.Mode = value; }

    /// <inheritdoc />
    public override int GetWidth() => Width;
    /// <inheritdoc />
    public override int GetHeight() { ThrowIfDisposed(); return 1; }
    /// <inheritdoc />
    public override Image? GetImage() => CapturePixels()?.CopyImage();
    internal override TexturePixels? CapturePixels() => _data.CapturePixels();

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<CurveTexture, int>(nameof(Width), t => t.Width, (t, v) => t.Width = v, _ => 256),
        new PropertyDescriptor<CurveTexture, Curve?>(nameof(Curve), t => t.Curve, (t, v) => t.Curve = v, _ => null),
        new PropertyDescriptor<CurveTexture, TextureModeEnum>(nameof(TextureMode), t => t.TextureMode, (t, v) => t.TextureMode = v, _ => TextureModeEnum.RGB),
    ]);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CurveTexture();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) =>
        _data.CopyTo(((CurveTexture)target)._data, deep, duplicateSubresource);
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _data.Dispose();
        base.Dispose(disposing);
    }
}
