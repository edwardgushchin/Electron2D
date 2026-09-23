namespace Electron2D;

/// <summary>Moves and optionally repeats a spatial subtree within a parallax background.</summary>
/// <remarks>Only a direct <see cref="ParallaxBackground"/> parent drives this layer. Position and scale
/// are captured on tree entry and restored on exit. Attached mutation belongs to the scene owner thread.</remarks>
public sealed class ParallaxLayer : Entity
{
    private Vector2 _motionScale = Vector2.One, _motionOffset, _mirroring;
    private Vector2 _originalPosition, _originalScale = Vector2.One;

    /// <summary>Creates a layer with unit motion scale and no offset or repetition.</summary>
    public ParallaxLayer() { }

    /// <summary>Gets or sets the multiplier applied to background scrolling on each axis.</summary>
    /// <value>One on each axis initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public Vector2 MotionScale
    {
        get { CheckState(); return _motionScale; }
        set { EnsureMutable(); Finite(value); _motionScale = value; Refresh(); }
    }

    /// <summary>Gets or sets the offset applied after motion scaling, in canvas units.</summary>
    /// <value>Zero initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public Vector2 MotionOffset
    {
        get { CheckState(); return _motionOffset; }
        set { EnsureMutable(); Finite(value); _motionOffset = value; Refresh(); }
    }

    /// <summary>Gets or sets the per-axis interval for one additional repeated copy.</summary>
    /// <value>Zero initially. Negative components clamp to zero; repetition is unavailable on a zero axis.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public Vector2 MotionMirroring
    {
        get { CheckState(); return _mirroring; }
        set { EnsureMutable(); Finite(value); _mirroring = new(MathF.Max(0, value.X), MathF.Max(0, value.Y)); }
    }

    internal Vector2 RepeatPeriod => Parent is ParallaxBackground && IsInsideTree ? _mirroring * _originalScale : Vector2.Zero;

    internal void ApplyBackground(Vector2 offset, float scale)
    {
        if (!IsInsideTree || Parent is not ParallaxBackground) return;
        if (!offset.IsFinite() || !float.IsFinite(scale)) throw new InvalidOperationException("Layer camera transform is invalid.");
        var position = offset * _motionScale + (_motionOffset + _originalPosition) * scale;
        var period = _mirroring * scale;
        if (period.X != 0) position.X -= period.X * MathF.Ceiling(position.X / period.X);
        if (period.Y != 0) position.Y -= period.Y * MathF.Ceiling(position.Y / period.Y);
        var size = _originalScale * scale;
        if (!position.IsFinite() || !size.IsFinite()) throw new InvalidOperationException("Layer scroll overflowed finite coordinates.");
        List<Exception>? errors = null;
        try { Position = position; }
        catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed)
            try { Scale = size; }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Layer transform callbacks failed.", errors);
    }

    /// <inheritdoc />
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings();
        return Parent is ParallaxBackground ? warnings : [.. warnings, "ParallaxLayer requires a direct ParallaxBackground parent."];
    }

    internal override void OnTreeMembershipChanged(bool entering)
    {
        if (!entering)
        {
            try
            {
                List<Exception>? errors = null;
                try { Position = _originalPosition; }
                catch (Exception error) { CollectException(ref errors, error); }
                if (!IsDisposed)
                    try { Scale = _originalScale; }
                    catch (Exception error) { CollectException(ref errors, error); }
                ThrowCollected("Layer transform restoration failed.", errors);
            }
            finally { base.OnTreeMembershipChanged(false); }
            return;
        }
        base.OnTreeMembershipChanged(true);
        _originalPosition = Position; _originalScale = Scale;
        Refresh();
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Where(property => property.Name != nameof(Position) && property.Name != nameof(Scale))
            .Concat(LayerProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateLayer;

    private static Node CreateLayer() => new ParallaxLayer();
    private void Refresh()
    {
        if (IsInsideTree && Parent is ParallaxBackground background) background.RefreshLayer(this);
    }
    private void CheckState() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private static void Finite(Vector2 value)
    {
        if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value), "Motion coordinates must be finite.");
    }

    private static readonly PropertyDescriptor[] LayerProperties =
    [
        new PropertyDescriptor<ParallaxLayer, Vector2>(nameof(Position), n => n.IsInsideTree ? n._originalPosition : n.Position, (n, v) => n.Position = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<ParallaxLayer, Vector2>(nameof(Scale), n => n.IsInsideTree ? n._originalScale : n.Scale, (n, v) => n.Scale = v, _ => Vector2.One, stored: true),
        new PropertyDescriptor<ParallaxLayer, Vector2>(nameof(MotionScale), n => n.MotionScale, (n, v) => n.MotionScale = v, _ => Vector2.One, stored: true),
        new PropertyDescriptor<ParallaxLayer, Vector2>(nameof(MotionOffset), n => n.MotionOffset, (n, v) => n.MotionOffset = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<ParallaxLayer, Vector2>(nameof(MotionMirroring), n => n.MotionMirroring, (n, v) => n.MotionMirroring = v, _ => Vector2.Zero, stored: true),
    ];
}
