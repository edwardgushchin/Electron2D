using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;

internal static partial class DeferredSceneTests
{
    private static string At(string folder, string name) => System.IO.Path.Combine(folder, name);
    private static void Register() => ResourceFileTypes.RegisterNode("tests.DeferredActor", Actor.Create);
    private static void RunFiles()
    {
        var folder = At(System.IO.Path.GetTempPath(), "electron2d-deferred-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            Child("produce", folder); Child("unregistered", folder); CorruptFields(folder); Child("read", folder);
            File.Delete(At(folder, "actor.e2dscene")); Child("missing", folder);
        }
        finally { Directory.Delete(folder, true); }
    }
    private static void Child(string mode, string folder)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(DeferredSceneTests).Assembly.Location);
        start.ArgumentList.Add("--deferred-scene"); start.ArgumentList.Add(mode); start.ArgumentList.Add(folder);
        start.Environment.Remove("ELECTRON2D_TEST_DEFERRED_SCENES");
        using var child = Process.Start(start)!;
        var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(60000)) { child.Kill(true); throw new TimeoutException("Deferred scene child deadline."); }
        Check(child.ExitCode == 0, "Deferred child " + mode + ": " + output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    }
    internal static void RunChild(string mode, string folder)
    {
        if (mode == "unregistered")
        {
            var path = At(folder, "bundled.e2dscene");
            Check(!ResourceLoader.GetClassesUsed(path).Contains(typeof(Actor)), "Skipped inherited records do not require deferred custom class metadata.");
            using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep);
            using var root = scene.Instantiate();
            Check(root.GetNode<InstancePlaceholder>("Pending0").GetStoredValue<Vector2>(nameof(Entity.Position)) == new Vector2(42, 53) &&
                !ResourceLoader.HasCached(At(folder, "actor.e2dscene")), "Deferred loading succeeds before its application factories are registered.");
            return;
        }
        Register();
        if (mode == "produce") Produce(folder);
        else if (mode == "read") ReadFiles(folder);
        else if (mode == "missing") MissingFile(folder);
        else throw new ArgumentException("Unknown deferred child profile.");
    }
    private static void Produce(string folder)
    {
        using var data = new MissingResource { RecordingProperties = true, ResourceLocalToScene = true };
        data.SetRecordedProperty("Value", 73); data.SetRecordedProperty<Resource?>("Self", data);
        using var shared = new MissingResource { RecordingProperties = true }; shared.SetRecordedProperty("Value", 91);
        using var source = new Actor { Name = "Actor", Position = new(7, 9), Data = data, Shared = shared, Items = [data, null, data] };
        var inherited = new Node { Name = "Inherited" }; source.AddChild(inherited); inherited.Owner = source;
        var typedInherited = new Actor { Name = "TypedInherited" }; source.AddChild(typedInherited); typedInherited.Owner = source;
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.Red);
        using var texture = ImageTexture.CreateFromImage(image); texture.SetSizeOverride(new(12, 12)); texture.ResourceLocalToScene = true;
        var visual = new Sprite { Name = "Visual", Texture = texture, Centered = false, TextureFilter = TextureFilter.Nearest };
        source.AddChild(visual); visual.Owner = source;
        using var shape = new CircleShape { Radius = 2 };
        var body = new RigidBody { Name = "Body", GravityScale = 0, LinearVelocity = new(12, 0) };
        var collision = new CollisionShape { Name = "Collision", Shape = shape }; body.AddChild(collision); source.AddChild(body);
        body.Owner = source; collision.Owner = source;
        using var actor = new PackedScene(); actor.Pack(source);
        ResourceSaver.Save(actor, At(folder, "actor.e2dscene"), SaverFlags.BundleResources | SaverFlags.SaveBigEndian | SaverFlags.Compress);
        actor.ResourcePath = At(folder, "actor.e2dscene");
        using var root = new Node { Name = "Level" };
        for (var i = 0; i < 2; i++)
        {
            var pending = (Actor)actor.Instantiate(); pending.Name = "Pending" + i; root.AddChild(pending); pending.Owner = root;
            pending.Position = new(42 + i, 53); pending.Label = "deferred"; pending.Values = [2, 5, 8]; pending.Target = pending.GetNode("Inherited");
            pending.SetSceneInstanceLoadPlaceholder(true); root.SetEditableInstance(pending, true);
        }
        using var parent = new PackedScene(); parent.Pack(root);
        foreach (var (name, flags) in new[] { ("plain", (SaverFlags)0), ("bundled", SaverFlags.BundleResources | SaverFlags.RelativePaths | SaverFlags.SaveBigEndian | SaverFlags.Compress) })
            ResourceSaver.Save(parent, At(folder, name + ".e2dscene"), flags);
    }
    private static void ReadFiles(string folder)
    {
        foreach (var name in new[] { "plain", "bundled" })
        {
            var path = At(folder, name + ".e2dscene"); _factories = 0;
            Check(ResourceLoader.GetClassesUsed(path).Contains(typeof(InstancePlaceholder)), "Deferred class discovery consumes both field tables.");
            Check(ResourceLoader.GetDependencies(path).Length == 0, "Deferred paths are late lookup values rather than eager resource edges.");
            using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep);
            var root = scene.Instantiate(); using var tree = new SceneTree(root);
            var first = root.GetNode<InstancePlaceholder>("Pending0"); var second = root.GetNode<InstancePlaceholder>("Pending1");
            Check(_factories == 0 && !ResourceLoader.HasCached(At(folder, "actor.e2dscene")), "Fresh loading never reads the deferred template or constructs its nodes.");
            var raw = (MissingResource)first.GetStoredValue<Resource>(nameof(Actor.Data));
            Check(raw.GetRecordedProperty<int>("Value") == 73 && raw.GetRecordedProperty<Resource>("Self") == raw, "Pending resource cycles survive file decoding.");
            var values = first.GetStoredValue<int[]>(nameof(Actor.Values)); values[0] = 999;
            Check(first.GetStoredValue<int[]>(nameof(Actor.Values))[0] == 2 && first.GetStoredValue<string>(nameof(Actor.Label)) == "deferred", "Pending scalar arrays are independent snapshots.");
            Reject<InvalidCastException>(() => first.GetStoredValue<int>(nameof(Actor.Label)));
            Reject<InvalidCastException>(() => first.GetStoredValue<string>(nameof(Actor.Data)));
            scene.Dispose();
            var one = (Actor)first.CreateInstance()!;
            var template = one.SpawnSceneIdentity!;
            var two = (Actor)second.CreateInstance()!;
            Check(template == two.SpawnSceneIdentity && !template.IsDisposed && one.Data != two.Data && one.Data != raw, "Two default creations share one live template and isolate local resources.");
            Check(one.Items![0] == one.Data && one.Items[2] == one.Data && one.Items[1] is null && ((MissingResource)one.Data!).GetRecordedProperty<Resource>("Self") == one.Data, "Created local graph retains aliases, nulls and cycles.");
            Check(one.Position == new Vector2(42, 53) && one.Target == one.GetNode("Inherited") && one.NotifiedPosition == new Vector2(7, 9), "Fresh pending values restore after notification and resolve internal references first.");
            one.Data = null; one.Items = null; two.Data = null; two.Items = null;
            one.Owner = root; two.Owner = root;
            using var capture = new PackedScene(); capture.Pack(root);
            var snapshot = capture.GetState();
            one.Dispose(); Check(!template.IsDisposed, "A sibling and captured template links retain shared file ownership.");
            two.Dispose(); Check(!template.IsDisposed, "Captured links retain an automatically loaded template after actors leave.");
            using (var repeated = capture.Instantiate()) Check(repeated.GetChildCount() == 4, "Captured automatic template identities remain executable.");
            capture.Dispose(); Check(!template.IsDisposed, "An exported SceneState retains the captured template graph.");
            snapshot.Dispose(); Check(template.IsDisposed, "The final internal lease retires the automatically loaded template.");
            using var reloaded = ResourceLoader.Load<PackedScene>(At(folder, "actor.e2dscene"));
            using (var created = first.CreateInstance()!) Check(created.SpawnSceneIdentity == reloaded, "A caller-owned cache entry can supply default creation.");
            Check(!reloaded.IsDisposed, "Default creation does not take disposal ownership of an externally cached template.");
            using (var changes = new SceneTree(new Node { Name = "SceneHost" }))
            {
                changes.ChangeSceneToFile(At(folder, "actor.e2dscene")); changes.FlushDeferred();
                changes.ReloadCurrentScene(); changes.FlushDeferred();
                var reloadedIdentity = changes.CurrentScene!.SpawnSceneIdentity!;
                Check(!reloadedIdentity.IsDisposed && reloadedIdentity != reloaded, "File reload bypasses cache while retaining its new executable template.");
                changes.Dispose(); Check(reloadedIdentity.IsDisposed && !reloaded.IsDisposed, "Scene shutdown releases only its automatically loaded reload template.");
            }
            using var outer = new Node { Name = "Outer" };
            using var kept = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep);
            var level = kept.Instantiate(); outer.AddChild(level); level.Owner = outer; outer.SetEditableInstance(level, true);
            var inherited = level.GetNode<InstancePlaceholder>("Pending0");
            ((PropertyDescriptor<InstancePlaceholder, Vector2>)inherited.GetPropertyList().Single(p => p.Name == nameof(Entity.Position))).SetValue(inherited, new(80, 90));
            using var grand = new PackedScene(); grand.Pack(outer);
            ResourceSaver.Save(grand, At(folder, "grand.e2dscene"), SaverFlags.BundleResources | SaverFlags.SaveBigEndian);
            using var grandFile = ResourceLoader.Load<PackedScene>(At(folder, "grand.e2dscene"), ResourceLoader.CacheMode.IgnoreDeep);
            using var restored = grandFile.Instantiate();
            Check(restored.GetNode<InstancePlaceholder>("Level/Pending0").GetStoredValue<Vector2>(nameof(Entity.Position)) == new Vector2(80, 90), "An inherited placeholder retains its path and editable pending overrides on recapture.");
            using var detached = grandFile.Instantiate();
            var nestedPending = detached.GetNode<InstancePlaceholder>("Level/Pending0");
            var retained = (MissingResource)nestedPending.GetStoredValue<Resource>(nameof(Actor.Shared));
            nestedPending.Parent!.RemoveChild(nestedPending); detached.Dispose(); restored.Dispose(); grandFile.Dispose();
            using var detachedHost = new Node { Name = "DetachedHost" }; detachedHost.AddChild(nestedPending);
            using var detachedTree = new SceneTree(detachedHost);
            Check(retained.GetRecordedProperty<int>("Value") == 91, "A nested placeholder retains pending nonlocal resources after leaving its decoded parent graph.");
            var materialized = (Actor)nestedPending.CreateInstance(customScene: reloaded)!;
            detachedHost.RemoveChild(materialized); nestedPending.Dispose(); detachedTree.Dispose();
            Check(materialized.Shared == retained && retained.GetRecordedProperty<int>("Value") == 91, "Materialization transfers pending graph retention to a detached created actor.");
            materialized.Dispose(); Check(retained.IsDisposed, "Final materialized actor retirement releases the detached pending file graph.");
        }
        Console.WriteLine("Deferred files: fresh lazy load, endian/compression, local aliases, shared template leases and nested recapture passed.");
    }
    private static void MissingFile(string folder)
    {
        using var scene = ResourceLoader.Load<PackedScene>(At(folder, "bundled.e2dscene"), ResourceLoader.CacheMode.IgnoreDeep);
        var root = scene.Instantiate(); using var tree = new SceneTree(root);
        var pending = root.GetNode<InstancePlaceholder>("Pending0");
        Reject<Exception>(() => pending.CreateInstance(replace: true));
        Check(!pending.IsDisposed && pending.Parent == root && !pending.IsQueuedForDeletion, "A missing template does not destroy the placeholder or publish an instance.");
        using var source = new Actor { Name = "Replacement" }; using var custom = new PackedScene(); custom.Pack(source);
        Check(pending.CreateInstance(customScene: custom) is Actor, "A supplied compatible scene replaces the missing path lookup.");
    }
    private static void CorruptFields(string folder)
    {
        var source = At(folder, "plain.e2dscene");
        var payload = ResourceArchive.ReadPayload(source, out var bigEndian, out var version);
        Check(!bigEndian && version == 6, "Malformed-field fixture uses the plain current archive layout.");
        using var scan = new StreamPeerBuffer { DataArray = payload };
        _ = scan.Get32(); ResourceArchiveStrings.Read(scan); ResourceArchive.ReadRoleMetadata(scan, version);
        ResourceArchiveStrings.Read(scan); scan.GetU8(); ResourceArchiveStrings.Read(scan); ResourceArchiveStrings.Read(scan); scan.Get64(); scan.Get32();
        var nodes = scan.Get32(); var linked = new bool[nodes];
        var fields = new Dictionary<string, (int Role, int Endian, int Value, int Size)>();
        for (var index = 0; index < 2; index++)
        {
            ResourceArchiveStrings.Read(scan); ResourceArchiveStrings.Read(scan); scan.Get32(); scan.Get32(); scan.Get32();
            ResourceArchive.ReadSceneLink(scan, version, index, linked, 65536);
            var groups = scan.Get32(); for (var i = 0; i < groups; i++) ResourceArchiveStrings.Read(scan);
            SkipFields(false); SkipFields(index == 1);
        }
        void SkipFields(bool record)
        {
            var count = scan.Get32();
            for (var i = 0; i < count; i++)
            {
                var name = ResourceArchiveStrings.Read(scan); ResourceArchiveStrings.Read(scan);
                var role = scan.GetPosition(); scan.GetU8(); var endian = scan.GetPosition(); scan.GetU8();
                var size = scan.Get32(); var value = scan.GetPosition();
                if (record) fields.Add(name, (role, endian, value, size));
                scan.Seek(value + size);
            }
        }
        foreach (var problem in new[] { "role", "endian", "reference", "node-path" })
        {
            var altered = (byte[])payload.Clone();
            if (problem == "role") altered[fields[nameof(Entity.Position)].Role] = 255;
            else if (problem == "endian") altered[fields[nameof(Entity.Position)].Endian] = 2;
            else System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(altered.AsSpan(fields[problem == "reference" ? nameof(Actor.Data) : nameof(Actor.Target)].Value), int.MaxValue);
            var file = ResourceArchive.ReadFile(source);
            System.Security.Cryptography.SHA256.HashData(altered).CopyTo(file, 32); altered.CopyTo(file, 64);
            var path = At(folder, "invalid-" + problem + ".e2dscene"); File.WriteAllBytes(path, file);
            Reject<InvalidDataException>(() => ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.ReplaceDeep));
            Check(!ResourceLoader.HasCached(path), "Malformed deferred " + problem + " cannot publish a cache entry.");
        }
    }
}
