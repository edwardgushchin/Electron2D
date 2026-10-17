using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using Electron2D;
using static TextureArrayTests;

internal static partial class MissingTextureTests
{
    private const string TextureID = "tests.RestorableTexture";
    private sealed class RestoredTexture : Texture
    {
        internal int Width = 1, Height = 1;
        internal Color Tint = Colors.Blue;
        internal Resource? Link;
        internal Resource?[] Many = [];
        public override int GetWidth() { ThrowIfDisposed(); return Width; }
        public override int GetHeight() { ThrowIfDisposed(); return Height; }
        public override Image GetImage() { ThrowIfDisposed(); var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Tint); return image; }
        protected override Resource CreateDuplicateInstance() => new RestoredTexture();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> copy, Func<Resource?, Resource?> force)
        {
            var other = (RestoredTexture)target; other.Width = Width; other.Height = Height; other.Tint = Tint; other.Link = copy(Link); other.Many = Many.Select(copy).ToArray();
        }
        protected override void RemapResourceReferences(Func<Resource, Resource> remap)
        {
            base.RemapResourceReferences(remap); if (Link is not null) Link = remap(Link); Many = Many.Select(r => r is null ? null : remap(r)).ToArray();
        }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [
            new PropertyDescriptor<RestoredTexture, int>(nameof(Width), t => t.Width, (t, v) => t.Width = v, _ => 1, stored: true),
            new PropertyDescriptor<RestoredTexture, int>(nameof(Height), t => t.Height, (t, v) => t.Height = v, _ => 1, stored: true),
            new PropertyDescriptor<RestoredTexture, Color>(nameof(Tint), t => t.Tint, (t, v) => t.Tint = v, _ => Colors.Blue, stored: true),
            new PropertyDescriptor<RestoredTexture, Resource?>(nameof(Link), t => t.Link, (t, v) => t.Link = v, _ => null, stored: true),
            new PropertyDescriptor<RestoredTexture, Resource?[]>(nameof(Many), t => t.Many, (t, v) => t.Many = v, _ => [], stored: true)
        ]);
    }
    private static RestoredTexture CreateTexture() => new();
    private static string At(string folder, string name) => System.IO.Path.Combine(folder, name);
    private static void Register() => ResourceFileTypes.RegisterResource(TextureID, CreateTexture);

    internal static void RunManaged()
    {
        var folder = At(System.IO.Path.GetTempPath(), "electron2d-missing-texture-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            Child("produce", folder);
            Preserve(folder);
            Child("restore", folder);
            External(folder);
            MissingPayloadRecords(folder);
            EmptyOrigin(folder);
            WrongDependencyIdentity(folder);
            Legacy(folder);
            Corruption(folder);
            ResourceLoader.SetAbortOnMissingResources(false);
            using var cached = ResourceLoader.Load<Texture>(At(folder, "source.e2dres"));
            Check(cached is PlaceholderTexture, "Unknown factory publishes a typed cached placeholder.");
            Register();
            using var restored = ResourceLoader.Load<Texture>(At(folder, "source.e2dres"), ResourceLoader.CacheMode.ReplaceDeep);
            Check(restored is RestoredTexture && !ReferenceEquals(restored, cached) && !cached.IsDisposed && ReferenceEquals(ResourceLoader.GetCachedRef<Texture>(At(folder, "source.e2dres")), restored), "Available factory replaces the cache identity without morphing or disposing the borrowed placeholder.");
            VerifyRestored((RestoredTexture)restored);
        }
        finally { ResourceLoader.SetAbortOnMissingResources(true); Directory.Delete(folder, true); }
        Console.WriteLine("Missing textures: typed fallback, opaque graph/copy/local/fresh restoration, missing-payload persistence/revival, cache replacement, legacy layouts, identity and corruption rejection passed.");
    }

    private static void Produce(string folder)
    {
        Register();
        using var texture = new RestoredTexture { Width = 24, Height = 12, Tint = Colors.Blue, ResourceLocalToScene = true }; texture.Link = texture; texture.Many = [texture, null, texture];
        ResourceSaver.Save(texture, At(folder, "source.e2dres"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
        using var root = MakeRoot(texture); using var scene = new PackedScene(); scene.Pack(root);
        ResourceSaver.Save(scene, At(folder, "scene.e2dscene"), SaverFlags.BundleResources);
    }
    private static Entity MakeRoot(Texture texture)
    {
        var root = new Entity { Name = "Root" }; var first = new Sprite { Name = "First", Texture = texture, Centered = false, Position = new(4, 4), TextureFilter = TextureFilter.Nearest }; var second = new Sprite { Name = "Second", Texture = texture, Centered = false, Position = new(36, 4), TextureFilter = TextureFilter.Nearest };
        root.AddChild(first); root.AddChild(second); first.Owner = root; second.Owner = root; return root;
    }
    private static void Preserve(string folder)
    {
        ResourceLoader.SetAbortOnMissingResources(true);
        Reject<InvalidDataException>(() => ResourceLoader.Load<Texture>(At(folder, "source.e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
        Check(!ResourceLoader.HasCached(At(folder, "source.e2dres")), "Strict unknown factory failure publishes no cache.");
        ResourceLoader.SetAbortOnMissingResources(false);
        using var loaded = ResourceLoader.Load<Texture>(At(folder, "source.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        Check(loaded is PlaceholderTexture placeholder && placeholder.Size == new Vector2(24, 12) && placeholder.GetImage() is null, "Unknown ordinary texture retains layout and never invents readable image data.");
        using var duplicate = loaded.DuplicateDeep(DeepDuplicateMode.All);
        ResourceSaver.Save(duplicate, At(folder, "saved.e2dres"), SaverFlags.BundleResources);
        ResourceLoader.LoadThreadedRequest<Texture>(At(folder, "source.e2dres"), cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
        using var threaded = ResourceLoader.LoadThreadedGet<Texture>(At(folder, "source.e2dres"));
        Check(threaded is PlaceholderTexture p && p.Size == new Vector2(24, 12), "Threaded publication supports the typed missing texture.");
        Check(ResourceLoader.GetClassesUsed(At(folder, "scene.e2dscene")).Contains(typeof(PlaceholderTexture)), "Class metadata includes the typed missing texture without constructing its factory.");
        using var scene = ResourceLoader.Load<PackedScene>(At(folder, "scene.e2dscene"), ResourceLoader.CacheMode.IgnoreDeep);
        using var first = scene.Instantiate(); using var second = scene.Instantiate();
        var one = (PlaceholderTexture)((Sprite)first.GetNode("First")).Texture!; var alias = ((Sprite)first.GetNode("Second")).Texture; var two = ((Sprite)second.GetNode("First")).Texture;
        Check(one == alias && one != two && one.GetLocalScene() == first, "Missing texture local aliases and per-instance lifetime.");
        using var packed = new PackedScene(); packed.Pack(first);
        ResourceSaver.Save(packed, At(folder, "saved.e2dscene"), SaverFlags.BundleResources | SaverFlags.SaveBigEndian | SaverFlags.Compress);
    }
    private static void Restore(string folder)
    {
        Register(); ResourceLoader.SetAbortOnMissingResources(true);
        using var loaded = ResourceLoader.Load<Texture>(At(folder, "saved.e2dres"), ResourceLoader.CacheMode.IgnoreDeep); Check(loaded is RestoredTexture, "Re-registered factory restores the actual source class."); VerifyRestored((RestoredTexture)loaded);
        using var scene = ResourceLoader.Load<PackedScene>(At(folder, "saved.e2dscene"), ResourceLoader.CacheMode.IgnoreDeep); using var root = scene.Instantiate();
        var texture = (RestoredTexture)((Sprite)root.GetNode("First")).Texture!; VerifyRestored(texture); Check(texture == ((Sprite)root.GetNode("Second")).Texture && texture.GetLocalScene() == root, "Original schema, aliases and scene-local ownership recover after a placeholder re-save.");
    }
    private static void VerifyRestored(RestoredTexture texture) => Check(texture.Width == 24 && texture.Height == 12 && texture.Tint == Colors.Blue && texture.Link == texture && texture.Many.Length == 3 && texture.Many[0] == texture && texture.Many[1] is null && texture.Many[2] == texture, "Original scalar state and relocated cycles/aliases are preserved.");

    private static void External(string folder)
    {
        var dependency = At(folder, "external-image.e2dres"); var scenePath = At(folder, "external.e2dscene"); var bundledPath = At(folder, "external-bundled.e2dscene"); var copiedPath = At(folder, "external-copy.e2dscene");
        SaveImage(dependency);
        using (var texture = ResourceLoader.Load<ImageTexture>(dependency, ResourceLoader.CacheMode.Ignore))
        using (var root = MakeRoot(texture))
        using (var scene = new PackedScene()) { scene.Pack(root); ResourceSaver.Save(scene, scenePath); }
        var dependencies = ResourceLoader.GetDependencies(scenePath, true); Check(dependencies.Length == 1 && dependencies[0].Contains("ImageTexture", StringComparison.Ordinal), "Texture role metadata does not disrupt dependency discovery.");
        File.Delete(dependency); ResourceLoader.SetAbortOnMissingResources(false);
        using (var scene = ResourceLoader.Load<PackedScene>(scenePath, ResourceLoader.CacheMode.IgnoreDeep))
        {
            using var root = scene.Instantiate(); var texture = ((Sprite)root.GetNode("First")).Texture;
            Check(texture is PlaceholderTexture p && p.Size == new Vector2(18, 10) && p.ResourceLocalToScene && p.ResourceName == "External image" && p.GetLocalScene() == root, "Missing known image dependency retains dimensions, name and scene-local ownership.");
            ResourceSaver.Save(scene, bundledPath, SaverFlags.BundleResources | SaverFlags.Compress);
            ResourceSaver.Save(scene, copiedPath, SaverFlags.RelativePaths);
        }
        ResourceLoader.RenameDependencies(bundledPath, new Dictionary<string, string>());
        Check(ResourceLoader.GetDependencies(bundledPath).Length == 0, "Metadata rewriting retains the bundled missing-payload marker.");
        using (var bundled = ResourceLoader.Load<PackedScene>(bundledPath, ResourceLoader.CacheMode.IgnoreDeep))
        using (var root = bundled.Instantiate()) Check(((Sprite)root.GetNode("First")).Texture is PlaceholderTexture p && p.Size == new Vector2(18, 10), "Bundling missing pixels retains a placeholder even when its original built-in factory is available.");
        ResourceLoader.SetAbortOnMissingResources(true); Reject<InvalidDataException>(() => ResourceLoader.Load<PackedScene>(bundledPath, ResourceLoader.CacheMode.IgnoreDeep));
        SaveImage(dependency);
        using var revived = ResourceLoader.Load<PackedScene>(copiedPath, ResourceLoader.CacheMode.IgnoreDeep); using var instance = revived.Instantiate();
        var actual = ((Sprite)instance.GetNode("First")).Texture; Check(actual is ImageTexture && actual.GetSize() == new Vector2(18, 10), "Unbundled external identity loads real data when the file returns."); using var pixels = actual!.GetImage()!; Check(pixels.GetPixel(0, 0) == Colors.Green, "Revived image contains actual source pixels.");
    }
    private static void SaveImage(string path)
    {
        using var image = Image.CreateEmpty(3, 2, false, Image.Format.Rgba8); image.Fill(Colors.Green); using var texture = ImageTexture.CreateFromImage(image); texture.SetSizeOverride(new(18, 10)); texture.ResourceName = "External image"; texture.ResourceLocalToScene = true; ResourceSaver.Save(texture, path);
    }
    private static void EmptyOrigin(string folder)
    {
        using var source = new MissingResource { RecordingProperties = true }; source.SetRecordedProperty("Number", 71); source.SetRecordedProperty<Resource?>("Self", source);
        var path = At(folder, "empty-origin.e2dres"); ResourceSaver.Save(source, path, SaverFlags.BundleResources);
        using var loaded = ResourceLoader.Load<MissingResource>(path, ResourceLoader.CacheMode.IgnoreDeep); Check(loaded.OriginalClass.Length == 0 && loaded.GetRecordedProperty<int>("Number") == 71 && loaded.GetRecordedProperty<Resource?>("Self") == loaded, "Authored empty-origin missing resource retains recorded fields and cycles.");
    }
    private static void MissingPayloadRecords(string folder)
    {
        foreach (var mesh in new[] { false, true })
        {
            var path = At(folder, mesh ? "missing-mesh.e2dres" : "missing-generic.e2dres"); var bundled = path + ".bundled.e2dres";
            using (Resource dependency = mesh ? new PlaceholderMesh { AABB = new(1, 2, 3, 4) } : new Resource())
            using (var source = new MissingResource { RecordingProperties = true })
            {
                dependency.ResourcePath = path + ".absent.e2dres"; source.SetRecordedProperty<Resource?>("Child", dependency); ResourceSaver.Save(source, path);
            }
            ResourceLoader.SetAbortOnMissingResources(false);
            using var loaded = ResourceLoader.Load<MissingResource>(path, ResourceLoader.CacheMode.IgnoreDeep); using var copy = loaded.DuplicateDeep(DeepDuplicateMode.All);
            ResourceSaver.Save(copy, bundled, SaverFlags.BundleResources);
            using var tolerant = ResourceLoader.Load<MissingResource>(bundled, ResourceLoader.CacheMode.IgnoreDeep); var child = tolerant.GetRecordedProperty<Resource?>("Child")!;
            Check(mesh ? child is PlaceholderMesh p && p.AABB == new Rect2(1, 2, 3, 4) : child is MissingResource, "Known generic/mesh factories cannot reconstruct an unavailable copied payload.");
            ResourceLoader.SetAbortOnMissingResources(true); Reject<InvalidDataException>(() => ResourceLoader.Load<MissingResource>(bundled, ResourceLoader.CacheMode.IgnoreDeep));
            if (child is MissingResource missing)
            {
                missing.OriginalClass = missing.OriginalClass; ResourceSaver.Save(missing, path + ".authored.e2dres");
                using var authored = ResourceLoader.Load<Resource>(path + ".authored.e2dres", ResourceLoader.CacheMode.IgnoreDeep); Check(authored.GetType() == typeof(Resource), "Explicit original-class assignment authors a new known binding.");
            }
        }
    }
    private static void WrongDependencyIdentity(string folder)
    {
        var dependency = At(folder, "identity-child.e2dres"); var parent = At(folder, "identity-parent.e2dres");
        using (var child = new MissingResource { OriginalClass = "tests.ExpectedMissing", ResourcePath = dependency })
        using (var root = new MissingResource { OriginalClass = "tests.MissingHolder", RecordingProperties = true }) { ResourceSaver.Save(child, dependency); root.SetRecordedProperty<Resource?>("Child", child); ResourceSaver.Save(root, parent); }
        using (var wrong = new MissingResource { OriginalClass = "tests.OtherMissing" }) ResourceSaver.Save(wrong, dependency);
        ResourceLoader.SetAbortOnMissingResources(false); Reject<InvalidDataException>(() => ResourceLoader.Load<MissingResource>(parent, ResourceLoader.CacheMode.IgnoreDeep)); Check(!ResourceLoader.HasCached(parent), "Matching wrapper types cannot hide different original external factory identities.");
    }
    private static void Legacy(string folder)
    {
        ResourceLoader.SetAbortOnMissingResources(true);
        foreach (var version in new[] { 1, 2, 3 })
        {
            using var body = new StreamPeerBuffer(); body.PutU8(0); body.Put32(9); body.Put32(7);
            using var data = new StreamPeerBuffer(); data.Put32(1); ResourceArchiveStrings.Write(data, "ImageTexture"); if (version >= 2) data.PutU8(0); ResourceArchiveStrings.Write(data, "legacy"); data.PutU8(0); ResourceArchiveStrings.Write(data, ""); ResourceArchiveStrings.Write(data, ""); data.Put64(-1); data.Put32(body.GetSize()); data.PutData(body.DataArray);
            var path = At(folder, "legacy-" + version + ".e2dres"); WriteEnvelope(path, data.DataArray, version, SaverFlags.None);
            using var loaded = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.IgnoreDeep); Check(loaded.GetSize() == new Vector2(9, 7), "Legacy known texture role remains readable at version " + version);
        }
    }
    private static void Corruption(string folder)
    {
        ResourceLoader.SetAbortOnMissingResources(false);
        var data = ResourceArchive.ReadPayload(At(folder, "source.e2dres"), out var endian, out var version);
        using var probe = new StreamPeerBuffer { DataArray = data, BigEndian = endian }; probe.Get32(); ResourceArchiveStrings.Read(probe); var role = probe.GetPosition();
        foreach (var bad in new[] { "role", "missing", "size" })
        {
            using var value = new StreamPeerBuffer { DataArray = data, BigEndian = endian }; value.Seek(role + (bad == "role" ? 0 : bad == "missing" ? 1 : 2));
            if (bad == "size") value.PutFloat(float.NaN); else value.PutU8(bad == "role" ? (byte)3 : (byte)2);
            var path = At(folder, "corrupt-" + bad + ".e2dres"); WriteEnvelope(path, value.DataArray, version, endian ? SaverFlags.SaveBigEndian : SaverFlags.None);
            Reject<InvalidDataException>(() => ResourceLoader.Load<Texture>(path, ResourceLoader.CacheMode.ReplaceDeep)); Check(!ResourceLoader.HasCached(path), "Malformed texture metadata does not publish a cache.");
        }
    }
    private static void WriteEnvelope(string path, byte[] payload, int version, SaverFlags flags)
    {
        var file = new byte[64 + payload.Length]; "E2DASSET"u8.CopyTo(file); BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(8), version); BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(12), (int)flags); BinaryPrimitives.WriteInt64LittleEndian(file.AsSpan(16), 1); BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(24), payload.Length); BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(28), payload.Length); SHA256.HashData(payload).CopyTo(file, 32); payload.CopyTo(file, 64); File.WriteAllBytes(path, file);
    }
    private static void Child(string mode, string folder)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        if ((System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? "").Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(MissingTextureTests).Assembly.Location);
        start.ArgumentList.Add("--missing-texture"); start.ArgumentList.Add(mode); start.ArgumentList.Add(folder); start.Environment.Remove("ELECTRON2D_TEST_MISSING_TEXTURE");
        using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Missing texture child deadline."); }
        Check(child.ExitCode == 0, "Fresh missing texture " + mode + ": " + output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    }
    internal static void RunChild(string mode, string folder)
    {
        if (mode == "produce") Produce(folder);
        else if (mode == "restore") Restore(folder);
        else if (mode.StartsWith("restore-native-", StringComparison.Ordinal)) { Restore(folder); Render(folder, true, mode["restore-native-".Length..]); }
        else throw new ArgumentException("Unknown missing texture child mode.");
    }
}
