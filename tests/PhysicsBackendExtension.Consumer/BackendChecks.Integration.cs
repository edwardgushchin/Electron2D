using Electron2D;

public static partial class BackendChecks
{
    public static void RunIntegration(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { IntegrationChecks(backend); IntegrationOperations(backend); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
    private static void IntegrationChecks(PhysicsServer.Backend? backend = null)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("IntegrationChecks", () => solver = new());
        using var world = backend is { } selected ? new World(selected) : new World("IntegrationChecks");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 };
        var scene = new IntegrationBody { GravityScale = 0, CanSleep = false };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        _ = world.Space;
        if (solver is not null) solver.IdentityHook = RejectSceneWrites;
        using var tree = new SceneTree(root);
        if (solver is not null) solver.IdentityHook = RejectSceneWrites;
        PhysicsServer.BodyAttachCanvasInstanceID(scene.GetRID(), 23);
        if (solver is not null) solver.IdentityHook = null;
        Check(!scene.CustomIntegrator && scene.MaxContactsReported == 0,
            "Rejected initial/live integration edits preserve configuration");
        var raw = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID());
            PhysicsServer.BodySetTransform(raw, new(0, new Vector2(100, 0)));
            PhysicsServer.BodySetSpace(raw, world.Space);
            foreach (var rid in new[] { scene.GetRID(), raw })
            {
                PhysicsServer.BodySetGravityScale(rid, 0); PhysicsServer.BodySetCanSleep(rid, false);
                PhysicsServer.BodySetLinearDampMode(rid, RigidBody.DampMode.Replace);
                PhysicsServer.BodySetAngularDampMode(rid, RigidBody.DampMode.Replace);
                PhysicsServer.BodySetLinearDamp(rid, 0); PhysicsServer.BodySetAngularDamp(rid, 0);
                PhysicsServer.BodySetConstantForce(rid, new(6, 0));
                PhysicsServer.BodySetOmitForceIntegration(rid, true);
                PhysicsServer.BodyApplyCentralForce(rid, new(6, 0));
            }
            tree.PhysicsFrame(1d / 60);
            foreach (var rid in new[] { scene.GetRID(), raw })
            {
                Check(PhysicsServer.BodyIsOmittingForceIntegration(rid) && PhysicsServer.BodyGetLinearVelocity(rid) == Vector2.Zero,
                    "Omission suppresses ordinary and constant forces for scene/raw bodies");
                PhysicsServer.BodyApplyCentralImpulse(rid, new(2, 0));
                Check(MathF.Abs(PhysicsServer.BodyGetLinearVelocity(rid).X - 2) < .001f, "Omission retains real impulse response");
                PhysicsServer.BodySetLinearVelocity(rid, Vector2.Zero);
                PhysicsServer.BodySetOmitForceIntegration(rid, false);
            }
            tree.PhysicsFrame(1d / 60);
            foreach (var rid in new[] { scene.GetRID(), raw })
                Check(MathF.Abs(PhysicsServer.BodyGetLinearVelocity(rid).X - .1f) < .001f,
                    "Reenabled integration applies persistent force once without replaying omitted pending force");
            var rawTicks = 0;
            PhysicsServer.BodySetForceIntegrationCallback(scene.GetRID(), (state, data) =>
            {
                Check(ReferenceEquals(data, scene) && scene.Order == 0, "Typed userdata reaches force callback before scene hook");
                scene.Order = 1; state.LinearVelocity = new(3, 0);
                scene.CustomIntegrator = true; scene.MaxContactsReported = 2;
            }, scene);
            scene.VerifyOrder = true;
            PhysicsServer.BodySetStateSyncCallback(scene.GetRID(), state =>
            {
                Check(scene.Order == 2 && state.LinearVelocity.X == 4, "Sync observer follows scene integration hook");
                scene.Order = 0;
            });
            PhysicsServer.BodySetForceIntegrationCallback(raw, state => { rawTicks++; state.LinearVelocity = new(5, 0); });
            for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            Collect(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && rawTicks == 192, $"Warm complete callback steps allocated {own}/{all} owner/all-thread bytes; raw callbacks {rawTicks}/192");
            PhysicsServer.BodySetForceIntegrationCallback(scene.GetRID(), null); scene.VerifyOrder = false;
            PhysicsServer.BodySetStateSyncCallback(scene.GetRID(), null);
            PhysicsServer.BodySetForceIntegrationCallback(raw, null);
            tree.PhysicsFrame(1d / 60); Check(rawTicks == 192, "Clearing callbacks stops invocation");
            var syncTicks = 0;
            PhysicsServer.BodySetForceIntegrationCallback(raw, state =>
            { state.LinearVelocity = new(7, 0); throw new InvalidOperationException("Expected game callback failure"); });
            PhysicsServer.BodySetStateSyncCallback(raw, _ => syncTicks++);
            Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
            Check(syncTicks == 1 && PhysicsServer.BodyGetLinearVelocity(raw).X == 7,
                "Callback failure preserves committed edits and still invokes sync observer");
            PhysicsServer.BodySetForceIntegrationCallback(raw, null);
            PhysicsServer.BodySetStateSyncCallback(raw, null);
            PhysicsServer.BodySetConstantForce(raw, Vector2.Zero); scene.ConstantForce = Vector2.Zero;
            PhysicsServer.BodySetOmitForceIntegration(raw, true);
            scene.MaxContactsReported = 1; PhysicsServer.BodySetMaxContactsReported(raw, 1);
            Check(PhysicsServer.BodyGetMaxContactsReported(scene.GetRID()) == 1 && PhysicsServer.BodyGetMaxContactsReported(raw) == 1,
                "Scene and server contact caps share authoring");
            PhysicsServer.BodySetTransform(scene.GetRID(), new(0, Vector2.Zero));
            PhysicsServer.BodySetTransform(raw, new(0, new Vector2(5, 0)));
            PhysicsServer.BodySetLinearVelocity(scene.GetRID(), new(10, 0));
            PhysicsServer.BodySetLinearVelocity(raw, new(-10, 0));
            tree.PhysicsFrame(1d / 60);
            var sceneState = PhysicsServer.BodyGetDirectState(scene.GetRID()) ?? throw new InvalidOperationException("Scene view missing");
            var rawState = PhysicsServer.BodyGetDirectState(raw) ?? throw new InvalidOperationException("Raw view missing");
            Check(sceneState.GetContactCount() == 1 && rawState.GetContactCount() == 1 &&
                sceneState.GetContactCollider(0) == raw && rawState.GetContactCollider(0) == scene.GetRID(),
                "Custom integration retains real capped scene/raw contact identities");
            Check(PhysicsServer.BodyGetLinearVelocity(scene.GetRID()).X < 10 && PhysicsServer.BodyGetLinearVelocity(raw).X > -10,
                "Custom integration retains numerical contact response");
            PhysicsServer.BodySetMaxContactsReported(scene.GetRID(), 0); PhysicsServer.BodySetMaxContactsReported(raw, 0);
            tree.PhysicsFrame(1d / 60);
            Check(sceneState.GetContactCount() == 0 && rawState.GetContactCount() == 0, "Clearing caps disables contact publication");
            Console.WriteLine($"Integration policy on {backend?.ToString() ?? "registered"}: omission/impulses, force-scene-sync order, typed userdata and 64 warm frames {own}/{all} B passed.");
        }
        finally { PhysicsServer.FreeRID(raw); }
        void RejectSceneWrites(RID body)
        {
            Reject<InvalidOperationException>(() => scene.CustomIntegrator = true);
            Reject<InvalidOperationException>(() => scene.MaxContactsReported = 2);
            Reject<InvalidOperationException>(() => scene.ContactMonitor = true);
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetOmitForceIntegration(body, true));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetMaxContactsReported(body, 2));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetForceIntegrationCallback(body, null));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetStateSyncCallback(body, null));
        }
    }

    private sealed class IntegrationBody : RigidBody
    {
        internal bool VerifyOrder;
        internal int Order;
        protected override void IntegrateForces(PhysicsDirectBodyState state)
        {
            if (!VerifyOrder) return;
            Check(Order == 1 && state.LinearVelocity.X == 3, "Scene hook receives force callback edits");
            Order = 2; state.LinearVelocity = new(4, 0);
        }
    }
}
