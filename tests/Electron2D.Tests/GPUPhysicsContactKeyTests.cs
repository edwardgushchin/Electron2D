using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;
using static PhysicsDebugTests;

internal static class GPUPhysicsContactKeyTests
{
    internal static void Run()
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, Environment.GetEnvironmentVariable("ELECTRON2D_TEST_GPU_PUBLICATION_THREAD") == "1");
        try { Native(); MixedNative(); EagerStorage(); SnapshotStorage(false); SnapshotStorage(true); Scene(); MixedScene(); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Console.WriteLine("Compact contact keys: frozen fields, identity, demand, replay and zero-allocation publication passed.");
    }
    private static void Native()
    {
        Check(System.Runtime.CompilerServices.Unsafe.SizeOf<Store.ContactIdentity>() == 16, "Compact contact identity has a 16-byte stride");
        using var store = new Store { CaptureContactReports = true };
        using var shape = new CircleShape { Radius = 10 };
        var floor = store.Add(new(Mode.Static, new(0, 18), 0, default, 0));
        var body = store.Add(new(Mode.RigidLinear, default, 0, new(20, 30), 0, CanSleep: false));
        store.AddShape(floor, shape); store.AddShape(body, shape);
        store.Simulate(.01f, Vector2.Zero);
        Span<Store.ContactReport> full = stackalloc Store.ContactReport[4], saved = stackalloc Store.ContactReport[4];
        Span<Store.ContactIdentity> keys = stackalloc Store.ContactIdentity[4];
        Span<int> counts = stackalloc int[1], offsets = stackalloc int[1];
        store.ReadContactReports([body], [4], counts, full);
        var count = counts[0]; Check(count > 0, "Native keys fixture solves real contacts");
        var before = store.ReportResultReadbackBytes;
        store.ReadContactReports(new([body], [4], counts, keys, offsets));
        var version = store.ContactResultVersion;
        var compactBytes = store.ReportResultReadbackBytes - before;
        store.ReadFrozenContactReports(version, offsets[0], saved[..count]);
        for (var i = 0; i < count; i++)
            Check(saved[i].Equals(full[i]) && keys[i].Shape == full[i].Shape && keys[i].ColliderShapeGeneration == full[i].ColliderShapeGeneration,
                "Compact selection freezes every full field and retains both shape generations");
        Check(compactBytes > 0 && compactBytes % 16 == 0, "Selected compact payload uses sixteen bytes per bounded slot");
        store.SetPose(body, new(200, 100), 1); store.SetSolverVelocity(body, new(-90, 80), 3);
        store.Step(0, Vector2.Zero); store.Remove(floor);
        store.ReadFrozenContactReports(version, offsets[0], saved[..count]);
        for (var i = 0; i < count; i++) Check(saved[i].Equals(full[i]), "Frozen numerical fields survive later motion and collider removal");
        store.ReadContactReports(new([body], [4], counts, keys, offsets));
        store.ReadContactReports(new([body], [4], counts, keys, offsets));
        var rejected = false;
        try { store.ReadFrozenContactReports(version, 0, saved[..1]); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected && saved[0].Equals(full[0]), "Replaced selection rejects before writing its destination");
        var invalid = false;
        try { store.ReadContactReports(new([body], [4], counts, keys, counts)); } catch (ArgumentException) { invalid = true; }
        Check(invalid, "Compact offset/count aliasing rejects before publication");
    }
    private static void MixedNative()
    {
        using var store = new Store { CaptureContactReports = true };
        using var shape = new CircleShape { Radius = 10 };
        var floor = store.Add(new(Mode.Static, new(0, 18), 0, default, 0));
        var secondFloor = store.Add(new(Mode.Static, new(30, 18), 0, default, 0));
        var a = store.Add(new(Mode.RigidLinear, default, 0, new(20, 30), 0, CanSleep: false));
        var b = store.Add(new(Mode.RigidLinear, new(30, 0), 0, new(-20, 30), 0, CanSleep: false));
        foreach (var body in new[] { floor, secondFloor, a, b }) store.AddShape(body, shape);
        store.Simulate(.01f, Vector2.Zero);
        Store.BodyHandle[] receivers = [a, b, a, b]; int[] limits = [4, 4, 4, 4], counts = new int[4], offsets = new int[4];
        byte[] formats = [1, 0, 1, 1];
        Span<Store.ContactReport> reference = stackalloc Store.ContactReport[16], full = stackalloc Store.ContactReport[4];
        Span<Store.ContactIdentity> keys = stackalloc Store.ContactIdentity[12];
        store.ReadContactReports(receivers, limits, counts, reference);
        var expectedCounts = counts.ToArray(); var submissions = store.SubmissionCount; var bytes = store.ReportResultReadbackBytes;
        store.ReadContactReports(new(receivers, limits, counts, full, keys, offsets, formats));
        Check(store.SubmissionCount == submissions + 1, "Mixed duplicate receiver formats share one selected-result completion");
        Check(counts.SequenceEqual(expectedCounts) && counts.All(value => value > 0), "Mixed duplicate receivers preserve complete nonempty counts");
        var fullOffset = 0; var keyOffset = 0;
        for (var i = 0; i < receivers.Length; i++)
        {
            for (var j = 0; j < counts[i]; j++)
            {
                var expected = reference[i * 4 + j];
                if (formats[i] == 0) Check(full[fullOffset + j].Equals(expected), "Mixed numerical receivers retain every frozen report field");
                else
                {
                    var key = keys[keyOffset + j];
                    Check(key.Shape == expected.Shape && key.ShapeGeneration == expected.ShapeGeneration && key.ColliderShape == expected.ColliderShape &&
                        key.ColliderShapeGeneration == expected.ColliderShapeGeneration, "Mixed compact receivers retain both observed shape generations");
                }
            }
            if (formats[i] == 0) fullOffset += limits[i]; else keyOffset += limits[i];
        }
        Check(store.ReportResultReadbackBytes - bytes < 8 * 68, "Sparse numerical demand reduces the bounded mixed result payload");
        int[] limitCounts = [4, 4, 4, 4];
        store.ReadContactReports(new(receivers, limitCounts, limitCounts, full, keys, offsets, formats));
        Check(limitCounts.SequenceEqual(expectedCounts), "Mixed publication commits aliased limits/counts only after every result segment");
        var version = store.ContactResultVersion; var before = counts.ToArray(); var rejected = false;
        try { store.ReadContactReports(new(receivers, limits, counts, full, keys, offsets, System.Runtime.InteropServices.MemoryMarshal.AsBytes(counts.AsSpan())[..4])); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected && counts.SequenceEqual(before) && store.ContactResultVersion == version, "Aliased mixed formats reject before selection or output writes");
        rejected = false;
        try { store.ReadContactReports(new(receivers, limits, counts, full[..3], keys, offsets, formats)); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected && counts.SequenceEqual(before) && store.ContactResultVersion == version, "An undersized mixed numerical destination rejects before selection");
        rejected = false; formats[0] = 2;
        try { store.ReadContactReports(new(receivers, limits, counts, full, keys, offsets, formats)); }
        catch (ArgumentOutOfRangeException) { rejected = true; }
        formats[0] = 1;
        Check(rejected && counts.SequenceEqual(before) && store.ContactResultVersion == version, "An undefined receiver format rejects before selection");
        rejected = false;
        try { store.ReadContactReports(new(receivers, limits, counts, full, System.Runtime.InteropServices.MemoryMarshal.Cast<Store.ContactReport, Store.ContactIdentity>(full)[..12], offsets, formats)); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected && counts.SequenceEqual(before) && store.ContactResultVersion == version, "Overlapping mixed result destinations reject before output writes");
        rejected = false;
        Span<Store.ContactReport> aliasData = stackalloc Store.ContactReport[16];
        var aliasLimits = System.Runtime.InteropServices.MemoryMarshal.Cast<Store.ContactReport, int>(aliasData)[..4]; aliasLimits.Fill(4);
        try { store.ReadContactReports(new(receivers, aliasLimits, counts, aliasData[..4], keys, offsets, formats)); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected && counts.SequenceEqual(before) && store.ContactResultVersion == version, "Result writes cannot replace limits still needed by later receiver segments");
        store.SetPose(a, new(200, 100), 1); store.SetSolverVelocity(a, new(-90, 80), 3); store.Step(0, Vector2.Zero); store.Remove(floor);
        Span<Store.ContactReport> late = stackalloc Store.ContactReport[1];
        store.ReadFrozenContactReports(version, offsets[0], late);
        Check(late[0].Equals(reference[0]), "A compact row in a mixed packet retains its numerical fields through authoring and collider removal");
    }
    private static void EagerStorage()
    {
        var space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU);
        var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetMaxContactsReported(body, 4);
            PhysicsServer.BodySetSpace(body, space);
            PhysicsServer.SpaceSetActive(space, true);
            PhysicsServer.SpaceStep(space, 1d / 60);
            var bytes = PhysicsServer.Service.GetSceneSpace(space).GPUContactStagingBytes;
            Check(bytes.Reports > 0 && bytes.Keys == 0, "An eager raw receiver reserves numerical staging without an unused key mirror");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
    }
    private static void SnapshotStorage(bool portable)
    {
        using var world = new World(PhysicsServer.Backend.GPU);
        using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 10 };
        var floor = new StaticBody { Position = new(0, 18) };
        floor.AddChild(new CollisionShape { Shape = shape }); root.AddChild(floor);
        var quiet = new RigidBody { Freeze = true, ContactMonitor = true, MaxContactsReported = 128 };
        root.AddChild(quiet);
        var body = new RigidBody { ContactMonitor = true, MaxContactsReported = 4, CanSleep = false };
        body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
        using var tree = new SceneTree(root);
        PhysicsServer.AreaSetGravity(world.Space, 0); tree.PhysicsFrame(.01);
        var space = PhysicsServer.Service.GetSceneSpace(world.Space);
        Check(space.GPUContactStagingBytes.Reports == 0, "A fresh compact snapshot source has no numerical staging mirror");
        Span<Store.ContactReport> expected = stackalloc Store.ContactReport[1];
        space.GPUStore!.ReadFrozenContactReports(space.GPUStore.ContactResultVersion, 0, expected);
        if (portable)
        {
            using var map = new PhysicsSnapshotMap(world.Space);
            map.Bind(1, 1, floor.GetRID()); map.Bind(2, 1, quiet.GetRID()); map.Bind(3, 1, body.GetRID());
            var packet = new PhysicsSnapshot(1 << 20); map.Capture(packet); map.Apply(packet);
        }
        else
        {
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space); checkpoint.Restore();
        }
        var view = PhysicsServer.BodyGetDirectState(body.GetRID())!;
        Check(space.GPUContactStagingBytes.Reports > 0 && view.GetContactCount() > 0 && view.GetContactCollider(0) == floor.GetRID() &&
            view.GetContactLocalPosition(0) == expected[0].Position && view.GetContactColliderPosition(0) == expected[0].ColliderPosition &&
            view.GetContactLocalNormal(0) == expected[0].Normal && view.GetContactImpulse(0) == expected[0].Impulse &&
            view.GetContactLocalVelocityAtPosition(0) == expected[0].Velocity && view.GetContactColliderVelocityAtPosition(0) == expected[0].ColliderVelocity,
            "First snapshot demand materializes and restores complete contacts after an empty receiver's reserved segment");
    }
    private static void Scene()
    {
        using var world = new World(PhysicsServer.Backend.GPU);
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 10 };
        using var rectangle = new RectangleShape { Size = new(1000, 20) };
        var floor = new StaticBody { Position = new(0, 20) };
        var floorShape = new CollisionShape { Shape = rectangle }; floor.AddChild(floorShape); root.AddChild(floor);
        var body = new RigidBody { ContactMonitor = true, MaxContactsReported = 4, CanSleep = false, LockRotation = true };
        body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
        using var tree = new SceneTree(root);
        PhysicsServer.AreaSetGravity(world.Space, 60); PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
        var space = PhysicsServer.Service.GetSceneSpace(world.Space); var store = space.GPUStore!;
        Frame();
        var staging = space.GPUContactStagingBytes;
        Check(staging.Keys > 0 && staging.Reports == 0, "A monitor-only receiver reserves keys without a numerical staging mirror");
        Frame(); var view = PhysicsServer.BodyGetDirectState(body.GetRID())!;
        Check(view.GetContactCount() > 0 && view.GetContactCollider(0) == floor.GetRID(), "The monitor and identity getters observe real contact keys");
        var reads = store.ReportReadCount; _ = view.GetContactColliderID(0); _ = view.GetContactColliderShape(0);
        Check(store.ReportReadCount == reads, "Identity getters leave numerical results resident");
        Span<Store.ContactReport> expected = stackalloc Store.ContactReport[1];
        store.ReadFrozenContactReports(store.ContactResultVersion, 0, expected);
        var old = expected[0]; var floorRID = floor.GetRID(); var floorID = floor.InstanceID;
        body.Position = new(300, -200); body.LinearVelocity = new(90, -80);
        floor.RemoveChild(floorShape); floorShape.Dispose();
        Check(view.GetContactLocalPosition(0) == old.Position && view.GetContactColliderPosition(0) == old.ColliderPosition &&
            view.GetContactLocalNormal(0) == old.Normal && view.GetContactImpulse(0) == old.Impulse &&
            view.GetContactLocalVelocityAtPosition(0) == old.Velocity && view.GetContactColliderVelocityAtPosition(0) == old.ColliderVelocity &&
            view.GetContactCollider(0) == floorRID && view.GetContactColliderID(0) == floorID,
            "Late getters retain the selected fields, logical shapes and identity through pose/velocity/shape edits");
        Check(space.GPUContactStagingBytes.Reports == 0, "A late single-row getter does not reserve a whole numerical staging mirror");
        floor.AddChild(new CollisionShape { Shape = rectangle });
        // Recreate a stationary contact before comparing the same complete frame path.
        for (var i = 0; i < 128; i++) Frame();
        var fullBytes = Traffic(true); var keyBytes = Traffic(false);
        Check(fullBytes * 16 == keyBytes * 68 && keyBytes > 0, "Identical scene frames transfer 16 instead of 68 bytes per selected contact slot");
        // Synchronous solver-pose notifications retain the existing no-query step guard.
        Frame();
        store.ReadFrozenContactReports(store.ContactResultVersion, 0, expected); old = expected[0];
        body.LinearVelocity = new(40, -50);
        var observed = 0; body.NotifyLocalTransformChanges = true;
        Action<CanvasItem> notification = _ =>
        {
            observed++;
            var rejected = false;
            try { view.GetContactLocalPosition(0); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Synchronous solver-pose notifications cannot query a space while it is stepping");
        };
        body.LocalTransformChanged += notification;
        tree.PhysicsFrame(1d / 60);
        body.LocalTransformChanged -= notification;
        Check(observed > 0, "The contact lifetime regression executes a real synchronous pose notification");
        Frame();
        using var point = PhysicsServer.SpaceCreateCheckpoint(world.Space);
        var snapshot = view.CapturedContacts.ToArray();
        Frame(); point.Restore();
        Check(view.CapturedContacts.SequenceEqual(snapshot), "Local replay restores full immutable values after compact publication");
        using var map = new PhysicsSnapshotMap(world.Space); map.Bind(1, 1, body.GetRID()); map.Bind(2, 1, floor.GetRID());
        var packet = new PhysicsSnapshot(65536); Frame(); map.Capture(packet); snapshot = view.CapturedContacts.ToArray();
        Frame(); map.Apply(packet);
        Check(view.CapturedContacts.SequenceEqual(snapshot), "Portable replay restores full immutable values after compact publication");
        PhysicsServer.BodySetContactsReportedDepthThreshold(body.GetRID(), .001f);
        Check(Traffic(false) == fullBytes, "Positive contact depth thresholds retain the complete packet");
        PhysicsServer.BodySetContactsReportedDepthThreshold(body.GetRID(), 0);
        PhysicsServer.BodySetStateSyncCallback(body.GetRID(), state => { if (state.GetContactCount() > 0) _ = state.GetContactImpulse(0); });
        Check(Traffic(false) == fullBytes, "Numerical contact callbacks retain the complete packet");
        PhysicsServer.BodySetStateSyncCallback(body.GetRID(), null);
        for (var i = 0; i < 128; i++) Frame();
        AllocationWarmupCheck.Collect();
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame();
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, $"128 warmed compact full frames allocate {owner}/{all} owner/all-thread bytes");
        Console.WriteLine($"Contact keys: full/key selected bytes over 8 frames={fullBytes}/{keyBytes}; warmed frames owner/all={owner}/{all} B.");
        void Frame() { body.Position = Vector2.Zero; body.LinearVelocity = new(5, 20); tree.PhysicsFrame(1d / 60); }
        long Traffic(bool full)
        {
            space.ForceFullGPUContactReports = full;
            Frame(); Frame();
            var bytes = store.ReportResultReadbackBytes;
            for (var i = 0; i < 8; i++) Frame();
            return store.ReportResultReadbackBytes - bytes;
        }
    }

    private static void MixedScene()
    {
        const int count = 64;
        using var world = new World(PhysicsServer.Backend.GPU);
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 10 };
        using var rectangle = new RectangleShape { Size = new(4000, 20) };
        var floor = new StaticBody { Position = new(0, 20) }; floor.AddChild(new CollisionShape { Shape = rectangle }); root.AddChild(floor);
        var bodies = new RigidBody[count];
        for (var i = 0; i < count; i++)
        {
            var body = new RigidBody { Position = new(i * 30, 0), ContactMonitor = true, MaxContactsReported = 4, CanSleep = false, LockRotation = true };
            body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body); bodies[i] = body;
        }
        var quiet = new RigidBody { Freeze = true, ContactMonitor = true, MaxContactsReported = 128 }; root.AddChild(quiet);
        using var tree = new SceneTree(root);
        PhysicsServer.AreaSetGravity(world.Space, 60);
        var space = PhysicsServer.Service.GetSceneSpace(world.Space); var store = space.GPUStore!; var observed = 0;
        PhysicsServer.BodySetStateSyncCallback(bodies[count / 2].GetRID(), state =>
        {
            Check(state.GetContactCount() > 0 && state.GetContactCollider(0) == floor.GetRID() && state.GetContactLocalNormal(0).LengthSquared() > .99f,
                "The sparse numerical receiver observes its actual floor and unit normal");
            _ = state.GetContactImpulse(0); observed++;
        });
        for (var i = 0; i < 128; i++) Frame();
        Check(space.GPUContactStagingBytes.Reports <= 8 * 68 && space.GPUContactStagingBytes.Keys > 0,
            "One numerical receiver does not reserve numerical staging for unrelated monitors");
        var compact = Measure(false); var full = Measure(true);
        Check(compact < full / 2 && observed > 0, "One numerical callback keeps unrelated receiver results compact");
        space.ForceFullGPUContactReports = false; Frame(); Frame();
        var view = PhysicsServer.BodyGetDirectState(bodies[1].GetRID())!; _ = view.GetContactImpulse(0);
        var promotedBytes = store.ReportResultReadbackBytes; var promotedReads = store.ReportReadCount;
        Frame();
        promotedBytes = store.ReportResultReadbackBytes - promotedBytes;
        Check(promotedBytes > compact / 16 && promotedBytes < full / 16 && store.ReportReadCount == promotedReads + 1,
            "A late numerical getter promotes only its receiver in the next single-read publication");
        using (var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space)) checkpoint.Restore();
        using var map = new PhysicsSnapshotMap(world.Space);
        for (var i = 0; i < count; i++) map.Bind((ulong)i + 1, 1, bodies[i].GetRID());
        map.Bind(count + 1, 1, floor.GetRID()); map.Bind(count + 2, 1, quiet.GetRID());
        var packet = new PhysicsSnapshot(2 << 20); map.Capture(packet); map.Apply(packet);
        Check(PhysicsServer.BodyGetDirectState(bodies[^1].GetRID())!.GetContactCollider(0) == floor.GetRID(), "Mixed local/portable snapshots preserve compact receiver contacts");
        Console.WriteLine($"Mixed contact scene: {count} contacting receivers + empty receiver/one numerical callback, full/compact result bytes={full}/{compact}; local/portable snapshots passed.");

        void Frame()
        {
            for (var i = 0; i < count; i++) { bodies[i].Position = new(i * 30, 0); bodies[i].LinearVelocity = new(5, 20); }
            tree.PhysicsFrame(1d / 60);
        }
        long Measure(bool allFull)
        {
            space.ForceFullGPUContactReports = allFull; Frame(); Frame(); AllocationWarmupCheck.Collect();
            var bytes = store.ReportResultReadbackBytes; var reads = store.ReportReadCount;
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 16; i++) Frame();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0 && store.ReportReadCount == reads + 16, "Mixed complete frames retain zero owner/all-thread allocation and one contact read each");
            return store.ReportResultReadbackBytes - bytes;
        }
    }
}
