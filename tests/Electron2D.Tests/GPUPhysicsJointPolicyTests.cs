using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Kind = Electron2D.PhysicsServer.JointType;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class GPUPhysicsJointPolicyTests
{
    internal static void Run()
    {
        VerifyBias(); VerifySoftness(); VerifyCaps(); VerifyCombinedBudget(); VerifyContinuousBudget(); VerifyEdits();
        Console.WriteLine("Resident joint policies: bias, vector correction limits, anchor softness, accumulated force budgets, CCD and edits passed.");
    }
    private static Store.BodyHandle Body(Store s, Vector2 at = default, Vector2 velocity = default, float mass = 1, float inertia = 1, Mode mode = Mode.Rigid) =>
        s.Add(new(mode, at, 0, velocity, 0, mass, inertia, CanSleep: false));
    private static Store.Snapshot Read(Store s, Store.BodyHandle b)
    { Span<Store.Snapshot> data = stackalloc Store.Snapshot[1]; s.Read([b], data); return data[0]; }
    private static Vector2 Velocity(Store.Snapshot b) => new(b.Velocity.X, b.Velocity.Y);
    private static void Step(Store s, float dt = 0.01f, int substeps = 1) =>
        s.Simulate(dt, Vector2.Zero, substeps: substeps, iterations: 64, correctionFactor: 0.1f, maxCorrectionSpeed: 100_000);
    private static void VerifyBias()
    {
        foreach (var groove in new[] { false, true })
        {
            using var s = new Store();
            var origin = groove ? new Vector2(-10, 10) : new Vector2(10, 10);
            var b = Body(s, origin, mode: Mode.RigidLinear);
            var a = groove ? Body(s, mode: Mode.Static) : b;
            var d = new Store.JointDefinition(groove ? Kind.Groove : Kind.Pin, a, groove ? b : default, Transform.Identity, Transform.Identity);
            var joint = s.AddJoint(d);
            foreach (var contactBias in new[] { 0f, 1f })
            {
                s.SetContactSettings(new(contactBias, .3f)); s.SetPose(b, origin, 0);
                s.Simulate(.01f, Vector2.Zero, substeps: 1, iterations: 64, maxCorrectionSpeed: 100_000);
                Near(Read(s, b).Position, origin * (1 - ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultConstraintBias)),
                    .0002f, "Contact policy does not change inherited joint bias");
            }
            s.SetPose(b, origin, 0);
            Step(s); Near(Read(s, b).Position, origin * 0.9f, 0.0002f, "Zero bias inherits the world correction fraction");
            s.SetJoint(joint, d with { Bias = 0.25f }); s.SetPose(b, origin, 0); Step(s);
            Near(Read(s, b).Position, origin * 0.75f, 0.0002f, "Per-joint bias controls position correction");
            s.SetJoint(joint, d with { Bias = 1, MaxBias = 5 }); s.SetPose(b, origin, 0); Step(s);
            Near((Read(s, b).Position - origin).Length(), 0.05f, 0.0002f, "MaxBias caps vector speed, not each component");
            s.SetJoint(joint, d with { Bias = 1, MaxBias = 0 }); s.SetPose(b, origin, 0); Step(s);
            Near(Read(s, b).Position, origin, 0.0002f, "Zero correction cap disables positional recovery");
        }
        using (var s = new Store())
        {
            var b = Body(s); s.SetPose(b, Vector2.Zero, 0.5f);
            var d = new Store.JointDefinition(Kind.Pin, b, default, Transform.Identity, Transform.Identity)
            { LimitEnabled = true, LowerAngle = -0.1f, UpperAngle = 0.1f, Bias = 0.25f };
            var j = s.AddJoint(d); Step(s);
            Near(Read(s, b).Rotation, 0.4f, 0.0002f, "The inactive angular stop does not cancel correction at the violated limit");
            s.SetJoint(j, d with { MaxBias = 2 }); s.SetPose(b, Vector2.Zero, 0.5f); Step(s);
            Near(Read(s, b).Rotation, 0.48f, 0.0002f, "Angular correction obeys its speed cap in radians per second");
        }
    }
    private static void VerifySoftness()
    {
        using var s = new Store();
        var a = Body(s, velocity: new(12, -3)); var b = Body(s, mass: 3);
        var d = new Store.JointDefinition(Kind.Pin, a, b, Transform.Identity, Transform.Identity) { Softness = 2 };
        var joint = s.AddJoint(d);
        s.SetVelocity(a, new(12, -3), 2); s.SetVelocity(b, Vector2.Zero, -1);
        for (var repeat = 0; repeat < 2; repeat++)
        {
            var beforeA = Velocity(Read(s, a)); var beforeB = Velocity(Read(s, b));
            var impulse = (beforeA - beforeB) / (1 + 1f / 3 + 2);
            s.SolveConstraints(0.01f, iterations: 64);
            Near(Velocity(Read(s, a)), beforeA - impulse, 0.0002f, "Soft pin solves inverse mass plus anchor softness");
            Near(Velocity(Read(s, b)), beforeB + impulse / 3, 0.0002f, "Soft pin preserves pair momentum with warm history");
            Near(Read(s, a).Velocity.Z, 2, 0.0001f, "Anchor softness does not damp free rotation");
        }
        s.SetJoint(joint, d with { Softness = 0 }); s.SetVelocity(a, new(12, -3), 0); s.SetVelocity(b, Vector2.Zero, 0);
        s.SolveConstraints(0.01f, iterations: 64); Near(Velocity(Read(s, a)), new(3, -0.75f), 0.0002f, "Zero softness restores the rigid anchor");
    }
    private static void VerifyCaps()
    {
        foreach (var groove in new[] { false, true })
        {
            using var s = new Store();
            var v = new Vector2(groove ? -6 : 6, 8); var b = Body(s, velocity: v, mode: Mode.RigidLinear);
            var a = groove ? Body(s, mode: Mode.Static) : b;
            var d = new Store.JointDefinition(groove ? Kind.Groove : Kind.Pin, a, groove ? b : default, Transform.Identity, Transform.Identity)
            { MaxForce = 10, MaxBias = 0, UpperTranslation = 0 };
            var joint = s.AddJoint(d);
            foreach (var iterations in new[] { 32, 96 })
            {
                s.SetVelocity(b, v, 0); s.SolveConstraints(0.1f, iterations: iterations);
                Near(Velocity(Read(s, b)), v * 0.9f, 0.002f, "Accumulated two-axis impulse is limited to force times duration");
            }
            s.SetJoint(joint, d with { MaxForce = 0 }); s.SetVelocity(b, v, 0); Step(s);
            Near(Velocity(Read(s, b)), v, 0.0002f, "Zero force discards old warm impulses");
        }
        using (var s = new Store())
        {
            var a = Body(s); var b = Body(s, new(10, 0));
            var d = new Store.JointDefinition(Kind.DampedSpring, a, b, Transform.Identity, Transform.Identity)
            { RestLength = 0, Stiffness = 100, Damping = 0, MaxForce = 10 };
            var j = s.AddJoint(d); s.SolveConstraints(0.1f);
            Near(Read(s, a).Velocity.X, 1, 0.0002f, "Spring total axial impulse uses its force cap");
            Near(Read(s, b).Velocity.X, -1, 0.0002f, "Capped spring preserves momentum");
            s.SetJoint(j, d with { Stiffness = 0, Damping = 100 }); s.SetVelocity(a, Vector2.Zero, 0); s.SetVelocity(b, new(100, 0), 0);
            s.SolveConstraints(0.1f); Near(Read(s, a).Velocity.X, 1, 0.0002f, "Pure damper shares the same axial force budget");
        }
        using (var s = new Store())
        {
            var b = Body(s, inertia: 2);
            s.AddJoint(new(Kind.Pin, b, default, Transform.Identity, Transform.Identity) { MotorEnabled = true, MotorVelocity = 100, MotorMaxTorque = 1, MaxForce = 10 });
            s.SolveConstraints(0.1f, iterations: 64);
            Near(Read(s, b).Velocity.Z, -0.5f, 0.0002f, "General angular impulse cap also bounds a stronger motor");
        }
    }
    private static void VerifyCombinedBudget()
    {
        foreach (var substeps in new[] { 1, 4 })
        {
            using var s = new Store(); var at = new Vector2(10, 10); var v = new Vector2(6, 8);
            var b = Body(s, at, v, mode: Mode.RigidLinear);
            s.AddJoint(new(Kind.Pin, b, default, Transform.Identity, Transform.Identity) { MaxForce = 10 });
            Step(s, 0.1f, substeps); var state = Read(s, b);
            Check((v - Velocity(state)).Length() <= 1.001f, "Substeps do not multiply the total force budget.");
            if (substeps == 1)
            {
                var correction = (state.Position - at) / 0.1f - Velocity(state);
                Near((v - Velocity(state)).Length() + correction.Length(), 1, 0.001f, "Physical and positional impulses share one linear budget");
            }
        }
    }
    private static void VerifyContinuousBudget()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 1 }; using var wall = new RectangleShape { Size = new(0.2f, 100) };
        var projectile = s.Add(new(Mode.Rigid, Vector2.Zero, 0, new(3000, 0), 0, CanSleep: false, ContinuousMode: CCDMode.CastShape));
        s.AddShape(projectile, circle, friction: 0, bounce: 0.5f); s.AddShape(Body(s, new(20, 0), mode: Mode.Static), wall, friction: 0, bounce: 0.5f);
        var b = Body(s, new(0, 1000), new(100, 0));
        s.AddJoint(new(Kind.Pin, b, default, Transform.Identity, new Transform(0, new(0, 1000))) { MaxForce = 10, MaxBias = 0 });
        Step(s, 0.02f);
        Check(s.CCDIntervalCount > 0, "The force-budget check exercised a real additional impact solve.");
        Near(Read(s, b).Velocity.X, 99.8f, 0.0005f, "CCD impacts cannot spend a joint's original substep budget again");
    }
    private static void VerifyEdits()
    {
        using var s = new Store(); var b = Body(s); var d = new Store.JointDefinition(Kind.Pin, b, default, Transform.Identity, Transform.Identity);
        var j = s.AddJoint(d);
        foreach (var bad in new[] { d with { Bias = float.NaN }, d with { Bias = -1 }, d with { Bias = 1.01f }, d with { MaxBias = -1 }, d with { MaxForce = float.PositiveInfinity }, d with { Softness = -1 } })
        {
            var rejected = false; try { s.SetJoint(j, bad); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected && s.GetJointDefinition(j) == d, "Invalid policy edit preserves the prior definition.");
        }
        var policy = d with { MaxForce = 10, Softness = 2, Bias = 0.4f, MaxBias = 5 };
        for (var i = 0; i < 128; i++) { s.SetJoint(j, policy with { MaxForce = 10 + i % 2 }); s.SetVelocity(b, new(6, 8), 0); Step(s); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) { s.SetJoint(j, policy with { MaxForce = 10 + i % 2 }); s.SetVelocity(b, new(6, 8), 0); Step(s); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed limited soft-joint steps and real joint/body edits allocate zero managed bytes.");
        s.RemoveJoint(j); Step(s);
        var replacement = s.AddJoint(d); Check(s.GetJointDefinition(replacement) == d && replacement != j, "Reused joint slots reset all policies and budgets.");
    }
    private static void Near(Vector2 actual, Vector2 expected, float tolerance, string message) => Near(actual.DistanceTo(expected), 0, tolerance, message);
    private static void Near(float actual, float expected, float tolerance, string message) => Check(MathF.Abs(actual - expected) <= tolerance, $"{message}: {actual} vs {expected}");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
