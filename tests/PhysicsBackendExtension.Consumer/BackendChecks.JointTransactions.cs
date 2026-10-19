using Electron2D;

public static partial class BackendChecks
{
    public static void RunJointTransactions(bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null; var cases = 0;
        PhysicsServerManager.RegisterServer("JointTransactions", () => solver = new());
        try
        {
            for (var role = 0; role < 3; role++)
                for (var field = 0; field <= 13; field++)
                {
                    if (field is >= 3 and <= 9 && role != 0 || field is >= 10 and <= 12 && role != 2 || field == 13 && role != 1) continue;
                    for (var scene = field == 13 ? 1 : 0; scene < 2; scene++)
                        for (var after = 0; after < 2; after++)
                            for (var trigger = 0; trigger < 6; trigger++) Run(role, field, scene != 0, after != 0, trigger);
                }
            Console.WriteLine($"Joint parameter transactions/{threaded}: {cases} role/field/route/before-after/recovery cases passed.");
            JointTransactionLifetime(threaded);
            JointTransactionFailures(threaded);
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }

        void Run(int role, int field, bool scene, bool after, int trigger)
        {
            using var world = new World("JointTransactions"); using var root = new SubViewport { World = world };
            var first = new RigidBody { Name = "A", GravityScale = 0, CanSleep = false }; var second = new StaticBody { Name = "B" };
            root.AddChild(first); root.AddChild(second);
            Joint? node = scene ? role switch { 0 => new PinJoint(), 1 => new GrooveJoint(), _ => new DampedSpringJoint() } : null;
            if (node is not null) { node.NodeA = "../A"; node.NodeB = "../B"; root.AddChild(node); }
            using var tree = new SceneTree(root); var joint = node?.GetRID() ?? PhysicsServer.JointCreate(); var owner = Thread.CurrentThread;
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            try
            {
                if (node is null) MakeJoint(joint, role, first.GetRID(), second.GetRID());
                if (role == 0) { PhysicsServer.PinJointSetAngularLimitLower(joint, -.5f); PhysicsServer.PinJointSetAngularLimitUpper(joint, .5f); }
                Set(false);
                using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space); using var map = new PhysicsSnapshotMap(world.Space);
                map.Bind(1, 1, first.GetRID()); map.Bind(2, 1, second.GetRID()); map.Bind(3, 1, joint); var snapshot = new PhysicsSnapshot();
                var failure = new ApplicationException("joint-parameter-publication"); var calls = 0; var expected = true; var consistent = true; var guarded = true;
                solver!.StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "Joint parameter recovery executes on the actual selected numerical thread");
                Action<RID> hook = id =>
                {
                    if (id != joint) return; calls++; consistent &= Read() == JointPolicyValue(field, expected);
                    Reject<InvalidOperationException>(() => Set(expected));
                    Exception? error = null; try { world.DirectSpaceState.IntersectPoint(point, hits); } catch (Exception caught) { error = caught; }
                    guarded &= error is InvalidOperationException; throw failure;
                };
                if (after) solver.JointHook = hook; else solver.JointParameterHook = hook;
                var errors = JointParameterErrors(() => Set(true));
                Check(errors.Length == 1 && ReferenceEquals(errors[0], failure) && Read() == JointPolicyValue(field, true), "Failed parameter publication retains the complete requested value and original error");
                var tick = PhysicsServer.SpaceGetTick(world.Space); tree.PhysicsFrame(0);
                PhysicsServer.SpaceSetActive(world.Space, false); tree.PhysicsFrame(.01); PhysicsServer.SpaceSetActive(world.Space, true);
                Check(calls == 1 && PhysicsServer.SpaceGetTick(world.Space) == tick, "Skipped frames do not publish pending parameters or solve");
                expected = trigger != 5; errors = JointParameterErrors(Attempt);
                if (errors.Length != 1 || !ReferenceEquals(errors[0], failure)) throw new InvalidOperationException("Joint parameter retry skipped failed publication.");
                Check(Read() == JointPolicyValue(field, expected) && PhysicsServer.SpaceGetTick(world.Space) == tick, "Repeated publication errors preserve latest request and completed tick");
                Check(calls == 2 && consistent && guarded, "Borrowed parameter hooks retain configured reads and reject writes/live query reentry");
                solver.JointHook = solver.JointParameterHook = null; Attempt();
                Check(Read() == JointPolicyValue(field, expected) && SelectedJointPolicy(solver.Joints[joint].Settings, field) == Read(), "Successful recovery publishes the latest complete retained parameter");
                if (trigger != 4) tree.PhysicsFrame(.01);
                Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1, "Parameter recovery advances exactly one actual solver interval"); cases++;
                void Attempt()
                {
                    switch (trigger)
                    {
                        case 0: Set(true); break;
                        case 1: Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0, "Recovered query has no unrelated geometry"); break;
                        case 2: checkpoint.Capture(); break;
                        case 3: map.Capture(snapshot); break;
                        case 4: tree.PhysicsFrame(.01); break;
                        default: Set(false); break;
                    }
                }
                void Set(bool value) => SetJointPolicy(joint, node, field, JointPolicyValue(field, value));
                float Read() => ReadJointPolicy(joint, node, field);
            }
            finally
            {
                solver!.JointHook = solver.JointParameterHook = null;
                if (node is null) PhysicsServer.FreeRID(joint); else node.Dispose();
            }
        }
    }

    private static Exception[] JointParameterErrors(Action action)
    {
        try { action(); return []; }
        catch (AggregateException error) { return [.. error.Flatten().InnerExceptions]; }
        catch (Exception error) { return [error]; }
    }

    private static float JointPolicyValue(int field, bool changed) => field switch
    {
        0 or 3 or 11 or 12 => changed ? .2f : .1f,
        1 or 2 or 10 => changed ? 9 : 5,
        4 or 7 => changed ? 1 : 0,
        5 => changed ? -.1f : -.25f,
        6 => changed ? .4f : .25f,
        8 or 9 => changed ? 2 : 1,
        _ => changed ? -20 : 20
    };

    private static void SetJointPolicy(RID id, Joint? node, int field, float value)
    {
        if (node is not null)
        {
            switch (field)
            {
                case 0: node.Bias = value; return;
                case 1: node.MaxBias = value; return;
                case 2: node.MaxForce = value; return;
                case 3: ((PinJoint)node).Softness = value; return;
                case 4: ((PinJoint)node).AngularLimitEnabled = value != 0; return;
                case 5: ((PinJoint)node).AngularLimitLower = value; return;
                case 6: ((PinJoint)node).AngularLimitUpper = value; return;
                case 7: ((PinJoint)node).MotorEnabled = value != 0; return;
                case 8: ((PinJoint)node).MotorTargetVelocity = value; return;
                case 9: ((PinJoint)node).MotorMaxTorque = value; return;
                case 10: ((DampedSpringJoint)node).RestLength = value; return;
                case 11: ((DampedSpringJoint)node).Stiffness = value; return;
                case 12: ((DampedSpringJoint)node).Damping = value; return;
                default: ((GrooveJoint)node).Length = value; return;
            }
        }
        switch (field)
        {
            case 0: PhysicsServer.JointSetBias(id, value); break;
            case 1: PhysicsServer.JointSetMaxBias(id, value); break;
            case 2: PhysicsServer.JointSetMaxForce(id, value); break;
            case 3: PhysicsServer.PinJointSetSoftness(id, value); break;
            case 4: PhysicsServer.PinJointSetAngularLimitEnabled(id, value != 0); break;
            case 5: PhysicsServer.PinJointSetAngularLimitLower(id, value); break;
            case 6: PhysicsServer.PinJointSetAngularLimitUpper(id, value); break;
            case 7: PhysicsServer.PinJointSetMotorEnabled(id, value != 0); break;
            case 8: PhysicsServer.PinJointSetMotorTargetVelocity(id, value); break;
            case 9: PhysicsServer.PinJointSetMotorMaxTorque(id, value); break;
            case 10: PhysicsServer.DampedSpringJointSetRestLength(id, value); break;
            case 11: PhysicsServer.DampedSpringJointSetStiffness(id, value); break;
            default: PhysicsServer.DampedSpringJointSetDamping(id, value); break;
        }
    }

    private static float ReadJointPolicy(RID id, Joint? node, int field) => field switch
    {
        0 => PhysicsServer.JointGetBias(id),
        1 => PhysicsServer.JointGetMaxBias(id),
        2 => PhysicsServer.JointGetMaxForce(id),
        3 => PhysicsServer.PinJointGetSoftness(id),
        4 => PhysicsServer.PinJointGetAngularLimitEnabled(id) ? 1 : 0,
        5 => PhysicsServer.PinJointGetAngularLimitLower(id),
        6 => PhysicsServer.PinJointGetAngularLimitUpper(id),
        7 => PhysicsServer.PinJointGetMotorEnabled(id) ? 1 : 0,
        8 => PhysicsServer.PinJointGetMotorTargetVelocity(id),
        9 => PhysicsServer.PinJointGetMotorMaxTorque(id),
        10 => PhysicsServer.DampedSpringJointGetRestLength(id),
        11 => PhysicsServer.DampedSpringJointGetStiffness(id),
        12 => PhysicsServer.DampedSpringJointGetDamping(id),
        _ => ((GrooveJoint)node!).Length
    };

    private static float SelectedJointPolicy(PhysicsJointConfiguration c, int field) => field switch
    {
        0 => c.Bias,
        1 => c.MaxBias,
        2 => c.MaxForce,
        3 => c.PinSoftness,
        4 => c.PinLimitEnabled ? 1 : 0,
        5 => c.PinLimitLower,
        6 => c.PinLimitUpper,
        7 => c.PinMotorEnabled ? 1 : 0,
        8 => c.PinMotorVelocity,
        9 => c.PinMotorMaxTorque,
        10 => c.EffectiveSpringRestLength,
        11 => c.SpringStiffness,
        12 => c.SpringDamping,
        _ => c.LowerTranslation < 0 ? c.LowerTranslation : c.UpperTranslation
    };
}
