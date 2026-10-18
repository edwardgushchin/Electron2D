using System.Diagnostics;
using System.Runtime.InteropServices;
using Electron2D;
using SDL3;
using static PhysicsDebugTests;
using Body = Electron2D.GPUPhysicsBodyStore.BodyHandle;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class GPUPhysicsCheckpointTests
{
    internal static void Run()
    {
        ShapelessPortable(); ReplayColorCacheGrowth(); ReplayContacts(); ReplayJointsAndTargets(); ReplayOneWayGrowth(); ReplayCCD(); SleepAndPending(); Guards(); CopyFailure(); Warm();
        Console.WriteLine("GPU checkpoints: contact/joint/sleep/target/one-way replay, growth, publication, configuration/lifetime guards and warmed allocation passed.");
    }
    private static Body Add(GPUPhysicsBodyStore store, Vector2 position, Mode mode = Mode.Rigid, bool sleep = false) =>
        store.Add(new(mode, position, 0, default, 0, CanSleep: sleep));
    private static void Step(GPUPhysicsBodyStore store, int frames = 1, float gravity = 980)
    { for (var i = 0; i < frames; i++) store.Simulate(1f / 60, new(0, gravity)); }
    private static void ShapelessPortable()
    {
        foreach (var joint in new[] { false, true })
        {
            using var store = new GPUPhysicsBodyStore();
            var a = Add(store, default); var b = Add(store, new(30, 0));
            if (joint) store.AddJoint(new(PhysicsServer.JointType.Pin, a, b, Transform.Identity, Transform.Identity));
            store.Step(0, default); store.BeginPortableRestore();
            Restore(a, default); Restore(b, new(30, 0));
            Check(GPUPhysicsBodyStore.BufferCreatedForTesting is null, "Portable initialization owns its allocation probe");
            GPUPhysicsBodyStore.BufferCreatedForTesting = PoisonStatus;
            try { store.CompletePortableRestore([]); }
            finally { GPUPhysicsBodyStore.BufferCreatedForTesting = null; }
            Check(store.ActiveSimulationBodyCount == 2 && store.PublishedActiveBodyCount == 2 && store.PublishedIslandCount == (joint ? 1 : 0),
                $"Shapeless portable status, joint={joint}: awake={store.ActiveSimulationBodyCount}, active={store.PublishedActiveBodyCount}, islands={store.PublishedIslandCount}");
            void Restore(Body body, Vector2 position) => store.SetPortableMotion(body, new(0, position), new(3, 0), 0, 0,
                false, false, default, 0, default, 0, default, 0, 0, true);
        }
    }
    private static unsafe void PoisonStatus(nint device, nint buffer, uint size)
    {
        if (size != 16) return;
        var transfer = SDL.CreateGPUTransferBuffer(device, new() { Size = size, Usage = SDL.GPUTransferBufferUsage.Upload });
        Check(transfer != 0, "Create status initialization probe");
        nint command = 0, fence = 0;
        try
        {
            var mapped = SDL.MapGPUTransferBuffer(device, transfer, false); Check(mapped != 0, "Map status initialization probe");
            new Span<byte>((void*)mapped, (int)size).Fill(255); SDL.UnmapGPUTransferBuffer(device, transfer);
            command = SDL.AcquireGPUCommandBuffer(device); Check(command != 0, "Acquire status initialization probe");
            var copy = SDL.BeginGPUCopyPass(command); Check(copy != 0, "Begin status initialization probe");
            SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = transfer }, new() { Buffer = buffer, Size = size }, false); SDL.EndGPUCopyPass(copy);
            var submitted = command; command = 0; fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
            Check(fence != 0 && SDL.WaitForGPUFences(device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "Finish status initialization probe");
        }
        finally
        {
            if (command != 0) SDL.CancelGPUCommandBuffer(command);
            if (fence != 0) SDL.ReleaseGPUFence(device, fence);
            SDL.ReleaseGPUTransferBuffer(device, transfer);
        }
    }
    private static void ReplayColorCacheGrowth()
    {
        using var store = new GPUPhysicsBodyStore(); using var circle = new CircleShape { Radius = 1 };
        store.AddShape(Add(store, default), circle);
        for (var i = 0; i < 4; i++) store.AddShape(Add(store, new(2, 0), Mode.Static), circle);
        var supports = new Body[96];
        for (var i = 0; i < supports.Length; i++)
        {
            store.AddShape(Add(store, new(100 + 10 * i, 0)), circle);
            supports[i] = Add(store, new(100 + 10 * i, 10), Mode.Static); store.AddShape(supports[i], circle);
        }
        Solve(); Solve();
        Check(store.ContactPointCount == 4 && store.ContactColorCount == 4 && store.ContactColorRounds == 0,
            "Capture starts with a validated four-row color cache");
        using var point = store.CreateCheckpoint(); var capacity = store.ContactColorCacheStorageBytes;
        for (var i = 0; i < supports.Length; i++) store.SetPose(supports[i], new(100 + 10 * i, 2), 0);
        Solve(); Solve();
        Check(store.ContactPointCount == 100 && store.ContactColorCacheStorageBytes > capacity,
            "The discarded future grows the live key records and table beyond the captured lookup size");
        point.Restore(); Solve();
        Check(store.ContactPointCount == 4 && store.ContactColorCount == 4 && store.ContactColorRounds == 0 && store.ContactColorFallbacks == 0,
            "Restore retains the captured key table and lookup size inside larger live allocations");
        Console.WriteLine("GPU checkpoint color cache: four-row scheduling survives a discarded 100-row allocation growth without rebuilding or fallback.");
        void Solve() => store.SolveConstraints(1f / 60, iterations: 1, margin: 0);
    }
    private static void ReplayContacts()
    {
        using var store = new GPUPhysicsBodyStore { CaptureContactReports = true };
        using var box = new RectangleShape { Size = new(20, 20) }; using var floor = new RectangleShape { Size = new(600, 20) };
        store.AddShape(Add(store, new(0, 200), Mode.Static), floor);
        var bodies = new Body[8];
        for (var i = 0; i < bodies.Length; i++) { bodies[i] = Add(store, new(0, 180 - 20 * i)); store.AddShape(bodies[i], box); }
        Step(store, 120);
        var before = new GPUPhysicsBodyStore.Snapshot[bodies.Length]; store.Read(bodies, before);
        var reports = new GPUPhysicsBodyStore.ContactReport[4]; var counts = new int[1];
        store.ReadContactReports(bodies.AsSpan(0, 1), new[] { 4 }, counts, reports); var count = counts[0];
        Check(count > 0, "Checkpoint contains real solved contacts");
        var changes = new GPUPhysicsBodyStore.BodyChange[store.BodySlotCount]; store.ReadChanges(changes);
        using var checkpoint = store.CreateCheckpoint();
        var expected = new GPUPhysicsBodyStore.Snapshot[bodies.Length]; var actual = new GPUPhysicsBodyStore.Snapshot[bodies.Length];
        Advance(); store.Read(bodies, expected); store.ReadChanges(changes);
        checkpoint.Restore(); store.Read(bodies, actual); Exact(before, actual);
        Check(store.ReadChanges(changes) == store.Count, "Restore republishes every live body instead of retaining future consumer state");
        var restored = new GPUPhysicsBodyStore.ContactReport[4];
        store.ReadContactReports(bodies.AsSpan(0, 1), new[] { 4 }, counts, restored);
        Check(counts[0] == count && MemoryMarshal.AsBytes(reports.AsSpan(0, count)).SequenceEqual(MemoryMarshal.AsBytes(restored.AsSpan(0, count))), "Completed contact reports survive rollback");
        Advance(); store.Read(bodies, actual); Near(expected, actual, .02f, .1f);
        // Keep both old and new checkpoints alive; neither aliases live device storage.
        using var later = store.CreateCheckpoint(); Step(store, 3); checkpoint.Restore(); store.Read(bodies, actual); Exact(before, actual);
        later.Restore(); store.Read(bodies, actual); Near(expected, actual, .02f, .1f);
        Console.WriteLine("GPU checkpoint contacts: exact restored publication and reports; 40-frame replay within .02 position/.1 velocity units.");
        void Advance()
        {
            for (var i = 0; i < 40; i++)
            {
                if (i == 1) store.ApplyImpulse(bodies[^1], new(8, -3), .2f);
                if (i == 8) store.ApplyForce(bodies[2], new(10, 0), 0);
                if (i == 12) store.SetConstantForce(bodies[3], new(2, 0), 0);
                if (i == 28) store.SetConstantForce(bodies[3], default, 0);
                Step(store);
            }
        }
    }
    private static void ReplayJointsAndTargets()
    {
        using var store = new GPUPhysicsBodyStore(); using var circle = new CircleShape { Radius = 5 };
        var a = Add(store, new(-15, 0)); var b = Add(store, new(15, 0));
        store.AddShape(a, circle); store.AddShape(b, circle);
        store.AddJoint(new(PhysicsServer.JointType.Pin, a, b, new(0, new(15, 0)), new(0, new(-15, 0))) { MotorEnabled = true, MotorVelocity = 1, MotorMaxTorque = 40 });
        var moving = Add(store, new(100, 50), Mode.Kinematic); store.AddShape(moving, circle);
        Step(store, 30, 0); store.SetKinematicTarget(moving, new(130, 50), .4f);
        using var checkpoint = store.CreateCheckpoint();
        var bodies = new[] { a, b, moving }; var expected = new GPUPhysicsBodyStore.Snapshot[3]; var actual = new GPUPhysicsBodyStore.Snapshot[3];
        Advance(); store.Read(bodies, expected); checkpoint.Restore(); Advance(); store.Read(bodies, actual); Near(expected, actual, .005f, .005f);
        Check(actual[2].Position.DistanceTo(new(130, 50)) < .001f, "Pending target survives capture and restore");
        Console.WriteLine("GPU checkpoint joints/targets: motor warm state and pending kinematic target replay within .005 units.");
        void Advance() { store.ApplyImpulse(a, new(4, 1), 0); Step(store, 30, 0); }
    }
    private static void ReplayOneWayGrowth()
    {
        using var store = new GPUPhysicsBodyStore(); using var circle = new CircleShape { Radius = 10 };
        using var platform = new RectangleShape { Size = new(3000, 4) };
        var floor = store.AddShape(Add(store, default, Mode.Static), platform);
        store.SetShapeOneWay(floor, new(true, Vector2.Up, 8));
        var bodies = new Body[96];
        for (var i = 0; i < bodies.Length; i++) { bodies[i] = Add(store, i == 0 ? new(0, -12) : new(10000 + i * 30, -12)); store.AddShape(bodies[i], circle); }
        Step(store, 8); var savedPairs = store.OneWayPairCount; Check(savedPairs > 0, "One-way episode exists before capture");
        using var checkpoint = store.CreateCheckpoint();
        var expected = new GPUPhysicsBodyStore.Snapshot[1]; var actual = new GPUPhysicsBodyStore.Snapshot[1];
        Step(store, 24); store.Read(bodies.AsSpan(0, 1), expected);
        for (var i = 0; i < bodies.Length; i++) { store.SetPose(bodies[i], new((i - 48) * 24, -12), 0); store.SetVelocity(bodies[i], default, 0); }
        Step(store, 8); Check(store.OneWayPairCount > savedPairs + 64, "Future episode/hash storage grows");
        checkpoint.Restore(); Check(store.OneWayPairCount == savedPairs, "Restored one-way history count is not the future count");
        Step(store, 24); store.Read(bodies.AsSpan(0, 1), actual); Near(expected, actual, .001f, .001f);
        Console.WriteLine("GPU checkpoint one-way: replay survives later contact/history/hash capacity growth.");
    }
    private static void ReplayCCD()
    {
        using var store = new GPUPhysicsBodyStore(); using var circle = new CircleShape { Radius = 2 };
        using var wall = new RectangleShape { Size = new(1, 200) };
        store.AddShape(Add(store, default, Mode.Static), wall);
        var body = Add(store, new(-20, 0)); store.AddShape(body, circle, friction: 0, bounce: .5f);
        store.SetCCDMode(body, CCDMode.CastShape); store.SetVelocity(body, new(2000, 0), 0);
        using var checkpoint = store.CreateCheckpoint();
        var expected = new GPUPhysicsBodyStore.Snapshot[1]; var actual = new GPUPhysicsBodyStore.Snapshot[1];
        Step(store, 3, 0); store.Read([body], expected);
        Check(expected[0].Position.X < -2 && expected[0].Velocity.X < 0, "Real swept impact rebounds on the original side");
        checkpoint.Restore(); Step(store, 3, 0); store.Read([body], actual); Near(expected, actual, .001f, .001f);
        Console.WriteLine("GPU checkpoint CCD: thin-wall impact interval and rebound replay within .001 units.");
    }
    private static void CopyFailure()
    {
        using var store = new GPUPhysicsBodyStore(); var body = Add(store, default);
        using var checkpoint = store.CreateCheckpoint();
        checkpoint.BeforeSubmit = () =>
        {
            Reject<InvalidOperationException>(store.Dispose); Reject<InvalidOperationException>(checkpoint.Dispose);
            throw new IOException("Injected checkpoint submission failure");
        };
        Reject<IOException>(checkpoint.Restore);
        Check(store.HasFailed, "A failed restore cannot resume a partially restored world");
        checkpoint.BeforeSubmit = null;
        Reject<InvalidOperationException>(checkpoint.Restore);
        Reject<InvalidOperationException>(() => store.Read([body], new GPUPhysicsBodyStore.Snapshot[1]));
    }

    private static void SleepAndPending()
    {
        using var store = new GPUPhysicsBodyStore(); using var circle = new CircleShape { Radius = 10 };
        var body = Add(store, default, sleep: true); store.AddShape(body, circle); Step(store, 90, 0);
        var saved = new GPUPhysicsBodyStore.Snapshot[1]; var actual = new GPUPhysicsBodyStore.Snapshot[1]; store.Read([body], saved);
        Check(saved[0].Sleeping, "Body has completed the physical sleep delay");
        using var checkpoint = store.CreateCheckpoint(); store.ApplyImpulse(body, new(80, 0), 0); Step(store, 4, 0);
        checkpoint.Restore(); store.Read([body], actual); Exact(saved, actual);
        var submits = store.SubmissionCount; Step(store, 8, 0); Check(store.SubmissionCount == submits, "Restored sleeping state retains the no-work idle path");
        store.SetSleeping(body, false); store.ApplyForce(body, new(60, 0), 0); checkpoint.Capture();
        Step(store, 1, 0); store.Read([body], saved); Step(store, 8, 0);
        // An unsubmitted edit in the discarded future must not survive restore.
        store.SetVelocity(body, new(999, 0), 0); checkpoint.Restore(); Step(store, 1, 0); store.Read([body], actual); Exact(saved, actual);
        Check(Math.Abs(actual[0].Velocity.X - 1) < .001f, "Captured transient force is consumed exactly once");
    }
    private static void Guards()
    {
        using var store = new GPUPhysicsBodyStore(); using var circle = new CircleShape { Radius = 2 };
        var body = Add(store, default); store.AddShape(body, circle); Step(store, 1, 0);
        var checkpoint = store.CreateCheckpoint();
        Reject<InvalidOperationException>(() => Task.Run(checkpoint.Restore).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(checkpoint.Dispose).GetAwaiter().GetResult());
        circle.Radius = 3; Reject<InvalidOperationException>(checkpoint.Restore); Check(!store.HasFailed, "Schema rejection does not invalidate the live world");
        checkpoint.Capture(); var added = Add(store, new(40, 0)); Reject<InvalidOperationException>(checkpoint.Restore); store.Remove(added);
        checkpoint.Capture(); store.SetMassProfile(body, new(2)); Reject<InvalidOperationException>(checkpoint.Restore);
        checkpoint.Capture(); store.Dispose(); Reject<ObjectDisposedException>(checkpoint.Restore); checkpoint.Dispose();
        using var failed = new GPUPhysicsBodyStore(); var fast = Add(failed, default); using var saved = failed.CreateCheckpoint();
        failed.SetVelocity(fast, new(float.MaxValue, 0), 0);
        Reject<InvalidOperationException>(() => failed.Simulate(float.MaxValue, new(float.MaxValue, 0)));
        Reject<InvalidOperationException>(saved.Restore);
    }
    private static void Warm()
    {
        using var store = new GPUPhysicsBodyStore(); using var circle = new CircleShape { Radius = 4 };
        var bodies = new Body[512];
        for (var i = 0; i < bodies.Length; i++)
        {
            var position = new Vector2((i % 32) * 20, (i / 32) * 20);
            bodies[i] = Add(store, position); store.AddShape(bodies[i], circle);
            store.AddShape(Add(store, position + new Vector2(0, 7), Mode.Static), circle);
        }
        store.AddJoint(new(PhysicsServer.JointType.Pin, bodies[0], default, Transform.Identity, Transform.Identity));
        Step(store, 1, 0); using var checkpoint = store.CreateCheckpoint();
        for (var i = 0; i < 64; i++) Cycle();
        var memory = store.BufferMemory;
        var copied = store.DeviceCopyBytes; var up = store.UploadBytes; var down = store.ReadbackBytes; var submits = store.SubmissionCount; var wait = store.WaitMS;
        var times = new double[128];
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); var begin = Stopwatch.GetTimestamp();
        for (var i = 0; i < 128; i++)
        { var start = Stopwatch.GetTimestamp(); checkpoint.Capture(); checkpoint.Restore(); times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        var elapsed = Stopwatch.GetElapsedTime(begin).TotalMilliseconds;
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, "Warm GPU checkpoint capture/restore allocates zero managed bytes");
        Check(store.BufferMemory == memory, "Warm checkpoint copies allocate no native buffers");
        Array.Sort(times);
        Console.WriteLine($"GPU checkpoint: 1024 bodies/512 colliding pairs/1 world pin, 64 warmups/128 capture+restore samples, mean={elapsed / 128:F4}, p50/p95/p99={times[64]:F4}/{times[121]:F4}/{times[126]:F4} ms, wait={(store.WaitMS - wait) / 128:F4} ms, {owner}/{all} owner/all managed B; device copies={(store.DeviceCopyBytes - copied) / 128} B, up/down={(store.UploadBytes - up) / 128}/{(store.ReadbackBytes - down) / 128} B, submissions={(store.SubmissionCount - submits) / 128}; checkpoint device/authored-array capacity={checkpoint.DeviceCapacityBytes}/{checkpoint.ManagedCapacityBytes} B.");
        var checkpointBytes = checkpoint.DeviceCapacityBytes;
        checkpoint.Dispose();
        var released = store.BufferMemory;
        Check(released.DeviceBytes == memory.DeviceBytes - checkpointBytes && released.TransferBytes == memory.TransferBytes,
            "Checkpoint retirement releases exactly its device copies and retains world transfer capacity");
        Console.WriteLine($"GPU complete buffer capacity, world/checkpoint: device {released.DeviceBytes}/{checkpointBytes}, transfer {released.TransferBytes}, transient peak {memory.PeakBytes} B; live buffers {memory.DeviceBuffers}+{memory.TransferBuffers}, allocations/releases {memory.Allocations}/{memory.Releases}.");
        store.Dispose(); GPUPhysicsMemoryTests.Empty(store);
        void Cycle() { checkpoint.Capture(); store.ApplyImpulse(bodies[0], new(1, 0), 0); Step(store, 1, 0); checkpoint.Restore(); }
    }
    private static void Exact(GPUPhysicsBodyStore.Snapshot[] expected, GPUPhysicsBodyStore.Snapshot[] actual) =>
        Check(MemoryMarshal.AsBytes(expected.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(actual.AsSpan())), "Restore reproduces saved body bytes, including fields and sleep clocks");
    private static void Near(GPUPhysicsBodyStore.Snapshot[] expected, GPUPhysicsBodyStore.Snapshot[] actual, float position, float velocity)
    {
        for (var i = 0; i < expected.Length; i++)
            Check(expected[i].Position.DistanceTo(actual[i].Position) <= position &&
                Math.Abs(expected[i].Rotation - actual[i].Rotation) <= position &&
                System.Numerics.Vector4.Distance(expected[i].Velocity, actual[i].Velocity) <= velocity && expected[i].Sleeping == actual[i].Sleeping,
                $"Replay body {i}: expected {expected[i].Position}/{expected[i].Velocity}, actual {actual[i].Position}/{actual[i].Velocity}; tolerance {position}/{velocity}");
    }
}
