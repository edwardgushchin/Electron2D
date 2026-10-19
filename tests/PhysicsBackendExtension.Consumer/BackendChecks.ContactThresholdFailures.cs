using Electron2D;

public static partial class BackendChecks
{
    private static void ContactThresholdFailures(bool threaded)
    {
        Solver? solver = null; PhysicsServerManager.RegisterServer("ContactThresholdFailures", () => solver = new());
        foreach (var phase in new[] { LifecyclePhase.Solve, LifecyclePhase.Sync, LifecyclePhase.Statistics })
        {
            using var world = new World(PhysicsServer.Backend.CPU); using var root = new SubViewport { World = world };
            PhysicsBody[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone()];
            foreach (var node in nodes) root.AddChild(node);
            using var tree = new SceneTree(root); using var replacement = new World(PhysicsServer.Backend.CPU);
            var space = PhysicsServer.SpaceCreate("ContactThresholdFailures");
            var raw = new RID[4]; var owner = Thread.CurrentThread;
            try
            {
                for (var i = 0; i < raw.Length; i++) { raw[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(raw[i], (PhysicsServer.BodyMode)i); PhysicsServer.BodySetSpace(raw[i], space); }
                var all = nodes.Select(n => n.GetRID()).Concat(raw).ToArray();
                foreach (var id in all) { PhysicsServer.BodySetSpace(id, space); PhysicsServer.BodySetContactsReportedDepthThreshold(id, .75f); }
                var failure = new ApplicationException("contact-threshold-terminal");
                solver!.LifecycleHook = current =>
                {
                    if (current == LifecyclePhase.Solve) Check((Thread.CurrentThread != owner) == threaded, "Threshold failure uses the actual selected numerical thread");
                    if (current == phase) throw failure;
                };
                PhysicsServer.SpaceSetActive(space, true);
                Check(ReleaseErrors(() => PhysicsServer.SpaceStep(space, .01)).Any(e => ReferenceEquals(e, failure)), "Failed publication preserves the original selected error");
                solver.LifecycleHook = null; var steps = solver.Steps;
                foreach (var id in all)
                {
                    Reject<InvalidOperationException>(() => PhysicsServer.BodyGetContactsReportedDepthThreshold(id));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetContactsReportedDepthThreshold(id, .75f));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetContactsReportedDepthThreshold(id, 0));
                }
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(space, .01));
                PhysicsServer.FreeRID(space); space = default;
                foreach (var id in all)
                {
                    Check(PhysicsServer.BodyGetContactsReportedDepthThreshold(id) == .75f, "Failed equal/changed writes preserve every retained scene/raw policy after failed-world retirement");
                }
                foreach (var id in raw)
                {
                    PhysicsServer.BodySetSpace(id, replacement.Space);
                    Check(PhysicsServer.BodyGetContactsReportedDepthThreshold(id) == .75f, "All raw modes retain threshold authoring in a healthy replacement world");
                }
                Check(solver.Steps == steps, "Threshold rejection and recovery do not replay failed Solve");
            }
            finally { solver!.LifecycleHook = null; if (space.IsValid()) PhysicsServer.FreeRID(space); foreach (var id in raw) if (id.IsValid()) PhysicsServer.FreeRID(id); }
        }
        Console.WriteLine($"Contact threshold failures/{threaded}: 81 terminal read/equal-write/changed-write rejections across nine scene/raw owners, retained recovery and no replay passed.");
    }
}
