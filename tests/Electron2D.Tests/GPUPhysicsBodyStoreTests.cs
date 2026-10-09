using System.Diagnostics;
using Electron2D;

internal static class GPUPhysicsBodyStoreTests
{
    internal static void Run()
    {
        VerifyMotionAndEdits();
        VerifyFailure();
        VerifyResidencyAndAllocation();
    }

    private static void VerifyMotionAndEdits()
    {
        using var store = new GPUPhysicsBodyStore();
        var dynamicBody = store.Add(new(PhysicsServer.BodyMode.Rigid, new(10, 20), 0, new(3, 0), 0.2f,
            Mass: 2, Inertia: 3, GravityScale: 0.5f, ConstantForce: new(4, 0), ConstantTorque: 6));
        var stationary = store.Add(new(PhysicsServer.BodyMode.Static, new(70, 80), 0.7f, new(9, 8), 3));
        var kinematic = store.Add(new(PhysicsServer.BodyMode.Kinematic, new(2, 3), 0, new(5, -2), 0));
        var locked = store.Add(new(PhysicsServer.BodyMode.RigidLinear, new(0, 0), 0, new(0, 0), 8, Inertia: 1, GravityScale: 0));
        var damped = store.Add(new(PhysicsServer.BodyMode.Rigid, Vector2.Zero, 0, new(4, 3), 1,
            Inertia: 1, GravityScale: 0, LinearDamp: 10, AngularDamp: 5));
        var amplified = store.Add(new(PhysicsServer.BodyMode.Rigid, Vector2.Zero, 0, new(1, 0), 1,
            Inertia: 1, GravityScale: 0, LinearDamp: -1, AngularDamp: -2));
        GPUPhysicsBodyStore.BodyHandle[] handles = [dynamicBody, stationary, kinematic, locked, damped, amplified];
        var state = new GPUPhysicsBodyStore.Snapshot[handles.Length];
        const float dt = 1f / 120;
        for (var i = 0; i < 120; i++) store.Step(dt, new(0, 100));
        store.Read(handles, state);
        var accelerationTime = dt * dt * 120 * 121 / 2;
        // Semi-implicit Euler with constant acceleration; tolerances allow float accumulation
        // over 120 ticks in scene units, not a comparison to another solver's bit pattern.
        Near(state[0].Position, new(13 + 2 * accelerationTime, 20 + 50 * accelerationTime), 0.002f, "Resident mass/force/gravity integration");
        Near(new(state[0].Velocity.X, state[0].Velocity.Y), new(5, 50), 0.001f, "Resident velocity");
        Check(MathF.Abs(state[0].Rotation - (0.2f + 2 * accelerationTime)) < 0.001f, "Angular torque and rotation integrate on the device.");
        Near(state[1].Position, new(70, 80), 0, "Static pose does not integrate stored velocity");
        Near(state[2].Position, new(7, 1), 0.001f, "Kinematic velocity ignores gravity");
        Check(state[3].Rotation == 0 && state[3].Velocity.Z == 0, "RigidLinear keeps rotation locked.");

        Near(new(state[4].Velocity.X, state[4].Velocity.Y), new Vector2(4, 3) * MathF.Pow(1 - dt * 10, 120), 0.00001f,
            "Finite positive damping applies the per-step decay");
        Check(MathF.Abs(state[4].Velocity.Z - MathF.Pow(1 - dt * 5, 120)) < 0.00001f &&
            MathF.Abs(state[5].Velocity.X - MathF.Pow(1 + dt, 120)) < 0.0001f &&
            MathF.Abs(state[5].Velocity.Z - MathF.Pow(1 + dt * 2, 120)) < 0.0001f,
            "Angular and signed damping retain their configured effects.");

        var position = state[0].Position;
        store.SetConstantForce(dynamicBody, Vector2.Zero);
        store.ApplyImpulse(dynamicBody, new(100, 100), 100);
        store.SetVelocity(dynamicBody, new(5, 6), 2);
        store.ApplyImpulse(dynamicBody, new(4, -2), 3);
        store.Read(handles, state);
        Near(state[0].Position, position, 0, "Reading queued edits does not advance time");
        Near(new(state[0].Velocity.X, state[0].Velocity.Y), new(7, 5), 0.00001f, "Queued velocity overrides prior impulses and retains later impulses");
        Check(MathF.Abs(state[0].Velocity.Z - 3) < 0.00001f, "Angular impulse respects inertia.");
        store.SetPose(dynamicBody, new(42, 43), -0.4f);
        var uploads = store.UploadBytes; var readbacks = store.ReadbackBytes;
        store.Read(handles.AsSpan(0, 1), state);
        Near(state[0].Position, new(42, 43), 0, "Sparse pose edit");
        Check(store.UploadBytes - uploads == 8 + 144 + 16 && store.ReadbackBytes - readbacks == 8 + 48,
            "One edit and one requested body transfer only their command, handle, result and status.");

        var copies = store.DeviceCopyBytes;
        for (var i = 0; i < 130; i++) store.Add(new(PhysicsServer.BodyMode.Rigid, new(i, i), 0, Vector2.Zero, 0));
        store.Read(handles.AsSpan(0, 1), state);
        Near(state[0].Position, new(42, 43), 0, "Capacity growth preserves evolved device state");
        Check(store.DeviceCopyBytes > copies, "Growth copies resident bodies on the device.");
        store.Remove(dynamicBody);
        var replacement = store.Add(new(PhysicsServer.BodyMode.Rigid, new(91, 92), 0, Vector2.Zero, 0));
        Check(replacement.Index == dynamicBody.Index && replacement.Generation != dynamicBody.Generation, "Reused slots have fresh identities.");
        Reject<ArgumentException>(() => store.SetPose(dynamicBody, Vector2.Zero, 0));
        handles[0] = replacement;
        store.Read(handles.AsSpan(0, 1), state);
        Near(state[0].Position, new(91, 92), 0, "Remove/recreate before publication replaces the old device generation");
        using var other = new GPUPhysicsBodyStore();
        other.Add(new(PhysicsServer.BodyMode.Rigid, Vector2.Zero, 0, Vector2.Zero, 0));
        var foreign = other.Add(new(PhysicsServer.BodyMode.Rigid, Vector2.Zero, 0, Vector2.Zero, 0));
        Check(foreign.Index == stationary.Index && foreign.Generation == stationary.Generation, "Foreign-handle check covers matching slot and generation.");
        Reject<ArgumentException>(() => store.SetVelocity(foreign, Vector2.Zero, 0));
        Reject<ArgumentOutOfRangeException>(() => store.Step(float.NaN, Vector2.Zero));
        Reject<ArgumentOutOfRangeException>(() => store.SetPose(replacement, new(float.PositiveInfinity, 0), 0));
        Task.Run(() => Reject<InvalidOperationException>(() => store.Step(dt, Vector2.Zero))).GetAwaiter().GetResult();
        Console.WriteLine("Resident body motion, sparse edits, growth and generation/owner guards passed.");
    }

    private static void VerifyFailure()
    {
        using var store = new GPUPhysicsBodyStore();
        var body = store.Add(new(PhysicsServer.BodyMode.Rigid, Vector2.Zero, 0, new(float.MaxValue, 0), 0));
        Reject<InvalidOperationException>(() => store.Step(2, Vector2.Zero));
        Reject<InvalidOperationException>(() => store.Step(0.01f, Vector2.Zero));
        Reject<InvalidOperationException>(() => store.SetVelocity(body, Vector2.Zero, 0));
        Reject<InvalidOperationException>(() => store.Read([body], new GPUPhysicsBodyStore.Snapshot[1]));
        Console.WriteLine("Nonfinite device integration invalidates resident state without CPU replay.");
    }

    private static void VerifyResidencyAndAllocation()
    {
        const int count = 65_536, warmup = 384, samples = 256;
        using var store = new GPUPhysicsBodyStore();
        var handles = new GPUPhysicsBodyStore.BodyHandle[count];
        for (var i = 0; i < count; i++)
        {
            var body = store.Add(new(PhysicsServer.BodyMode.Rigid, new(i % 256, i / 256), 0, new(0.25f, 0.5f), 0));
            handles[i] = body;
        }
        for (var i = 0; i < warmup; i++) store.Step(1f / 120, Vector2.Zero);
        var timings = new double[samples];
        var upload = store.UploadBytes; var readback = store.ReadbackBytes; var wait = store.WaitMS; var uniforms = store.UniformBytes;
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < samples; i++)
        {
            var start = Stopwatch.GetTimestamp();
            store.Step(1f / 120, Vector2.Zero);
            timings[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        var measuredWait = store.WaitMS - wait;
        Check(allocated == 0, "Warmed resident body steps allocate zero managed bytes.");
        Check(store.UploadBytes - upload == 8 * samples && store.ReadbackBytes - readback == 8 * samples && store.UniformBytes - uniforms == 32 * samples,
            "Resident steps exchange a eight-byte status reset/result and 32 uniform bytes, not poses or velocities.");
        var snapshots = new GPUPhysicsBodyStore.Snapshot[count];
        readback = store.ReadbackBytes; upload = store.UploadBytes;
        store.Read(handles.AsSpan(0, 2), snapshots);
        Check(store.ReadbackBytes - readback == 8 + 2 * 48 && store.UploadBytes - upload == 8 + 2 * 16,
            "Explicit readback downloads only the requested two bodies.");
        var travel = new Vector2(0.25f, 0.5f) * ((warmup + samples) / 120f);
        // Diagnostic full read is outside the measured step window and validates every dispatched body.
        var readStart = Stopwatch.GetTimestamp();
        store.Read(handles, snapshots);
        var fullReadMS = Stopwatch.GetElapsedTime(readStart).TotalMilliseconds;
        for (var i = 0; i < count; i++)
            Near(snapshots[i].Position, new Vector2(i % 256, i / 256) + travel, 0.02f, "Every resident body advances without CPU publication");
        Array.Sort(timings);
        Console.WriteLine($"Resident integration only: {count} bodies, {warmup} warmup, {samples} samples, p50={timings[samples / 2]:F4} ms, p95={timings[(int)(samples * 0.95)]:F4} ms, wait={measuredWait / samples:F4} ms/tick, {allocated} B/tick, 8 B buffer upload + 32 B uniforms + 8 B readback/tick; driver={store.Driver}, device={store.DeviceName}, runtime={Environment.Version}; diagnostic full read={fullReadMS:F4} ms (outside step samples); authored body/command capacity={store.AuthoredBodyCapacityBytes} B.");
    }

    private static void Near(Vector2 actual, Vector2 expected, float tolerance, string message) => Check(actual.DistanceTo(expected) <= tolerance, message);
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
