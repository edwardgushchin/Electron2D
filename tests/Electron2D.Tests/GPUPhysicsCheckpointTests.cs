using System.Diagnostics;
using System.Runtime.InteropServices;
using Electron2D;
using static PhysicsDebugTests;
using Body = Electron2D.GPUPhysicsBodyStore.BodyHandle;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class GPUPhysicsCheckpointTests
{
    internal static void Run()
    {
        ReplayContacts(); ReplayJointsAndTargets(); ReplayOneWayGrowth(); ReplayCCD(); SleepAndPending(); Guards(); CopyFailure(); Warm();
        Console.WriteLine("GPU checkpoints: contact/joint/sleep/target/one-way replay, growth, publication, configuration/lifetime guards and warmed allocation passed.");
    }
    private static Body Add(GPUPhysicsBodyStore store, Vector2 position, Mode mode = Mode.Rigid, bool sleep = false) =>
        store.Add(new(mode, position, 0, default, 0, CanSleep: sleep));
    private static void Step(GPUPhysicsBodyStore store, int frames = 1, float gravity = 980)
    { for (var i = 0; i < frames; i++) store.Simulate(1f / 60, new(0, gravity)); }
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
        var copied = store.DeviceCopyBytes; var up = store.UploadBytes; var down = store.ReadbackBytes; var submits = store.SubmissionCount; var wait = store.WaitMS;
        var times = new double[128];
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); var begin = Stopwatch.GetTimestamp();
        for (var i = 0; i < 128; i++)
        { var start = Stopwatch.GetTimestamp(); checkpoint.Capture(); checkpoint.Restore(); times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        var elapsed = Stopwatch.GetElapsedTime(begin).TotalMilliseconds;
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, "Warm GPU checkpoint capture/restore allocates zero managed bytes");
        Array.Sort(times);
        Console.WriteLine($"GPU checkpoint: 1024 bodies/512 colliding pairs/1 world pin, 64 warmups/128 capture+restore samples, mean={elapsed / 128:F4}, p50/p95/p99={times[64]:F4}/{times[121]:F4}/{times[126]:F4} ms, wait={(store.WaitMS - wait) / 128:F4} ms, {owner}/{all} owner/all managed B; device copies={(store.DeviceCopyBytes - copied) / 128} B, up/down={(store.UploadBytes - up) / 128}/{(store.ReadbackBytes - down) / 128} B, submissions={(store.SubmissionCount - submits) / 128}; checkpoint device/authored-array capacity={checkpoint.DeviceCapacityBytes}/{checkpoint.ManagedCapacityBytes} B.");
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
