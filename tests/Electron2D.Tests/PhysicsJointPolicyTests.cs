using Electron2D;

internal sealed class PhysicsJointPolicyTests(bool gpu, PhysicsServer.Backend backend)
{
    internal static void Run(bool gpu = false) => new PhysicsJointPolicyTests(gpu, PhysicsServer.Backend.CPU).RunCore();
    internal static void Run(PhysicsServer.Backend backend) => new PhysicsJointPolicyTests(false, backend).RunCore();
    private void RunCore()
    {
        VerifyParametersAndPacking(); VerifyBias(gpu); VerifySoftness(gpu); VerifyLimits(gpu); VerifyFiniteSpringCaps(gpu); VerifyKinematicBudget(gpu); VerifyLifetime(gpu);
        Console.WriteLine($"Public joint policies passed on {(gpu ? "CPU host/GPU stages" : backend.ToString())}: defaults, bias, softness, force/correction caps, packing, lifecycle and allocation.");
    }
    private sealed class Rig : IDisposable
    {
        internal readonly RID Space, A = PhysicsServer.BodyCreate(), B = PhysicsServer.BodyCreate(), Joint = PhysicsServer.JointCreate();
        internal Rig(bool gpu, PhysicsServer.Backend backend)
        {
            Space = PhysicsServer.SpaceCreate(backend);
            Check(PhysicsServer.SpaceGetBackend(Space) == backend, "Joint policies use the selected public backend");
            PhysicsServer.SpaceSetActive(Space, true);
            if (gpu) PhysicsServer.Service.GetSceneSpace(Space).EnableGPUSolver();
            foreach (var body in new[] { A, B })
            {
                PhysicsServer.BodySetCanSleep(body, false); PhysicsServer.BodySetGravityScale(body, 0);
                PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(body, 0);
                PhysicsServer.BodySetAngularDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(body, 0);
                PhysicsServer.BodySetInertia(body, 1); PhysicsServer.BodySetSpace(body, Space);
            }
            PhysicsServer.BodySetMode(A, PhysicsServer.BodyMode.Static);
        }
        internal void Pin() => PhysicsServer.JointMakePin(Joint, Vector2.Zero, A, B);
        internal void Step(double dt = 0.04) => PhysicsServer.SpaceStep(Space, dt);
        internal Vector2 Position => PhysicsServer.BodyGetTransform(B).Origin;
        internal Vector2 Velocity => PhysicsServer.BodyGetLinearVelocity(B);
        public void Dispose() { PhysicsServer.FreeRID(Joint); PhysicsServer.FreeRID(A); PhysicsServer.FreeRID(B); PhysicsServer.FreeRID(Space); }
    }
    private void VerifyParametersAndPacking()
    {
        using var pin = new PinJoint(); var rid = pin.GetRID();
        Check(pin.Bias == 0 && pin.Softness == 0 && pin.MaxBias == float.MaxValue && pin.MaxForce == float.MaxValue, "Scene defaults retain zero bias/softness and unlimited caps.");
        PhysicsServer.JointSetBias(rid, 0.4f); PhysicsServer.JointSetMaxBias(rid, 9); PhysicsServer.JointSetMaxForce(rid, 20); PhysicsServer.PinJointSetSoftness(rid, 2);
        Check(pin.Bias == 0.4f && pin.MaxBias == 9 && pin.MaxForce == 20 && pin.Softness == 2, "Server writes update the scene projection.");
        pin.Bias = 0.3f; pin.MaxBias = 8; pin.MaxForce = 30; pin.Softness = 3;
        Check(PhysicsServer.JointGetBias(rid) == 0.3f && PhysicsServer.JointGetMaxBias(rid) == 8 && PhysicsServer.JointGetMaxForce(rid) == 30 && PhysicsServer.PinJointGetSoftness(rid) == 3, "Scene writes update the server projection.");
        using var packed = new PackedScene(); packed.Pack(pin);
        using var copy = (PinJoint)packed.Instantiate();
        Check(copy.Bias == pin.Bias && copy.MaxBias == pin.MaxBias && copy.MaxForce == pin.MaxForce && copy.Softness == pin.Softness, "PackedScene retains every joint policy.");
        foreach (var invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
        {
            Reject<ArgumentOutOfRangeException>(() => pin.Bias = invalid); Reject<ArgumentOutOfRangeException>(() => pin.MaxBias = invalid);
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.JointSetMaxForce(rid, invalid)); Reject<ArgumentOutOfRangeException>(() => pin.Softness = invalid);
        }
        Reject<ArgumentOutOfRangeException>(() => pin.Bias = 1.01f);
        Check(pin.Bias == 0.3f && pin.MaxBias == 8 && pin.MaxForce == 30 && pin.Softness == 3, "Invalid policy writes preserve all prior settings.");
        using var groove = new GrooveJoint(); Reject<ArgumentException>(() => PhysicsServer.PinJointSetSoftness(groove.GetRID(), 0));
        var old = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultConstraintBias);
        try
        {
            ProjectSettings.Set(ProjectSettings.Physics2DDefaultConstraintBias, 0.35f);
            using var r = new Rig(false, backend); Near(PhysicsServer.SpaceGetConstraintDefaultBias(r.Space), 0.35f, 0, "New spaces capture the project constraint default");
            ProjectSettings.Set(ProjectSettings.Physics2DDefaultConstraintBias, 0.7f);
            Near(PhysicsServer.SpaceGetConstraintDefaultBias(r.Space), 0.35f, 0, "Changing the project default does not overwrite an existing space");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DDefaultConstraintBias, old); }
    }
    private void VerifyBias(bool gpu)
    {
        foreach (var groove in new[] { false, true })
        {
            using var r = new Rig(gpu, backend);
            if (groove) PhysicsServer.JointMakeGroove(r.Joint, Vector2.Zero, new(50, 0), Vector2.Zero, r.A, r.B); else r.Pin();
            var position = new Vector2(groove ? -1 : 1, 1);
            PhysicsServer.SpaceSetConstraintDefaultBias(r.Space, 0.1f);
            PhysicsServer.BodySetTransform(r.B, new Transform(0, position)); r.Step();
            Near(r.Position, position * MathF.Pow(0.9f, 4), 0.002f, "Zero joint bias inherits the world's fraction on every substep");
            PhysicsServer.JointSetBias(r.Joint, 0.25f); PhysicsServer.BodySetTransform(r.B, new Transform(0, position)); PhysicsServer.BodySetLinearVelocity(r.B, Vector2.Zero); r.Step();
            Near(r.Position, position * MathF.Pow(0.75f, 4), 0.002f, "Explicit bias overrides the world default");
            PhysicsServer.JointSetMaxBias(r.Joint, 5); PhysicsServer.BodySetTransform(r.B, new Transform(0, position)); PhysicsServer.BodySetLinearVelocity(r.B, Vector2.Zero); r.Step();
            Near((r.Position - position).Length(), 0.2f, 0.002f, "Diagonal correction shares one speed cap in scene units");
            PhysicsServer.JointSetMaxBias(r.Joint, 0); PhysicsServer.BodySetTransform(r.B, new Transform(0, position)); PhysicsServer.BodySetLinearVelocity(r.B, Vector2.Zero); r.Step();
            Near(r.Position, position, 0.002f, "Zero correction limit retains position while removing relative velocity");
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.SpaceSetConstraintDefaultBias(r.Space, float.NaN));
            Near(PhysicsServer.SpaceGetConstraintDefaultBias(r.Space), 0.1f, 0, "Invalid space bias preserves the previous value");
        }
        using (var r = new Rig(gpu, backend))
        {
            r.Pin(); PhysicsServer.PinJointSetAngularLimitLower(r.Joint, -0.1f); PhysicsServer.PinJointSetAngularLimitUpper(r.Joint, 0.1f);
            PhysicsServer.PinJointSetAngularLimitEnabled(r.Joint, true); PhysicsServer.JointSetBias(r.Joint, 0.25f);
            PhysicsServer.BodySetTransform(r.B, new Transform(0.5f, Vector2.Zero)); r.Step();
            Near(PhysicsServer.BodyGetTransform(r.B).Rotation, 0.1f + 0.4f * MathF.Pow(0.75f, 4), 0.002f, "Angular limits use the same positional bias fraction");
            PhysicsServer.JointSetMaxBias(r.Joint, 2); PhysicsServer.BodySetTransform(r.B, new Transform(0.5f, Vector2.Zero)); PhysicsServer.BodySetAngularVelocity(r.B, 0); r.Step();
            Near(PhysicsServer.BodyGetTransform(r.B).Rotation, 0.42f, 0.002f, "Angular correction cap uses radians per second");
        }
    }
    private void VerifySoftness(bool gpu)
    {
        using var r = new Rig(gpu, backend); PhysicsServer.BodySetMode(r.A, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetMass(r.B, 3); r.Pin();
        PhysicsServer.JointSetMaxBias(r.Joint, 0); PhysicsServer.PinJointSetSoftness(r.Joint, 2);
        PhysicsServer.BodySetLinearVelocity(r.A, new(12, -3)); r.Step();
        var relative = new Vector2(12, -3) * MathF.Pow(0.6f, 4); var center = new Vector2(3, -0.75f);
        Near(PhysicsServer.BodyGetLinearVelocity(r.A), center + 0.75f * relative, 0.002f, "Soft pin follows the inverse-mass equation over four substeps");
        Near(r.Velocity, center - 0.25f * relative, 0.002f, "Soft pin preserves pair momentum");
        PhysicsServer.PinJointSetSoftness(r.Joint, 0); PhysicsServer.BodySetLinearVelocity(r.A, new(12, -3)); PhysicsServer.BodySetLinearVelocity(r.B, Vector2.Zero); r.Step();
        Near(r.Velocity, center, 0.002f, "Zero softness restores a rigid anchor");
    }
    private void VerifyLimits(bool gpu)
    {
        foreach (var groove in new[] { false, true })
        {
            using var r = new Rig(gpu, backend);
            if (groove) PhysicsServer.JointMakeGroove(r.Joint, Vector2.Zero, Vector2.Zero, Vector2.Zero, r.A, r.B); else r.Pin();
            PhysicsServer.JointSetMaxBias(r.Joint, 0); PhysicsServer.JointSetMaxForce(r.Joint, 10);
            var v = new Vector2(6, 8); PhysicsServer.BodySetLinearVelocity(r.B, v); r.Step(0.1);
            Near(r.Velocity, v * 0.9f, 0.003f, "Four substeps share the expected force-times-duration impulse");
            PhysicsServer.JointSetMaxForce(r.Joint, 0); PhysicsServer.BodySetLinearVelocity(r.B, v); r.Step(0.1);
            Near(r.Velocity, v, 0.001f, "Zero force discards the old warm impulse");
        }
        using (var r = new Rig(gpu, backend))
        {
            r.Pin(); PhysicsServer.BodySetInertia(r.B, 2); PhysicsServer.JointSetMaxForce(r.Joint, 10);
            PhysicsServer.PinJointSetMotorEnabled(r.Joint, true); PhysicsServer.PinJointSetMotorTargetVelocity(r.Joint, 100); r.Step(0.1);
            Near(PhysicsServer.BodyGetAngularVelocity(r.B), 0.5f, 0.002f, "General angular force cap uses squared scene units independently of the motor N-m cap");
        }
        using (var r = new Rig(gpu, backend))
        {
            PhysicsServer.BodySetTransform(r.B, new Transform(0, new(10, 0)));
            PhysicsServer.JointMakeDampedSpring(r.Joint, Vector2.Zero, new(10, 0), r.A, r.B);
            PhysicsServer.DampedSpringJointSetRestLength(r.Joint, 0); PhysicsServer.DampedSpringJointSetStiffness(r.Joint, 100); PhysicsServer.DampedSpringJointSetDamping(r.Joint, 0); PhysicsServer.JointSetMaxForce(r.Joint, 10);
            r.Step(0.1); Near(r.Velocity.X, -1, 0.002f, "The spring's combined impulse is force capped");
        }
    }
    private void VerifyFiniteSpringCaps(bool gpu)
    {
        using var r = new Rig(gpu, backend);
        PhysicsServer.BodySetTransform(r.B, new(0, new(10, 0)));
        PhysicsServer.JointMakeDampedSpring(r.Joint, Vector2.Zero, new(10, 0), r.A, r.B);
        PhysicsServer.DampedSpringJointSetStiffness(r.Joint, float.MaxValue);
        PhysicsServer.DampedSpringJointSetDamping(r.Joint, 0); PhysicsServer.JointSetMaxForce(r.Joint, 10);
        foreach (var rest in new[] { 0f, 10_000_000f })
        {
            PhysicsServer.BodySetTransform(r.B, new(0, new(10, 0))); PhysicsServer.BodySetLinearVelocity(r.B, Vector2.Zero);
            PhysicsServer.DampedSpringJointSetRestLength(r.Joint, rest); r.Step(.1);
            Near(r.Velocity.X, rest == 0 ? -1 : 1, .002f, "A finite spring force cap bounds either sign of extreme authored stiffness");
        }
        PhysicsServer.BodySetTransform(r.B, new(0, new(10, 0))); PhysicsServer.BodySetLinearVelocity(r.B, new(1, 0));
        PhysicsServer.DampedSpringJointSetDamping(r.Joint, float.MaxValue); r.Step(.1);
        Near(r.Velocity, Vector2.Zero, .002f, "Complete axial decay suppresses the elastic term before applying the finite impulse cap");
        PhysicsServer.DampedSpringJointSetDamping(r.Joint, 0); PhysicsServer.JointSetMaxForce(r.Joint, 0);
        PhysicsServer.BodySetLinearVelocity(r.B, new(6, 8)); r.Step(.1);
        Near(r.Velocity, new(6, 8), .001f, "Zero force supplies zero spring impulse even for extreme finite coefficients");
    }

    private void VerifyKinematicBudget(bool gpu)
    {
        using var shape = new CircleShape { Radius = 1 };
        using var r = new Rig(gpu, backend);
        PhysicsServer.BodyAddShape(r.A, shape.GetRID()); PhysicsServer.BodyAddShape(r.B, shape.GetRID());
        PhysicsServer.BodySetMode(r.A, PhysicsServer.BodyMode.Kinematic); PhysicsServer.BodySetTransform(r.A, Transform.Identity); r.Pin();
        PhysicsServer.JointSetMaxBias(r.Joint, 0); PhysicsServer.JointSetMaxForce(r.Joint, 10);
        PhysicsServer.BodySetTransform(r.A, new Transform(0, new(20, 0))); r.Step(0.1);
        Near(PhysicsServer.BodyGetTransform(r.A).Origin.X, 20, 0.003f, "The kinematic endpoint consumes its full authored interval");
        Near(r.Velocity.X, 1, 0.003f, "Kinematic path subdivisions do not multiply the joint force allowance");
    }
    private void VerifyLifetime(bool gpu)
    {
        using var r = new Rig(gpu, backend); r.Pin(); PhysicsServer.JointSetBias(r.Joint, 0.3f); PhysicsServer.JointSetMaxBias(r.Joint, 5); PhysicsServer.JointSetMaxForce(r.Joint, 10); PhysicsServer.PinJointSetSoftness(r.Joint, 2);
        Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.JointSetMaxForce(r.Joint, 0)).GetAwaiter().GetResult());
        PhysicsServer.BodySetCanSleep(r.B, true); PhysicsServer.BodySetSleeping(r.B, true); PhysicsServer.PinJointSetSoftness(r.Joint, 3);
        Check(!PhysicsServer.BodyGetSleeping(r.B), "A live policy edit wakes the connected sleeping body.");
        PhysicsServer.BodySetSpace(r.B, default); PhysicsServer.BodySetSpace(r.B, r.Space);
        Check(PhysicsServer.PinJointGetSoftness(r.Joint) == 3 && PhysicsServer.JointGetBias(r.Joint) == 0.3f, "Reattachment preserves policy and identity.");
        PhysicsServer.JointClear(r.Joint); Check(PhysicsServer.JointGetMaxForce(r.Joint) == 10 && PhysicsServer.JointGetMaxBias(r.Joint) == 5, "Clear preserves general settings.");
        r.Pin(); Check(PhysicsServer.PinJointGetSoftness(r.Joint) == 0 && PhysicsServer.JointGetBias(r.Joint) == 0.3f, "Concrete replacement resets only pin-specific softness.");
        PhysicsServer.BodySetCanSleep(r.B, false);
        for (var i = 0; i < 128; i++) { PhysicsServer.JointSetMaxForce(r.Joint, 10 + i % 2); PhysicsServer.BodySetLinearVelocity(r.B, new(6, 8)); r.Step(); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) { PhysicsServer.JointSetMaxForce(r.Joint, 10 + i % 2); PhysicsServer.BodySetLinearVelocity(r.B, new(6, 8)); r.Step(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warm public policy edits and physical frames allocate zero owner-thread managed bytes.");
    }
    private static void Near(Vector2 actual, Vector2 expected, float tolerance, string message) => Near(actual.DistanceTo(expected), 0, tolerance, message);
    private static void Near(float actual, float expected, float tolerance, string message) => Check(MathF.Abs(actual - expected) <= tolerance, $"{message}: {actual} vs {expected}");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
