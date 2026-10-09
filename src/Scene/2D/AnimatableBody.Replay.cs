namespace Electron2D;

public sealed partial class AnimatableBody
{
    internal sealed class AnimatableReplay
    {
        internal readonly record struct Configuration(bool _syncToPhysics);
        internal Configuration Settings;
        internal bool _hasTarget;
        internal Transform _lastValidTransform;
        internal Transform _targetTransform;
    }
    private AnimatableReplay.Configuration ReplayConfiguration() => new(_syncToPhysics);
    internal void CaptureReplay(AnimatableReplay state)
    {
        state.Settings = ReplayConfiguration();
        state._hasTarget = _hasTarget;
        state._lastValidTransform = _lastValidTransform;
        state._targetTransform = _targetTransform;
    }
    internal void ValidateReplay(AnimatableReplay state)
    {
        PhysicsReplayCopy.Require(state.Settings == ReplayConfiguration());
    }
    internal void RestoreReplay(AnimatableReplay state)
    {
        _hasTarget = state._hasTarget;
        _lastValidTransform = state._lastValidTransform;
        _targetTransform = state._targetTransform;
    }
}
