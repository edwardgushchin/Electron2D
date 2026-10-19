using Electron2D;

public static partial class BackendChecks
{
    public static void RunDebugContacts(bool threaded = false)
    {
        var original = ProjectSettings.Get(ProjectSettings.DebugCollisionMaxContacts);
        var previousThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            DebugContactTransactions();
            foreach (var cap in new[] { 0, 1, 3, 8 }) DebugContactScope(cap, threaded);
            for (var failure = 0; failure < 3; failure++) DebugContactFailure(failure);
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.DebugCollisionMaxContacts, original);
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previousThread);
        }
        Console.WriteLine("Registered diagnostics: preparation before solve, odd/zero caps, pause, errors, cleanup and zero warm allocations passed.");
    }

    private static void DebugContactScope(int cap, bool threaded)
    {
        ProjectSettings.Set(ProjectSettings.DebugCollisionMaxContacts, cap);
        Solver? solver = null;
        PhysicsServerManager.RegisterServer($"DebugContacts{cap}", () => solver = new());
        using var world = new World($"DebugContacts{cap}");
        using var circle = new CircleShape { Radius = 2 };
        using var root = new SubViewport { World = world };
        for (var i = 0; i < 2; i++)
        {
            var fixedBody = new StaticBody { Name = $"Fixed{i}", Position = new(i * 20, 0) };
            fixedBody.AddChild(new CollisionShape { Shape = circle }); root.AddChild(fixedBody);
            var moving = new RigidBody { Name = $"Moving{i}", Position = new(i * 20, 3), GravityScale = 0, CanSleep = false, MaxContactsReported = 0 };
            moving.AddChild(new CollisionShape { Shape = circle }); root.AddChild(moving);
        }
        using var tree = new SceneTree(root); var owner = Thread.CurrentThread;
        tree.PhysicsFrame(1d / 120);
        Check(solver!.DebugReads == 0 && solver.DebugSets == 0, "Capture remains off independently of actual solving");
        ProjectSettings.Set(ProjectSettings.DebugCollisionMaxContacts, 7);
        solver.DebugPolicyHook = _ =>
        {
            Check(Thread.CurrentThread == owner, "Diagnostic policy stays on the scene owner");
            Reject<InvalidOperationException>(() => tree.DebugCollisionsHint = true);
            Reject<InvalidOperationException>(() => tree.Dispose());
            Reject<InvalidOperationException>(() => tree.FinalizeLoop());
            Check(!tree.IsDisposed && !root.IsDisposed && root.IsInsideTree, "Rejected teardown leaves the borrowed scene intact");
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetActive(world.Space, false));
        };
        tree.DebugCollisionsHint = true; solver.DebugPolicyHook = null;
        Check(solver.DebugLimit == cap && solver.DebugReads == 0 && solver.DebugSets == (cap == 0 ? 0 : 1),
            "The sampled odd/zero capacity reaches the solver before any readback");
        var sets = solver.DebugSets;
        tree.DebugCollisionsHint = true;
        Check(solver.DebugSets == sets, "Equal hints do not repeat preparation");
        var inspectBorrowing = true;
        solver.StepHook = () =>
        {
            Check((Thread.CurrentThread != owner) == threaded, "Diagnostics use the actual captured numerical lane");
            Check(solver.DebugLimit == cap, "Solve sees the already prepared diagnostic cap");
            if (inspectBorrowing) Reject<InvalidOperationException>(() => tree.DebugCollisionsHint = false);
        };
        solver.DebugReadHook = () =>
        {
            Check(Thread.CurrentThread == owner && solver.Ready, "Diagnostic publication follows synchronization on the owner");
            Reject<InvalidOperationException>(() => tree.DebugCollisionsHint = false);
            Reject<InvalidOperationException>(() => tree.Dispose());
            Reject<InvalidOperationException>(() => tree.FinalizeLoop());
        };
        Reset(); tree.PhysicsFrame(1d / 120); solver.DebugReadHook = null; inspectBorrowing = false;
        Check(solver.DebugLastCount == Math.Min(cap, 4) && solver.DebugReads == (cap == 0 ? 0 : 1),
            "Two real circle pairs publish the odd point prefix despite zero gameplay contact caps");
        var reads = solver.DebugReads;
        tree.PhysicsFrame(0); PhysicsServer.SpaceSetActive(world.Space, false); tree.PhysicsFrame(1d / 120);
        Check(solver.DebugReads == reads && solver.DebugLastCount == Math.Min(cap, 4), "Zero and inactive steps retain the latest diagnostic sample");
        PhysicsServer.SpaceSetActive(world.Space, true);
        Task.Run(() => Reject<InvalidOperationException>(() => tree.DebugCollisionsHint = false)).GetAwaiter().GetResult();
        Collect(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++) { Reset(); tree.PhysicsFrame(1d / 120); }
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) { Reset(); tree.PhysicsFrame(1d / 120); }
        own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(own == 0 && all == 0, $"Diagnostic active frames allocated {own}/{all} owner/all-thread B");
        foreach (var b in solver.Bodies.Values)
            if (b.InvMass > 0) PhysicsServer.BodySetTransform(b.RID, new(0, b.Position + new Vector2(0, 100)));
        tree.PhysicsFrame(1d / 120);
        Check(solver.DebugPointCount == 0 && solver.DebugLastCount == 0, "An active noncontacting interval replaces the sample with empty output");
        if (cap == 8) DebugContactRaw(solver, world, tree, circle);
        solver.StepHook = null;
        tree.DebugCollisionsHint = false;
        Check(solver.DebugLimit == 0 && solver.DebugPointCount == 0, "Disable clears and stops numerical diagnostic capture immediately");
        reads = solver.DebugReads; tree.PhysicsFrame(1d / 120);
        Check(solver.DebugReads == reads, "Disabled intervals have no diagnostic readback");
        solver.FailDebugPreparation = true;
        if (cap > 0)
        {
            Reject<ApplicationException>(() => tree.DebugCollisionsHint = true);
            Check(!tree.DebugCollisionsHint && solver.DebugLimit == 0, "Rejected preparation preserves the disabled policy");
        }
        solver.FailDebugPreparation = false; tree.DebugCollisionsHint = true;
        tree.Dispose();
        Check(solver.DebugLimit == 0 && !solver.Finished && !world.IsDisposed,
            "Tree detach relinquishes capture while the caller retains its World resource");
        _ = PhysicsServer.SpaceGetTick(world.Space);
        world.Dispose();
        Check(solver.Finished && solver.IsDisposed, "Final caller-owned World release retires the implementation");
        Console.WriteLine($"Registered debug cap={cap}/{threaded}: 128 active measured frames, owner/all={own}/{all} B.");
        void Reset()
        {
            foreach (var b in solver.Bodies.Values)
                if (b.InvMass > 0)
                {
                    PhysicsServer.BodySetTransform(b.RID, new(0, new Vector2(b.Position.X, 3)));
                    PhysicsServer.BodySetLinearVelocity(b.RID, new(0, -1));
                }
        }
    }

    private static void DebugContactFailure(int failure)
    {
        ProjectSettings.Set(ProjectSettings.DebugCollisionMaxContacts, 3);
        Solver? solver = null;
        var name = $"DebugFailure{failure}";
        PhysicsServerManager.RegisterServer(name, () => solver = new());
        using var world = new World(name);
        using var root = new SubViewport { World = world };
        root.AddChild(new StaticBody());
        using var tree = new SceneTree(root) { DebugCollisionsHint = true };
        solver!.BadDebugCount = failure == 0; solver.NegativeDebugCount = failure == 1; solver.BadDebugPoint = failure == 2;
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 120));
        Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetTick(world.Space));
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 120));
        Check(solver.Steps == 1 && solver.DebugReads == 1, "A terminal diagnostic failure never replays Solve or publication");
        tree.DebugCollisionsHint = false;
        Check(solver.DebugLimit == 0 && solver.DebugSets == 2, "Terminal diagnostic errors still permit the zero-cap cleanup hook");
        Reject<InvalidOperationException>(() => tree.DebugCollisionsHint = true);
        tree.Dispose();
        Check(solver.DebugLimit == 0 && !solver.Finished, "Failed-world detach preserves the remaining caller ownership");
        world.Dispose();
        Check(solver.Finished && solver.IsDisposed, "Terminal diagnostics retain ordinary implementation retirement");
    }
}
