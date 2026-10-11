using Electron2D;

public static partial class BackendChecks
{
    private readonly record struct JointParameterValues(float Bias, float MaxBias, float MaxForce, float Softness,
        bool Limit, float Lower, float Upper, bool Motor, float Speed, float Torque, float Rest, float Stiffness, float Damping);

    private static JointParameterValues JointParameterState(RID id)
    {
        var pin = PhysicsServer.JointGetType(id) == PhysicsServer.JointType.Pin;
        var spring = PhysicsServer.JointGetType(id) == PhysicsServer.JointType.DampedSpring;
        return new(PhysicsServer.JointGetBias(id), PhysicsServer.JointGetMaxBias(id), PhysicsServer.JointGetMaxForce(id),
            pin ? PhysicsServer.PinJointGetSoftness(id) : 0, pin && PhysicsServer.PinJointGetAngularLimitEnabled(id),
            pin ? PhysicsServer.PinJointGetAngularLimitLower(id) : 0, pin ? PhysicsServer.PinJointGetAngularLimitUpper(id) : 0,
            pin && PhysicsServer.PinJointGetMotorEnabled(id), pin ? PhysicsServer.PinJointGetMotorTargetVelocity(id) : 0,
            pin ? PhysicsServer.PinJointGetMotorMaxTorque(id) : 0, spring ? PhysicsServer.DampedSpringJointGetRestLength(id) : 0,
            spring ? PhysicsServer.DampedSpringJointGetStiffness(id) : 0, spring ? PhysicsServer.DampedSpringJointGetDamping(id) : 0);
    }

    private static void SetJointParameters(RID id, int index)
    {
        PhysicsServer.JointSetBias(id, .1f + .1f * index); PhysicsServer.JointSetMaxBias(id, 50 + index); PhysicsServer.JointSetMaxForce(id, 0);
        switch (PhysicsServer.JointGetType(id))
        {
            case PhysicsServer.JointType.Pin:
                PhysicsServer.PinJointSetSoftness(id, .1f + .1f * index);
                PhysicsServer.PinJointSetAngularLimitEnabled(id, false);
                PhysicsServer.PinJointSetAngularLimitLower(id, -.2f); PhysicsServer.PinJointSetAngularLimitUpper(id, .2f);
                PhysicsServer.PinJointSetAngularLimitEnabled(id, index != 0);
                PhysicsServer.PinJointSetMotorMaxTorque(id, 0); PhysicsServer.PinJointSetMotorTargetVelocity(id, 2 + index);
                PhysicsServer.PinJointSetMotorEnabled(id, index != 0);
                break;
            case PhysicsServer.JointType.DampedSpring:
                PhysicsServer.DampedSpringJointSetRestLength(id, 100 + index);
                PhysicsServer.DampedSpringJointSetStiffness(id, .12f + .12f * index); PhysicsServer.DampedSpringJointSetDamping(id, index);
                break;
        }
    }

    private static void VerifyJointParameters(RID id, Solver? solver)
    {
        var v = JointParameterState(id);
        Check(v.Bias is >= .1f and <= .2f && v.MaxBias is >= 50 and <= 51 && v.MaxForce == 0, "Typed common parameters retain the authored profile");
        if (solver is null) return;
        var s = solver.Joints[id].Settings;
        Check(s.Bias == v.Bias && s.MaxBias == v.MaxBias && s.MaxForce == v.MaxForce, "The selected joint receives the complete common profile");
        if (s.Type == PhysicsServer.JointType.Pin)
            Check(s.PinSoftness == v.Softness && s.PinLimitEnabled == v.Limit && s.PinLimitLower == v.Lower && s.PinLimitUpper == v.Upper &&
                s.PinMotorEnabled == v.Motor && s.PinMotorVelocity == v.Speed && s.PinMotorMaxTorque == v.Torque, "All selected pin parameter/flag operations receive their authored values");
        if (s.Type == PhysicsServer.JointType.DampedSpring)
            Check(s.EffectiveSpringRestLength == v.Rest && s.SpringStiffness == v.Stiffness && s.SpringDamping == v.Damping, "The selected spring receives its real force inputs");
    }

    private static void RejectSceneJointWrites(Joint[] nodes)
    {
        foreach (var node in nodes)
        {
            var bias = node.Bias; var maxBias = node.MaxBias; var force = node.MaxForce; var a = node.NodeA; var b = node.NodeB;
            Reject<InvalidOperationException>(() => node.Bias = .4f); Reject<InvalidOperationException>(() => node.Bias = bias);
            Reject<InvalidOperationException>(() => node.MaxBias = 7); Reject<InvalidOperationException>(() => node.MaxForce = 8);
            Reject<InvalidOperationException>(() => node.NodeA = "../Missing"); Reject<InvalidOperationException>(() => node.NodeB = "../Missing");
            Check(node.Bias == bias && node.MaxBias == maxBias && node.MaxForce == force && node.NodeA == a && node.NodeB == b,
                "Scene scalar/path writes reject before descriptors change");
        }
        var pin = (PinJoint)nodes[0];
        Reject<InvalidOperationException>(() => pin.Softness = .2f); Reject<InvalidOperationException>(() => pin.AngularLimitEnabled = true);
        Reject<InvalidOperationException>(() => pin.AngularLimitLower = -.2f); Reject<InvalidOperationException>(() => pin.AngularLimitUpper = .2f);
        Reject<InvalidOperationException>(() => pin.MotorEnabled = true); Reject<InvalidOperationException>(() => pin.MotorTargetVelocity = 2);
        Reject<InvalidOperationException>(() => pin.MotorMaxTorque = 3);
        var groove = (GrooveJoint)nodes[1]; var length = groove.Length; var offset = groove.InitialOffset;
        Reject<InvalidOperationException>(() => groove.Length = 20); Reject<InvalidOperationException>(() => groove.InitialOffset = 10);
        Check(groove.Length == length && groove.InitialOffset == offset, "Groove geometry rejects before local fields change");
        var spring = (DampedSpringJoint)nodes[2]; length = spring.Length;
        Reject<InvalidOperationException>(() => spring.Length = 20); Reject<InvalidOperationException>(() => spring.RestLength = 20);
        Reject<InvalidOperationException>(() => spring.Stiffness = 2); Reject<InvalidOperationException>(() => spring.Damping = 3);
        Check(spring.Length == length, "Spring geometry rejects before local fields change");
    }
}
