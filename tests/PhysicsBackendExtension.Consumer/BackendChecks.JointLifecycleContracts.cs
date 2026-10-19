using Electron2D;

public static partial class BackendChecks
{
    private static void JointLifecycleContracts(SceneTree tree, World world, PhysicsBody scene, RID raw, Joint[] nodes, RID[] joints, Solver? solver)
    {
        var root = (SubViewport)scene.Parent!;
        using var peer = new RigidBody { Name = "LifecyclePeer", Position = new(100, 0), GravityScale = 0, CanSleep = false }; root.AddChild(peer);
        var rawPeer = PhysicsServer.BodyCreate(); var id = PhysicsServer.JointCreate(); var cases = 0;
        try
        {
            PhysicsServer.BodySetGravityScale(rawPeer, 0); PhysicsServer.BodySetTransform(rawPeer, new(0, new Vector2(100, 0))); PhysicsServer.BodySetSpace(rawPeer, world.Space);
            Check(PhysicsServer.JointGetType(id) == PhysicsServer.JointType.Empty, "JointCreate publishes a live empty resource without a numerical attachment");
            for (var oldRole = -1; oldRole < 3; oldRole++)
                for (var nextRole = 0; nextRole < 4; nextRole++)
                    for (var owners = 0; owners < 4; owners++)
                    {
                        var first = (owners & 1) == 0 ? scene.GetRID() : raw;
                        var second = (owners & 2) == 0 ? peer.GetRID() : rawPeer;
                        PhysicsServer.JointClear(id);
                        if (oldRole >= 0) { MakeJoint(id, oldRole, first, second); SetJointParameters(id, 1); }
                        PhysicsServer.JointSetBias(id, .3f); PhysicsServer.JointSetMaxBias(id, 7); PhysicsServer.JointSetMaxForce(id, 11);
                        PhysicsServer.JointDisableCollisionsBetweenBodies(id, false);
                        var oldAttachment = solver is not null && solver.Joints.TryGetValue(id, out var previous) ? previous : null;
                        if (nextRole == 3) PhysicsServer.JointMakePin(id, Vector2.Zero, first); else MakeJoint(id, nextRole, first, second);
                        Check((int)PhysicsServer.JointGetType(id) == (nextRole == 3 ? 0 : nextRole) && PhysicsServer.JointGetBias(id) == .3f && PhysicsServer.JointGetMaxBias(id) == 7 && PhysicsServer.JointGetMaxForce(id) == 11 && !PhysicsServer.JointIsDisabledCollisionsBetweenBodies(id),
                            "Every role replacement retains RID and the complete common policy");
                        if (nextRole is 0 or 3)
                            Check(PhysicsServer.PinJointGetSoftness(id) == 0 && !PhysicsServer.PinJointGetAngularLimitEnabled(id) && PhysicsServer.PinJointGetAngularLimitLower(id) == 0 && PhysicsServer.PinJointGetAngularLimitUpper(id) == 0 && !PhysicsServer.PinJointGetMotorEnabled(id) && PhysicsServer.PinJointGetMotorTargetVelocity(id) == 0 && PhysicsServer.PinJointGetMotorMaxTorque(id) == 10,
                                "Pin replacement resets its complete concrete family");
                        if (nextRole == 2)
                            Check(PhysicsServer.DampedSpringJointGetRestLength(id) == 100 && PhysicsServer.DampedSpringJointGetStiffness(id) == 20 && PhysicsServer.DampedSpringJointGetDamping(id) == 1.5f, "Spring replacement restores sampled rest and raw factory defaults");
                        if (solver is not null)
                            Check(!ReferenceEquals(oldAttachment, solver.Joints[id]) && solver.Joints[id].Endpoints == (first, nextRole == 3 ? default : second), "Selected replacement uses fresh attachment identity and exact incoming body RIDs");
                        cases++;
                    }
            PhysicsServer.JointClear(id);
            var stale = PhysicsServer.JointCreate(); PhysicsServer.FreeRID(stale);
            foreach (var invalid in new[] { default, raw, world.Space, stale })
            {
                Reject<ArgumentException>(() => PhysicsServer.JointGetType(invalid)); Reject<ArgumentException>(() => PhysicsServer.JointClear(invalid));
                for (var role = 0; role < 3; role++) Reject<ArgumentException>(() => MakeJoint(invalid, role, scene.GetRID(), raw));
            }
            var all = joints.Concat(nodes.Select(n => n.GetRID())).ToArray();
            Task.Run(() =>
            {
                foreach (var joint in all)
                {
                    Reject<InvalidOperationException>(() => PhysicsServer.JointGetType(joint)); Reject<InvalidOperationException>(() => PhysicsServer.JointClear(joint));
                    for (var role = 0; role < 3; role++) Reject<InvalidOperationException>(() => MakeJoint(joint, role, scene.GetRID(), raw));
                }
            }).GetAwaiter().GetResult();
            if (solver is not null)
            {
                var previous = solver.StepHook;
                solver.StepHook = () =>
                {
                    previous?.Invoke();
                    foreach (var joint in all)
                    {
                        Reject<InvalidOperationException>(() => PhysicsServer.JointGetType(joint)); Reject<InvalidOperationException>(() => PhysicsServer.JointClear(joint));
                        for (var role = 0; role < 3; role++) Reject<InvalidOperationException>(() => MakeJoint(joint, role, scene.GetRID(), raw));
                    }
                    var detached = PhysicsServer.JointCreate();
                    try { Check(PhysicsServer.JointGetType(detached) == PhysicsServer.JointType.Empty, "Global detached RID creation does not borrow the solving world"); PhysicsServer.JointClear(detached); }
                    finally { PhysicsServer.FreeRID(detached); }
                };
                try { tree.PhysicsFrame(1d / 60); } finally { solver.StepHook = previous; }
            }
            var callbacks = 0;
            PhysicsServer.BodySetStateSyncCallback(raw, _ =>
            {
                callbacks++;
                for (var role = 0; role < 3; role++)
                {
                    var next = PhysicsServer.JointCreate();
                    try { MakeJoint(next, role, scene.GetRID(), raw); Check((int)PhysicsServer.JointGetType(next) == role, "Ordinary callbacks may create every concrete joint role"); PhysicsServer.JointClear(next); }
                    finally { PhysicsServer.FreeRID(next); }
                }
            });
            try { tree.PhysicsFrame(1d / 60); } finally { PhysicsServer.BodySetStateSyncCallback(raw, null); }
            Check(callbacks == 1, "Ordinary callback lifecycle family executes once");
            Console.WriteLine($"Joint lifecycle contracts: {cases} empty/role/world-pin/endpoint-owner transitions, complete ID/owner/Solve guards and ordinary callback family passed.");
        }
        finally { PhysicsServer.FreeRID(id); PhysicsServer.FreeRID(rawPeer); }
    }
}
