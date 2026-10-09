using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class GPUPhysicsReportTests
{
    internal static void Run()
    {
        foreach (var steps in new[] { 1, 4, 8 }) { Momentum(steps); Transient(steps); }
        WarmHistory(); Rotating(); Selection(); Sleeping(); SurfaceAndSensors(); Proximity(); Lifetime(); Measure();
        Console.WriteLine("Resident contact reports: complete impulses, transient/CCD contacts, caps, snapshots, sleep, lifecycle and warmed allocation passed.");
    }
    private static Store.Snapshot State(Store s, Store.BodyHandle body)
    { Span<Store.Snapshot> result = stackalloc Store.Snapshot[1]; s.Read([body], result); return result[0]; }
    private static int Reports(Store s, Store.BodyHandle body, Span<Store.ContactReport> results)
    { Span<int> count = stackalloc int[1]; s.ReadContactReports([body], [results.Length], count, results); return count[0]; }
    private static Vector2 Sum(ReadOnlySpan<Store.ContactReport> reports)
    { var total = Vector2.Zero; foreach (var report in reports) total += report.Impulse; return total; }
    private static Vector2 Linear(Store.Snapshot body) => new(body.Velocity.X, body.Velocity.Y);
    private static Store.BodyHandle Add(Store s, Mode mode, Vector2 position, Vector2 velocity = default, float mass = 2, Store.CCDMode ccd = Store.CCDMode.Disabled) =>
        s.Add(new(mode, position, 0, velocity, 0, Mass: mass, CanSleep: false, LockRotation: true, ContinuousMode: ccd));
    private static void Momentum(int steps)
    {
        using var s = new Store { CaptureContactReports = true }; using var shape = new RectangleShape { Size = new(20, 20) };
        using var ground = new RectangleShape { Size = new(1000, 20) };
        var floor = Add(s, Mode.Static, new(0, 100)); var body = Add(s, Mode.Rigid, new(0, 80), new(120, 180));
        var fs = s.AddShape(floor, ground); var bs = s.AddShape(body, shape);
        Span<Store.ContactReport> reports = stackalloc Store.ContactReport[16];
        Check(Reports(s, body, reports) == 0, "No reports before first solver publication.");
        s.Simulate(1f / 60, Vector2.Zero, substeps: steps, iterations: 32);
        var count = Reports(s, body, reports); var state = State(s, body); var total = Sum(reports[..count]);
        Check(count > 0, "Impact produces reports."); Near(total, (Linear(state) - new Vector2(120, 180)) * 2, .02f, "Report includes complete normal and friction momentum");
        Check(total.X < -1 && total.Y < -1, "Both impulse channels contribute.");
        for (var i = 0; i < count; i++)
        {
            var r = reports[i]; Check(r.Shape == bs.Index && r.ShapeGeneration == bs.Generation && r.ColliderShape == fs.Index && r.ColliderBody == floor.Index, "Receiver/shape identities.");
            Near(r.Velocity, Linear(state), .001f, "Locked body's final point velocity"); Near(r.ColliderVelocity, Vector2.Zero, 0, "Static point velocity");
            Check(r.Normal.Y < -.99f, "Receiver normal faces from floor.");
        }
        var copy = reports[0]; s.SetPose(body, new(100, 100), 1); s.SetVelocity(body, new(999, 999), 4); s.Simulate(0, Vector2.Zero);
        Check(Reports(s, body, reports) == count && reports[0].Equals(copy), "Completed snapshot survives later writes and zero-time calls.");
        // Compare the same public CPU contract, without comparing backend feature/order layouts.
        var space = PhysicsServer.SpaceCreate(); var cpuFloor = PhysicsServer.BodyCreate(); var cpuBody = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.AreaSetGravity(space, 0); PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
            PhysicsServer.BodySetMode(cpuFloor, Mode.Static); PhysicsServer.BodySetTransform(cpuFloor, new(0, new(0, 100))); PhysicsServer.BodyAddShape(cpuFloor, ground.GetRID()); PhysicsServer.BodySetSpace(cpuFloor, space);
            PhysicsServer.BodySetMode(cpuBody, Mode.RigidLinear); PhysicsServer.BodySetMass(cpuBody, 2); PhysicsServer.BodySetCanSleep(cpuBody, false);
            PhysicsServer.BodySetMaxContactsReported(cpuBody, 16); PhysicsServer.BodySetTransform(cpuBody, new(0, new(0, 80))); PhysicsServer.BodyAddShape(cpuBody, shape.GetRID());
            PhysicsServer.BodySetLinearVelocity(cpuBody, new(120, 180)); PhysicsServer.BodySetSpace(cpuBody, space); PhysicsServer.SpaceStep(space, 1d / 60);
            var view = PhysicsServer.BodyGetDirectState(cpuBody)!; var cpuImpulse = Vector2.Zero;
            for (var i = 0; i < view.GetContactCount(); i++) cpuImpulse += view.GetContactImpulse(i);
            Near(cpuImpulse, (view.LinearVelocity - new Vector2(120, 180)) * 2, .08f, "Public CPU obeys the same full-frame momentum invariant");
            Check(cpuImpulse.X < -1 && cpuImpulse.Y < -1, "Public CPU exposes both impulse channels with the same receiver signs.");
        }
        finally { PhysicsServer.FreeRID(cpuBody); PhysicsServer.FreeRID(cpuFloor); PhysicsServer.FreeRID(space); }
    }
    private static void Transient(int steps)
    {
        using var s = new Store { CaptureContactReports = true }; using var shape = new CircleShape { Radius = 5 };
        var a = Add(s, Mode.Rigid, new(-50, 0), new(600, 0), ccd: Store.CCDMode.CastShape);
        var b = Add(s, Mode.Rigid, new(50, 0), new(-600, 0), mass: 3, ccd: Store.CCDMode.CastShape);
        s.AddShape(a, shape, friction: 0, bounce: 1); s.AddShape(b, shape, friction: 0, bounce: 1);
        s.Simulate(.1f, Vector2.Zero, substeps: steps, iterations: 32);
        Span<Store.ContactReport> reports = stackalloc Store.ContactReport[16]; Span<int> counts = stackalloc int[2];
        s.ReadContactReports([a, b], [8, 8], counts, reports);
        Check(counts[0] > 0 && counts[1] > 0, "CCD impact remains published after separation.");
        var first = Sum(reports[..counts[0]]); var second = Sum(reports.Slice(8, counts[1]));
        Near(first, (Linear(State(s, a)) - new Vector2(600, 0)) * 2, .03f, "CCD first momentum");
        Near(second, (Linear(State(s, b)) - new Vector2(-600, 0)) * 3, .03f, "CCD second momentum");
        Near(first + second, Vector2.Zero, .002f, "Both reporters do not double-count the same contact.");
        s.Simulate(.1f, Vector2.Zero, substeps: steps); Check(Reports(s, a, reports) == 0, "Next separated tick retires transient contacts.");
    }
    private static void WarmHistory()
    {
        using var s = new Store { CaptureContactReports = true };
        var a = Add(s, Mode.Rigid, Vector2.Zero, new(600, 0)); var b = Add(s, Mode.Rigid, Vector2.Zero, new(-600.7f, 0), 3);
        s.AddJoint(new(PhysicsServer.JointType.Pin, a, b, Transform.Identity, Transform.Identity));
        s.SolveConstraints(.000001f, iterations: 64); var first = Linear(State(s, a)); var second = Linear(State(s, b));
        s.SolveConstraints(.025f, iterations: 64);
        Near(Linear(State(s, a)), first, .003f, "Longer interval must not amplify and cancel a cached joint impact");
        Near(Linear(State(s, b)), second, .003f, "Unequal-mass joint retains velocity across interval growth");
        Span<Store.ContactReport> reports = stackalloc Store.ContactReport[4]; Check(Reports(s, a, reports) == 0, "Joint rows are not contact reports.");
    }
    private static void Rotating()
    {
        using var s = new Store { CaptureContactReports = true }; using var circle = new CircleShape { Radius = 10 }; using var shape = new RectangleShape { Size = new(200, 20) };
        var floor = Add(s, Mode.Kinematic, new(0, 100)); var body = Add(s, Mode.Rigid, new(40, 80), new(0, 40));
        s.AddShape(floor, shape); s.AddShape(body, circle); s.SetKinematicTarget(floor, new(0, 100), -.1f);
        s.Simulate(1f / 60, Vector2.Zero, substeps: 8, iterations: 32);
        Span<Store.ContactReport> reports = stackalloc Store.ContactReport[64]; var count = Reports(s, body, reports);
        Check(count > 0, "Rotating surface contacts."); Near(Sum(reports[..count]), (Linear(State(s, body)) - new Vector2(0, 40)) * 2, .04f, "World-vector accumulation preserves changing normals");
    }
    private static void Selection()
    {
        using var s = new Store { CaptureContactReports = true }; using var circle = new CircleShape { Radius = 10 };
        var b = Add(s, Mode.Rigid, Vector2.Zero);
        s.AddShape(b, circle);
        for (var i = 0; i < 6; i++) { var a = Add(s, Mode.Static, new(0, 15 + (i / 2) * .2f)); s.AddShape(a, circle); }
        s.SolveConstraints(.01f, iterations: 4);
        Span<Store.ContactReport> all = stackalloc Store.ContactReport[16]; var count = Reports(s, b, all); Check(count == 6, "Complete geometric population before cap.");
        Span<Store.ContactReport> selected = stackalloc Store.ContactReport[2]; var selectedCount = Reports(s, b, selected);
        Span<Store.ContactReport> expected = stackalloc Store.ContactReport[2]; var used = 0;
        for (var i = 0; i < count; i++)
        {
            var slot = used < 2 ? used++ : expected[1].Depth < expected[0].Depth ? 1 : 0;
            if (i < 2 || all[i].Depth > expected[slot].Depth) expected[slot] = all[i];
        }
        Check(selectedCount == 2 && selected[0].Equals(expected[0]) && selected[1].Equals(expected[1]), "Replace first shallowest only for strictly greater depth, in encounter order.");
        Check(Reports(s, b, Span<Store.ContactReport>.Empty) == 0, "Zero cap returns no points.");
    }
    private static void Sleeping()
    {
        using var s = new Store { CaptureContactReports = true }; using var box = new RectangleShape { Size = new(20, 20) };
        var a = Add(s, Mode.Static, new(0, 20)); var b = Add(s, Mode.Rigid, Vector2.Zero, new(0, 10)); s.AddShape(a, box); s.AddShape(b, box); s.SetCanSleep(b, true);
        s.Simulate(.01f, Vector2.Zero); s.SetSleeping(b, true); s.Simulate(.01f, Vector2.Zero);
        Span<Store.ContactReport> reports = stackalloc Store.ContactReport[16];
        Check(Reports(s, b, reports) > 0, "Sleeping pair retains contact geometry.");
        for (var step = 0; step < 3; step++)
        {
            s.Simulate(.01f, Vector2.Zero); var count = Reports(s, b, reports); Check(count > 0 && State(s, b).Sleeping, "Inactive fast path preserves sleeping reports.");
            Near(Sum(reports[..count]), Vector2.Zero, 0, "Inactive frames cannot repeat old impulses.");
        }
        s.CaptureContactReports = false; Check(Reports(s, b, reports) == 0, "Disabling capture clears publication.");
        s.CaptureContactReports = true; s.Simulate(.01f, Vector2.Zero); Check(Reports(s, b, reports) > 0, "Enabling capture in an inactive world refreshes geometry.");
    }
    private static void SurfaceAndSensors()
    {
        using var s = new Store { CaptureContactReports = true }; using var box = new RectangleShape { Size = new(20, 20) };
        var a = Add(s, Mode.Static, new(0, 20)); var b = Add(s, Mode.Rigid, Vector2.Zero); var shape = s.AddShape(a, box); s.AddShape(b, box);
        s.SetMassProfile(a, new(2, Center: new(3, 0))); s.SetVelocity(a, new(7, 0), 2); s.SolveConstraints(.01f);
        Span<Store.ContactReport> reports = stackalloc Store.ContactReport[8]; var count = Reports(s, b, reports); Check(count > 0, "Surface reports exist.");
        for (var i = 0; i < count; i++)
        {
            var radius = reports[i].ColliderPosition - new Vector2(3, 20);
            Near(reports[i].ColliderVelocity, new Vector2(7, 0) + new Vector2(-radius.Y, radius.X) * 2, .001f, "Collider point velocity includes virtual angular surface velocity about its COM");
        }
        s.SetShapeFilter(shape, 1, 1, true); s.Simulate(.01f, Vector2.Zero);
        Check(Reports(s, b, reports) == 0, "Sensors cannot replay previous solid impulse reports.");
        s.SetShapeFilter(shape, 1, 1, false); s.SetCollisionException(b, a, true); s.Simulate(.01f, Vector2.Zero);
        Check(Reports(s, b, reports) == 0, "Filtered pairs cannot publish old contacts.");
    }
    private static void Proximity()
    {
        using var s = new Store { CaptureContactReports = true }; using var circle = new CircleShape { Radius = 5 };
        var a = Add(s, Mode.Static, Vector2.Zero); var b = Add(s, Mode.Rigid, new(0, 11)); s.AddShape(a, circle); s.AddShape(b, circle);
        var space = PhysicsServer.SpaceCreate(); var cpuA = PhysicsServer.BodyCreate(); var cpuB = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.AreaSetGravity(space, 0);
            PhysicsServer.BodySetMode(cpuA, Mode.Static); PhysicsServer.BodyAddShape(cpuA, circle.GetRID()); PhysicsServer.BodySetSpace(cpuA, space);
            PhysicsServer.BodyAddShape(cpuB, circle.GetRID()); PhysicsServer.BodySetMaxContactsReported(cpuB, 4); PhysicsServer.BodySetSpace(cpuB, space);
            Span<Store.ContactReport> reports = stackalloc Store.ContactReport[4];
            foreach (var distance in new[] { 11f, 13f })
            {
                s.SetPose(b, new(0, distance), 0); s.Simulate(.01f, Vector2.Zero);
                PhysicsServer.BodySetTransform(cpuB, new(0, new(0, distance))); PhysicsServer.SpaceStep(space, .01);
                var expected = PhysicsServer.BodyGetDirectState(cpuB)!.GetContactCount();
                Check(expected == (distance == 11 ? 1 : 0) && Reports(s, b, reports) == expected, "Speculative contact geometry, including zero impulse, matches public CPU reporting.");
                if (expected > 0) Near(reports[0].Impulse, Vector2.Zero, 0, "Proximity without approach does not invent an impulse.");
            }
        }
        finally { PhysicsServer.FreeRID(cpuB); PhysicsServer.FreeRID(cpuA); PhysicsServer.FreeRID(space); }
    }
    private static void Lifetime()
    {
        using var s = new Store { CaptureContactReports = true }; using var other = new Store(); using var box = new RectangleShape { Size = new(10, 10) };
        var a = Add(s, Mode.Static, new(0, 10)); var b = Add(s, Mode.Rigid, Vector2.Zero); s.AddShape(a, box); s.AddShape(b, box); s.Simulate(.01f, Vector2.Zero);
        var foreign = Add(other, Mode.Rigid, Vector2.Zero);
        Reject<ArgumentException>(() => Reports(s, foreign, new Store.ContactReport[1]));
        Reject<ArgumentOutOfRangeException>(() => s.ReadContactReports([b], [4096], new int[1], new Store.ContactReport[4096]));
        Reject<ArgumentException>(() => s.ReadContactReports([b], [2], new int[1], new Store.ContactReport[1]));
        Reject<InvalidOperationException>(() => Task.Run(() => Reports(s, b, new Store.ContactReport[1])).GetAwaiter().GetResult());
        s.Remove(b); var reused = Add(s, Mode.Rigid, Vector2.Zero); Span<Store.ContactReport> reports = stackalloc Store.ContactReport[4];
        Check(Reports(s, reused, reports) == 0, "Reused slot does not inherit old publication.");
        Reject<ArgumentException>(() => Reports(s, b, new Store.ContactReport[1]));
        var added = reused; for (var i = 0; i < 130; i++) added = Add(s, Mode.Static, new(1000 + i, 0));
        Check(Reports(s, added, reports) == 0, "Post-publication growth does not read beyond old body-head capacity.");
        s.AddShape(reused, box); s.Simulate(.01f, Vector2.Zero); Check(Reports(s, reused, reports) > 0, "Reused body can publish new contacts.");
        s.Dispose(); Reject<ObjectDisposedException>(() => Reports(s, reused, new Store.ContactReport[1]));
    }
    private static void Measure()
    {
        using var s = new Store { CaptureContactReports = true }; using var box = new RectangleShape { Size = new(2, 2) };
        const int population = 1024; var bodies = new Store.BodyHandle[population]; var limits = new int[population]; Array.Fill(limits, 4);
        var counts = new int[population]; var reports = new Store.ContactReport[population * 4];
        for (var i = 0; i < population; i++)
        {
            var p = new Vector2(i % 32 * 10, i / 32 * 10); var floor = Add(s, Mode.Static, p + new Vector2(0, 2));
            bodies[i] = Add(s, Mode.Rigid, p); s.AddShape(floor, box); s.AddShape(bodies[i], box);
        }
        void Tick() => s.Simulate(1f / 120, new(0, 10), substeps: 4, iterations: 4);
        var baseline = new double[64]; s.CaptureContactReports = false;
        for (var i = 0; i < 96; i++) Tick();
        var baselineBytes = GC.GetAllocatedBytesForCurrentThread(); var baselineWait = s.WaitMS; var baselineUpload = s.UploadBytes; var baselineDownload = s.ReadbackBytes;
        for (var i = 0; i < baseline.Length; i++) { var start = Stopwatch.GetTimestamp(); Tick(); baseline[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        baselineBytes = GC.GetAllocatedBytesForCurrentThread() - baselineBytes; Check(baselineBytes == 0 && s.ReportPublicationCount == 0, "Disabled reporting adds no publication work.");
        Array.Sort(baseline);
        Console.WriteLine($"Resident reports baseline: same {population} active boxes + floors, capture disabled; 96 warmup/64 samples; p50/p95/p99={baseline[32]:F4}/{baseline[60]:F4}/{baseline[63]:F4} ms, wait={(s.WaitMS - baselineWait) / 64:F4} ms; upload={(s.UploadBytes - baselineUpload) / 64}, readback={(s.ReadbackBytes - baselineDownload) / 64} B/tick; {baselineBytes} managed B.");
        s.CaptureContactReports = true;
        for (var i = 0; i < 96; i++) { Tick(); s.ReadContactReports(bodies, limits, counts, reports); }
        var samples = new double[64]; var reads = new double[64]; var wait = s.WaitMS; var upload = s.UploadBytes; var download = s.ReadbackBytes;
        long readUploads = 0, readDownloads = 0; double readWaits = 0;
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < samples.Length; i++)
        {
            var start = Stopwatch.GetTimestamp(); Tick(); samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            var beforeUpload = s.UploadBytes; var beforeDownload = s.ReadbackBytes; var beforeWait = s.WaitMS;
            start = Stopwatch.GetTimestamp(); s.ReadContactReports(bodies, limits, counts, reports); reads[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            readUploads += s.UploadBytes - beforeUpload; readDownloads += s.ReadbackBytes - beforeDownload; readWaits += s.WaitMS - beforeWait;
        }
        bytes = GC.GetAllocatedBytesForCurrentThread() - bytes; Check(bytes == 0, "Warmed simulation/publication/readback allocate zero owner-thread managed bytes.");
        for (var i = 0; i < population; i++) { Check(counts[i] == 2, $"All requested bodies retain both contact points: receiver={i}, count={counts[i]}, first other={reports[i * 4].ColliderBody}, last other={reports[i * 4 + counts[i] - 1].ColliderBody}."); Near(Sum(reports.AsSpan(i * 4, counts[i])), new(0, -20f / 120), .003f, "No dropped population/impulses in batched read"); }
        Array.Sort(samples); Array.Sort(reads);
        Console.WriteLine($"Resident reports: {population} active boxes + floors, 4 substeps/4 iterations, 96 warmup/64 samples; tick p50/p95/p99={samples[32]:F4}/{samples[60]:F4}/{samples[63]:F4} ms; batch read p50/p95/p99={reads[32]:F4}/{reads[60]:F4}/{reads[63]:F4} ms; mean waits={(s.WaitMS - wait) / 64:F4} ms, upload={(s.UploadBytes - upload) / 64}, readback={(s.ReadbackBytes - download) / 64} B/tick+read; {bytes} managed B; {s.Driver}, {s.DeviceName}.");
        Console.WriteLine($"Resident report batch only: wait={readWaits / 64:F4} ms; upload={readUploads / 64}, readback={readDownloads / 64} B/read; retained device={s.ReportDeviceCapacityBytes}, transfer={s.ReportTransferCapacityBytes} B.");
    }
    private static void Near(Vector2 actual, Vector2 expected, float tolerance, string message) => Check(actual.IsFinite() && actual.DistanceTo(expected) <= tolerance, $"{message}: {actual} vs {expected}");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
