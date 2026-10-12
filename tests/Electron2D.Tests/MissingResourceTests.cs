using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using Electron2D;
using static TextureArrayTests;

internal static class MissingResourceTests
{
    private const string GraphID = "tests.MissingGraph", MeshID = "tests.MissingMesh", PolicyID = "tests.MissingPolicy";
    private static readonly ManualResetEventSlim Entered = new(), Released = new();
    private static bool BlockFactory;
    private sealed class Restored : Resource
    {
        internal int Number;
        internal Resource? Link;
        internal Resource?[]? Many;
        protected override Resource CreateDuplicateInstance() => new Restored();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> copy, Func<Resource?, Resource?> force) { var r = (Restored)target; r.Number = Number; r.Link = copy(Link); r.Many = Many?.Select(copy).ToArray(); }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat([new PropertyDescriptor<Restored, int>("Number", r => r.Number, (r, v) => r.Number = v, _ => 0, stored: true), new PropertyDescriptor<Restored, Resource?>("Link", r => r.Link, (r, v) => r.Link = v, _ => null, stored: true), new PropertyDescriptor<Restored, Resource?[]?>("Many", r => r.Many, (r, v) => r.Many = v, _ => null, stored: true)]);
    }
    private static Restored CreateRestored() => new();
    private static Restored CreatePolicy() { if (BlockFactory) { Entered.Set(); Check(Released.Wait(TimeSpan.FromSeconds(15)), "Policy fixture timed out."); } return new(); }
    private sealed class RestoredMesh : Mesh
    {
        internal Vector2[] Vertices = [];
        internal int[] Indices = [];
        private MeshSurfaceData Data() => new() { Vertices = (Vector2[])Vertices.Clone(), Indices = (int[])Indices.Clone(), Colors = Enumerable.Repeat(Colors.Blue, Vertices.Length).ToArray() };
        protected override int OnGetSurfaceCount() => Vertices.Length == 0 ? 0 : 1;
        private void Index(int surface) { if (surface != 0 || Vertices.Length == 0) throw new ArgumentOutOfRangeException(nameof(surface)); }
        protected override MeshSurfaceData OnSurfaceGetArrays(int surface) { Index(surface); return Data(); }
        protected override int OnSurfaceGetArrayLen(int surface) { Index(surface); return Vertices.Length; }
        protected override int OnSurfaceGetArrayIndexLen(int surface) { Index(surface); return Indices.Length; }
        protected override PrimitiveType OnSurfaceGetPrimitiveType(int surface) { Index(surface); return PrimitiveType.Triangles; }
        protected override ArrayFormat OnSurfaceGetFormat(int surface) { Index(surface); return ArrayFormat.Vertex | ArrayFormat.Color | ArrayFormat.Index; }
        protected override Material? OnSurfaceGetMaterial(int surface) { Index(surface); return null; }
        protected override void OnSurfaceSetMaterial(int surface, Material? material) { Index(surface); throw new NotSupportedException(); }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat([new PropertyDescriptor<RestoredMesh, Vector2[]>("Vertices", r => (Vector2[])r.Vertices.Clone(), (r, v) => r.Vertices = (Vector2[])v.Clone(), _ => [], stored: true), new PropertyDescriptor<RestoredMesh, int[]>("Indices", r => (int[])r.Indices.Clone(), (r, v) => r.Indices = (int[])v.Clone(), _ => [], stored: true)]);
        protected override Resource CreateDuplicateInstance() => new RestoredMesh();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> copy, Func<Resource?, Resource?> force) { var r = (RestoredMesh)target; r.Vertices = (Vector2[])Vertices.Clone(); r.Indices = (int[])Indices.Clone(); }
    }
    private static RestoredMesh CreateMesh() => new();
    internal static void Run()
    {
        ResourceFileTypes.RegisterResource(PolicyID, CreatePolicy);
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-missing-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try { ResourceLoader.SetAbortOnMissingResources(false); Authoring(); Graphs(directory); Malformed(directory); External(directory); ThreadPolicy(directory); LocalScenes(directory); Legacy(directory); Fresh(directory); Console.WriteLine("Missing resources: typed fields, role-aware relocation, aliases/cycles, recovery, legacy graphs, thread policy and mesh bounds passed."); }
        finally { ResourceLoader.SetAbortOnMissingResources(true); Directory.Delete(directory, true); }
    }
    private static MissingResource Graph()
    {
        var r = new MissingResource { OriginalClass = GraphID, RecordingProperties = true, ResourceLocalToScene = true };
        r.SetRecordedProperty("Number", 71); r.SetRecordedProperty<Resource?>("Link", r); r.SetRecordedProperty<Resource?[]?>("Many", [r, null, r]); r.RecordingProperties = false; return r;
    }
    private static void Authoring()
    {
        using var r = new MissingResource(); Check(!r.RecordingProperties && r.OriginalClass == "", "Defaults."); Reject<InvalidOperationException>(() => r.SetRecordedProperty("Number", 1)); r.RecordingProperties = true; var bytes = new byte[] { 1, 2 }; r.SetRecordedProperty("Bytes", bytes); bytes[0] = 8; Check(r.GetRecordedProperty<byte[]>("Bytes")[0] == 1, "Input arrays are encoded snapshots."); r.RecordingProperties = false; r.SetRecordedProperty("Bytes", new byte[] { 3 }); Reject<InvalidOperationException>(() => r.SetRecordedProperty("Added", 1)); Reject<InvalidOperationException>(() => r.SetRecordedProperty("Bytes", 2)); Check(r.GetRecordedProperty<byte[]>("Bytes").SequenceEqual(new byte[] { 3 }), "Failed edits preserve old field."); Reject<InvalidOperationException>(() => r.GetRecordedProperty<int>("Bytes"));
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshLODTests.Data()); using var placeholder = mesh.CreatePlaceholder(); Check(placeholder.AABB == mesh.GetAABB() && placeholder.GetSurfaceCount() == 0 && placeholder.GetFaces().Length == 0, "Real placeholder copies bounds and draws no fabricated faces."); var rid = placeholder.GetRID(); placeholder.AABB = new(2, 3, 40, 20); Check(placeholder.GetRID() == rid && placeholder.GetAABB().Position == new Vector2(2, 3), "Bounds edits retain logical mesh identity."); Reject<ArgumentException>(() => placeholder.AABB = new(0, 0, -1, 2)); Reject<ArgumentException>(() => placeholder.AABB = new(float.NaN, 0, 1, 2)); Reject<ArgumentOutOfRangeException>(() => placeholder.SurfaceGetArrays(0)); using var copy = (PlaceholderMesh)placeholder.Duplicate(); Check(copy.AABB == placeholder.AABB, "Placeholder copies.");
        r.Dispose(); Reject<ObjectDisposedException>(() => r.GetRecordedProperty<byte[]>("Bytes"));
    }
    private static void Graphs(string directory)
    {
        foreach (var flags in new[] { SaverFlags.None, SaverFlags.Compress | SaverFlags.SaveBigEndian, SaverFlags.BundleResources })
        {
            using var source = Graph(); var path = System.IO.Path.Combine(directory, "graph-" + (int)flags + ".e2dres"); ResourceSaver.Save(source, path, flags);
            ResourceLoader.SetAbortOnMissingResources(true); Reject<InvalidDataException>(() => ResourceLoader.Load<MissingResource>(path, ResourceLoader.CacheMode.Ignore)); Check(!ResourceLoader.HasCached(path), "Strict failure publishes no cache."); ResourceLoader.SetAbortOnMissingResources(false);
            using var loaded = ResourceLoader.Load<MissingResource>(path); Check(loaded.OriginalClass == GraphID && loaded.GetRecordedProperty<int>("Number") == 71 && ReferenceEquals(loaded.GetRecordedProperty<Resource?>("Link"), loaded), "Role-aware self cycle."); var many = loaded.GetRecordedProperty<Resource?[]?>("Many")!; Check(many.Length == 3 && many[0] == loaded && many[1] is null && many[2] == loaded, "Resource-array aliases."); many[0] = null; Check(loaded.GetRecordedProperty<Resource?[]?>("Many")![0] == loaded, "Copied array query.");
            using var deep = (MissingResource)loaded.DuplicateDeep(); Check(deep.GetRecordedProperty<Resource?>("Link") == deep && deep.GetRecordedProperty<Resource?[]?>("Many")![2] == deep, "Deep copy redirects opaque self aliases."); using var shallow = (MissingResource)loaded.Duplicate(); Check(shallow.GetRecordedProperty<Resource?>("Link") == shallow, "Shallow root self-alias follows the common duplicate identity session.");
            source.SetRecordedProperty("Number", 99); ResourceSaver.Save(source, path, flags); Check(ReferenceEquals(ResourceLoader.Load<MissingResource>(path, ResourceLoader.CacheMode.Replace), loaded) && loaded.GetRecordedProperty<int>("Number") == 99 && loaded.GetRecordedProperty<Resource?>("Link") == loaded, "Replace retains identity and remaps opaque cycles.");
            var valid = File.ReadAllBytes(path); valid[^1] ^= 1; File.WriteAllBytes(path, valid); Reject<InvalidDataException>(() => ResourceLoader.Load<MissingResource>(path, ResourceLoader.CacheMode.Replace)); Check(loaded.GetRecordedProperty<int>("Number") == 99, "Corruption cannot replace a cached resource.");
        }
        using var root = Graph(); using var child = Graph(); root.SetRecordedProperty<Resource?>("Link", child); child.SetRecordedProperty<Resource?>("Link", root); root.SetRecordedProperty<Resource?[]?>("Many", [child, child]); var graphPath = System.IO.Path.Combine(directory, "reroot.e2dres"); ResourceSaver.Save(root, graphPath, SaverFlags.BundleResources); using var graph = ResourceLoader.Load<MissingResource>(graphPath, ResourceLoader.CacheMode.Ignore); var nested = (MissingResource)graph.GetRecordedProperty<Resource?>("Link")!; var reroot = System.IO.Path.Combine(directory, "nested.e2dres"); ResourceSaver.Save(nested, reroot, SaverFlags.BundleResources | SaverFlags.SaveBigEndian); using var newRoot = ResourceLoader.Load<MissingResource>(reroot, ResourceLoader.CacheMode.Ignore); Check(((MissingResource)newRoot.GetRecordedProperty<Resource?>("Link")!).GetRecordedProperty<Resource?>("Link") == newRoot, "Saving an opaque subresource as root relocates resource IDs.");
        using var plain = new PlaceholderMesh { AABB = new(1, 2, 10, 20) }; var meshPath = System.IO.Path.Combine(directory, "placeholder.e2dres"); ResourceSaver.Save(plain, meshPath); using var mesh = ResourceLoader.Load<PlaceholderMesh>(meshPath, ResourceLoader.CacheMode.Ignore); Check(mesh.AABB == plain.AABB, "Ordinary placeholder bounds storage.");
    }
    private static void Malformed(string directory)
    {
        using var source = Graph(); var path = System.IO.Path.Combine(directory, "malformed.e2dres"); ResourceSaver.Save(source, path); var original = File.ReadAllBytes(path); var payload = ResourceArchive.ReadPayload(path, out _);
        using var stream = new StreamPeerBuffer { DataArray = payload }; Check(stream.Get32() == 1, "Single root fixture."); _ = ResourceArchiveStrings.Read(stream); Check(stream.GetU8() == 0, "Generic role fixture."); _ = ResourceArchiveStrings.Read(stream); _ = stream.GetU8(); _ = ResourceArchiveStrings.Read(stream); _ = ResourceArchiveStrings.Read(stream); _ = stream.Get64(); _ = stream.Get32(); _ = stream.Get32(); _ = ResourceArchiveStrings.Read(stream); _ = ResourceArchiveStrings.Read(stream); var roleOffset = stream.GetPosition();
        foreach (var change in new Action<byte[]>[] { b => b[roleOffset] = 255, b => b[roleOffset + 1] = 2, b => BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(roleOffset + 2), 5), b => BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(roleOffset + 2), int.MaxValue) })
        {
            var broken = (byte[])payload.Clone(); change(broken); var file = (byte[])original.Clone(); broken.CopyTo(file, 64); SHA256.HashData(broken).CopyTo(file, 32); File.WriteAllBytes(path, file); Reject<InvalidDataException>(() => ResourceLoader.Load<MissingResource>(path, ResourceLoader.CacheMode.Ignore)); Check(!ResourceLoader.HasCached(path), "Malformed metadata/known scalar never publishes.");
        }
        using var raw = new MissingResource { OriginalClass = "tests.AbsentCodec", RecordingProperties = true }; raw.Data.Put(new("Opaque", "uninstalled.scalar", ResourceFieldRole.Scalar, true, [0x12, 0x34, 0x56], [], false)); raw.RecordingProperties = false; var rawPath = System.IO.Path.Combine(directory, "opaque.e2dres"); ResourceSaver.Save(raw, rawPath, SaverFlags.SaveBigEndian); using var loaded = ResourceLoader.Load<MissingResource>(rawPath, ResourceLoader.CacheMode.Ignore); Check(loaded.Data.Fields["Opaque"].BigEndian && loaded.Data.Fields["Opaque"].Bytes.SequenceEqual(new byte[] { 0x12, 0x34, 0x56 }), "Absent scalar codec bytes retained exactly."); var again = System.IO.Path.Combine(directory, "opaque-again.e2dres"); ResourceSaver.Save(loaded, again); using var copied = ResourceLoader.Load<MissingResource>(again, ResourceLoader.CacheMode.Ignore); Check(copied.Data.Fields["Opaque"].BigEndian && copied.Data.Fields["Opaque"].Bytes.SequenceEqual(loaded.Data.Fields["Opaque"].Bytes), "Opaque byte order remains explicit after changing archive order.");
        var descriptor = (PropertyDescriptor<MissingResource, byte[]>)source.GetPropertyList().Single(p => p.Name == "Number"); Reject<EndOfStreamException>(() => descriptor.SetValue(source, [1, 2, 3])); Check(source.GetRecordedProperty<int>("Number") == 71, "Bad scalar patch preserves prior field.");
        using var nulls = new MissingResource { RecordingProperties = true }; nulls.SetRecordedProperty<Resource?[]?>("Null", null); Check(nulls.GetRecordedProperty<Resource?[]?>("Null") is null, "Null array identity is separate from an empty array.");
    }
    private static void External(string directory)
    {
        var dependency = System.IO.Path.Combine(directory, "gone.e2dres"); var path = System.IO.Path.Combine(directory, "external.e2dres"); using var child = Graph(); ResourceSaver.Save(child, dependency); child.ResourcePath = dependency; using var root = new Restored { Number = 6, Link = child, Many = [child, child] }; ResourceSaver.Save(root, path); child.Dispose(); File.Delete(dependency);
        using var missing = ResourceLoader.Load<Restored>(path, ResourceLoader.CacheMode.Ignore); Check(missing.Link is MissingResource r && r.OriginalClass == GraphID && missing.Link == missing.Many![0] && missing.Many[0] == missing.Many[1], "Absent external dependency retains original identity and aliases.");
        ResourceLoader.SetAbortOnMissingResources(true); Reject<InvalidDataException>(() => ResourceLoader.Load<Restored>(path, ResourceLoader.CacheMode.Ignore)); ResourceLoader.SetAbortOnMissingResources(false);
        ResourceLoader.LoadThreadedRequest<Restored>(path, cacheMode: ResourceLoader.CacheMode.IgnoreDeep); using var threaded = ResourceLoader.LoadThreadedGet<Restored>(path); Check(threaded.Link is MissingResource && threaded.Many![0] == threaded.Link, "Threaded missing external dependency has owned prepared placeholder.");
    }
    private static void ThreadPolicy(string directory)
    {
        var path = System.IO.Path.Combine(directory, "policy.e2dres"); using var child = Graph(); using var root = new Restored { Number = 17, Link = child }; ResourceSaver.Save(root, path, SaverFlags.BundleResources);
        Entered.Reset(); Released.Reset(); BlockFactory = true; ResourceLoader.LoadThreadedRequest<Restored>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
        try { Check(Entered.Wait(TimeSpan.FromSeconds(15)), "Prepared factory entered."); ResourceLoader.SetAbortOnMissingResources(true); Reject<InvalidOperationException>(() => ResourceLoader.LoadThreadedRequest<Restored>(path, cacheMode: ResourceLoader.CacheMode.Ignore)); }
        finally { Released.Set(); BlockFactory = false; }
        using var loaded = ResourceLoader.LoadThreadedGet<Restored>(path); Check(loaded.Number == 17 && loaded.Link is MissingResource, "Thread request captures missing policy before worker dispatch."); ResourceLoader.SetAbortOnMissingResources(false);
    }
    private static void LocalScenes(string directory)
    {
        using var root = new Entity { Name = "Local" }; using var mesh = new PlaceholderMesh { ResourceLocalToScene = true, AABB = new(0, 0, 32, 32) }; using var graph = Graph(); graph.SetRecordedProperty<Resource?>("Link", mesh); mesh.Data.OriginalClass = "tests.LocalMissingMesh"; mesh.Data.Recording = true; mesh.Data.Set<Resource?>("Graph", graph); mesh.Data.Recording = false;
        var a = new MeshInstance { Name = "A", Mesh = mesh }; var b = new MeshInstance { Name = "B", Mesh = mesh }; root.AddChild(a); root.AddChild(b); a.Owner = root; b.Owner = root; using var scene = new PackedScene(); scene.Pack(root);
        var path = System.IO.Path.Combine(directory, "local.e2dscene"); ResourceSaver.Save(scene, path, SaverFlags.BundleResources); using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var first = loaded.Instantiate(); using var second = loaded.Instantiate(); var one = (PlaceholderMesh)((MeshInstance)first.GetChild(0)).Mesh!; var alias = ((MeshInstance)first.GetChild(1)).Mesh; var two = (PlaceholderMesh)((MeshInstance)second.GetChild(0)).Mesh!; var nested = (MissingResource)one.Data.Get<Resource?>("Graph")!;
        Check(one == alias && one != two && nested.GetRecordedProperty<Resource?>("Link") == one && one.Data.Get<Resource?>("Graph") != two.Data.Get<Resource?>("Graph"), "Local missing mesh/generic resource cycles retain within-instance aliases and independent instances.");
    }
    private static void Legacy(string directory)
    {
        using var payload = new StreamPeerBuffer(); payload.Put32(1); ResourceArchiveStrings.Write(payload, PolicyID); ResourceArchiveStrings.Write(payload, "legacy"); payload.PutU8(0); ResourceArchiveStrings.Write(payload, ""); ResourceArchiveStrings.Write(payload, ""); payload.Put64(-1);
        using var body = new StreamPeerBuffer(); body.Put32(1); ResourceArchiveStrings.Write(body, "Number"); ResourceArchiveStrings.Write(body, "System.Int32"); body.Put32(4); body.Put32(5); var data = body.DataArray; payload.Put32(data.Length); payload.PutData(data); var bytes = payload.DataArray;
        var file = new byte[64 + bytes.Length]; "E2DASSET"u8.CopyTo(file); BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(8), 1); BinaryPrimitives.WriteInt64LittleEndian(file.AsSpan(16), 1); BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(24), bytes.Length); BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(28), bytes.Length); SHA256.HashData(bytes).CopyTo(file, 32); bytes.CopyTo(file, 64);
        var path = System.IO.Path.Combine(directory, "legacy.e2dres"); File.WriteAllBytes(path, file); using var loaded = ResourceLoader.Load<Restored>(path, ResourceLoader.CacheMode.Ignore); Check(loaded.Number == 5, "Version-1 known properties remain readable.");
    }
    internal static void MakeScene(string path)
    {
        using var root = new Entity { Name = "Root" }; using var ordinary = new ArrayMesh(); var vertices = new Vector2[] { new(0, 0), new(32, 0), new(32, 32), new(0, 32) }; int[] indices = [0, 1, 2, 0, 2, 3]; ordinary.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData { Vertices = vertices, Indices = indices, Colors = Enumerable.Repeat(Colors.Green, 4).ToArray() });
        using var missing = new PlaceholderMesh { AABB = new(0, 0, 32, 32) }; missing.Data.OriginalClass = MeshID; missing.Data.Recording = true; missing.Data.Set("Vertices", vertices); missing.Data.Set("Indices", indices); missing.Data.Recording = false; var a = new MeshInstance { Name = "Known", Mesh = ordinary, Position = new(4, 4) }; var b = new MeshInstance { Name = "Absent", Mesh = missing, Position = new(64, 4) }; root.AddChild(a); root.AddChild(b); a.Owner = root; b.Owner = root;
        using var scene = new PackedScene(); scene.Pack(root); ResourceSaver.Save(scene, path, SaverFlags.BundleResources);
    }
    private static void Fresh(string directory)
    {
        var path = System.IO.Path.Combine(directory, "fresh.e2dres"); using var root = Graph(); ResourceSaver.Save(root, path, SaverFlags.BundleResources | SaverFlags.Compress); MakeScene(path + ".e2dscene");
        Child("preserve", path); Child("restore", path);
    }
    internal static void Child(string mode, string path, bool native = false)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.Environment["ELECTRON2D_TEST_MISSING_CHILD"] = path; start.Environment["ELECTRON2D_TEST_MISSING_MODE"] = mode; start.Environment["ELECTRON2D_TEST_MISSING_RENDER"] = native ? "1" : "0";
        using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEnd(); var error = child.StandardError.ReadToEnd(); if (native) Console.Write(output); Check(child.WaitForExit(60000) && child.ExitCode == 0, "Fresh " + mode + " failed: " + output + error);
    }
    internal static void RunChild(string path, string mode, bool native)
    {
        ResourceLoader.SetAbortOnMissingResources(mode != "preserve");
        if (mode == "preserve")
        {
            using var resource = ResourceLoader.Load<MissingResource>(path, ResourceLoader.CacheMode.Ignore); Check(resource.GetRecordedProperty<int>("Number") == 71 && resource.GetRecordedProperty<Resource?>("Link") == resource, "Fresh missing graph."); ResourceSaver.Save(resource, path + ".saved.e2dres", SaverFlags.BundleResources | SaverFlags.SaveBigEndian | SaverFlags.Compress);
            using var scene = ResourceLoader.Load<PackedScene>(path + ".e2dscene", ResourceLoader.CacheMode.Ignore); using var root = scene.Instantiate(); var mesh = ((MeshInstance)root.GetChild(1)).Mesh; Check(mesh is PlaceholderMesh p && p.AABB.Size == new Vector2(32, 32), "Fresh scene retains missing mesh bounds."); ResourceSaver.Save(scene, path + ".saved.e2dscene", SaverFlags.BundleResources | SaverFlags.SaveBigEndian);
            if (native) MissingResourceNativeTests.Render(scene, false);
        }
        else
        {
            ResourceFileTypes.RegisterResource(GraphID, CreateRestored); ResourceFileTypes.RegisterResource(MeshID, CreateMesh);
            using var restored = ResourceLoader.Load<Restored>(path + ".saved.e2dres", ResourceLoader.CacheMode.Ignore); Check(restored.Number == 71 && restored.Link == restored && restored.Many![0] == restored && restored.Many[2] == restored, "Restored compiled factory recovers relocated properties/aliases.");
            using var scene = ResourceLoader.Load<PackedScene>(path + ".saved.e2dscene", ResourceLoader.CacheMode.Ignore); using var root = scene.Instantiate(); var mesh = ((MeshInstance)root.GetChild(1)).Mesh; Check(mesh is RestoredMesh && mesh.GetSurfaceCount() == 1 && MeshLODTests.Draw(mesh, Transform.Identity).Count == 6, "Restored custom mesh produces drawable geometry."); if (native) MissingResourceNativeTests.Render(scene, true);
        }
    }
}
