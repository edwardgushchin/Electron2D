using Electron2D;

public static partial class BackendChecks
{
    private static void JointLifecycleIdentity(SceneTree tree, World world, RID first, RID second, RID[] all, Solver? solver)
    {
        var area = PhysicsServer.AreaCreate();
        try
        {
            for (var i = 0; i < all.Length; i++)
            {
                var id = all[i]; var role = i % 3;
                SetJointParameters(id, 1);
                var before = JointParameterState(id); var attachment = solver?.Joints[id];
                Reject<ArgumentOutOfRangeException>(() => PhysicsServer.JointMakePin(id, new(float.NaN, 0), first, second));
                Reject<ArgumentException>(() => MakeJoint(id, role, first, first));
                Reject<ArgumentException>(() => MakeJoint(id, role, area, second));
                if (i >= 3) Reject<InvalidOperationException>(() => MakeJoint(id, (role + 1) % 3, first, second));
                Check((int)PhysicsServer.JointGetType(id) == role && JointParameterState(id) == before &&
                    (solver is null || ReferenceEquals(solver.Joints[id], attachment)), "Rejected candidates preserve the old role/profile/attachment");
                PhysicsServer.JointClear(id); PhysicsServer.JointClear(id);
                Check(PhysicsServer.JointGetType(id) == PhysicsServer.JointType.Empty && PhysicsServer.JointGetBias(id) == before.Bias &&
                    PhysicsServer.JointGetMaxForce(id) == before.MaxForce, "Clear preserves RID and common policy while forgetting the concrete connection");
                if (i < 3) Reject<ArgumentException>(() => PhysicsServer.PinJointGetSoftness(id));
                else Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(id));
                MakeJoint(id, role, first, second); SetJointParameters(id, 1);
            }
            tree.PhysicsFrame(1d / 60);
            var retained = all.Select(JointParameterState).ToArray();
            var frames = solver is null ? null : all.Select(id => solver.Joints[id].Settings).ToArray();
            PhysicsServer.BodySetSpace(second, default);
            Check(solver is null || solver.Joints.Count == 0, "Endpoint departure removes every numerical attachment");
            for (var i = 0; i < all.Length; i++) Check((int)PhysicsServer.JointGetType(all[i]) == i % 3 && JointParameterState(all[i]) == retained[i], "Pending endpoints retain concrete identity and all parameters");
            PhysicsServer.BodySetSpace(second, world.Space);
            for (var i = 0; i < all.Length; i++)
            {
                Check(JointParameterState(all[i]) == retained[i], "Reentry preserves retained joint values");
                if (solver is not null) Check(solver.Joints[all[i]].Settings == frames![i], "Fresh numerical attachments receive unchanged sampled frames and policy");
            }
            Task.Run(() =>
            {
                foreach (var id in all)
                {
                    Reject<InvalidOperationException>(() => PhysicsServer.JointClear(id));
                    Reject<InvalidOperationException>(() => PhysicsServer.JointGetType(id));
                    Reject<InvalidOperationException>(() => MakeJoint(id, 0, first, second));
                }
            }).GetAwaiter().GetResult();
            var callbacks = 0;
            PhysicsServer.BodySetStateSyncCallback(second, _ => { callbacks++; PhysicsServer.JointClear(all[0]); MakeJoint(all[0], 0, first, second); });
            try { tree.PhysicsFrame(1d / 60); }
            finally { PhysicsServer.BodySetStateSyncCallback(second, null); }
            Check(callbacks == 1 && PhysicsServer.JointGetType(all[0]) == PhysicsServer.JointType.Pin, "Ordinary callbacks may replace the next interval's connection");
        }
        finally { PhysicsServer.FreeRID(area); }
    }
}
