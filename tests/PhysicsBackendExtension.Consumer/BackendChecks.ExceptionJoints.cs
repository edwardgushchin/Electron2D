using Electron2D;

public static partial class BackendChecks
{
    private static void ExceptionJoints(SceneTree tree, SubViewport root, RigidBody scene, StaticBody peer, Solver? solver)
    {
        var joints = new[] { PhysicsServer.JointCreate(), PhysicsServer.JointCreate(), PhysicsServer.JointCreate() };
        using var node = new DampedSpringJoint(); root.AddChild(node);
        try
        {
            PhysicsServer.JointMakePin(joints[0], Vector2.Zero, scene.GetRID(), peer.GetRID());
            PhysicsServer.JointMakeGroove(joints[1], new(-1, 0), new(1, 0), Vector2.Zero, scene.GetRID(), peer.GetRID());
            Spring(joints[2]); Spring(node.GetRID());
            var all = joints.Append(node.GetRID()).ToArray();
            Check(Listed() && all.All(PhysicsServer.JointIsDisabledCollisionsBetweenBodies), "All three raw joint types and a scene joint contribute one deduplicated peer");
            if (solver is not null)
            {
                solver.BodyHook = _ =>
                {
                    foreach (var id in all)
                    {
                        Reject<InvalidOperationException>(() => PhysicsServer.JointDisableCollisionsBetweenBodies(id, false));
                        Check(PhysicsServer.JointIsDisabledCollisionsBetweenBodies(id), "Borrowed joint policy rejects before its retained value changes");
                    }
                    Reject<InvalidOperationException>(() => node.DisableCollision = false);
                };
                try { PhysicsServer.BodySetLinearVelocity(scene.GetRID(), Vector2.Zero); }
                finally { solver.BodyHook = null; }
            }
            scene.AddCollisionExceptionWith(peer);
            foreach (var id in all) PhysicsServer.JointDisableCollisionsBetweenBodies(id, false);
            Check(Listed(), "Disabling every joint contribution preserves an explicit exception");
            scene.RemoveCollisionExceptionWith(peer); Check(!Listed(), "Removing the final explicit entry clears the union");
            foreach (var id in all) PhysicsServer.JointDisableCollisionsBetweenBodies(id, true);
            foreach (var id in joints)
            {
                PhysicsServer.JointClear(id); Check(Listed(), "Clearing one joint preserves the other independent contributions");
            }
            PhysicsServer.JointDisableCollisionsBetweenBodies(node.GetRID(), false); Check(!Listed(), "The final joint contribution clears without an explicit entry");
            Spring(joints[2]); node.DisableCollision = true;
            for (var i = 0; i < 64; i++) Cycle();
            GC.Collect(); GC.WaitForPendingFinalizers();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var total = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Cycle();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; total = GC.GetTotalAllocatedBytes(true) - total;
            Check(owner == 0 && total == 0, $"Scene/raw joint collision-policy edits and 128 full frames allocated {owner}/{total} B");
            Check(node.DisableCollision && PhysicsServer.JointIsDisabledCollisionsBetweenBodies(node.GetRID()), "Scene and server collision-policy getters share identity");
        }
        finally
        {
            if (solver is not null) solver.BodyHook = null;
            foreach (var id in joints) PhysicsServer.FreeRID(id);
        }

        bool Listed() => scene.GetCollisionExceptions() is [var body] && ReferenceEquals(body, peer);
        void Spring(RID id)
        {
            PhysicsServer.JointMakeDampedSpring(id, scene.GlobalPosition, peer.GlobalPosition, scene.GetRID(), peer.GetRID());
            PhysicsServer.DampedSpringJointSetStiffness(id, 0); PhysicsServer.DampedSpringJointSetDamping(id, 0);
        }
        void Cycle()
        {
            PhysicsServer.JointDisableCollisionsBetweenBodies(joints[2], false); node.DisableCollision = false;
            tree.PhysicsFrame(1d / 60);
            PhysicsServer.JointDisableCollisionsBetweenBodies(joints[2], true); node.DisableCollision = true;
            tree.PhysicsFrame(1d / 60);
        }
    }
}
