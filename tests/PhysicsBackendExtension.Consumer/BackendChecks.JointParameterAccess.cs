using Electron2D;

public static partial class BackendChecks
{
    private static void JointParameterAccess(SceneTree tree, World world, RID raw, Joint[] nodes, RID[] joints, Solver? solver)
    {
        var fields = new List<(RID ID, Joint? Node, int Field, float Value)>();
        for (var role = 0; role < 3; role++)
            for (var field = 0; field <= 13; field++)
            {
                if (field is >= 3 and <= 9 && role != 0 || field is >= 10 and <= 12 && role != 2 || field == 13 && role != 1) continue;
                for (var scene = field == 13 ? 1 : 0; scene < 2; scene++)
                {
                    var node = scene == 0 ? null : nodes[role]; var id = node?.GetRID() ?? joints[role];
                    fields.Add((id, node, field, ReadJointPolicy(id, node, field)));
                }
            }
        var all = joints.Concat(nodes.Select(n => n.GetRID())).ToArray(); var retained = all.Select(JointParameterState).ToArray();
        var stale = PhysicsServer.JointCreate(); PhysicsServer.FreeRID(stale);
        foreach (var id in new[] { default, raw, world.Space, stale })
            for (var field = 0; field < 13; field++)
            {
                Reject<ArgumentException>(() => SetJointPolicy(id, null, field, JointPolicyValue(field, true)));
                Reject<ArgumentException>(() => ReadJointPolicy(id, null, field));
            }
        for (var field = 3; field < 13; field++)
        {
            var wrongRole = field < 10 ? joints[1] : joints[0];
            Reject<ArgumentException>(() => SetJointPolicy(wrongRole, null, field, JointPolicyValue(field, true)));
            Reject<ArgumentException>(() => ReadJointPolicy(wrongRole, null, field));
        }
        Task.Run(() =>
        {
            foreach (var item in fields)
            {
                if (item.Field != 13)
                {
                    Reject<InvalidOperationException>(() => SetJointPolicy(item.ID, null, item.Field, item.Value));
                    Reject<InvalidOperationException>(() => ReadJointPolicy(item.ID, null, item.Field));
                }
                if (item.Node is not null) Reject<InvalidOperationException>(() => SetJointPolicy(item.ID, item.Node, item.Field, item.Value));
            }
        }).GetAwaiter().GetResult();
        if (solver is not null)
        {
            var previous = solver.StepHook;
            solver.StepHook = () =>
            {
                previous?.Invoke();
                foreach (var item in fields)
                {
                    if (item.Field != 13)
                    {
                        Reject<InvalidOperationException>(() => SetJointPolicy(item.ID, null, item.Field, item.Value));
                        Reject<InvalidOperationException>(() => SetJointPolicy(item.ID, null, item.Field, JointPolicyValue(item.Field, true)));
                        Reject<InvalidOperationException>(() => ReadJointPolicy(item.ID, null, item.Field));
                    }
                    if (item.Node is not null) Reject<InvalidOperationException>(() => SetJointPolicy(item.ID, item.Node, item.Field, item.Value));
                }
            };
            try { tree.PhysicsFrame(1d / 60); } finally { solver.StepHook = previous; }
        }
        for (var i = 0; i < all.Length; i++) Check(JointParameterState(all[i]) == retained[i], "Rejected RID/owner/Solve operations preserve the complete profile");
        var callbacks = 0;
        PhysicsServer.BodySetStateSyncCallback(raw, _ => { callbacks++; foreach (var id in all) SetJointParameters(id, 0); });
        try { tree.PhysicsFrame(1d / 60); } finally { PhysicsServer.BodySetStateSyncCallback(raw, null); }
        Check(callbacks == 1, "Ordinary state callbacks can author the complete typed joint parameter family");
        foreach (var id in all) VerifyJointParameters(id, solver);
        Console.WriteLine("Joint parameter access: complete RID/role/owner/Solve guards and ordinary callback family passed.");
    }
}
