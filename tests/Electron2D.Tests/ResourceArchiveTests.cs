using Electron2D;
internal static class ResourceArchiveTests
{
    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    private static void Reject<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
    private sealed class Data : Resource
    {
        internal int Number;
        internal Data? Next;
        internal bool RejectCapture;
        internal int EditorTag;
        internal bool RejectCopy;
        protected override Resource CreateDuplicateInstance() => new Data();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> copy, Func<Resource?, Resource?> force)
        {
            var data = (Data)target;
            data.Number = Number; data.EditorTag = EditorTag;
            data.Next = Next;
            if (RejectCopy) throw new InvalidOperationException("Copy fixture");
        }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
        {
            foreach (var p in base.GetPropertyDescriptors()) yield return p;
            yield return new PropertyDescriptor<Data, int>("Number", n => n.RejectCapture ? throw new InvalidOperationException("Capture fixture") : n.Number, (n, v) => n.Number = v, _ => 0, stored: true);
            yield return new PropertyDescriptor<Data, int>("__editor_tag", n => n.EditorTag, (n, v) => n.EditorTag = v, _ => 0, stored: true);
            yield return new PropertyDescriptor<Data, Data?>("Next", n => n.Next, (n, v) => n.Next = v, _ => null, stored: true);
        }
    }
    private class ExpectedResource : Resource { }
    private sealed class WrongResource : ExpectedResource { }
    private static WrongResource? LastWrong;
    private static ExpectedResource WrongFactory() => LastWrong = new WrongResource();
    private sealed class Actor : Entity
    {
        internal Data? Data;
        internal Actor? Partner;
        internal int Number;
        internal int ReadyNumber;
        protected override void OnReady() => ReadyNumber = Number;
        protected override Func<Node> CreateSceneInstanceFactory() => CreateActor;
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
        {
            foreach (var p in base.GetPropertyDescriptors()) yield return p;
            yield return new PropertyDescriptor<Actor, Data?>("Data", n => n.Data, (n, v) => n.Data = v, _ => null, stored: true);
            yield return new PropertyDescriptor<Actor, Actor?>("Partner", n => n.Partner, (n, v) => n.Partner = v, _ => null, stored: true);
            yield return new PropertyDescriptor<Actor, int>("Number", n => n.Number, (n, v) => n.Number = v, _ => 0, stored: true);
        }
    }
    private static Data CreateData() => new();
    private static Actor CreateActor() => new();
    internal static void Run()
    {
        ResourceFileTypes.RegisterResource("tests.Data", CreateData);
        ResourceFileTypes.RegisterNode("tests.Actor", CreateActor);
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-archive-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            foreach (var flags in new[] {
 SaverFlags.None, SaverFlags.Compress | SaverFlags.SaveBigEndian, SaverFlags.BundleResources | SaverFlags.ReplaceSubresourcePaths }
           ) Graph(folder, flags);
            Scene(folder);
            Dependencies(folder);
            Pixels(folder);
            Formats(folder);
            SceneFiles(folder);
            Composite(folder);
            SceneReplacement(folder); FileReplication(folder); CopyOwnership(folder); StateOwnership(folder); EmptyAndEditor(folder);
            FailureBoundaries(folder);
            FreshProcess(folder);
            Console.WriteLine("Typed resource graph and scene archive checks passed.");
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
    private static void Graph(string folder, SaverFlags flags)
    {
        var path = System.IO.Path.Combine(folder, "data.e2dres");
        using var root = new Data
        {
            Number = 14,
            ResourceName = "root\0\uFEFF\uD800"
        };
        using var child = new Data
        {
            Number = 29
        };
        root.Next = child;
        child.Next = root;
        ResourceSaver.Save(root, path, flags);
        using var loaded = ResourceLoader.Load<Data>(path, ResourceLoader.CacheMode.Ignore);
        Check(loaded.Number == 14 && loaded.Next!.Number == 29 && ReferenceEquals(loaded.Next.Next, loaded), "Cycle/alias preservation.");
        Check(loaded.ResourceName == root.ResourceName, "Resource metadata.");
        var uid = ResourceSaver.GetResourceIDForPath(path);
        Check(uid >= 0 && ResourceUID.TextToID(ResourceUID.IDToText(uid)) == uid, "UID roundtrip.");
        using var cached = ResourceLoader.Load<Data>(path);
        root.Number = 19;
        ResourceSaver.Save(root, path);
        Check(ReferenceEquals(ResourceLoader.Load<Data>(path, ResourceLoader.CacheMode.Replace), cached) && cached.Number == 19 && ReferenceEquals(cached.Next!.Next, cached), "Replace identity and redirected cycle.");
        var bytes = System.IO.File.ReadAllBytes(path);
        bytes[^1] ^= 1;
        System.IO.File.WriteAllBytes(path, bytes);
        Reject<InvalidDataException>(() => ResourceLoader.Load<Data>(path, ResourceLoader.CacheMode.Replace));
        Check(cached.Number == 19, "Corrupt replacement preserves cache.");
    }
    private static void Dependencies(string folder)
    {
        var dependencyPath = System.IO.Path.Combine(folder, "dependency.e2dres");
        var rootPath = System.IO.Path.Combine(folder, "external.e2dres");
        using var dependency = new Data
        {
            Number = 72
        };
        ResourceSaver.Save(dependency, dependencyPath);
        dependency.ResourcePath = dependencyPath;
        using var root = new Data
        {
            Number = 8,
            Next = dependency
        };
        ResourceSaver.Save(root, rootPath, SaverFlags.RelativePaths);
        var tokens = ResourceLoader.GetDependencies(rootPath, true);
        Check(tokens.Length == 1 && tokens[0].Contains("::tests.Data::dependency.e2dres"), "Typed UID dependency/fallback token.");
        using var loaded = ResourceLoader.Load<Data>(rootPath, ResourceLoader.CacheMode.Ignore);
        Check(ReferenceEquals(loaded.Next, dependency), "Ordinary Ignore reuses external dependency.");
        using var deep = ResourceLoader.Load<Data>(rootPath, ResourceLoader.CacheMode.IgnoreDeep);
        Check(!ReferenceEquals(deep.Next, dependency) && deep.Next!.Number == 72, "Deep ignore loads independent dependency.");
        ResourceSaver.Save(root, rootPath, SaverFlags.BundleResources);
        Check(ResourceLoader.GetDependencies(rootPath).Length == 0, "Bundled graph removes external dependencies.");
        var uid = ResourceSaver.GetResourceIDForPath(rootPath);
        ResourceSaver.SetUID(rootPath, uid + 1);
        Check(ResourceLoader.GetResourceUID(rootPath) == uid + 1, "UID replacement.");
        Check(ResourceUID.EnsurePath(ResourceUID.IDToText(uid + 1)) == rootPath, "UID path resolution.");
        ResourceSaver.Save(root, rootPath, SaverFlags.ChangePath);
        Check(root.ResourcePath == "" && dependency.ResourcePath == dependencyPath, "Temporary ChangePath restores original ownership.");
    }
    private static void Pixels(string folder)
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        image.Fill(new Color(1, .2f, .3f, 1));
        using var texture = ImageTexture.CreateFromImage(image);
        using var atlas = new AtlasTexture
        {
            Atlas = texture,
            Region = new Rect2(0, 0, 1, 1)
        };
        var root = new Sprite
        {
            Name = "Sprite:@%\0\uFEFF",
            Texture = atlas
        };
        using var scene = new PackedScene();
        scene.Pack(root);
        root.Dispose();
        var path = System.IO.Path.Combine(folder, "pixels.e2dscene");
        ResourceSaver.Save(scene, path);
        using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore);
        using var sprite = (Sprite)loaded.Instantiate();
        loaded.Dispose();
        using var pixels = sprite.Texture!.GetImage();
        Check(sprite.Name == "Sprite:@%\0\uFEFF" && pixels is not null && pixels.Width == 1 && pixels.GetPixel(0, 0).R == 1, "Serialized sprite/atlas/image texture has real copied pixels.");
    }
    private static void Composite(string folder)
    {
        using var font = new FontFile
        {
            Data = ThemeDB.FallbackFont is FontFile defaultFont ? defaultFont.Data : throw new InvalidOperationException("Default font fixture."),
            OpenTypeFeatureOverrides = new()
            {
                ["liga"] = 0
            }
        };
        using var fallback = new FontFile();
        font.Fallbacks = [fallback, fallback];
        var fontPath = System.IO.Path.Combine(folder, "font.e2dres");
        ResourceSaver.Save(font, fontPath, SaverFlags.Compress);
        using var loadedFont = ResourceLoader.Load<FontFile>(fontPath, ResourceLoader.CacheMode.Ignore);
        Check(loadedFont.Data.AsSpan().SequenceEqual(font.Data) && loadedFont.OpenTypeFeatureOverrides["liga"] == 0 && loadedFont.Fallbacks.Length == 2 && ReferenceEquals(loadedFont.Fallbacks[0], loadedFont.Fallbacks[1]) && loadedFont.GetStringSize("File AV").X > 0, "Native font data, features, resource arrays and layout roundtrip.");
        using var style = new StyleBoxFlat
        {
            BGColor = new Color(.3f, .4f, .5f, 1)
        };
        var control = new Button
        {
            Name = "UI",
            Text = "Saved"
        };
        control.AddThemeStyleBoxOverride("normal", style);
        control.AddThemeFontOverride("font", font);
        using var packed = new PackedScene();
        packed.Pack(control);
        control.Dispose();
        var path = System.IO.Path.Combine(folder, "ui.e2dscene");
        ResourceSaver.Save(packed, path, SaverFlags.BundleResources);
        using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore);
        using var button = (Button)loaded.Instantiate();
        Check(button.Text == "Saved" && button.GetThemeStyleBox("normal") is StyleBoxFlat
        {
            BGColor.R: .3f
        }
 && button.GetThemeFont("font")!.GetStringSize("AV").X > 0, "Typed UI scene/theme/font executes.");
        using var shape = new CapsuleShape
        {
            Radius = 5,
            Height = 18
        };
        var shapePath = System.IO.Path.Combine(folder, "shape.e2dres");
        ResourceSaver.Save(shape, shapePath);
        using var restoredShape = ResourceLoader.Load<CapsuleShape>(shapePath, ResourceLoader.CacheMode.Ignore);
        Check(restoredShape.Radius == 5 && restoredShape.Height == 18, "Stored shape geometry.");
    }
    private static void FailureBoundaries(string folder)
    {
        var path = System.IO.Path.Combine(folder, "bounds.e2dres");
        using var root = new Data
        {
            Number = 9
        };
        ResourceSaver.Save(root, path, SaverFlags.Compress);
        var original = System.IO.File.ReadAllBytes(path); root.RejectCapture = true; Reject<AggregateException>(() => ResourceSaver.Save(root, path, SaverFlags.ChangePath)); root.RejectCapture = false; Check(System.IO.File.ReadAllBytes(path).AsSpan().SequenceEqual(original) && root.ResourcePath == "", "Failed authoring preserves destination and temporary path.");
        ResourceFileTypes.RegisterResource("tests.Expected", WrongFactory); using var expected = new ExpectedResource(); var badFactoryPath = System.IO.Path.Combine(folder, "bad-factory.e2dres"); ResourceSaver.Save(expected, badFactoryPath); Reject<InvalidOperationException>(() => ResourceLoader.Load<ExpectedResource>(badFactoryPath, ResourceLoader.CacheMode.Ignore)); Check(LastWrong!.IsDisposed && !expected.IsDisposed && !ResourceLoader.HasCached(badFactoryPath), "Wrong fresh factory result is cleaned up without cache publication/source disposal.");
        var directoryID = ResourceUID.CreateID(); ResourceUID.AddID(directoryID, folder); using (var directory = DirAccess.Open(ResourceUID.IDToText(directoryID))) Check(directory.FileExists("bounds.e2dres") && DirAccess.DirExistsAbsolute(ResourceUID.IDToText(directoryID)), "UID directory resolution applies the existing scope."); ResourceUID.RemoveID(directoryID);
        Check(!ResourceLoader.Exists<PackedScene>(path), "Exists honors the exact archive root type.");
        Reject<InvalidDataException>(() => ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore));
        foreach (var offset in new[] {
 8, 12, 24, 32 }
)
        {
            var bad = (byte[])original.Clone();
            bad[offset] ^= 128;
            System.IO.File.WriteAllBytes(path, bad);
            Reject<InvalidDataException>(() => ResourceLoader.Load<Data>(path, ResourceLoader.CacheMode.Ignore));
        }
        Check(ResourceLoader.GetCachedRef<Data>(path) is null, "Failed loads publish no cache entries.");
        Reject<InvalidDataException>(() => ResourceSaver.SetUID(path, 1));
        System.IO.File.WriteAllBytes(path, original);
        var depPath = System.IO.Path.Combine(folder, "owned.e2dres");
        using var dependency = new Data
        {
            Number = 2
        };
        ResourceSaver.Save(dependency, depPath);
        dependency.ResourcePath = depPath;
        root.Next = dependency;
        ResourceSaver.Save(root, path);
        var deep = ResourceLoader.Load<Data>(path, ResourceLoader.CacheMode.IgnoreDeep);
        var owned = deep.Next!;
        deep.Dispose();
        Check(owned.IsDisposed && !dependency.IsDisposed, "Deep dependencies are owned while reused dependencies stay borrowed.");
        using var cached = ResourceLoader.Load<Data>(path);
        var uid = ResourceLoader.GetResourceUID(path);
        Check(ResourceLoader.HasCached(ResourceUID.IDToText(uid)) && ReferenceEquals(ResourceLoader.GetCachedRef<Data>(ResourceUID.IDToText(uid)), cached), "UID cache identity.");
        var renamed = System.IO.Path.Combine(folder, "renamed.e2dres");
        System.IO.File.Move(depPath, renamed);
        ResourceLoader.RenameDependencies(path, new Dictionary<string, string>
        {
            [depPath] = renamed
        }
       );
        using var rewritten = ResourceLoader.Load<Data>(path, ResourceLoader.CacheMode.IgnoreDeep);
        Check(rewritten.Next!.Number == 2, "Atomic dependency rewrite loads its new target.");
    }
    private static void SceneReplacement(string folder)
    {
        var path = System.IO.Path.Combine(folder, "retained.e2dscene");
        using var firstStyle = new StyleBoxFlat
        {
            BGColor = new Color(1, 0, 0, 1)
        };
        var source = new Button
        {
            Name = "UI"
        };
        source.AddThemeStyleBoxOverride("normal", firstStyle);
        using var authored = new PackedScene();
        authored.Pack(source);
        source.Dispose();
        ResourceSaver.Save(authored, path);
        using var cached = ResourceLoader.Load<PackedScene>(path);
        using var oldNode = (Button)cached.Instantiate();
        var oldStyle = oldNode.GetThemeStyleBox("normal")!;
        var replacement = new Button
        {
            Name = "UI2"
        };
        using var secondStyle = new StyleBoxFlat
        {
            BGColor = new Color(0, 0, 1, 1)
        };
        replacement.AddThemeStyleBoxOverride("normal", secondStyle);
        authored.Pack(replacement);
        replacement.Dispose();
        ResourceSaver.Save(authored, path);
        ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Replace);
        using var newNode = (Button)cached.Instantiate();
        cached.Dispose();
        Check(!oldStyle.IsDisposed && ((StyleBoxFlat)newNode.GetThemeStyleBox("normal")!).BGColor == new Color(0, 0, 1, 1), "Old and new file graphs survive template Replace/disposal.");
        oldNode.ReplaceBy(newNode); oldNode.Dispose(); Check(!oldStyle.IsDisposed, "Node replacement transfers and merges file graph leases."); newNode.Dispose();
        Check(oldStyle.IsDisposed, "Final instance releases its retired file graph.");
    }
    private static void FileReplication(string folder)
    {
        var path = System.IO.Path.Combine(folder, "level.e2dscene"); var remotePath = System.IO.Path.Combine(folder, "remote.e2dscene"); using (var source = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore)) ResourceSaver.Save(source, remotePath, SaverFlags.BundleResources);
        using var host = new ENetMultiplayerPeer(); host.SetBindIP("127.0.0.1"); host.CreateServer(0); using var client = new ENetMultiplayerPeer(); client.CreateClient("127.0.0.1", host.Host!.GetLocalPort());
        using var serverAPI = new SceneMultiplayer { MultiplayerPeer = host }; using var clientAPI = new SceneMultiplayer { MultiplayerPeer = client };
        Node Branch(string file, out MultiplayerSpawner spawner) { var root = new Node { Name = "Root" }; root.AddChild(new Node { Name = "Actors" }); spawner = new MultiplayerSpawner { Name = "Spawner", SpawnPath = "../Actors" }; root.AddChild(spawner); spawner.AddSpawnableScene(file); return root; }
        var serverRoot = Branch(path, out var serverSpawner); var clientRoot = Branch(remotePath, out var clientSpawner); using var serverTree = new SceneTree(serverRoot); using var clientTree = new SceneTree(clientRoot); serverTree.SetMultiplayer(serverAPI); clientTree.SetMultiplayer(clientAPI);
        void Wait(Func<bool> ready) { var end = Environment.TickCount64 + 10000; while (!ready()) { serverTree.ProcessFrame(0); clientTree.ProcessFrame(0); if (Environment.TickCount64 > end) throw new TimeoutException("File scene replication."); Thread.Yield(); } }
        Wait(() => serverAPI.GetPeers().Length == 1 && clientAPI.GetPeers().Length == 1);
        var local = (Actor)serverSpawner.GetSpawnableScene(0).Instantiate(); local.Name = "Spawned"; serverRoot.GetNode("Actors").AddChild(local); Wait(() => clientRoot.GetNodeOrNull("Actors/Spawned") is not null);
        var remote = clientRoot.GetNode<Actor>("Actors/Spawned"); Check(remote.Number == 31 && remote.ReadyNumber == 31 && remote.Data!.Number == 7, "Native ENet replicates independent file-backed scene state before Ready."); serverSpawner.ClearSpawnableScenes(); clientSpawner.ClearSpawnableScenes(); Check(local.Data!.Number == 7 && remote.Data!.Number == 7, "Cleared file registrations preserve live instance graphs."); local.Dispose(); Wait(() => clientRoot.GetNodeOrNull("Actors/Spawned") is null);
        serverTree.SetMultiplayer(null); clientTree.SetMultiplayer(null);
    }
    private static void CopyOwnership(string folder)
    {
        var path = System.IO.Path.Combine(folder, "level.e2dscene"); var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var copy = (PackedScene)loaded.Duplicate(); loaded.Dispose(); using var node = (Actor)copy.Instantiate(); Check(node.Number == 31 && node.Data!.Number == 7, "Copied file scene retains its borrowed file graph after source disposal.");
        var firstPath = System.IO.Path.Combine(folder, "copy-first.e2dres"); var secondPath = System.IO.Path.Combine(folder, "copy-second.e2dres"); using var a = new Data { Next = new Data { Number = 1 } }; using var b = new Data { Next = new Data { Number = 2 } }; ResourceSaver.Save(a, firstPath); ResourceSaver.Save(b, secondPath); using var target = ResourceLoader.Load<Data>(firstPath, ResourceLoader.CacheMode.Ignore); var source = ResourceLoader.Load<Data>(secondPath, ResourceLoader.CacheMode.Ignore); var oldChild = target.Next!; var newChild = source.Next!; source.RejectCopy = true; Reject<InvalidOperationException>(() => target.CopyFromResource(source)); source.Dispose(); Check(!oldChild.IsDisposed && !newChild.IsDisposed && target.Next!.Number == 2, "Failed custom copy retains both graphs for partial state."); target.Dispose(); Check(oldChild.IsDisposed && newChild.IsDisposed, "Failed-copy graph retention releases both graphs deterministically."); a.Next!.Dispose(); b.Next!.Dispose();
    }
    private static void StateOwnership(string folder)
    {
        var path = System.IO.Path.Combine(folder, "ui.e2dscene"); var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var state = loaded.GetState(); var index = Enumerable.Range(0, state.GetNodePropertyCount(0)).Single(i => state.GetNodePropertyName(0, i) == "ThemeStyleBoxOverride/normal"); var style = state.GetNodePropertyValue<StyleBox>(0, index)!; loaded.Dispose(); Check(!style.IsDisposed && ((StyleBoxFlat)style).BGColor.R == .3f && state.GetNodeCount() == 1, "Exported scene state retains its final file-backed resource snapshot."); state.Dispose(); Check(style.IsDisposed, "Final metadata state disposal releases its file graph.");
    }
    private static void EmptyAndEditor(string folder)
    {
        using var data = new Data { Number = 4, EditorTag = 42 }; var path = System.IO.Path.Combine(folder, "editor.e2dres"); ResourceSaver.Save(data, path, SaverFlags.OmitEditorProperties); using var omitted = ResourceLoader.Load<Data>(path, ResourceLoader.CacheMode.Ignore); Check(omitted.Number == 4 && omitted.EditorTag == 0, "Reserved editor storage omission preserves ordinary state."); ResourceSaver.Save(data, path); using var included = ResourceLoader.Load<Data>(path, ResourceLoader.CacheMode.Ignore); Check(included.EditorTag == 42, "Ordinary saving retains reserved metadata.");
        using var empty = new Image(); var emptyPath = System.IO.Path.Combine(folder, "empty.e2dres"); ResourceSaver.Save(empty, emptyPath); using var restored = ResourceLoader.Load<Image>(emptyPath, ResourceLoader.CacheMode.Ignore); Check(restored.IsEmpty, "Empty image storage.");
        using var texture = new ImageTexture(); texture.SetSizeOverride(new Vector2i(8, 7)); var texturePath = System.IO.Path.Combine(folder, "uninitialized.e2dres"); ResourceSaver.Save(texture, texturePath); using var decoded = ResourceLoader.Load<ImageTexture>(texturePath, ResourceLoader.CacheMode.Ignore); Check(decoded.GetImage() is null && decoded.GetSize() == new Vector2i(8, 7), "Uninitialized texture retains logical size without inventing pixels.");
        using var packed = new PackedScene(); var scenePath = System.IO.Path.Combine(folder, "empty.e2dscene"); ResourceSaver.Save(packed, scenePath); using var loaded = ResourceLoader.Load<PackedScene>(scenePath, ResourceLoader.CacheMode.Ignore); Check(!loaded.CanInstantiate(), "Empty packed file remains an empty template."); Reject<InvalidOperationException>(() => loaded.Instantiate());
    }
    private static void SceneFiles(string folder)
    {
        var path = System.IO.Path.Combine(folder, "level.e2dscene");
        using var spawner = new MultiplayerSpawner();
        spawner.AddSpawnableScene(path);
        Check(spawner.GetSpawnableScenePath(0) == path, "File-backed spawn scene path.");
        spawner.ClearSpawnableScenes();
        using var tree = new SceneTree(new Node
        {
            Name = "Host"
        }
);
        tree.ChangeSceneToFile(path);
        tree.ProcessFrame(0);
        Check(tree.CurrentScene is Actor
        {
            Number: 31
        }
        , "Deferred file scene replacement.");
        var old = tree.CurrentScene;
        tree.ReloadCurrentScene();
        tree.ProcessFrame(0);
        Check(old!.IsDisposed && tree.CurrentScene is Actor
        {
            Number: 31
        }
        , "Current scene reload instantiates fresh state.");
    }
    private static void FreshProcess(string folder)
    {
        var path = System.IO.Path.Combine(folder, "level.e2dscene");
        var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(ResourceArchiveTests).Assembly.Location);
        start.Environment["ELECTRON2D_TEST_RESOURCE_ARCHIVE"] = "0";
        start.Environment["ELECTRON2D_TEST_RESOURCE_ARCHIVE_CHILD"] = path;
        using var process = System.Diagnostics.Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(15000))
        {
            process.Kill(true);
            throw new TimeoutException("Fresh archive process did not terminate.");
        }
        Check(process.ExitCode == 0, "Fresh process archive load/run: " + error.GetAwaiter().GetResult());
        Check(output.GetAwaiter().GetResult().Contains("Fresh scene archive process passed"), "Fresh process marker.");
    }
    internal static void RunChild(string path)
    {
        ResourceFileTypes.RegisterResource("tests.Data", CreateData);
        ResourceFileTypes.RegisterNode("tests.Actor", CreateActor);
        using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore);
        using var root = (Actor)loaded.Instantiate();
        Check(root.Number == 31 && root.GetNode<Actor>("Child").Number == 42 && root.Data!.Number == 7, "Fresh process has decoded file state.");
        using var tree = new SceneTree(root);
        tree.ProcessFrame(0);
        Check(root.ReadyNumber == 31, "Fresh process Ready state.");
        Console.WriteLine("Fresh scene archive process passed");
    }
    private sealed class Saver(List<string> calls, string name, bool fail) : ResourceFormatSaver
    {
        public override bool Equals(object? other) => other is Saver;
        public override int GetHashCode() => 1;
        public override bool Recognize(Resource resource) => resource is Data;
        public override string[] GetRecognizedExtensions(Resource resource) => ["probe"];
        public override void Save(Resource resource, string path, SaverFlags flags)
        {
            calls.Add(name);
            Check(resource.ResourcePath == ProjectSettings.LocalizePath(path), "Saver sees temporary destination.");
            if (fail) throw new IOException("fixture fallback");
            System.IO.File.WriteAllText(path, ((Data)resource).Number.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }
    private sealed class Loader : ResourceFormatLoader
    {
        internal bool Reenter;
        public override string[] GetRecognizedExtensions() => ["probe"];
        public override bool HandlesType(Type type) => type.IsAssignableFrom(typeof(Data));
        public override Type? GetResourceType(string path) => typeof(Data);
        public override Resource Load(string path, string originalPath, bool useSubThreads, ResourceLoader.CacheMode cacheMode) => Reenter ? ResourceLoader.Load<Data>(path, cacheMode) : new Data
        {
            Number = int.Parse(System.IO.File.ReadAllText(path), System.Globalization.CultureInfo.InvariantCulture)
        };
    }
    private static void Formats(string folder)
    {
        var calls = new List<string>();
        using var fallback = new Saver(calls, "fallback", false);
        using var first = new Saver(calls, "first", true);
        using var loader = new Loader();
        ResourceSaver.AddResourceFormatSaver(fallback);
        ResourceSaver.AddResourceFormatSaver(first, true);
        ResourceSaver.AddResourceFormatSaver(first, true);
        ResourceLoader.AddResourceFormatLoader(loader, true);
        var path = System.IO.Path.Combine(folder, "format.probe");
        using var data = new Data
        {
            Number = 57
        };
        data.ResourcePath = "res://fixture-owned";
        try
        {
            ResourceSaver.Save(data, path, SaverFlags.ChangePath);
            Check(calls.SequenceEqual(["first", "fallback"]) && data.ResourcePath == "res://fixture-owned" && ReferenceEquals(ResourceLoader.GetCachedRef<Data>(data.ResourcePath), data), "Saver priority/dedup/fallback and cache rollback.");
            using var customCached = ResourceLoader.Load<Data>(path);
            System.IO.File.WriteAllText(path, "63");
            Check(ReferenceEquals(ResourceLoader.Load<Data>(path, ResourceLoader.CacheMode.Replace), customCached) && customCached.Number == 63, "Custom format Replace keeps exact identity.");
            System.IO.File.WriteAllText(path, "57");
            using var loaded = ResourceLoader.Load<Data>(path, ResourceLoader.CacheMode.Ignore);
            Check(loaded.Number == 57 && ResourceLoader.Exists<Data>(path) && ResourceLoader.GetRecognizedExtensionsForType<Data>().Contains("probe"), "Typed custom format loading/discovery.");
            loader.Reenter = true; Reject<InvalidDataException>(() => ResourceLoader.Load<Data>(path, ResourceLoader.CacheMode.Ignore)); loader.Reenter = false;
            Reject<ArgumentOutOfRangeException>(() => ResourceSaver.Save(data, path, (SaverFlags)128));
            Reject<InvalidOperationException>(() => Engine.UnregisterSingleton(nameof(ResourceSaver)));
            Reject<InvalidOperationException>(() => Engine.GetSingleton<ResourceUID>(nameof(ResourceUID)).Dispose());
            Check(ResourceUID.IDToText(long.MaxValue) == "uid://d4n4ub6itg400", "Pinned UID numeric/text boundary.");
        }
        finally
        {
            ResourceSaver.RemoveResourceFormatSaver(first);
            ResourceSaver.RemoveResourceFormatSaver(fallback);
            ResourceLoader.RemoveResourceFormatLoader(loader);
        }
    }
    private static void Scene(string folder)
    {
        var path = System.IO.Path.Combine(folder, "level.e2dscene");
        using var data = new Data
        {
            Number = 7,
            ResourceLocalToScene = true
        };
        var root = new Actor
        {
            Name = "Root",
            Number = 31,
            Data = data,
            Position = new(4, 8)
        };
        var child = new Actor
        {
            Name = "Child",
            Number = 42,
            Data = data
        };
        root.AddChild(child);
        child.Owner = root;
        root.Partner = child;
        child.Partner = root;
        child.AddToGroup("kept", true);
        using var packed = new PackedScene();
        packed.Pack(root);
        root.Dispose();
        ResourceSaver.Save(packed, path, SaverFlags.Compress | SaverFlags.SaveBigEndian);
        using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore);
        using var a = (Actor)loaded.Instantiate();
        var b = a.GetNode<Actor>("Child");
        Check(a.Number == 31 && b.Number == 42 && ReferenceEquals(a.Partner, b) && ReferenceEquals(b.Partner, a), "Typed node references and stored state.");
        Check(ReferenceEquals(a.Data, b.Data) && !ReferenceEquals(a.Data, data) && a.Position == new Vector2(4, 8) && b.Owner == a, "Local resource alias/copy/geometry/owner.");
        using var tree = new SceneTree(a);
        tree.ProcessFrame(0);
        Check(a.ReadyNumber == 31 && b.ReadyNumber == 42, "Fresh loaded scene enters normal lifecycle.");
    }
}
