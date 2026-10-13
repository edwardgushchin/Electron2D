namespace Electron2D;

internal struct CanvasTransformInterpolation
{
    internal bool Enabled;
    internal Transform Previous, Current;
    internal void Reset(Transform transform) => Previous = Current = transform;
    internal void Tick(Transform transform, bool start) { if (start) Previous = Current; Current = transform; }
    internal Transform Render(Transform canonical, float fraction, bool active) => Enabled && active ? Previous.InterpolateWith(Current, fraction) : canonical;
    internal Transform Rebase(Transform transform, Transform canonical)
    {
        if (!transform.IsFinite()) throw new ArgumentException("Interpolation transforms must be finite.", nameof(transform));
        var previous = transform * Previous; var current = transform * Current;
        var transformed = transform * canonical;
        if (!previous.IsFinite() || !current.IsFinite() || !transformed.IsFinite()) throw new ArgumentException("Interpolation products must be finite.", nameof(transform));
        Previous = previous; Current = current; return transformed;
    }
}
