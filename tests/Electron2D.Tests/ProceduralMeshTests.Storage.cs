using System.Buffers.Binary;
using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;

internal static partial class ProceduralMeshTests
{
    private static void Producers()
    {
        using var producer = new ImporterMesh(); producer.AddBlendShape("directions"); var data = Data(); data.Normals = Enumerable.Repeat(new Vector3(0, 0, 1), 4).ToArray();
        var shape = new MeshSurfaceData { VertexCount = 4, Normals = Enumerable.Repeat(new Vector3(0, 1, 0), 4).ToArray() };
        producer.AddSurface(Mesh.PrimitiveType.Triangles, data, [shape], new Dictionary<float, int[]> { [2] = [0, 1, 2] }, name: "generated", flags: Empty);
        producer.GenerateLODs(60); Check(producer.GetSurfaceLODCount(0) == 1 && producer.GetSurfaceLODIndices(0, 0).SequenceEqual(new[] { 0, 1, 2 }), "Generation preserves authored procedural LODs without attempting geometric simplification.");
        using var mesh = producer.GetMesh()!; using var pose = new MeshMorphPose(); var output = pose.Prepare(mesh, [.5f]); Check(output.SurfaceGetArrayLen(0) == 4 && output.SurfaceGetArrays(0).Vertices.Length == 0 && output.SurfaceGetArrays(0).Normals.Length == 4 && output.SurfaceGetLODs(0).Count == 1, "Direction targets retain numerical frame data, logical counts and levels without position targets.");
        using var copied = ImporterMesh.FromMesh(mesh); using var compiled = copied.GetMesh()!; Check(compiled.SurfaceGetArrayLen(0) == 4 && compiled.SurfaceGetArrays(0).Vertices.Length == 0, "Compiled-to-importer-to-mesh preserves the procedural schema.");
        var packed = mesh.ExportPacked(0); Check(packed.BlendShapeData.Length == 16 && packed.BlendShapeFormat == Mesh.ArrayFormat.Normal, "Packed targets omit both position bytes and synthesized base tangents."); using var packedMesh = new ArrayMesh(); packedMesh.AddBlendShape("directions"); packedMesh.AppendPacked(ArrayMesh.PreparePacked(packed, 1)); Check(packedMesh.SurfaceGetBlendShapeArrays(0)[0].Normals.Length == 4 && packedMesh.SurfaceGetBlendShapeArrays(0)[0].Tangents.Length == 0, "Packed direction-only shape schema roundtrip.");
        var property = producer.GetPropertyList().OfType<PropertyDescriptor<ImporterMesh, byte[]>>().Single(p => p.Name == "_data"); var bytes = property.GetValue(producer); Check(BinaryPrimitives.ReadUInt32LittleEndian(bytes) == 0x32504d49, "IMP2 carries an explicit count."); using var restored = new ImporterMesh(); property.SetValue(restored, bytes); Check(restored.GetSurfaceArrays(0).VertexCount == 4 && restored.GetSurfaceArrays(0).Vertices.Length == 0 && restored.GetSurfaceBlendShapeArrays(0, 0).Normals.Length == 4, "IMP2 roundtrip includes direction-only targets.");
        Reject<InvalidDataException>(() => property.SetValue(restored, bytes[..^1])); var broken = (byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(broken.AsSpan(4), int.MaxValue); Reject<InvalidDataException>(() => property.SetValue(restored, broken)); Check(restored.GetSurfaceArrays(0).VertexCount == 4, "Malformed producer storage preserves prior geometry.");
        using var duplicate = (ImporterMesh)producer.Duplicate(); Check(duplicate.GetSurfaceArrays(0).VertexCount == 4 && duplicate.GetSurfaceFormat(0) == Empty, "Raw duplication retains exact flags/count.");
        using var merged = ImporterMesh.MergeImporterMeshes([copied, copied], [Transform.Identity, Transform.Identity]); Check(merged.GetSurfaceArrays(0).VertexCount == 8 && merged.GetSurfaceArrays(0).Indices.Max() == 7 && merged.GetSurfaceArrays(0).Normals.Length == 8 && merged.GetBlendShapeCount() == 0, "Identity merges retain channel counts/remapped indices and ordinary target-discard policy.");
        Reject<ArgumentException>(() => ImporterMesh.MergeImporterMeshes([copied], [new(0, Vector2.Right)])); Check(copied.GetSurfaceArrays(0).VertexCount == 4, "Unknown generated positions cannot be geometrically baked by a merge.");
        using var ordinary = new ArrayMesh(); ordinary.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [Vector2.Zero, Vector2.Right, Vector2.Down] }); var old = ordinary.SaveSurfaces(); BinaryPrimitives.WriteUInt32LittleEndian(old, 0x37534d41); using var legacy = new ArrayMesh(); legacy.LoadSurfaces(old); Check(legacy.SurfaceGetArrays(0).Vertices.Length == 3, "AMS7 remains readable with its position-count layout.");
        using var stream = new MemoryStream(); using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            writer.Write(0x31504d49u); writer.Write(0); writer.Write(1); writer.Write((int)Mesh.PrimitiveType.Triangles); writer.Write(0UL); writer.Write(0);
            writer.Write(3); for (var i = 0; i < 8; i++) writer.Write(0); for (var i = 0; i < 4; i++) { writer.Write(-1); writer.Write(0); }
            foreach (var v in new[] { Vector2.Zero, Vector2.Right, Vector2.Down }) { writer.Write(v.X); writer.Write(v.Y); }
            writer.Write(0);
        }
        using var legacyProducer = new ImporterMesh(); property.SetValue(legacyProducer, stream.ToArray()); Check(legacyProducer.GetSurfaceArrays(0).VertexCount == 3, "IMP1 position counts remain readable.");
        Fresh(producer, mesh);
    }
    private static void Lifecycle()
    {
        using var mesh = new ArrayMesh(); var events = 0; mesh.Changed += _ => events++;
        mesh.SetBlockSignals(true); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data(), flags: Empty); mesh.SetBlockSignals(false); Check(events == 0 && mesh.GetSurfaceCount() == 1, "Blocked signals retain committed procedural geometry.");
        Action<Resource> failure = _ => throw new InvalidOperationException("procedural observer"); mesh.Changed += failure; Reject<InvalidOperationException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data(), flags: Empty)); mesh.Changed -= failure; Check(events == 1 && mesh.GetSurfaceCount() == 2, "Observer failure preserves a complete committed surface.");
        var bytes = mesh.SaveSurfaces(); Reject<InvalidDataException>(() => mesh.LoadSurfaces(bytes[..^1])); Check(mesh.GetSurfaceCount() == 2, "Malformed counted archive replacement is atomic.");
        Reject<ArgumentOutOfRangeException>(() => mesh.SurfaceUpdateVertexRegion(0, 0, [0])); Reject<InvalidOperationException>(() => mesh.RegenNormalMaps());
        using var shader = Program("GLSL"); using var material = new ShaderMaterial { Shader = shader }; var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); var canvas = new CanvasMesh(mesh, Transform.Identity, Colors.White);
        void Draw(MaterialState? state) { vertices.Clear(); batches.Clear(); canvas.Append(vertices, batches, null, Transform.Identity, Colors.White, state, BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false); }
        Reject<NotSupportedException>(() => Draw(null)); Draw(material.GetCanvasState()); Check(vertices.Count == 12 && batches.Count == 2 && batches.All(b => b.Procedural && b.Primitive == Mesh.PrimitiveType.Triangles), "Separate procedural surfaces retain native draw boundaries.");
        using var custom = new GeneratedMesh(); var customDraw = new CanvasMesh(custom, Transform.Identity, Colors.White);
        void DrawCustom() { vertices.Clear(); batches.Clear(); customDraw.Append(vertices, batches, null, Transform.Identity, Colors.White, material.GetCanvasState(), BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false); }
        Check(custom.GetAABB() == default, "Custom default bounds do not recursively query themselves for absent positions."); DrawCustom(); Check(vertices.Count == 6 && batches[0].VisibilityBounds == default(Rect2), "Custom generated drawing supports empty default bounds.");
        custom.Manual = new(-1, -2, 30, 40); custom.EmitChanged(); DrawCustom(); Check(batches[0].VisibilityBounds == custom.Manual, "Custom authored bounds reach procedural visibility."); custom.Callback = custom.EmitChanged; custom.EmitChanged(); Reject<InvalidOperationException>(DrawCustom); custom.Callback = null; DrawCustom(); custom.Callback = DrawCustom; custom.EmitChanged(); Reject<InvalidOperationException>(DrawCustom); custom.Callback = null; DrawCustom();
        using var line = CreateMesh(Mesh.PrimitiveType.LineStrip); using var instances = new MultiMesh { Mesh = line, InstanceCount = 2 }; instances.SetInstanceTransform2D(1, new(0, new(50, 0))); vertices.Clear(); batches.Clear(); new CanvasMesh(line, Transform.Identity, Colors.White).Append(vertices, batches, null, Transform.Identity, Colors.White, material.GetCanvasState(), BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false, instances); Check(batches.Count == 2 && batches.All(b => b.Count == 3 && b.Primitive == Mesh.PrimitiveType.LineStrip), "Fallback instances never connect independent strips.");
    }
    private sealed class GeneratedMesh : Mesh
    {
        internal Action? Callback; internal Rect2? Manual;
        protected override int OnGetSurfaceCount() => 1;
        protected override MeshSurfaceData OnSurfaceGetArrays(int index) => Data();
        protected override int OnSurfaceGetArrayLen(int index) => 4;
        protected override int OnSurfaceGetArrayIndexLen(int index) => 6;
        protected override PrimitiveType OnSurfaceGetPrimitiveType(int index) => PrimitiveType.Triangles;
        protected override ArrayFormat OnSurfaceGetFormat(int index) => Empty;
        protected override Material? OnSurfaceGetMaterial(int index) => null;
        protected override void OnSurfaceSetMaterial(int index, Material? material) { }
        protected override Rect2 OnGetAABB() { Callback?.Invoke(); return Manual ?? base.OnGetAABB(); }
    }
    private static void Fresh(ImporterMesh producer, ArrayMesh mesh)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "procedural-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(producer, path); ResourceSaver.Save(mesh, path + ".mesh.e2dres");
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(ProceduralMeshTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_PROCEDURAL_MESH"); start.Environment["ELECTRON2D_TEST_PROCEDURAL_MESH_CHILD"] = path;
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh procedural archives."); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh procedural archives passed"), errors.GetAwaiter().GetResult());
        }
        finally { File.Delete(path); File.Delete(path + ".mesh.e2dres"); }
    }
    internal static void Child(string path)
    {
        using var producer = ResourceLoader.Load<ImporterMesh>(path, ResourceLoader.CacheMode.Ignore); using var mesh = ResourceLoader.Load<ArrayMesh>(path + ".mesh.e2dres", ResourceLoader.CacheMode.Ignore); using var compiled = producer.GetMesh()!;
        Check(compiled.SurfaceGetArrayLen(0) == 4 && mesh.SurfaceGetArrayLen(0) == 4 && compiled.SurfaceGetArrays(0).Vertices.Length == 0 && mesh.SurfaceGetArrays(0).Vertices.Length == 0 && mesh.SurfaceGetBlendShapeArrays(0)[0].Normals.Length == 4 && mesh.SurfaceGetLODs(0)[2].SequenceEqual(new[] { 0, 1, 2 }), "Fresh producer and compiled archives retain count/target/LOD schemas."); Console.WriteLine("Fresh procedural archives passed.");
    }
}
