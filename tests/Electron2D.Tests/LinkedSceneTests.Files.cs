using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using Electron2D;
using static TextureArrayTests;

internal static partial class LinkedSceneTests
{
    private static string At(string folder, string name) => System.IO.Path.Combine(folder, name);
    private static void Files()
    {
        var folder = At(System.IO.Path.GetTempPath(), "electron2d-linked-scenes-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            Child("produce", folder); Child("read", folder); CorruptLinks(folder);
            SaveActor(folder, Colors.Green, new(2, 0)); Child("updated", folder);
            File.Delete(At(folder, "actor.e2dscene")); Child("bundled", folder);
            LegacyScenes(folder);
        }
        finally { Directory.Delete(folder, true); }
    }
    private static void SaveActor(string folder, Color color, Vector2 offset)
    {
        Register(); using var texture = NewTexture(color); using var root = MakeActor(texture); ((Sprite)root.GetNode("Visual")).Position = offset;
        using var data = new MissingResource { RecordingProperties = true, ResourceLocalToScene = true }; data.SetRecordedProperty<Resource?>("Self", data); data.SetRecordedProperty("Value", 0); ((Probe)root).Data = data;
        var receiver = new Probe { Name = "Receiver" }; root.AddChild(receiver); receiver.Owner = root;
        using var token = _pulse.Subscribe((Probe)root, receiver, 3); using var actor = new PackedScene(); actor.Pack(root);
        ResourceSaver.Save(actor, At(folder, "actor.e2dscene"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
    }
    private static void Produce(string folder)
    {
        SaveActor(folder, Colors.Red, Vector2.Zero);
        using var actor = ResourceLoader.Load<PackedScene>(At(folder, "actor.e2dscene"), ResourceLoader.CacheMode.IgnoreDeep);
        using var level = new Probe { Name = "Level", Tag = "Outer" };
        var one = (Probe)actor.Instantiate(); one.Name = "One"; one.Tag = "Override"; one.Position = new(4, 4); level.AddChild(one); one.Owner = level; level.SetEditableInstance(one, true);
        using (var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8)) { image.Fill(Colors.Blue); ((ImageTexture)((Sprite)one.GetNode("Visual")).Texture!).Update(image); }
        ((MissingResource)one.Data!).SetRecordedProperty("Value", 83); one.Target = one.GetNode("Alias");
        var two = (Probe)actor.Instantiate(); two.Name = "Two"; two.Position = new(36, 4); level.AddChild(two); two.Owner = level;
        using var parent = new PackedScene(); parent.Pack(level);
        ResourceSaver.Save(parent, At(folder, "external.e2dscene"));
        ResourceSaver.Save(parent, At(folder, "relative.e2dscene"), SaverFlags.RelativePaths | SaverFlags.Compress | SaverFlags.SaveBigEndian);
        ResourceSaver.Save(parent, At(folder, "bundled.e2dscene"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
    }
    private static void ReadFiles(string folder, bool updated, bool bundledOnly)
    {
        Register();
        foreach (var name in bundledOnly ? new[] { "bundled" } : new[] { "external", "relative", "bundled" })
        {
            var path = At(folder, name + ".e2dscene");
            using var parent = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); var state = parent.GetState();
            var oneIndex = Enumerable.Range(0, state.GetNodeCount()).Single(i => state.GetNodeName(i) == "One"); var twoIndex = Enumerable.Range(0, state.GetNodeCount()).Single(i => state.GetNodeName(i) == "Two");
            Check(state.GetNodeInstance(oneIndex) is { } scene && scene == state.GetNodeInstance(twoIndex), "Fresh archives preserve shared linked-template identity.");
            Check(ResourceLoader.GetClassesUsed(path).Contains(typeof(Probe)), "Version-5 class discovery consumes linked headers.");
            Check(ResourceLoader.GetDependencies(path).Length == (name == "bundled" ? 0 : 1), "Linked dependency metadata matches external/bundled policy.");
            ResourceLoader.RenameDependencies(path, new Dictionary<string, string>());
            ResourceLoader.LoadThreadedRequest<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
            using (var threaded = ResourceLoader.LoadThreadedGet<PackedScene>(path))
            using (var detached = threaded.Instantiate()) Check(detached.GetNode("One/Visual") is Sprite, "Threaded graph preparation retains linked runtime instances.");
            Notifications.Clear(); using var root = parent.Instantiate(); var one = (Probe)root.GetNode("One"); var two = (Probe)root.GetNode("Two");
            Check(Notifications.SequenceEqual(new[] { "Inner:0", "Inner:0", "Outer:0" }) && one.Tag == "Override", "Fresh restored child notifications precede parent overlays.");
            var visual = (Sprite)one.GetNode("Visual"); var alias = (Sprite)one.GetNode("Alias"); var other = (Sprite)two.GetNode("Visual");
            Check(visual.Texture == alias.Texture && visual.Texture != other.Texture && visual.Texture!.GetLocalScene() == one && other.Texture!.GetLocalScene() == two, "Fresh linked local texture aliases and ownership.");
            using var blue = visual.Texture!.GetImage()!; using var original = other.Texture!.GetImage()!;
            var follows = updated && name != "bundled";
            Check(blue.GetPixel(0, 0) == Colors.Blue && original.GetPixel(0, 0) == (follows ? Colors.Green : Colors.Red) && other.Position == (follows ? new Vector2(2, 0) : Vector2.Zero), "External links follow returned file data while bundled snapshots remain independent.");
            var data = (MissingResource)one.Data!; Check(data.GetRecordedProperty<Resource?>("Self") == data && data.GetRecordedProperty<int>("Value") == 83 && one.Target == alias, "Fresh resource cycles, edited fields and typed node references.");
            using var tree = new SceneTree(root); var receiver = (Probe)one.GetNode("Receiver"); one.Emit(); Check(receiver.Calls == 3, "Fresh inherited binding delivers exactly once.");
            using (var container = new Entity { Name = "Container" })
            using (var grand = new PackedScene())
            {
                var level = parent.Instantiate(); container.AddChild(level); level.Owner = container; grand.Pack(container);
                using var three = grand.Instantiate();
                Check(((Sprite)three.GetNode("Level/One/Visual")).Texture!.GetLocalScene() == three.GetNode("Level/One"), "Three template levels preserve the most specific local-scene owner.");
            }
            using var recaptured = new PackedScene(); recaptured.Pack(root); using var again = recaptured.Instantiate();
            Check(((Sprite)again.GetNode("One/Visual")).Texture!.GetImage() is { } pixels && PixelValue(pixels, Colors.Blue), "Repacking retains actual editable link metadata and overlays.");
        }
    }
    private static bool PixelValue(Image image, Color expected) { using (image) return image.GetPixel(0, 0) == expected; }
    private static void LegacyScenes(string folder)
    {
        foreach (var version in new[] { 1, 2, 3, 4, 5 })
        {
            using var scene = new StreamPeerBuffer(); scene.Put32(1); ResourceArchiveStrings.Write(scene, "Entity"); ResourceArchiveStrings.Write(scene, "Legacy"); scene.Put32(-1); scene.Put32(-1); scene.Put32(-1);
            if (version >= 5) { scene.Put32(-1); scene.Put32(-1); ResourceArchiveStrings.Write(scene, ""); }
            scene.Put32(0); scene.Put32(0); if (version >= 3) scene.Put32(0);
            using var body = new StreamPeerBuffer(); body.Put32(1); ResourceArchiveStrings.Write(body, "PackedScene"); if (version >= 2) body.PutU8(0); if (version >= 4) body.PutU8(0); ResourceArchiveStrings.Write(body, ""); body.PutU8(0); ResourceArchiveStrings.Write(body, ""); ResourceArchiveStrings.Write(body, ""); body.Put64(-1); body.Put32(scene.GetSize()); body.PutData(scene.DataArray);
            var path = At(folder, "legacy-" + version + ".e2dscene"); Envelope(path, body.DataArray, version);
            using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); using var root = loaded.Instantiate(); Check(root.Name == "Legacy" && root.GetChildCount() == 0, "Known legacy scene layout remains readable at version " + version);
        }
    }
    private static void CorruptLinks(string folder)
    {
        var source = At(folder, "external.e2dscene"); var payload = ResourceArchive.ReadPayload(source, out var endian, out var version);
        using var stream = new StreamPeerBuffer { DataArray = payload, BigEndian = endian }; stream.Get32(); ResourceArchiveStrings.Read(stream); ResourceArchive.ReadRoleMetadata(stream, version); ResourceArchiveStrings.Read(stream); stream.GetU8(); ResourceArchiveStrings.Read(stream); ResourceArchiveStrings.Read(stream); stream.Get64(); stream.Get32();
        stream.Get32(); ResourceArchiveStrings.Read(stream); ResourceArchiveStrings.Read(stream); stream.Get32(); stream.Get32(); stream.Get32(); var link = stream.GetPosition();
        foreach (var bad in new[] { "root-link", "reference-budget", "negative-root" })
        {
            using var invalid = new StreamPeerBuffer { DataArray = payload, BigEndian = endian }; invalid.Seek(link + (bad == "negative-root" ? 4 : 0)); invalid.Put32(bad == "root-link" ? 0 : bad == "negative-root" ? -2 : 65536);
            var path = At(folder, "corrupt-" + bad + ".e2dscene"); Envelope(path, invalid.DataArray, version);
            Reject<InvalidDataException>(() => ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.ReplaceDeep)); Check(!ResourceLoader.HasCached(path), "Malformed linked metadata cannot publish a cache entry.");
        }
    }
    private static void Envelope(string path, byte[] payload, int version)
    {
        var bytes = new byte[64 + payload.Length]; "E2DASSET"u8.CopyTo(bytes); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), version); BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(16), 1); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(24), payload.Length); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(28), payload.Length); SHA256.HashData(payload).CopyTo(bytes, 32); payload.CopyTo(bytes, 64); File.WriteAllBytes(path, bytes);
    }
    private static void Child(string mode, string folder)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        if ((System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? "").Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(LinkedSceneTests).Assembly.Location);
        start.ArgumentList.Add("--linked-scene"); start.ArgumentList.Add(mode); start.ArgumentList.Add(folder); start.Environment.Remove("ELECTRON2D_TEST_LINKED_SCENES");
        using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(60000)) { child.Kill(true); throw new TimeoutException("Linked scene child deadline."); }
        Check(child.ExitCode == 0, "Fresh linked scene " + mode + ": " + output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    }
    internal static void RunChild(string mode, string folder)
    {
        if (mode == "produce") Produce(folder);
        else if (mode is "read" or "updated" or "bundled") ReadFiles(folder, mode == "updated", mode == "bundled");
        else throw new ArgumentException("Unknown linked scene child mode.");
    }
}
