using System.Buffers.Binary;
using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;

internal static partial class MeshPackedTests
{
    private static MeshSurfaceBuffers Minimal(int count = 3)
    {
        var bytes = new byte[count * 8];
        for (var i = 0; i < count; i++) { BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * 8), i % 2 * 16); BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * 8 + 4), i / 2 * 16); }
        return new() { VertexCount = count, VertexData = bytes, AABB = new(0, 0, 16, 16) };
    }
    internal static void Run()
    {
        foreach (var primitive in Enum.GetValues<Mesh.PrimitiveType>()) foreach (var compressed in new[] { false, true }) foreach (var eight in new[] { false, true }) foreach (var first in new[] { 0, 4 })
                    {
                        using var original = new ArrayMesh(); original.AddBlendShape("A"); original.AddBlendShape("B");
                        var data = MeshFrameTests.Data(); if (primitive == Mesh.PrimitiveType.Lines) data.Indices = [0, 1, 2, 3];
                        data.Colors = [new(.1f, .2f, .3f, .4f), Colors.Red, Colors.Blue, Colors.White]; data.UVs = [new(-2, 3), new(4, 0), Vector2.One, Vector2.Zero]; data.UV2s = [new(1, 2), new(-3, -4), Vector2.Zero, Vector2.One];
                        for (var channel = 0; channel < 4; channel++) data.SetCustom(channel, MeshCustomTests.Channel((MeshCustomFormat)(first + channel), new(.25f, 0, 0, 1)));
                        var slots = eight ? 8 : 4; data.Bones = new int[4 * slots]; data.Weights = new float[data.Bones.Length];
                        for (var i = 0; i < 4; i++) { data.Bones[i * slots] = 2; data.Weights[i * slots] = .5f; data.Weights[i * slots + 1] = .5f; }
                        var shapes = new[] { new MeshSurfaceData { Vertices = [new(1, 0), new(17, 0), new(17, 16), new(1, 16)], Normals = (Vector3[])data.Normals.Clone(), Tangents = (Vector4[])data.Tangents.Clone() }, new MeshSurfaceData { Vertices = (Vector2[])data.Vertices.Clone(), Normals = (Vector3[])data.Normals.Clone(), Tangents = (Vector4[])data.Tangents.Clone() } };
                        var lodCount = primitive switch { Mesh.PrimitiveType.Points => 1, Mesh.PrimitiveType.Lines or Mesh.PrimitiveType.LineStrip => 2, _ => 3 };
                        var flags = (compressed ? Mesh.ArrayFormat.CompressAttributes : 0) | (eight ? Mesh.ArrayFormat.Use8BoneWeights : 0);
                        original.AddSurfaceFromArrays(primitive, data, shapes, new Dictionary<float, int[]> { [2] = Enumerable.Range(0, lodCount).ToArray() }, flags);
                        var buffers = original.ExportPacked(0); buffers.AABB = new(-2, -2, 20, 20);
                        using var restored = new ArrayMesh(); restored.AddBlendShape("A"); restored.AddBlendShape("B"); restored.AppendPacked(ArrayMesh.PreparePacked(buffers, 2)); Same(buffers, restored.ExportPacked(0));
                        Check(restored.GetAABB() == buffers.AABB && buffers.BoneAABBs.Length == 3 && buffers.BoneAABBs[1] is null, "Authored bounds and sparse per-bone bounds execute.");
                        buffers.VertexData[0] ^= 1; buffers.AttributeData[0] ^= 1; buffers.BlendShapeData[0] ^= 1; buffers.BoneAABBs[0] = default;
                        var stable = restored.ExportPacked(0); Check(stable.VertexData[0] != buffers.VertexData[0] && stable.AttributeData[0] != buffers.AttributeData[0] && stable.BlendShapeData[0] != buffers.BlendShapeData[0] && stable.BoneAABBs[0] is not null, "Import owns every byte/bounds array.");
                        using var copy = (ArrayMesh)restored.Duplicate(); Same(stable, copy.ExportPacked(0));
                        restored.SurfaceUpdateVertexRegion(0, 0, new byte[4]); Check(restored.GetAABB() == stable.AABB, "Raw vertex edits retain explicitly authored visibility bounds.");
                    }
        foreach (var count in new[] { 65536, 65537 })
        {
            var buffers = Minimal(count); buffers.Primitive = Mesh.PrimitiveType.Points; buffers.Format |= Mesh.ArrayFormat.Index; buffers.IndexCount = 1; buffers.IndexData = new byte[count <= 65536 ? 2 : 4];
            if (count == 65536) BinaryPrimitives.WriteUInt16LittleEndian(buffers.IndexData, 65535); else BinaryPrimitives.WriteUInt32LittleEndian(buffers.IndexData, 65536);
            using var mesh = new ArrayMesh(); mesh.AppendPacked(ArrayMesh.PreparePacked(buffers, 0)); Same(buffers, mesh.ExportPacked(0)); Check(mesh.SurfaceGetArrays(0).Indices[0] == count - 1, "Both exact index-width boundaries decode.");
        }
        var colors = Minimal(256); colors.Primitive = Mesh.PrimitiveType.Points; colors.Format |= Mesh.ArrayFormat.Color; colors.AttributeData = Enumerable.Range(0, 1024).Select(i => (byte)i).ToArray();
        using (var mesh = new ArrayMesh()) { mesh.AppendPacked(ArrayMesh.PreparePacked(colors, 0)); Same(colors, mesh.ExportPacked(0)); }
        IndependentShapeSchema(); Invalid(); Fresh();
        Reject<InvalidOperationException>(() => RenderingServer.MeshCreateFromSurfaces([])); Reject<InvalidOperationException>(() => RenderingServer.MeshAddSurface(default, Minimal())); Reject<InvalidOperationException>(() => RenderingServer.MeshGetSurface(default, 0));
        Console.WriteLine("Packed surfaces: all topologies/custom formats, compressed frames/UVs, four/eight skin, two shapes, LODs, exact buffers, copies, bounds, failures and fresh authoring passed.");
    }
    private static void IndependentShapeSchema()
    {
        foreach (var compressed in new[] { false, true })
        {
            using var original = new ArrayMesh(); original.AddBlendShape("Target"); var data = MeshFrameTests.Data(); data.Tangents = [];
            var target = new MeshSurfaceData { Vertices = data.Vertices.Select(p => p + new Vector2(4, 0)).ToArray(), Normals = (Vector3[])data.Normals.Clone() };
            original.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, data, [target], flags: compressed ? Mesh.ArrayFormat.CompressAttributes : 0); original.RegenNormalMaps();
            var buffers = original.ExportPacked(0); Check(buffers.BlendShapeFormat == (Mesh.ArrayFormat.Vertex | Mesh.ArrayFormat.Normal), "Export retains the independent normal-only shape schema.");
            using var restored = new ArrayMesh(); restored.AddBlendShape("Target"); restored.AppendPacked(ArrayMesh.PreparePacked(buffers, 1)); Same(buffers, restored.ExportPacked(0)); Check(restored.SurfaceGetBlendShapeArrays(0)[0].Tangents.Length == 0, "Raw import does not fabricate authored target tangents.");
            foreach (var mode in Enum.GetValues<BlendShapeMode>())
            {
                original.BlendShapeMode = restored.BlendShapeMode = mode; using var a = new MeshMorphPose(); using var b = new MeshMorphPose();
                var before = a.Prepare(original, [.5f]).SurfaceGetArrays(0); var after = b.Prepare(restored, [.5f]).SurfaceGetArrays(0);
                Check(before.Vertices.SequenceEqual(after.Vertices) && before.Normals.SequenceEqual(after.Normals) && before.Tangents.SequenceEqual(after.Tangents), "Both live morph modes preserve geometry and direction channels after packed round-trip.");
            }
        }
    }
    private static void Invalid()
    {
        void Bad<T>(Action<MeshSurfaceBuffers> mutate) where T : Exception { var b = Minimal(); mutate(b); Reject<T>(() => ArrayMesh.PreparePacked(b, 0)); }
        Bad<ArgumentOutOfRangeException>(b => b.VertexCount = 0); Bad<ArgumentOutOfRangeException>(b => b.IndexCount = -1);
        Bad<ArgumentNullException>(b => b.VertexData = null!); Bad<ArgumentNullException>(b => b.LODs = null!);
        Bad<ArgumentException>(b => b.VertexData = new byte[23]); Bad<ArgumentException>(b => b.VertexData = new byte[25]); Bad<ArgumentException>(b => b.AttributeData = [0]); Bad<ArgumentException>(b => b.SkinData = [0]); Bad<ArgumentException>(b => b.IndexData = [0]);
        Bad<ArgumentException>(b => BinaryPrimitives.WriteSingleLittleEndian(b.VertexData, float.NaN)); Bad<ArgumentException>(b => { b.Format |= Mesh.ArrayFormat.TexUV; b.AttributeData = new byte[24]; BinaryPrimitives.WriteSingleLittleEndian(b.AttributeData, float.PositiveInfinity); });
        Bad<ArgumentException>(b => b.Format |= Mesh.ArrayFormat.Bones); Bad<ArgumentException>(b => b.Format |= Mesh.ArrayFormat.Index);
        Bad<ArgumentException>(b => b.AABB = new(0, 0, -1, 2)); Bad<ArgumentException>(b => b.AABB = new(float.MaxValue, 0, float.MaxValue, 1)); Bad<ArgumentException>(b => b.UVScale = new(float.NaN, 0, 0, 0));
        Bad<ArgumentException>(b => b.BoneAABBs = [new Rect2(0, 0, 1, 1)]); Bad<ArgumentException>(b => b.BlendShapeData = [1]); Bad<ArgumentException>(b => b.LODs = new Dictionary<float, byte[]> { [1] = [0] });
        Bad<NotSupportedException>(b => b.Format |= (Mesh.ArrayFormat)(1ul << 63)); Bad<NotSupportedException>(b => b.Format &= ~Mesh.ArrayFormat.Use2DVertices);
        Bad<ArgumentOutOfRangeException>(b => b.Primitive = (Mesh.PrimitiveType)100);
        using (var material = new CanvasItemMaterial()) { material.Dispose(); Bad<ObjectDisposedException>(b => b.Material = material); }
        Bad<ArgumentException>(b => b.BlendShapeFormat = Mesh.ArrayFormat.Color);
        Reject<ArgumentNullException>(() => ArrayMesh.PreparePacked(null!, 0)); Reject<ArgumentException>(() => ArrayMesh.PreparePacked(Minimal(), 1));
        using var target = new ArrayMesh(); target.AppendPacked(ArrayMesh.PreparePacked(Minimal(), 0)); var before = target.ExportPacked(0);
        var indexed = Minimal(); indexed.Format |= Mesh.ArrayFormat.Index; indexed.IndexCount = 3; indexed.IndexData = [0, 0, 1, 0, 255, 255];
        Reject<ArgumentException>(() => target.AppendPacked(ArrayMesh.PreparePacked(indexed, 0))); Same(before, target.ExportPacked(0));
        Action<Resource> failure = _ => throw new InvalidOperationException("packed append observer"); target.Changed += failure; Reject<InvalidOperationException>(() => target.AppendPacked(ArrayMesh.PreparePacked(Minimal(), 0))); target.Changed -= failure;
        Check(target.GetSurfaceCount() == 2, "Observer failure retains complete committed append.");
    }
    private static void Same(MeshSurfaceBuffers a, MeshSurfaceBuffers b)
    {
        Check(a.Primitive == b.Primitive && a.Format == b.Format && a.VertexCount == b.VertexCount && a.IndexCount == b.IndexCount && a.BlendShapeFormat == b.BlendShapeFormat && a.AABB == b.AABB && a.UVScale == b.UVScale && ReferenceEquals(a.Material, b.Material), "Packed scalar/material metadata round-trips.");
        Check(a.VertexData.SequenceEqual(b.VertexData) && a.AttributeData.SequenceEqual(b.AttributeData) && a.SkinData.SequenceEqual(b.SkinData) && a.IndexData.SequenceEqual(b.IndexData) && a.BlendShapeData.SequenceEqual(b.BlendShapeData) && a.BoneAABBs.SequenceEqual(b.BoneAABBs), "All independent packed buffers/bounds round-trip exactly.");
        Check(a.LODs.Count == b.LODs.Count && a.LODs.All(pair => b.LODs.TryGetValue(pair.Key, out var bytes) && pair.Value.SequenceEqual(bytes)), "Every LOD buffer round-trips.");
    }
    private static readonly System.Text.Json.JsonSerializerOptions PackedJSON = new() { IncludeFields = true, IgnoreReadOnlyProperties = true };
    private static void Fresh()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-packed-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(Minimal(), PackedJSON));
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(MeshPackedTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_MESH_PACKED"); start.Environment["ELECTRON2D_TEST_MESH_PACKED_CHILD"] = path;
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Packed mesh child."); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh packed surface passed"), errors.GetAwaiter().GetResult());
        }
        finally { File.Delete(path); }
    }
    internal static void Child(string path)
    {
        var surface = System.Text.Json.JsonSerializer.Deserialize<MeshSurfaceBuffers>(File.ReadAllText(path), PackedJSON)!;
        using var mesh = new ArrayMesh(); mesh.AppendPacked(ArrayMesh.PreparePacked(surface, 0)); Same(Minimal(), mesh.ExportPacked(0)); Console.WriteLine("Fresh packed surface passed.");
    }
}
