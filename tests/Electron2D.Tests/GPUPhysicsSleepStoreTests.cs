using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;
using Kind = Electron2D.PhysicsServer.JointType;

internal static class GPUPhysicsSleepStoreTests
{
    internal static void Run()
    {
        VerifyCommands(); VerifyIslands(); VerifySupports(); VerifyImpactAndResources(); VerifyJoints(); VerifyResidency(64); VerifyResidency(256);
        Console.WriteLine("Resident sleep: settings, ordered commands, connected sleep/wake, support changes, joints and residency passed.");
    }
    private static Store.BodyHandle Add(Store s, Vector2 at = default, bool sleep = false, bool canSleep = true, Mode mode = Mode.Rigid) =>
        s.Add(new(mode, at, 0, Vector2.Zero, 0, Sleeping: sleep, CanSleep: canSleep));
    private static void Settle(Store s, int ticks = 20, Vector2 gravity = default)
    { for (var i = 0; i < ticks; i++) s.Simulate(0.01f, gravity, substeps: 1); }
    private static Store.Snapshot Read(Store s, Store.BodyHandle b)
    { Span<Store.Snapshot> state = stackalloc Store.Snapshot[1]; s.Read([b], state); return state[0]; }
    private static void VerifyCommands()
    {
        using var s = new Store(); s.SetSleepSettings(new(2, 0.1f, 0.05f));
        var b = Add(s, sleep: true); using var shape = new CircleShape { Radius = 2 }; s.AddShape(b, shape);
        Check(Read(s, b).Sleeping, "Initial sleeping configuration survives geometry creation.");
        s.SetConstantForce(b, new(10, 0)); s.SetSleeping(b, true); s.Simulate(0.01f, Vector2.Zero, substeps: 1);
        Check(Read(s, b).Sleeping && Read(s, b).Position == Vector2.Zero, "Latest explicit sleep preserves pending force without integrating it.");
        s.SetSleeping(b, false); s.Step(0.1f, Vector2.Zero);
        Near(Read(s, b).Velocity.X, 1, 0.00001f, "Wake retains constant force");
        s.SetConstantForce(b, Vector2.Zero); s.SetSleeping(b, true); s.ApplyImpulse(b, new(3, 0));
        Check(!Read(s, b).Sleeping && Read(s, b).Velocity.X == 3, "Impulse after sleep wakes and applies.");
        s.ApplyImpulse(b, new(3, 0)); s.SetSleeping(b, true);
        Check(Read(s, b).Sleeping && Read(s, b).Velocity.X == 0, "Sleep after impulse clears motion.");
        s.SetVelocity(b, Vector2.Zero, 0); Check(!Read(s, b).Sleeping, "Explicit zero velocity wakes.");
        s.Simulate(0, Vector2.Zero); Near(Read(s, b).SleepTime, 0, 0, "Zero delta does not age sleep");
        Settle(s); Check(Read(s, b).Sleeping, "An isolated idle body sleeps after the configured interval.");
        s.SetCanSleep(b, false); Settle(s);
        Check(!Read(s, b).Sleeping && !Read(s, b).CanSleep && !s.GetCanSleep(b), "Disabling sleep wakes and prevents automatic sleep.");
        s.SetCanSleep(b, true); s.SetVelocity(b, new(3, 0), 0); Settle(s);
        Check(!Read(s, b).Sleeping, "Linear threshold prevents sleep.");
        s.SetVelocity(b, Vector2.Zero, 0.2f); Settle(s); Check(!Read(s, b).Sleeping, "Angular threshold prevents sleep.");
        s.SetSleepSettings(new(0, 0, 0)); s.SetVelocity(b, new(1, 0), 0); s.Simulate(0.01f, Vector2.Zero);
        Check(!Read(s, b).Sleeping, "Zero delay never sleeps a moving body.");
        s.SetVelocity(b, Vector2.Zero, 0); s.Simulate(0.01f, Vector2.Zero); Check(Read(s, b).Sleeping, "Zero delay sleeps an eligible body.");
        var settings = s.GetSleepSettings(); Reject<ArgumentOutOfRangeException>(() => s.SetSleepSettings(new(float.NaN, 1, 1)));
        Check(s.GetSleepSettings() == settings, "Rejected settings preserve the usable world.");
        var stationary = Add(s, new(20, 0), mode: Mode.Static); s.SetSleeping(stationary, true);
        Check(!Read(s, stationary).Sleeping, "Static roles do not expose dynamic sleep.");
    }
    private static void VerifyIslands()
    {
        using var s = new Store(); s.SetSleepSettings(new(2, 0.1f, 0.05f));
        using var circle = new CircleShape { Radius = 1 };
        const int count = 257; var bodies = new Store.BodyHandle[count]; var states = new Store.Snapshot[count];
        for (var i = count - 1; i >= 0; i--) { bodies[i] = Add(s, new(2 * i, 0)); s.AddShape(bodies[i], circle); }
        var other = Add(s, new(1000, 0)); s.AddShape(other, circle);
        Settle(s); s.Read(bodies, states); foreach (var state in states) Check(state.Sleeping, "Every connected contact body sleeps.");
        s.SetVelocity(bodies[0], Vector2.Zero, 0); s.SolveConstraints(0.01f); s.Read(bodies, states);
        foreach (var state in states) Check(!state.Sleeping, "Wake crosses a reversed 257-body chain without a fixed iteration cutoff.");
        Check(Read(s, other).Sleeping, "Disconnected body stays asleep.");
        Settle(s); var middle = bodies[count / 2]; s.Remove(middle);
        var replacement = Add(s, new(2000, 0), sleep: true); s.AddShape(replacement, circle);
        s.SolveConstraints(0.01f);
        Check(!Read(s, bodies[0]).Sleeping && !Read(s, bodies[^1]).Sleeping, "Removal wakes both former component halves.");
        Check(Read(s, replacement).Sleeping, "Generation reuse does not apply an old island wake to the new body.");
        Check(Read(s, other).Sleeping, "Removal does not wake unrelated components.");
    }
    private static void VerifySupports()
    {
        using var s = new Store(); s.SetSleepSettings(new(2, 0.15f, 0.05f));
        using var circle = new CircleShape { Radius = 2 }; using var floor = new RectangleShape { Size = new(100, 2) };
        var ground = Add(s, new(0, 3), mode: Mode.Static); var support = s.AddShape(ground, floor);
        var a = Add(s, new(-20, 0)); var b = Add(s, new(20, 0)); s.AddShape(a, circle); s.AddShape(b, circle);
        Settle(s, gravity: new(0, 980)); Check(Read(s, a).Sleeping && Read(s, b).Sleeping, "Supported bodies settle under gravity.");
        var pose = Read(s, b).Pose;
        s.SetVelocity(a, Vector2.Zero, 0); s.SolveConstraints(0.01f);
        Check(!Read(s, a).Sleeping && Read(s, b).Sleeping, "Static floor does not join independent contact islands.");
        Settle(s, gravity: new(0, 980)); s.RemoveShape(support); s.Simulate(0.01f, new(0, 980), substeps: 1);
        Check(!Read(s, b).Sleeping && Read(s, b).Velocity.Y > 9.7f && Read(s, b).Pose != pose, "Removing support wakes the old contact and integrates gravity immediately.");
        s.AddShape(ground, floor); s.SetPose(a, new(-20, 0), 0); s.SetVelocity(a, Vector2.Zero, 0);
        s.SetPose(b, new(20, 0), 0); s.SetVelocity(b, Vector2.Zero, 0); Settle(s, gravity: new(0, 980));
        s.SetVelocity(ground, new(20, 0), 0); s.Simulate(0.01f, new(0, 980), substeps: 1);
        Check(!Read(s, a).Sleeping && Read(s, a).Velocity.X > 0, "A moving stationary surface wakes and carries sleepers.");
        Check(Read(s, ground).Position == new Vector2(0, 3), "Virtual surface motion never moves static pose.");
        s.SetVelocity(ground, Vector2.Zero, 0); Settle(s, 100, new(0, 980));
        s.Simulate(0.01f, new(0, -980), substeps: 1); Check(!Read(s, a).Sleeping && Read(s, a).Velocity.Y < 0, "Changed gravity wakes sleeping dynamics.");
        using var isolated = new Store(); isolated.SetSleepSettings(new(2, 0.1f, 0.05f));
        var sensor = Add(isolated); isolated.AddShape(sensor, circle, sensor: true);
        var solid = Add(isolated); isolated.AddShape(solid, circle); Settle(isolated);
        isolated.SetVelocity(sensor, Vector2.Zero, 0); isolated.SolveConstraints(0.01f);
        Check(Read(isolated, solid).Sleeping, "Sensor overlap does not couple sleep islands.");
        isolated.Remove(sensor); isolated.Remove(solid);
        var moved = Add(isolated, new(0, 0)); isolated.AddShape(moved, circle);
        var neighbour = Add(isolated, new(4, 0)); isolated.AddShape(neighbour, circle); Settle(isolated);
        isolated.SetPose(moved, new(100, 0), 0); isolated.SetSleeping(moved, true); isolated.Simulate(0.01f, Vector2.Zero, substeps: 1);
        Check(Read(isolated, moved).Sleeping && !Read(isolated, neighbour).Sleeping,
            "Explicit sleep after teleport preserves old-neighbour invalidation without waking the now-isolated target.");
    }
    private static void VerifyImpactAndResources()
    {
        using var s = new Store(); s.SetSleepSettings(new(2, 0.1f, 0.05f));
        using var circle = new CircleShape { Radius = 1 };
        var sleeper = Add(s, sleep: true); s.AddShape(sleeper, circle, friction: 0);
        var moving = Add(s, new(-2, 0)); s.AddShape(moving, circle, friction: 0); s.SetVelocity(moving, new(10, 0), 0);
        s.Simulate(0.01f, Vector2.Zero, substeps: 1, margin: 0);
        Near(Read(s, sleeper).Velocity.X, 5, 0.001f, "Contact wakes before mass/Jacobian preparation");
        Near(Read(s, moving).Velocity.X, 5, 0.001f, "Wake impact preserves pair momentum");
        s.SetVelocity(moving, Vector2.Zero, 0); s.SetVelocity(sleeper, Vector2.Zero, 0); Settle(s);
        Check(s.ActiveSimulationBodyCount == 0, "The device reports a fully inactive world.");
        circle.Radius = 1.1f; s.Simulate(0.01f, Vector2.Zero, substeps: 1);
        Check(!Read(s, sleeper).Sleeping && !Read(s, moving).Sleeping, "Resource geometry revision invalidates the idle-world skip.");
        Settle(s, 200); circle.Dispose(); s.Simulate(0.01f, Vector2.Zero, substeps: 1);
        Check(!Read(s, sleeper).Sleeping, "Disposed shape invalidates sleeping support and cached activity.");
        Settle(s); s.SetSleepSettings(new(1, 0.2f, 0.1f)); s.Simulate(0.01f, Vector2.Zero);
        Check(!Read(s, sleeper).Sleeping, "Changing sleep thresholds invalidates the idle-world skip.");
        Settle(s); s.Simulate(0.01f, Vector2.Zero, allowedPenetration: 0.01f);
        Check(!Read(s, sleeper).Sleeping, "Changing contact/solver policy invalidates the idle-world skip.");
    }
    private static void VerifyJoints()
    {
        using var s = new Store(); s.SetSleepSettings(new(2, 0.1f, 0.05f));
        const int count = 129; var bodies = new Store.BodyHandle[count]; var states = new Store.Snapshot[count];
        for (var i = 0; i < count; i++) bodies[i] = Add(s);
        for (var i = 1; i < count; i++) s.AddJoint(new(Kind.Pin, bodies[i - 1], bodies[i], Transform.Identity, Transform.Identity));
        Settle(s); s.Read(bodies, states); foreach (var state in states) Check(state.Sleeping, "Joint-only component sleeps.");
        s.SetCanSleep(bodies[^1], false); s.SolveConstraints(0.01f); s.Read(bodies, states);
        foreach (var state in states) Check(!state.Sleeping, "Joint wake propagates to all endpoints.");
        Settle(s); s.Read(bodies, states); foreach (var state in states) Check(!state.Sleeping, "One can-sleep=false member keeps the connected component awake.");
        using var springWorld = new Store(); springWorld.SetSleepSettings(new(2, 0.1f, 0.05f));
        var a = Add(springWorld, sleep: true); var b = Add(springWorld, new(12, 0), sleep: true);
        springWorld.AddJoint(new(Kind.DampedSpring, a, b, Transform.Identity, Transform.Identity) { RestLength = 10, Stiffness = 20, Damping = 0 });
        springWorld.SolveConstraints(0.01f); Near(Read(springWorld, a).Velocity.X, 0.4f, 0.001f, "An extended spring wakes and applies its impulse");
    }
    private static void VerifyResidency(int side)
    {
        using var s = new Store(); s.SetSleepSettings(new(2, 0.1f, 0.05f));
        using var circle = new CircleShape { Radius = 1 };
        var count = side * side; const int warmup = 128, samples = 128;
        var bodies = new Store.BodyHandle[count]; var states = new Store.Snapshot[count];
        for (var i = 0; i < count; i++) { bodies[i] = Add(s, new(i % side * 2, i / side * 2)); s.AddShape(bodies[i], circle); }
        Settle(s, warmup); s.Read(bodies, states); foreach (var state in states) Check(state.Sleeping, "Resident population reaches sleep.");
        var upload = s.UploadBytes; var readback = s.ReadbackBytes; var wait = s.WaitMS;
        var times = new double[samples]; var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < samples; i++)
        {
            var start = Stopwatch.GetTimestamp();
            for (var repeat = 0; repeat < 256; repeat++) s.Simulate(1f / 120, Vector2.Zero);
            times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds / 256;
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Array.Sort(times);
        Check(allocated == 0, "Warm connected sleeping steps allocate no managed bytes.");
        Check(s.ActiveSimulationBodyCount == 0 && s.UploadBytes == upload && s.ReadbackBytes == readback && s.WaitMS == wait,
            "A device-confirmed unchanged sleeping world performs no transfer or wait.");
        Console.WriteLine($"Resident sleep: {count} connected circles, {warmup} warmup/{samples} batches of 256 inactive calls, 4 substeps/16 iterations; p50={times[samples / 2]:F6}, p95={times[(int)(samples * 0.95)]:F6}, wait={(s.WaitMS - wait) / samples:F4} ms; {allocated} managed B/tick, upload={(s.UploadBytes - upload) / samples}, readback={(s.ReadbackBytes - readback) / samples} B/tick; {s.Driver}, {s.DeviceName}.");
        s.Read(bodies, states); foreach (var state in states) Check(state.Sleeping && state.Velocity == System.Numerics.Vector4.Zero, "Sleeping states stay resident and motionless.");
        for (var i = 0; i < 16; i++) { s.SetVelocity(bodies[i], Vector2.Zero, 0); Settle(s, 8); }
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 16; i++) { s.SetVelocity(bodies[i], Vector2.Zero, 0); Settle(s, 8); }
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "Warm repeated island wake/sleep allocates no managed bytes.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Near(float value, float expected, float tolerance, string message) => Check(MathF.Abs(value - expected) <= tolerance, $"{message}: {value} vs {expected}");
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
