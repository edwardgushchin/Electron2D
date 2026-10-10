using Electron2D;
using System.Diagnostics;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class PhysicsReportOnlyTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        using var shape = new CircleShape { Radius = 10 };
        foreach (var firstMode in new[] { Mode.Static, Mode.Kinematic })
            foreach (var secondMode in new[] { Mode.Static, Mode.Kinematic })
                foreach (var reverse in new[] { false, true })
                {
                    var baselinePairs = PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.CollisionPairs);
                    var space = PhysicsServer.SpaceCreate(backend); var a = PhysicsServer.BodyCreate(); var b = PhysicsServer.BodyCreate();
                    try
                    {
                        PhysicsServer.SpaceSetActive(space, true);
                        Setup(a, firstMode, default); Setup(b, secondMode, new(18, 0));
                        if (reverse) { PhysicsServer.BodySetSpace(b, space); PhysicsServer.BodySetSpace(a, space); }
                        else { PhysicsServer.BodySetSpace(a, space); PhysicsServer.BodySetSpace(b, space); }
                        foreach (var receiver in new[] { a, b })
                        {
                            PhysicsServer.BodySetMaxContactsReported(a, receiver == a ? 4 : 0);
                            PhysicsServer.BodySetMaxContactsReported(b, receiver == b ? 4 : 0);
                            PhysicsServer.SpaceStep(space, 1d / 60);
                            using var state = PhysicsServer.BodyGetDirectState(receiver)!;
                            var expected = firstMode != Mode.Static || secondMode != Mode.Static;
                            Check(expected ? state.GetContactCount() > 0 : state.GetContactCount() == 0,
                                $"{backend}: {firstMode}/{secondMode}, receiver={(receiver == a ? "A" : "B")}: report count {state.GetContactCount()}");
                            for (var i = 0; i < state.GetContactCount(); i++)
                            {
                                Check(state.GetContactCollider(i) == (receiver == a ? b : a) && state.GetContactLocalShape(i) == 0 && state.GetContactColliderShape(i) == 0, "Report-only contact retains collider and shape identity");
                                Check(state.GetContactImpulse(i) == Vector2.Zero && state.GetContactLocalNormal(i).IsFinite(), "A report-only pair has finite geometry and zero impulse");
                            }
                            Check(PhysicsServer.BodyGetTransform(a).Origin == Vector2.Zero && PhysicsServer.BodyGetTransform(b).Origin == new Vector2(18, 0), "Reports do not correct nonresponding poses");
                        }
                        PhysicsServer.BodySetMaxContactsReported(a, 1); PhysicsServer.BodySetMaxContactsReported(b, 1); PhysicsServer.SpaceStep(space, 1d / 60);
                        using var first = PhysicsServer.BodyGetDirectState(a)!; using var second = PhysicsServer.BodyGetDirectState(b)!;
                        var count = firstMode != Mode.Static || secondMode != Mode.Static ? 1 : 0;
                        Check(first.GetContactCount() == count && second.GetContactCount() == count, "Both receivers keep independent capped report-only snapshots");
                        Check(PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.CollisionPairs) == baselinePairs + count, "Report-only pair statistics count the shared pair once");
                    }
                    finally { PhysicsServer.FreeRID(a); PhysicsServer.FreeRID(b); PhysicsServer.FreeRID(space); }
                    void Setup(RID body, Mode mode, Vector2 position)
                    {
                        PhysicsServer.BodySetMode(body, mode); PhysicsServer.BodySetTransform(body, new(0, position));
                        PhysicsServer.BodyAddShape(body, shape.GetRID());
                    }
                }
        Lifecycle(backend); Geometry(backend); Sweep(backend); Scene(backend); DynamicSleep(backend, true); DynamicSleep(backend, false); Measure(backend);
        Console.WriteLine($"Report-only body role/receiver, lifecycle and scene checks passed on {backend}.");
    }
    private static void Geometry(PhysicsServer.Backend backend)
    {
        using var circle = new CircleShape { Radius = 10 }; using var ray = new SeparationRayShape { Length = 20 }; using var plane = new WorldBoundaryShape();
        foreach (var shape in new Shape[] { circle, ray })
        {
            var space = PhysicsServer.SpaceCreate(backend); var a = PhysicsServer.BodyCreate(); var b = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.SpaceSetActive(space, true); PhysicsServer.BodySetMode(a, Mode.Kinematic); PhysicsServer.BodySetMode(b, Mode.Static);
                PhysicsServer.BodyAddShape(a, shape.GetRID()); PhysicsServer.BodyAddShape(b, plane.GetRID());
                PhysicsServer.BodySetTransform(a, new(0, new(20000, -8))); PhysicsServer.BodySetSpace(a, space); PhysicsServer.BodySetSpace(b, space);
                foreach (var receiver in new[] { a, b })
                {
                    PhysicsServer.BodySetMaxContactsReported(a, receiver == a ? 4 : 0); PhysicsServer.BodySetMaxContactsReported(b, receiver == b ? 4 : 0);
                    PhysicsServer.SpaceStep(space, 1d / 60); using var state = PhysicsServer.BodyGetDirectState(receiver)!;
                    Check(state.GetContactCount() > 0, $"{backend}: report-only {shape.GetType().Name}/boundary contact far outside the boundary marker");
                    Check(state.GetContactCollider(0) == (receiver == a ? b : a) && state.GetContactImpulse(0) == Vector2.Zero, "Boundary and directed-ray reports preserve identity with no response");
                }
                Check(PhysicsServer.BodyGetTransform(a).Origin == new Vector2(20000, -8), "Report-only boundary/ray geometry never corrects the prescribed pose");
            }
            finally { PhysicsServer.FreeRID(a); PhysicsServer.FreeRID(b); PhysicsServer.FreeRID(space); }
        }
    }
    private static void Lifecycle(PhysicsServer.Backend backend)
    {
        using var shape = new CircleShape { Radius = 10 }; var space = PhysicsServer.SpaceCreate(backend);
        var a = PhysicsServer.BodyCreate(); var b = PhysicsServer.BodyCreate(); var joint = PhysicsServer.JointCreate();
        try
        {
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.BodySetMode(a, Mode.Kinematic); PhysicsServer.BodySetMode(b, Mode.Static);
            PhysicsServer.BodyAddShape(a, shape.GetRID()); PhysicsServer.BodyAddShape(b, shape.GetRID()); PhysicsServer.BodySetTransform(b, new(0, new(18, 0)));
            PhysicsServer.BodySetSpace(a, space); PhysicsServer.BodySetSpace(b, space); PhysicsServer.SpaceStep(space, 1d / 60);
            using var view = PhysicsServer.BodyGetDirectState(a)!;
            Check(view.GetContactCount() == 0, "Reporting starts disabled");
            PhysicsServer.BodySetMaxContactsReported(a, 1); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(view.GetContactCount() == 1, "Enabling reporting in an already prepared world discovers the nonresponding pair");
            PhysicsServer.BodySetCollisionMask(a, 0); PhysicsServer.BodySetCollisionMask(b, 0); PhysicsServer.SpaceStep(space, 1d / 60); Check(view.GetContactCount() == 0, "Rejecting both directions removes reports");
            PhysicsServer.BodySetCollisionMask(b, 1); PhysicsServer.SpaceStep(space, 1d / 60); Check(view.GetContactCount() == 1, "The other endpoint can admit a report-only pair");
            PhysicsServer.BodyAddCollisionException(b, a); PhysicsServer.SpaceStep(space, 1d / 60); Check(view.GetContactCount() == 0, "Reverse exception suppresses report-only contact");
            PhysicsServer.BodyRemoveCollisionException(b, a); PhysicsServer.SpaceStep(space, 1d / 60); Check(view.GetContactCount() == 1, "Removing the exception restores the report");
            PhysicsServer.JointMakePin(joint, new(9, 0), a, b); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(view.GetContactCount() == 0, "A joint veto suppresses report-only contacts");
            PhysicsServer.JointDisableCollisionsBetweenBodies(joint, false); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(view.GetContactCount() == 1, "Releasing the joint veto restores report-only contacts");
            PhysicsServer.JointClear(joint);
            PhysicsServer.BodySetMaxContactsReported(a, 0); PhysicsServer.SpaceStep(space, 1d / 60); Check(view.GetContactCount() == 0, "Disabling the only receiver clears reports");
            PhysicsServer.BodySetMaxContactsReported(a, 1); PhysicsServer.SpaceStep(space, 1d / 60); Check(view.GetContactCount() == 1, "Reenabled reporting remains usable");
            PhysicsServer.FreeRID(a); a = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(a, Mode.Kinematic); PhysicsServer.BodyAddShape(a, shape.GetRID());
            PhysicsServer.BodySetMaxContactsReported(a, 1); PhysicsServer.BodySetSpace(a, space); PhysicsServer.SpaceStep(space, 1d / 60);
            using var replacement = PhysicsServer.BodyGetDirectState(a)!; Check(replacement.GetContactCount() == 1 && replacement.GetContactCollider(0) == b, "Reused backend slots install current reporting policy and identity");
            PhysicsServer.BodySetMaxContactsReported(a, 0); PhysicsServer.BodySetMaxContactsReported(b, 1);
            for (var i = 0; i < 120; i++) PhysicsServer.SpaceStep(space, 1d / 60);
            using var staticView = PhysicsServer.BodyGetDirectState(b)!; Check(staticView.GetContactCount() == 1, "A static receiver retains its contact with an unreported kinematic peer");
            PhysicsServer.BodySetMaxContactsReported(a, 1); Check(!PhysicsServer.BodyGetSleeping(a), "Enabling server kinematic reports exposes active state before another step");
            PhysicsServer.BodySetMaxContactsReported(a, 0);
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(space);
            PhysicsServer.BodySetTransform(b, new(0, new(50, 0))); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(staticView.GetContactCount() == 0, "Static receiver departures remain current after the quiet interval");
            PhysicsServer.BodySetMaxContactsReported(b, 0); PhysicsServer.SpaceStep(space, 1d / 60);
            PhysicsServer.BodySetMaxContactsReported(b, 1);
            checkpoint.Restore(); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(PhysicsServer.BodyGetMaxContactsReported(b) == 1 && staticView.GetContactCount() == 1 && staticView.GetContactCollider(0) == a, "Checkpoint restores backend report eligibility and contact identity with the validated authored cap");
            var selected = PhysicsServer.Service.GetSceneSpace(space); selected.SetDebugContacts(8);
            PhysicsServer.BodySetTransform(b, new(0, new(20, 0))); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(staticView.GetContactCount() > 0 && selected.DebugContacts.Length == 0, "Touching report-only geometry retains a contact without penetrating debug markers");
        }
        finally { PhysicsServer.FreeRID(joint); PhysicsServer.FreeRID(a); PhysicsServer.FreeRID(b); PhysicsServer.FreeRID(space); }
    }
    private static void Sweep(PhysicsServer.Backend backend)
    {
        using var circle = new CircleShape { Radius = 10 }; var space = PhysicsServer.SpaceCreate(backend);
        var a = PhysicsServer.BodyCreate(); var b = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.BodySetMode(a, Mode.Kinematic); PhysicsServer.BodySetMode(b, Mode.Static);
            PhysicsServer.BodyAddShape(a, circle.GetRID()); PhysicsServer.BodyAddShape(b, circle.GetRID());
            PhysicsServer.BodySetTransform(a, new(0, new(-60, 0))); PhysicsServer.BodySetSpace(a, space); PhysicsServer.BodySetSpace(b, space);
            PhysicsServer.BodySetMaxContactsReported(a, 4); PhysicsServer.SpaceStep(space, 1d / 60);
            PhysicsServer.BodySetTransform(a, new(0, new(60, 0))); PhysicsServer.SpaceStep(space, 1d / 60);
            using var state = PhysicsServer.BodyGetDirectState(a)!;
            Check(state.GetContactCount() > 0 && state.GetContactCollider(0) == b && state.GetContactImpulse(0) == Vector2.Zero, $"{backend}: a completed sweep retains transient report-only contact");
            Check(PhysicsServer.BodyGetTransform(a).Origin.DistanceTo(new(60, 0)) < .001f, "Reporting never blocks the kinematic destination");
            PhysicsServer.SpaceStep(space, 1d / 60); Check(state.GetContactCount() == 0, "The next idle frame clears the transient contact");
        }
        finally { PhysicsServer.FreeRID(a); PhysicsServer.FreeRID(b); PhysicsServer.FreeRID(space); }
    }
    private static void Scene(PhysicsServer.Backend backend)
    {
        using var shape = new RectangleShape { Size = new(20, 20) }; using var world = new World(backend); using var root = new SubViewport { World = world };
        var body = new RigidBody { Name = "Body", Freeze = true, FreezeMode = RigidFreezeMode.Kinematic, ContactMonitor = true, MaxContactsReported = 4 };
        var wall = new StaticBody { Name = "Wall", Position = new(18, 0), ConstantLinearVelocity = new(3, 5), ConstantAngularVelocity = 2 };
        foreach (var collider in new PhysicsBody[] { body, wall }) { collider.AddChild(new CollisionShape { Shape = shape }); root.AddChild(collider); }
        using var tree = new SceneTree(root); var enters = 0; var exits = 0; var shapeEnters = 0; var shapeExits = 0; var syncs = 0;
        PhysicsServer.BodySetStateSyncCallback(body.GetRID(), state => { Check(state.GetContactCount() > 0 || wall.Position.X == 50, "Sync callbacks observe current report-only contacts"); syncs++; });
        body.BodyEntered += other => { Check(other == wall, "Report-only object owner"); enters++; }; body.BodyExited += _ => exits++;
        body.BodyShapeEntered += (rid, other, remote, local) => { Check(rid == wall.GetRID() && other == wall && remote == 0 && local == 0, "Report-only shape owner and indices"); shapeEnters++; };
        body.BodyShapeExited += (_, _, _, _) => shapeExits++;
        var space = PhysicsServer.Service.GetSceneSpace(world.Space); tree.DebugCollisionsHint = true;
        tree.PhysicsFrame(1d / 60); Check(body.GetContactCount() > 0 && enters == 1 && shapeEnters == 1, "Frozen kinematic scene body reports real contacts without impulse response");
        using var view = PhysicsServer.BodyGetDirectState(body.GetRID())!;
        var offset = view.GetContactColliderPosition(0) - wall.GlobalPosition;
        var expected = wall.ConstantLinearVelocity + new Vector2(-offset.Y, offset.X) * wall.ConstantAngularVelocity;
        Check(view.GetContactColliderVelocityAtPosition(0).DistanceTo(expected) < .001f, "Report-only point velocity includes virtual linear and angular surface motion");
        Check(space.DebugContacts.Length > 0 && space.DebugContacts.Length <= tree.DebugContactLimit, "Report-only geometry contributes bounded debug markers");
        for (var i = 0; i < 120; i++) tree.PhysicsFrame(1d / 60);
        Check(body.Position == Vector2.Zero && body.LinearVelocity == Vector2.Zero && enters == 1 && exits == 0 && syncs == 121, "Stationary reported kinematic contacts remain active without duplicate events or motion");
        wall.Position = new(50, 0); tree.PhysicsFrame(1d / 60); Check(body.GetContactCount() == 0 && exits == 1 && shapeExits == 1, "Separation publishes one report-only departure");
        body.MaxContactsReported = 0; tree.PhysicsFrame(1d / 60); body.MaxContactsReported = 4;
        Check(!PhysicsServer.BodyGetSleeping(body.GetRID()), "The scene cap setter immediately exposes a reported frozen kinematic body as active");
    }
    private static void DynamicSleep(PhysicsServer.Backend backend, bool sync)
    {
        using var floorShape = new RectangleShape { Size = new(200, 20) }; using var circle = new CircleShape { Radius = 10 };
        using var world = new World(backend); using var root = new SubViewport { World = world };
        var floor = new AnimatableBody { Name = "Floor", Position = new(0, 100), SyncToPhysics = sync }; floor.AddChild(new CollisionShape { Shape = floorShape }); root.AddChild(floor);
        var body = new RigidBody { Name = "Body", MaxContactsReported = 4 }; body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
        using var tree = new SceneTree(root); PhysicsServer.BodySetMaxContactsReported(floor.GetRID(), 4);
        for (var i = 0; i < 180; i++) tree.PhysicsFrame(1d / 60);
        Check(body.GetContactCount() > 0 && body.Sleeping, $"{backend}: kinematic reporting does not prevent dynamic sleep: contacts={body.GetContactCount()}, position={body.Position}, velocity={body.LinearVelocity}, sleeping={body.Sleeping}");
        floor.Position += new Vector2(.5f, 0); tree.PhysicsFrame(1d / 60);
        Check(!body.Sleeping, "Actual platform movement still wakes the supported dynamic body");
    }
    private static void Measure(PhysicsServer.Backend backend)
    {
        using var shape = new RectangleShape { Size = new(20, 20) }; var space = PhysicsServer.SpaceCreate(backend);
        var a = PhysicsServer.BodyCreate(); var b = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.BodySetMode(a, Mode.Kinematic); PhysicsServer.BodySetMode(b, Mode.Static);
            PhysicsServer.BodyAddShape(a, shape.GetRID()); PhysicsServer.BodyAddShape(b, shape.GetRID());
            PhysicsServer.BodySetSpace(a, space); PhysicsServer.BodySetSpace(b, space); PhysicsServer.BodySetMaxContactsReported(a, 4);
            PhysicsServer.SpaceStep(space, 1d / 60); using var view = PhysicsServer.BodyGetDirectState(a)!;
            var elapsed = new long[128]; var store = PhysicsServer.Service.GetSceneSpace(space).GPUStore;
            foreach (var changing in new[] { false, true })
            {
                Collect(); for (var i = 0; i < 128; i++) Frame(i, changing);
                var upload = store?.UploadBytes ?? 0; var readback = store?.ReadbackBytes ?? 0; var wait = store?.WaitMS ?? 0;
                var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < elapsed.Length; i++) { var start = Stopwatch.GetTimestamp(); Frame(i, changing); elapsed[i] = Stopwatch.GetTimestamp() - start; }
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(owner == 0 && all == 0, $"{backend}: warmed report-only frames allocated {owner}/{all} owner/all bytes");
                Array.Sort(elapsed); var ms = 1000d / Stopwatch.Frequency;
                Console.WriteLine($"Report-only {backend}, changing={changing}: 2 bodies, dt=1/60, 4 substeps/16 iterations, 128 warmup/128 samples; p50/p95/p99={elapsed[64] * ms:F4}/{elapsed[121] * ms:F4}/{elapsed[126] * ms:F4} ms, allocations={owner}/{all}, upload/readback={(store?.UploadBytes - upload) / 128}/{(store?.ReadbackBytes - readback) / 128} B, mean wait={(store?.WaitMS - wait) / 128:F4} ms.");
            }
            void Frame(int i, bool changing)
            {
                var overlap = !changing || (i & 1) == 0;
                PhysicsServer.BodySetTransform(b, new(0, new(overlap ? 18 : 50, 0))); PhysicsServer.SpaceStep(space, 1d / 60);
                Check(overlap ? view.GetContactCount() > 0 : view.GetContactCount() == 0, "Measured frame publishes current report-only membership");
                for (var j = 0; j < view.GetContactCount(); j++) Check(view.GetContactImpulse(j) == Vector2.Zero, "Measured report-only impulse is zero");
            }
        }
        finally { PhysicsServer.FreeRID(a); PhysicsServer.FreeRID(b); PhysicsServer.FreeRID(space); }
    }
    private static void Collect() { GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
