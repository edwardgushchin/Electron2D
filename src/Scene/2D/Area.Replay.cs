namespace Electron2D;

public partial class Area
{
    internal sealed class AreaReplay
    {
        internal readonly record struct Configuration(bool _monitoring, bool _monitorable);
        internal Configuration Settings;
        internal Vector2 _lastPosition;
        internal float _lastRotation;
        internal readonly PhysicsShapePairTracker Pairs = new();
    }
    private AreaReplay.Configuration ReplayConfiguration() => new(_monitoring, _monitorable);
    internal void CaptureReplay(AreaReplay state)
    {
        state.Settings = ReplayConfiguration();
        state._lastPosition = _lastPosition;
        state._lastRotation = _lastRotation;
        _shapePairs.CopyTo(state.Pairs);
    }
    internal void ValidateReplay(AreaReplay state)
    {
        PhysicsReplayCopy.Require(state.Settings == ReplayConfiguration());
    }
    internal void RestoreReplay(AreaReplay state)
    {
        _lastPosition = state._lastPosition;
        _lastRotation = state._lastRotation;
        _shapesDirty = false;
        state.Pairs.CopyTo(_shapePairs); _pairChanges.Clear();
    }
}
