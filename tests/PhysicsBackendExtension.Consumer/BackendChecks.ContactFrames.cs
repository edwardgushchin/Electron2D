using Electron2D;

public static partial class BackendChecks
{
    public static void RunContactFrames(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { RunContactFramesCore(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void RunContactFramesCore(PhysicsServer.Backend? backend, bool threaded)
    {
        var ownerThread = Thread.CurrentThread;
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ContactFrames", () => solver = new() { StepHook = () => Check((Thread.CurrentThread != ownerThread) == threaded, "Selected Solve retains the requested numerical thread") });
        using var world = backend is { } selected ? new World(selected) : new World("ContactFrames");
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
            "Multi-peer contact frames use the requested implementation without fallback");
        using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 1 };
        var scene = new RigidBody
        {
            Mass = 2,
            Inertia = 2,
            GravityScale = 0,
            CanSleep = false,
            MaxContactsReported = 8,
            ContinuousCD = CCDMode.CastShape,
            LinearDamp = 0,
            LinearDampMode = RigidBody.DampMode.Replace
        };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene); using var tree = new SceneTree(root);
        var right = PhysicsServer.BodyCreate(); var left = PhysicsServer.BodyCreate(); var third = PhysicsServer.BodyCreate();
        try
        {
            foreach (var body in new[] { scene.GetRID(), right, left, third })
            {
                if (body != scene.GetRID()) { PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(body, circle.GetRID()); }
                PhysicsServer.BodySetFriction(body, 0); PhysicsServer.BodySetBounce(body, .5f);
                if (body != scene.GetRID()) PhysicsServer.BodySetSpace(body, world.Space);
            }
            PhysicsServer.BodySetTransform(right, new(0, new Vector2(20, 0))); PhysicsServer.BodySetTransform(left, new(0, new Vector2(-20, 0)));
            PhysicsServer.BodySetTransform(third, new(0, new Vector2(0, 1000)));
            PhysicsServer.BodySetLinearVelocity(scene.GetRID(), new(3000, 0)); tree.PhysicsFrame(.028);
            using var state = PhysicsServer.BodyGetDirectState(scene.GetRID())!;
            VerifyTransient();
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            var expectedPose = scene.GlobalTransform; var expectedVelocity = scene.LinearVelocity;
            PhysicsServer.BodySetTransform(scene.GetRID(), new(0, new Vector2(100, 0)));
            tree.PhysicsFrame(.01); Check(Count() == 0, "A later contact-free frame does not replay retired transient contacts");
            checkpoint.Restore(); VerifyTransient();
            if (solver is not null)
            {
                using var independent = new OwnedState(solver, scene.GetRID());
                Check(independent.GetContactCount() == 2, "Private implementation history also restores both peers for a caller-created view");
                Check((independent.GetContactImpulse(0) + independent.GetContactImpulse(1)).DistanceTo((scene.LinearVelocity - new Vector2(3000, 0)) * 2) < 20,
                    "Caller-created history observations preserve the complete signed impulse sum");
            }
            Check(scene.GlobalTransform.Origin.DistanceTo(expectedPose.Origin) < .001f && scene.LinearVelocity.DistanceTo(expectedVelocity) < .001f,
                "Private history restores both the numerical multi-peer frame and its physical motion");
            for (var i = 0; i < 128; i++) Cycle(); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Cycle();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Multi-peer transient frame/replay cycles allocated {own}/{all} B");
            VerifyCaps();
            Console.WriteLine($"Contact frames on {backend?.ToString() ?? "registered"}: transient peers, signed impulse sum, cap/depth/ties, private restore and 128 cycles {own}/{all} B passed.");
            void Cycle()
            {
                checkpoint.Restore(); PhysicsServer.BodySetTransform(scene.GetRID(), Transform.Identity);
                PhysicsServer.BodySetLinearVelocity(scene.GetRID(), new(3000, 0)); tree.PhysicsFrame(.028); VerifyTransient(); checkpoint.Capture();
            }
            int Count() => state.GetContactCount();
            void VerifyTransient()
            {
                if (state.GetContactCount() != 2) throw new InvalidOperationException($"Both separated peers remain in the completed frame: {state.GetContactCount()}");
                var sum = Vector2.Zero; var sawRight = false; var sawLeft = false;
                for (var i = 0; i < state.GetContactCount(); i++)
                {
                    var peer = state.GetContactCollider(i); var impulse = state.GetContactImpulse(i); sum += impulse;
                    if (peer == right) { sawRight = true; Check(MathF.Abs(impulse.X + 12000) < 20, "First elastic peer retains its complete negative impulse"); }
                    else if (peer == left) { sawLeft = true; Check(MathF.Abs(impulse.X - 12000) < 20, "Second elastic peer retains its complete positive impulse"); }
                    else throw new InvalidOperationException("Unexpected transient contact peer.");
                }
                Check(sawRight && sawLeft && sum.DistanceTo((scene.LinearVelocity - new Vector2(3000, 0)) * 2) < 20,
                    "The complete multi-peer impulse sum agrees with full-step momentum change");
            }
            void VerifyCaps()
            {
                circle.Radius = 2; PhysicsServer.BodySetContinuousCollisionDetectionMode(scene.GetRID(), CCDMode.Disabled);
                PhysicsServer.BodySetMode(scene.GetRID(), PhysicsServer.BodyMode.Kinematic);
                PhysicsServer.BodySetTransform(scene.GetRID(), Transform.Identity); PhysicsServer.BodySetLinearVelocity(scene.GetRID(), Vector2.Zero);
                PhysicsServer.BodySetTransform(right, new(0, new Vector2(3.75f, 0)));
                PhysicsServer.BodySetTransform(left, new(0, new Vector2(0, 3.5f)));
                PhysicsServer.BodySetTransform(third, new(0, new Vector2(-3, 0)));
                PhysicsServer.BodySetMaxContactsReported(scene.GetRID(), 3); PhysicsServer.BodySetContactsReportedDepthThreshold(scene.GetRID(), 0);
                tree.PhysicsFrame(1d / 60); Check(Count() == 3, "All nonresponsive kinematic/static peers are reported with zero impulse");
                CheckPeers(3, right, left, third);
                PhysicsServer.BodySetMaxContactsReported(scene.GetRID(), 2); tree.PhysicsFrame(1d / 60); CheckPeers(2, left, third);
                PhysicsServer.BodySetMaxContactsReported(scene.GetRID(), 1); tree.PhysicsFrame(1d / 60); CheckPeers(1, third);
                PhysicsServer.BodySetMaxContactsReported(scene.GetRID(), 3); PhysicsServer.BodySetContactsReportedDepthThreshold(scene.GetRID(), .49f);
                tree.PhysicsFrame(1d / 60); CheckPeers(2, left, third);
                PhysicsServer.BodySetContactsReportedDepthThreshold(scene.GetRID(), 1.1f); tree.PhysicsFrame(1d / 60); Check(Count() == 0, "Depth policy filters every shallow candidate before reporting");
                for (var i = 0; i < 128; i++) CapCycle(); Collect();
                var capOwn = GC.GetAllocatedBytesForCurrentThread(); var capAll = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) CapCycle();
                capOwn = GC.GetAllocatedBytesForCurrentThread() - capOwn; capAll = GC.GetTotalAllocatedBytes(true) - capAll;
                Check(capOwn == 0 && capAll == 0, $"Changed cap/depth full frames allocated {capOwn}/{capAll} B");
                Console.WriteLine($"Contact cap/depth on {backend?.ToString() ?? "registered"}: 128 cycles/768 full frames {capOwn}/{capAll} B.");
                PhysicsServer.BodySetMaxContactsReported(scene.GetRID(), 3);
                // Equality uses observed depth because numerical backends need not publish identical geometry bits.
                circle.Radius = 100; PhysicsServer.BodySetTransform(right, new(0, new Vector2(175, 0)));
                PhysicsServer.BodySetTransform(left, new(0, new Vector2(0, 150))); PhysicsServer.BodySetTransform(third, new(0, new Vector2(-100, 0)));
                PhysicsServer.BodySetContactsReportedDepthThreshold(scene.GetRID(), 0); tree.PhysicsFrame(1d / 60);
                CheckPeers(3, right, left, third); var equalDepth = solver is not null ? 50f : 0f;
                if (solver is null)
                    for (var i = 0; i < Count(); i++)
                        if (state.GetContactCollider(i) == left)
                            equalDepth = (state.GetContactColliderPosition(i) - state.GetContactLocalPosition(i)).Dot(state.GetContactLocalNormal(i));
                Check(MathF.Abs(equalDepth - 50) < .001f, "Selected observed equality depth stays within the physical geometric bound");
                PhysicsServer.BodySetContactsReportedDepthThreshold(scene.GetRID(), equalDepth); tree.PhysicsFrame(1d / 60); CheckPeers(2, left, third);
                circle.Radius = 2; PhysicsServer.BodySetTransform(left, new(0, new Vector2(0, 3.5f)));
                PhysicsServer.BodySetContactsReportedDepthThreshold(scene.GetRID(), 0);
                PhysicsServer.BodySetTransform(right, new(0, new Vector2(3.5f, 0))); PhysicsServer.BodySetTransform(third, new(0, new Vector2(-3.5f, 0)));
                tree.PhysicsFrame(1d / 60); var first = state.GetContactCollider(0);
                PhysicsServer.BodySetMaxContactsReported(scene.GetRID(), 1); tree.PhysicsFrame(1d / 60); CheckPeers(1, first);
                PhysicsServer.BodySetMaxContactsReported(scene.GetRID(), 0); tree.PhysicsFrame(1d / 60); Check(Count() == 0, "Zero cap suppresses contact publication");
                PhysicsServer.BodySetMode(right, PhysicsServer.BodyMode.Kinematic); PhysicsServer.BodySetMaxContactsReported(right, 1);
                tree.PhysicsFrame(1d / 60); using var rawState = PhysicsServer.BodyGetDirectState(right)!;
                Check(rawState.GetContactCount() == 1 && rawState.GetContactCollider(0) == scene.GetRID() && rawState.GetContactImpulse(0).Length() < .001f,
                    "A raw kinematic receiver reports its scene peer even when that peer has zero cap");
                PhysicsServer.BodySetContactsReportedDepthThreshold(right, .51f); tree.PhysicsFrame(1d / 60);
                Check(rawState.GetContactCount() == 0, "Raw report-only depth policy filters its completed contact too");
                void CapCycle()
                {
                    PhysicsServer.BodySetMaxContactsReported(scene.GetRID(), 3); PhysicsServer.BodySetContactsReportedDepthThreshold(scene.GetRID(), 0);
                    tree.PhysicsFrame(1d / 60); CheckPeers(3, right, left, third);
                    PhysicsServer.BodySetMaxContactsReported(scene.GetRID(), 2); tree.PhysicsFrame(1d / 60); CheckPeers(2, left, third);
                    PhysicsServer.BodySetContactsReportedDepthThreshold(scene.GetRID(), .49f); tree.PhysicsFrame(1d / 60); CheckPeers(2, left, third);
                    PhysicsServer.BodySetMaxContactsReported(scene.GetRID(), 1); tree.PhysicsFrame(1d / 60); CheckPeers(1, third);
                    PhysicsServer.BodySetContactsReportedDepthThreshold(scene.GetRID(), 1.1f); tree.PhysicsFrame(1d / 60); CheckPeers(0);
                    PhysicsServer.BodySetMaxContactsReported(scene.GetRID(), 0); tree.PhysicsFrame(1d / 60); CheckPeers(0);
                }
            }
            void CheckPeers(int count, RID first = default, RID second = default, RID last = default)
            {
                Check(state.GetContactCount() == count, "The completed report respects cap and depth policy");
                for (var i = 0; i < count; i++)
                {
                    var peer = state.GetContactCollider(i);
                    Check((peer == first || peer == second || peer == last) && state.GetContactImpulse(i).Length() < .001f,
                        "The deepest eligible peer set is selected without creating a nonresponsive impulse");
                    for (var j = 0; j < i; j++) Check(peer != state.GetContactCollider(j), "Distinct peer observations do not duplicate a collider");
                }
            }
        }
        finally { PhysicsServer.FreeRID(third); PhysicsServer.FreeRID(left); PhysicsServer.FreeRID(right); }
    }
}
