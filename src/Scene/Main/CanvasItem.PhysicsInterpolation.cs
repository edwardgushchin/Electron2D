namespace Electron2D;

public abstract partial class CanvasItem
{
    internal bool HasRenderingInterpolation
    {
        get
        {
            var moving = ServerState is not { } state || state.Transform is null && state.Interpolated is null
                ? IsPhysicsInterpolatedAndEnabled() && _interpolationValid && _interpolationPrevious != _interpolationCurrent
                : (state.Interpolated ?? (state.Owned || IsPhysicsInterpolated())) && state.Interpolation.Previous != state.Interpolation.Current;
            return moving || HasMultiMeshInterpolation;
        }
    }

    private bool HasMultiMeshInterpolation
    {
        get { if (_multiMeshes is not null) for (var i = 0; i < _multiMeshCount; i++) if (_multiMeshes[i].HasRenderingInterpolation) return true; return false; }
    }

    private Transform _interpolationPrevious;
    private Transform _interpolationCurrent;
    private bool _interpolationValid;
    private bool _interpolationFirstTick;

    internal virtual void BeginPhysicsInterpolationTick()
    {
        if (!IsPhysicsInterpolatedAndEnabled()) { _interpolationValid = false; return; }
        _interpolationFirstTick = !_interpolationValid;
        if (!_interpolationValid) { ResetInterpolationSnapshot(); _interpolationFirstTick = true; }
        if (_interpolationPrevious != _interpolationCurrent) RenderingServer.NotifyRenderMutation();
        _interpolationPrevious = _interpolationCurrent;
    }

    internal virtual void EndPhysicsInterpolationTick()
    {
        if (!IsPhysicsInterpolatedAndEnabled()) { _interpolationValid = false; return; }
        var current = GetVisualTransform();
        if (!_interpolationValid || _interpolationFirstTick) _interpolationPrevious = current;
        if (_interpolationCurrent != current) RenderingServer.NotifyRenderMutation();
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

    internal Transform GetInterpolatedGlobalVisualTransform(float fraction) => GetParentItem() is { } parent ? parent.GetInterpolatedGlobalVisualTransform(fraction) * GetInterpolatedVisualTransform(fraction) : GetInterpolatedVisualTransform(fraction);

    internal void CopyInterpolationSnapshot(ref CanvasTransformInterpolation history)
    {
        var canonical = GetVisualTransform();
        history.Previous = _interpolationValid ? _interpolationPrevious : canonical;
        history.Current = _interpolationValid ? _interpolationCurrent : canonical;
    }
    internal void ResetVisualInterpolationSnapshot()
    {
        _interpolationPrevious = _interpolationCurrent = GetVisualTransform();
        _interpolationValid = true; _interpolationFirstTick = false;
    }
    private void ResetInterpolationSnapshot()
    {
        if (ServerState is { } state) state.Interpolation.Reset(state.Transform ?? GetVisualTransform());
        if (_multiMeshes is not null) for (var i = 0; i < _multiMeshCount; i++) _multiMeshes[i].ResetInterpolation();
        _interpolationPrevious = _interpolationCurrent = GetVisualTransform();
        _interpolationValid = true;
        _interpolationFirstTick = false;
    }
}
