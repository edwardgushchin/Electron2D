using Electron2D;

public static partial class BackendChecks
{
    public static void Run()
    {
        AllocationWarmupCheck.Verify(Collect);
        RunJointResponse();
        RunHistory();
        RunSleep();
        RunContactResponse();
        Solver? solver = null;
        RunShapeResources();
        RunShapeGeometry();
        RunShapePolicy();
        RunFactoryViews();
        RunDebugContacts();
        RunLifecycle();
        StateChecks();
        ForceChecks();
        ExceptionChecks();
        JointParameterChecks();
        JointLifecycleChecks();
        ResourceReleaseChecks();
        AreaMonitorChecks();
        RunContactThreshold();
        RunMotionExclusions();
        RunNotificationPhase();
        RunShapePairs();
        IntegrationChecks();
        MotionRoleChecks();
        ParameterReadChecks();
        WorldPolicyChecks();
        SceneSpaceChecks();
        AreaFieldChecks();
        BodyMutationChecks();
        ShapeMutationChecks();
        IdentityChecks();
        RunCustom();
        PortableEpisodeChecks();
        RunConstruction();
        RunPicking();
        RunShapeOwners();
        SelectionChecks();
        DebugFailureChecks();
        var calls = 0;
        PhysicsServerManager.RegisterServer("Circles", () => { calls++; return solver = new(); });
        using var world = new World("Circles");
        using var alias = (World)world.Duplicate();
        Check(calls == 0 && world.RequestedPhysicsBackend == PhysicsServer.Backend.Extension && world.RequestedPhysicsBackendName == "Circles", "Named selection stays lazy");
        using var circle = new CircleShape { Radius = 3 };
        using var root = new SubViewport { World = world };
        var scene = new RigidBody
        {
            GravityScale = 0,
            CanSleep = false,
            Mass = 2,
            Inertia = 8,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace,
            ConstantForce = new(12, 0)
        };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate();
        PhysicsDirectBodyState? rawView = null; var callbacks = 0;
        try
        {
            PhysicsServer.AreaSetGravity(world.Space, 0); PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetMass(raw, 4); PhysicsServer.BodySetInertia(raw, 16);
            PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
            PhysicsServer.BodySetConstantForce(raw, new(24, 0)); PhysicsServer.BodySetTransform(raw, new(0, new(0, 30)));
            PhysicsServer.BodySetSpace(raw, world.Space);
            PhysicsServer.BodySetStateSyncCallback(raw, state => { rawView = state; callbacks++; Check(state.TotalGravity == Vector2.Zero, "Independent field publication reaches the raw callback"); });
            tree.PhysicsFrame(1d / 60);
            Check(world.PhysicsBackend == PhysicsServer.Backend.Extension && PhysicsServer.SpaceGetBackend(world.Space) == PhysicsServer.Backend.Extension,
                "An independent solver has a distinct implementation identity");
            Check(scene.LinearVelocity.DistanceTo(new(.1f, 0)) < .000001f && PhysicsServer.BodyGetLinearVelocity(raw).DistanceTo(new(.1f, 0)) < .000001f,
                "Independent scene/raw integration consumes engine-unit mass and persistent forces");
            Check(calls == 1 && alias.Space == world.Space && world.PhysicsBackendName == "Circles", "Aliases retain one lazy implementation");
            Reject<InvalidOperationException>(() => solver!.Dispose());
            solver!.StatusHook = () => Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetTick(world.Space));
            _ = PhysicsServer.BodyGetLinearVelocity(raw);
            solver.StatusHook = null;
            Check(solver!.Bodies.Count == 2 && solver.Steps == 1 && callbacks == 1 && rawView is not null, "Complete owner step publishes actual independent body state");
            PhysicsServerManager.RegisterServer("Reuse", () => solver);
            using (var reuse = new World("Reuse"))
                Reject<InvalidOperationException>(() => _ = reuse.Space);
            Check(solver.Bodies.Count == 2 && !solver.Finished, "Rejected implementation reuse preserves the first world and its attachments");
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            var firstPose = scene.GlobalTransform; var rawPose = PhysicsServer.BodyGetTransform(raw);
            tree.PhysicsFrame(1d / 60); checkpoint.Restore();
            Check(scene.GlobalTransform == firstPose && PhysicsServer.BodyGetTransform(raw) == rawPose, "Independent private history restores scene/raw state and pose");
            using var map = new PhysicsSnapshotMap(world.Space); map.Bind(1, 1, scene.GetRID()); map.Bind(2, 1, raw);
            var snapshot = new PhysicsSnapshot(65536);
            map.Capture(snapshot); tree.PhysicsFrame(1d / 60); map.Apply(snapshot);
            Check(scene.GlobalTransform == firstPose && PhysicsServer.BodyGetTransform(raw) == rawPose, "Independent portable values restore through the common map");
            tree.DebugCollisionsHint = true;
            tree.PhysicsFrame(1d / 60);
            Check(solver.DebugReads > 0, "Scene debug capture reaches the independent implementation");
            tree.DebugCollisionsHint = false;
            solver.StepHook = () =>
            {
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(raw));
                Reject<InvalidOperationException>(() => root.RemoveChild(scene));
            };
            tree.PhysicsFrame(1d / 60); solver.StepHook = null;
            using var pointQuery = new PhysicsPointQueryParameters { Position = scene.Position };
            pointQuery.Exclude = [scene.GetRID()];
            Check(!world.DirectSpaceState.IntersectPoint(pointQuery).Any(h => h.ColliderRID == scene.GetRID()), "Point query borrows explicit exclusions");
            Collect(); for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Independent complete frame allocated {owner}/{all} owner/all-thread bytes");
            Check(solver.Bodies[raw].Position.X > rawPose.Origin.X + 1 && scene.Position.X > firstPose.Origin.X + 1,
                "Independent warm frames continue real scene/raw motion");
            using var parameters = new PhysicsTestMotionParameters { From = PhysicsServer.BodyGetTransform(raw), Motion = new(0, -40) };
            using var motion = new PhysicsTestMotionResult();
            Check(PhysicsServer.BodyTestMotion(raw, parameters, motion) && motion.GetColliderRID() == scene.GetRID(), "Independent circle motion reaches the selected output boundary");
            PhysicsServer.BodyAddCollisionException(raw, scene.GetRID());
            Check(!PhysicsServer.BodyTestMotion(raw, parameters, motion), "Typed explicit exceptions suppress the independent body query");
            PhysicsServer.BodyRemoveCollisionException(raw, scene.GetRID());
            var direct = world.DirectSpaceState;
            using var ray = new PhysicsRayQueryParameters { From = scene.Position + new Vector2(-10, 0), To = scene.Position + new Vector2(10, 0) };
            Check(direct.IntersectRay(ray)?.ColliderRID == scene.GetRID(), "Independent ray geometry reaches the cached direct-space view");
            using var query = new PhysicsShapeQueryParameters { Shape = circle, Transform = new(0, scene.Position + new Vector2(1, 0)), Margin = 0 };
            query.Exclude = [scene.GetRID()];
            Check(!direct.IntersectShape(query).Any(h => h.ColliderRID == scene.GetRID()) && direct.GetRestInfo(query) is null, "Shape/rest exclusions reach the independent implementation");
            query.Exclude = [];
            Check(direct.IntersectShape(query).Any(h => h.ColliderRID == scene.GetRID()) && direct.GetRestInfo(query)?.ColliderRID == scene.GetRID(), "Independent shape and rest queries share logical identity");
            var retainedVelocity = solver.Bodies[raw].Velocity; solver.Bodies[raw].Velocity = new(float.NaN, 0);
            Reject<InvalidOperationException>(() => PhysicsServer.BodyGetLinearVelocity(raw)); solver.Bodies[raw].Velocity = retainedVelocity;
            var area = new Area { Position = scene.Position };
            area.AddChild(new CollisionShape { Shape = circle }); root.AddChild(area);
            var enters = 0; area.BodyEntered += _ => enters++;
            tree.PhysicsFrame(1d / 60);
            Check(enters == 1 && area.GetOverlappingBodies().Contains(scene), "Independent Area pairs reach ordinary scene events");
            scene.MaxContactsReported = 2; PhysicsServer.BodySetMaxContactsReported(raw, 2);
            PhysicsServer.BodySetTransform(raw, scene.GlobalTransform); PhysicsServer.BodySetLinearVelocity(raw, new(-60, 0));
            tree.PhysicsFrame(1d / 60);
            var sceneView = PhysicsServer.BodyGetDirectState(scene.GetRID())!;
            Check(sceneView.GetContactCount() == 1 && sceneView.GetContactCollider(0) == raw && rawView!.GetContactCount() == 1,
                "Independent circle response publishes logical scene/raw contact snapshots");
            var contacts = sceneView.GetContactCount(); var beforeCallbacks = callbacks; var tick = PhysicsServer.SpaceGetTick(world.Space);
            solver.ContactFailure = scene.GetRID();
            Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60)); solver.ContactFailure = default;
            Check(sceneView.GetContactCount() == contacts && callbacks > beforeCallbacks && !solver.Finished &&
                PhysicsServer.SpaceGetTick(world.Space) == tick + 1, "One contact receiver failure preserves its snapshot while later callbacks and committed tick continue");
            tree.PhysicsFrame(1d / 60);
            var joint = PhysicsServer.JointCreate();
            try
            {
                PhysicsServer.JointMakeDampedSpring(joint, scene.Position, PhysicsServer.BodyGetTransform(raw).Origin, scene.GetRID(), raw);
                PhysicsServer.DampedSpringJointSetRestLength(joint, 2); PhysicsServer.DampedSpringJointSetStiffness(joint, 30);
                PhysicsServer.DampedSpringJointSetDamping(joint, 0); PhysicsServer.JointSetMaxForce(joint, 20);
                var before = scene.LinearVelocity;
                tree.PhysicsFrame(1d / 60);
                Check(solver.Joints[joint].Settings.SpringStiffness == 30 && solver.Joints[joint].Settings.MaxForce == 20 &&
                    scene.LinearVelocity != before && solver.Exceptions.Contains((scene.GetRID(), raw)),
                    "Engine-assigned joint identity and numerical configuration reach independent spring response and typed joint exclusions");
            }
            finally { PhysicsServer.FreeRID(joint); }
            Reject<ArgumentOutOfRangeException>(() => new World(PhysicsServer.Backend.Extension));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.SpaceCreate(PhysicsServer.Backend.Extension));
            Console.WriteLine($"Independent registered adapter: scene/raw forces, callbacks, private/portable restore, lifetime and 64 full warmed frames: {owner}/{all} B passed.");
        }
        finally { PhysicsServer.FreeRID(raw); }
        Solver? failedSolver = null;
        PhysicsServerManager.RegisterServer("Failure", () => failedSolver = new Solver { FailSolve = true });
        var owned = PhysicsServer.SpaceCreate("Circles");
        var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.AreaSetGravity(owned, 0); PhysicsServer.AreaSetLinearDamp(owned, 0);
            PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetSpace(body, owned);
            PhysicsServer.BodySetLinearVelocity(body, new(60, 0)); PhysicsServer.SpaceSetActive(owned, true);
            PhysicsServer.SpaceStep(owned, 1d / 60);
            Check(PhysicsServer.BodyGetTransform(body).Origin.DistanceTo(Vector2.Right) < .000001f && PhysicsServer.SpaceGetBackendName(owned) == "Circles", "Explicit owned space executes independent raw numerical motion");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(owned); }
        var failed = PhysicsServer.SpaceCreate("Failure");
        try
        {
            PhysicsServer.SpaceSetActive(failed, true);
            Reject<AggregateException>(() => PhysicsServer.SpaceStep(failed, 1d / 60));
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(failed, 1d / 60));
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetDirectState(failed).IntersectRay(new()));
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetTick(failed));
            Check(failedSolver!.Steps == 0 && failedSolver.Synchronizations == 1, "A failed independent solve is terminal without completing or replaying an interval");
        }
        finally { PhysicsServer.FreeRID(failed); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
    public static void RunAllocationWarmup() => AllocationWarmupCheck.Verify(Collect);
    private static void Collect() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers(); }
}
