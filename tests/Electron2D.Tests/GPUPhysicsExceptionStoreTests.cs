using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class GPUPhysicsExceptionStoreTests
{
    internal static void Run()
    {
        VerifyContributions(); VerifyLifetime(); VerifyResponse(); VerifySleep(); VerifyCCD(); VerifyResidency();
        Console.WriteLine("Resident exceptions: directional ownership, joint contributions, sensors, lifetime, sleep, CCD and warmed residency passed.");
    }
    private static Store.BodyHandle Body(Store s, Vector2 at = default, Mode mode = Mode.Rigid, bool sleep = false) =>
        s.Add(new(mode, at, 0, Vector2.Zero, 0, CanSleep: sleep));
    private static Store.Snapshot Read(Store s, Store.BodyHandle body)
    { Span<Store.Snapshot> value = stackalloc Store.Snapshot[1]; s.Read([body], value); return value[0]; }
    private static void VerifyContributions()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 1 };
        var a = Body(s); var b = Body(s, new(1.8f, 0));
        s.AddShape(a, circle); s.AddShape(b, circle);
        Check(s.FindPairs() == 1 && s.FindContacts() == 1, "Unfiltered pair and manifold exist.");
        s.SetCollisionException(a, b, true);
        Check(s.HasCollisionException(a, b) && !s.HasCollisionException(b, a) && s.FindContacts() == 0, "Unilateral authoring suppresses both contact directions.");
        var submissions = s.SubmissionCount; var uploads = s.ExceptionUploadBytes;
        s.SetCollisionException(a, b, true); s.SetCollisionException(b, a, false); s.FindContacts();
        Check(s.SubmissionCount == submissions && s.ExceptionUploadBytes == uploads, "Idempotent edits send no device work.");
        s.SetCollisionException(b, a, true); s.SetCollisionException(a, b, false);
        Check(s.FindPairs() == 0, "Reverse entry keeps the pair excluded.");
        var d = new Store.JointDefinition(PhysicsServer.JointType.Pin, a, b, Transform.Identity, Transform.Identity);
        var j = s.AddJoint(d); var other = s.AddJoint(d);
        s.SetCollisionException(b, a, false); s.RemoveJoint(j);
        Check(s.FindPairs() == 0, "One remaining joint independently suppresses contact.");
        s.SetCollisionException(a, b, true); s.SetJoint(other, d with { DisableCollision = false });
        Check(s.FindPairs() == 0, "Joint opt-in cannot erase an explicit exception.");
        s.SetCollisionException(a, b, false);
        Check(s.FindContacts() == 1, "Last veto removal restores an already overlapping pair.");
        s.SetJoint(other, d); s.SetCollisionException(a, b, true);
        var sensor = s.AddShape(a, circle, layer: 4, mask: 1, sensor: true);
        Check(s.FindPairs() == 1, "Area sensing keeps its directional mask despite all body vetoes.");
        s.SetShapeFilter(sensor, 4, 0, true);
        Check(s.FindPairs() == 0, "A sensor with no receiver mask does not gain a pair from exceptions.");
        s.SetCollisionException(a, a, true); s.SetCollisionException(a, a, false);
        Check(s.CollisionExceptionCount == 1, "Self entries can be removed without unlinking other edges.");
    }
    private static void VerifyLifetime()
    {
        using var s = new Store(); using var foreign = new Store(); using var circle = new CircleShape { Radius = 1 };
        var a = Body(s); var b = Body(s, new(1.8f, 0)); s.AddShape(a, circle); s.AddShape(b, circle);
        var outsiders = new Store.BodyHandle[130];
        for (var i = 0; i < outsiders.Length; i++)
        {
            outsiders[i] = Body(s, new(100 + 4 * i, 0));
            s.SetCollisionException(outsiders[i], b, true); s.SetCollisionException(b, outsiders[i], true);
        }
        s.SetCollisionException(a, b, true); s.SetCollisionException(b, b, true);
        Check(s.FindPairs() == 0 && s.CollisionExceptionCount == 262, "Filter storage grows without losing directed entries.");
        s.Remove(b); b = Body(s, new(1.8f, 0)); s.AddShape(b, circle);
        Check(s.CollisionExceptionCount == 0 && s.FindContacts() == 1, "Removing a target clears incoming/outgoing device edges before a body slot is reused.");
        var stale = b;
        s.SetCollisionException(a, b, true); s.Remove(b); b = Body(s, new(1.8f, 0)); s.AddShape(b, circle);
        s.SetCollisionException(b, a, true); s.SetCollisionException(b, a, false);
        Check(s.FindContacts() == 1, "Pending delete/reuse/coalesced edits cannot exclude a new generation.");
        Reject<ArgumentException>(() => s.SetCollisionException(a, stale, true));
        var f = Body(foreign); Reject<ArgumentException>(() => s.SetCollisionException(a, f, true));
        Reject<InvalidOperationException>(() => Task.Run(() => s.SetCollisionException(a, b, true)).GetAwaiter().GetResult());
        Check(s.CollisionExceptionCount == 0 && s.FindContacts() == 1, "Invalid and foreign-thread edits leave the valid world intact.");
        s.SetCollisionException(a, b, true); s.Remove(a);
        Check(s.CollisionExceptionCount == 0, "Owner removal releases its remaining directed edge.");
        s.Dispose(); Reject<ObjectDisposedException>(() => s.SetCollisionException(b, b, true));
    }
    private static void VerifyResponse()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 1 };
        var a = Body(s); var b = Body(s, new(1.8f, 0), Mode.Static);
        s.AddShape(a, circle, friction: 0); s.AddShape(b, circle, friction: 0);
        s.SetVelocity(a, new(10, 0), 0); s.SolveConstraints(0.01f, maxCorrectionSpeed: 0);
        Near(Read(s, a).Velocity.X, 0, 0.001f, "Ordinary contact stops normal velocity");
        s.SetCollisionException(b, a, true); s.SetVelocity(a, new(10, 0), 0); s.SolveConstraints(0.01f, maxCorrectionSpeed: 0);
        Near(Read(s, a).Velocity.X, 10, 0.001f, "Explicit veto removes physical impulse");
        Check(s.ContactPointCount == 0 && s.WarmStartedPointCount == 0, "Filtered contacts do not keep solver rows or warm impulses.");
        s.SetCollisionException(b, a, false); s.SolveConstraints(0.01f, maxCorrectionSpeed: 0);
        Near(Read(s, a).Velocity.X, 0, 0.001f, "Restored contact responds again");
        Check(s.WarmStartedPointCount == 0, "A newly restored pair does not reuse the removed contact's history.");
    }
    private static void VerifySleep()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 1 };
        var a = Body(s, sleep: true); var b = Body(s, new(1.8f, 0), sleep: true); var c = Body(s, new(3.6f, 0), sleep: true);
        foreach (var body in new[] { a, b, c }) s.AddShape(body, circle);
        s.SetSleepSettings(new(2, 0.1f, 0.02f));
        for (var i = 0; i < 8; i++) s.Simulate(0.01f, Vector2.Zero, substeps: 1, margin: 0, maxCorrectionSpeed: 0);
        Check(s.ActiveSimulationBodyCount == 0, "Connected quiet bodies sleep before editing the filter.");
        var submissions = s.SubmissionCount; s.Simulate(0.001f, Vector2.Zero, substeps: 1, margin: 0, maxCorrectionSpeed: 0);
        Check(s.SubmissionCount == submissions, "Unchanged sleeping world skips submission.");
        s.SetCollisionException(a, b, true); s.Simulate(0.001f, Vector2.Zero, substeps: 1, margin: 0, maxCorrectionSpeed: 0);
        Check(!Read(s, a).Sleeping && !Read(s, b).Sleeping && !Read(s, c).Sleeping, "Filter edit wakes endpoints and prior connected neighbours on device.");
        Check(s.ContactPointCount == 1, "Only the remaining connected pair survives the filter edit.");
    }
    private static void VerifyCCD()
    {
        using var circle = new CircleShape { Radius = 1 }; using var wall = new RectangleShape { Size = new(0.2f, 100) };
        foreach (var mode in new[] { Store.CCDMode.CastRay, Store.CCDMode.CastShape })
        {
            using var s = new Store();
            var a = Body(s); var b = Body(s, new(20, 0), Mode.Static);
            s.AddShape(a, circle, friction: 0); s.AddShape(b, wall, friction: 0);
            s.SetCCDMode(a, mode); s.SetVelocity(a, new(3000, 0), 0); s.SetCollisionException(b, a, true);
            s.Simulate(0.02f, Vector2.Zero, substeps: 1, margin: 0);
            Near(Read(s, a).Position.X, 60, 0.002f, "CCD skips excepted obstacle");
            s.SetPose(a, Vector2.Zero, 0); s.SetVelocity(a, new(3000, 0), 0); s.SetCollisionException(b, a, false);
            s.Simulate(0.02f, Vector2.Zero, substeps: 1, iterations: 32, margin: 0);
            Check(Read(s, a).Position.X < 20 && MathF.Abs(Read(s, a).Velocity.X) < 0.1f, "Removing an exception restores a thin-wall CCD impact.");
        }
    }
    private static void VerifyResidency()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 1 };
        const int count = 4096; var bodies = new Store.BodyHandle[2 * count];
        for (var i = 0; i < count; i++)
        {
            var at = new Vector2(i % 64 * 8, i / 64 * 8);
            bodies[2 * i] = Body(s, at, Mode.Static); bodies[2 * i + 1] = Body(s, at + new Vector2(1.8f, 0));
            s.AddShape(bodies[2 * i], circle); s.AddShape(bodies[2 * i + 1], circle);
            s.SetCollisionException(bodies[2 * i], bodies[2 * i + 1], true);
        }
        Check(s.FindContacts() == 0, "All 4096 authored vetoes survive GPU hash construction.");
        for (var i = 0; i < count; i += 2) s.SetCollisionException(bodies[2 * i], bodies[2 * i + 1], false);
        Check(s.FindContacts() == count / 2, "Removing half retains the exact unfiltered manifold population.");
        for (var i = 0; i < count; i += 2) s.SetCollisionException(bodies[2 * i], bodies[2 * i + 1], true);
        // One sparse edit per tick must not resend all authored pairs or download any filter table.
        void Tick(int i)
        {
            s.SetCollisionException(bodies[0], bodies[1], (i & 1) == 0);
            s.SetPose(bodies[1], new(1.8f, 0), 0); s.SetVelocity(bodies[1], Vector2.Zero, 0);
            s.Simulate(1f / 60, Vector2.Zero, substeps: 1, iterations: 4, maxCorrectionSpeed: 0);
        }
        for (var i = 0; i < 128; i++) Tick(i);
        var samples = new double[128]; var allocated = GC.GetAllocatedBytesForCurrentThread();
        var upload = s.UploadBytes; var read = s.ReadbackBytes; var uniform = s.UniformBytes; var wait = s.WaitMS; var edits = s.ExceptionUploadBytes; var filterMS = s.FilterMS; var filterWait = s.FilterWaitMS;
        for (var i = 0; i < samples.Length; i++)
        {
            var start = Stopwatch.GetTimestamp(); Tick(i); samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Check(allocated == 0 && s.ExceptionUploadBytes - edits == 32 * samples.Length, "Warmed actual filter edits allocate zero and upload exactly one 32-byte edit per tick.");
        Array.Sort(samples);
        Console.WriteLine($"Resident exceptions: {count * 2} bodies/{count} entries, 128 warmup/128 samples, 1 substep/4 iterations; p50={samples[64]:F4}, p95={samples[121]:F4}, p99={samples[126]:F4} ms, wait={(s.WaitMS - wait) / 128:F4} ms, filter={(s.FilterMS - filterMS) / 128:F4} ms/filter wait={(s.FilterWaitMS - filterWait) / 128:F4} ms, {allocated} managed B, upload={(s.UploadBytes - upload) / 128}, readback={(s.ReadbackBytes - read) / 128}, uniforms={(s.UniformBytes - uniform) / 128} B/tick; {s.Driver}, {s.DeviceName}.");
        var stable = s.ExceptionUploadBytes; var filters = s.FilterSubmissionCount;
        for (var i = 0; i < 8; i++) s.Simulate(1f / 60, Vector2.Zero, substeps: 1, iterations: 4, maxCorrectionSpeed: 0);
        Check(s.ExceptionUploadBytes == stable && s.FilterSubmissionCount == filters, "Unchanged filters remain resident across active steps.");
    }
    private static void Near(float actual, float expected, float tolerance, string message) => Check(float.IsFinite(actual) && MathF.Abs(actual - expected) <= tolerance, $"{message}: {actual} vs {expected}");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
