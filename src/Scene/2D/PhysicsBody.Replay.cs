namespace Electron2D;

public abstract partial class PhysicsBody
{
    internal sealed class BodyReplay
    {
        internal readonly record struct Configuration(PhysicsMaterial? _materialOverride);
        internal Configuration Settings;
        internal Vector2 _lastPosition;
        internal float _lastRotation;
        internal Transform _validatedTransform;
        internal float _validatedRotation;
        internal Transform _preparedTransform;
        internal long _preparedGeometryEpoch;
    }
    private BodyReplay.Configuration ReplayConfiguration() => new(_materialOverride);
    internal void CaptureReplay(BodyReplay state)
    {
        state.Settings = ReplayConfiguration();
        state._lastPosition = _lastPosition;
        state._lastRotation = _lastRotation;
        state._validatedTransform = _validatedTransform;
        state._validatedRotation = _validatedRotation;
        state._preparedTransform = _preparedTransform;
        state._preparedGeometryEpoch = _preparedGeometryEpoch;
    }
    internal void ValidateReplay(BodyReplay state)
    {
        PhysicsReplayCopy.Require(state.Settings == ReplayConfiguration());
    }
    internal void RestoreReplay(BodyReplay state)
    {
        _lastPosition = state._lastPosition;
        _lastRotation = state._lastRotation;
        _validatedTransform = state._validatedTransform;
        _validatedRotation = state._validatedRotation;
        _preparedTransform = state._preparedTransform;
        _preparedGeometryEpoch = state._preparedGeometryEpoch;
        _shapesDirty = false;
    }
}
