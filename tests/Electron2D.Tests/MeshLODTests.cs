using System.Buffers.Binary;
using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;

internal static class MeshLODTests
{
    internal static MeshSurfaceData Data() => new()
    {
        Vertices = [new(0, 0), new(32, 0), new(32, 32), new(0, 32), new(40, 0), new(72, 0), new(72, 32), new(40, 32)],
        Indices = [0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7],
        Colors = Enumerable.Repeat(Colors.White, 8).ToArray(),
        UVs = Enumerable.Repeat(Vector2.Zero, 8).ToArray(),
        Normals = Enumerable.Repeat(new Vector3(0, 0, 1), 8).ToArray(),
        Tangents = Enumerable.Repeat(new Vector4(1, 0, 0, 1), 8).ToArray()
    };
    internal static Dictionary<float, int[]> Levels() => new() { [4] = [0, 1, 2], [2] = [0, 1, 2, 0, 2, 3] };
    internal static ArrayMesh Mesh(bool morph = false)
    {
        var mesh = new ArrayMesh(); var data = Data(); if (morph) mesh.AddBlendShape("move");
        mesh.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, data, morph ? [MeshMorphTests.Pose(data, new(4, 0))] : null, Levels()); return mesh;
    }
    internal static List<CanvasVertex> Draw(Electron2D.Mesh mesh, Transform transform, MultiMesh? instances = null)
    {
        var vertices = new List<CanvasVertex>(); new CanvasMesh(mesh, Transform.Identity, Colors.White).Append(vertices, [], null, transform, Colors.White, null, BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false, instances); return vertices;
    }
    internal static void Run()
    {
        Authoring(); Selection(); Callbacks(); Storage(); Fresh(); Warm();
        Console.WriteLine("LOD API: copied validated levels, full-transform/per-instance selection, morph/skin storage, legacy/fresh archives and zero warmed replay passed.");
    }
    private static void Authoring()
    {
        using var mesh = Mesh(); var levels = mesh.SurfaceGetLODs(0); Check(levels.Keys.SequenceEqual(new[] { 2f, 4f }), "LOD keys are ascending."); levels[2][0] = 7; Check(mesh.SurfaceGetLODs(0)[2][0] == 0, "Queries copy every index set."); Reject<ArgumentOutOfRangeException>(() => mesh.SurfaceGetLODs(-1));
        using var target = new ArrayMesh(); var source = Levels(); target.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, Data(), lods: source); source[2][0] = 7; Check(target.SurfaceGetLODs(0)[2][0] == 0, "Import copies arrays.");
        foreach (var key in new[] { 0, -1, float.NaN, float.PositiveInfinity }) Reject<ArgumentException>(() => target.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, Data(), lods: new Dictionary<float, int[]> { [key] = [0, 1, 2] }));
        foreach (var order in new int[][] { [], [0, 1], Data().Indices }) Reject<ArgumentException>(() => target.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, Data(), lods: new Dictionary<float, int[]> { [2] = order }));
        Reject<ArgumentOutOfRangeException>(() => target.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, Data(), lods: new Dictionary<float, int[]> { [2] = [0, 1, 8] })); var sequential = Data(); sequential.Indices = []; Reject<ArgumentException>(() => target.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, sequential, lods: Levels())); Check(target.GetSurfaceCount() == 1, "Invalid levels leave prior surfaces intact.");
        for (var primitive = 0; primitive < 5; primitive++)
        {
            var data = Data(); data.Indices = primitive == 1 ? [0, 1, 2, 3] : [0, 1, 2, 3, 4, 5]; var indices = primitive switch { 1 => new[] { 0, 1 }, 3 => new[] { 0, 1, 2 }, 4 => new[] { 0, 1, 2 }, _ => new[] { 0, 1 } };
            using var topology = new ArrayMesh(); topology.AddSurfaceFromArrays((Electron2D.Mesh.PrimitiveType)primitive, data, lods: new Dictionary<float, int[]> { [1] = indices }); Check(Draw(topology, Transform.Identity).Count > 0, "All five LOD topologies draw.");
        }
        mesh.RegenNormalMaps(); Check(mesh.SurfaceGetLODs(0)[2].Length == 6, "Tangent regeneration retains levels."); using var copy = (ArrayMesh)mesh.Duplicate(); Check(copy.SurfaceGetLODs(0)[4].Length == 3, "Duplication retains independent levels.");
    }
    private static void Selection()
    {
        using var mesh = Mesh(); Transform Scale(float value) => new(new Vector2(value, 0), new Vector2(0, value), Vector2.Zero);
        Check(Draw(mesh, Transform.Identity).Count == 12 && Draw(mesh, Scale(.5f)).Count == 6 && Draw(mesh, Scale(.25f)).Count == 3, "Base/fine/coarse thresholds execute."); Check(Draw(mesh, Scale(MathF.BitIncrement(.5f))).Count == 12 && Draw(mesh, Scale(-.25f)).Count == 3, "Strict threshold and reflection."); Check(Draw(mesh, new(new Vector2(.1f, 0), new Vector2(0, 1), Vector2.Zero)).Count == 12, "Anisotropic scale uses greatest axis."); Check(Draw(mesh, new(new Vector2(.4f, 0), new Vector2(.4f, .4f), Vector2.Zero)).Count == 12, "Shear uses singular value, not maximum column length."); Check(Draw(mesh, new(Vector2.Zero, Vector2.Zero, Vector2.Zero)).Count == 3, "Singular collapse remains finite.");
        using var multi = new MultiMesh { Mesh = mesh, InstanceCount = 2 }; multi.SetInstanceTransform2D(0, Scale(.25f)); multi.SetInstanceTransform2D(1, Transform.Identity); Check(Draw(mesh, Transform.Identity, multi).Count == 15, "Each instance selects its own level."); Check(mesh.GetFaces().Length == 12 && mesh.GetAABB().Size.X == 72, "Bounds and base face queries retain full geometry.");
        using var shaped = Mesh(true); using var root = new Node(); var node = new MeshInstance { Mesh = shaped, Scale = new(.5f, .5f) }; root.AddChild(node); using var tree = new SceneTree(root); node.SetBlendShapeValue(0, 1); node.PrepareCanvas(); var vertices = new List<CanvasVertex>(); node.AppendCanvas(vertices, [], node.GlobalTransform); Check(vertices.Count == 6 && vertices[0].Position.X == 2, "Instance-owned morph output retains LOD and deforms before selection drawing.");
    }
    private sealed class Custom : Electron2D.Mesh
    {
        internal Action? Callback;
        internal bool Invalid;
        protected override int OnGetSurfaceCount() => 1;
        protected override MeshSurfaceData OnSurfaceGetArrays(int index) => Data();
        protected override int OnSurfaceGetArrayLen(int index) => 8;
        protected override int OnSurfaceGetArrayIndexLen(int index) => 12;
        protected override PrimitiveType OnSurfaceGetPrimitiveType(int index) => PrimitiveType.Triangles;
        protected override ArrayFormat OnSurfaceGetFormat(int index) => ArrayFormat.Vertex | ArrayFormat.Index;
        protected override Material? OnSurfaceGetMaterial(int index) => null;
        protected override void OnSurfaceSetMaterial(int index, Material? material) { }
        protected override IReadOnlyDictionary<float, int[]> OnSurfaceGetLODs(int index) { Callback?.Invoke(); return Invalid ? new Dictionary<float, int[]> { [2] = [0, 1, 99] } : Levels(); }
    }
    private static void Callbacks()
    {
        using var source = new Custom(); var draw = new CanvasMesh(source, Transform.Identity, Colors.White); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        void Run() { vertices.Clear(); batches.Clear(); draw.Append(vertices, batches, null, new(new Vector2(.5f, 0), new Vector2(0, .5f), Vector2.Zero), Colors.White, null, BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false); }
        source.Callback = source.EmitChanged; Reject<InvalidOperationException>(Run); source.Callback = null; Run(); Check(vertices.Count == 6, "Changed callback snapshots reject and retry."); source.Invalid = true; source.EmitChanged(); Reject<ArgumentOutOfRangeException>(Run); source.Invalid = false; Run(); source.Callback = Run; source.EmitChanged(); Reject<InvalidOperationException>(Run); source.Callback = null; Run();
    }
    private static void Storage()
    {
        using var mesh = Mesh(true); var property = (PropertyDescriptor<ArrayMesh, byte[]>)mesh.GetPropertyList().Single(p => p.Name == "_surface_data"); var bytes = property.GetValue(mesh); Check(BinaryPrimitives.ReadUInt32LittleEndian(bytes) == 0x37534d41, "AMS7 version."); using var target = new ArrayMesh(); property.SetValue(target, bytes); Check(target.SurfaceGetLODs(0)[4].Length == 3 && target.GetBlendShapeCount() == 1, "Shapes and LODs roundtrip together.");
        var truncated = bytes[..^1]; Reject<InvalidDataException>(() => property.SetValue(target, truncated)); var bad = (byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(bad.Length - 4), 99); Reject<InvalidDataException>(() => property.SetValue(target, bad)); Check(target.SurfaceGetLODs(0)[4].Length == 3, "Malformed indices retain old storage.");
        foreach (var error in new[] { float.NaN, 0f, 2f }) { bad = (byte[])bytes.Clone(); BinaryPrimitives.WriteSingleLittleEndian(bad.AsSpan(bad.Length - 20), error); Reject<InvalidDataException>(() => property.SetValue(target, bad)); }
        bad = (byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(bad.Length - 16), int.MaxValue); Reject<InvalidDataException>(() => property.SetValue(target, bad));
        using var noLOD = MeshMorphTests.Mesh(); var current = property.GetValue(noLOD); var old = new byte[current.Length - 20]; current.AsSpan(0, 112).CopyTo(old); current.AsSpan(132).CopyTo(old.AsSpan(112)); BinaryPrimitives.WriteUInt32LittleEndian(old, 0x35534d41); using var legacy = new ArrayMesh(); property.SetValue(legacy, old); Check(legacy.GetBlendShapeCount() == 1 && legacy.SurfaceGetLODs(0).Count == 0, "AMS5 shaped archives remain readable.");
    }
    private static void Fresh()
    {
        using var mesh = Mesh(); using var root = new Node(); var node = new MeshInstance { Name = "LOD", Mesh = mesh, Scale = new(.25f, .25f) }; root.AddChild(node); node.Owner = root; using var scene = new PackedScene(); scene.Pack(root); var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-lod-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try { ResourceSaver.Save(scene, path); var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(MeshLODTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_MESH_LOD"); start.Environment.Remove("ELECTRON2D_TEST_MESH_LOD_NATIVE"); start.Environment["ELECTRON2D_TEST_MESH_LOD_CHILD"] = path; using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("LOD scene child."); } Check(child.ExitCode == 0 && stdout.GetAwaiter().GetResult().Contains("Fresh LOD scene passed"), stderr.GetAwaiter().GetResult()); } finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var root = scene.Instantiate(); var node = (MeshInstance)root.GetNode("LOD"); using var tree = new SceneTree(root); node.PrepareCanvas(); var vertices = new List<CanvasVertex>(); node.AppendCanvas(vertices, [], node.GlobalTransform); Check(vertices.Count == 3 && node.Mesh!.SurfaceGetLODs(0).Count == 2, "Fresh authored scene selects saved levels."); Console.WriteLine("Fresh LOD scene passed.");
    }
    private static void Warm()
    {
        using var mesh = Mesh(); var draw = new CanvasMesh(mesh, Transform.Identity, Colors.White); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); var patch = new byte[8];
        void Step(int i) { var scale = i % 3 == 0 ? 1 : i % 3 == 1 ? .5f : .25f; BinaryPrimitives.WriteSingleLittleEndian(patch, i % 2); mesh.SurfaceUpdateVertexRegion(0, 0, patch); vertices.Clear(); batches.Clear(); draw.Append(vertices, batches, null, new(new Vector2(scale, 0), new Vector2(0, scale), Vector2.Zero), Colors.White, null, BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false); }
        for (var i = 0; i < 24; i++) Step(i); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Step(i); Check(before == GC.GetAllocatedBytesForCurrentThread(), "Warmed region edits and changing LOD replay allocate zero managed bytes."); Check(vertices[0].Position.X == 1, "Alternate views observe live packed positions.");
    }
}
