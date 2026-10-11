using Electron2D;

public static partial class BackendChecks
{
    private static void JointParameterLifetime(SceneTree tree, World world, RID raw, RID[] all, Solver? solver)
    {
        foreach (var id in all)
        {
            var before = JointParameterState(id);
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.JointSetBias(id, 2));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.JointSetMaxBias(id, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.JointSetMaxForce(id, -1));
            if (PhysicsServer.JointGetType(id) == PhysicsServer.JointType.Pin)
            {
                Reject<ArgumentOutOfRangeException>(() => PhysicsServer.PinJointSetSoftness(id, -1));
                PhysicsServer.PinJointSetAngularLimitEnabled(id, true);
                before = JointParameterState(id);
                Reject<ArgumentOutOfRangeException>(() => PhysicsServer.PinJointSetAngularLimitLower(id, 1));
                Reject<ArgumentOutOfRangeException>(() => PhysicsServer.PinJointSetAngularLimitUpper(id, -1));
                Reject<ArgumentOutOfRangeException>(() => PhysicsServer.PinJointSetMotorTargetVelocity(id, float.NaN));
                Reject<ArgumentOutOfRangeException>(() => PhysicsServer.PinJointSetMotorMaxTorque(id, -1));
            }
            if (PhysicsServer.JointGetType(id) == PhysicsServer.JointType.DampedSpring)
            {
                Reject<ArgumentOutOfRangeException>(() => PhysicsServer.DampedSpringJointSetRestLength(id, -1));
                Reject<ArgumentOutOfRangeException>(() => PhysicsServer.DampedSpringJointSetStiffness(id, float.NaN));
                Reject<ArgumentOutOfRangeException>(() => PhysicsServer.DampedSpringJointSetDamping(id, -1));
            }
            Check(JointParameterState(id) == before, "Invalid candidates leave the complete common and concrete profile unchanged");
        }
        if (solver is not null) solver.JointHook = null;
        PhysicsServer.BodySetSpace(raw, default);
        foreach (var id in all) SetJointParameters(id, 1);
        var retained = all.Select(JointParameterState).ToArray();
        PhysicsServer.BodySetSpace(raw, world.Space); tree.PhysicsFrame(1d / 60);
        for (var i = 0; i < all.Length; i++)
        {
            Check(JointParameterState(all[i]) == retained[i], "Endpoint reentry preserves identity, role and authored parameters");
            VerifyJointParameters(all[i], solver);
        }
        Task.Run(() =>
        {
            foreach (var id in all)
            {
                Reject<InvalidOperationException>(() => PhysicsServer.JointSetBias(id, .3f));
                Reject<InvalidOperationException>(() => PhysicsServer.JointGetMaxForce(id));
            }
        }).GetAwaiter().GetResult();
    }
}
