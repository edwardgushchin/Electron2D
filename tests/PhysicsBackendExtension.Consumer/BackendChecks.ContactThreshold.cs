using Electron2D;

public static partial class BackendChecks
{
    public static void RunContactThreshold(PhysicsServer.Backend? backend = null)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ContactThreshold", () => solver = new());
        using var world = backend is { } selected ? new World(selected) : new World("ContactThreshold");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 };
        var scene = new RigidBody { GravityScale = 0, CanSleep = false, MaxContactsReported = 4, ContactMonitor = true };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        var obstacle = new StaticBody { Position = new(4, 0) };
        obstacle.AddChild(new CollisionShape { Shape = circle }); root.AddChild(obstacle);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate(); var remote = PhysicsServer.BodyCreate(); var sceneID = scene.GetRID();
        var enters = 0; var exits = 0; scene.BodyEntered += _ => enters++; scene.BodyExited += _ => exits++;
        try
        {
            foreach (var sibling in new PhysicsBody[] { new StaticBody(), new AnimatableBody(), new CharacterBody(), new PhysicalBone(), new RigidBody() })
            {
                sibling.Name = "Threshold" + sibling.GetType().Name;
                var id = sibling.GetRID();
                try
                {
                    Check(PhysicsServer.BodyGetContactsReportedDepthThreshold(id) == 0, "Every scene body role shares the reporting default");
                    PhysicsServer.BodySetContactsReportedDepthThreshold(id, 3.5f); root.AddChild(sibling);
                    PhysicsServer.BodySetSpace(id, default); PhysicsServer.BodySetSpace(id, world.Space);
                    Check(PhysicsServer.BodyGetContactsReportedDepthThreshold(id) == 3.5f, "Every scene body role retains authoring through membership reentry");
                }
                finally { sibling.Dispose(); }
                Reject<ArgumentException>(() => PhysicsServer.BodyGetContactsReportedDepthThreshold(id));
            }
            foreach (var id in new[] { raw, remote }) PhysicsServer.BodyAddShape(id, circle.GetRID());
            PhysicsServer.BodySetMode(remote, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodySetTransform(remote, new(0, new Vector2(4, 30)));
            PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
            PhysicsServer.BodySetMaxContactsReported(raw, 4); PhysicsServer.BodySetMaxContactsReported(remote, 4);
            foreach (var id in new[] { sceneID, raw, remote })
            {
                Check(PhysicsServer.BodyGetContactsReportedDepthThreshold(id) == 0, "Scene/raw reporting threshold defaults to zero");
                PhysicsServer.BodySetContactsReportedDepthThreshold(id, 100);
                foreach (var invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
                    Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetContactsReportedDepthThreshold(id, invalid));
                Check(PhysicsServer.BodyGetContactsReportedDepthThreshold(id) == 100, "Invalid threshold preserves retained authoring");
            }
            if (solver is not null) solver.IdentityHook = _ =>
            {
                foreach (var id in new[] { sceneID, raw, remote })
                {
                    if (PhysicsServer.BodyGetSpace(id) != world.Space) continue;
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetContactsReportedDepthThreshold(id, 0));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetContactsReportedDepthThreshold(id, 100));
                    Check(PhysicsServer.BodyGetContactsReportedDepthThreshold(id) == 100, "Numerical hook reads authored threshold without mutation");
                }
            };
            PhysicsServer.BodySetSpace(raw, world.Space); PhysicsServer.BodySetSpace(remote, world.Space);
            if (solver is not null) solver.IdentityHook = null;
            Reset(); tree.PhysicsFrame(1d / 60);
            var sceneView = PhysicsServer.BodyGetDirectState(sceneID)!; var rawView = PhysicsServer.BodyGetDirectState(raw)!;
            var staticView = PhysicsServer.BodyGetDirectState(remote)!;
            Check(sceneView.GetContactCount() == 0 && rawView.GetContactCount() == 0 && staticView.GetContactCount() == 0 && enters == 0,
                "High reporting threshold suppresses both scene and raw snapshots");
            var scenePose = scene.Position; var rawPose = PhysicsServer.BodyGetTransform(raw).Origin;
            SetThreshold(.5f); Reset(); tree.PhysicsFrame(1d / 60);
            Check(sceneView.GetContactCount() > 0 && rawView.GetContactCount() > 0 && staticView.GetContactCount() > 0 && enters == 1,
                "Small positive scene-unit threshold publishes physical penetration and scene monitor entry");
            Check(scene.Position.DistanceTo(scenePose) < .00001f && PhysicsServer.BodyGetTransform(raw).Origin.DistanceTo(rawPose) < .00001f,
                "Reporting policy leaves physical response unchanged on the same backend");
            var sample = new PhysicsBodyContact(sceneID, 0, obstacle.GetRID(), 0, new(3, 0), new(2, 0), Vector2.Left,
                Vector2.Zero, Vector2.Zero, Vector2.Zero);
            Check(sample.Depth == 1, "Boundary-anchor constructor derives signed scene-unit penetration");
            sample = new(sceneID, 0, obstacle.GetRID(), 0, Vector2.Zero, Vector2.Zero, Vector2.Left,
                Vector2.Zero, Vector2.Zero, Vector2.Zero, 2);
            Check(sample.Depth == 2, "Explicit sampled depth survives immutable result construction");
            Reject<ArgumentOutOfRangeException>(() => _ = new PhysicsBodyContact(sceneID, 0, obstacle.GetRID(), 0,
                Vector2.Zero, Vector2.Zero, Vector2.Left, Vector2.Zero, Vector2.Zero, Vector2.Zero, float.NaN));
            if (solver is not null)
            {
                SetThreshold(2); Reset(); tree.PhysicsFrame(1d / 60);
                Check(sceneView.GetContactCount() == 1 && rawView.GetContactCount() == 1, "Exact sampled depth equals the threshold and remains eligible");
            }
            SetThreshold(100);
            Check(sceneView.GetContactCount() > 0 && rawView.GetContactCount() > 0, "Policy edits preserve completed snapshots until the next publication");
            Reset(); tree.PhysicsFrame(1d / 60);
            Check(sceneView.GetContactCount() == 0 && rawView.GetContactCount() == 0 && exits == 1, "Filtering commits one scene exit");
            PhysicsServer.BodySetStateSyncCallback(raw, _ =>
            {
                PhysicsServer.BodySetContactsReportedDepthThreshold(raw, 0);
                throw new ApplicationException("Threshold sync observer failure");
            });
            var tick = PhysicsServer.SpaceGetTick(world.Space);
            Reset(); Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
            PhysicsServer.BodySetStateSyncCallback(raw, null);
            Check(PhysicsServer.BodyGetContactsReportedDepthThreshold(raw) == 0 && rawView.GetContactCount() == 0 &&
                PhysicsServer.SpaceGetTick(world.Space) == tick + 1,
                "Failed ordinary callback retains the policy edit and completed pre-edit snapshot without replay");
            Reset(); tree.PhysicsFrame(1d / 60);
            Check(rawView.GetContactCount() > 0, "The next healthy publication applies the callback edit");
            SetThreshold(100); Reset(); tree.PhysicsFrame(1d / 60);
            PhysicsServer.BodySetSpace(raw, default); PhysicsServer.BodySetSpace(raw, world.Space);
            Check(PhysicsServer.BodyGetContactsReportedDepthThreshold(raw) == 100, "Detach/reentry retains reporting policy");
            rawView = PhysicsServer.BodyGetDirectState(raw)!;
            using (var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space))
            {
                SetThreshold(0); Reject<InvalidOperationException>(() => checkpoint.Restore()); SetThreshold(100); checkpoint.Restore();
            }
            using var map = new PhysicsSnapshotMap(world.Space);
            map.Bind(1, 1, sceneID); map.Bind(2, 1, obstacle.GetRID()); map.Bind(3, 1, raw); map.Bind(4, 1, remote);
            var snapshot = new PhysicsSnapshot(65536); map.Capture(snapshot);
            SetThreshold(0); Reject<InvalidDataException>(() => map.Apply(snapshot)); SetThreshold(100); map.Apply(snapshot);
            for (var i = 0; i < 64; i++) { SetThreshold(i % 2 == 0 ? 0 : 100); Reset(); tree.PhysicsFrame(1d / 60); }
            Collect(); GC.WaitForPendingFinalizers(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            var entryStart = enters; var exitStart = exits;
            for (var i = 0; i < 64; i++) { SetThreshold(i % 2 == 0 ? 0 : 100); Reset(); tree.PhysicsFrame(1d / 60); }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(enters - entryStart == 32 && exits - exitStart == 32, "Edited warm frames deliver filtered physical monitor transitions");
            Check(owner == 0 && all == 0, $"Contact threshold complete frames allocate owner={owner}/all={all}");
            Check(Task.Run(() =>
            {
                Reject<InvalidOperationException>(() => PhysicsServer.BodyGetContactsReportedDepthThreshold(raw));
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetContactsReportedDepthThreshold(raw, 0));
            }).Wait(TimeSpan.FromSeconds(2)), "Attached threshold access rejects off-owner threads");
            foreach (var invalid in new[] { default, world.Space, circle.GetRID() })
            {
                Reject<ArgumentException>(() => PhysicsServer.BodyGetContactsReportedDepthThreshold(invalid));
                Reject<ArgumentException>(() => PhysicsServer.BodySetContactsReportedDepthThreshold(invalid, 0));
            }
            Console.WriteLine($"Contact reporting threshold on {backend?.ToString() ?? "registered"}: scene/raw response, history, 32 entries/exits and 64 warm edited frames {owner}/{all} B passed.");
        }
        finally { if (solver is not null) solver.IdentityHook = null; PhysicsServer.BodySetStateSyncCallback(raw, null); PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(remote); }
        Reject<ArgumentException>(() => PhysicsServer.BodyGetContactsReportedDepthThreshold(raw));
        Reject<ArgumentException>(() => PhysicsServer.BodySetContactsReportedDepthThreshold(raw, 0));
        void SetThreshold(float value)
        { PhysicsServer.BodySetContactsReportedDepthThreshold(sceneID, value); PhysicsServer.BodySetContactsReportedDepthThreshold(raw, value); PhysicsServer.BodySetContactsReportedDepthThreshold(remote, value); }
        void Reset()
        {
            scene.Position = Vector2.Zero; scene.LinearVelocity = Vector2.Zero; scene.AngularVelocity = 0;
            PhysicsServer.BodySetTransform(raw, new(0, new Vector2(0, 30))); PhysicsServer.BodySetLinearVelocity(raw, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(raw, 0);
        }
    }
}
