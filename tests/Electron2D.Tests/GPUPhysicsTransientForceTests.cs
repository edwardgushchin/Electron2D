using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class GPUPhysicsTransientForceTests
{
    internal static void Run()
    {
        VerifyCPUResponse(); VerifyEligibility(); VerifyLateWake(); VerifyConstraintsAndCCD(); VerifyLifetime(); VerifyFailure(); VerifyResidency();
        Console.WriteLine("Resident transient forces: public CPU equations, outer-tick consumption, modes, sleep, omission, constraints, CCD, lifetime and allocation passed.");
    }
    private static Store.Snapshot Read(Store s, Store.BodyHandle b)
    { Span<Store.Snapshot> result = stackalloc Store.Snapshot[1]; s.Read([b], result); return result[0]; }
    private sealed class CPU : IDisposable
    {
        internal readonly RID Space = PhysicsServer.SpaceCreate(), Body = PhysicsServer.BodyCreate();
        internal CPU()
        {
            PhysicsServer.SpaceSetActive(Space, true); PhysicsServer.AreaSetGravity(Space, 0);
            PhysicsServer.BodySetMass(Body, 2); PhysicsServer.BodySetInertia(Body, 4); PhysicsServer.BodySetCanSleep(Body, false);
            PhysicsServer.BodySetLinearDampMode(Body, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(Body, RigidBody.DampMode.Replace);
            PhysicsServer.BodySetSpace(Body, Space);
        }
        internal void Force(Vector2 linear, float angular)
        { PhysicsServer.BodyApplyCentralForce(Body, linear); PhysicsServer.BodyApplyTorque(Body, angular); }
        internal void Step() => PhysicsServer.SpaceStep(Space, 0.1);
        public void Dispose() { PhysicsServer.FreeRID(Body); PhysicsServer.FreeRID(Space); }
    }
    private static void VerifyCPUResponse()
    {
        foreach (var flush in new[] { false, true })
            foreach (var substeps in new[] { 0, 1, 4, 8 })
            {
                using var s = new Store(); using var cpu = new CPU();
                var b = s.Add(new(Mode.Rigid, Vector2.Zero, 0, new(10, 20), 4, Mass: 2, Inertia: 4, LinearDamp: 3, AngularDamp: 5, ConstantForce: new(4, 6), ConstantTorque: 8, CanSleep: false));
                PhysicsServer.BodySetLinearVelocity(cpu.Body, new(10, 20)); PhysicsServer.BodySetAngularVelocity(cpu.Body, 4);
                PhysicsServer.BodySetLinearDamp(cpu.Body, 3); PhysicsServer.BodySetAngularDamp(cpu.Body, 5);
                PhysicsServer.BodySetConstantForce(cpu.Body, new(4, 6)); PhysicsServer.BodySetConstantTorque(cpu.Body, 8);
                s.ApplyForce(b, new(20, -10), 40); cpu.Force(new(20, -10), 40);
                if (flush) Read(s, b);
                s.ApplyForce(b, new(-2, 6), -4); cpu.Force(new(-2, 6), -4);
                // Forces use integration-time mass, unlike impulses which capture it at call time.
                s.SetMassProfile(b, new(4, 8)); PhysicsServer.BodySetMass(cpu.Body, 4); PhysicsServer.BodySetInertia(cpu.Body, 8);
                s.Step(0, Vector2.Zero); s.Simulate(0, Vector2.Zero); s.SolveConstraints(0.1f);
                Near(Read(s, b).Velocity.X, 10, 0, "Reads, flushes and constraint-only solves do not consume or integrate queued forces");
                for (var tick = 0; tick < 2; tick++)
                {
                    if (substeps == 0) s.Step(0.1f, Vector2.Zero); else s.Simulate(0.1f, Vector2.Zero, substeps: substeps);
                    cpu.Step(); var actual = Read(s, b); var expected = PhysicsServer.BodyGetLinearVelocity(cpu.Body);
                    Near(new(actual.Velocity.X, actual.Velocity.Y), expected, 0.0005f, "Single-use force agrees with public CPU across subdivisions and intervening reads");
                    Near(actual.Velocity.Z, PhysicsServer.BodyGetAngularVelocity(cpu.Body), 0.0005f, "Transient and constant torques retain separate lifetimes");
                }
            }
    }
    private static void VerifyEligibility()
    {
        foreach (var mode in Enum.GetValues<Mode>())
        {
            using var s = new Store(); using var cpu = new CPU();
            var b = s.Add(new(mode, Vector2.Zero, 0, Vector2.Zero, 0, Mass: 2, Inertia: 4, CanSleep: false));
            PhysicsServer.BodySetMode(cpu.Body, mode); s.ApplyForce(b, new(20, 0), 40); cpu.Force(new(20, 0), 40);
            s.Simulate(0.1f, Vector2.Zero); cpu.Step(); Compare();
            s.SetMode(b, Mode.Rigid); PhysicsServer.BodySetMode(cpu.Body, Mode.Rigid);
            s.Simulate(0.1f, Vector2.Zero); cpu.Step(); Compare();
            void Compare()
            {
                var result = Read(s, b); Near(result.Velocity.X, PhysicsServer.BodyGetLinearVelocity(cpu.Body).X, 0.0005f, "Static retains pending force; kinematic consumes without response");
                Near(result.Velocity.Z, PhysicsServer.BodyGetAngularVelocity(cpu.Body), 0.0005f, "Locked torque is consumed and cannot return after unlocking");
            }
        }
        using var world = new Store(); using var reference = new CPU();
        var body = world.Add(new(Mode.Rigid, Vector2.Zero, 0, Vector2.Zero, 0, Mass: 2, Inertia: 4));
        world.ApplyForce(body, new(20, 0), 40); reference.Force(new(20, 0), 40);
        world.SetSleeping(body, true); PhysicsServer.BodySetSleeping(reference.Body, true);
        for (var i = 0; i < 3; i++) { world.Simulate(0.1f, Vector2.Zero); reference.Step(); }
        Check(Read(world, body).Sleeping && PhysicsServer.BodyGetSleeping(reference.Body), "Later explicit sleep retains force across inactive steps.");
        world.ApplyForce(body, Vector2.Zero); reference.Force(Vector2.Zero, 0);
        Check(!Read(world, body).Sleeping, "Even zero force explicitly wakes a dynamic body.");
        world.Simulate(0.1f, Vector2.Zero); reference.Step();
        Near(Read(world, body).Velocity.X, PhysicsServer.BodyGetLinearVelocity(reference.Body).X, 0.0005f, "Dormant force applies on the next eligible tick");
        world.SetIntegrationPolicy(body, new(0, OmitForceIntegration: true)); PhysicsServer.BodySetOmitForceIntegration(reference.Body, true);
        world.ApplyForce(body, new(20, 0), 40); reference.Force(new(20, 0), 40);
        world.ApplyImpulse(body, new(2, 0), 4); PhysicsServer.BodyApplyCentralImpulse(reference.Body, new(2, 0)); PhysicsServer.BodyApplyTorqueImpulse(reference.Body, 4);
        world.Simulate(0.1f, Vector2.Zero); reference.Step();
        world.SetIntegrationPolicy(body, new(0)); PhysicsServer.BodySetOmitForceIntegration(reference.Body, false);
        world.Simulate(0.1f, Vector2.Zero); reference.Step();
        var omitted = Read(world, body); Near(omitted.Velocity.X, PhysicsServer.BodyGetLinearVelocity(reference.Body).X, 0.0005f, "Omission consumes force without erasing impulse");
        Near(omitted.Velocity.Z, PhysicsServer.BodyGetAngularVelocity(reference.Body), 0.0005f, "Omitted torque cannot return in a later tick");
    }
    private static void VerifyLateWake()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 1 }; using var floor = new RectangleShape { Size = new(20, 2) };
        var a = s.Add(new(Mode.Static, new(0, 2), 0, new(1, 0), 0)); s.AddShape(a, floor, friction: 0);
        var b = s.Add(new(Mode.Rigid, Vector2.Zero, 0, Vector2.Zero, 0, Inertia: 1)); s.AddShape(b, circle, friction: 0);
        s.ApplyForce(b, new(100, 0)); s.SetSleeping(b, true); s.Simulate(0.1f, Vector2.Zero);
        Check(!Read(s, b).Sleeping, "Moving surface wakes the sleeper during contact preparation.");
        Near(Read(s, b).Velocity.X, 0, 0.0001f, "Contact wake does not change the outer-tick force eligibility snapshot");
        s.Simulate(0.1f, Vector2.Zero); Near(Read(s, b).Velocity.X, 10, 0.0005f, "Late-woken body applies retained force on its next tick");
        s.Simulate(0.1f, Vector2.Zero); Near(Read(s, b).Velocity.X, 10, 0.0005f, "Late-woken force remains single-use");
        s.SetVelocity(b, Vector2.Zero, 0); s.SetSleepSettings(new(100, 100, 0)); s.ApplyForce(b, new(1, 0));
        s.SetPose(a, new(100, 100), 0); s.Simulate(0.1f, Vector2.Zero); Check(Read(s, b).Sleeping, "Quiet force recipient can sleep during its consuming tick.");
        s.SetCanSleep(b, false); s.Simulate(0.1f, Vector2.Zero); Near(Read(s, b).Velocity.X, 0, 0, "Consumed force never returns after automatic sleep and wake");
    }
    private static void VerifyConstraintsAndCCD()
    {
        using var s = new Store(); using var wall = new RectangleShape { Size = new(0.2f, 100) }; using var circle = new CircleShape { Radius = 1 };
        var a = s.Add(new(Mode.Static, new(20, 0), 0, Vector2.Zero, 0)); s.AddShape(a, wall, friction: 0);
        var b = s.Add(new(Mode.Rigid, Vector2.Zero, 0, new(3000, 0), 0, Inertia: 1, CanSleep: false, ContinuousMode: CCDMode.CastShape)); s.AddShape(b, circle, friction: 0);
        s.ApplyForce(b, new(0, 100)); s.Simulate(0.02f, Vector2.Zero, substeps: 1, margin: 0);
        var first = Read(s, b); Check(first.Position.X < 20 && s.CCDIntervalCount > 0, "Force-loaded body collides through the continuous path.");
        Near(first.Velocity.Y, 2, 0.001f, "CCD impact intervals do not integrate transient force again");
        s.Simulate(0.02f, Vector2.Zero); Near(Read(s, b).Velocity.Y, 2, 0.001f, "CCD final interval consumes the outer force");
        s.SetVelocity(b, Vector2.Zero, 0); s.SetPose(b, Vector2.Zero, 0);
        s.AddJoint(new(PhysicsServer.JointType.Pin, b, default, Transform.Identity, Transform.Identity));
        s.ApplyForce(b, new(100, 100), 10); s.Simulate(0.1f, Vector2.Zero, iterations: 32);
        Near(new(Read(s, b).Velocity.X, Read(s, b).Velocity.Y), Vector2.Zero, 0.001f, "Joint constraints respond to transient force in the same tick");
    }
    private static void VerifyLifetime()
    {
        using var s = new Store();
        var b = s.Add(new(Mode.Static, Vector2.Zero, 0, Vector2.Zero, 0, Mass: 2, Inertia: 4));
        s.ApplyForce(b, new(20, 0), 40); Read(s, b);
        var copies = s.DeviceCopyBytes;
        for (var i = 0; i < 130; i++) s.Add(new(Mode.Static, new(i, 0), 0, Vector2.Zero, 0));
        Check(s.DeviceCopyBytes > copies, "Growth copies device state without a host snapshot.");
        s.SetMode(b, Mode.Rigid); s.Simulate(0.1f, Vector2.Zero); Near(Read(s, b).Velocity.X, 1, 0.0005f, "Device growth preserves pending force");
        s.ApplyForce(b, new(20, 0), 40); Read(s, b); s.Remove(b);
        var replacement = s.Add(new(Mode.Rigid, Vector2.Zero, 0, Vector2.Zero, 0));
        Check(replacement.Index == b.Index && replacement.Generation != b.Generation, "Replacement reuses only the slot, not force identity.");
        s.Simulate(0.1f, Vector2.Zero); Near(Read(s, replacement).Velocity.X, 0, 0, "Reused generation starts without stale force");
        s.ApplyForce(replacement, new(20, 0)); s.Remove(replacement);
        var unsubmitted = s.Add(new(Mode.Rigid, Vector2.Zero, 0, Vector2.Zero, 0)); s.Step(0.1f, Vector2.Zero);
        Near(Read(s, unsubmitted).Velocity.X, 0, 0, "Unsubmitted force is discarded on destruction");
        Reject<ArgumentException>(() => s.ApplyForce(b, Vector2.One));
    }
    private static void VerifyFailure()
    {
        using var s = new Store(); using var other = new Store();
        var b = s.Add(new(Mode.Static, Vector2.Zero, 0, Vector2.Zero, 0)); var foreign = other.Add(new(Mode.Static, Vector2.Zero, 0, Vector2.Zero, 0));
        Reject<ArgumentOutOfRangeException>(() => s.ApplyForce(b, new(float.NaN, 0)));
        Reject<ArgumentOutOfRangeException>(() => s.ApplyForce(b, Vector2.Zero, float.PositiveInfinity));
        Reject<ArgumentException>(() => s.ApplyForce(foreign, Vector2.Zero));
        Reject<InvalidOperationException>(() => Task.Run(() => s.ApplyForce(b, Vector2.Zero)).GetAwaiter().GetResult());
        s.ApplyForce(b, new(float.MaxValue, 0));
        Reject<ArgumentOutOfRangeException>(() => s.ApplyForce(b, new(float.MaxValue, 1), 2));
        s.ApplyForce(b, new(-float.MaxValue, 0)); s.SetMode(b, Mode.Rigid); s.Step(0.1f, Vector2.Zero);
        Check(Read(s, b).Velocity == System.Numerics.Vector4.Zero, "Rejected queued sum cannot partially update another channel or wake state.");
        s.SetMode(b, Mode.Static); s.ApplyForce(b, new(float.MaxValue, 0)); Read(s, b);
        s.ApplyForce(b, new(float.MaxValue, 0));
        Reject<InvalidOperationException>(() => Read(s, b));
        Reject<InvalidOperationException>(() => s.ApplyForce(b, Vector2.Zero));
        s.Dispose(); Reject<ObjectDisposedException>(() => s.ApplyForce(b, Vector2.Zero));
    }
    private static void VerifyResidency()
    {
        using var s = new Store(); var handles = new Store.BodyHandle[4096];
        for (var i = 0; i < handles.Length; i++) handles[i] = s.Add(new(Mode.Rigid, new(i, 0), 0, Vector2.Zero, 0, Inertia: 1, CanSleep: false));
        void Tick(int i) { s.ApplyForce(handles[i % handles.Length], new(2, 1), 3); s.Simulate(1f / 60, Vector2.Zero); }
        for (var i = 0; i < 128; i++) Tick(i);
        var samples = new double[128]; var upload = s.UploadBytes; var readback = s.ReadbackBytes; var waits = s.WaitMS; var allocation = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < samples.Length; i++) { var start = Stopwatch.GetTimestamp(); Tick(i); samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        allocation = GC.GetAllocatedBytesForCurrentThread() - allocation;
        Check(allocation == 0 && s.UploadBytes - upload == 128 * (176 + 72) && s.ReadbackBytes - readback == 128 * (72 + 8),
            "Resolved force edits use one 176-byte command, no state readback and zero warmed owner-thread managed bytes.");
        Array.Sort(samples);
        Console.WriteLine($"Resident transient forces: 4096 bodies, 128 warmup/128 samples, 4 substeps/16 iterations; p50={samples[64]:F4}, p95={samples[121]:F4}, p99={samples[126]:F4} ms, wait={(s.WaitMS - waits) / 128:F4} ms; {allocation} managed B, upload={(s.UploadBytes - upload) / 128}, readback={(s.ReadbackBytes - readback) / 128} B/tick; {s.Driver}, {s.DeviceName}.");
    }
    private static void Near(Vector2 a, Vector2 b, float tolerance, string message) => Check(a.IsFinite() && (a - b).Length() <= tolerance, $"{message}: {a} vs {b}");
    private static void Near(float a, float b, float tolerance, string message) => Check(float.IsFinite(a) && MathF.Abs(a - b) <= tolerance, $"{message}: {a} vs {b}");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
