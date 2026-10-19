using Electron2D;

public static partial class BackendChecks
{
    private static void JointLifecycleFailures(Solver solver, RID first, RID second, RID id)
    {
        var probe = PhysicsServer.JointCreate();
        try
        {
            for (var role = 0; role < 3; role++)
            {
                solver.FailJointFactory = true;
                try { Reject<InvalidOperationException>(() => MakeJoint(id, role, first, second)); }
                finally { solver.FailJointFactory = false; }
                Check(!solver.Joints.ContainsKey(id) && (int)PhysicsServer.JointGetType(id) == role, "Factory failure retains validated identity/role for an explicit retry");
                solver.NullJointFactory = true;
                try { Reject<InvalidOperationException>(() => MakeJoint(id, role, first, second)); }
                finally { solver.NullJointFactory = false; }
                MakeJoint(id, role, first, second);
                var original = solver.Joints[id];
                solver.ReusedJoint = original;
                try { Reject<InvalidOperationException>(() => MakeJoint(probe, role, first, second)); }
                finally { solver.ReusedJoint = null; }
                Check(ReferenceEquals(solver.Joints[id], original) && !solver.Joints.ContainsKey(probe), "Rejected joint reuse preserves the original attachment");
                MakeJoint(probe, role, first, second); PhysicsServer.JointClear(probe);

                solver.FailJointAttach = true;
                try { Reject<InvalidOperationException>(() => MakeJoint(probe, role, first, second)); }
                finally { solver.FailJointAttach = false; }
                Check(!solver.Joints.ContainsKey(probe), "Failed attachment is detached before returning its error");

                solver.FailJointAttach = solver.FailJointDetach = true;
                try
                {
                    try { MakeJoint(probe, role, first, second); throw new InvalidOperationException("Expected joint attachment aggregate"); }
                    catch (AggregateException error)
                    {
                        Check(error.InnerExceptions.Count == 2 && error.InnerExceptions[0].Message == "Joint attachment failure" &&
                            error.InnerExceptions[1].Message == "Joint detach failure", "Attachment and cleanup retain both failures");
                    }
                }
                finally { solver.FailJointAttach = solver.FailJointDetach = false; }
                Check(!solver.Joints.ContainsKey(probe), "Failed cleanup removes the matching numerical attachment");
                MakeJoint(probe, role, first, second);
                solver.FailJointDetach = true;
                try { Reject<InvalidOperationException>(() => PhysicsServer.JointClear(probe)); }
                finally { solver.FailJointDetach = false; }
                Check(!solver.Joints.ContainsKey(probe) && PhysicsServer.JointGetType(probe) == PhysicsServer.JointType.Empty, "A detach error still commits cleared identity and retires the numerical attachment");
                PhysicsServer.JointClear(probe); PhysicsServer.JointClear(id);
                Check(PhysicsServer.JointGetType(probe) == PhysicsServer.JointType.Empty, "Repeated clear remains idempotent after reported cleanup errors");
            }
        }
        finally
        {
            solver.FailJointFactory = solver.NullJointFactory = solver.FailJointAttach = solver.FailJointDetach = false;
            solver.ReusedJoint = null; PhysicsServer.FreeRID(probe);
        }
    }
}
