using System.Buffers.Binary;
using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;

internal static class MeshMorphTests
{
    internal static MeshSurfaceData Pose(MeshSurfaceData basis, Vector2 offset, bool absolute = false)
    {
        return new() { Vertices = basis.Vertices.Select(v => absolute ? v + offset : offset).ToArray(), Normals = basis.Normals.Select(_ => new Vector3(0, 0, 1)).ToArray(), Tangents = basis.Tangents.Select(_ => new Vector4(-1, 0, 0, -1)).ToArray() };
    }
    internal static ArrayMesh Mesh(bool absolute = false, bool skin = false)
    {
        var data = MeshCustomTests.Data(); if (skin) { data.Bones = new int[32]; data.Weights = new float[32]; for (var i = 0; i < 4; i++) { data.Bones[i * 8 + 5] = 5; data.Weights[i * 8 + 5] = 1; } }
        var mesh = new ArrayMesh { BlendShapeMode = absolute ? BlendShapeMode.Normalized : BlendShapeMode.Relative }; mesh.AddBlendShape("move"); mesh.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, data, [Pose(data, new(8, 0), absolute)], flags: skin ? Electron2D.Mesh.ArrayFormat.Use8BoneWeights : 0); return mesh;
    }
    internal static void Run()
    {
        Definitions(); Extraction(); Drawing(); Callbacks(); Archives(); Legacy(); Fresh(); Warm();
        Console.WriteLine("Morph API: definitions/schema/copies, exact extraction, independent normalized/relative drawing, finite rollback, archives/fresh scenes and zero warmed managed updates passed.");
    }
    private static void Definitions()
    {
        using var mesh = new ArrayMesh(); mesh.AddBlendShape("move"); mesh.AddBlendShape("move"); mesh.AddBlendShape("move 2"); Check(mesh.GetBlendShapeName(1) == "move 2" && mesh.GetBlendShapeName(2) == "move 2 2", "Definition suffixes are ordinal and first free."); mesh.SetBlendShapeName(2, "move"); Check(mesh.GetBlendShapeName(2) == "move 3", "Rename uses the same suffix rule."); mesh.ClearBlendShapes(); Check(mesh.GetBlendShapeCount() == 0 && mesh.BlendShapeMode == BlendShapeMode.Relative, "Clear defaults/definition lifetime."); Reject<ArgumentOutOfRangeException>(() => mesh.BlendShapeMode = (BlendShapeMode)2); Reject<ArgumentOutOfRangeException>(() => mesh.GetBlendShapeName(0));
        var basis = MeshCustomTests.Data(); var pose = Pose(basis, new(8, 0)); mesh.AddBlendShape(""); Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, basis)); var wrong = pose.Copy(); wrong.Vertices = [Vector2.Zero]; Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, basis, [wrong])); wrong = pose.Copy(); wrong.Colors = [Colors.White]; Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, basis, [wrong])); wrong = pose.Copy(); wrong.Normals[0] = new(float.NaN, 0, 0); Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, basis, [wrong])); Check(mesh.GetSurfaceCount() == 0, "Failed payloads preserve geometry and definitions.");
        mesh.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, basis, [pose]); pose.Vertices[0] = Vector2.Zero; var query = mesh.SurfaceGetBlendShapeArrays(0); Check(query[0].Vertices[0] == new Vector2(8, 0) && query[0].Normals[0].Z == 1 && query[0].Tangents[0].W == -1, "Import copies target channels using frame precision."); query[0].Vertices[0] = Vector2.Zero; Check(mesh.SurfaceGetBlendShapeArrays(0)[0].Vertices[0].X == 8, "Pose queries copy data."); Reject<InvalidOperationException>(() => mesh.AddBlendShape("late")); Reject<InvalidOperationException>(mesh.ClearBlendShapes); mesh.ClearSurfaces(); Check(mesh.GetBlendShapeCount() == 1, "Surface clearing retains definitions."); mesh.ClearBlendShapes();
    }
    private static void Extraction()
    {
        using var mesh = Mesh(); using var material = new CanvasItemMaterial { BlendMode = BlendMode.Add }; mesh.SurfaceSetMaterial(0, material); using var tool = new SurfaceTool(); tool.CreateFromBlendShape(mesh, 0, "move"); var data = tool.CommitToArrays(); Check(data.Vertices.All(v => v == new Vector2(8, 0)) && data.Normals[0].Z == 1 && data.Tangents[0].W == -1 && data.Custom3!.Data.SequenceEqual(mesh.SurfaceGetArrays(0).Custom3!.Data) && data.Indices.SequenceEqual(mesh.SurfaceGetArrays(0).Indices), "Extraction replaces only raw target channels."); using var output = tool.Commit(); Check(ReferenceEquals(output.SurfaceGetMaterial(0), material), "Pose commit retains borrowed material."); Reject<ArgumentException>(() => tool.CreateFromBlendShape(mesh, 0, "missing")); Check(tool.CommitToArrays().Vertices[0].X == 8, "Missing pose preserves prior authoring.");
    }
    private static List<CanvasVertex> Draw(MeshInstance node)
    {
        node.PrepareCanvas(); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); node.AppendCanvas(vertices, batches, Transform.Identity); return vertices;
    }
    private static void Drawing()
    {
        foreach (var absolute in new[] { false, true })
        {
            using var mesh = Mesh(absolute); using var root = new Node(); var a = new MeshInstance { Name = "A", Mesh = mesh }; var b = new MeshInstance { Name = "B", Mesh = mesh }; root.AddChild(a); root.AddChild(b); using var tree = new SceneTree(root); a.SetBlendShapeValue(0, .5f); var av = Draw(a); var bv = Draw(b); Check(av[0].Position == new Vector2(4, 0) && bv[0].Position == Vector2.Zero && av[0].Normal == bv[0].Normal && av[0].Tangent == bv[0].Tangent && av[0].Custom3 == bv[0].Custom3, "Independent weighted positions preserve base numerical/other channels."); Check(a.RenderBounds().Position == new Vector2(4, 0), "Bounds use actual weighted geometry.");
            Reject<ArgumentException>(() => a.SetBlendShapeValue(0, float.MaxValue)); Check(a.GetBlendShapeValue(0) == .5f && Draw(a)[0].Position.X == 4, "Overflowing weighted geometry preserves values and output."); a.SetBlendShapeValue(0, .0001f); Check(Draw(a)[0].Position.X == 0 && a.GetBlendShapeValue(0) == .0001f, "Small values remain authored while drawing skips them."); a.SetBlendShapeValue(0, -.5f); Check(Draw(a)[0].Position.X == -4, "Negative weights retain the two mode equations."); a.SetBlendShapeValue(0, 2); Check(Draw(a)[0].Position.X == 16, "Weights above one are not clamped."); a.SetBlendShapeValue(0, 0); Check(Draw(a)[0].Position == Vector2.Zero, "Zero restores base drawing.");
        }
        using var normalOnly = new ArrayMesh(); var basis = new MeshSurfaceData { Vertices = [Vector2.Zero, Vector2.Right, Vector2.Down], Normals = [new(0, 0, -1), new(0, 0, -1), new(0, 0, -1)] }; normalOnly.AddBlendShape("normal"); normalOnly.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, basis, [Pose(basis, Vector2.Right)]); using var node = new MeshInstance { Mesh = normalOnly }; node.SetBlendShapeValue(0, .5f); Check(node.GetBlendShapeValue(0) == .5f, "Original normal-only target schema remains usable with synthesized base tangents.");
    }
    private sealed class Custom : Electron2D.Mesh
    {
        internal Action? CountCallback;
        internal Action? ShapeCallback;
        private readonly MeshSurfaceData _basis = MeshCustomTests.Data();
        protected override int OnGetSurfaceCount() => 1;
        protected override int OnSurfaceGetArrayLen(int surfaceIndex) => 4;
        protected override int OnSurfaceGetArrayIndexLen(int surfaceIndex) => 6;
        protected override PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex) => PrimitiveType.Triangles;
        protected override ArrayFormat OnSurfaceGetFormat(int surfaceIndex) => ArrayMesh.Surface.Channels(_basis);
        protected override MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex) => _basis.Copy();
        protected override Material? OnSurfaceGetMaterial(int surfaceIndex) => null;
        protected override void OnSurfaceSetMaterial(int surfaceIndex, Material? material) => throw new NotSupportedException();
        protected override int OnGetBlendShapeCount() { CountCallback?.Invoke(); return 1; }
        protected override string OnGetBlendShapeName(int index) => "move";
        protected override void OnSetBlendShapeName(int index, string name) => throw new NotSupportedException();
        protected override MeshSurfaceData[] OnSurfaceGetBlendShapeArrays(int surfaceIndex) { ShapeCallback?.Invoke(); return [Pose(_basis, new(8, 0))]; }
    }
    private static void Callbacks()
    {
        using var source = new Custom(); using var node = new MeshInstance { Mesh = source }; source.CountCallback = () => node.SetBlendShapeValue(0, 1); Reject<InvalidOperationException>(() => node.SetBlendShapeValue(0, .5f)); source.CountCallback = null; Check(node.GetBlendShapeValue(0) == 0 && !node.IsDisposed, "Count callback mutation preserves actor state and permits retry.");
        source.ShapeCallback = () => node.Position = Vector2.One; Reject<InvalidOperationException>(() => node.SetBlendShapeValue(0, .5f)); source.ShapeCallback = null; Check(node.Position == Vector2.Zero, "Inherited instance mutations also reject during shape callbacks.");
        source.ShapeCallback = node.Dispose; Reject<InvalidOperationException>(() => node.SetBlendShapeValue(0, .5f)); source.ShapeCallback = null; Check(!node.IsDisposed && node.GetBlendShapeValue(0) == 0, "Shape callback disposal rejects before lifetime commitment."); node.SetBlendShapeValue(0, .5f); Check(node.GetBlendShapeValue(0) == .5f, "Protected custom shape callbacks reach the actual instance consumer.");
        using var tool = new SurfaceTool(); tool.CreateFromBlendShape(source, 0, "move"); source.ShapeCallback = () => tool.Begin(Electron2D.Mesh.PrimitiveType.Triangles); Reject<InvalidOperationException>(() => tool.CreateFromBlendShape(source, 0, "move")); source.ShapeCallback = null; Check(tool.CommitToArrays().Vertices[0].X == 8, "Pose extraction rejects builder reentry before changing its prior geometry.");
        source.ShapeCallback = source.EmitChanged; Reject<InvalidOperationException>(() => tool.CreateFromBlendShape(source, 0, "move")); source.ShapeCallback = null; tool.CreateFromBlendShape(source, 0, "move");
    }
    private static void Archives()
    {
        using var mesh = Mesh(true); var property = (PropertyDescriptor<ArrayMesh, byte[]>)mesh.GetPropertyList().Single(p => p.Name == "_surface_data"); var archive = property.GetValue(mesh); using var restored = new ArrayMesh(); property.SetValue(restored, archive); Check(restored.GetBlendShapeName(0) == "move" && restored.SurfaceGetBlendShapeArrays(0)[0].Vertices.SequenceEqual(mesh.SurfaceGetBlendShapeArrays(0)[0].Vertices), "AMS8 stores exact definition/pose channels."); mesh.RegenNormalMaps(); Check(mesh.GetBlendShapeCount() == 1 && mesh.SurfaceGetBlendShapeArrays(0)[0].Vertices.SequenceEqual(restored.SurfaceGetBlendShapeArrays(0)[0].Vertices), "Frame regeneration preserves definition/payload coherence."); using var duplicate = (ArrayMesh)mesh.Duplicate(); Check(duplicate.BlendShapeMode == BlendShapeMode.Normalized && duplicate.GetBlendShapeCount() == 1 && duplicate.SurfaceGetBlendShapeArrays(0)[0].Tangents[0].W == -1, "Duplication preserves mode/definitions/pose frame."); var broken = (byte[])archive.Clone(); BinaryPrimitives.WriteInt32LittleEndian(broken.AsSpan(8), int.MaxValue); Reject<InvalidDataException>(() => property.SetValue(restored, broken)); Check(restored.GetBlendShapeCount() == 1, "Malformed definition budget preserves old storage.");
    }
    private static void Legacy()
    {
        using var source = new ArrayMesh(); source.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, MeshCustomTests.Data()); var property = (PropertyDescriptor<ArrayMesh, byte[]>)source.GetPropertyList().Single(p => p.Name == "_surface_data"); var current = property.GetValue(source); var legacy = new byte[current.Length - 32]; current.AsSpan(0, 8).CopyTo(legacy); current.AsSpan(12, 80).CopyTo(legacy.AsSpan(8)); current.AsSpan(120).CopyTo(legacy.AsSpan(88)); BinaryPrimitives.WriteUInt32LittleEndian(legacy, 0x34534d41); using var target = new ArrayMesh(); property.SetValue(target, legacy); Check(target.GetBlendShapeCount() == 0 && target.SurfaceGetArrays(0).Custom3!.Data.SequenceEqual(source.SurfaceGetArrays(0).Custom3!.Data) && target.Get(0).FrameWords.SequenceEqual(source.Get(0).FrameWords), "AMS4 custom/frame payloads remain readable without definitions.");
    }
    private static void Fresh()
    {
        using var mesh = Mesh(true); using var root = new Node(); var a = new MeshInstance { Name = "A", Mesh = mesh }; var b = new MeshInstance { Name = "B", Mesh = mesh }; root.AddChild(a); root.AddChild(b); a.Owner = root; b.Owner = root; a.SetBlendShapeValue(0, .5f); b.SetBlendShapeValue(0, 1); using var scene = new PackedScene(); scene.Pack(root); var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-morph-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try { ResourceSaver.Save(scene, path); var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(MeshMorphTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_MESH_MORPH"); start.Environment.Remove("ELECTRON2D_TEST_MESH_MORPH_NATIVE"); start.Environment["ELECTRON2D_TEST_MESH_MORPH_CHILD"] = path; using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Morph scene child."); } Check(child.ExitCode == 0 && stdout.GetAwaiter().GetResult().Contains("Fresh morph scene passed"), stderr.GetAwaiter().GetResult()); } finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var root = scene.Instantiate(); var a = (MeshInstance)root.GetNode("A"); var b = (MeshInstance)root.GetNode("B"); Check(ReferenceEquals(a.Mesh, b.Mesh) && a.GetBlendShapeValue(0) == .5f && b.GetBlendShapeValue(0) == 1 && ((ArrayMesh)a.Mesh!).BlendShapeMode == BlendShapeMode.Normalized, "Fresh graph preserves shared shape resources and independent stored values."); using var tree = new SceneTree(root); Check(Draw(a)[0].Position.X == 4 && Draw(b)[0].Position.X == 8, "Fresh instances execute different poses."); Console.WriteLine("Fresh morph scene passed.");
    }
    private static void Warm()
    {
        using var mesh = Mesh(); using var root = new Node(); var node = new MeshInstance { Mesh = mesh }; root.AddChild(node); using var tree = new SceneTree(root); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); var patch = new byte[8]; BinaryPrimitives.WriteSingleLittleEndian(patch, 1); BinaryPrimitives.WriteSingleLittleEndian(patch.AsSpan(4), 2);
        void Step(int i) { node.SetBlendShapeValue(0, i % 2 == 0 ? .5f : 1); mesh.SurfaceUpdateVertexRegion(0, 0, patch); node.PrepareCanvas(); vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity); }
        for (var i = 0; i < 24; i++) Step(i); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Step(i); Check(before == GC.GetAllocatedBytesForCurrentThread(), "Prepared weighted changes/source regions/draws allocate zero managed bytes.");
    }
}
