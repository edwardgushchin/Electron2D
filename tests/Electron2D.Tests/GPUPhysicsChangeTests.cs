using System.Diagnostics;
using System.Runtime.InteropServices;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;

internal static class GPUPhysicsChangeTests
{
    internal static void Run()
    {
        VerifyLifecycle();
        VerifySimulation();
        MeasurePublication();
    }

    private static void VerifyLifecycle()
    {
        Check(Marshal.SizeOf<Store.BodyChange>() == 80, "Body publication wire stride");
        using var store = new Store();
        Check(store.ReadChanges([]) == 0, "Empty publication");
        var a = store.Add(new(PhysicsServer.BodyMode.Rigid, new(10, 20), 0, default, 0));
        var b = store.Add(new(PhysicsServer.BodyMode.Static, new(30, 40), 0, new(2, 3), 4));
        var output = new Store.BodyChange[store.BodySlotCount + 1];
        var upload = store.UploadBytes;
        Reject<ArgumentException>(() => store.ReadChanges(output.AsSpan(0, 1)));
        Check(store.UploadBytes == upload, "Capacity rejected before flushing edits");
        output[^1].Generation = 123;
        var count = store.ReadChanges(output);
        Check(count == 2 && output[^1].Generation == 123, "First publication and untouched tail");
        Check(Find(output, count, a).PreviousGeneration == 0 && Find(output, count, a).State.Position == new Vector2(10, 20), "Initial identity and pose");
        Check(Find(output, count, b).State.Velocity.X == 2 && Find(output, count, b).State.Velocity.Z == 4, "Static surface motion published");
        Check(store.ReadChanges(output) == 0, "Unchanged state suppressed");
        store.SetPose(a, new(12, 20), .5f);
        Store.Snapshot[] full = new Store.Snapshot[1];
        store.Read([a], full);
        count = store.ReadChanges(output);
        Check(count == 1 && Find(output, count, a).State.Pose == full[0].Pose, "Selected reads do not consume changes");
        store.SetSleeping(a, true);
        count = store.ReadChanges(output);
        Check(count == 1 && Find(output, count, a).State.Sleeping, "Sleep transition without pose change");
        store.SetSleeping(a, false);
        count = store.ReadChanges(output);
        Check(count == 1 && !Find(output, count, a).State.Sleeping, "Wake transition without pose change");
        store.SetCanSleep(a, false); store.SetCCDMode(a, CCDMode.CastShape);
        store.SetIntegrationPolicy(a, store.GetIntegrationPolicy(a) with { LockRotation = true, OmitForceIntegration = true });
        count = store.ReadChanges(output);
        var policy = Find(output, count, a).State;
        Check(count == 1 && !policy.CanSleep && policy.ContinuousMode == CCDMode.CastShape && policy.RotationLocked && policy.OmitForceIntegration, "Observable policy without pose changes");
        store.SetMode(a, PhysicsServer.BodyMode.Kinematic);
        count = store.ReadChanges(output);
        Check(count == 1 && Find(output, count, a).State.Mode == PhysicsServer.BodyMode.Kinematic, "Role-only change");
        store.Remove(b);
        var replacement = store.Add(new(PhysicsServer.BodyMode.Static, new(30, 40), 0, new(2, 3), 4));
        count = store.ReadChanges(output);
        Check(count == 1 && Find(output, count, replacement).PreviousGeneration == b.Generation && replacement.Generation != b.Generation, "Identical-pose replacement retains both generations");
        store.Remove(replacement);
        count = store.ReadChanges(output);
        Check(count == 1 && output[0].Alive == 0 && output[0].PreviousGeneration == replacement.Generation, "Deletion tombstone");
        Check(store.ReadChanges(output) == 0, "Deletion consumed once");
        for (var i = 0; i < 130; i++) store.Add(new(PhysicsServer.BodyMode.Static, new(i, 100), 0, default, 0));
        Array.Resize(ref output, store.BodySlotCount);
        count = store.ReadChanges(output);
        Check(count == 130 && output.AsSpan(0, count).ToArray().All(change => change.Index != a.Index), "Device growth retains publication history");
        Check(store.ReadChanges(output) == 0, "Grown history initialized");
        Task.Run(() => Reject<InvalidOperationException>(() => store.ReadChanges(output))).GetAwaiter().GetResult();
        store.Dispose();
        Reject<ObjectDisposedException>(() => store.ReadChanges(output));
        using var failed = new Store();
        failed.Add(new(PhysicsServer.BodyMode.Rigid, default, 0, new(float.MaxValue, 0), 0));
        Reject<InvalidOperationException>(() => failed.Step(2, default));
        Reject<InvalidOperationException>(() => failed.ReadChanges(output));
        Console.WriteLine("GPU changed-body publication: lifetime, replacement, removal, growth, guards and failure passed.");
    }

    private static void VerifySimulation()
    {
        using var store = new Store();
        using var circle = new CircleShape { Radius = 5 };
        using var floorShape = new RectangleShape { Size = new(200, 10) };
        var handles = new Store.BodyHandle[9];
        handles[0] = store.Add(new(PhysicsServer.BodyMode.Static, new(0, 40), 0, default, 0));
        store.AddShape(handles[0], floorShape);
        for (var i = 1; i < handles.Length; i++)
        {
            handles[i] = store.Add(new(PhysicsServer.BodyMode.Rigid, new(-80 + i * 18, 0), 0, default, 0));
            store.AddShape(handles[i], circle, bounce: .2f);
        }
        var changes = new Store.BodyChange[handles.Length];
        var projected = new Store.Snapshot[handles.Length];
        var reference = new Store.Snapshot[handles.Length];
        for (var tick = 0; tick < 180; tick++)
        {
            if (tick == 120) store.SetSleeping(handles[1], true);
            if (tick == 130) store.ApplyImpulse(handles[1], new(0, -40));
            store.Simulate(1f / 60, new(0, 98), substeps: 2);
            var count = store.ReadChanges(changes);
            foreach (ref readonly var change in changes.AsSpan(0, count)) projected[change.Index] = change.State;
            store.Read(handles, reference);
            for (var i = 0; i < handles.Length; i++)
                Check(projected[i].Pose == reference[i].Pose && projected[i].Velocity == reference[i].Velocity && projected[i].Fields == reference[i].Fields &&
                    projected[i].Sleeping == reference[i].Sleeping && projected[i].Mode == reference[i].Mode, "Sparse publication preserves actual solved state exactly");
        }
        // A sleep clock alone is solver bookkeeping; it must not cause a body-state update.
        using var quiet = new Store();
        quiet.Add(new(PhysicsServer.BodyMode.Rigid, default, 0, default, 0, CanSleep: false));
        quiet.Step(1f / 60, default); quiet.ReadChanges(changes);
        for (var i = 0; i < 20; i++) { quiet.Step(1f / 60, default); Check(quiet.ReadChanges(changes) == 0, "Stationary awake body is unchanged"); }
        Console.WriteLine("GPU changed-body publication: solved contacts, motion, sleep/wake and exact consumer projection passed.");
    }

    private static void MeasurePublication()
    {
        const int population = 65_536, warmup = 128, samples = 128;
        using var store = new Store();
        var bodies = new Store.BodyHandle[population];
        var full = new Store.Snapshot[population];
        var changes = new Store.BodyChange[population];
        var fullMS = new double[samples]; var changeMS = new double[samples];
        for (var i = 0; i < population; i++) bodies[i] = store.Add(new(PhysicsServer.BodyMode.Rigid, new(i % 256, i / 256), 0, default, 0, Sleeping: true));
        foreach (var active in new[] { 0, 1, 256, population })
        {
            for (var i = 0; i < active; i++) { store.SetSleeping(bodies[i], false); store.SetVelocity(bodies[i], new(.25f, .5f), 0); }
            for (var i = 0; i < warmup; i++) { store.Step(1f / 120, default); store.Read(bodies, full); store.ReadChanges(changes); }
            long fullUp = 0, fullDown = 0, changeUp = 0, changeDown = 0;
            double fullWait = 0, changeWait = 0;
            var allocated = GC.GetAllocatedBytesForCurrentThread(); var allAllocated = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < samples; i++)
            {
                store.Step(1f / 120, default);
                // Alternate order so the second reader does not always get the warmed device/cache.
                if ((i & 1) == 0) { Full(i); Changes(i); } else { Changes(i); Full(i); }
            }
            allAllocated = GC.GetTotalAllocatedBytes(true) - allAllocated; allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Check(allocated == 0 && allAllocated == 0, "Warmed publication has no managed allocations");
            Check(changeDown == samples * (8L + active * 80) && fullDown == samples * (8L + population * 64), "Only changed prefix downloaded");
            Array.Sort(fullMS); Array.Sort(changeMS);
            Console.WriteLine($"GPU publication {active}/{population} changed, {warmup} warmup/{samples} samples: full p50/p95/p99={fullMS[64]:F4}/{fullMS[121]:F4}/{fullMS[126]:F4} ms, wait={fullWait / samples:F4} ms, up/down={fullUp / samples}/{fullDown / samples} B; changes={changeMS[64]:F4}/{changeMS[121]:F4}/{changeMS[126]:F4} ms, wait={changeWait / samples:F4} ms, up/down={changeUp / samples}/{changeDown / samples} B; {allocated}/{allAllocated} managed B; device/transfer capacity={store.ChangeDeviceCapacityBytes}/{store.ChangeTransferCapacityBytes} B; {store.Driver}, {store.DeviceName}.");

            void Full(int sample)
            {
                var up = store.UploadBytes; var down = store.ReadbackBytes; var wait = store.WaitMS; var start = Stopwatch.GetTimestamp();
                store.Read(bodies, full); fullMS[sample] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                fullUp += store.UploadBytes - up; fullDown += store.ReadbackBytes - down; fullWait += store.WaitMS - wait;
            }
            void Changes(int sample)
            {
                var up = store.UploadBytes; var down = store.ReadbackBytes; var wait = store.WaitMS; var start = Stopwatch.GetTimestamp();
                var count = store.ReadChanges(changes); changeMS[sample] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                changeUp += store.UploadBytes - up; changeDown += store.ReadbackBytes - down; changeWait += store.WaitMS - wait;
                Check(count == active, "Changed-body population retained");
            }
        }
    }

    private static Store.BodyChange Find(Store.BodyChange[] changes, int count, Store.BodyHandle body)
    {
        for (var i = 0; i < count; i++) if (changes[i].Index == body.Index && changes[i].Generation == body.Generation) return changes[i];
        throw new InvalidOperationException("Missing changed body.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
}
