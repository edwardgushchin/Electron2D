using System.Runtime.CompilerServices;
using System.Text;
using Electron2D;
using Path = System.IO.Path;

public static partial class BackendChecks
{
    public static void RunResourcePathIDs(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        var folder = Path.Combine(Path.GetTempPath(), "e2d-path-ids-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            VerifyResourcePathIDs();
            using var first = new CircleShape { Radius = 3, ResourceSceneUniqueID = "embedded_first" };
            using var second = new CircleShape { Radius = 6, ResourceSceneUniqueID = "embedded_second" };
            var firstPath = Path.Combine(folder, "first.e2dres"); var secondPath = Path.Combine(folder, "second.e2dres");
            ResourceSaver.Save(first, firstPath); ResourceSaver.Save(second, secondPath);
            first.SetPathCache(firstPath); second.SetPathCache(secondPath);
            using var packed = PathIDScene(first, second);
            using var saver = new PathIDSaver(); using var loader = new PathIDLoader();
            ResourceSaver.AddResourceFormatSaver(saver, true); ResourceLoader.AddResourceFormatLoader(loader, true);
            var path = Path.Combine(folder, "graph.e2dpathids");
            try
            {
                first.SetIDForPath(path, "shape label::Ω\nfirst");
                ResourceSaver.Save(packed, path); var bytes = File.ReadAllBytes(path);
                Check(second.GetIDForPath(path) == "shape_2" && first.ResourceSceneUniqueID == "embedded_first" && second.ResourceSceneUniqueID == "embedded_second",
                    "A custom format commits its own per-file labels independently of embedded IDs");
                ResourceSaver.Save(packed, path);
                Check(bytes.AsSpan().SequenceEqual(File.ReadAllBytes(path)), "Repeated format saves keep identical dependency labels and bytes");
                second.SetIDForPath(path, ""); saver.Fail = true;
                Reject<AggregateException>(() => ResourceSaver.Save(packed, path));
                Check(second.GetIDForPath(path) == "" && bytes.AsSpan().SequenceEqual(File.ReadAllBytes(path)), "Failed custom save preserves the file and does not publish staged IDs");
                saver.Fail = false; ResourceSaver.Save(packed, path);
                RunIdentityChild(path, backend, threaded, "--resource-path-ids-child", "Fresh resource path IDs passed");
                using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep);
                CheckPathIDScene(loaded, path, backend, threaded, true);
            }
            finally
            {
                first.SetIDForPath(path, ""); second.SetIDForPath(path, "");
                ResourceSaver.RemoveResourceFormatSaver(saver); ResourceLoader.RemoveResourceFormatLoader(loader);
            }
            Console.WriteLine($"Resource path IDs {backend?.ToString() ?? "registered"}, threaded={threaded}: exact path pairs, alias/lifetime, custom format, fresh process and 128 full frames passed.");
        }
        finally { Directory.Delete(folder, true); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    public static void VerifyResourcePathIDs()
    {
        var prefix = "path-ids-" + Guid.NewGuid(); var path = prefix + "/scene"; var resourcePath = prefix + "/resource";
        using var source = new CircleShape { ResourceSceneUniqueID = "embedded" };
        using var alias = new CircleShape(); source.SetPathCache(resourcePath); alias.SetPathCache(resourcePath);
        var changes = 0; source.Changed += _ => changes++;
        try
        {
            Check(source.GetIDForPath(path) == "", "Missing path pairs return empty");
            source.SetIDForPath(path, "opaque / Ω\nID");
            Check(alias.GetIDForPath(path) == "opaque / Ω\nID" && source.GetIDForPath(path.ToUpperInvariant()) == "" && source.GetIDForPath(path + "/../scene") == "",
                "Aliases share exact ordinal pair keys without normalization or scene-ID validation");
            source.SetIDForPath(path + "2", "other"); alias.SetIDForPath(path, "replacement");
            source.SetPathCache(resourcePath + "2"); Check(source.GetIDForPath(path) == "", "Changing resource path selects another cache key");
            source.SetIDForPath(path, "new-path"); source.SetPathCache(resourcePath);
            using var copy = source.Duplicate();
            Check(source.GetIDForPath(path) == "replacement" && source.GetIDForPath(path + "2") == "other" && copy.GetIDForPath(path) == "",
                "Old keys survive path changes; duplication without a path does not copy instance metadata");
            alias.CopyFromResource(source);
            Check(alias.GetIDForPath(path) == "replacement" && changes == 0 && source.ResourceSceneUniqueID == "embedded", "Copy and authoring IDs preserve path identity, content events and embedded IDs");
            Reject<ArgumentNullException>(() => source.GetIDForPath(null!));
            Reject<ArgumentNullException>(() => source.SetIDForPath(null!, "x"));
            Reject<ArgumentNullException>(() => source.SetIDForPath(path, null!));
            Parallel.For(0, 32, i => { using var item = new Resource(); item.SetPathCache(resourcePath); var key = path + "/thread/" + i; item.SetIDForPath(key, "thread"); Check(item.GetIDForPath(key) == "thread", "Concurrent authoring retains pair values"); item.SetIDForPath(key, ""); });
            var weak = PathIDWeakResource(resourcePath + "/weak", path); Collect();
            Check(!weak.TryGetTarget(out _), "ID cache retains no resource instance");
            using var reload = new Resource(); reload.SetPathCache(resourcePath + "/weak");
            Check(reload.GetIDForPath(path) == "retained", "A new identity at the same path retains the process authoring ID"); reload.SetIDForPath(path, "");
            using var retired = new Resource(); retired.Dispose();
            Reject<ObjectDisposedException>(() => retired.GetIDForPath(path)); Reject<ObjectDisposedException>(() => retired.SetIDForPath(path, "x"));
            Resource[] family = [new Resource(), new CircleShape(), new RectangleShape(), new CapsuleShape(), new SegmentShape(), new ConvexPolygonShape(), new ConcavePolygonShape(), new SeparationRayShape(), new WorldBoundaryShape(), new CustomShape(Annulus.Create(2, 4)), new SpaceCircle(), new PhysicsMaterial(), new World(), new PackedScene()];
            try
            {
                for (var i = 0; i < family.Length; i++)
                {
                    var item = family[i]; item.SetPathCache(resourcePath + "/family/" + i); item.SetIDForPath(path, "inherited");
                    using var view = new Resource(); view.SetPathCache(item.ResourcePath);
                    Check(view.GetIDForPath(path) == "inherited", "Every physical resource role uses the same inherited path-ID namespace");
                    item.SetIDForPath(path, "");
                }
            }
            finally { foreach (var item in family) item.Dispose(); }
            using var registered = new Resource { ResourcePath = resourcePath + "/owned" }; registered.SetIDForPath(path, "owned");
            using var replacement = new Resource(); replacement.TakeOverPath(registered.ResourcePath);
            Check(registered.ResourcePath == "" && replacement.GetIDForPath(path) == "owned", "Path takeover retains path-qualified IDs independently of displaced object identity");
            replacement.SetIDForPath(path, "");
            source.SetIDForPath(path, ""); Check(alias.GetIDForPath(path) == "", "An empty ID removes the shared entry");
            source.SetPathCache(""); source.SetIDForPath("", "empty pair"); alias.SetPathCache("");
            Check(alias.GetIDForPath("") == "empty pair", "Empty paths remain exact keys rather than aliases for a filesystem path");
            source.SetIDForPath("", "");
        }
        finally
        {
            source.SetPathCache(resourcePath); source.SetIDForPath(path, ""); source.SetIDForPath(path + "2", "");
            source.SetPathCache(resourcePath + "2"); source.SetIDForPath(path, "");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Resource> PathIDWeakResource(string resourcePath, string path)
    {
        var resource = new Resource(); resource.SetPathCache(resourcePath); resource.SetIDForPath(path, "retained"); return new(resource);
    }

    private static PackedScene PathIDScene(Shape first, Shape second)
    {
        using var root = new Node { Name = "Asset" }; AddBody(root, "First", 0, first); AddBody(root, "Second", 30, second); AddBody(root, "Alias", 60, first);
        var mover = new RigidBody { Name = "Mover", GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0, LinearVelocity = new(3, 0), Position = new(0, 100) };
        root.AddChild(mover); mover.Owner = root; var child = new CollisionShape { Name = "Shape", Shape = first }; mover.AddChild(child); child.Owner = root;
        var packed = new PackedScene(); try { packed.Pack(root); return packed; } catch { packed.Dispose(); throw; }
    }

    private sealed class PathIDSaver : ResourceFormatSaver
    {
        internal bool Fail;
        public override string[] GetRecognizedExtensions(Resource resource) => ["e2dpathids"];
        public override bool Recognize(Resource resource) => resource is PackedScene;
        public override void Save(Resource resource, string path, SaverFlags flags)
        {
            using var root = ((PackedScene)resource).Instantiate();
            var shapes = new[] { root.GetNode<CollisionShape>("First/Shape").Shape!, root.GetNode<CollisionShape>("Second/Shape").Shape! };
            var ids = new string[2]; var lines = new string[5]; lines[0] = "E2D_PATH_IDS_1";
            for (var i = 0; i < shapes.Length; i++)
            {
                ids[i] = shapes[i].GetIDForPath(path); if (ids[i].Length == 0) ids[i] = "shape_" + (i + 1);
                lines[1 + 2 * i] = Convert.ToBase64String(Encoding.UTF8.GetBytes(shapes[i].ResourcePath));
                lines[2 + 2 * i] = Convert.ToBase64String(Encoding.UTF8.GetBytes(ids[i]));
            }
            if (Fail) throw new IOException("Injected format encoding failure.");
            var temporary = path + ".writing";
            try { File.WriteAllLines(temporary, lines); File.Move(temporary, path, true); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            for (var i = 0; i < shapes.Length; i++) shapes[i].SetIDForPath(path, ids[i]);
        }
    }

    private sealed class PathIDLoader : ResourceFormatLoader
    {
        public override string[] GetRecognizedExtensions() => ["e2dpathids"];
        public override bool HandlesType(Type type) => type.IsAssignableFrom(typeof(PackedScene));
        public override Type? GetResourceType(string path) => typeof(PackedScene);
        public override Resource Load(string path, string originalPath, bool useSubThreads, ResourceLoader.CacheMode cacheMode)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length != 5 || lines[0] != "E2D_PATH_IDS_1") throw new InvalidDataException("Invalid path-ID scene.");
            var firstPath = Encoding.UTF8.GetString(Convert.FromBase64String(lines[1])); var firstID = Encoding.UTF8.GetString(Convert.FromBase64String(lines[2]));
            var secondPath = Encoding.UTF8.GetString(Convert.FromBase64String(lines[3])); var secondID = Encoding.UTF8.GetString(Convert.FromBase64String(lines[4]));
            if (firstID.Length == 0 || secondID.Length == 0) throw new InvalidDataException("Missing path ID.");
            var first = ResourceLoader.Load<CircleShape>(firstPath, ResourceLoader.CacheMode.Reuse);
            var second = ResourceLoader.Load<CircleShape>(secondPath, ResourceLoader.CacheMode.Reuse);
            var packed = PathIDScene(first, second);
            first.SetIDForPath(originalPath, firstID); second.SetIDForPath(originalPath, secondID); return packed;
        }
    }

    public static void ReadResourcePathIDs(string path, PhysicsServer.Backend? backend, bool threaded)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        var lines = File.ReadAllLines(path); using var probe = new CircleShape(); probe.SetPathCache(Encoding.UTF8.GetString(Convert.FromBase64String(lines[1])));
        Check(probe.GetIDForPath(path) == "", "Fresh process starts without the source process cache");
        using var loader = new PathIDLoader(); ResourceLoader.AddResourceFormatLoader(loader, true);
        try { using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); CheckPathIDScene(packed, path, backend, threaded, false); }
        finally { ResourceLoader.RemoveResourceFormatLoader(loader); }
        Console.WriteLine("Fresh resource path IDs passed");
    }

    private static void CheckPathIDScene(PackedScene packed, string path, PhysicsServer.Backend? backend, bool threaded, bool measure)
    {
        var root = packed.Instantiate(); var first = root.GetNode<CollisionShape>("First/Shape").Shape!; var second = root.GetNode<CollisionShape>("Second/Shape").Shape!;
        Check(first == root.GetNode<CollisionShape>("Alias/Shape").Shape && first.GetIDForPath(path) == "shape label::Ω\nfirst" && second.GetIDForPath(path) == "shape_2", "Loaded format restores labels and aliased physical resources");
        using var world = backend is { } selected ? new World(selected) : RegisteredIdentityWorld();
        var host = new SubViewport { World = world }; host.AddChild(root); using var tree = new SceneTree(host);
        var moving = root.GetNode<RigidBody>("Mover"); using var query = new PhysicsPointQueryParameters { Position = new(35, 0) }; var hits = new PhysicsPointResult[1];
        for (var i = 0; i < 128; i++) Frame();
        Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < (measure ? 128 : 1); i++) Frame();
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, $"Path ID full frames allocate {owner}/{all} B");
        if (backend is { } requested) Check(world.PhysicsBackend == requested, "Physical backend selection remains explicit");
        void Frame()
        {
            var x = moving.Position.X; tree.PhysicsFrame(1d / 60);
            first.SetIDForPath(path, "shape label::Ω\nfirst"); second.SetIDForPath(path, "shape_2");
            Check(first.GetIDForPath(path) == "shape label::Ω\nfirst" && second.GetIDForPath(path) == "shape_2" &&
                MathF.Abs(moving.Position.X - x - .05f) < .00005f && world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == root.GetNode<StaticBody>("Second").GetRID(), "Warm authoring metadata preserves actual physics and public query identity");
        }
    }
}
