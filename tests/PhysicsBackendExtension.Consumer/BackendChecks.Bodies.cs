using Electron2D;

public static partial class BackendChecks
{
    private static void BodyMutationChecks()
    {
        Solver? solver = null;
        PhysicsServerManager.RegisterServer("BodyMutations", () => solver = new());
        using var world = new World("BodyMutations"); var space = world.Space;
        using var circle = new CircleShape { Radius = 3 };
        var raw = PhysicsServer.BodyCreate();
        using var root = new SubViewport { World = world };
        var scene = new RigidBody { Position = new(50, 0), GravityScale = 0, CanSleep = false };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        PhysicsDirectBodyState? rawView = null, sceneView = null;
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID());
            PhysicsServer.BodySetTransform(raw, new(0, new Vector2(-50, 0)));
            PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
            solver!.IdentityHook = RejectWrites;
            PhysicsServer.BodySetSpace(raw, space); tree.PhysicsFrame(0); solver.IdentityHook = null;
            Check(PhysicsServer.BodyGetConstantForce(raw) == Vector2.Zero && PhysicsServer.BodyGetConstantForce(scene.GetRID()) == Vector2.Zero &&
                PhysicsServer.BodyGetMass(raw) == 1 && PhysicsServer.BodyGetMass(scene.GetRID()) == 1 &&
                !PhysicsServer.BodyIsOmittingForceIntegration(raw) && !PhysicsServer.BodyIsOmittingForceIntegration(scene.GetRID()) &&
                PhysicsServer.BodyGetMaxContactsReported(raw) == 0 && PhysicsServer.BodyGetMaxContactsReported(scene.GetRID()) == 0,
                "Rejected initial body edits preserve retained authoring");
            rawView = PhysicsServer.BodyGetDirectState(raw); sceneView = PhysicsServer.BodyGetDirectState(scene.GetRID());
            solver.BodyHook = RejectWrites;
            foreach (var b in new[] { raw, scene.GetRID() })
            {
                PhysicsServer.BodySetMass(b, 2); PhysicsServer.BodySetInertia(b, 8);
                PhysicsServer.BodySetLinearVelocity(b, new(6, 0)); PhysicsServer.BodySetAngularVelocity(b, 2);
                PhysicsServer.BodySetCanSleep(b, false);
            }
            solver.BodyHook = null;
            var ticks = 0;
            PhysicsServer.BodySetForceIntegrationCallback(raw, state => { ticks++; state.LinearVelocity = new(6, 0); });
            PhysicsServer.BodySetStateSyncCallback(raw, _ => ticks++);
            tree.PhysicsFrame(1d / 60);
            Check(ticks == 2 && PhysicsServer.BodyGetTransform(raw).Origin.X > -50 && scene.Position.X > 50,
                "Ordinary callbacks and numerical motion remain executable after rejected implementation edits");
            PhysicsServer.BodySetForceIntegrationCallback(raw, null); PhysicsServer.BodySetStateSyncCallback(raw, null);
            var linear = PhysicsServer.BodyGetLinearVelocity(raw); var angular = PhysicsServer.BodyGetAngularVelocity(raw);
            PhysicsServer.BodyApplyCentralImpulse(raw, new(2, 0)); PhysicsServer.BodyApplyTorqueImpulse(raw, 8);
            Check(MathF.Abs(PhysicsServer.BodyGetLinearVelocity(raw).X - linear.X - 1) < 0.001f &&
                MathF.Abs(PhysicsServer.BodyGetAngularVelocity(raw) - angular - 1) < 0.001f, "Mass/inertia determine real selected impulse response");
            Collect(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) EditAndStep();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) EditAndStep();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Registered state edits/full frames allocated {own}/{all} owner/all-thread B");
            Task.Run(() =>
            {
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetMass(raw, 4));
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetConstantTorque(scene.GetRID(), 4));
                Reject<InvalidOperationException>(() => PhysicsServer.BodyGetCanSleep(raw));
            }).GetAwaiter().GetResult();
            Console.WriteLine($"Registered body authoring, actual impulses/callbacks and 64 warm full frames: {own}/{all} owner/all-thread B");
        }
        finally { solver!.IdentityHook = null; solver.BodyHook = null; PhysicsServer.FreeRID(raw); }

        void RejectWrites(RID b)
        {
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetConstantForce(b, new(9, 0)));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetConstantTorque(b, 9));
            Reject<InvalidOperationException>(() => PhysicsServer.BodyAddConstantCentralForce(b, new(9, 0)));
            Reject<InvalidOperationException>(() => PhysicsServer.BodyAddConstantForce(b, new(9, 0), Vector2.One));
            Reject<InvalidOperationException>(() => PhysicsServer.BodyAddConstantTorque(b, 9));
            Reject<InvalidOperationException>(() => PhysicsServer.BodyApplyCentralForce(b, new(9, 0)));
            Reject<InvalidOperationException>(() => PhysicsServer.BodyApplyForce(b, new(9, 0), Vector2.One));
            Reject<InvalidOperationException>(() => PhysicsServer.BodyApplyTorque(b, 9));
            Reject<InvalidOperationException>(() => PhysicsServer.BodyApplyCentralImpulse(b, new(9, 0)));
            Reject<InvalidOperationException>(() => PhysicsServer.BodyApplyImpulse(b, new(9, 0), Vector2.One));
            Reject<InvalidOperationException>(() => PhysicsServer.BodyApplyTorqueImpulse(b, 9));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetMass(b, 9));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetInertia(b, 9));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetCenterOfMass(b, Vector2.One));
            Reject<InvalidOperationException>(() => PhysicsServer.BodyResetMassProperties(b));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetTransform(b, new(0, Vector2.One)));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetLinearVelocity(b, new(9, 0)));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetAngularVelocity(b, 9));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetAxisVelocity(b, new(9, 0)));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetSleeping(b, true));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetCanSleep(b, true));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetGravityScale(b, 9));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetFriction(b, 9));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetBounce(b, 1));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetLinearDamp(b, 9));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetAngularDamp(b, 9));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetLinearDampMode(b, RigidBody.DampMode.Replace));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetAngularDampMode(b, RigidBody.DampMode.Replace));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetForceIntegrationCallback(b, null));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetStateSyncCallback(b, null));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetOmitForceIntegration(b, true));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetMaxContactsReported(b, 2));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetContinuousCollisionDetectionMode(b, CCDMode.CastShape));
            if (b == raw) Reject<InvalidOperationException>(() => PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Static));
            if ((b == raw ? rawView : sceneView) is { } view)
            {
                Reject<InvalidOperationException>(() => view.Sleeping = true);
                Reject<InvalidOperationException>(() => view.LinearVelocity = new(9, 0));
                Reject<InvalidOperationException>(() => view.AngularVelocity = 9);
                Reject<InvalidOperationException>(() => view.Transform = new(0, Vector2.One));
                Reject<InvalidOperationException>(() => view.SetConstantForce(Vector2.One));
                Reject<InvalidOperationException>(() => view.SetConstantTorque(9));
                Reject<InvalidOperationException>(() => view.IntegrateForces());
            }
            Check(PhysicsServer.BodyGetConstantForce(b) == Vector2.Zero && !PhysicsServer.BodyIsOmittingForceIntegration(b) &&
                PhysicsServer.BodyGetMaxContactsReported(b) == 0, "Borrowed authored getters remain usable after rejected writes");
        }
        void EditAndStep()
        {
            PhysicsServer.BodySetLinearVelocity(raw, new(6, 0)); PhysicsServer.BodySetAngularVelocity(raw, 2);
            PhysicsServer.BodySetConstantForce(raw, new(2, 0)); PhysicsServer.BodySetConstantTorque(raw, 32);
            tree.PhysicsFrame(1d / 60);
            Check(PhysicsServer.BodyGetLinearVelocity(raw).X > 6 && PhysicsServer.BodyGetAngularVelocity(raw) > 2,
                "Warm authoring continues real force integration");
        }
    }
}
