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
            foreach (var id in all) PhysicsServer.JointSetMaxForce(id, 0);
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
                var previous = solver.StepHook;
                solver.StepHook = () =>
                {
                    previous?.Invoke();
                    foreach (var id in all)
                    {
                        Reject<InvalidOperationException>(() => PhysicsServer.JointDisableCollisionsBetweenBodies(id, false));
                        Reject<InvalidOperationException>(() => PhysicsServer.JointDisableCollisionsBetweenBodies(id, true));
                        Reject<InvalidOperationException>(() => PhysicsServer.JointIsDisabledCollisionsBetweenBodies(id));
                    }
                    Check(solver.ReadExceptions(scene.GetRID()).Contains(peer.GetRID()), "Actual Solve reads the common joint union without a numerical query");
                };
                try { tree.PhysicsFrame(1d / 60); } finally { solver.StepHook = previous; }
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
            JointResponse();
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
        void JointResponse()
        {
            foreach (var id in joints)
            {
                if (PhysicsServer.JointGetType(id) == PhysicsServer.JointType.Empty) MakeJoint(id, Array.IndexOf(joints, id), scene.GetRID(), peer.GetRID());
                PhysicsServer.JointSetMaxForce(id, 0);
            }
            var all = joints.Append(node.GetRID()).ToArray();
            using var motion = new PhysicsTestMotionParameters { Motion = new(60, 0) };
            foreach (var selected in all)
            {
                foreach (var id in all) PhysicsServer.JointDisableCollisionsBetweenBodies(id, id == selected);
                Reset(); motion.From = new(0, Vector2.Zero);
                Check(!PhysicsServer.BodyTestMotion(scene.GetRID(), motion), "Each joint role immediately suppresses body motion independently");
                for (var i = 0; i < 50; i++) tree.PhysicsFrame(1d / 60);
                Check(scene.GlobalPosition.X > 40 && PhysicsServer.BodyGetDirectState(scene.GetRID())!.GetContactCount() == 0, "Disabled collision passes actual bodies through every zero-force joint role");
                PhysicsServer.JointDisableCollisionsBetweenBodies(selected, false); Reset();
                Check(PhysicsServer.BodyTestMotion(scene.GetRID(), motion), "Removing the final joint contribution immediately restores motion filtering");
                for (var i = 0; i < 40; i++) tree.PhysicsFrame(1d / 60);
                var state = PhysicsServer.BodyGetDirectState(scene.GetRID())!;
                Check(scene.GlobalPosition.X is > 23 and < 25.1f && MathF.Abs(state.LinearVelocity.X) < .01f && state.GetContactCount() > 0 && state.GetContactCollider(0) == peer.GetRID(), "Every joint collision policy restores actual contact response and peer identity");
            }
            foreach (var id in joints) PhysicsServer.JointClear(id);
            Spring(joints[2]); PhysicsServer.JointSetMaxForce(joints[2], 0); node.DisableCollision = true;
            void Reset() { PhysicsServer.BodySetTransform(scene.GetRID(), new(0, Vector2.Zero)); PhysicsServer.BodySetLinearVelocity(scene.GetRID(), new(60, 0)); }
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
