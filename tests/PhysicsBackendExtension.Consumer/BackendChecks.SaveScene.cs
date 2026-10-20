using Electron2D;
using IOPath = System.IO.Path;

public static partial class BackendChecks
{
    private static bool _saveSceneRegistered;
    private static SaveSceneNode CreateSaveSceneNode() => new();
    private static SaveSceneBody CreateSaveSceneBody() => new();
    private static void RegisterSaveScene()
    {
        if (_saveSceneRegistered) return;
        ResourceFileTypes.RegisterNode("save-scene/node/1", CreateSaveSceneNode);
        ResourceFileTypes.RegisterNode("save-scene/body/1", CreateSaveSceneBody);
        _saveSceneRegistered = true;
    }
    private sealed class SaveSceneNode : Node
    {
        internal Action<int>? Observe;
        internal Action? BeforeValidation;
        internal Exception? CaptureError;
        internal int Pre, Post;
        protected override Func<Node> CreateSceneInstanceFactory() => CaptureError is { } error ? throw error : CreateSaveSceneNode;
        protected override void ValidateMutation() { BeforeValidation?.Invoke(); base.ValidateMutation(); }
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationEditorPreSave) Pre++;
            else if (what == NotificationEditorPostSave) Post++;
            else return;
            Observe?.Invoke(what);
        }
    }
    private sealed class SaveSceneBody : StaticBody
    {
        internal Action<int>? Observe;
        protected override Func<Node> CreateSceneInstanceFactory() => CreateSaveSceneBody;
        protected override void OnNotification(int what)
        { base.OnNotification(what); if (what is NotificationEditorPreSave or NotificationEditorPostSave) Observe?.Invoke(what); }
    }
    private sealed class SaveSceneFailure(Exception failure, Action check) : ResourceFormatSaver
    {
        public override bool Recognize(Resource resource) => resource is PackedScene;
        public override string[] GetRecognizedExtensions(Resource resource) => ["scene-fail"];
        public override void Save(Resource resource, string path, SaverFlags flags) { check(); throw failure; }
    }
    private static void SaveSceneErrors(Action operation, params Exception[] expected)
    {
        try { operation(); throw new InvalidOperationException("The requested save failure was not reported."); }
        catch (Exception error)
        {
            var actual = error is AggregateException aggregate ? aggregate.Flatten().InnerExceptions.ToArray() : [error];
            Check(actual.Length == expected.Length && actual.Zip(expected).All(pair => ReferenceEquals(pair.First, pair.Second)),
                "Save errors preserve each original operation/cleanup exception in order");
        }
    }

    private static void ConcurrentSceneSave(string path)
    {
        using var root = new SaveSceneNode(); using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        root.Observe = what => { if (what == Node.NotificationEditorPreSave) { entered.Set(); Check(release.Wait(10000), "Concurrent save probe is released"); } };
        var writer = Task.Run(() => ResourceSaver.SaveScene(root, path));
        try
        {
            Check(entered.Wait(10000), "Detached authoring reaches its real pre-save callback on the writer");
            Reject<InvalidOperationException>(() => ResourceSaver.SaveScene(root, path));
            Check(root.Pre == 1 && root.Post == 0, "Concurrent same-root rejection publishes no second lifecycle");
        }
        finally { release.Set(); writer.GetAwaiter().GetResult(); }
        Check(root.Pre == 1 && root.Post == 1 && File.Exists(path), "The original detached writer completes its file and cleanup");
    }

    public static void RunSaveScene(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        RegisterSaveScene(); var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("DeferredContract", () => new Solver());
        var directory = Directory.CreateTempSubdirectory("electron2d-scene-save-");
        try
        {
            ConcurrentSceneSave(IOPath.Combine(directory.FullName, "concurrent.e2dscene"));
            using var frames = new DeferredFrames(backend); using var root = new SaveSceneNode { Name = "Saved" };
            using var shape = new CircleShape { Radius = 2 };
            var body = new SaveSceneBody { Name = "Body", Position = new(100, 0) };
            var collision = new CollisionShape { Name = "Shape", Shape = shape };
            root.AddChild(body); body.Owner = root; body.AddChild(collision); collision.Owner = root;
            var skipped = new SaveSceneNode { Name = "Unowned" }; root.AddChild(skipped, Node.InternalMode.Back);
            frames.Root.AddChild(root);
            var trace = new List<string>();
            var path = IOPath.Combine(directory.FullName, "saved.e2dscene");
            root.Observe = what =>
            {
                trace.Add("root:" + what);
                Reject<InvalidOperationException>(() => ResourceSaver.SaveScene(root, path));
                if (what == Node.NotificationEditorPreSave) { shape.Radius = 3; body.CollisionLayer = 2; }
                else { shape.Radius = 2; body.CollisionLayer = 1; }
            };
            body.Observe = what => trace.Add("body:" + what);
            skipped.Observe = what => trace.Add("internal:" + what);
            Reject<ArgumentNullException>(() => ResourceSaver.SaveScene(null!, path));
            Reject<ArgumentNullException>(() => ResourceSaver.SaveScene(root, null!));
            Reject<ArgumentException>(() => ResourceSaver.SaveScene(root));
            Reject<ArgumentOutOfRangeException>(() => ResourceSaver.SaveScene(root, path, (SaverFlags)128));
            Task.Run(() => Reject<InvalidOperationException>(() => ResourceSaver.SaveScene(root, path))).GetAwaiter().GetResult();
            Check(root.Pre == 0 && root.Post == 0, "Invalid authoring requests publish no callbacks");
            var validation = new ApplicationException("Expected authoring preflight error");
            root.BeforeValidation = () => throw validation;
            SaveSceneErrors(() => ResourceSaver.SaveScene(root, path), validation); root.BeforeValidation = null;
            Check(root.Pre == 0 && root.Post == 0, "Failed capture-access preflight publishes neither save notification");
            root.BeforeValidation = () => Reject<InvalidOperationException>(() => ResourceSaver.SaveScene(root, path));
            ResourceSaver.SaveScene(root, path, SaverFlags.Compress); root.BeforeValidation = null;
            Check(trace.SequenceEqual(["root:9001", "body:9001", "internal:9001", "root:9002", "body:9002", "internal:9002"]),
                "Authoring notifications are parent-first and include unsaved internal children");
            Check(root.SceneFilePath == ProjectSettings.LocalizePath(path) && shape.Radius == 2 && body.CollisionLayer == 1,
                "Committed origin precedes cleanup while temporary physical authoring restores");
            RunIdentityChild(path, backend, threaded, "--save-scene-child", "Fresh saved scene passed");
            var saved = File.ReadAllBytes(path); var originalPath = root.SceneFilePath;
            var capture = new ApplicationException("Expected scene capture error"); root.CaptureError = capture;
            SaveSceneErrors(() => ResourceSaver.SaveScene(root, path), capture); root.CaptureError = null;
            Check(File.ReadAllBytes(path).SequenceEqual(saved) && root.SceneFilePath == originalPath && shape.Radius == 2,
                "Capture failure preserves the saved file/origin and performs post-save cleanup");
            var write = new IOException("Expected scene saver error"); var cleanup = new ApplicationException("Expected post-save error");
            var failPath = IOPath.Combine(directory.FullName, "saved.scene-fail"); File.WriteAllText(failPath, "prior contents");
            using (var saver = new SaveSceneFailure(write, () => Reject<InvalidOperationException>(() => ResourceSaver.SaveScene(root, path))))
            {
                ResourceSaver.AddResourceFormatSaver(saver, true);
                var ordinary = root.Observe; root.Observe = what => { ordinary!(what); if (what == Node.NotificationEditorPostSave) throw cleanup; };
                try { SaveSceneErrors(() => ResourceSaver.SaveScene(root, failPath), write, cleanup); }
                finally { root.Observe = ordinary; ResourceSaver.RemoveResourceFormatSaver(saver); }
            }
            Check(File.ReadAllText(failPath) == "prior contents" && root.SceneFilePath == originalPath && shape.Radius == 2,
                "Saver failure and cleanup error retain the old file/origin and release same-root guard");
            var pre = new ApplicationException("Expected pre-save error"); var after = new ApplicationException("Expected paired cleanup error");
            var normal = root.Observe; root.Observe = what => { normal!(what); throw what == Node.NotificationEditorPreSave ? pre : after; };
            SaveSceneErrors(() => ResourceSaver.SaveScene(root, path), pre, after); root.Observe = normal;
            Check(File.ReadAllBytes(path).SequenceEqual(saved) && shape.Radius == 2 && root.Pre == root.Post && skipped.Pre == skipped.Post,
                "Pre-save failure still visits later receivers, cleans temporary state and leaves the file intact");
            root.Observe = what => { normal!(what); if (what == Node.NotificationEditorPostSave) throw cleanup; };
            var committed = IOPath.Combine(directory.FullName, "committed.e2dscene");
            SaveSceneErrors(() => ResourceSaver.SaveScene(root, committed), cleanup); root.Observe = normal;
            Check(root.SceneFilePath == ProjectSettings.LocalizePath(committed) && File.Exists(committed), "Post-save error does not undo a successful file/origin commit");
            ResourceSaver.SaveScene(root); Check(root.Pre == root.Post, "Default destination and a subsequent save recover after every failure");
            var before = root.Pre;
            using (var packed = new PackedScene()) { packed.Pack(root); packed.PackForSave(root); ResourceSaver.Save(packed, path); }
            Check(root.Pre == before && root.Post == before, "Ordinary resource save and packing publish no authoring notifications");
            using var restored = new PhysicsPointQueryParameters { Position = new(102.5f, 0), CollisionMask = 1 };
            Check(frames.World.DirectSpaceState.IntersectPoint(restored, new PhysicsPointResult[1]) == 0,
                "The live numerical geometry observes post-save restoration before another step");
            for (var i = 0; i < 128; i++) frames.Frame(); Collect(); var position = frames.Body.Position;
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) frames.Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && frames.Body.Position.DistanceTo(position + new Vector2(4, 1)) < .003f,
                $"Cold authoring leaves complete moving physical frames at strict zero managed bytes: {own}/{all}");
            Console.WriteLine($"Scene save {backend?.ToString() ?? "registered"}, threaded={threaded}: real file/fresh process, order/cleanup/errors/reentry, 128 moving full frames at 0/0 B.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); directory.Delete(true); }
    }

    public static void ReadSaveScene(string path, PhysicsServer.Backend? backend, bool threaded)
    {
        RegisterSaveScene(); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("DeferredContract", () => new Solver());
        using var world = backend is { } selected ? new World(selected) : new World("DeferredContract");
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
            "Fresh saved scenes execute the requested backend without fallback");
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); var root = (SaveSceneNode)packed.Instantiate();
        using var tree = new SceneTree(new SubViewport { World = world }); tree.Root.AddChild(root);
        var body = (StaticBody)root.GetNode("Body"); var shape = (CircleShape)((CollisionShape)body.GetNode("Shape")).Shape!;
        Check(body.CollisionLayer == 2 && shape.Radius == 3 && root.GetNodeOrNull("Unowned") is null && root.Pre == 0 && root.Post == 0,
            "Fresh reconstruction retains pre-save state, owner filtering and no artificial authoring notifications");
        using var query = new PhysicsPointQueryParameters { Position = new(102.5f, 0), CollisionMask = 2 };
        var hits = new PhysicsPointResult[2];
        Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == body.GetRID(), "The persisted temporary physical geometry executes on the selected backend");
        tree.PhysicsFrame(1d / 128); Console.WriteLine("Fresh saved scene passed");
    }
}
