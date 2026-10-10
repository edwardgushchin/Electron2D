using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class GPUPhysicsKinematicTests
{
    internal static void Run()
    {
        VerifyCPUState(); VerifyOrderAndLifetime(); VerifySurfacesAndJoints(); VerifyContinuousPaths(); VerifyFailures(); VerifyResidency();
        Console.WriteLine("Resident kinematics: public CPU target/surface semantics, exact destination/idle, ordering, contact/joint separation, fast paths, lifetime and allocation passed.");
    }
    private static Store.Snapshot Read(Store s, Store.BodyHandle b)
    { Span<Store.Snapshot> state = stackalloc Store.Snapshot[1]; s.Read([b], state); return state[0]; }
    private static Store.BodyHandle Body(Store s, Mode mode, Vector2 position = default, Vector2 velocity = default, float angular = 0, Vector2? center = null) =>
        s.Add(new(mode, position, 0, velocity, angular, Inertia: 1, CanSleep: false, CenterOfMass: center));
    private static void VerifyCPUState()
    {
        foreach (var substeps in new[] { 0, 1, 4, 8 })
        {
            using var s = new Store(); var center = new Vector2(3, -2); var initial = new Vector2(5, 7);
            var b = Body(s, Mode.Kinematic, initial, center: center);
            var space = PhysicsServer.SpaceCreate(); var cpu = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.SpaceSetActive(space, true); PhysicsServer.AreaSetGravity(space, 0);
                PhysicsServer.BodySetMode(cpu, Mode.Kinematic); PhysicsServer.BodySetCenterOfMass(cpu, center);
                PhysicsServer.BodySetTransform(cpu, new(0, initial)); PhysicsServer.BodySetSpace(cpu, space);
                PhysicsServer.BodySetLinearVelocity(cpu, new(7, -2)); PhysicsServer.BodySetAngularVelocity(cpu, 0.4f); s.SetVelocity(b, new(7, -2), 0.4f);
                PhysicsServer.SpaceStep(space, 0.1); Tick();
                Near(Read(s, b).Position, PhysicsServer.BodyGetTransform(cpu).Origin, 0.0005f, "Virtual velocity keeps the kinematic pose stationary");
                s.SetKinematicTarget(b, new(100, 100), 1); s.SetKinematicTarget(b, new(9, 10), 0.1f);
                PhysicsServer.BodySetTransform(cpu, new(0.1f, new(9, 10)));
                s.Simulate(0, Vector2.Zero); s.Step(0, Vector2.Zero); s.SolveConstraints(0.1f); PhysicsServer.SpaceStep(space, 0);
                Near(Read(s, b).Position, initial, 0, "Read/zero-time/constraint-only work leaves the target pending");
                PhysicsServer.SpaceStep(space, 0.1); Tick(); var moved = Read(s, b);
                Near(moved.Position, new(9, 10), 0, "GPU reaches the exact authored body origin");
                Near(moved.Position, PhysicsServer.BodyGetTransform(cpu).Origin, 0.02f, "CPU/GPU origin agrees within CPU angular estimation error at an offset center");
                Near(new(moved.Velocity.X, moved.Velocity.Y), PhysicsServer.BodyGetLinearVelocity(cpu), 0.1f, "Observed velocity combines center travel and authored surface motion");
                var analytic = (new Vector2(9, 10) + center.Rotated(0.1f) - initial - center) / 0.1f + new Vector2(7, -2);
                Near(new(moved.Velocity.X, moved.Velocity.Y), analytic, 0.0001f, "GPU target velocity satisfies the analytic center displacement equation");
                Near(moved.Velocity.Z, 1.4f, 0.00001f, "GPU angular velocity satisfies the analytic shortest-arc equation");
                Near(moved.Velocity.Z, PhysicsServer.BodyGetAngularVelocity(cpu), 0.05f, "Angular target motion uses the shortest arc and adds the surface channel");
                PhysicsServer.SpaceStep(space, 0.1); Tick(); var idle = Read(s, b);
                Near(new Vector2(idle.Velocity.X, idle.Velocity.Y), new(7, -2), 0, "Only consumed target velocity clears on the following tick");
                Near(idle.Velocity.Z, 0.4f, 0, "Authored angular surface motion survives target consumption");
                for (var i = 0; i < 32; i++) Tick();
                Check(Read(s, b).Pose == moved.Pose, "An idle kinematic body never drifts from quaternion re-decoding or virtual angular motion.");
            }
            finally { PhysicsServer.FreeRID(cpu); PhysicsServer.FreeRID(space); }
            void Tick() { if (substeps == 0) s.Step(0.1f, new(0, 980)); else s.Simulate(0.1f, new(0, 980), substeps: substeps); }
        }
    }
    private static void VerifyOrderAndLifetime()
    {
        foreach (var flush in new[] { false, true })
        {
            using var s = new Store(); var b = Body(s, Mode.Kinematic, velocity: new(2, 3), angular: 0.5f);
            void Flush() { if (flush) Read(s, b); }
            s.SetKinematicTarget(b, new(10, 20), 0.2f); Flush(); s.SetVelocity(b, new(3, 4), 1); Flush();
            s.SetPose(b, new(5, 7), 0.3f); Flush(); s.Step(0.1f, Vector2.Zero);
            Near(Read(s, b).Position, new(5, 7), 0, "A later teleport cancels the prior target while preserving virtual motion");
            s.SetKinematicTarget(b, new(20, 30), 0.6f); Flush(); s.SetMode(b, Mode.Static); Flush(); s.SetMode(b, Mode.Kinematic);
            s.Step(0.1f, Vector2.Zero); Near(Read(s, b).Position, new(5, 7), 0, "Role changes retire pending destinations even when coalesced back to kinematic");
            s.SetKinematicTarget(b, new(8, 9), 0.4f); Flush(); s.SetMassProfile(b, new(2, 10, new(4, 5))); Flush();
            s.Step(0.1f, Vector2.Zero); Near(Read(s, b).Position, new(8, 9), 0, "Target derivation uses the latest mass center without altering the origin target");
            s.SetKinematicTarget(b, new(10, 11), 0.5f); Read(s, b);
            var copies = s.DeviceCopyBytes;
            for (var i = 0; i < 130; i++) Body(s, Mode.Static, new(i, 100));
            Check(s.DeviceCopyBytes > copies, "Growth keeps target state on the device."); s.Step(0.1f, Vector2.Zero);
            Near(Read(s, b).Position, new(10, 11), 0, "Pending target survives growth");
            s.SetKinematicTarget(b, new(100, 100), 1); Read(s, b); s.Remove(b); var replacement = Body(s, Mode.Kinematic);
            Check(replacement.Index == b.Index && replacement.Generation != b.Generation, "Body reuse has a distinct target lifetime.");
            s.Step(0.1f, Vector2.Zero); Near(Read(s, replacement).Position, Vector2.Zero, 0, "Reused slot cannot inherit its predecessor's target");
            for (var angle = 0; angle < 16; angle++)
            {
                s.SetKinematicTarget(replacement, new(1, 2), angle * 0.17f); s.Step(0.1f, Vector2.Zero); var pose = Read(s, replacement).Pose;
                for (var idle = 0; idle < 4; idle++) s.Step(0.1f, Vector2.Zero);
                Check(Read(s, replacement).Pose == pose, "Idle poses retain exact trigonometric target bits at every tested angle.");
            }
            Reject<ArgumentException>(() => s.SetKinematicTarget(b, Vector2.Zero, 0));
        }
    }
    private static void VerifySurfacesAndJoints()
    {
        using var circle = new CircleShape { Radius = 1 };
        foreach (var mode in new[] { Mode.Static, Mode.Kinematic })
        {
            using var s = new Store(); var a = Body(s, mode); var b = Body(s, Mode.Rigid, new(0, -1.9f));
            s.AddShape(a, circle, friction: 1); s.AddShape(b, circle, friction: 1);
            s.SetVelocity(a, new(0, -10), 0); s.SolveConstraints(0.01f, maxCorrectionSpeed: 0);
            Near(Read(s, b).Velocity.Y, -10, 0.001f, "Stationary surface normal velocity transfers contact momentum");
            Near(Read(s, a).Position, Vector2.Zero, 0, "Contact response never displaces the prescribed surface");
            s.SetVelocity(b, Vector2.Zero, 0); s.SetVelocity(a, Vector2.Zero, 4); s.Simulate(0.01f, new(0, 1000));
            Check(Read(s, b).Velocity.X > 0.1f, "Stationary angular surface motion contributes tangent point velocity.");
            Near(Read(s, a).Rotation, 0, 0, "Angular surface motion does not rotate geometry");
            s.SetVelocity(a, Vector2.Zero, 0); s.SetCanSleep(b, true); s.SetSleeping(b, true); s.Simulate(0.01f, Vector2.Zero);
            s.SetVelocity(a, new(0, -5), 0); s.Simulate(0.01f, Vector2.Zero);
            Check(!Read(s, b).Sleeping, "Surface edits wake an attached sleeper.");
        }
        foreach (var kind in new[] { PhysicsServer.JointType.Pin, PhysicsServer.JointType.DampedSpring })
        {
            using var s = new Store(); var a = Body(s, Mode.Kinematic); var b = Body(s, Mode.Rigid, new(10, 0));
            var d = new Store.JointDefinition(kind, a, b, kind == PhysicsServer.JointType.Pin ? new(0, new(10, 0)) : Transform.Identity, Transform.Identity) { RestLength = 10, Stiffness = 0, Damping = 100 };
            s.AddJoint(d); s.SetVelocity(a, new(50, 0), 3); s.Simulate(0.1f, Vector2.Zero, maxCorrectionSpeed: 0);
            Check(Read(s, b).Velocity == System.Numerics.Vector4.Zero, "A joint's fixed anchor never follows virtual surface motion.");
            if (kind == PhysicsServer.JointType.Pin)
            {
                s.SetKinematicTarget(a, new(1, 0), 0); s.Simulate(0.1f, Vector2.Zero, maxCorrectionSpeed: 0);
                Near(Read(s, b).Velocity.X, 10, 0.001f, "Actual target travel drives the joint without added conveyor velocity");
            }
        }
    }
    private static void VerifyContinuousPaths()
    {
        using var circle = new CircleShape { Radius = 1 };
        using (var s = new Store())
        {
            var a = Body(s, Mode.Kinematic); var b = Body(s, Mode.Rigid, new(20, 0)); s.AddShape(a, circle, friction: 0); s.AddShape(b, circle, friction: 0);
            s.SetCanSleep(b, true); s.SetSleeping(b, true); s.SetKinematicTarget(a, new(80, 0), 0);
            s.Simulate(0.02f, Vector2.Zero, substeps: 1, margin: 0);
            Near(Read(s, a).Position.X, 80, 0, "Fast kinematic motion reaches its complete target");
            Check(s.CCDIntervalCount > 0 && Read(s, b).Position.X > 81.9f && Read(s, b).Velocity.X > 3999 && !Read(s, b).Sleeping,
                "Kinematic path wakes and pushes a default-CCD dynamic body before crossing it.");
            Check(s.GetCCDMode(b) == CCDMode.Disabled, "Fast kinematic handling never changes its peer's authored CCD policy.");
        }
        foreach (var surface in new[] { -4000f, 4000f })
        {
            using var s = new Store(); var a = Body(s, Mode.Kinematic, velocity: new(surface, 0)); var b = Body(s, Mode.Rigid, new(20, 0));
            s.AddShape(a, circle, friction: 0); s.AddShape(b, circle, friction: 0); s.SetKinematicTarget(a, new(80, 0), 0);
            s.Simulate(0.02f, Vector2.Zero, substeps: 1, margin: 0);
            Near(Read(s, a).Position.X, 80, 0, "Target completes when virtual normal motion cancels or augments actual travel");
            Near(Read(s, b).Velocity.X, 4000 + surface, 0.02f, "Contact impulse follows combined actual/virtual normal velocity");
            Check(s.CCDIntervalCount > 0, "The mixed-surface case still detects the swept contact.");
        }
        using var rod = new RectangleShape { Size = new(20, 0.2f) }; using var small = new CircleShape { Radius = 0.5f };
        using (var s = new Store())
        {
            var a = Body(s, Mode.Kinematic); var b = Body(s, Mode.Rigid, new(0, 8)); s.AddShape(a, rod, friction: 0); s.AddShape(b, small, friction: 0);
            s.SetKinematicTarget(a, Vector2.Zero, MathF.PI - 0.000001f); s.Simulate(0.02f, Vector2.Zero, substeps: 1, margin: 0);
            Check(s.CCDIntervalCount > 0 && Read(s, b).Velocity.X < -1, "Rotational path catches a default-CCD body missed by both endpoint poses.");
        }
        using (var s = new Store())
        {
            var a = Body(s, Mode.Kinematic); var b = Body(s, Mode.Rigid, new(20, 0)); s.AddShape(a, circle); s.AddShape(b, circle);
            s.SetCollisionException(a, b, true); s.SetKinematicTarget(a, new(80, 0), 0); s.Simulate(0.02f, Vector2.Zero, substeps: 1);
            Near(Read(s, b).Position, new(20, 0), 0, "Kinematic sweeps retain directed exception filtering");
        }
    }
    private static void VerifyFailures()
    {
        using var s = new Store(); using var other = new Store(); var b = Body(s, Mode.Kinematic); var rigid = Body(s, Mode.Rigid); var foreign = Body(other, Mode.Kinematic);
        s.SetKinematicTarget(b, new(1, 2), 0);
        Reject<ArgumentOutOfRangeException>(() => s.SetKinematicTarget(b, new(float.NaN, 0), 0));
        Reject<ArgumentOutOfRangeException>(() => s.SetKinematicTarget(b, Vector2.Zero, float.PositiveInfinity));
        Reject<InvalidOperationException>(() => s.SetKinematicTarget(rigid, Vector2.Zero, 0));
        Reject<ArgumentException>(() => s.SetKinematicTarget(foreign, Vector2.Zero, 0));
        Reject<InvalidOperationException>(() => Task.Run(() => s.SetKinematicTarget(b, Vector2.Zero, 0)).GetAwaiter().GetResult());
        s.Step(0.1f, Vector2.Zero); Near(Read(s, b).Position, new(1, 2), 0, "Rejected target edits retain the earlier valid destination");
        s.SetKinematicTarget(b, new(float.MaxValue, 0), 0); Reject<InvalidOperationException>(() => s.Step(0.01f, Vector2.Zero));
        Reject<InvalidOperationException>(() => s.SetKinematicTarget(b, Vector2.Zero, 0)); s.Dispose();
        Reject<ObjectDisposedException>(() => s.SetKinematicTarget(b, Vector2.Zero, 0));
    }
    private static void VerifyResidency()
    {
        using var s = new Store(); var bodies = new Store.BodyHandle[4096];
        for (var i = 0; i < bodies.Length; i++) bodies[i] = Body(s, Mode.Kinematic, new(i * 4, 0), new(2, 0));
        void Tick(int tick)
        {
            for (var i = 0; i < bodies.Length; i++) s.SetKinematicTarget(bodies[i], new(i * 4, tick % 2), 0.01f * (tick % 2));
            s.Simulate(1f / 60, Vector2.Zero);
        }
        for (var i = 0; i < 128; i++) Tick(i);
        var samples = new double[128]; var upload = s.UploadBytes; var readback = s.ReadbackBytes; var wait = s.WaitMS; var allocation = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < samples.Length; i++) { var start = Stopwatch.GetTimestamp(); Tick(i); samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        allocation = GC.GetAllocatedBytesForCurrentThread() - allocation;
        Check(allocation == 0 && s.UploadBytes - upload == 128L * (176 * 4096 + 72) && s.ReadbackBytes - readback == 128 * (72 + 8),
            "All target derivation stays on the GPU with only authored commands and status traffic, and zero warmed owner-thread allocation.");
        Array.Sort(samples);
        Console.WriteLine($"Resident kinematic targets: 4096 bodies/edits, no shapes, 128 warmup/128 samples, 4 substeps/16 iterations; p50={samples[64]:F4}, p95={samples[121]:F4}, p99={samples[126]:F4} ms, wait={(s.WaitMS - wait) / 128:F4} ms, {allocation} managed B, upload={(s.UploadBytes - upload) / 128}, readback={(s.ReadbackBytes - readback) / 128} B/tick; {s.Driver}, {s.DeviceName}.");
        var states = new Store.Snapshot[bodies.Length]; s.Read(bodies, states);
        for (var i = 0; i < bodies.Length; i++) Near(states[i].Position, new(i * 4, 1), 0, "Full requested population reaches its target; read is outside the measured interval");
    }
    private static void Near(Vector2 a, Vector2 b, float tolerance, string message) => Check(a.IsFinite() && (a - b).Length() <= tolerance, $"{message}: {a} vs {b}");
    private static void Near(float a, float b, float tolerance, string message) => Check(float.IsFinite(a) && MathF.Abs(a - b) <= tolerance, $"{message}: {a} vs {b}");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
