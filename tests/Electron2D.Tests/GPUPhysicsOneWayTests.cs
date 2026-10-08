using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class GPUPhysicsOneWayTests
{
    internal static void Run()
    {
        VerifyLatchAndEdits(); VerifyRolesAndMotion(); VerifyPiecesAndSensors(); VerifyCCD(); VerifyRotatingCCD(); VerifyResidency();
        Console.WriteLine("Resident one-way: latched sides, edits, pieces, sensors, moving owners, CCD and zero-allocation residency passed.");
    }
    private static Store.BodyHandle Body(Store s, Vector2 at = default, Mode mode = Mode.Rigid, float rotation = 0, float mass = 1) =>
        s.Add(new(mode, at, rotation, Vector2.Zero, 0, Mass: mass, CanSleep: false));
    private static Store.Snapshot Read(Store s, Store.BodyHandle b)
    { Span<Store.Snapshot> state = stackalloc Store.Snapshot[1]; s.Read([b], state); return state[0]; }
    private static void VerifyLatchAndEdits()
    {
        using var s = new Store(); using var floor = new SegmentShape { A = new(-10, 0), B = new(10, 0) }; using var ball = new CircleShape { Radius = 1 };
        var a = Body(s, mode: Mode.Static); var b = Body(s, new(0, 0.5f));
        var surface = s.AddShape(a, floor); var moverShape = s.AddShape(b, ball);
        Check(s.GetShapeOneWay(surface) == new Store.OneWaySettings(false, Vector2.Down, 1), "New shape has ordinary two-sided defaults.");
        s.SetShapeOneWay(surface, new(true, new(0, 100), 1));
        Check(s.GetShapeOneWay(surface).Direction == Vector2.Down && s.FindContacts() == 0 && s.OneWayPairCount == 1, "Pass-side decision is retained without publishing contact points.");
        s.SetPose(b, new(0, -0.5f), 0);
        Check(s.FindContacts() == 0 && s.OneWayPairCount == 1, "Crossing the surface while overlapping cannot flip a rejected decision.");
        s.SetPose(b, new(0, -4), 0); Check(s.FindContacts() == 0 && s.OneWayPairCount == 0, "Separation retires the side decision.");
        s.SetPose(b, new(0, -0.5f), 0); Check(s.FindContacts() == 1, "New approach from solid side creates a contact.");
        s.SetPose(b, new(0, 0.5f), 0); Check(s.FindContacts() == 1, "An accepted episode also remains latched until separation.");
        s.SetShapeOneWay(surface, new(true, Vector2.Down, 100)); Check(s.FindContacts() == 0, "Edited fixture policy discards its old decision; margin does not select a rigid contact side.");
        s.SetShapeOneWay(surface, new(false, Vector2.Down, 100)); Check(s.FindContacts() == 1, "Disabling one-way restores two-sided response.");
        s.SetShapeOneWay(surface, new(true, Vector2.Zero, 0)); Check(s.FindContacts() == 0, "Zero direction has no solid face.");
        var valid = s.GetShapeOneWay(surface);
        Reject<ArgumentOutOfRangeException>(() => s.SetShapeOneWay(surface, new(true, new(float.NaN, 0), 1)));
        Reject<ArgumentOutOfRangeException>(() => s.SetShapeOneWay(surface, new(true, Vector2.Down, -1)));
        Reject<InvalidOperationException>(() => Task.Run(() => s.SetShapeOneWay(surface, valid)).GetAwaiter().GetResult());
        Check(s.GetShapeOneWay(surface) == valid, "Invalid edits retain authored configuration.");
        s.SetShapeOneWay(surface, new(true, new(float.MaxValue, float.MaxValue), 1));
        Check(MathF.Abs(s.GetShapeOneWay(surface).Direction.Length() - 1) < 1e-6f, "Large finite directions normalize without overflow.");
        s.RemoveShape(surface); var replacement = s.AddShape(a, floor);
        Reject<ArgumentException>(() => s.GetShapeOneWay(surface));
        Check(!s.GetShapeOneWay(replacement).Enabled && s.FindContacts() == 1, "Shape reuse gets default policy and no inherited decision.");
        s.SetPose(b, new(0, -0.5f), 0);
        s.SetShapeOneWay(moverShape, new(true, Vector2.Down, 1)); Check(s.FindContacts() == 0, "The second shape uses the opposite normal sign.");
        s.SetShapeOneWay(moverShape, new(true, Vector2.Up, 1)); Check(s.FindContacts() == 1, "Second-shape solid side accepts the pair.");
        s.SetShapeOneWay(replacement, new(true, Vector2.Up, 1)); Check(s.FindContacts() == 0, "Either one-way shape can reject a dual-policy pair.");
        s.SetShapeOneWay(replacement, new(true, Vector2.Down, 1)); Check(s.FindContacts() == 1, "Both one-way shapes must accept.");
    }
    private static void VerifyRolesAndMotion()
    {
        using var floor = new SegmentShape { A = new(-100, 0), B = new(100, 0) }; using var ball = new CircleShape { Radius = 1 };
        foreach (var mode in new[] { Mode.Static, Mode.Kinematic, Mode.Rigid })
            foreach (var rotateBody in new[] { false, true })
            {
                using var s = new Store();
                var a = Body(s, mode: mode, rotation: rotateBody ? MathF.PI / 2 : 0, mass: 1000);
                var surface = s.AddShape(a, floor, rotateBody ? Transform.Identity : new Transform(MathF.PI / 2, Vector2.Zero));
                s.SetShapeOneWay(surface, new(true, Vector2.Down, 1));
                var b = Body(s, new(-5, 0)); s.AddShape(b, ball, friction: 0);
                s.SetVelocity(b, new(40, 0), 0);
                for (var i = 0; i < 60; i++) s.Simulate(1f / 120, Vector2.Zero, margin: 0);
                Check(Read(s, b).Position.X > 10, "Body and child rotation both rotate the pass-through side for every owner role.");
                s.SetPose(a, Vector2.Zero, rotateBody ? MathF.PI / 2 : 0); s.SetVelocity(a, Vector2.Zero, 0);
                s.SetPose(b, new(5, 0), 0); s.SetVelocity(b, new(-40, 0), 0);
                for (var i = 0; i < 60; i++) s.Simulate(1f / 120, Vector2.Zero, margin: 0);
                Check(Read(s, b).Position.X > 0.5f && Read(s, b).Position.X < 1.5f, "Static, kinematic and dynamic owners block their solid side.");
            }
        using var moving = new Store();
        var platform = Body(moving, mode: Mode.Kinematic); var shape = moving.AddShape(platform, floor);
        moving.SetShapeOneWay(shape, new(true, Vector2.Down, 1));
        var rider = Body(moving, new(0, -1)); moving.AddShape(rider, ball);
        moving.SetVelocity(platform, new(0, -10), 0);
        for (var i = 0; i < 30; i++) moving.Simulate(1f / 120, Vector2.Zero);
        Check(Read(moving, rider).Position.Y < -3 && Read(moving, rider).Velocity.Y < -9, "Moving one-way platform pushes an already supported rider.");
    }
    private static void VerifyPiecesAndSensors()
    {
        using var s = new Store(); using var edges = new ConcavePolygonShape();
        edges.Segments = [new(-10, 0), new(10, 0), new(-10, 1), new(10, 1)];
        using var ball = new CircleShape { Radius = 1 };
        var a = Body(s, mode: Mode.Static); var surface = s.AddShape(a, edges); s.SetShapeOneWay(surface, new(true, Vector2.Down, 1));
        var b = Body(s, new(0, 0.5f)); s.AddShape(b, ball);
        var contacts = new Store.ContactPoint[4];
        Check(s.ReadContacts(contacts) == 1 && s.OneWayPairCount == 2 && contacts[0].PieceA == 1, "Distinct concave pieces retain different side decisions.");
        s.SetPose(b, new(0, -0.5f), 0); Check(s.FindContacts() == 0 && s.OneWayPairCount == 1, "Rejected piece stays rejected as the accepted piece separates.");
        edges.Segments = [new(-10, 0), new(10, 0)];
        Check(s.FindContacts() == 1, "Shared geometry edit invalidates prior piece decisions.");
        s.SetShapeFilter(surface, 1, uint.MaxValue, true); s.SetShapeOneWay(surface, new(true, Vector2.Zero, 1));
        Check(s.FindContacts() == 1 && s.OneWayPairCount == 0, "Sensors ignore one-way flags and keep overlap points.");
        s.SetShapeFilter(surface, 1, uint.MaxValue, false); s.SetShapeOneWay(surface, new(true, Vector2.Down, 1));
        s.SetCollisionException(b, a, true); Check(s.FindContacts() == 0 && s.OneWayPairCount == 0, "Explicit body vetoes retire one-way episodes too.");
        s.SetCollisionException(b, a, false); Check(s.FindContacts() == 1, "Removing explicit veto recreates the appropriate side.");
    }
    private static void VerifyCCD()
    {
        using var floor = new RectangleShape { Size = new(100, 0.2f) }; using var ball = new CircleShape { Radius = 1 };
        foreach (var ccd in new[] { Store.CCDMode.CastRay, Store.CCDMode.CastShape })
        {
            using var s = new Store(); var a = Body(s, mode: Mode.Static); var surface = s.AddShape(a, floor);
            s.SetShapeOneWay(surface, new(true, Vector2.Down, 1));
            var b = Body(s, new(0, 10)); s.AddShape(b, ball); s.SetCCDMode(b, ccd); s.SetVelocity(b, new(0, -3000), 0);
            s.Simulate(0.01f, Vector2.Zero, substeps: 1, margin: 0);
            Near(Read(s, b).Position.Y, -20, 0.002f, "CCD permits the complete pass-through trajectory");
            s.SetPose(b, new(0, -10), 0); s.SetVelocity(b, new(0, 3000), 0);
            s.Simulate(0.01f, Vector2.Zero, substeps: 1, margin: 0);
            Check(Read(s, b).Position.Y < 0 && MathF.Abs(Read(s, b).Velocity.Y) < 0.1f, "CCD stops at a thin platform's solid side.");
        }
    }
    private static void VerifyRotatingCCD()
    {
        foreach (var margin in new[] { 0f, 2f })
        {
            using var s = new Store(); using var beam = new RectangleShape { Size = new(20, 0.2f) }; using var ball = new CircleShape { Radius = 1 };
            var platform = Body(s, mode: Mode.Kinematic); var surface = s.AddShape(platform, beam, friction: 0);
            s.SetShapeOneWay(surface, new(true, Vector2.Down, 1));
            var b = Body(s, new(5, 0.5f)); s.AddShape(b, ball, friction: 0); s.SetCCDMode(b, Store.CCDMode.CastShape);
            Check(s.FindContacts(margin) == 0 && s.OneWayPairCount == 1, "Rotating CCD starts in a rejected overlap.");
            s.SetVelocity(platform, Vector2.Zero, MathF.PI / 0.05f);
            s.Simulate(0.05f, Vector2.Zero, substeps: 1, iterations: 32, margin: margin);
            var state = Read(s, b);
            Console.WriteLine($"Rotating one-way CCD: margin={margin}, velocity={state.Velocity}, intervals={s.CCDIntervalCount}.");
            Check(new Vector2(state.Velocity.X, state.Velocity.Y).Length() > 1 && s.CCDIntervalCount > 0,
                "A pass-through episode can separate and become a solid-side impact during one rotating interval.");
        }
    }
    private static void VerifyResidency()
    {
        using var s = new Store(); using var floor = new SegmentShape { A = new(-3, 0), B = new(3, 0) }; using var ball = new CircleShape { Radius = 1 };
        const int count = 4096; var movers = new Store.BodyHandle[count];
        for (var i = 0; i < count; i++)
        {
            var at = new Vector2(i % 64 * 10, i / 64 * 10);
            var platform = Body(s, at, Mode.Static); var shape = s.AddShape(platform, floor); s.SetShapeOneWay(shape, new(true, Vector2.Down, 1));
            movers[i] = Body(s, at + new Vector2(0, i % 2 == 0 ? 0.5f : -0.5f)); s.AddShape(movers[i], ball);
        }
        Check(s.FindContacts() == count / 2 && s.OneWayPairCount == count, "Complete population includes accepted and rejected episodes after capacity recovery.");
        void Tick() => s.Simulate(1f / 60, Vector2.Zero, substeps: 1, iterations: 4, margin: 0, maxCorrectionSpeed: 0);
        for (var i = 0; i < 128; i++) Tick();
        var samples = new double[128]; var bytes = GC.GetAllocatedBytesForCurrentThread(); var upload = s.UploadBytes; var readback = s.ReadbackBytes; var wait = s.WaitMS;
        for (var i = 0; i < samples.Length; i++) { var start = Stopwatch.GetTimestamp(); Tick(); samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        Check(bytes == 0 && s.OneWayPairCount == count && s.ContactPointCount == count / 2, "Warmed one-way history stays resident with complete counts and no managed allocations.");
        Array.Sort(samples);
        Console.WriteLine($"Resident one-way: {count * 2} bodies/{count} episodes, 128 warmup/128 samples, 1 substep/4 iterations; p50={samples[64]:F4}, p95={samples[121]:F4}, p99={samples[126]:F4} ms, wait={(s.WaitMS - wait) / 128:F4} ms; {bytes} managed B, upload={(s.UploadBytes - upload) / 128}, readback={(s.ReadbackBytes - readback) / 128} B/tick; {s.Driver}, {s.DeviceName}.");
    }
    private static void Near(float a, float b, float tolerance, string message) => Check(float.IsFinite(a) && MathF.Abs(a - b) <= tolerance, $"{message}: {a} vs {b}");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
