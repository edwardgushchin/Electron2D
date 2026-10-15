using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal sealed partial class NetworkWorld
{
    internal int JointSteps, JointKicks;
    internal float MaxPinError, MaxGrooveError, MaxSpringLength;
    internal static bool IsJoint(ObjectKind kind) => kind is ObjectKind.Pin or ObjectKind.Groove or ObjectKind.Spring or ObjectKind.SupportPin or ObjectKind.SupportGroove or ObjectKind.SupportSpring;

    private void KickJoints(ulong tick)
    {
        if (tick < 120) return;
        ((RigidBody)Find(41)!.Body!).ApplyCentralImpulse(new(.5f, -.1f));
        ((RigidBody)Find(51)!.Body!).ApplyCentralImpulse(new(.2f, -.1f));
        JointKicks++;
    }

    private void CheckJoints()
    {
        var pinError = (Find(20)!.Body!.GlobalTransform * new Vector2(20, 10)).DistanceTo(Find(21)!.Body!.GlobalTransform * new Vector2(-15, -10));
        var groove = (GrooveJoint)Find(101)!.Joint!;
        var spring = (DampedSpringJoint)Find(102)!.Joint!;
        var guided = Find(41)!.Body!.GlobalPosition - groove.GlobalPosition;
        var springLength = Find(51)!.Body!.GlobalPosition.DistanceTo(spring.GlobalPosition);
        var error = MathF.Max(MathF.Abs(guided.X), MathF.Max(-guided.Y, guided.Y - groove.Length));
        if (!float.IsFinite(pinError) || pinError >= .5f || !guided.IsFinite() || !float.IsFinite(springLength) || error >= .5f || springLength >= 130)
            throw new InvalidOperationException($"Network joint continuation failed at {Tick} in {Phase}: pin error {pinError}, groove error {error}, spring length {springLength}.");
        MaxPinError = MathF.Max(MaxPinError, pinError);
        MaxGrooveError = MathF.Max(MaxGrooveError, error);
        MaxSpringLength = MathF.Max(MaxSpringLength, springLength);
        JointSteps++;
    }
}
