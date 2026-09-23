namespace Electron2D;

/// <summary>A one-row RGB floating-point texture generated from three borrowed scalar curves.</summary>
/// <remarks>X, Y and Z name the red, green and blue channels, not spatial coordinates. Texel i samples each
/// curve at i / Width. Missing channels are zero; values are not clamped. A curve shared by multiple channels
/// causes one rebake and Changed notification per source event. Threading and rendering follow CurveTexture.</remarks>
public sealed class CurveXYZTexture : Texture
{
    private readonly CurveTextureData _data;

    /// <summary>Creates an uninitialized 256-by-1 RGB texture with no curves or image.</summary>
    public CurveXYZTexture() { _data = new(this, single: false); }

    /// <summary>Gets or sets the number of horizontal samples.</summary>
    /// <value>256 by default; valid values are 32 through 4096 inclusive.</value>
    /// <remarks>An actual change rebakes and emits Changed. Equal assignments do nothing.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The width is outside the supported range.</exception>
    /// <exception cref="ObjectDisposedException">This texture or a borrowed curve is disposed.</exception>
    public int Width { get => _data.Width; set => _data.Width = value; }

    /// <summary>Gets or sets the borrowed scalar curve for the red channel.</summary>
    /// <value>Null by default, supplying zero when initialized.</value>
    /// <remarks>Changing the reference rebakes and emits Changed; curves are never disposed by this texture.</remarks>
    /// <exception cref="ObjectDisposedException">This texture or a supplied curve is disposed.</exception>
    public Curve? CurveX { get => _data.GetCurve(0); set => _data.SetCurve(0, value); }
    /// <summary>Gets or sets the borrowed scalar curve for the green channel.</summary>
    /// <value>Null by default, supplying zero when initialized.</value>
    /// <remarks>Changing the reference rebakes and emits Changed; shared channels subscribe only once.</remarks>
    /// <exception cref="ObjectDisposedException">This texture or a supplied curve is disposed.</exception>
    public Curve? CurveY { get => _data.GetCurve(1); set => _data.SetCurve(1, value); }
    /// <summary>Gets or sets the borrowed scalar curve for the blue channel.</summary>
    /// <value>Null by default, supplying zero when initialized.</value>
    /// <remarks>Changing the reference rebakes and emits Changed; shared channels subscribe only once.</remarks>
    /// <exception cref="ObjectDisposedException">This texture or a supplied curve is disposed.</exception>
    public Curve? CurveZ { get => _data.GetCurve(2); set => _data.SetCurve(2, value); }

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
        new PropertyDescriptor<CurveXYZTexture, int>(nameof(Width), t => t.Width, (t, v) => t.Width = v, _ => 256),
        new PropertyDescriptor<CurveXYZTexture, Curve?>(nameof(CurveX), t => t.CurveX, (t, v) => t.CurveX = v, _ => null),
        new PropertyDescriptor<CurveXYZTexture, Curve?>(nameof(CurveY), t => t.CurveY, (t, v) => t.CurveY = v, _ => null),
        new PropertyDescriptor<CurveXYZTexture, Curve?>(nameof(CurveZ), t => t.CurveZ, (t, v) => t.CurveZ = v, _ => null),
    ]);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CurveXYZTexture();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) =>
        _data.CopyTo(((CurveXYZTexture)target)._data, deep, duplicateSubresource);
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _data.Dispose();
        base.Dispose(disposing);
    }
}
