namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    internal void SetSurfaceVelocity(BodyHandle body, Vector2 linear, float angular)
    {
        Validate(body);
        if (!linear.IsFinite() || !float.IsFinite(angular)) throw new ArgumentOutOfRangeException(nameof(linear));
        ref var slot = ref _slots[body.Index];
        if (slot.Surface.X == linear.X && slot.Surface.Y == linear.Y && slot.Surface.Z == angular) return;
        slot.Surface = new(linear.X, linear.Y, angular, slot.Surface.W);
        ref var command = ref Edit(body.Index); command.Mask |= SurfaceEdit; command.Body.Surface = slot.Surface;
        Wake(body.Index, structural: true);
    }

    private const uint SurfaceEdit = 65536, TargetEdit = 131072, CancelTarget = 262144, ClearKinematicVelocityEdit = 16777216;
    private int _kinematicBodyCount;
    internal void ClearKinematicVelocity(BodyHandle body)
    {
        Validate(body);
        ref var command = ref Edit(body.Index);
        command.Mask = (command.Mask | ClearKinematicVelocityEdit) & ~(Velocity | LinearVelocityEdit | AngularVelocityEdit);
    }

    /// <summary>Replaces the pending kinematic destination; reads and zero-time steps retain it.</summary>
    internal void SetKinematicTarget(BodyHandle body, Vector2 position, float rotation)
    {
        Validate(body);
        if (_slots[body.Index].Mode != PhysicsServer.BodyMode.Kinematic)
            throw new InvalidOperationException("Only kinematic bodies accept a motion target.");
        if (!position.IsFinite() || !float.IsFinite(rotation)) throw new ArgumentOutOfRangeException(nameof(position));
        ref var command = ref Edit(body.Index); command.Mask |= TargetEdit;
        command.Target = new(position.X, position.Y, MathF.Cos(rotation), MathF.Sin(rotation));
    }

    private void CancelKinematicTarget(int index)
    {
        ref var command = ref Edit(index); command.Mask = (command.Mask | CancelTarget) & ~TargetEdit;
        command.Target = default;
    }
}
