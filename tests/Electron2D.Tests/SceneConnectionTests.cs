using Electron2D;
using static TextureArrayTests;
using IOPath = System.IO.Path;

internal static class SceneConnectionTests
{
    private static EventConnection Rename(Node publisher, Sprite subscriber, Vector2 offset) =>
        EventConnection.Subscribe<Node>(h => publisher.Renamed += h, h => publisher.Renamed -= h, _ => subscriber.Position += offset);
    private static EventConnection RenameOnce(Node publisher, Sprite subscriber, Vector2 offset) =>
        EventConnection.Subscribe<Node>(h => publisher.Renamed += h, h => publisher.Renamed -= h, _ => subscriber.Position += offset, oneShot: true);
    private static EventConnection DeferredRename(Node publisher, Sprite subscriber, Vector2 offset) =>
        EventConnection.Subscribe<Node>(h => publisher.Renamed += h, h => publisher.Renamed -= h,
            _ => subscriber.Position += offset, oneShot: true, defer: callback => publisher.Tree!.Defer(callback));
    private static EventConnection RenameResource(Node publisher, Sprite subscriber, CircleShape shape) =>
        EventConnection.Subscribe<Node>(h => publisher.Renamed += h, h => publisher.Renamed -= h, _ => subscriber.Position += new Vector2(shape.Radius, 0));
    internal static void Run()
    {
        if (MissingNodeTests.Child()) return;
        MissingNodeTests.Run();
        var binding = ResourceFileTypes.RegisterSceneConnection<Node, Sprite, Vector2>("tests.rename-position", nameof(Node.Renamed), "Move", Rename, unbinds: 1);
        var once = ResourceFileTypes.RegisterSceneConnection<Node, Sprite, Vector2>("tests.rename-once", nameof(Node.Renamed), "MoveOnce", RenameOnce, unbinds: 1, oneShot: true);
        Throws<InvalidOperationException>(() => ResourceFileTypes.RegisterSceneConnection<Node, Sprite, Vector2>("tests.rename-position", "Renamed", "Move", Rename));
        using var root = new Node { Name = "Root" }; var sprite = new Sprite { Name = "Picture" }; root.AddChild(sprite); sprite.Owner = root;
        using var runtime = EventConnection.Subscribe<Node>(h => root.Renamed += h, h => root.Renamed -= h, _ => sprite.Position = new(99, 99));
        using var first = binding.Subscribe(root, sprite, new(3, 4));
        using var second = binding.Subscribe(root, sprite, new(3, 4));
        using var single = once.Subscribe(root, sprite, new(1, 2));
        using var packed = new PackedScene(); packed.Pack(root); var state = packed.GetState();
        Check(state.GetConnectionCount() == 3 && state.GetConnectionSource(0) == "." && state.GetConnectionTarget(0) == "Picture", "Capture endpoint paths and duplicate order.");
        Check(state.GetConnectionSignal(0) == nameof(Node.Renamed) && state.GetConnectionMethod(0) == "Move" && state.GetConnectionFlags(0) == 2 && state.GetConnectionFlags(2) == 6 && state.GetConnectionUnbinds(0) == 1 && state.GetConnectionBinds<Vector2>(0) == new Vector2(3, 4), "Complete typed connection metadata.");
        Throws<ArgumentOutOfRangeException>(() => state.GetConnectionTarget(-1)); Throws<InvalidOperationException>(() => state.GetConnectionBinds<int>(0));
        using (var instance = packed.Instantiate())
        {
            using var tree = new SceneTree(instance);
            var picture = (Sprite)instance.GetNode("Picture"); instance.Name = "Changed";
            Check(picture.Position == new Vector2(7, 10), "Duplicate and one-shot connections invoke rebound receivers, omitting runtime delegate.");
            instance.Name = "ChangedAgain"; Check(picture.Position == new Vector2(13, 18), "One-shot consumed exactly once.");
            using var recaptured = new PackedScene(); recaptured.Pack(instance); Check(recaptured.GetState().GetConnectionCount() == 2, "Consumed one-shot omitted on repack.");
            picture.Dispose(); instance.Name = "NoReceiver";
        }
        using (var resource = new CircleShape { Radius = 9 })
        {
            resource.ResourceLocalToScene = true;
            var resourceBinding = ResourceFileTypes.RegisterSceneConnection<Node, Sprite, CircleShape>("tests.resource-binding", "Renamed", "RadiusMove", RenameResource);
            using var resourceToken = resourceBinding.Subscribe(root, sprite, resource);
            using var resourceScene = new PackedScene(); resourceScene.Pack(root);
            using var duplicate = (PackedScene)resourceScene.Duplicate(true);
            Check(!ReferenceEquals(duplicate.GetState().GetConnectionBinds<CircleShape>(3), resource), "Deep duplication remaps bound Resource.");
            using var instance = resourceScene.Instantiate(); using var tree = new SceneTree(instance); instance.Name = "Resource";
            Check(((Sprite)instance.GetNode("Picture")).Position == new Vector2(16, 10), "Resource binding executes alongside ordinary payloads.");
        }
        using (var saveScene = new PackedScene())
        {
            saveScene.PackForSave(root); Check(saveScene.GetState().GetConnectionCount() == 3, "RESET save preparation retains authored connections.");
        }
        var deferred = ResourceFileTypes.RegisterSceneConnection<Node, Sprite, Vector2>("tests.deferred", "Renamed", "DeferredMove", DeferredRename, oneShot: true, deferred: true);
        using (var deferredRoot = new Node { Name = "Deferred" })
        {
            var receiver = new Sprite { Name = "Receiver" }; deferredRoot.AddChild(receiver); receiver.Owner = deferredRoot;
            using var tree = new SceneTree(deferredRoot); using var token = deferred.Subscribe(deferredRoot, receiver, new(8, 9));
            deferredRoot.Name = "Queued"; Check(!token.IsConnected && receiver.Position == Vector2.Zero, "Deferred one-shot consumes before safe point.");
            receiver.Dispose(); tree.FlushDeferred(); Check(receiver.IsDisposed, "Endpoint disposal cancels pending delivery.");
            var next = new Sprite { Name = "Next" }; deferredRoot.AddChild(next); using var active = deferred.Subscribe(deferredRoot, next, new(8, 9));
            deferredRoot.Name = "Flush"; tree.FlushDeferred(); Check(next.Position == new Vector2(8, 9), "Deferred callback executes once at safe point.");
            Throws<InvalidOperationException>(() => Task.Run(() => binding.Subscribe(deferredRoot, next, Vector2.One)).GetAwaiter().GetResult());
        }
        var invalid = ResourceFileTypes.RegisterSceneConnection<Node, Sprite, Vector2>("tests.invalid-policy", "Renamed", "Move", Rename, oneShot: true);
        Throws<InvalidOperationException>(() => invalid.Subscribe(root, sprite, Vector2.One));
        var directory = IOPath.Combine(IOPath.GetTempPath(), "e2d-connections-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = IOPath.Combine(directory, "scene.e2dscene"); ResourceSaver.Save(packed, path);
            using var loaded = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
            using var instance = loaded.Instantiate(); using var tree = new SceneTree(instance); instance.Name = "FromFile";
            Check(((Sprite)instance.GetNode("Picture")).Position == new Vector2(7, 10), "File payload and compiled bindings execute after load.");
            Check(loaded.GetState().GetConnectionCount() == 3, "File metadata table retained.");
            var bytes = System.IO.File.ReadAllBytes(path); var identity = System.Text.Encoding.Unicode.GetBytes("tests.rename-position");
            var at = bytes.AsSpan(64).IndexOf(identity); Check(at >= 0, "Portable binding ID present in archive."); bytes[64 + at] = (byte)'X';
            System.Security.Cryptography.SHA256.HashData(bytes.AsSpan(64)).CopyTo(bytes.AsSpan(32, 32));
            var malformed = IOPath.Combine(directory, "unknown.e2dscene"); System.IO.File.WriteAllBytes(malformed, bytes);
            Throws<InvalidDataException>(() => ResourceLoader.Load<PackedScene>(malformed, cacheMode: ResourceLoader.CacheMode.Ignore));
            Check(loaded.GetState().GetConnectionCount() == 3, "Unknown binding rejects before changing existing loaded scene.");
        }
        finally { Directory.Delete(directory, true); }
        first.Dispose(); second.Dispose(); single.Dispose(); packed.Pack(root); Check(state.GetConnectionCount() == 0, "Disposed bindings omitted and live SceneState refreshed.");
        using var outside = new Sprite(); using var external = binding.Subscribe(root, outside, Vector2.One); packed.Pack(root); Check(state.GetConnectionCount() == 0, "Unselected receiver omitted.");
        outside.Dispose(); root.Name = "DisposedReceiver";
        Throws<ObjectDisposedException>(() => binding.Subscribe(root, outside, Vector2.Zero));
        Console.WriteLine("Typed scene connections: capture, metadata, file round trip, rebinding, duplicate, one-shot, disposal and selection passed.");
    }
    internal static void RunNative()
    {
        var binding = ResourceFileTypes.RegisterSceneConnection<Node, Sprite, Vector2>("tests.native-rename", "Renamed", "Move", Rename, unbinds: 1);
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            foreach (var backend in new[] { "gpu", "compatibility" })
            {
                ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
                using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.Red);
                using var texture = ImageTexture.CreateFromImage(image); using var authored = new Node { Name = "Source" };
                var source = new Sprite { Name = "Picture", Texture = texture, Centered = false, Position = new(8, 8) }; authored.AddChild(source); source.Owner = authored;
                using var token = binding.Subscribe(authored, source, new(24, 0)); using var packed = new PackedScene(); packed.Pack(authored);
                var directory = IOPath.Combine(IOPath.GetTempPath(), "e2d-native-connection-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
                try
                {
                    var path = IOPath.Combine(directory, "scene.e2dscene"); ResourceSaver.Save(packed, path, SaverFlags.BundleResources);
                    using var loaded = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
                    var window = new Window { Size = new(64, 48) }; var instance = loaded.Instantiate(); window.AddChild(instance);
                    window.AddChild(new NativeDriver(window, instance));
                    window.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested backend without fallback."); RenderingServer.RenderLoopEnabled = false; RenderingServer.SetDefaultClearColor(Colors.Black); };
                    try { Check(Engine.Run(window) == 0 && window.IsDisposed, "Native connection host cleanup."); } finally { if (!window.IsDisposed) window.Dispose(); }
                    Console.WriteLine("Native typed connections: " + backend + " loaded scene pixels and cleanup passed.");
                }
                finally { Directory.Delete(directory, true); }
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }
    private sealed class NativeDriver(Window window, Node publisher) : Node
    {
        protected override void OnReady() => ProcessEnabled = true;
        protected override void OnProcess(double delta)
        {
            RenderingServer.ForceDraw(false); Pixel(12, Colors.Red);
            publisher.Name = "Moved"; RenderingServer.ForceDraw(false); Pixel(12, Colors.Black); Pixel(36, Colors.Red);
            ((Sprite)publisher.GetNode("Picture")).Dispose(); publisher.Name = "ReceiverDisposed"; RenderingServer.ForceDraw(false); Pixel(36, Colors.Black);
            Tree!.Quit();
        }
        private void Pixel(int x, Color expected)
        {
            using var image = window.GetTexture().GetImage(); var actual = image!.GetPixel(x, 12);
            Check(Math.Abs(actual.R - expected.R) < .02 && Math.Abs(actual.G - expected.G) < .02 && Math.Abs(actual.B - expected.B) < .02, "Loaded connection reaches real rendered pixels.");
        }
    }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
