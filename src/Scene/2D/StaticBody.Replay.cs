namespace Electron2D;

public partial class StaticBody
{
    internal sealed class StaticReplay
    {
        internal Vector2 _constantLinearVelocity;
        internal float _constantAngularVelocity;
    }
    internal void CaptureReplay(StaticReplay state)
    {
        state._constantLinearVelocity = _constantLinearVelocity;
        state._constantAngularVelocity = _constantAngularVelocity;
    }
    internal void RestoreReplay(StaticReplay state)
    {
        _constantLinearVelocity = state._constantLinearVelocity;
        _constantAngularVelocity = state._constantAngularVelocity;
    }
}
