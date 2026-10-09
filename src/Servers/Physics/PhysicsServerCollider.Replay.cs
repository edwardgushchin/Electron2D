namespace Electron2D;

internal sealed partial class PhysicsServerCollider
{
    internal readonly record struct ReplayState(Transform Transform, Vector2 Linear, float Angular, bool Sleeping, bool CanSleep,
        bool FirstTarget, bool HasTarget, Transform Target);
    internal ReplayState CaptureReplay() => new(_transform, _linearVelocity, _angularVelocity, _sleeping, _canSleep, _firstKinematicTransform, _hasKinematicTarget, _kinematicTarget);
    internal void RestoreReplay(in ReplayState state)
    {
        _transform = state.Transform; _linearVelocity = state.Linear; _angularVelocity = state.Angular; _sleeping = state.Sleeping; _canSleep = state.CanSleep;
        _firstKinematicTransform = state.FirstTarget; _hasKinematicTarget = state.HasTarget; _kinematicTarget = state.Target; _shapesDirty = false;
    }
    internal PhysicsReplayEntry.ShapeState ReplayShape(int index)
    {
        var slot = _slots[index]; return new(slot.Shape.Geometry, slot.LocalTransform, !slot.Disabled, index, slot.OneWay,
            slot.Margin, slot.Direction, slot.Shape.Geometry.IsDisposed ? ulong.MaxValue : slot.Shape.Geometry.GeometryRevision,
            slot.Shape.Geometry.IsDisposed ? 0 : slot.Shape.Geometry.CustomSolverBias);
    }
}
