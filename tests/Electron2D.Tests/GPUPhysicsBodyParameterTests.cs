using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class GPUPhysicsBodyParameterTests
{
    internal static void Run()
    {
        VerifyCPUFieldResponse(); VerifyModes(); VerifyOrderedEdits(); VerifyConstraintsAndSleep(); VerifyCCD(); VerifyFailures(); VerifyResidency();
        Console.WriteLine("Resident body parameters: CPU field/mode parity, ordered edits, locks, omission, constraints, sleep, CCD and allocation passed.");
    }
    private static Store.Snapshot Read(Store s, Store.BodyHandle b)
    { Span<Store.Snapshot> state = stackalloc Store.Snapshot[1]; s.Read([b], state); return state[0]; }
    private sealed class CPU : IDisposable
    {
        internal readonly RID Space = PhysicsServer.SpaceCreate(), Body = PhysicsServer.BodyCreate();
        internal CPU()
        {
            PhysicsServer.SpaceSetActive(Space, true); PhysicsServer.AreaSetGravity(Space, 30); PhysicsServer.AreaSetGravityVector(Space, Vector2.Down);
            PhysicsServer.BodySetMass(Body, 2); PhysicsServer.BodySetInertia(Body, 4); PhysicsServer.BodySetCanSleep(Body, false);
            PhysicsServer.BodySetLinearDampMode(Body, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(Body, RigidBody.DampMode.Replace);
            PhysicsServer.BodySetSpace(Body, Space);
        }
        internal void Velocity(Vector2 linear, float angular) { PhysicsServer.BodySetLinearVelocity(Body, linear); PhysicsServer.BodySetAngularVelocity(Body, angular); }
        public void Dispose() { PhysicsServer.FreeRID(Body); PhysicsServer.FreeRID(Space); }
    }
    private static void VerifyCPUFieldResponse()
    {
        using var cpu = new CPU(); using var s = new Store();
        var b = s.Add(new(Mode.Rigid, Vector2.Zero, 0, Vector2.Zero, 0, Mass: 2, Inertia: 4, CanSleep: false));
        Check(s.GetIntegrationPolicy(b) == new Store.IntegrationPolicy(1), "New body's authored integration defaults are retained.");
        foreach (var policy in new[] { new Store.IntegrationPolicy(-2, 3, 5), new(0.5f, -2, -3), new(1, 100, 100), new(3, 5, 7, OmitForceIntegration: true) })
        {
            PhysicsServer.BodySetGravityScale(cpu.Body, policy.GravityScale); PhysicsServer.BodySetLinearDamp(cpu.Body, policy.LinearDamp);
            PhysicsServer.BodySetAngularDamp(cpu.Body, policy.AngularDamp); PhysicsServer.BodySetOmitForceIntegration(cpu.Body, policy.OmitForceIntegration);
            PhysicsServer.BodySetConstantForce(cpu.Body, new(4, 6)); PhysicsServer.BodySetConstantTorque(cpu.Body, 8);
            cpu.Velocity(new(10, 20), 4); PhysicsServer.SpaceStep(cpu.Space, 0.1);
            var expected = PhysicsServer.BodyGetLinearVelocity(cpu.Body); var angular = PhysicsServer.BodyGetAngularVelocity(cpu.Body);
            foreach (var substeps in new[] { 1, 4, 8 })
            {
                s.SetIntegrationPolicy(b, policy); s.SetConstantForce(b, new(4, 6), 8); s.SetVelocity(b, new(10, 20), 4);
                s.Simulate(0.1f, new(0, 30), substeps: substeps);
                var result = Read(s, b);
                Near(new(result.Velocity.X, result.Velocity.Y), expected, 0.0005f, "GPU damping-before-forces matches public CPU for every subdivision");
                Near(result.Velocity.Z, angular, 0.0005f, "GPU angular damping/torque matches public CPU");
                Check(result.OmitForceIntegration == policy.OmitForceIntegration && s.GetIntegrationPolicy(b) == policy, "Authored/observed omission matches the live policy.");
            }
        }
    }
    private static void VerifyModes()
    {
        using var cpu = new CPU(); using var s = new Store();
        var b = s.Add(new(Mode.Rigid, new(10, 20), 0.4f, Vector2.Zero, 0, Mass: 2, Inertia: 4, CanSleep: false));
        var profile = s.GetMassProfile(b);
        foreach (var start in Enum.GetValues<Mode>())
            foreach (var end in Enum.GetValues<Mode>())
            {
                PhysicsServer.BodySetMode(cpu.Body, start); s.SetMode(b, start);
                cpu.Velocity(new(6, 8), 3); s.SetVelocity(b, new(6, 8), 3); Read(s, b);
                PhysicsServer.BodySetMode(cpu.Body, end); s.SetMode(b, end); var state = Read(s, b);
                Near(new(state.Velocity.X, state.Velocity.Y), PhysicsServer.BodyGetLinearVelocity(cpu.Body), 0.0001f, "All mode transitions preserve the public CPU linear state rule");
                Near(state.Velocity.Z, PhysicsServer.BodyGetAngularVelocity(cpu.Body), 0.0001f, "All mode transitions preserve the public CPU angular state rule");
                Check(s.GetMode(b) == end && state.Mode == end && state.RotationLocked == (end == Mode.RigidLinear) && s.GetMassProfile(b) == profile, "Role edits preserve identity and authored mass while publishing effective locks.");
                Near(state.Position, new(10, 20), 0, "Mode changes never teleport a body");
            }
        s.SetMode(b, Mode.Static); s.SetVelocity(b, new(12, -3), 5); s.Step(0.1f, new(0, 980)); var stationary = Read(s, b);
        Near(stationary.Position, new(10, 20), 0, "Static velocity remains virtual surface motion");
        Near(stationary.Velocity.Z, 5, 0, "Static angular surface velocity is retained");
        s.SetMode(b, Mode.Kinematic); s.SetVelocity(b, new(12, -3), 0); s.ApplyImpulse(b, new(1000, 1000), 1000); s.SetKinematicTarget(b, new(11.2f, 19.7f), 0.4f); s.Step(0.1f, new(0, 980));
        Near(Read(s, b).Position, new(11.2f, 19.7f), 0.0001f, "Kinematic motion ignores gravity and impulses after a live transition");
    }
    private static void VerifyOrderedEdits()
    {
        using var s = new Store();
        var initialSleep = s.Add(new(Mode.Rigid, Vector2.Zero, 0, new(6, 8), 3, Sleeping: true));
        s.SetSleeping(initialSleep, false); Check(Read(s, initialSleep).Velocity == System.Numerics.Vector4.Zero, "Waking before the first submission cannot restore velocities cleared by initial sleep.");
        var initialLock = s.Add(new(Mode.RigidLinear, Vector2.Zero, 0, new(6, 8), 3));
        s.SetMode(initialLock, Mode.Rigid); Near(Read(s, initialLock).Velocity.Z, 0, 0, "Changing the initial locked role before submission cannot restore rejected rotation.");
        s.Remove(initialSleep); s.Remove(initialLock);
        var b = s.Add(new(Mode.Rigid, Vector2.Zero, 0, new(6, 8), 3, Mass: 2, Inertia: 4, CanSleep: false, ContinuousMode: CCDMode.CastShape));
        s.SetIntegrationPolicy(b, new(2, LockRotation: true)); var state = Read(s, b);
        Check(!state.CanSleep && state.ContinuousMode == CCDMode.CastShape && state.RotationLocked, "Editing an unsubmitted body preserves CCD and sleep bits.");
        foreach (var flush in new[] { false, true })
        {
            void Flush() { if (flush) Read(s, b); }
            s.SetMode(b, Mode.Rigid); s.SetIntegrationPolicy(b, new(0)); s.SetVelocity(b, new(6, 8), 3); Read(s, b);
            s.ApplyImpulse(b, new(2, 0), 8); Flush();
            s.SetIntegrationPolicy(b, new(0, LockRotation: true)); Flush();
            s.SetVelocity(b, new(7, 8), 20); Flush();
            s.SetIntegrationPolicy(b, new(0)); Flush();
            s.ApplyImpulse(b, Vector2.Zero, 4); state = Read(s, b);
            Near(state.Velocity.Z, 1, 0, "Locking discards prior rotation and locked velocity writes, while later unlocked impulses survive");
            s.SetVelocity(b, new(7, 8), 3); Read(s, b);
            s.SetMode(b, Mode.Static); Flush(); s.SetMode(b, Mode.Rigid); Flush(); state = Read(s, b);
            Check(state.Velocity == System.Numerics.Vector4.Zero, "Intermediate nondynamic role clears motion even if the final role returns to rigid before submission.");
            s.SetIntegrationPolicy(b, new(0, LockRotation: true)); Flush(); s.SetIntegrationPolicy(b, new(0)); Flush();
            s.SetVelocity(b, Vector2.Zero, 9); Near(Read(s, b).Velocity.Z, 9, 0, "Later explicit unlocked velocity supersedes a queued angular clear");
        }
        s.SetSleeping(b, true); s.SetMode(b, Mode.RigidLinear); Check(!Read(s, b).Sleeping, "A role change wakes an explicitly sleeping body.");
        s.SetMode(b, Mode.Rigid); s.SetIntegrationPolicy(b, new(1)); s.SetSleeping(b, true); Check(Read(s, b).Sleeping, "A later explicit sleep wins over policy wakeup.");
    }
    private static void VerifyConstraintsAndSleep()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 1 };
        var a = s.Add(new(Mode.Static, Vector2.Zero, 0, Vector2.Zero, 0));
        var b = s.Add(new(Mode.Rigid, new(1.8f, 0), 0, Vector2.Zero, 0, Inertia: 1));
        s.AddShape(a, circle); var shape = s.AddShape(b, circle);
        s.SetIntegrationPolicy(b, new(10, 100, 100, OmitForceIntegration: true));
        s.SetConstantForce(b, new(100, 50), 20); s.SetVelocity(b, new(-10, 0), 0); s.SolveConstraints(0.01f, maxCorrectionSpeed: 0);
        Near(Read(s, b).Velocity.X, 0, 0.001f, "Omission retains physical contact response");
        s.SetSleeping(b, true); Read(s, b); s.SetIntegrationPolicy(b, new(0)); Check(!Read(s, b).Sleeping, "Changed live integration settings wake a sleeper.");
        s.SetConstantForce(b, Vector2.Zero); s.SetCollisionException(a, b, true);
        var joint = s.AddJoint(new(PhysicsServer.JointType.Pin, a, b, new Transform(0, new(1.8f, 0)), Transform.Identity));
        s.SetMode(b, Mode.Static); Check(s.FindContacts() == 0, "Two static roles produce no physical pair.");
        s.SetMode(b, Mode.Rigid); s.SetVelocity(b, new(10, 5), 4); s.SolveConstraints(0.01f, maxCorrectionSpeed: 0); var constrained = Read(s, b);
        Near(new(constrained.Velocity.X, constrained.Velocity.Y), Vector2.Zero, 0.001f, "The same joint handle constrains the restored dynamic endpoint");
        Check(s.GetJointDefinition(joint).BodyB == b && s.HasCollisionException(a, b) && s.ReadShapeBounds(shape).HasValue, "Mode switching retains joint, exception and shape ownership.");
        s.SetIntegrationPolicy(b, new(0, LockRotation: true)); s.ApplyImpulse(b, Vector2.Zero, 100); s.Simulate(0.02f, Vector2.Zero);
        Near(Read(s, b).Velocity.Z, 0, 0, "Locked body rejects angular impulses through the real solver");
    }
    private static void VerifyCCD()
    {
        using var s = new Store(); using var wall = new RectangleShape { Size = new(0.2f, 100) }; using var circle = new CircleShape { Radius = 1 };
        var a = s.Add(new(Mode.Static, new(20, 0), 0, Vector2.Zero, 0)); s.AddShape(a, wall, friction: 0);
        var b = s.Add(new(Mode.Static, Vector2.Zero, 0, Vector2.Zero, 0, ContinuousMode: CCDMode.CastShape, CanSleep: false)); s.AddShape(b, circle, friction: 0);
        s.SetMode(b, Mode.Rigid); s.SetIntegrationPolicy(b, new(0, 2)); s.SetVelocity(b, new(3000, 100), 0);
        s.Simulate(0.02f, Vector2.Zero, substeps: 1, margin: 0);
        Check(Read(s, b).Position.X < 20 && s.CCDIntervalCount > 0, "A static-to-rigid transition activates its retained CCD mode.");
        Near(Read(s, b).Velocity.Y, 96, 0.001f, "CCD impact intervals do not apply outer-tick damping again");
        var queries = s.CCDQueryCount; s.SetMode(b, Mode.Kinematic); s.SetPose(b, Vector2.Zero, 0); s.SetKinematicTarget(b, new(60, 0), 0);
        s.Simulate(0.02f, Vector2.Zero, substeps: 1, margin: 0);
        Check(s.GetCCDMode(b) == CCDMode.CastShape && s.CCDQueryCount == queries && Read(s, b).Position.X > 59, "Kinematic targets retain configured CCD but do not collide with static peers.");
    }
    private static void VerifyFailures()
    {
        using var s = new Store(); using var other = new Store();
        var b = s.Add(new(Mode.Rigid, Vector2.Zero, 0, Vector2.Zero, 0)); var foreign = other.Add(new(Mode.Rigid, Vector2.Zero, 0, Vector2.Zero, 0));
        Reject<ArgumentOutOfRangeException>(() => s.SetMode(b, (Mode)99));
        Reject<ArgumentOutOfRangeException>(() => s.SetIntegrationPolicy(b, new(float.NaN)));
        Reject<ArgumentOutOfRangeException>(() => s.SetIntegrationPolicy(b, new(1, float.NegativeInfinity)));
        Reject<ArgumentException>(() => s.SetMode(foreign, Mode.Static));
        Reject<InvalidOperationException>(() => Task.Run(() => s.SetIntegrationPolicy(b, new(2))).GetAwaiter().GetResult());
        Check(s.GetMode(b) == Mode.Rigid && s.GetIntegrationPolicy(b) == new Store.IntegrationPolicy(1), "Invalid settings leave valid authored state untouched.");
        s.SetIntegrationPolicy(b, new(0, -float.MaxValue)); s.SetVelocity(b, Vector2.One, 0);
        Reject<InvalidOperationException>(() => s.Simulate(2, Vector2.Zero));
        Reject<InvalidOperationException>(() => s.SetMode(b, Mode.Static));
        Reject<InvalidOperationException>(() => s.GetIntegrationPolicy(b));
        s.Dispose(); Reject<ObjectDisposedException>(() => s.SetIntegrationPolicy(b, new(1)));
    }
    private static void VerifyResidency()
    {
        using var s = new Store(); var handles = new Store.BodyHandle[4096];
        for (var i = 0; i < handles.Length; i++) handles[i] = s.Add(new(Mode.Rigid, new(i, 0), 0, Vector2.Zero, 0, Inertia: 1, CanSleep: false));
        void Tick(int i)
        {
            var b = handles[i % handles.Length]; s.SetMode(b, Mode.Static); s.SetMode(b, Mode.Rigid);
            s.SetIntegrationPolicy(b, new(1, i % 2, 0.5f, LockRotation: i % 2 == 0));
            s.SetVelocity(b, new(2, 1), 1); s.Simulate(1f / 60, Vector2.Zero);
        }
        for (var i = 0; i < 128; i++) Tick(i);
        var samples = new double[128]; var upload = s.UploadBytes; var readback = s.ReadbackBytes; var wait = s.WaitMS; var allocation = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < samples.Length; i++) { var start = Stopwatch.GetTimestamp(); Tick(i); samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        allocation = GC.GetAllocatedBytesForCurrentThread() - allocation;
        Check(allocation == 0 && s.UploadBytes - upload == 128 * (176 + 96) && s.ReadbackBytes - readback == 128 * 96,
            "Live role/policy/velocity edits share one 176-byte command, never download states and allocate zero managed bytes after warmup.");
        Array.Sort(samples);
        Console.WriteLine($"Resident body parameters: 4096 bodies, 128 warmup/128 samples, 4 substeps/16 iterations; p50={samples[64]:F4}, p95={samples[121]:F4}, p99={samples[126]:F4} ms, wait={(s.WaitMS - wait) / 128:F4} ms; {allocation} managed B, upload={(s.UploadBytes - upload) / 128}, readback={(s.ReadbackBytes - readback) / 128} B/tick; {s.Driver}, {s.DeviceName}.");
    }
    private static void Near(Vector2 a, Vector2 b, float tolerance, string message) => Check(a.IsFinite() && (a - b).Length() <= tolerance, $"{message}: {a} vs {b}");
    private static void Near(float a, float b, float tolerance, string message) => Check(float.IsFinite(a) && MathF.Abs(a - b) <= tolerance, $"{message}: {a} vs {b}");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
