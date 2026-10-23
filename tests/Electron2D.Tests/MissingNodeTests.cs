using Electron2D;
using System.Diagnostics;
using static TextureArrayTests;
using IOPath = System.IO.Path;

internal static class MissingNodeTests
{
    private const string ID = "tests.recovered-actor", ConnectionID = "tests.recovered-rename";
    private static bool Expanded;
    private sealed class Actor : Node
    {
        internal int Value;
        internal string Added = "";
        internal int[] Numbers = [];
        internal Resource? Shared;
        internal Actor? Peer;
        protected override Func<Node> CreateSceneInstanceFactory() => CreateActor;
        protected override void OnProcess(double delta) => Value++;
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<Actor, int>("Value", n => n.Value, (n, v) => n.Value = v, _ => 0, stored: true),
         new PropertyDescriptor<Actor, int[]>("Numbers", n => n.Numbers, (n, v) => n.Numbers = v, _ => [], stored: true),
         new PropertyDescriptor<Actor, Resource?>("Shared", n => n.Shared, (n, v) => n.Shared = v, _ => null, stored: true),
         new PropertyDescriptor<Actor, Actor?>("Peer", n => n.Peer, (n, v) => n.Peer = v, _ => null, stored: true)]).Concat(Expanded ? new PropertyDescriptor[] { new PropertyDescriptor<Actor, string>("Added", n => n.Added, (n, v) => n.Added = v, _ => "", stored: true) } : []);
    }
    private static Actor CreateActor() => new();
    private static EventConnection Rename(Actor publisher, Actor subscriber, int amount) => EventConnection.Subscribe<Node>(
        handler => publisher.Renamed += handler, handler => publisher.Renamed -= handler, _ => subscriber.Value += amount);
    private static EventConnection RenameResource(Actor publisher, Actor subscriber, Resource payload) => EventConnection.Subscribe<Node>(
        handler => publisher.Renamed += handler, handler => publisher.Renamed -= handler, _ => subscriber.Numbers = [payload.ResourceName.Length]);
    private static SceneConnectionBinding<Actor, Actor, Resource> ResourceBinding() => ResourceFileTypes.RegisterSceneConnection<Actor, Actor, Resource>("tests.recovered-resource", "ResourceEvent", "ReceiveResource", RenameResource);
    private static SceneConnectionBinding<Actor, Actor, int> Register()
    {
        ResourceFileTypes.RegisterNode(ID, CreateActor);
        return ResourceFileTypes.RegisterSceneConnection<Actor, Actor, int>(ConnectionID, "Renamed", "Increment", Rename);
    }
    internal static bool Child()
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_MISSING_NODE_PHASE") is not { } phase) return false;
        var folder = Environment.GetEnvironmentVariable("ELECTRON2D_MISSING_NODE_FOLDER")!;
        if (phase == "write") Write(folder); else if (phase == "recover") Recover(folder); else if (phase == "restore") Restore(folder); else if (phase == "missing-scene") MissingScene(folder); else if (phase == "archive-boundaries") ArchiveBoundaries(folder); else if (phase == "native") Native(folder); else throw new ArgumentException("Unknown recovery fixture phase.");
        Console.WriteLine("Missing-node phase passed: " + phase); return true;
    }
    internal static void Run()
    {
        var output = Environment.GetEnvironmentVariable("ELECTRON2D_MISSING_NODE_OUTPUT");
        var folder = output ?? IOPath.Combine(IOPath.GetTempPath(), "electron2d-missing-node-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            foreach (var phase in new[] { "write", "recover", "restore", "missing-scene", "archive-boundaries" }.Concat(Environment.GetEnvironmentVariable("ELECTRON2D_MISSING_NODE_NATIVE") == "1" ? ["native"] : Array.Empty<string>()))
            {
                var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(MissingNodeTests).Assembly.Location);
                start.Environment["ELECTRON2D_TEST_SCENE_CONNECTIONS"] = "1";
                start.Environment["ELECTRON2D_MISSING_NODE_PHASE"] = phase; start.Environment["ELECTRON2D_MISSING_NODE_FOLDER"] = folder;
                using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
                if (!child.WaitForExit(60000)) { child.Kill(entireProcessTree: true); throw new TimeoutException("Missing-node fixture did not finish: " + phase); }
                Task.WaitAll(stdout, stderr); File.WriteAllText(IOPath.Combine(folder, phase + ".log"), stdout.Result + stderr.Result);
                Check(child.ExitCode == 0, "Fresh recovery process failed: " + phase + "\n" + stdout.Result + stderr.Result);
            }
            Console.WriteLine("Missing nodes: fresh write/recover/resave/restored compiled behavior and typed connections passed.");
        }
        finally { if (output is null) Directory.Delete(folder, recursive: true); }
    }
    private static void Write(string folder)
    {
        var binding = Register(); using var root = new Node { Name = "Root" }; using var shared = new Resource { ResourceName = "Shared", ResourceLocalToScene = true };
        var first = new Actor { Name = "First", Value = 17, Numbers = [1, 2], Shared = shared }; var second = new Actor { Name = "Second", Value = 31, Shared = shared };
        root.AddChild(first); root.AddChild(second); first.Owner = second.Owner = root; first.Peer = second; second.Peer = first;
        first.AddToGroup("recovered", persistent: true); using var token = binding.Subscribe(first, second, 3);
        ResourceSaver.SaveScene(root, IOPath.Combine(folder, "source.e2dscene"), SaverFlags.Compress | SaverFlags.SaveBigEndian);
    }
    private static void Recover(string folder)
    {
        var source = IOPath.Combine(folder, "source.e2dscene"); ResourceLoader.SetAbortOnMissingResources(true);
        Reject<InvalidDataException>(() => ResourceLoader.Load<PackedScene>(source, ResourceLoader.CacheMode.Ignore));
        ResourceLoader.SetAbortOnMissingResources(false); using var packed = ResourceLoader.Load<PackedScene>(source, ResourceLoader.CacheMode.Ignore);
        using var state = packed.GetState(); Check(state.GetConnectionCount() == 1 && state.GetConnectionBinds<int>(0) == 3 && state.GetConnectionSignal(0) == "Renamed", "Unknown connection metadata remains inspectable.");
        using var root = packed.Instantiate(); var first = root.GetNode<MissingNode>("First"); var second = root.GetNode<MissingNode>("Second");
        Check(first.OriginalClass == ID && first.OriginalScene == "" && !first.RecordingProperties && !first.RecordingSignals && first.GetConfigurationWarnings().Length == 2, "Loader-created recovery identity/defaults and diagnostics.");
        Check(first.GetStoredValue<int>("Value") == 17 && first.IsInGroup("recovered") && first.Owner == root, "Fields, ownership and persistent groups survive unavailable compiled factories.");
        Check(ReferenceEquals(first.GetStoredValue<Resource>("Shared"), second.GetStoredValue<Resource>("Shared")) && first.GetStoredValue<Resource>("Shared").GetLocalScene() == root, "Scene-local resources retain aliases and local-scene ownership.");
        var numbers = first.GetStoredValue<int[]>("Numbers"); numbers[0] = 99; Check(first.GetStoredValue<int[]>("Numbers")[0] == 1, "Observed arrays are independent snapshots.");
        Reject<InvalidOperationException>(() => first.SetStoredValue("Added", 1)); first.SetStoredValue("Value", 23);
        first.RecordingProperties = true; first.SetStoredValue("Added", new int[] { 5, 6 }); first.RecordingProperties = false;
        first.SetStoredValue("Added", "changed schema"); Check(first.GetStoredValue<string>("Added") == "changed schema", "Existing retained names may change schema without recording.");
        using (var tree = new SceneTree(root))
        {
            Engine.Start(tree);
            try
            {
                Engine.AdvanceFrame(1d / 60);
                Check(first.GetStoredValue<int>("Value") == 23, "Unavailable compiled processing is never synthesized.");
                first.Name = "Renamed"; Check(second.GetStoredValue<int>("Value") == 31, "Retained connection does not impersonate an executing subscription.");
                Reject<InvalidOperationException>(() => Task.Run(() => first.GetStoredValue<int>("Value")).GetAwaiter().GetResult());
                Reject<InvalidOperationException>(() => Task.Run(() => first.SetStoredValue("Value", 99)).GetAwaiter().GetResult());
            }
            finally { Engine.Stop(); }
        }
        Reject<ObjectDisposedException>(() => first.GetStoredPropertyNames());
        Reject<ObjectDisposedException>(() => first.SetStoredValue("Value", 99));
        // The scene-tree owner disposed its root; use another detached instance for resaving.
        using var savedRoot = packed.Instantiate(); var edited = savedRoot.GetNode<MissingNode>("First"); edited.SetStoredValue("Value", 23); edited.Name = "Renamed";
        Check(savedRoot.GetNode<MissingNode>("Second").GetStoredValue<string>("Peer").Contains("Renamed", StringComparison.Ordinal), "Retained node paths follow actual endpoint renaming.");
        using (var duplicate = savedRoot.Duplicate())
        {
            var copied = duplicate.GetNode<MissingNode>("Renamed");
            Check(copied.OriginalClass == ID && copied.GetStoredValue<int>("Value") == 23 && ReferenceEquals(copied.GetStoredValue<Node>("Peer"), duplicate.GetNode("Second")), "Duplicate preserves recovery identity, fields and copied node endpoints.");
            Check(ReferenceEquals(copied.GetStoredValue<Resource>("Shared"), edited.GetStoredValue<Resource>("Shared")), "Ordinary duplicate shares retained resource edges.");
            using var captured = new PackedScene(); captured.Pack(duplicate); Check(captured.GetState().GetConnectionCount() == 1, "Duplicate preserves retained connection records exactly once.");
        }
        using (var duplicate = savedRoot.Duplicate(Node.DuplicateFlags.Default & ~(Node.DuplicateFlags.Signals | Node.DuplicateFlags.UseInstantiation)))
        { using var captured = new PackedScene(); captured.Pack(duplicate); Check(captured.GetState().GetConnectionCount() == 0, "A raw duplicate with disabled Signals omits retained authored records."); }
        edited.RecordingProperties = true; edited.SetStoredValue("Added", new int[] { 5, 6 }); edited.RecordingProperties = false; edited.SetStoredValue("Added", "restored field");
        var extra = ResourceFileTypes.RegisterSceneConnection<Actor, Actor, int>("tests.recovered-extra", "AddedEvent", "IncrementExtra", Rename);
        var failing = ResourceFileTypes.RegisterSceneConnection<Actor, Actor, int>("tests.recovered-failure", "FailureEvent", "Failure", Rename);
        var receiver = savedRoot.GetNode<MissingNode>("Second");
        Reject<InvalidOperationException>(() => edited.RecordSceneConnection(extra, receiver, 4));
        using var authoringTree = new SceneTree(savedRoot); authoringTree.EditedSceneRoot = savedRoot;
        void Fail(Node _) => throw new ArithmeticException("record authoring failure");
        edited.EditorStateChanged += Fail; edited.RecordingSignals = true;
        Reject<ArithmeticException>(() => edited.RecordSceneConnection(failing, receiver, 9)); edited.EditorStateChanged -= Fail;
        edited.RecordingSignals = false; Reject<InvalidOperationException>(() => edited.RecordSceneConnection(failing, receiver, 9));
        edited.RecordingSignals = true; using var extraLease = edited.RecordSceneConnection(extra, receiver, 4); edited.RecordingSignals = false;
        var removed = edited.RecordSceneConnection(extra, receiver, 8); removed.Dispose(); removed.Dispose();
        using (var capture = new PackedScene()) { capture.Pack(savedRoot); Check(capture.GetState().GetConnectionCount() == 2, "Recording, existing event identity, explicit removal and callback rollback retain exactly the two intended records."); }
        edited.RecordingSignals = true; using var resourceLease = edited.RecordSceneConnection(ResourceBinding(), receiver, edited.GetStoredValue<Resource>("Shared")); edited.RecordingSignals = false;
        using (var capture = new PackedScene()) { capture.Pack(savedRoot); Check(ReferenceEquals(capture.GetState().GetConnectionBinds<Resource>(2), edited.GetStoredValue<Resource>("Shared")), "Retained resource payloads borrow the same field graph identity."); }
        var recoveredPath = IOPath.Combine(folder, "recovered.e2dscene"); ResourceSaver.SaveScene(savedRoot, recoveredPath, SaverFlags.None);
        var committed = File.ReadAllBytes(recoveredPath); edited.OriginalClass = "";
        try { ResourceSaver.SaveScene(savedRoot, recoveredPath); throw new InvalidOperationException("Invalid recovery save succeeded."); }
        catch (AggregateException error) { Check(error.InnerExceptions.Count == 1 && error.InnerException is InvalidDataException, "Archive identity validation is reported through the saver failure contract."); }
        Check(File.ReadAllBytes(recoveredPath).SequenceEqual(committed), "An invalid recovery identity cannot replace the last valid archive."); edited.OriginalClass = ID;
    }
    private static void ArchiveBoundaries(string folder)
    {
        _ = Register();
        for (var version = 3; version <= 7; version++)
        {
            var path = Fixture(version, "valid"); using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore);
            using var root = packed.Instantiate(); using var tree = new SceneTree(root); root.Name = "Loaded";
            Check(((Actor)root).Value == 9, "Legacy/current archives restore real registered connection behavior: " + version);
        }
        ResourceLoader.SetAbortOnMissingResources(false);
        foreach (var error in new[] { "label", "flags", "unbinds", "endpoint", "payload", "role", "unknown-binding", "wrong-node-role" })
            Reject<InvalidDataException>(() => ResourceLoader.Load<PackedScene>(Fixture(7, error), ResourceLoader.CacheMode.Ignore));
        Reject<InvalidDataException>(() => ResourceLoader.Load<PackedScene>(Fixture(6, "missing"), ResourceLoader.CacheMode.Ignore));
        using var recovered = ResourceLoader.Load<PackedScene>(Fixture(7, "missing"), ResourceLoader.CacheMode.Ignore);
        using var missing = recovered.Instantiate(); Check(missing is MissingNode && recovered.GetState().GetConnectionCount() == 1, "Only a self-describing connection with an unavailable endpoint permits tolerant retention.");

        string Fixture(int version, string mode)
        {
            // Independent minimal format fixtures keep the previous reader contracts executable.
            using var scene = new StreamPeerBuffer(); scene.Put32(1);
            Text(scene, mode == "missing" ? "tests.unavailable" : mode == "wrong-node-role" ? "Resource" : ID); Text(scene, "Root");
            scene.Put32(-1); scene.Put32(-1); scene.Put32(-1);
            if (version >= 5) { scene.Put32(-1); scene.Put32(-1); Text(scene, ""); }
            if (version >= 6) Text(scene, "");
            scene.Put32(0); scene.Put32(0); if (version >= 6) scene.Put32(0);
            scene.Put32(1); Text(scene, mode is "missing" or "unknown-binding" ? "tests.unavailable-binding" : ConnectionID);
            if (version >= 7) { Text(scene, mode == "label" ? "Changed" : "Renamed"); Text(scene, "Increment"); scene.Put32(mode == "flags" ? 0 : 2); scene.Put32(mode == "unbinds" ? -1 : 0); }
            scene.Put32(mode == "endpoint" ? 1 : 0); scene.Put32(0);
            if (version >= 7) { scene.Put32(1); Text(scene, "Bindings"); Text(scene, mode == "payload" ? "System.Single" : "System.Int32"); scene.PutU8(mode == "role" ? (byte)1 : (byte)0); scene.PutU8(0); }
            scene.Put32(4); scene.Put32(9);
            using var archive = new StreamPeerBuffer(); archive.Put32(1); Text(archive, "PackedScene"); archive.PutU8(0); if (version >= 4) archive.PutU8(0);
            Text(archive, ""); archive.PutU8(0); Text(archive, ""); Text(archive, ""); archive.Put64(-1); archive.Put32(scene.GetSize()); archive.PutData(scene.DataArray);
            var payload = archive.DataArray; var bytes = new byte[64 + payload.Length]; "E2DASSET"u8.CopyTo(bytes);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), version);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(24), payload.Length);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(28), payload.Length);
            System.Security.Cryptography.SHA256.HashData(payload).CopyTo(bytes.AsSpan(32)); payload.CopyTo(bytes, 64);
            var path = IOPath.Combine(folder, $"fixture-{version}-{mode}.e2dscene"); File.WriteAllBytes(path, bytes); return path;
        }
        static void Text(StreamPeerBuffer stream, string text) { stream.Put32(text.Length); foreach (var c in text) stream.PutU16(c); }
    }
    private static void MissingScene(string folder)
    {
        var templatePath = IOPath.Combine(folder, "gone-template.e2dscene"); var scenePath = IOPath.Combine(folder, "with-instance.e2dscene");
        using var templateRoot = new Node { Name = "Template" }; var inherited = new Entity { Name = "Inherited", Position = new(7, 9) };
        templateRoot.AddChild(inherited); inherited.Owner = templateRoot;
        using var template = new PackedScene(); template.Pack(templateRoot); ResourceSaver.Save(template, templatePath); template.TakeOverPath(templatePath);
        using var outer = new Node { Name = "Outer" }; var instance = template.Instantiate(); instance.Name = "Gone"; outer.AddChild(instance); instance.Owner = outer;
        var added = new Node { Name = "Added" }; instance.AddChild(added); added.Owner = outer; outer.SetEditableInstance(instance, true);
        ResourceSaver.SaveScene(outer, scenePath); File.Delete(templatePath); ResourceLoader.SetAbortOnMissingResources(false);
        using var loaded = ResourceLoader.Load<PackedScene>(scenePath, ResourceLoader.CacheMode.IgnoreDeep);
        using var recovered = loaded.Instantiate(); var missing = recovered.GetNode<MissingNode>("Gone");
        Check(missing.OriginalClass == "" && missing.OriginalScene.EndsWith("gone-template.e2dscene", StringComparison.Ordinal) && missing.GetConfigurationWarnings().Length == 2, "A missing linked template has an actual original-scene producer and save warning.");
        Check(!missing.HasNode("Inherited") && missing.HasNode("Added"), "Unavailable inherited content is absent while ordinary outer-owned additions remain.");
        var flattenedPath = IOPath.Combine(folder, "flattened-missing-scene.e2dscene"); ResourceSaver.Save(loaded, flattenedPath);
        ResourceLoader.SetAbortOnMissingResources(true); using var flattened = ResourceLoader.Load<PackedScene>(flattenedPath, ResourceLoader.CacheMode.IgnoreDeep);
        using var flatRoot = flattened.Instantiate(); Check(flatRoot.GetNode("Gone").GetType() == typeof(Node) && flatRoot.HasNode("Gone/Added") && !flatRoot.HasNode("Gone/Inherited"), "Resaving the missing instance discards its unavailable template role and keeps a valid ordinary hierarchy.");
    }
    private sealed class Capture(MissingNode first, MissingNode second) : MovieWriter
    {
        internal int Frames;
        protected override string[] OnGetSupportedExtensions() => ["missingnode"];
        protected override bool OnHandlesFile(string path) => IOPath.GetExtension(path) == ".missingnode";
        protected override void OnWriteBegin(Vector2i size, int fps, string path) => Check(size == new Vector2i(64, 48), "Actual native frame size.");
        protected override void OnWriteFrame(Image image, ReadOnlySpan<int> audio)
        {
            var color = image.GetPixel(4, 4); Check(color.R > .9f && color.G < .05f, "Healthy sibling pixels remain visible beside recovered nodes.");
            Check(first.GetStoredValue<int>("Value") == 23 && second.GetStoredValue<int>("Value") == 31, "Unavailable callbacks stay inactive during real rendered frames.");
            Frames++;
        }
        protected override void OnWriteEnd() { }
    }
    private static void Native(string folder)
    {
        ResourceLoader.SetAbortOnMissingResources(false);
        foreach (var backend in new[] { "gpu", "compatibility" })
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            using var packed = ResourceLoader.Load<PackedScene>(IOPath.Combine(folder, "recovered.e2dscene"), ResourceLoader.CacheMode.IgnoreDeep);
            var root = packed.Instantiate(); var first = root.GetNode<MissingNode>("Renamed"); var second = root.GetNode<MissingNode>("Second");
            var window = new Window { Size = new(64, 48) }; window.AddChild(root); var healthy = new Entity(); healthy.Draw += canvas => canvas.DrawRect(new(2, 2, 8, 8), Colors.Red); window.AddChild(healthy);
            using var capture = new Capture(first, second); MovieWriter.AddWriter(capture);
            Check(Engine.Run((host, next) => { if (next == 1) first.Name = "Native"; if (next == 4) host.Tree!.Quit(); }, window, "capture.missingnode", 60) == 0 && capture.Frames == 3 && window.IsDisposed, "Recovered scene has three actual native frames and normal owned cleanup.");
            Console.WriteLine("Missing-node native " + backend + ": completed RGB frames, inactive missing callbacks, healthy sibling and cleanup passed.");
        }
    }
    private static void Restore(string folder)
    {
        Expanded = true; _ = Register(); _ = ResourceFileTypes.RegisterSceneConnection<Actor, Actor, int>("tests.recovered-extra", "AddedEvent", "IncrementExtra", Rename); _ = ResourceBinding();
        using var packed = ResourceLoader.Load<PackedScene>(IOPath.Combine(folder, "recovered.e2dscene"), ResourceLoader.CacheMode.Ignore);
        using var root = packed.Instantiate(); var first = root.GetNode<Actor>("Renamed"); var second = root.GetNode<Actor>("Second");
        Check(first.Value == 23 && first.Added == "restored field" && second.Value == 31 && first.Peer == second && second.Peer == first, "Available factories recover modified fields and node references.");
        Check(ReferenceEquals(first.Shared, second.Shared) && first.Shared!.GetLocalScene() == root, "Recovered resource aliases retain ordinary local ownership.");
        Check(ReferenceEquals(packed.GetState().GetConnectionBinds<Resource>(2), packed.GetState().GetNodePropertyValue<Resource>(1, Enumerable.Range(0, packed.GetState().GetNodePropertyCount(1)).Single(i => packed.GetState().GetNodePropertyName(1, i) == "Shared"))), "File connection payload and stored field share one archived resource identity.");
        using var tree = new SceneTree(root);
        first.Name = "Restored"; Check(second.Value == 38, "Both original and newly recorded compiled adapters restore real executing subscriptions.");
        Check(second.Numbers.SequenceEqual(new[] { 6 }), "The restored resource-bound adapter executes using the actual local resource.");
        Engine.Start(tree);
        try { Engine.AdvanceFrame(1d / 60); Check(first.Value == 24 && second.Value == 39, "The original compiled process behavior executes after restoration."); }
        finally { Engine.Stop(); }
    }
}
