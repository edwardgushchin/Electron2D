using Electron2D;

public static partial class BackendChecks
{
    public static void RunWorldCopies(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previousThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var previousEngine = ProjectSettings.Get(ProjectSettings.Physics2DEngine);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? borrowed = null;
        if (backend is null)
        {
            PhysicsServerManager.RegisterServer("WorldCopies", () => new Solver());
            PhysicsServerManager.RegisterServer("WorldCopyBorrowed", () => borrowed = new Solver());
        }
        ProjectSettings.Set(ProjectSettings.Physics2DEngine, backend?.ToString() ?? "WorldCopies");
        try { WorldCopies(backend, () => borrowed); }
        finally
        {
            ProjectSettings.Set(ProjectSettings.Physics2DEngine, previousEngine);
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previousThread);
        }
    }

    private static void WorldCopies(PhysicsServer.Backend? backend, Func<Solver?> borrowed)
    {
        World Create() => backend is { } selected ? new World(selected) : new World("WorldCopies");
        using var bound = Create(); using var source = Create();
        using var owned = backend is { } selected ? new World(selected) : new World("WorldCopyBorrowed");
        bound.ResourceName = "bound"; source.ResourceName = "source"; owned.ResourceName = "owned";
        source.ResourceLocalToScene = true;
        source.CanTranslateMessages = false; source.TranslationDomain = StoredResourceDomain;
        bound.SetPathCache("local://world-copy-bound"); bound.ResourceSceneUniqueID = "bound1";
        owned.SetPathCache("local://world-copy-owned"); owned.ResourceSceneUniqueID = "owned1";
        using var root = new SubViewport { World = bound }; using var circle = new CircleShape { Radius = 2 };
        var mover = new RigidBody
        {
            Name = "Mover",
            GravityScale = 0,
            CanSleep = false,
            LinearVelocity = new(60, 0),
            LinearDampMode = RigidBody.DampMode.Replace,
            LinearDamp = 0,
            AngularDampMode = RigidBody.DampMode.Replace,
            AngularDamp = 0
        };
        mover.AddChild(new CollisionShape { Shape = circle }); root.AddChild(mover);
        using var tree = new SceneTree(root);
        Check(bound.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && bound.PhysicsFallbackReason is null,
            "World copying executes the explicitly selected implementation without fallback");
        var changes = 0; bound.Changed += _ => changes++;
        var original = State(bound);
        Reject<InvalidOperationException>(() => bound.CopyFromResource(source));
        Check(State(bound) == original && changes == 0, "Rejected bound-world copy preserves all resource fields and runtime identities without Changed");
        using (var alias = (World)bound.Duplicate())
        {
            alias.ResourceName = "alias"; alias.ResourceLocalToScene = true;
            bound.CopyFromResource(alias);
            Check(bound.ResourceName == "alias" && bound.ResourceLocalToScene && changes == 1 &&
                bound.Space == original.Space && bound.Canvas == original.Canvas && bound.NavigationMap == original.Map,
                "A bound same-runtime alias copy changes base state once while preserving all identities");
            bound.CopyFromResource(bound); bound.ResetState();
            Check(changes == 1, "Self-copy and ordinary World reset remain harmless no-ops");
        }
        var ownOriginal = State(owned); var ownChanges = 0; owned.Changed += _ => ownChanges++;
        Exception? workerError = null;
        var worker = new Thread(() => { try { owned.CopyFromResource(source); } catch (Exception error) { workerError = error; } });
        worker.Start(); worker.Join();
        Check(workerError is InvalidOperationException && State(owned) == ownOriginal && ownChanges == 0,
            "A last-owner off-thread replacement rejects before metadata or notification mutation");

        World expired;
        using (var expiredRoot = new SubViewport())
        {
            using var expiredTree = new SceneTree(expiredRoot);
            _ = expiredRoot.FindWorld(); expired = (World)expiredRoot.World!.Duplicate(); _ = expired.Space;
        }
        using (expired)
        {
            expired.ResourceName = "expired"; expired.ResourceLocalToScene = true;
            Reject<ArgumentException>(() => _ = expired.Space);
            Reject<ArgumentException>(() => owned.CopyFromResource(expired));
            Check(State(owned) == ownOriginal && ownChanges == 0, "Expired-source copying preserves the target before retain failure");
        }
        var callbacks = 0; var boundOriginal = State(bound); var boundChanges = changes;
        PhysicsServer.BodySetStateSyncCallback(mover.GetRID(), _ =>
        {
            callbacks++; Reject<InvalidOperationException>(() => bound.CopyFromResource(source));
            Check(bound.ResourceName == boundOriginal.Name && bound.ResourceLocalToScene == boundOriginal.Local && changes == boundChanges,
                "Callback-time bound replacement leaves base state unchanged");
        });
        try
        {
            tree.PhysicsFrame(1d / 60);
            Check(callbacks == 1 && State(bound) == boundOriginal, "Refused copy leaves the completed world usable");
        }
        finally { PhysicsServer.BodySetStateSyncCallback(mover.GetRID(), null); }
        if (backend is null)
        {
            var solver = borrowed()!; var reads = 0;
            solver.StatusHook = () =>
            {
                reads++; Reject<InvalidOperationException>(() => owned.CopyFromResource(source));
                Check(owned.ResourceName == ownOriginal.Name && !owned.ResourceLocalToScene && ownChanges == 0,
                    "Borrowed last-owner replacement preserves base state before release rejection");
            };
            try { using var empty = new PhysicsPointQueryParameters(); var none = new PhysicsPointResult[1]; owned.DirectSpaceState.IntersectPoint(empty, none); }
            finally { solver.StatusHook = null; }
            Check(reads > 0 && State(owned) == ownOriginal, "A real selected status hook exercises the release guard");
        }
        owned.CopyFromResource(source);
        Check(owned.Space == source.Space && owned.Canvas == source.Canvas && owned.NavigationMap == source.NavigationMap &&
            owned.ResourceName == "source" && owned.ResourceLocalToScene && owned.InstanceID == ownOriginal.ID &&
            owned.ResourcePath == ownOriginal.Path && owned.ResourceSceneUniqueID == ownOriginal.SceneID && ownChanges == 1,
            "Allowed unbound replacement transfers runtime sharing and preserves target resource identity");
        Reject<ArgumentException>(() => PhysicsServer.SpaceGetTick(ownOriginal.Space));
        Reject<ArgumentException>(() => NavigationServer.MapIsActive(ownOriginal.Map));
        using var notified = Create(); notified.ResourceName = "notified";
        Action<Resource> fail = _ => throw new InvalidOperationException("Expected accepted world-copy observer failure");
        owned.Changed += fail;
        Reject<InvalidOperationException>(() => owned.CopyFromResource(notified));
        owned.Changed -= fail;
        Check(owned.Space == notified.Space && owned.Canvas == notified.Canvas && owned.NavigationMap == notified.NavigationMap &&
            owned.ResourceName == "notified" && ownChanges == 2,
            "A post-copy observer failure preserves accepted runtime sharing and base state");
        if (backend is null)
        {
            using var retiring = new World("WorldCopyBorrowed") { ResourceName = "retiring" };
            var oldSpace = retiring.Space; var oldSolver = borrowed()!; oldSolver.FailFinish = true;
            var retiredChanges = 0; retiring.Changed += _ => retiredChanges++;
            Exception? retirementError = null;
            try { retiring.CopyFromResource(source); } catch (Exception error) { retirementError = error; }
            Check(retirementError is InvalidOperationException { Message: "Finish failure" },
                "A lone selected retirement failure preserves its original exception without an aggregate wrapper");
            Check(oldSolver.Finished && oldSolver.IsDisposed && retiring.Space == source.Space &&
                retiring.ResourceName == "source" && retiredChanges == 1,
                "Retirement failure preserves the accepted new runtime and balances retained ownership");
            Reject<ArgumentException>(() => PhysicsServer.SpaceGetTick(oldSpace));
            var sharedSpace = source.Space; var sharedMap = source.NavigationMap;
            retiring.Dispose(); source.Dispose();
            Reject<ArgumentException>(() => PhysicsServer.SpaceGetTick(sharedSpace));
            Reject<ArgumentException>(() => NavigationServer.MapIsActive(sharedMap));
        }
        using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
        for (var i = 0; i < 128; i++) Frame();
        Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame();
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, $"World-copy recovery full frames/queries allocate {owner}/{all} B");
        Console.WriteLine($"World copies {backend?.ToString() ?? "registered"}, threaded={ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread)}: bound/expired/owner/callback rejection, aliases, release and 128 full frames/queries {owner}/{all} B passed.");

        void Frame()
        {
            var x = mover.Position.X; tree.PhysicsFrame(1d / 60); query.Position = mover.Position;
            Check(MathF.Abs(mover.Position.X - x - 1) < .02f && bound.DirectSpaceState.IntersectPoint(query, hits) == 1 &&
                hits[0].ColliderRID == mover.GetRID(), "Rejected world copies preserve real simulation and query identity");
        }
        static (string Name, bool Local, string Path, string SceneID, ulong ID, RID Space, RID Canvas, RID Map, bool Translate, string Domain) State(World world) =>
            (world.ResourceName, world.ResourceLocalToScene, world.ResourcePath, world.ResourceSceneUniqueID,
                world.InstanceID, world.Space, world.Canvas, world.NavigationMap, world.CanTranslateMessages, world.TranslationDomain);
    }
}
