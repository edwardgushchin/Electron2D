using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;
using Electron2D;
using static TextureArrayTests;

internal static class ImporterMeshTests
{
    internal static MeshSurfaceData Grid(int cells = 16)
    {
        var n = (cells + 1) * (cells + 1); var d = new MeshSurfaceData { Vertices = new Vector2[n], UVs = new Vector2[n], UV2s = new Vector2[n], Normals = new Vector3[n], Tangents = new Vector4[n], Colors = new Color[n], Indices = new int[cells * cells * 6] };
        for (var y = 0; y <= cells; y++) for (var x = 0; x <= cells; x++) { var i = y * (cells + 1) + x; d.Vertices[i] = new(x * 2, y * 2); d.UVs[i] = d.UV2s[i] = new(x / (float)cells, y / (float)cells); d.Normals[i] = new(0, 0, -1); d.Tangents[i] = new(1, 0, 0, 1); d.Colors[i] = Colors.White; }
        var next = 0; for (var y = 0; y < cells; y++) for (var x = 0; x < cells; x++) { var a = y * (cells + 1) + x; foreach (var index in new[] { a, a + 1, a + cells + 2, a, a + cells + 2, a + cells + 1 }) d.Indices[next++] = index; }
        d.Custom0 = MeshCustomTests.Channel(MeshCustomFormat.RGBA8UNORM, new(.25f, .5f, .75f, 1), n); d.Custom1 = MeshCustomTests.Channel(MeshCustomFormat.RGBA8SNORM, new(-1, .5f, -2, 1), n); d.Custom2 = MeshCustomTests.Channel(MeshCustomFormat.RGHalf, new(.25f, .5f, 0, 1), n); d.Custom3 = MeshCustomTests.Channel(MeshCustomFormat.RGBAFloat, new(.25f, .5f, .75f, 1), n); return d;
    }
    internal static ImporterMesh Producer(bool shaped = false)
    {
        var r = new ImporterMesh(); var d = Grid(); if (shaped) r.AddBlendShape("move"); r.AddSurface(Mesh.PrimitiveType.Triangles, d, shaped ? [MeshMorphTests.Pose(d, new(4, 0))] : null, name: "grid", flags: Mesh.ArrayFormat.CompressAttributes); return r;
    }
    internal static void Run()
    {
        ResourceFileTypes.RegisterNode("tests.ImporterNode", CreateProducerNode); Raw(); Oracle(); Generation(); Compilation(); Storage(); Merging(); Edges(); SparseAndReflected(); LocalScenes(); Fresh(); Warm(); Console.WriteLine("ImporterMesh raw channels, atomic compile, true generated levels, storage, transformed merge and zero cached/replay allocation passed.");
    }
    private static void Raw()
    {
        using var r = new ImporterMesh(); Check(r.BlendShapeMode == BlendShapeMode.Normalized && r.GetMesh() is null, "Empty producer defaults."); r.AddBlendShape("a:b"); r.AddBlendShape("a_b"); Check(r.GetBlendShapeName(0) == "a_b" && r.GetBlendShapeName(1) == "a_b", "Names sanitize colons but preserve duplicate definitions."); var d = MeshFrameTests.Data(); d.Colors = Enumerable.Repeat(new Color(.1234567f, .2f, .3f, 1), 4).ToArray(); d.UVs[0] = new(.1234567f, .3456789f); r.AddSurface(Mesh.PrimitiveType.Triangles, d, [MeshMorphTests.Pose(d, new(1, 0)), MeshMorphTests.Pose(d, new(2, 0))], flags: Mesh.ArrayFormat.CompressAttributes); d.UVs[0] = Vector2.Zero; Check(r.GetSurfaceArrays(0).UVs[0].X == .1234567f && r.GetSurfaceArrays(0).Colors[0].R == .1234567f && r.GetSurfaceFormat(0) == Mesh.ArrayFormat.CompressAttributes, "Raw import copies and postpones precision loss, preserving exact flags."); using var mesh = r.GetMesh()!; Check(mesh.SurfaceGetArrays(0).UVs[0].X != .1234567f && mesh.GetBlendShapeName(1) != mesh.GetBlendShapeName(0), "Compilation applies canonical precision and ordinary name collisions."); Reject<InvalidOperationException>(() => r.AddBlendShape("late")); r.Clear(); Check(r.GetBlendShapeCount() == 0 && r.GetSurfaceCount() == 0 && !mesh.IsDisposed, "Clearing preserves caller-owned compiled resources.");
    }
    private static void Oracle()
    {
        using var stream = typeof(ImporterMeshTests).Assembly.GetManifestResourceStream("Electron2D.Mesh.ImporterOracle")!; using var json = JsonDocument.Parse(stream); var count = 0;
        foreach (var record in json.RootElement.GetProperty("records").EnumerateArray())
        {
            var d = Grid(record.GetProperty("cells").GetInt32()); using var r = new ImporterMesh(); var shaped = record.GetProperty("deformable").GetBoolean(); if (shaped) r.AddBlendShape("move"); r.AddSurface(Mesh.PrimitiveType.Triangles, d, shaped ? [MeshMorphTests.Pose(d, new(4, 0))] : null); r.GenerateLODs(60); var levels = record.GetProperty("levels"); Check(r.GetSurfaceLODCount(0) == levels.GetArrayLength(), "Independent native level count."); for (var i = 0; i < levels.GetArrayLength(); i++) Check(r.GetSurfaceLODIndices(0, i).SequenceEqual(levels[i].EnumerateArray().Select(v => v.GetInt32())), "Every generated index agrees with the independent pinned C++ loop."); count++;
        }
        Check(count == 6, "Three sizes and both deformation policies covered.");
    }
    private static void Generation()
    {
        using var r = Producer(true); var before = r.GetSurfaceArrays(0); var changed = 0; r.Changed += _ => changed++; r.GenerateLODs(60); Check(changed == 1 && r.GetSurfaceLODCount(0) > 0, "One publication follows actual generation.");
        var previous = 0f; var previousCount = before.Indices.Length; for (var i = 0; i < r.GetSurfaceLODCount(0); i++) { var indices = r.GetSurfaceLODIndices(0, i); var error = r.GetSurfaceLODSize(0, i); Check(error > previous && indices.Length < previousCount && indices.Length % 3 == 0 && indices.All(v => v >= 0 && v < before.Vertices.Length), "Generated thresholds grow while valid triangle levels shrink."); indices[0] = -1; Check(r.GetSurfaceLODIndices(0, i)[0] >= 0, "Level queries copy storage."); previous = error; previousCount = indices.Length; }
        Check(r.GetSurfaceArrays(0).UVs.SequenceEqual(before.UVs) && r.GetSurfaceArrays(0).Custom3!.Data.SequenceEqual(before.Custom3!.Data) && r.GetSurfaceBlendShapeArrays(0, 0).Vertices[0].X == 4, "Generation preserves raw attributes and target payloads.");
        using var side = new ImporterMesh(); side.AddSurface(Mesh.PrimitiveType.Triangles, Grid(8), name: "seam"); using var seam = ImporterMesh.MergeImporterMeshes([side, side], [Transform.Identity, new(0, new(16, 0))]); seam.GenerateLODs(60); var split = side.GetSurfaceArrays(0).Vertices.Length; Check(seam.GetSurfaceLODCount(0) > 0, "Seamed geometry still generates levels."); for (var lod = 0; lod < seam.GetSurfaceLODCount(0); lod++) { var order = seam.GetSurfaceLODIndices(0, lod); for (var i = 0; i < order.Length; i += 3) Check(order[i] < split && order[i + 1] < split && order[i + 2] < split || order[i] >= split && order[i + 1] >= split && order[i + 2] >= split, "Discontinuous UV wedges remain distinct through welding and simplification."); }
        using var mesh = r.GetMesh()!; Check(mesh.SurfaceGetLODs(0).Count == r.GetSurfaceLODCount(0) && MeshLODTests.Draw(mesh, Transform.Identity).Count < before.Indices.Length, "Generated levels are actual selected canvas geometry."); Reject<ArgumentOutOfRangeException>(() => r.GenerateLODs(float.NaN)); Check(r.GetSurfaceLODCount(0) > 0, "Invalid angle retains old levels.");
        using var skin = new ImporterMesh(); var d = Grid(); d.Bones = new int[d.Vertices.Length * 8]; d.Weights = new float[d.Bones.Length]; for (var i = 0; i < d.Vertices.Length; i++) { d.Bones[i * 8 + 7] = 7; d.Weights[i * 8 + 7] = 1; }
        skin.AddSurface(Mesh.PrimitiveType.Triangles, d, flags: Mesh.ArrayFormat.Use8BoneWeights); skin.GenerateLODs(60); var count = skin.GetSurfaceLODCount(0); Reject<ArgumentOutOfRangeException>(() => skin.GenerateLODs(60, [Transform.Identity])); Check(skin.GetSurfaceLODCount(0) == count, "Invalid late pose preserves previous levels."); skin.GenerateLODs(60, Enumerable.Repeat(Transform.Identity, 8).ToArray()); Check(skin.GetSurfaceLODCount(0) > 0, "Eight-slot pose feeds actual simplification.");
    }
    private static void Compilation()
    {
        using var r = Producer(); using var old = r.GetMesh()!; Check(ReferenceEquals(r.GetMesh(), old), "Stable cached identity."); r.SetSurfaceName(0, "new"); using var current = r.GetMesh()!; Check(!ReferenceEquals(old, current) && old.SurfaceGetName(0) == "grid" && current.SurfaceGetName(0) == "new", "Authoring invalidates the cache without mutating earlier snapshots."); using var target = new ArrayMesh(); target.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshFrameTests.Data()); r.ResetState(); var events = 0; target.Changed += _ => { events++; Check(target.GetSurfaceCount() == 2 && ReferenceEquals(r.GetMesh(), target), "Target callbacks see a complete cached publication."); throw new InvalidOperationException("listener"); }; Reject<InvalidOperationException>(() => r.GetMesh(target)); Check(events == 1 && target.GetSurfaceCount() == 2, "Listener failure preserves atomic appended geometry/cache.");
        using var copy = ImporterMesh.FromMesh(current); Check(copy.GetSurfaceCount() == 1 && copy.GetSurfaceName(0) == "new", "Copied compiled source metadata."); using var compiled = copy.GetMesh()!; r.Dispose(); Check(!compiled.IsDisposed && !old.IsDisposed, "Producer disposal retains returned mesh ownership.");
    }
    private static void Storage()
    {
        using var r = Producer(true); r.BlendShapeMode = BlendShapeMode.Relative; r.GenerateLODs(60); using var mat = new CanvasItemMaterial(); r.SetSurfaceMaterial(0, mat); r.AddSurface(Mesh.PrimitiveType.Triangles, r.GetSurfaceArrays(0), [r.GetSurfaceBlendShapeArrays(0, 0)], material: mat, flags: Mesh.ArrayFormat.CompressAttributes); using var duplicate = (ImporterMesh)r.Duplicate(); Check(duplicate.GetSurfaceMaterial(0) == mat && duplicate.GetSurfaceLODCount(0) == r.GetSurfaceLODCount(0), "Shallow duplicate preserves borrowed aliases and levels."); using var deep = (ImporterMesh)r.DuplicateDeep(); Check(deep.GetSurfaceMaterial(0) != mat && deep.GetSurfaceMaterial(0) == deep.GetSurfaceMaterial(1), "Deep duplicate preserves shared material alias.");
        var property = (PropertyDescriptor<ImporterMesh, byte[]>)r.GetPropertyList().Single(p => p.Name == "_data"); var bytes = property.GetValue(r); using var loaded = new ImporterMesh(); property.SetValue(loaded, bytes); Check(loaded.GetSurfaceArrays(0).UVs.SequenceEqual(r.GetSurfaceArrays(0).UVs) && loaded.GetSurfaceLODCount(0) == r.GetSurfaceLODCount(0), "Raw archive preserves original precision and levels."); Reject<InvalidDataException>(() => property.SetValue(loaded, bytes[..^1])); var broken = (byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(broken.AsSpan(8), int.MaxValue); Reject<InvalidDataException>(() => property.SetValue(loaded, broken)); Check(loaded.GetSurfaceCount() == 2, "Malformed archives preserve old producer state.");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "importer-" + Guid.NewGuid().ToString("N") + ".e2dres"); try { ResourceSaver.Save(r, path); using var resource = ResourceLoader.Load<ImporterMesh>(path, ResourceLoader.CacheMode.Ignore); Check(resource.BlendShapeMode == BlendShapeMode.Relative && resource.GetSurfaceMaterial(0) == resource.GetSurfaceMaterial(1), "Saved resource restores mode and material graph aliases."); using var mesh = resource.GetMesh()!; Check(mesh.GetBlendShapeCount() == 1 && mesh.GetSurfaceCount() == 2, "Loaded producer compiles actual meshes."); } finally { File.Delete(path); }
    }
    private static void Merging()
    {
        using var source = new ImporterMesh(); var d = MeshCustomTests.Data(); source.AddSurface(Mesh.PrimitiveType.Triangles, d, name: "same"); using var merged = ImporterMesh.MergeImporterMeshes([source, source], [Transform.Identity, new(0, new(20, 0))]); Check(merged.GetSurfaceCount() == 1 && merged.GetSurfaceArrays(0).Vertices.Length == 8 && merged.GetSurfaceArrays(0).Indices.Max() == 7, "Compatible named merge remaps indices."); using var separated = ImporterMesh.MergeImporterMeshes([source, source], [Transform.Identity, Transform.Identity], false); Check(separated.GetSurfaceCount() == 2 && separated.GetSurfaceName(1) == "same_2", "Disabled dedup uses collision suffixes.");
        using var reflected = ImporterMesh.MergeImporterMeshes([source], [new(new Vector2(-1, 0), new Vector2(0, 1), new(16, 0))]); var reflectedData = reflected.GetSurfaceArrays(0); Check(reflectedData.Indices[1] == 2 && reflectedData.Indices[2] == 1 && reflectedData.Custom3!.Data.SequenceEqual(d.Custom3!.Data), "Reflection rewinds indices and preserves raw payloads."); Reject<ArgumentException>(() => ImporterMesh.MergeImporterMeshes([source], [])); Check(source.GetSurfaceArrays(0).Vertices[0] == Vector2.Zero, "Merge inputs remain independent.");
    }
    private static void Edges()
    {
        foreach (var primitive in Enum.GetValues<Mesh.PrimitiveType>())
        {
            var d = MeshFrameTests.Data(); d.Indices = primitive == Mesh.PrimitiveType.Triangles ? d.Indices : primitive == Mesh.PrimitiveType.TriangleStrip ? [0, 1, 3, 2] : primitive == Mesh.PrimitiveType.Lines ? [0, 1, 2, 3] : [];
            using var r = new ImporterMesh(); r.AddSurface(primitive, d, name: "p"); r.GenerateLODs(60); Check(r.GetSurfaceLODCount(0) == 0, "Every topology remains executable without fabricated small-mesh levels."); using var merged = ImporterMesh.MergeImporterMeshes([r, r], [new(new Vector2(-1, 0), new Vector2(0, 1), new(16, 0)), new(0, new(40, 0))]); using var result = merged.GetMesh()!; var drawn = MeshLODTests.Draw(result, Transform.Identity); Check(drawn.Count > 0 && merged.GetSurfaceCount() == (primitive == Mesh.PrimitiveType.LineStrip ? 2 : 1), "All reflected/joined topologies draw with line strips kept separate.");
            if (primitive == Mesh.PrimitiveType.TriangleStrip) for (var i = 0; i < drawn.Count; i += 3) { var a = drawn[i].Position; var b = drawn[i + 1].Position; var c = drawn[i + 2].Position; if (Math.Abs((b - a).Cross(c - a)) > .00001f) Check(Math.Max(a.X, Math.Max(b.X, c.X)) <= 16 || Math.Min(a.X, Math.Min(b.X, c.X)) >= 40, "Joined strips have no nondegenerate bridge triangle."); }
        }
        using var r1 = new ImporterMesh(); var raw = new MeshSurfaceData { Vertices = [new(0, 0), new(1, 0), new(0, 1)], Indices = [0, 1, 2], Bones = new int[12], Weights = new float[12] }; raw.Bones[11] = 2; r1.AddSurface(Mesh.PrimitiveType.Triangles, raw); Reject<ArgumentOutOfRangeException>(() => r1.GenerateLODs(60, [Transform.Identity]));
        using var sequential = new ImporterMesh(); var d1 = Grid(); d1.Indices = []; d1.Vertices = [new(0, 0), new(1, 0), new(0, 1)]; d1.Normals = []; d1.Tangents = []; d1.UVs = []; d1.UV2s = []; d1.Colors = []; for (var c = 0; c < 4; c++) d1.SetCustom(c, null); sequential.AddSurface(Mesh.PrimitiveType.Triangles, d1, name: "mixed"); d1.Indices = [0, 1, 2]; using var indexed = new ImporterMesh(); indexed.AddSurface(Mesh.PrimitiveType.Triangles, d1, name: "mixed"); using var mixed = ImporterMesh.MergeImporterMeshes([sequential, indexed], [Transform.Identity, new(0, new(2, 0))]); Check(mixed.GetSurfaceArrays(0).Indices.SequenceEqual(new[] { 0, 1, 2, 3, 4, 5 }), "Mixed indexing preserves both geometries.");
        using var atomic = Producer(); var skin = Grid(); skin.Bones = new int[skin.Vertices.Length * 4]; skin.Weights = new float[skin.Bones.Length]; skin.Bones[^1] = 4; atomic.AddSurface(Mesh.PrimitiveType.Triangles, skin); atomic.GenerateLODs(60); var old = atomic.GetSurfaceLODIndices(0, 0); Reject<ArgumentOutOfRangeException>(() => atomic.GenerateLODs(10, [Transform.Identity])); Check(old.SequenceEqual(atomic.GetSurfaceLODIndices(0, 0)), "Late surface failure rolls back every generated level set.");
        r1.Dispose(); Reject<ObjectDisposedException>(() => r1.GetMesh());
    }
    private static void SparseAndReflected()
    {
        var sparse = Grid(8); sparse.Normals = []; sparse.Tangents = [];
        sparse.Vertices = [.. sparse.Vertices, new(100, 100)]; sparse.UVs = [.. sparse.UVs, Vector2.Zero]; sparse.UV2s = [.. sparse.UV2s, Vector2.Zero]; sparse.Colors = [.. sparse.Colors, Colors.White];
        for (var channel = 0; channel < 4; channel++) sparse.SetCustom(channel, null);
        using var raw = new ImporterMesh(); raw.AddSurface(Mesh.PrimitiveType.Triangles, sparse); raw.GenerateLODs(60);
        Check(raw.GetSurfaceLODCount(0) > 0, "Missing normals with unused vertices generate valid levels.");
        var data = new MeshSurfaceData { Vertices = [new(0, 0), new(2, 0), new(0, 2)], UVs = [new(0, 0), new(1, 0), new(0, 1)], Bones = Enumerable.Range(0, 24).ToArray(), Weights = Enumerable.Repeat(.125f, 24).ToArray(), Custom0 = new() { Format = MeshCustomFormat.RGBA8UNORM, Data = Enumerable.Range(0, 12).Select(x => (byte)x).ToArray() } };
        using var source = new ImporterMesh(); source.AddSurface(Mesh.PrimitiveType.Triangles, data, flags: Mesh.ArrayFormat.Use8BoneWeights);
        using var reflected = ImporterMesh.MergeImporterMeshes([source], [new(new Vector2(-1, 0), new Vector2(0, 1), Vector2.Zero)]);
        var copy = reflected.GetSurfaceArrays(0);
        Check(copy.Bones.AsSpan(8, 8).SequenceEqual(data.Bones.AsSpan(16, 8)) && copy.UVs[1] == data.UVs[2] && copy.Custom0!.Data.AsSpan(4, 4).SequenceEqual(data.Custom0!.Data.AsSpan(8, 4)), "Unindexed reflection moves complete eight-slot/custom/UV vertex records.");
        using var mesh = reflected.GetMesh()!; Check(MeshLODTests.Draw(mesh, Transform.Identity).Count == 3, "Reflected skin records compile and draw.");
        using var material = new CanvasItemMaterial(); using var other = new CanvasItemMaterial(); using var first = new ImporterMesh(); first.AddSurface(Mesh.PrimitiveType.Triangles, MeshFrameTests.Data(), material: material, name: "same"); using var second = new ImporterMesh(); second.AddSurface(Mesh.PrimitiveType.Triangles, MeshFrameTests.Data(), material: other, name: "same");
        using var joined = ImporterMesh.MergeImporterMeshes([first, second], [Transform.Identity, Transform.Identity]); Check(joined.GetSurfaceMaterial(0) == material, "Joined names deliberately retain the first borrowed material.");
    }
    private sealed class ProducerNode : Node
    {
        internal ImporterMesh? Producer;
        protected override Func<Node> CreateSceneInstanceFactory() => CreateProducerNode;
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat([new PropertyDescriptor<ProducerNode, ImporterMesh?>("Producer", n => n.Producer, (n, v) => n.Producer = v, _ => null, stored: true)]);
    }
    private static ProducerNode CreateProducerNode() => new();
    private static void LocalScenes()
    {
        using var r = Producer(); r.ResourceLocalToScene = true; using var root = new ProducerNode { Name = "Root", Producer = r }; var child = new ProducerNode { Name = "Child", Producer = r }; root.AddChild(child); child.Owner = root; using var scene = new PackedScene(); scene.Pack(root); using var a = (ProducerNode)scene.Instantiate(); using var b = (ProducerNode)scene.Instantiate(); Check(a.Producer != r && a.Producer != b.Producer && a.Producer == ((ProducerNode)a.GetNode("Child")).Producer, "Local importer copies preserve aliases inside independent scene instances.");
    }
    private static void Fresh()
    {
        using var r = Producer(true); r.BlendShapeMode = BlendShapeMode.Relative; r.GenerateLODs(60); using var root = new ProducerNode { Name = "Root", Producer = r }; var child = new ProducerNode { Name = "Child", Producer = r }; root.AddChild(child); child.Owner = root; using var scene = new PackedScene(); scene.Pack(root); var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "importer-scene-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try { ResourceSaver.Save(scene, path); var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(ImporterMeshTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_IMPORTER_MESH"); start.Environment.Remove("ELECTRON2D_TEST_IMPORTER_MESH_NATIVE"); start.Environment["ELECTRON2D_TEST_IMPORTER_MESH_CHILD"] = path; using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync(); if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Importer fresh scene."); } Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh importer scene passed"), error.GetAwaiter().GetResult()); } finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        ResourceFileTypes.RegisterNode("tests.ImporterNode", CreateProducerNode); using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var root = (ProducerNode)scene.Instantiate(); var r = root.Producer!; Check(r == ((ProducerNode)root.GetNode("Child")).Producer && r.BlendShapeMode == BlendShapeMode.Relative && r.GetSurfaceLODCount(0) > 0, "Fresh scene producer state and aliases."); r.GenerateLODs(60); using var mesh = r.GetMesh()!; Check(MeshLODTests.Draw(mesh, Transform.Identity).Count < mesh.SurfaceGetArrayIndexLen(0), "Fresh native simplification feeds actual managed canvas replay."); Console.WriteLine("Fresh importer scene passed.");
    }
    private static void Warm()
    {
        using var r = Producer(); r.GenerateLODs(60); using var mesh = r.GetMesh()!; for (var i = 0; i < 24; i++) Check(r.GetMesh() == mesh, "Warm cache."); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) if (r.GetMesh() != mesh) throw new InvalidOperationException("Cached identity"); Check(before == GC.GetAllocatedBytesForCurrentThread(), "64 warmed cached queries allocate zero managed bytes.");
    }
}
