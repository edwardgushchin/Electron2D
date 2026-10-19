using Electron2D;

public static partial class BackendChecks
{
    private static void ExceptionReads(SceneTree tree, World world, RigidBody scene, StaticBody peer, RID raw, Solver solver)
    {
        using (var unbound = new Solver()) Reject<InvalidOperationException>(() => unbound.ReadExceptions(raw));
        var inert = PhysicsServer.BodyCreate(); PhysicsServer.FreeRID(inert);
        foreach (var owner in new[] { raw, scene.GetRID() })
        {
            PhysicsServer.BodyAddCollisionException(owner, default); PhysicsServer.BodyAddCollisionException(owner, peer.GetRID());
            PhysicsServer.BodyAddCollisionException(owner, inert); PhysicsServer.BodyAddCollisionException(owner, peer.GetRID());
        }
        var statusReads = 0;
        solver.StatusHook = () => { Check(solver.ReadExceptions(raw)[1] == peer.GetRID(), "Scoped configured reads do not reenter failure status"); statusReads++; };
        try { Check(solver.ReadExceptions(raw)[1] == peer.GetRID() && statusReads == 1, "Ordinary scoped reads retain one failure-status guard"); }
        finally { solver.StatusHook = null; }
        var snapshot = solver.ReadExceptions(raw);
        Check(snapshot is [var empty, var target, var retired] && !empty.IsValid() && target == peer.GetRID() && retired == inert,
            "Scoped implementation reads preserve ordered, unique and inert RID entries");
        snapshot[1] = default;
        Check(solver.ReadExceptions(raw)[1] == peer.GetRID() && scene.GetCollisionExceptions() is [null, var node, null] && ReferenceEquals(node, peer),
            "Returned RID snapshots own their storage; scene projection retains null slots");
        var previousHook = solver.StepHook;
        solver.StepHook = () =>
        {
            Check(solver.ReadExceptions(raw)[1] == peer.GetRID(), "A solve hook may read common authored input without querying solver state");
            Reject<InvalidOperationException>(() => PhysicsServer.BodyRemoveCollisionException(raw, peer.GetRID()));
        };
        try { tree.PhysicsFrame(1d / 60); } finally { solver.StepHook = previousHook; }
        using (var ready = new ManualResetEventSlim())
        using (var done = new ManualResetEventSlim())
        {
            RID foreignID = default;
            var worker = Task.Run(() =>
            {
                try
                {
                    using var foreignWorld = new World(PhysicsServer.Backend.CPU); using var root = new SubViewport { World = foreignWorld };
                    var body = new RigidBody { ProcessMode = ProcessMode.Disabled }; root.AddChild(body);
                    using var foreignTree = new SceneTree(root); foreignID = body.GetRID(); ready.Set(); done.Wait();
                }
                finally { ready.Set(); }
            });
            ready.Wait(); var previous = solver.StepHook;
            try
            {
                if (worker.IsCompleted) worker.GetAwaiter().GetResult();
                Reject<InvalidOperationException>(() => solver.ReadExceptions(foreignID));
                solver.StepHook = () =>
                {
                    previous?.Invoke(); Exception? error = null;
                    try { solver.ReadExceptions(foreignID); } catch (Exception caught) { error = caught; }
                    if (error is not InvalidOperationException) throw new InvalidOperationException("A borrowed solver read a detached foreign-owner scene body.");
                };
                tree.PhysicsFrame(1d / 60);
            }
            finally { solver.StepHook = previous; done.Set(); worker.GetAwaiter().GetResult(); }
        }
        PhysicsServer.BodySetSpace(raw, default);
        Check(solver.ReadExceptions(raw)[1] == peer.GetRID(), "A bound implementation can read retained detached authoring");
        PhysicsServer.BodySetSleeping(raw, true); PhysicsServer.BodyAddCollisionException(raw, peer.GetRID());
        Check(!PhysicsServer.BodyGetSleeping(raw), "Exception edits wake retained detached dynamic state");
        PhysicsServer.BodySetSpace(raw, world.Space);
        Task.Run(() => Reject<InvalidOperationException>(() => solver.ReadExceptions(raw))).GetAwaiter().GetResult();
        using (var foreign = new World(PhysicsServer.Backend.CPU))
        {
            var other = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
            try
            {
                PhysicsServer.BodySetSpace(other, foreign.Space);
                Reject<ArgumentException>(() => solver.ReadExceptions(other)); Reject<ArgumentException>(() => solver.ReadExceptions(area));
                Reject<ArgumentException>(() => solver.ReadExceptions(default)); Reject<ArgumentException>(() => solver.ReadExceptions(inert));
            }
            finally { PhysicsServer.FreeRID(other); PhysicsServer.FreeRID(area); }
        }
        foreach (var owner in new[] { raw, scene.GetRID() })
        {
            PhysicsServer.BodyRemoveCollisionException(owner, default); PhysicsServer.BodyRemoveCollisionException(owner, peer.GetRID());
            PhysicsServer.BodyRemoveCollisionException(owner, inert);
        }
        Check(snapshot[2] == inert, "Earlier snapshots survive later authoring changes");
        Solver? retiredSolver = null;
        PhysicsServerManager.RegisterServer("ExceptionRetire", () => retiredSolver = new());
        var space = PhysicsServer.SpaceCreate("ExceptionRetire"); var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetSpace(body, space); PhysicsServer.SpaceSetActive(space, true);
            retiredSolver!.FailSolve = true;
            Reject<AggregateException>(() => PhysicsServer.SpaceStep(space, 1d / 60));
            Reject<InvalidOperationException>(() => retiredSolver.ReadExceptions(body));
            PhysicsServer.FreeRID(space); space = default;
            Reject<ObjectDisposedException>(() => retiredSolver.ReadExceptions(body));
        }
        finally { if (space.IsValid()) PhysicsServer.FreeRID(space); PhysicsServer.FreeRID(body); }
    }
}
