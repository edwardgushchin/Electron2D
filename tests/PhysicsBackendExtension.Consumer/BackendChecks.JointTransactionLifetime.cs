using Electron2D;

public static partial class BackendChecks
{
    private static void JointTransactionLifetime(bool threaded)
    {
        Solver? solver = null; var cases = 0;
        PhysicsServerManager.RegisterServer("JointParameterRetirement", () => solver = new());
        foreach (var field in new[] { 0, 4, 7, 8, 9, 13 })
            for (var after = 0; after < 2; after++)
                for (var route = 0; route < 4; route++) Run(field, after != 0, route);
        Console.WriteLine($"Joint parameter retirement/{threaded}: {cases} channel/before-after/clear/free/departure/scene cases passed.");
        JointPendingChannels();

        void Run(int field, bool after, int route)
        {
            using var world = new World("JointParameterRetirement"); using var root = new SubViewport { World = world };
            var first = new RigidBody { Name = "A", GravityScale = 0 }; var second = new StaticBody { Name = "B" }; root.AddChild(first); root.AddChild(second);
            Joint? node = route == 3 || field == 13 ? field == 13 ? new GrooveJoint() : new PinJoint() : null;
            if (node is not null) { node.NodeA = "../A"; node.NodeB = "../B"; root.AddChild(node); }
            using var tree = new SceneTree(root); var joint = node?.GetRID() ?? PhysicsServer.JointCreate(); var alive = true;
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            try
            {
                if (node is null) MakeJoint(joint, 0, first.GetRID(), second.GetRID());
                SetJointPolicy(joint, node, field, JointPolicyValue(field, false));
                var failure = new ApplicationException("retired-parameter-publication"); var calls = 0;
                Action<RID> hook = id => { if (id == joint) { calls++; throw failure; } };
                if (after) solver!.JointHook = hook; else solver!.JointParameterHook = hook;
                Check(JointParameterErrors(() => SetJointPolicy(joint, node, field, JointPolicyValue(field, true))) is [var error] && ReferenceEquals(error, failure), "Retirement begins with a real pending parameter publication");
                if (route == 0) PhysicsServer.JointClear(joint);
                else if (route == 2) PhysicsServer.BodySetSpace(first.GetRID(), default);
                else { alive = false; if (node is null) PhysicsServer.FreeRID(joint); else node.Dispose(); }
                Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0 && calls == 1, "Retirement discards parameter debt without invoking a discarded attachment");
                solver.JointHook = solver.JointParameterHook = null;
                if (route == 2)
                {
                    PhysicsServer.BodySetSpace(first.GetRID(), world.Space); tree.PhysicsFrame(.01);
                    Check(SelectedJointPolicy(solver.Joints[joint].Settings, field) == JointPolicyValue(field, true), "Fresh attachment receives the retained requested parameter");
                }
            }
            finally
            {
                solver!.JointHook = solver.JointParameterHook = null;
                if (alive) { if (node is null) PhysicsServer.FreeRID(joint); else node.Dispose(); }
            }
            cases++;
        }
    }

    private static void JointPendingChannels()
    {
        Solver? solver = null; PhysicsServerManager.RegisterServer("JointPendingChannels", () => solver = new());
        using var world = new World("JointPendingChannels"); using var root = new SubViewport { World = world };
        var first = new RigidBody { GravityScale = 0 }; var second = new StaticBody(); root.AddChild(first); root.AddChild(second);
        using var tree = new SceneTree(root); var joint = PhysicsServer.JointCreate();
        using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
        try
        {
            MakeJoint(joint, 0, first.GetRID(), second.GetRID()); var failure = new ApplicationException("multiple-pending-parameter-channels");
            solver!.JointHook = id => { if (id == joint) throw failure; };
            Check(JointParameterErrors(() => PhysicsServer.JointSetBias(joint, .3f)) is [var a] && ReferenceEquals(a, failure), "First incomplete policy is retained");
            Check(JointParameterErrors(() => PhysicsServer.PinJointSetAngularLimitLower(joint, -.3f)) is [var b] && ReferenceEquals(b, failure), "A second policy does not overwrite earlier debt");
            var calls = 0; solver.JointHook = id => { if (id == joint && ++calls == 2) throw failure; };
            Check(JointParameterErrors(() => world.DirectSpaceState.IntersectPoint(point, hits)) is [var c] && ReferenceEquals(c, failure) && calls == 2, "Recovery retains only the failing remaining channel");
            solver.JointHook = id => { if (id == joint) calls++; };
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0 && calls == 3, "Successful channels are not replayed when another channel retries");
            solver.JointHook = null;
            Check(solver.Joints[joint].Settings.Bias == .3f && solver.Joints[joint].Settings.PinLimitLower == -.3f, "Combined recovery publishes the complete latest profile");
        }
        finally { solver!.JointHook = null; PhysicsServer.FreeRID(joint); }
    }

    private static void JointTransactionFailures(bool threaded)
    {
        Solver? solver = null; var rejected = 0;
        PhysicsServerManager.RegisterServer("JointParameterFailure", () => solver = new());
        for (var kind = 0; kind < 3; kind++) Run(kind);
        Console.WriteLine($"Joint parameter failures/{threaded}: {rejected} terminal-world writes/reads rejected before mutation.");

        void Run(int kind)
        {
            using var world = new World("JointParameterFailure"); using var root = new SubViewport { World = world };
            var first = new RigidBody { Name = "A", GravityScale = 0 }; var second = new StaticBody { Name = "B" }; root.AddChild(first); root.AddChild(second);
            Joint[] nodes = [new PinJoint(), new GrooveJoint(), new DampedSpringJoint()];
            foreach (var node in nodes) { node.NodeA = "../A"; node.NodeB = "../B"; root.AddChild(node); }
            using var tree = new SceneTree(root); var raw = new[] { PhysicsServer.JointCreate(), PhysicsServer.JointCreate(), PhysicsServer.JointCreate() };
            try
            {
                for (var role = 0; role < 3; role++) MakeJoint(raw[role], role, first.GetRID(), second.GetRID());
                var fields = new List<(RID ID, Joint? Node, int Field, float Value)>();
                for (var role = 0; role < 3; role++)
                    for (var field = 0; field <= 13; field++)
                    {
                        if (field is >= 3 and <= 9 && role != 0 || field is >= 10 and <= 12 && role != 2 || field == 13 && role != 1) continue;
                        for (var scene = field == 13 ? 1 : 0; scene < 2; scene++)
                        {
                            var node = scene == 0 ? null : nodes[role]; var id = node?.GetRID() ?? raw[role]; var value = JointPolicyValue(field, false);
                            SetJointPolicy(id, node, field, value); fields.Add((id, node, field, value));
                        }
                    }
                using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space); using var map = new PhysicsSnapshotMap(world.Space); var snapshot = new PhysicsSnapshot();
                map.Bind(1, 1, first.GetRID()); map.Bind(2, 1, second.GetRID()); var index = 3ul;
                foreach (var id in raw.Concat(nodes.Select(n => n.GetRID()))) map.Bind(index++, 1, id);
                map.Capture(snapshot); var failure = new ApplicationException("joint-parameter-terminal-world");
                if (kind < 2) solver!.RestoreExecutionHook = () => throw failure; else solver!.StepHook = () => throw failure;
                var errors = JointParameterErrors(() => { if (kind == 0) checkpoint.Restore(); else if (kind == 1) map.Apply(snapshot); else tree.PhysicsFrame(.01); });
                Check(errors is [var error] && ReferenceEquals(error, failure), "Joint terminal-world test preserves the original selected failure");
                solver!.RestoreExecutionHook = solver.StepHook = null;
                foreach (var item in fields)
                {
                    Reject<InvalidOperationException>(() => SetJointPolicy(item.ID, item.Node, item.Field, item.Value)); rejected++;
                    Reject<InvalidOperationException>(() => SetJointPolicy(item.ID, item.Node, item.Field, JointPolicyValue(item.Field, true))); rejected++;
                    if (item.Field != 13) { Reject<InvalidOperationException>(() => ReadJointPolicy(item.ID, item.Node, item.Field)); rejected++; }
                    if (item.Node is not null) Check(ReadSceneJointPolicy(item.Node, item.Field) == item.Value, "Rejected terminal scene writes preserve configured values");
                }
            }
            finally { solver!.RestoreExecutionHook = solver.StepHook = null; foreach (var id in raw) PhysicsServer.FreeRID(id); }
        }
    }

    private static float ReadSceneJointPolicy(Joint node, int field) => field switch
    {
        0 => node.Bias,
        1 => node.MaxBias,
        2 => node.MaxForce,
        3 => ((PinJoint)node).Softness,
        4 => ((PinJoint)node).AngularLimitEnabled ? 1 : 0,
        5 => ((PinJoint)node).AngularLimitLower,
        6 => ((PinJoint)node).AngularLimitUpper,
        7 => ((PinJoint)node).MotorEnabled ? 1 : 0,
        8 => ((PinJoint)node).MotorTargetVelocity,
        9 => ((PinJoint)node).MotorMaxTorque,
        10 => ((DampedSpringJoint)node).RestLength,
        11 => ((DampedSpringJoint)node).Stiffness,
        12 => ((DampedSpringJoint)node).Damping,
        _ => ((GrooveJoint)node).Length
    };
}
