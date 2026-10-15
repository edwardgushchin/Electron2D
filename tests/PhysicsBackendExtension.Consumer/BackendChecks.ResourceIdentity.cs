using System.Diagnostics;
using Path = System.IO.Path;
using System.Reflection;
using Electron2D;

public static partial class BackendChecks
{
    public static void RunResourceIdentity(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { ResourceIdentity(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void ResourceIdentity(PhysicsServer.Backend? backend, bool threaded)
    {
        var folder = Path.Combine(Path.GetTempPath(), "e2d-resource-identity-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            using var first = new CircleShape { Radius = 3, ResourceName = "first", ResourceSceneUniqueID = "shared_id", CanTranslateMessages = false, TranslationDomain = StoredResourceDomain };
            using var second = new CircleShape { Radius = 6, ResourceName = "second", ResourceSceneUniqueID = "shared_id", TranslationDomain = StoredResourceDomain };
            using var material = new PhysicsMaterial { Friction = .3f, ResourceName = "empty ID", CanTranslateMessages = false, TranslationDomain = StoredResourceDomain };
            using var root = new Node { Name = "Asset" };
            AddBody(root, "First", 0, first); AddBody(root, "Second", 30, second); AddBody(root, "Alias", 60, first);
            var mover = new RigidBody
            {
                Name = "Mover",
                Position = new(0, 100),
                LinearVelocity = new(3, 0),
                GravityScale = 0,
                CanSleep = false,
                LinearDampMode = RigidBody.DampMode.Replace,
                LinearDamp = 0,
                AngularDampMode = RigidBody.DampMode.Replace,
                AngularDamp = 0,
                PhysicsMaterialOverride = material
            };
            root.AddChild(mover); mover.Owner = root; var movingShape = new CollisionShape { Name = "Shape", Shape = first }; mover.AddChild(movingShape); movingShape.Owner = root;
            using var packed = new PackedScene(); packed.Pack(root);
            var blocked = Path.Combine(folder, "blocked"); File.WriteAllText(blocked, "file blocks parent directory");
            second.ResourceSceneUniqueID = "temporary_unique";
            Reject<AggregateException>(() => ResourceSaver.Save(packed, Path.Combine(blocked, "failed.e2dscene")));
            Check(first.ResourceSceneUniqueID == "shared_id" && second.ResourceSceneUniqueID == "temporary_unique" && material.ResourceSceneUniqueID.Length == 0 &&
                first.ResourcePath.Length == 0 && second.ResourcePath.Length == 0 && material.ResourcePath.Length == 0,
                "Failed persistence preserves all source scene IDs and paths");
            second.ResourceSceneUniqueID = "shared_id";
            var path = Path.Combine(folder, "identity.e2dscene");
            ResourceSaver.Save(packed, path);
            Check(first.ResourceSceneUniqueID == "shared_id" && Generated(second.ResourceSceneUniqueID) && Generated(material.ResourceSceneUniqueID) &&
                second.ResourceSceneUniqueID != material.ResourceSceneUniqueID && first.ResourcePath.Length == 0 && second.ResourcePath.Length == 0,
                "Ordinary scene save preserves the first authored ID, repairs collisions and generates empty IDs without replacing paths");
            var ids = (first.ResourceSceneUniqueID, second.ResourceSceneUniqueID, material.ResourceSceneUniqueID);
            ResourceSaver.Save(packed, path, SaverFlags.Compress | SaverFlags.SaveBigEndian | SaverFlags.ReplaceSubresourcePaths);
            Check(ids == (first.ResourceSceneUniqueID, second.ResourceSceneUniqueID, material.ResourceSceneUniqueID) &&
                first.ResourcePath == path + "::" + ids.Item1 && second.ResourcePath == path + "::" + ids.Item2 && material.ResourcePath == path + "::" + ids.Item3,
                "Repeated save retains stable IDs and explicitly replaces unique built-in paths");
            Check(first.IsBuiltIn && second.IsBuiltIn && material.IsBuiltIn, "Committed embedded paths retain built-in classification");
            RunIdentityChild(path, backend, threaded);
            using var saved = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep);
            var loadedRoot = saved.Instantiate(); var loadedFirst = loadedRoot.GetNode<CollisionShape>("First/Shape").Shape!;
            var loadedSecond = loadedRoot.GetNode<CollisionShape>("Second/Shape").Shape!;
            Check(loadedFirst.ResourceSceneUniqueID == ids.Item1 && loadedSecond.ResourceSceneUniqueID == ids.Item2 && loadedFirst != loadedSecond &&
                loadedFirst == loadedRoot.GetNode<CollisionShape>("Alias/Shape").Shape && loadedFirst.ResourceName == "first" && loadedSecond.ResourceName == "second",
                "File graph preserves authored names, corrected IDs and repeated physical resource aliases");
            using var world = backend is { } selected ? new World(selected) : RegisteredIdentityWorld();
            var host = new SubViewport { World = world }; host.AddChild(loadedRoot); using var tree = new SceneTree(host);
            ResourceStoredState(world);
            ResourceCopyFailures(loadedRoot, world);
            ScriptGraphs(loadedRoot, world);
            ResourceEdgeScenes(loadedRoot, world);
            ResourceFactoryOwnership(loadedRoot, world, loadedFirst, loadedRoot.GetNode<RigidBody>("Mover").PhysicsMaterialOverride!);
            var moving = loadedRoot.GetNode<RigidBody>("Mover"); var queriedBody = loadedRoot.GetNode<StaticBody>("Second"); using var query = new PhysicsPointQueryParameters { Position = new(35, 0) };
            Check(!loadedFirst.CanTranslateMessages && loadedFirst.TranslationDomain == StoredResourceDomain &&
                loadedSecond.CanTranslateMessages && loadedSecond.TranslationDomain == StoredResourceDomain &&
                moving.PhysicsMaterialOverride is { CanTranslateMessages: false, TranslationDomain: StoredResourceDomain },
                "Packed/file physical resources retain inherited stored translation settings");
            var hits = new PhysicsPointResult[1];
            for (var i = 0; i < 128; i++) Frame();
            Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Stored resource identity full frames allocate {owner}/{all} B");
            Console.WriteLine($"Resource identity {backend?.ToString() ?? "registered"}, threaded={threaded}: ID repair, stable paths/aliases, actual fresh-process scene and 128 active frames {owner}/{all} B passed.");
            void Frame()
            {
                var x = moving.Position.X; tree.PhysicsFrame(1d / 60);
                Check(MathF.Abs(moving.Position.X - x - .05f) < .00005f && world.DirectSpaceState.IntersectPoint(query, hits) == 1 &&
                    hits[0].ColliderRID == queriedBody.GetRID() && loadedFirst.ResourceSceneUniqueID == ids.Item1,
                    "Loaded resource metadata preserves selected active simulation and physical query identity");
            }
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void AddBody(Node root, string name, float x, Shape shape)
    {
        var body = new StaticBody { Name = name, Position = new(x, 0) }; root.AddChild(body); body.Owner = root;
        var child = new CollisionShape { Name = "Shape", Shape = shape }; body.AddChild(child); child.Owner = root;
    }
    private static bool Generated(string id) => id.Length == 5 && id.All(c => c is >= 'a' and <= 'y' or >= '0' and <= '8');
    private static World RegisteredIdentityWorld()
    {
        PhysicsServerManager.RegisterServer("ResourceIdentity", () => new Solver()); return new World("ResourceIdentity");
    }
    private static void RunIdentityChild(string path, PhysicsServer.Backend? backend, bool threaded, string selector = "--resource-identity-child", string expected = "Fresh resource identity passed")
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
        if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, Assembly.GetEntryAssembly()!.GetName().Name + ".dll"));
        start.ArgumentList.Add(backend?.ToString().ToLowerInvariant() ?? "registered");
        if (threaded) start.ArgumentList.Add("threaded");
        start.ArgumentList.Add(selector); start.ArgumentList.Add(path);
        using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh resource identity scene."); }
        Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains(expected), error.GetAwaiter().GetResult());
    }

    public static void ReadResourceIdentity(string path, PhysicsServer.Backend? backend, bool threaded)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        using var unrelated = new Node(); using var unrelatedShape = new CircleShape();
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); var root = packed.Instantiate();
        var first = root.GetNode<CollisionShape>("First/Shape").Shape!; var second = root.GetNode<CollisionShape>("Second/Shape").Shape!;
        Check(!first.CanTranslateMessages && first.TranslationDomain == StoredResourceDomain &&
            second.CanTranslateMessages && second.TranslationDomain == StoredResourceDomain &&
            root.GetNode<RigidBody>("Mover").PhysicsMaterialOverride is { CanTranslateMessages: false, TranslationDomain: StoredResourceDomain },
            "Fresh-process physical resources preserve inherited stored translation settings");
        Check(first.ResourceSceneUniqueID == "shared_id" && Generated(second.ResourceSceneUniqueID) && first != second &&
            first == root.GetNode<CollisionShape>("Alias/Shape").Shape, "Fresh process restores portable IDs independently of local object/RID creation order");
        using var world = backend is { } selected ? new World(selected) : RegisteredIdentityWorld();
        var host = new SubViewport { World = world }; host.AddChild(root); using var tree = new SceneTree(host); tree.PhysicsFrame(1d / 60);
        using var query = new PhysicsPointQueryParameters { Position = new(35, 0) }; var hits = new PhysicsPointResult[1];
        Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == root.GetNode<StaticBody>("Second").GetRID(),
            "Fresh process executes real selected physics after saved graph reconstruction");
        if (backend is { } requested) Check(world.PhysicsBackend == requested, "Requested backend executes without fallback");
        Console.WriteLine("Fresh resource identity passed");
    }
}
