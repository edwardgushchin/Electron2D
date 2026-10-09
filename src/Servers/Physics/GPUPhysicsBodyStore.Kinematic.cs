namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    private const uint SurfaceEdit = 65536, TargetEdit = 131072, CancelTarget = 262144;
    private int _kinematicBodyCount;

    /// <summary>Replaces the pending kinematic destination; reads and zero-time steps retain it.</summary>
    internal void SetKinematicTarget(BodyHandle body, Vector2 position, float rotation)
    {
        Validate(body);
        if (_slots[body.Index].Mode != PhysicsServer.BodyMode.Kinematic)
            throw new InvalidOperationException("Only kinematic bodies accept a motion target.");
        if (!position.IsFinite() || !float.IsFinite(rotation)) throw new ArgumentOutOfRangeException(nameof(position));
        ref var command = ref Edit(body.Index); command.Mask |= TargetEdit;
        command.Target = new(position.X, position.Y, MathF.Cos(rotation), MathF.Sin(rotation));
        Wake(body.Index);
    }

    private void CancelKinematicTarget(int index)
    {
        ref var command = ref Edit(index); command.Mask = (command.Mask | CancelTarget) & ~TargetEdit;
        command.Target = default;
    }
}
