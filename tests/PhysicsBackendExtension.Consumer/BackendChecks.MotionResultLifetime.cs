using Electron2D;

public static partial class BackendChecks
{
    public static void RunMotionResultLifetime(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { MotionResultLifetime(backend); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
    private static void MotionResultLifetime(PhysicsServer.Backend? backend)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("MotionResultLifetime", () => solver = new());
        using var world = backend is { } selected ? new World(selected) : new World("MotionResultLifetime");
        using var circle = new CircleShape { Radius = 3 };
        using var root = new SubViewport { World = world };
        var body = new CharacterBody(); var obstacle = new StaticBody { Position = new(20, 0) };
        foreach (var node in new PhysicsBody[] { body, obstacle }) { node.AddChild(new CollisionShape { Shape = circle }); root.AddChild(node); }
        using var tree = new SceneTree(root);
        using var parameters = new PhysicsTestMotionParameters { From = Transform.Identity, Motion = new(40, 0), Margin = 0 };
        using var result = new PhysicsTestMotionResult();
        var moving = PhysicsServer.BodyCreate(); PhysicsTestMotionResult? borrowed = null;
        try
        {
            Check(backend is not null || solver is not null, "Registered motion fixture initialized its implementation");
            if (solver is not null)
            {
                solver.MotionResultHook = (_, scratch) => { borrowed = scratch; Reject<InvalidOperationException>(scratch.Dispose); };
                Query(); Check(!borrowed!.IsDisposed, "Selected motion output remains live after refused disposal");
                RejectBorrowedAccess(borrowed);
                var original = borrowed;
                solver.MotionResultHook = (_, scratch) =>
                {
                    Check(ReferenceEquals(original, scratch) && scratch.GetColliderRID() == obstacle.GetRID(), "Repeated motion reuses live borrowed output");
                    Exception? workerError = null;
                    var worker = new Thread(() =>
                    {
                        try
                        {
                            RejectBorrowedAccess(scratch);
                        }
                        catch (Exception error) { workerError = error; }
                    });
                    worker.Start(); worker.Join(); if (workerError is not null) throw workerError;
                };
                Query(); var before = result.GetTravel(); var identity = result.GetColliderRID();
                solver.MotionResultHook = (_, _) => throw new InvalidOperationException("Expected borrowed motion callback failure");
                Reject<InvalidOperationException>(Query);
                Check(result.GetTravel() == before && result.GetColliderRID() == identity, "Failed hook preserves caller-owned snapshot");
                solver.MotionResultHook = (_, scratch) => scratch.SetMotion(Vector2.Zero);
                Reject<InvalidOperationException>(Query);
                Check(result.GetTravel() == before && result.GetColliderRID() == identity, "Invalid hook output preserves caller-owned snapshot");
                solver.MotionResultHook = (_, scratch) => Check(ReferenceEquals(original, scratch), "Failures retain reusable output identity");
                Query(); solver.MotionResultHook = null;
            }
            Query(); result.SetMotion(new(7, 0)); Check(result.GetTravel() == new Vector2(7, 0), "Caller-owned results remain independently writable"); Query();
            PhysicsServer.AreaSetGravity(world.Space, 0); PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
            PhysicsServer.BodySetCanSleep(moving, false); PhysicsServer.BodySetLinearVelocity(moving, new(60, 0)); PhysicsServer.BodySetSpace(moving, world.Space);
            for (var i = 0; i < 128; i++) { tree.PhysicsFrame(1d / 60); Query(); }
            Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) { tree.PhysicsFrame(1d / 60); Query(); }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Full motion frames allocate {owner}/{all} B");
            Check(MathF.Abs(PhysicsServer.BodyGetTransform(moving).Origin.X - 256) < .02f, "Motion-result guards retain actual numerical steps");
            if (solver is not null)
            {
                var retirement = new InvalidOperationException("Expected borrowed result retirement failure");
                borrowed!.Disposed += _ => throw retirement; solver.FailFinish = true;
                tree.Dispose();
                try { world.Dispose(); throw new InvalidOperationException("Expected registered retirement failure"); }
                catch (AggregateException error)
                {
                    var failures = error.Flatten().InnerExceptions;
                    Check(failures.Contains(retirement) && failures.Count >= 2, "Retirement preserves implementation and scratch disposal failures");
                }
                Check(borrowed.IsDisposed, "World retirement releases its borrowed result despite Finish failure");
                Reject<ObjectDisposedException>(() => borrowed.GetTravel());
            }
            Console.WriteLine($"Motion-result lifetime {backend?.ToString() ?? "registered"}, threaded={ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread)}: callback/error/reuse/retirement and 128 full motion frames {owner}/{all} B passed.");
        }
        finally { if (solver is not null) { solver.MotionResultHook = null; solver.FailFinish = false; } PhysicsServer.FreeRID(moving); }
        void RejectBorrowedAccess(PhysicsTestMotionResult scratch)
        {
            Action[] operations = [scratch.Dispose, () => scratch.SetMotion(Vector2.Zero),
                () => scratch.SetCollision(body.GetRID(), 0, obstacle.GetRID(), 0, new(17, 0), Vector2.Left, 0, Vector2.Zero, new(14, 0), new(26, 0), .35f, .35f),
                () => scratch.GetCollider(), () => scratch.GetColliderID(), () => scratch.GetColliderRID(),
                () => scratch.GetColliderShape(), () => scratch.GetCollisionLocalShape(), () => scratch.GetColliderVelocity(),
                () => scratch.GetCollisionDepth(), () => scratch.GetCollisionNormal(), () => scratch.GetCollisionPoint(),
                () => scratch.GetCollisionSafeFraction(), () => scratch.GetCollisionUnsafeFraction(), () => scratch.GetTravel(), () => scratch.GetRemainder()];
            foreach (var operation in operations) Reject<InvalidOperationException>(operation);
        }
        void Query()
        {
            Check(PhysicsServer.BodyTestMotion(body.GetRID(), parameters, result) && result.GetColliderRID() == obstacle.GetRID(), "Real selected motion collision");
            Check(MathF.Abs(result.GetTravel().X - 14) < .2f && (result.GetTravel() + result.GetRemainder()).DistanceTo(new(40, 0)) < .001f,
                "Circle first-impact geometry retains eight-refinement tolerance and complete displacement");
            Check(PhysicsServer.BodyTestMotion(body.GetRID(), parameters), "Motion collision without caller output");
        }
    }
}
