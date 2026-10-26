using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using Electron2D;
using Path = System.IO.Path;

internal static partial class MaterialPlaceholderTests
{
    private const string MissingID = "tests.UnavailableMaterial";
    private static int _checks;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { _checks++; try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static string At(string root, string name) => Path.Combine(root, name);
    internal static void RunManaged()
    {
        Material[] family = [new CanvasItemMaterial { BlendMode = BlendMode.Add }, new ShaderMaterial(), new ParticleProcessMaterial(), new BlitMaterial(), new PlaceholderMaterial()];
        try
        {
            foreach (var source in family)
            {
                source.ResourceName = "Source"; source.ResourceLocalToScene = true;
                using var first = source.CreatePlaceholder(); using var second = source.CreatePlaceholder();
                Check(first != second && first.ResourceName.Length == 0 && !first.ResourceLocalToScene && first.ResourcePath.Length == 0, "Every material creates independent program-free metadata.");
                Check(first.GetRID().IsValid() && first.GetRID() != second.GetRID(), "Placeholder identity is ordinary resource-owned identity.");
                using var copied = first.DuplicateDeep(); Check(copied is PlaceholderMaterial && copied != first, "Placeholder supports ordinary typed duplication.");
            }
        }
        finally { foreach (var material in family) material.Dispose(); }
        var dead = new PlaceholderMaterial(); dead.Dispose(); Reject<ObjectDisposedException>(() => dead.CreatePlaceholder());
        using (var particles = new GPUParticles()) using (var placeholder = new PlaceholderMaterial()) Reject<NotSupportedException>(() => particles.ProcessMaterial = placeholder);
        var folder = At(Path.GetTempPath(), "electron2d-material-placeholder-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            Produce(folder); Preserve(folder); RestoreFiles(folder); Child("restore", folder);
            External(folder); OpaqueGraph(folder); CacheReplacement(folder); ConcreteBoundary(folder); Legacy(folder); Corruption(folder);
            ResourceLoader.SetAbortOnMissingResources(true);
            using var authored = new PlaceholderMaterial { ResourceName = "Authored", ResourceLocalToScene = true };
            ResourceSaver.Save(authored, At(folder, "authored.e2dres"));
            using var loaded = ResourceLoader.Load<PlaceholderMaterial>(At(folder, "authored.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
            Check(loaded.ResourceName == "Authored" && loaded.ResourceLocalToScene && loaded.Data.OriginalClass.Length == 0, "Authored placeholder is strictly loadable without a missing identity.");
            Console.WriteLine($"PASS {_checks} material placeholder/family/file/scene/worker/recovery/legacy/corruption contracts.");
        }
        finally { ResourceLoader.SetAbortOnMissingResources(true); Directory.Delete(folder, true); }
    }
    private static void Produce(string folder)
    {
        using var material = new CanvasItemMaterial { ResourceName = "Preserved", ResourceLocalToScene = true, BlendMode = BlendMode.Add, LightMode = LightMode.Unshaded };
        ResourceSaver.Save(material, At(folder, "known.e2dres"));
        using var root = new Entity { Name = "Root" };
        var first = new Polygon { Name = "First", Material = material, Color = Colors.Blue, Vertices = [Vector2.Zero, new(20, 0), new(20, 20), new(0, 20)] };
        var second = new Polygon { Name = "Second", Position = new(28, 0), Material = material, Color = Colors.Blue, Vertices = first.Vertices };
        root.AddChild(first); root.AddChild(second); first.Owner = second.Owner = root;
        ResourceSaver.SaveScene(root, At(folder, "known.e2dscene"), SaverFlags.BundleResources | SaverFlags.SaveBigEndian);
        RenameFactory(At(folder, "known.e2dres"), At(folder, "missing.e2dres"), "CanvasItemMaterial", MissingID);
        RenameFactory(At(folder, "known.e2dscene"), At(folder, "missing.e2dscene"), "CanvasItemMaterial", MissingID);
    }
    private static void Preserve(string folder)
    {
        ResourceLoader.SetAbortOnMissingResources(true);
        Reject<InvalidDataException>(() => ResourceLoader.Load<Material>(At(folder, "missing.e2dres"), ResourceLoader.CacheMode.ReplaceDeep));
        Check(!ResourceLoader.HasCached(At(folder, "missing.e2dres")), "Unknown strict factory cannot publish a cache.");
        ResourceLoader.SetAbortOnMissingResources(false);
        using var source = ResourceLoader.Load<Material>(At(folder, "missing.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        Check(source is PlaceholderMaterial && source.ResourceName == "Preserved" && source.ResourceLocalToScene, "Unknown material uses its exact typed placeholder role.");
        using var copy = source.DuplicateDeep(DeepDuplicateMode.All);
        ResourceSaver.Save(copy, At(folder, "saved.e2dres"), SaverFlags.BundleResources | SaverFlags.Compress);
        ResourceLoader.LoadThreadedRequest<Material>(At(folder, "missing.e2dres"), cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
        ResourceLoader.SetAbortOnMissingResources(true);
        using var worker = ResourceLoader.LoadThreadedGet<Material>(At(folder, "missing.e2dres")); Check(worker is PlaceholderMaterial, "Worker preserves the captured material recovery policy after the global policy changes.");
        ResourceLoader.SetAbortOnMissingResources(false);
        Check(ResourceLoader.GetClassesUsed(At(folder, "missing.e2dscene")).Contains(typeof(PlaceholderMaterial)), "Class discovery identifies missing material without constructing its source.");
        using var scene = ResourceLoader.Load<PackedScene>(At(folder, "missing.e2dscene"), ResourceLoader.CacheMode.IgnoreDeep);
        using var one = scene.Instantiate(); using var two = scene.Instantiate();
        var first = ((Polygon)one.GetNode("First")).Material!; var alias = ((Polygon)one.GetNode("Second")).Material; var other = ((Polygon)two.GetNode("First")).Material;
        Check(first is PlaceholderMaterial && first == alias && first != other && first.GetLocalScene() == one, "Local missing material preserves aliases with independent scene-owned copies.");
        using var packed = new PackedScene(); packed.Pack(one);
        ResourceSaver.Save(packed, At(folder, "saved.e2dscene"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
        using var fromRoot = first.DuplicateDeep(); ResourceSaver.Save(fromRoot, At(folder, "from-root.e2dres"), SaverFlags.BundleResources);
    }
    private static void RestoreFiles(string folder)
    {
        foreach (var name in new[] { "saved.e2dres", "saved.e2dscene", "from-root.e2dres" }) RenameFactory(At(folder, name), At(folder, "restored-" + name), MissingID, "CanvasItemMaterial");
    }
    private static void Restore(string folder)
    {
        ResourceLoader.SetAbortOnMissingResources(true);
        using var source = ResourceLoader.Load<Material>(At(folder, "restored-saved.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        Verify(source); using var fromRoot = ResourceLoader.Load<Material>(At(folder, "restored-from-root.e2dres"), ResourceLoader.CacheMode.IgnoreDeep); Verify(fromRoot);
        using var scene = ResourceLoader.Load<PackedScene>(At(folder, "restored-saved.e2dscene"), ResourceLoader.CacheMode.IgnoreDeep); using var root = scene.Instantiate();
        var first = ((Polygon)root.GetNode("First")).Material!; Verify(first);
        Check(first == ((Polygon)root.GetNode("Second")).Material && first.GetLocalScene() == root, "Fresh process restores local material aliases and ownership.");
        Console.WriteLine("PASS fresh-process material scalar/schema/local-scene restoration.");
    }
    private static void Verify(Material material) => Check(material is CanvasItemMaterial c && c.ResourceName == "Preserved" && c.ResourceLocalToScene && c.BlendMode == BlendMode.Add && c.LightMode == LightMode.Unshaded, "Original material metadata and exact scalar values restore.");
    private static void External(string folder)
    {
        var external = At(folder, "external.e2dres"); var parent = At(folder, "parent.e2dres");
        using (var material = new CanvasItemMaterial { ResourcePath = external })
        using (var holder = new MissingResource { RecordingProperties = true }) { holder.SetRecordedProperty<Material?>("Material", material); ResourceSaver.Save(holder, parent); }
        ResourceLoader.SetAbortOnMissingResources(false);
        using var source = ResourceLoader.Load<MissingResource>(parent, ResourceLoader.CacheMode.IgnoreDeep);
        var placeholder = source.GetRecordedProperty<Material?>("Material"); Check(placeholder is PlaceholderMaterial, "Absent external payload preserves the declared Material type.");
        using var copied = source.DuplicateDeep(DeepDuplicateMode.All); ResourceSaver.Save(copied, At(folder, "bundled.e2dres"), SaverFlags.BundleResources);
        using var reloaded = ResourceLoader.Load<MissingResource>(At(folder, "bundled.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        Check(reloaded.GetRecordedProperty<Material?>("Material") is PlaceholderMaterial p && p.Data.MissingPayload, "A bundled copy cannot manufacture an empty known material after payload loss.");
        ResourceLoader.SetAbortOnMissingResources(true); Reject<InvalidDataException>(() => ResourceLoader.Load<MissingResource>(At(folder, "bundled.e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
        using (var actual = new CanvasItemMaterial { BlendMode = BlendMode.Mul }) ResourceSaver.Save(actual, external);
        using var recovered = ResourceLoader.Load<MissingResource>(parent, ResourceLoader.CacheMode.IgnoreDeep); Check(recovered.GetRecordedProperty<Material?>("Material") is CanvasItemMaterial c && c.BlendMode == BlendMode.Mul, "The original unbundled dependency recovers when its actual payload returns.");
    }
    private static void OpaqueGraph(string folder)
    {
        // The fixture models a future compiled material absent from this build; its closed field roles remain relocatable.
        using var source = new PlaceholderMaterial { Data = new MissingResourceData { OriginalClass = MissingID, Recording = true }, ResourceLocalToScene = true };
        source.Data.Set<Material?>("Self", source); source.Data.Set<Material?[]>("Many", [source, null, source]); source.Data.Set("Value", 37);
        using var copy = (PlaceholderMaterial)source.DuplicateDeep(DeepDuplicateMode.All);
        Check(copy != source && copy.Data.Get<Material?>("Self") == copy && copy.Data.Get<Material?[]>("Many")[2] == copy, "Deep copy relocates opaque aliases/cycles.");
        var path = At(folder, "opaque.e2dres"); ResourceSaver.Save(copy, path, SaverFlags.BundleResources | SaverFlags.SaveBigEndian | SaverFlags.Compress);
        ResourceLoader.SetAbortOnMissingResources(false); using var loaded = ResourceLoader.Load<Material>(path, ResourceLoader.CacheMode.IgnoreDeep);
        var missing = (PlaceholderMaterial)loaded;
        Check(missing.Data.Get<int>("Value") == 37 && missing.Data.Get<Material?>("Self") == missing && missing.Data.Get<Material?[]>("Many")[1] is null, "Opaque scalar, nullable array and identity graph survive a file roundtrip.");
        using var target = new PlaceholderMaterial(); var rid = target.GetRID(); target.CopyFromResource(missing);
        Check(target.GetRID() == rid && target.Data.Get<Material?>("Self") == missing, "Replacement preserves the target RID and shallow-copies ordinary reference edges.");
    }
    private static void CacheReplacement(string folder)
    {
        var path = At(folder, "cached.e2dres"); File.Copy(At(folder, "missing.e2dres"), path);
        ResourceLoader.SetAbortOnMissingResources(false); using var cached = ResourceLoader.Load<Material>(path); var rid = cached.GetRID();
        RenameFactory(path, path, MissingID, "Resource");
        Reject<InvalidDataException>(() => ResourceLoader.Load<Material>(path, ResourceLoader.CacheMode.ReplaceDeep));
        Check(ResourceLoader.Load<Material>(path) == cached && cached.GetRID() == rid, "Failed material decode preserves the prior live cached wrapper.");
        RenameFactory(At(folder, "known.e2dres"), path, "CanvasItemMaterial", "CanvasItemMaterial");
        ResourceLoader.SetAbortOnMissingResources(true);
        Check(ResourceLoader.Load<Material>(path) == cached, "Reuse does not replace an existing placeholder merely because the source changes.");
        using var actual = ResourceLoader.Load<Material>(path, ResourceLoader.CacheMode.ReplaceDeep); Verify(actual);
        Check(actual != cached && !cached.IsDisposed && ResourceLoader.Load<Material>(path) == actual, "Successful class replacement publishes the real material without disposing a borrowed placeholder.");
    }
    private sealed class ConcreteHolder : Resource
    {
        internal CanvasItemMaterial? Value;
        private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<ConcreteHolder, CanvasItemMaterial?>("Value", r => r.Value, (r, v) => r.Value = v, _ => null, stored: true)];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
        protected override Resource CreateDuplicateInstance() => new ConcreteHolder();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> copy, Func<Resource?, Resource?> force) => ((ConcreteHolder)target).Value = (CanvasItemMaterial?)copy(Value);
    }
    private static ConcreteHolder CreateHolder() => new();
    private static void ConcreteBoundary(string folder)
    {
        ResourceFileTypes.RegisterResource("tests.MaterialConcreteHolder", CreateHolder);
        using var material = new CanvasItemMaterial(); using var holder = new ConcreteHolder { Value = material };
        var known = At(folder, "concrete.e2dres"); var missing = At(folder, "concrete-missing.e2dres");
        ResourceSaver.Save(holder, known, SaverFlags.BundleResources); RenameFactory(known, missing, "CanvasItemMaterial", MissingID);
        ResourceLoader.SetAbortOnMissingResources(false);
        Reject<InvalidDataException>(() => ResourceLoader.Load<ConcreteHolder>(missing, ResourceLoader.CacheMode.ReplaceDeep));
        Check(!ResourceLoader.HasCached(missing), "Tolerant Material fallback cannot impersonate an incompatible concrete subtype.");
    }
    private static void Legacy(string folder)
    {
        ResourceLoader.SetAbortOnMissingResources(true);
        foreach (var version in Enumerable.Range(1, 7))
        {
            using var payload = new StreamPeerBuffer(); payload.Put32(1); ResourceArchiveStrings.Write(payload, "CanvasItemMaterial");
            if (version >= 2) payload.PutU8(0); if (version >= 4) payload.PutU8(0);
            ResourceArchiveStrings.Write(payload, "legacy"); payload.PutU8(0); ResourceArchiveStrings.Write(payload, ""); ResourceArchiveStrings.Write(payload, ""); payload.Put64(-1); payload.Put32(4); payload.Put32(0);
            var path = At(folder, "legacy-" + version + ".e2dres"); Envelope(path, payload.DataArray, version, false);
            using var material = ResourceLoader.Load<Material>(path, ResourceLoader.CacheMode.IgnoreDeep); Check(material is CanvasItemMaterial c && c.BlendMode == BlendMode.Mix, "Known legacy material role remains readable at version " + version);
        }
    }
    private static void Corruption(string folder)
    {
        var data = ResourceArchive.ReadPayload(At(folder, "known.e2dres"), out _, out _); using var stream = new StreamPeerBuffer { DataArray = data }; stream.Get32(); ResourceArchiveStrings.Read(stream); var role = stream.GetPosition();
        foreach (var kind in new[] { "role", "missing", "legacy-material" })
        {
            using var changed = new StreamPeerBuffer { DataArray = data }; changed.Seek(role + (kind == "missing" ? 1 : 0)); changed.PutU8(kind == "role" ? (byte)99 : kind == "missing" ? (byte)2 : (byte)3);
            var path = At(folder, "bad-" + kind + ".e2dres"); Envelope(path, changed.DataArray, kind == "legacy-material" ? 7 : 8, false);
            ResourceLoader.SetAbortOnMissingResources(false); Reject<InvalidDataException>(() => ResourceLoader.Load<Material>(path, ResourceLoader.CacheMode.ReplaceDeep)); Check(!ResourceLoader.HasCached(path), "Corrupt role/provenance never publishes an identity.");
        }
        RenameFactory(At(folder, "known.e2dres"), At(folder, "wrong-role.e2dres"), "CanvasItemMaterial", "Resource");
        Reject<InvalidDataException>(() => ResourceLoader.Load<Material>(At(folder, "wrong-role.e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
    }
    private static void RenameFactory(string source, string destination, string before, string after)
    {
        var data = ResourceArchive.ReadPayload(source, out var endian, out var version); using var input = new StreamPeerBuffer { DataArray = data, BigEndian = endian }; using var output = new StreamPeerBuffer { BigEndian = endian };
        var count = input.Get32(); var last = 0; var replaced = 0;
        for (var i = 0; i < count; i++)
        {
            var start = input.GetPosition(); var id = ResourceArchiveStrings.Read(input); var end = input.GetPosition(); input.Seek(start); ResourceArchive.ReadDefinition(input, version, false);
            if (id != before) continue;
            output.PutData(data.AsSpan(last, start - last)); ResourceArchiveStrings.Write(output, after); last = end; replaced++;
        }
        Check(replaced > 0, "Fixture replaced at least one exact factory identity."); output.PutData(data.AsSpan(last)); Envelope(destination, output.DataArray, version, endian);
    }
    private static void Envelope(string path, byte[] data, int version, bool endian)
    {
        var bytes = new byte[64 + data.Length]; "E2DASSET"u8.CopyTo(bytes); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), version); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12), endian ? (int)SaverFlags.SaveBigEndian : 0); BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(16), 1); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(24), data.Length); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(28), data.Length); SHA256.HashData(data).CopyTo(bytes, 32); data.CopyTo(bytes, 64); File.WriteAllBytes(path, bytes);
    }
    private static void Child(string mode, string folder)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(MaterialPlaceholderTests).Assembly.Location);
        start.ArgumentList.Add("--material-placeholder"); start.ArgumentList.Add(mode); start.ArgumentList.Add(folder);
        using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Material restoration child timed out."); }
        Check(child.ExitCode == 0, output.GetAwaiter().GetResult() + errors.GetAwaiter().GetResult());
    }
    internal static void RunChild(string mode, string folder) { if (mode == "restore") Restore(folder); else throw new ArgumentException("Unknown material fixture mode."); }
}
