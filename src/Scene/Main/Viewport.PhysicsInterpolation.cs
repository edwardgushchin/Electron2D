namespace Electron2D;

public abstract partial class Viewport
{
    private Transform _canvasInterpolationPrevious;
    private Transform _canvasInterpolationCurrent;
    private bool _canvasInterpolationValid;
    private bool _canvasInterpolationFirstTick;

    internal void ResetCanvasInterpolationSnapshot()
    {
        _canvasInterpolationPrevious = _canvasInterpolationCurrent = _canvasTransform;
        _canvasInterpolationValid = true;
        _canvasInterpolationFirstTick = false;
    }

    internal void BeginPhysicsInterpolationTick()
    {
        if (!ShouldInterpolateCamera()) { _canvasInterpolationValid = false; return; }
        _canvasInterpolationFirstTick = !_canvasInterpolationValid;
        if (!_canvasInterpolationValid) { ResetCanvasInterpolationSnapshot(); _canvasInterpolationFirstTick = true; }
        _canvasInterpolationPrevious = _canvasInterpolationCurrent;
    }

    internal void EndPhysicsInterpolationTick()
    {
        if (!ShouldInterpolateCamera()) { _canvasInterpolationValid = false; return; }
        if (!_canvasInterpolationValid || _canvasInterpolationFirstTick)
            _canvasInterpolationPrevious = _canvasTransform;
        _canvasInterpolationCurrent = _canvasTransform;
        _canvasInterpolationValid = true;
        _canvasInterpolationFirstTick = false;
    }

    internal Transform GetInterpolatedCanvasTransform(float fraction)
    {
        if (!ShouldInterpolateCamera() || !_canvasInterpolationValid) return _canvasTransform;
        if (_canvasTransform != _canvasInterpolationCurrent)
        {
            ResetCanvasInterpolationSnapshot();
            return _canvasTransform;
        }
        return _canvasInterpolationPrevious.InterpolateWith(_canvasInterpolationCurrent, Math.Clamp(fraction, 0f, 1f));
    }

    private bool ShouldInterpolateCamera() => _camera?.IsPhysicsInterpolatedAndEnabled() == true;

    /// <inheritdoc />
    /// <remarks>Also resets camera presentation history on the inherited physics-interpolation reset notification.</remarks>
    protected override void OnNotification(int what)
    {
        if (what == NotificationVPMouseEnter) _mouseInViewport = true; else if (what == NotificationVPMouseExit) _mouseInViewport = false;
        if (what == NotificationResetPhysicsInterpolation) ResetCanvasInterpolationSnapshot();
        base.OnNotification(what);
    }
}
