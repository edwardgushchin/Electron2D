using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;
using CCD = Electron2D.GPUPhysicsBodyStore.CCDMode;

internal static class GPUPhysicsCCDStoreTests
{
    internal static void Run()
    {
        VerifyModesAndImpact(); VerifyFamilies(); VerifyRayDifference(); VerifyMovingAndRotating(); VerifyPolicyAndForces(); VerifyFailure(); VerifyResidency();
        Console.WriteLine("Resident CCD: per-body modes, thin obstacles, impact response, full geometry/rotation, filters, force cadence and residency passed.");
    }
    private static Store.BodyHandle Body(Store s, Vector2 at = default, Vector2 speed = default, CCD ccd = CCD.Disabled, Mode mode = Mode.Rigid,
        float angular = 0, float inertia = 0, Vector2? center = null) =>
        s.Add(new(mode, at, 0, speed, angular, Inertia: inertia, CenterOfMass: center, CanSleep: false, ContinuousMode: ccd));
    private static Store.Snapshot Read(Store s, Store.BodyHandle b)
    { Span<Store.Snapshot> result = stackalloc Store.Snapshot[1]; s.Read([b], result); return result[0]; }
    private static void Tick(Store s, float delta = 0.02f) => s.Simulate(delta, Vector2.Zero, substeps: 1, iterations: 32, margin: 0, bounceThreshold: 0);
    private static void VerifyModesAndImpact()
    {
        using var circle = new CircleShape { Radius = 1 }; using var wall = new RectangleShape { Size = new(0.2f, 100) };
        foreach (var mode in new[] { CCD.Disabled, CCD.CastRay, CCD.CastShape })
        {
            using var s = new Store();
            var b = Body(s, speed: new(3000, 0), ccd: mode); var projectile = s.AddShape(b, circle, friction: 0, bounce: 0.5f);
            var boundary = Body(s, new(20, 0), mode: Mode.Static); s.AddShape(boundary, wall, friction: 0, bounce: 0.5f);
            Check(s.ReadShapeBounds(projectile, 0.02f) is { } bounds && bounds.End.X >= 60.99f && s.FindPairs(0.001f, 0.02f) == 1, "Swept bounds retain the complete thin-wall candidate.");
            Tick(s); var state = Read(s, b);
            Console.WriteLine($"CCD {mode}: position={state.Position}, velocity={state.Velocity}, queries={s.CCDQueryCount}, intervals={s.CCDIntervalCount}");
            Check(state.ContinuousMode == mode && s.GetCCDMode(b) == mode, "Authored and resident CCD modes agree.");
            if (mode == CCD.Disabled)
            { Near(state.Position.X, 60, 0.001f, "Disabled CCD leaves the discrete trajectory"); Check(s.CCDQueryCount == 0, "Disabled worlds run no CCD pipeline."); }
            else
            {
                Near(state.Velocity.X, -3000, 0.01f, "Continuous impact retains the incoming restitution speed");
                Near(state.Position.X, -22.2f, 0.02f, "Continuous bounce consumes the remaining interval");
                Check(s.CCDIntervalCount == 1, "One wall impact creates one continuous interval.");
            }
            if (mode == CCD.CastRay)
            {
                s.SetPose(b, Vector2.Zero, 0); s.SetVelocity(b, new(3000, 0), 1000); Tick(s);
                Near(Read(s, b).Velocity.X, -3000, 0.01f, "A spinning circle's leading ray follows translation");
            }
            s.SetCCDMode(b, CCD.CastShape); s.SetCanSleep(b, true); Check(Read(s, b).ContinuousMode == CCD.CastShape && Read(s, b).CanSleep, "CCD and sleep policy edits coexist in one command.");
            s.SetCanSleep(b, false); s.SetCCDMode(b, CCD.CastRay); Check(Read(s, b).ContinuousMode == CCD.CastRay && !Read(s, b).CanSleep, "Reversed policy-edit order retains both fields.");
            Reject<ArgumentOutOfRangeException>(() => s.SetCCDMode(b, (CCD)99));
            Check(s.GetCCDMode(b) == CCD.CastRay, "Invalid mode leaves configuration intact.");
            var queries = s.CCDQueryCount; s.SetCCDMode(b, CCD.Disabled); s.SetPose(b, Vector2.Zero, 0); s.SetVelocity(b, new(3000, 0), 0); Tick(s);
            Check(s.CCDQueryCount == queries && Read(s, b).Position.X > 59.99f, "Disabling the final CCD body restores the ordinary path.");
            s.Remove(b); Reject<ArgumentException>(() => s.SetCCDMode(b, CCD.Disabled));
            var replacement = Body(s); Check(Read(s, replacement).ContinuousMode == CCD.Disabled, "Reused slots do not inherit CCD policy.");
        }
    }
    private static void VerifyFamilies()
    {
        using var circle = new CircleShape { Radius = 1 };
        using var capsule = new CapsuleShape { Radius = 0.5f, Height = 5 };
        using var box = new RectangleShape { Size = new(2, 5) };
        using var segment = new SegmentShape { A = new(0, -2), B = new(0, 2) };
        using var polygon = new ConvexPolygonShape { Points = [new(-1, 0), new(0, -3), new(2, 0), new(0, 2)] };
        using var wall = new SegmentShape { A = new(0, -100), B = new(0, 100) };
        using var hollow = new ConcavePolygonShape { Segments = [new(0, -100), new(0, 0), new(0, 0), new(0, 100)] };
        foreach (var obstacle in new Shape[] { wall, hollow })
            foreach (var shape in new Shape[] { circle, capsule, box, segment, polygon, hollow })
            {
                if (shape == hollow && obstacle == hollow) continue; // The existing hollow/hollow collision exclusion remains unchanged.
                using var s = new Store();
                var b = Body(s, speed: new(3000, 0), ccd: CCD.CastShape); s.AddShape(b, shape, friction: 0);
                s.AddShape(Body(s, new(20, 0), mode: Mode.Static), obstacle, friction: 0);
                Tick(s); var state = Read(s, b);
                Console.WriteLine($"CCD family {shape.GetType().Name}/{obstacle.GetType().Name}: {state.Position}, {state.Velocity}, intervals={s.CCDIntervalCount}");
                Check(s.CCDIntervalCount > 0 && state.Position.IsFinite() && state.Position.X < 20.01f, $"{shape.GetType().Name} stops at a {obstacle.GetType().Name}.");
                for (var frame = 0; frame < 4; frame++) Tick(s);
                Check(Read(s, b).Position.X <= 20.0001f, "Repeated intervals do not carry residual closing velocity through a zero-thickness wall.");
            }
        using (var s = new Store())
        {
            var b = Body(s, new(0, -10), new(3000, 0), CCD.CastShape); s.AddShape(b, circle, new Transform(0, new(0, 10)), friction: 0);
            s.AddShape(Body(s, new(20, 0), mode: Mode.Static), wall, friction: 0); Tick(s);
            Near(Read(s, b).Position.X, 19, 0.01f, "Offset computed center shares the sweep/integration frame");
            // A two-micro-unit centroid conversion error times a 3000-unit impulse / inertia 0.5 allows 0.012 rad/s.
            Near(Read(s, b).Velocity.Z, 0, 0.02f, "Centered high-speed impulse respects the float centroid error bound");
        }
        using (var s = new Store())
        {
            using var ray = new SeparationRayShape { Length = 10 };
            var b = Body(s, new(0, -20), new(0, 3000), CCD.CastShape, Mode.RigidLinear); s.AddShape(b, ray, friction: 0);
            using var floor = new RectangleShape { Size = new(100, 0.2f) };
            s.AddShape(Body(s, mode: Mode.Static), floor, friction: 0); Tick(s);
            Check(s.CCDIntervalCount > 0 && Read(s, b).Position.Y < -9.9f, "Directed ray keeps its forward contact during a continuous crossing.");
        }
    }
    private static void VerifyRayDifference()
    {
        using var box = new RectangleShape { Size = new(2, 8) }; using var obstacle = new RectangleShape { Size = new(0.2f, 0.4f) };
        foreach (var mode in new[] { CCD.CastRay, CCD.CastShape })
        {
            using var s = new Store();
            var b = Body(s, speed: new(3000, 0), ccd: mode); s.AddShape(b, box, friction: 0);
            s.AddShape(Body(s, new(20, 3), mode: Mode.Static), obstacle, friction: 0); Tick(s);
            var state = Read(s, b);
            if (mode == CCD.CastRay) Check(s.CCDIntervalCount == 0 && state.Velocity.X == 3000, "A leading ray may miss an off-ray corner obstacle.");
            else Check(s.CCDIntervalCount > 0 && state.Velocity.X < 2999 && MathF.Abs(state.Velocity.Z) > 1, "Full-shape CCD captures the offset impact and torque.");
        }
    }
    private static void VerifyMovingAndRotating()
    {
        using var circle = new CircleShape { Radius = 1 };
        using (var s = new Store())
        {
            var a = Body(s, new(-20, 0), new(3000, 0), CCD.CastShape); var b = Body(s, new(20, 0), new(-3000, 0));
            s.AddShape(a, circle, friction: 0); s.AddShape(b, circle, friction: 0); Tick(s);
            Near(Read(s, a).Velocity.X, 0, 0.01f, "Relative moving-body sweep stops the light pair");
            Near(Read(s, b).Velocity.X, 0, 0.01f, "Disabled peer retains its policy and receives the physical impulse");
            Check(Read(s, a).Position.X < Read(s, b).Position.X && s.GetCCDMode(b) == CCD.Disabled, "Fast bodies do not swap sides or change another body's CCD policy.");
        }
        using (var s = new Store())
        {
            var a = Body(s, new(20, 0), ccd: CCD.CastShape); var k = Body(s, mode: Mode.Kinematic);
            s.AddShape(a, circle, friction: 0); s.AddShape(k, circle, friction: 0); s.SetKinematicTarget(k, new(80, 0), 0); Tick(s);
            Near(Read(s, k).Position.X, 80, 0.01f, "Kinematic target velocity integrates its full interval");
            Check(Read(s, a).Position.X > 81.9f && Read(s, a).Velocity.X > 3999, "A fast kinematic peer wakes and pushes a CCD body.");
        }
        using var bar = new RectangleShape { Size = new(20, 0.2f) }; using var target = new CircleShape { Radius = 0.5f };
        foreach (var rotatingTarget in new[] { false, true })
        {
            using var s = new Store();
            var rod = Body(s, ccd: rotatingTarget ? CCD.Disabled : CCD.CastShape, mode: rotatingTarget ? Mode.Kinematic : Mode.Rigid, angular: rotatingTarget ? 0 : MathF.PI / 0.02f);
            var point = Body(s, new(0, 8), ccd: rotatingTarget ? CCD.CastShape : CCD.Disabled, mode: rotatingTarget ? Mode.Rigid : Mode.Static);
            s.AddShape(rod, bar, friction: 0); s.AddShape(point, target, friction: 0);
            if (rotatingTarget) s.SetKinematicTarget(rod, Vector2.Zero, MathF.PI - 0.000001f);
            Tick(s);
            Check(s.CCDIntervalCount > 0, "The rotational arc is detected even when both endpoint poses miss.");
            if (!rotatingTarget) Check(Read(s, rod).Velocity.Z < MathF.PI / 0.02f - 1, "Rotational contact changes the dynamic rod's angular motion.");
            else Check(Read(s, point).Velocity.X < -1, "Rotating kinematic geometry pushes the stationary CCD body.");
        }
    }
    private static void VerifyPolicyAndForces()
    {
        using var circle = new CircleShape { Radius = 1 }; using var wall = new RectangleShape { Size = new(0.2f, 100) };
        foreach (var sensor in new[] { false, true })
        {
            using var s = new Store(); var b = Body(s, speed: new(3000, 0), ccd: CCD.CastShape); s.AddShape(b, circle);
            s.AddShape(Body(s, new(20, 0), mode: Mode.Static), wall, sensor: sensor, mask: sensor ? uint.MaxValue : 0);
            Tick(s); Near(Read(s, b).Position.X, 60, 0.001f, "Sensors and masked pairs do not create continuous physical stops");
        }
        using (var s = new Store())
        {
            var b = Body(s, speed: new(3000, 100), ccd: CCD.CastShape); s.AddShape(b, circle);
            s.AddShape(Body(s, new(20, 0), mode: Mode.Static), wall); Tick(s); var state = Read(s, b);
            Near(state.Velocity.X, 0, 0.01f, "Continuous contact stops the normal component");
            Near(state.Velocity.Y, 200f / 3, 0.02f, "Continuous friction retains rolling translation");
            Near(state.Velocity.Y - 0.5f * state.Velocity.Z, 100, 0.03f, "Continuous friction preserves angular momentum about the contact");
        }
        using (var s = new Store())
        {
            var b = Body(s, speed: new(3000, 0), ccd: CCD.CastShape); s.AddShape(b, circle);
            var other = Body(s, new(20, 0), mode: Mode.Static); s.AddShape(other, wall);
            s.AddJoint(new(PhysicsServer.JointType.DampedSpring, b, other, Transform.Identity, Transform.Identity) { Stiffness = 0, Damping = 0 });
            Tick(s); Check(Read(s, b).Position.X > 59.99f && s.CCDIntervalCount == 0, "Joint collision veto also filters swept candidates.");
        }
        using (var s = new Store())
        {
            var b = Body(s, speed: new(3000, 0), ccd: CCD.CastShape); s.AddShape(b, circle, friction: 0, bounce: 0.5f);
            s.AddShape(Body(s, new(20, 0), mode: Mode.Static), wall, friction: 0, bounce: 0.5f);
            var free = Body(s, new(0, 1000)); s.SetConstantForce(free, new(100, 0));
            var springA = Body(s, new(0, 2000)); var springB = Body(s, new(10, 2000));
            s.AddJoint(new(PhysicsServer.JointType.DampedSpring, springA, springB, Transform.Identity, Transform.Identity) { RestLength = 5, Stiffness = 20, Damping = 0 });
            var motor = Body(s, new(0, 3000), inertia: 1);
            s.AddJoint(new(PhysicsServer.JointType.Pin, motor, default, Transform.Identity, new Transform(0, new(0, 3000))) { MotorEnabled = true, MotorVelocity = 100, MotorMaxTorque = 0.001f });
            Tick(s);
            Near(Read(s, springA).Velocity.X, 2, 0.0001f, "CCD intervals apply spring force for the outer duration once");
            Near(MathF.Abs(Read(s, motor).Velocity.Z), 0.2f, 0.0001f, "CCD intervals preserve the motor torque budget");
            Near(Read(s, free).Velocity.X, 2, 0.0001f, "Impact intervals never reapply the outer force duration");
            Near(Read(s, free).Position.X, 0.04f, 0.0001f, "All bodies consume the same complete interval");
        }
    }
    private static void VerifyFailure()
    {
        using var s = new Store(); using var box = new RectangleShape { Size = new(20, 1) };
        using var wall = new RectangleShape { Size = new(0.2f, 100) };
        var b = Body(s, ccd: CCD.CastShape, angular: float.MaxValue / 100); s.AddShape(b, box, friction: 0);
        s.AddShape(Body(s, new(10.1f, 0), mode: Mode.Static), wall, friction: 0);
        Reject<InvalidOperationException>(() => Tick(s, 0.01f));
        Reject<InvalidOperationException>(() => s.GetCCDMode(b));
        Reject<InvalidOperationException>(() => Read(s, b));
        Console.WriteLine("Unrepresentable continuous motion fails the world without CPU replay.");
    }
    private static void VerifyResidency()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 1 };
        const int count = 256, warmup = 128, samples = 128;
        using var wall = new RectangleShape { Size = new(0.2f, count * 6 + 10) };
        var bodies = new Store.BodyHandle[count];
        for (var i = 0; i < count; i++) { bodies[i] = Body(s, new(0, i * 6), new(3000, 0), CCD.CastShape); s.AddShape(bodies[i], circle, friction: 0, bounce: 0.5f); }
        foreach (var x in new[] { -20f, 20f }) s.AddShape(Body(s, new(x, count * 3), mode: Mode.Static), wall, friction: 0, bounce: 0.5f);
        for (var i = 0; i < warmup; i++) Tick(s, 1f / 60);
        var times = new double[samples]; var queries = s.CCDQueryCount; var splits = s.CCDIntervalCount; var up = s.UploadBytes; var down = s.ReadbackBytes; var wait = s.WaitMS; var uniforms = s.UniformBytes;
        var allocation = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < samples; i++) { var start = Stopwatch.GetTimestamp(); Tick(s, 1f / 60); times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        allocation = GC.GetAllocatedBytesForCurrentThread() - allocation; Array.Sort(times);
        Check(allocation == 0 && s.CCDIntervalCount > splits, "Warm repeated real continuous impacts allocate no managed memory.");
        var uploadPerTick = (s.UploadBytes - up) / (double)samples; var readbackPerTick = (s.ReadbackBytes - down) / (double)samples;
        var waitPerTick = (s.WaitMS - wait) / samples; var uniformsPerTick = (s.UniformBytes - uniforms) / (double)samples;
        var states = new Store.Snapshot[count]; s.Read(bodies, states);
        foreach (var state in states) Check(MathF.Abs(state.Position.X) < 19.01f && MathF.Abs(MathF.Abs(state.Velocity.X) - 3000) < 0.1f, "Every continuous projectile remains inside and retains elastic speed.");
        Console.WriteLine($"Resident CCD: {count} projectiles, {warmup} warmup/{samples} samples, 60 Hz/1 substep/32 iterations; p50={times[samples / 2]:F4}, p95={times[(int)(samples * 0.95)]:F4}, wait={waitPerTick:F4} ms; {allocation} managed B/tick, queries={s.CCDQueryCount - queries}, intervals={s.CCDIntervalCount - splits}, upload={uploadPerTick:F2}, readback={readbackPerTick:F2}, uniforms={uniformsPerTick:F2} B/tick; {s.Driver}, {s.DeviceName}.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Near(float value, float expected, float tolerance, string message) => Check(MathF.Abs(value - expected) <= tolerance, $"{message}: {value} vs {expected}");
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
