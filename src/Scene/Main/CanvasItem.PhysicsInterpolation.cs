namespace Electron2D;

public abstract partial class CanvasItem
{
    private Transform _interpolationPrevious;
    private Transform _interpolationCurrent;
    private bool _interpolationValid;
    private bool _interpolationFirstTick;

    internal void BeginPhysicsInterpolationTick()
    {
        if (!IsPhysicsInterpolatedAndEnabled()) { _interpolationValid = false; return; }
        _interpolationFirstTick = !_interpolationValid;
        if (!_interpolationValid) { ResetInterpolationSnapshot(); _interpolationFirstTick = true; }
        _interpolationPrevious = _interpolationCurrent;
    }

    internal void EndPhysicsInterpolationTick()
    {
        if (!IsPhysicsInterpolatedAndEnabled()) { _interpolationValid = false; return; }
        var current = GetVisualTransform();
        if (!_interpolationValid || _interpolationFirstTick) _interpolationPrevious = current;
        _interpolationCurrent = current;
        _interpolationValid = true;
        _interpolationFirstTick = false;
    }

    internal Transform GetInterpolatedVisualTransform(float fraction)
    {
        var current = GetVisualTransform();
        if (!IsPhysicsInterpolatedAndEnabled() || !_interpolationValid) return current;
        if (current != _interpolationCurrent)
        {
            _interpolationPrevious = _interpolationCurrent = current;
            return current;
        }
        return _interpolationPrevious.InterpolateWith(_interpolationCurrent, Math.Clamp(fraction, 0f, 1f));
    }

    private void ResetInterpolationSnapshot()
    {
        if (_multiMeshes is not null) for (var i = 0; i < _multiMeshCount; i++) _multiMeshes[i].ResetInterpolation();
        _interpolationPrevious = _interpolationCurrent = GetVisualTransform();
        _interpolationValid = true;
        _interpolationFirstTick = false;
    }
}
