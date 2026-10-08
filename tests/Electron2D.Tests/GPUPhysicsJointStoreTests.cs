using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;
using Kind = Electron2D.PhysicsServer.JointType;

internal static class GPUPhysicsJointStoreTests
{
    internal static void Run()
    {
        VerifyPin(); VerifyGroove(); VerifySpring(); VerifyLifecycle(); VerifyContactsAndFailure(); GPUPhysicsJointPolicyTests.Run(); VerifyPopulation(false); VerifyPopulation(true);
    }
    private static Store.BodyHandle Body(Store store, Vector2 position = default, Vector2 velocity = default, float mass = 1, float inertia = 1, Mode mode = Mode.Rigid, float angular = 0) =>
        store.Add(new(mode, position, 0, velocity, angular, mass, inertia, CanSleep: false));
    private static void VerifyPin()
    {
        using var store = new Store();
        var a = Body(store, velocity: new(12, -3)); var b = Body(store, mass: 3);
        var joint = store.AddJoint(new(Kind.Pin, a, b, Transform.Identity, Transform.Identity));
        var state = new Store.Snapshot[2];
        store.SolveConstraints(0.01f, iterations: 64); store.Read([a, b], state);
        Near(state[0].Velocity.X, 3, 0.001f, "Pin conserves pair momentum");
        Near(state[1].Velocity.Y, -0.75f, 0.001f, "Pin equalizes anchor velocity");
        store.Remove(a); store.Remove(b);
        a = Body(store, velocity: new(20, 10), mode: Mode.Static, angular: 3); b = Body(store);
        store.AddJoint(new(Kind.Pin, a, b, Transform.Identity, Transform.Identity));
        store.SolveConstraints(0.01f); store.Read([b], state);
        Check(state[0].Velocity == System.Numerics.Vector4.Zero, "Virtual surface motion does not drive a fixed pin anchor.");
        store.Remove(a); store.Remove(b);
        a = Body(store, inertia: 100);
        var definition = new Store.JointDefinition(Kind.Pin, a, default, Transform.Identity, Transform.Identity) { MotorEnabled = true, MotorVelocity = 100, MotorMaxTorque = 0.01f };
        joint = store.AddJoint(definition);
        store.Simulate(1f / 60, Vector2.Zero, substeps: 1, iterations: 64); store.Read([a], state);
        Near(state[0].Velocity.Z, -1f / 60, 0.00002f, "World pin torque cap uses N metres and opposite endpoint sign");
        store.SetJoint(joint, definition with { MotorVelocity = 2, MotorMaxTorque = 10, LimitEnabled = true, LowerAngle = -0.2f, UpperAngle = 0.3f });
        for (var i = 0; i < 300; i++) store.Simulate(1f / 60, Vector2.Zero, iterations: 32);
        store.Read([a], state);
        Check(state[0].Rotation >= -0.33f && state[0].Rotation <= 0.23f, "Pin motor respects both angular stops.");
        store.Remove(a);
        a = Body(store, new(40, 0), inertia: 200);
        store.AddJoint(new(Kind.Pin, a, default, new Transform(0, new(-40, 0)), Transform.Identity));
        for (var i = 0; i < 300; i++) store.Simulate(1f / 60, new(0, 980), iterations: 32);
        store.Read([a], state);
        var anchor = new Transform(state[0].Rotation, state[0].Position) * new Vector2(-40, 0);
        Console.WriteLine($"Resident pin: anchor error={anchor.Length():F5}, speed={state[0].Velocity}.");
        var energy = 0.5 * (state[0].Velocity.X * state[0].Velocity.X + state[0].Velocity.Y * state[0].Velocity.Y) + 100 * state[0].Velocity.Z * state[0].Velocity.Z - 980 * state[0].Position.Y;
        Check(energy < 490, "Pendulum final energy respects the anchor-error gravitational allowance.");
        Check(anchor.Length() < 0.5f, "Unshaped pendulum retains its off-center world anchor.");
        for (var i = 0; i < 128; i++)
        {
            var extra = Body(store, new(1000 + i * 10, 0));
            store.AddJoint(new(Kind.Pin, extra, default, Transform.Identity, new Transform(0, new(1000 + i * 10, 0))));
        }
        store.Simulate(1f / 30, new(0, 980), iterations: 32); store.Read([a], state);
        anchor = new Transform(state[0].Rotation, state[0].Position) * new Vector2(-40, 0);
        Check(anchor.Length() < 0.5f, "Device joint/history growth and changed timestep retain the original moving anchor.");
    }
    private static void VerifyGroove()
    {
        using var store = new Store();
        var a = Body(store, mode: Mode.Static); var b = Body(store, velocity: new(40, 20), angular: 2);
        var definition = new Store.JointDefinition(Kind.Groove, a, b, Transform.Identity, Transform.Identity) { UpperTranslation = 30 };
        var joint = store.AddJoint(definition); var state = new Store.Snapshot[1];
        for (var i = 0; i < 180; i++) store.Simulate(1f / 60, Vector2.Zero, iterations: 32);
        store.Read([b], state);
        Check(MathF.Abs(state[0].Position.Y) < 0.1f && state[0].Position.X >= -0.1f && state[0].Position.X <= 30.1f, "Finite groove confines translation.");
        Near(state[0].Velocity.Z, 2, 0.001f, "Groove leaves rotation free");
        store.SetJoint(joint, definition with { LowerTranslation = -20, UpperTranslation = -10 });
        for (var i = 0; i < 120; i++) store.Simulate(1f / 60, Vector2.Zero, iterations: 32);
        store.Read([b], state); Check(state[0].Position.X >= -20.1f && state[0].Position.X <= -9.9f, "Edited negative guide range pulls the old anchor inside.");
        store.SetJoint(joint, definition with { UpperTranslation = 0 });
        for (var i = 0; i < 120; i++) store.Simulate(1f / 60, Vector2.Zero, iterations: 32);
        store.Read([b], state); Check(state[0].Position.Length() < 0.1f, "Zero-length groove fixes translation without constraining rotation.");
        store.Remove(a); store.Remove(b);
        a = Body(store, mass: 2, inertia: 10); b = Body(store, new(10, 0), new(0, 6), mass: 3, inertia: 15);
        store.AddJoint(new(Kind.Groove, a, b, Transform.Identity, Transform.Identity) { LowerTranslation = -100, UpperTranslation = 100 });
        var pair = new Store.Snapshot[2]; store.SolveConstraints(0.01f, iterations: 64); store.Read([a, b], pair);
        var impulse = -6f / (0.5f + 1f / 3 + 10);
        Near(pair[0].Velocity.Z, -impulse, 0.001f, "Rotating guide includes its moving-axis lever arm");
        Near(pair[1].Velocity.Y - pair[0].Velocity.Y - 10 * pair[0].Velocity.Z, 0, 0.001f, "Guide anchor relative normal velocity vanishes");
        Near(2 * pair[0].Velocity.Y + 3 * pair[1].Velocity.Y, 18, 0.001f, "Free groove preserves linear momentum");
        Near(10 * pair[0].Velocity.Z + 30 * pair[1].Velocity.Y, 180, 0.001f, "Free groove preserves angular momentum about the origin");
        Console.WriteLine("Resident groove: endpoint limits, negative/zero intervals, edits and free rotation passed.");
    }
    private static void VerifySpring()
    {
        using var store = new Store();
        var a = Body(store, new(0, -2), mass: 2, inertia: 4); var b = Body(store, new(12, -2), mass: 3, inertia: 6);
        var frame = new Transform(0, new(0, 2));
        var definition = new Store.JointDefinition(Kind.DampedSpring, a, b, frame, frame) { RestLength = 10, Stiffness = 20, Damping = 0 };
        var joint = store.AddJoint(definition); var state = new Store.Snapshot[2];
        store.SolveConstraints(0.01f); store.Read([a, b], state);
        Near(state[0].Velocity.X, 0.2f, 0.0001f, "Hooke impulse at the first anchor");
        Near(state[1].Velocity.X, -0.4f / 3, 0.0001f, "Hooke impulse at the second anchor");
        Near(state[0].Velocity.Z, -0.2f, 0.0001f, "Off-center spring torque");
        Near(state[1].Velocity.Z, 0.8f / 6, 0.0001f, "Opposite off-center spring torque");
        store.SetVelocity(a, Vector2.Zero, 0); store.SetVelocity(b, new(10, 5), 0);
        store.SetJoint(joint, definition with { Stiffness = 0, Damping = 5 });
        store.SolveConstraints(0.02f); store.Read([a, b], state);
        var relative = state[1].Velocity.X - 2 * state[1].Velocity.Z - state[0].Velocity.X + 2 * state[0].Velocity.Z;
        Near(relative, 10 * MathF.Exp(-0.25f), 0.0002f, "Pure axial exponential damping includes angular effective mass");
        Near(state[1].Velocity.Y, 5, 0.0001f, "Tangential velocity is not damped");
        store.SetPose(b, new(0, -2), 0); store.SetVelocity(a, Vector2.Zero, 0); store.SetVelocity(b, Vector2.Zero, 0);
        store.SolveConstraints(0.02f); store.Read([a, b], state);
        Check(state.All(x => x.Velocity == System.Numerics.Vector4.Zero), "Coincident spring anchors supply no direction.");
        Console.WriteLine("Resident spring: Hooke impulse, torque, pure axial damping, tangent and coincident anchors passed.");
    }
    private static void VerifyLifecycle()
    {
        using var store = new Store(); using var other = new Store(); using var circle = new CircleShape { Radius = 2 };
        var a = Body(store); var b = Body(store, new(3, 0));
        var shape = store.AddShape(a, circle); store.AddShape(b, circle);
        var definition = new Store.JointDefinition(Kind.Pin, a, b, Transform.Identity, Transform.Identity);
        var first = store.AddJoint(definition); var second = store.AddJoint(definition);
        Check(store.FindPairs() == 0, "Connected body collision is disabled on the device.");
        store.SetShapeFilter(shape, 1, uint.MaxValue, true);
        Check(store.FindPairs() == 1, "Joint collision veto does not suppress sensor monitoring.");
        store.SetShapeFilter(shape, 1, uint.MaxValue, false);
        store.SetJoint(first, definition with { DisableCollision = false });
        Check(store.FindPairs() == 0, "A second joint retains its independent collision veto.");
        store.RemoveJoint(second); Check(store.FindPairs() == 1, "Removing the final veto restores existing overlapping pairs.");
        Reject<ArgumentException>(() => store.SetJoint(first, definition with { BodyB = Body(other) }));
        Reject<ArgumentOutOfRangeException>(() => store.SetJoint(first, definition with { Stiffness = float.NaN }));
        Check(!store.GetJointDefinition(first).DisableCollision, "Invalid replacement retains authored configuration.");
        Task.Run(() => Reject<InvalidOperationException>(() => store.RemoveJoint(first))).GetAwaiter().GetResult();
        store.Remove(a); Check(store.JointCount == 0, "Endpoint removal releases dependent device joints.");
        Reject<ArgumentException>(() => store.RemoveJoint(first));
        a = Body(store); first = store.AddJoint(definition with { BodyA = a });
        Reject<ArgumentException>(() => store.RemoveJoint(second));
        Check(first != second, "Reused slots retain generation-safe identity.");
        store.Remove(b); Check(store.JointCount == 0, "Second endpoint deletion unlinks the opposite incident list.");
        Console.WriteLine("Resident joints: collision contributions, identity, replacement rollback and endpoint cleanup passed.");
    }
    private static void VerifyContactsAndFailure()
    {
        using (var store = new Store())
        using (var circle = new CircleShape { Radius = 2 })
        using (var floor = new RectangleShape { Size = new(1000, 2) })
        {
            var a = Body(store, mode: Mode.Static); var b = Body(store, new(0, 10), new(50, 0));
            store.AddShape(b, circle, friction: 0);
            store.AddShape(Body(store, new(0, 30), mode: Mode.Static), floor, friction: 0);
            var groove = store.AddJoint(new(Kind.Groove, a, b, new Transform(MathF.PI / 2, Vector2.Zero), Transform.Identity) { UpperTranslation = 100 });
            var spring = store.AddJoint(new(Kind.DampedSpring, a, b, Transform.Identity, Transform.Identity) { RestLength = 50, Stiffness = 20, Damping = 1 });
            for (var i = 0; i < 300; i++) store.Simulate(1f / 60, new(0, 980), iterations: 32, allowedPenetration: 0.01f);
            var result = new Store.Snapshot[1]; store.Read([b], result);
            Check(MathF.Abs(result[0].Position.X) < 0.1f && MathF.Abs(result[0].Position.Y - 27) < 0.1f, "Contact supports a spring-loaded body inside a perpendicular groove.");
            Check(MathF.Abs(result[0].Velocity.Y) < 0.1f, "Coupled contact and joint response settles.");
            store.RemoveJoint(groove); store.RemoveJoint(spring); store.SetVelocity(b, new(30, 0), 0);
            for (var i = 0; i < 60; i++) store.Simulate(1f / 60, new(0, 980));
            store.Read([b], result); Check(result[0].Position.X > 20, "Removed joint rows cannot constrain subsequent contact-only solves.");
        }
        using (var store = new Store())
        {
            var a = Body(store); var b = Body(store, new(12, 0));
            store.AddJoint(new(Kind.DampedSpring, a, b, Transform.Identity, Transform.Identity) { RestLength = 0, Stiffness = float.MaxValue, Damping = 0 });
            Reject<InvalidOperationException>(() => store.SolveConstraints(1));
            Reject<InvalidOperationException>(() => store.Read([a], new Store.Snapshot[1]));
        }
        using (var store = new Store())
        {
            var a = Body(store, velocity: new(100, 0), mass: float.MaxValue / 10);
            var b = Body(store, velocity: new(-100, 0), mass: float.MaxValue / 10);
            store.AddJoint(new(Kind.Pin, a, b, Transform.Identity, Transform.Identity));
            Reject<InvalidOperationException>(() => store.SolveConstraints(0.01f));
            Reject<InvalidOperationException>(() => store.Read([a], new Store.Snapshot[1]));
        }
        Console.WriteLine("Resident joints: coupled spring/groove/contact support, final removal and failed-world rejection passed.");
    }
    private static void VerifyPopulation(bool limited)
    {
        const int count = 4096, warmup = 128, samples = 128;
        using var store = new Store();
        var bodies = new Store.BodyHandle[count];
        var joints = new Store.JointHandle[count];
        for (var i = 0; i < count; i++)
        {
            var origin = new Vector2(i % 64 * 40, i / 64 * 40);
            bodies[i] = Body(store, origin + new Vector2(10, 0), inertia: 20);
            joints[i] = store.AddJoint(new(Kind.Pin, bodies[i], default, new Transform(0, new(-10, 0)), new Transform(0, origin))
            { Bias = limited ? 0.3f : 0, MaxBias = limited ? 100 : float.MaxValue, MaxForce = limited ? 5000 : float.MaxValue, Softness = limited ? 0.001f : 0 });
        }
        for (var i = 0; i < warmup; i++) store.Simulate(1f / 120, new(0, 980));
        var times = new double[samples]; var edits = store.JointUploadBytes; var upload = store.UploadBytes; var download = store.ReadbackBytes; var uniforms = store.UniformBytes; var wait = store.WaitMS;
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < samples; i++)
        {
            var start = Stopwatch.GetTimestamp();
            store.SetJoint(joints[0], store.GetJointDefinition(joints[0]));
            store.Simulate(1f / 120, new(0, 980));
            times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Check(allocated == 0 && edits == store.JointUploadBytes, "Warmed joint solving has no managed allocations or authored joint upload.");
        var uploadPerTick = (store.UploadBytes - upload) / samples; var downloadPerTick = (store.ReadbackBytes - download) / samples;
        var uniformPerTick = (store.UniformBytes - uniforms) / samples; var waitPerTick = (store.WaitMS - wait) / samples;
        var state = new Store.Snapshot[count]; store.Read(bodies, state);
        var maxError = 0f;
        for (var i = 0; i < count; i++)
        {
            var anchor = new Transform(state[i].Rotation, state[i].Position) * new Vector2(-10, 0);
            maxError = MathF.Max(maxError, anchor.DistanceTo(new(i % 64 * 40, i / 64 * 40)));
        }
        Check(maxError < 0.3f, "Every independent device pin retains its world anchor.");
        Array.Sort(times);
        Console.WriteLine($"Resident joints: {count} pins, limited={limited}, {warmup} warmup/{samples} samples, 4 substeps/16 iterations; p50={times[samples / 2]:F4}, p95={times[(int)(samples * 0.95)]:F4}, p99={times[(int)(samples * 0.99)]:F4}, wait={waitPerTick:F4} ms, {allocated} managed B/tick, upload={uploadPerTick}, readback={downloadPerTick}, uniforms={uniformPerTick} B/tick, max anchor error={maxError:F5}; {store.Driver}, {store.DeviceName}.");
    }
    private static void Near(float value, float expected, float tolerance, string message) => Check(MathF.Abs(value - expected) <= tolerance, $"{message}: {value} vs {expected}");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
