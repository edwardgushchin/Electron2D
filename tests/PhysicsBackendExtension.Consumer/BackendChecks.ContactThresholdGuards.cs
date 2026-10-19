using Electron2D;

public static partial class BackendChecks
{
    public static void RunContactThresholdGuards(bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            Solver? solver = null; PhysicsServerManager.RegisterServer("ContactThresholdGuards", () => solver = new());
            using var world = new World("ContactThresholdGuards"); using var root = new SubViewport { World = world };
            PhysicsBody[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone()];
            foreach (var node in nodes) root.AddChild(node);
            using var tree = new SceneTree(root); var raw = new RID[4]; var owner = Thread.CurrentThread; var status = 0; var guards = 0;
            try
            {
                for (var i = 0; i < raw.Length; i++) { raw[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(raw[i], (PhysicsServer.BodyMode)i); PhysicsServer.BodySetSpace(raw[i], world.Space); }
                var all = nodes.Select(n => n.GetRID()).Concat(raw).ToArray();
                for (var i = 0; i < all.Length; i++)
                {
                    PhysicsServer.BodySetContactsReportedDepthThreshold(all[i], i + .75f);
                    foreach (var invalid in new[] { -1f, -float.Epsilon, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetContactsReportedDepthThreshold(all[i], invalid));
                }
                var area = PhysicsServer.AreaCreate(); var joint = PhysicsServer.JointCreate(); var stale = PhysicsServer.BodyCreate(); PhysicsServer.FreeRID(stale);
                try
                {
                    foreach (var invalid in new[] { default, area, joint, world.Space, stale })
                    {
                        Reject<ArgumentException>(() => PhysicsServer.BodyGetContactsReportedDepthThreshold(invalid));
                        Reject<ArgumentException>(() => PhysicsServer.BodySetContactsReportedDepthThreshold(invalid, 0));
                    }
                }
                finally { PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(joint); }
                Task.Run(() =>
                {
                    foreach (var id in all)
                    {
                        Reject<InvalidOperationException>(() => PhysicsServer.BodyGetContactsReportedDepthThreshold(id));
                        Reject<InvalidOperationException>(() => PhysicsServer.BodySetContactsReportedDepthThreshold(id, 0));
                    }
                }).GetAwaiter().GetResult();
                solver!.StatusHook = () => status++;
                void Guard()
                {
                    guards++; var before = status;
                    for (var i = 0; i < all.Length; i++)
                    {
                        var id = all[i]; var value = i + .75f;
                        Check(PhysicsServer.BodyGetContactsReportedDepthThreshold(id) == value, "Borrowed reporting reads retain all scene/raw authoring without preparation");
                        Reject<InvalidOperationException>(() => PhysicsServer.BodySetContactsReportedDepthThreshold(id, value));
                        Reject<InvalidOperationException>(() => PhysicsServer.BodySetContactsReportedDepthThreshold(id, 0));
                    }
                    Reject<InvalidOperationException>(() => ((RigidBody)nodes[3]).MaxContactsReported = 0);
                    if (status != before) throw new InvalidOperationException("Contact reporting writes inspected selected status while numerically borrowed.");
                }
                solver.IdentityHook = _ => Guard();
                try { PhysicsServer.BodyAttachCanvasInstanceID(raw[0], 17); } finally { solver.IdentityHook = null; }
                solver.StepHook = () => { Check((Thread.CurrentThread != owner) == threaded, "Threshold guards use the actual selected numerical lane"); Guard(); };
                try { tree.PhysicsFrame(.01); } finally { solver.StepHook = null; }
                Check(guards >= 2, "Owner numerical publication and actual Solve both exercise the complete reporting family");
                solver.StatusHook = null;
                foreach (var id in new[] { nodes[3].GetRID(), raw[2] })
                {
                    PhysicsServer.BodySetSleeping(id, true);
                    foreach (var value in new[] { 0f, float.Epsilon, 3f, float.MaxValue })
                    {
                        PhysicsServer.BodySetContactsReportedDepthThreshold(id, value);
                        Check(PhysicsServer.BodyGetSleeping(id), "Reporting threshold edits preserve explicit sleep without waking the numerical body");
                    }
                }
                Console.WriteLine($"Contact threshold guards/{threaded}: nine scene/raw owners, exact configured reads and status-free rejected writes passed.");
            }
            finally
            {
                solver!.IdentityHook = null; solver.StepHook = solver.StatusHook = null;
                foreach (var id in raw) if (id.IsValid()) PhysicsServer.FreeRID(id);
            }
            ContactThresholdFailures(threaded);
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
