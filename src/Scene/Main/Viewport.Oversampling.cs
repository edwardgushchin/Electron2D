namespace Electron2D;

public abstract partial class Viewport
{
    private bool _oversampling = true;
    private float _oversamplingOverride;
    private int _dpiScaleKey = 64;
    /// <summary>Gets or sets whether font and scalable SVG rasterization follow viewport stretch.</summary>
    /// <value>True initially. False selects a raster factor of one.</value>
    public bool Oversampling
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _oversampling; }
        set { EnsureMutable(); RenderingOwner?.EnsureViewportMutation(); if (_oversampling == value) return; _oversampling = value; InvalidateViewportRecording(); }
    }
    /// <summary>Gets or sets a finite raster-scale override; nonpositive values select automatic scaling.</summary>
    /// <value>Zero initially. Effective only when Oversampling is enabled.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is not finite.</exception>
    public float OversamplingOverride
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _oversamplingOverride; }
        set { EnsureMutable(); RenderingOwner?.EnsureViewportMutation(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_oversamplingOverride == value) return; _oversamplingOverride = value; InvalidateViewportRecording(); }
    }
    /// <summary>Returns the effective font and scalable SVG raster factor for this viewport.</summary>
    /// <returns>One when disabled, the positive override, or the maximum active stretch axis.</returns>
    public float GetOversampling()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        if (!_oversampling) return 1;
        if (_oversamplingOverride > 0) return _oversamplingOverride;
        var scale = StretchTransform.Scale;
        var factor = MathF.Max(MathF.Abs(scale.X), MathF.Abs(scale.Y)) * (this is Window window ? window.RenderDensity : 1);
        return float.IsFinite(factor) && factor > 0 ? factor : 1;
    }
    internal int DPIScaleKey { get { SyncDPIScale(); return _dpiScaleKey; } }
    private void SyncDPIScale()
    {
        var next = CanvasRasterScale.Quantize(GetOversampling());
        if (next == _dpiScaleKey) return;
        CanvasRasterScale.ChangeReference(ref _dpiScaleKey, next);
    }
    private static readonly PropertyDescriptor[] OversamplingProperties = [
        new PropertyDescriptor<Viewport,bool>(nameof(Oversampling), v => v.Oversampling, (v,x) => v.Oversampling=x, _ => true, stored:true),
        new PropertyDescriptor<Viewport,float>(nameof(OversamplingOverride), v => v.OversamplingOverride, (v,x) => v.OversamplingOverride=x, _ => 0, stored:true)];
}
