using Electron2D;

public static partial class BackendChecks
{
    public static void RunExceptions(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { ExceptionChecks(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void ExceptionChecks(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ExceptionChecks", () => solver = new());
        using var world = backend is { } selected ? new World(selected) : new World("ExceptionChecks");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 };
        var scene = new RigidBody { GravityScale = 0, CanSleep = false };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        var peer = new StaticBody { Position = new(30, 0) };
        peer.AddChild(new CollisionShape { Shape = circle }); root.AddChild(peer);
        PhysicsBody[] siblings = [scene, peer, new AnimatableBody(), new CharacterBody(), new PhysicalBone()];
        for (var i = 2; i < siblings.Length; i++)
        {
            siblings[i].Position = new(1000 + i * 100, 0); siblings[i].AddChild(new CollisionShape { Shape = circle }); root.AddChild(siblings[i]);
        }
        using var tree = new SceneTree(root);
        var ownerThread = Thread.CurrentThread;
        if (solver is not null) solver.StepHook = () => Check((Thread.CurrentThread != ownerThread) == threaded, "Exceptions use the actual captured numerical thread");
        var raw = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
            PhysicsServer.BodyAddCollisionException(raw, peer.GetRID()); foreach (var body in siblings) body.AddCollisionExceptionWith(peer);
            if (solver is not null) solver.IdentityHook = RejectWrites;
            PhysicsServer.BodySetSpace(raw, world.Space);
            if (solver is not null)
            {
                solver.IdentityHook = null; solver.BodyHook = RejectWrites;
                PhysicsServer.BodySetMass(raw, 2); foreach (var body in siblings) PhysicsServer.BodySetMass(body.GetRID(), 2);
                PhysicsServer.BodySetLinearVelocity(raw, Vector2.Zero); PhysicsServer.BodySetLinearVelocity(scene.GetRID(), Vector2.Zero);
                solver.BodyHook = null;
            }
            PhysicsServer.BodyRemoveCollisionException(raw, peer.GetRID()); foreach (var body in siblings) body.RemoveCollisionExceptionWith(peer);
            ExceptionResponse(tree, root, world, scene, peer, raw, circle, solver);
            ExceptionJoints(tree, root, scene, peer, solver);
            if (solver is not null) ExceptionReads(tree, world, scene, peer, raw, solver);
            for (var i = 0; i < 64; i++) EditAndStep();
            GC.Collect(); GC.WaitForPendingFinalizers();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) EditAndStep();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Repeated sole-exception edits and frames allocated {owner}/{all} B");
            Console.WriteLine($"Exception family on {backend?.ToString() ?? "registered"}: scene/raw response, motion, Area sensing, wake, borrowed writes, joint union and 128 edited full frames: {owner}/{all} B passed.");
        }
        finally
        {
            if (solver is not null) { solver.IdentityHook = null; solver.BodyHook = null; }
            PhysicsServer.FreeRID(raw);
        }

        void EditAndStep()
        {
            PhysicsServer.BodyAddCollisionException(raw, peer.GetRID());
            foreach (var body in siblings) body.AddCollisionExceptionWith(peer);
            tree.PhysicsFrame(1d / 60);
            PhysicsServer.BodyRemoveCollisionException(raw, peer.GetRID());
            foreach (var body in siblings) body.RemoveCollisionExceptionWith(peer);
            tree.PhysicsFrame(1d / 60);
        }

        void RejectWrites(RID body)
        {
            foreach (var owner in new[] { raw }.Concat(siblings.Select(item => item.GetRID())))
            {
                Reject<InvalidOperationException>(() => PhysicsServer.BodyAddCollisionException(owner, default));
                Reject<InvalidOperationException>(() => PhysicsServer.BodyRemoveCollisionException(owner, peer.GetRID()));
                Check(solver!.ReadExceptions(owner) is [var item] && item == peer.GetRID(), "Numerical hooks cannot alter retained exception lists");
            }
            foreach (var node in siblings)
            {
                Reject<InvalidOperationException>(() => node.AddCollisionExceptionWith(node));
                Reject<InvalidOperationException>(() => node.RemoveCollisionExceptionWith(peer));
            }
        }
    }
}
