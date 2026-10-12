using System.Buffers.Binary;
using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;

internal static class MeshCustomTests
{
    internal static MeshCustomData Channel(MeshCustomFormat format, Vector4 value, int count = 4)
    {
        var stride = MeshCustomCodec.Stride(format); var data = new MeshCustomData { Format = format, Data = new byte[count * stride] }; for (var i = 0; i < count; i++) MeshCustomCodec.Write(format, value, data.Data.AsSpan(i * stride)); return data;
    }
    internal static MeshSurfaceData Data()
    {
        var data = MeshFrameTests.Data(); data.Custom0 = Channel(MeshCustomFormat.RGBA8UNORM, new(.25f, .5f, .75f, 1)); data.Custom1 = Channel(MeshCustomFormat.RGBA8SNORM, new(-1, .5f, -2, 1)); data.Custom2 = Channel(MeshCustomFormat.RGHalf, new(.25f, .5f, 0, 1)); data.Custom3 = Channel(MeshCustomFormat.RGBAFloat, new(.25f, .5f, .75f, 1)); return data;
    }
    internal static void Run()
    {
        Codecs(); Storage(); Legacy(); Authoring(); Indexing(); Fresh(); Warm();
        Console.WriteLine("Mesh custom channels: eight codecs, four independent schemas, transactional bytes, authoring/import/index/append, archives and warmed zero managed edits passed.");
    }
    private static void Codecs()
    {
        foreach (var format in Enum.GetValues<MeshCustomFormat>().Where(f => f != MeshCustomFormat.Max))
        {
            using var mesh = new ArrayMesh(); var data = MeshFrameTests.Data(); data.Custom0 = Channel(format, new(.25f, .5f, .75f, 1)); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, data); var copy = mesh.SurfaceGetArrays(0).Custom0!; Check(copy.Format == format && copy.Data.SequenceEqual(data.Custom0.Data), "Each format imports exact bytes."); copy.Data[0] ^= 1; Check(!copy.Data.SequenceEqual(mesh.SurfaceGetArrays(0).Custom0!.Data), "Queried custom bytes are independent.");
            using var tool = new SurfaceTool(); tool.CreateFrom(mesh, 0); tool.Index(); tool.Deindex(); Check(tool.CommitToArrays().Custom0!.Data.AsSpan(0, MeshCustomCodec.Stride(format)).SequenceEqual(data.Custom0.Data.AsSpan(0, MeshCustomCodec.Stride(format))), "Structural import/index/deindex preserves packed records.");
        }
        var unorm = Channel(MeshCustomFormat.RGBA8UNORM, new(-2, .5f, 2, 1), 1); Check(unorm.Data.SequenceEqual(new byte[] { 0, 127, 255, 255 }), "Unsigned authoring clamps and truncates."); var signed = Channel(MeshCustomFormat.RGBA8SNORM, new(-1, .5f, -2, 1), 1); Check(signed.Data.SequenceEqual(new byte[] { 129, 63, 128, 127 }), "Signed authoring retains minimum code and truncation.");
        var half = Channel(MeshCustomFormat.RGBAHalf, new(1.0006f, -0f, 1e-7f, .5f), 1); Check(BinaryPrimitives.ReadUInt16LittleEndian(half.Data) == 0x3c00 && BinaryPrimitives.ReadUInt16LittleEndian(half.Data.AsSpan(2)) == 0 && BinaryPrimitives.ReadUInt16LittleEndian(half.Data.AsSpan(4)) == 0 && BinaryPrimitives.ReadUInt16LittleEndian(half.Data.AsSpan(6)) == 0x3800, "Half authoring truncates mantissa and flushes tiny/signed-zero values."); Reject<ArgumentException>(() => Channel(MeshCustomFormat.RGHalf, new(float.MaxValue, 0, 0, 1), 1));
        using var invalid = new ArrayMesh(); var malformed = Data(); malformed.Custom0!.Data = [1]; Reject<ArgumentException>(() => invalid.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, malformed)); malformed = Data(); malformed.Custom2!.Data[0] = 0; malformed.Custom2.Data[1] = 0x7c; Reject<ArgumentException>(() => invalid.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, malformed)); malformed = Data(); malformed.Custom3!.Format = MeshCustomFormat.Max; Reject<ArgumentOutOfRangeException>(() => invalid.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, malformed));
    }
    private static void Storage()
    {
        using var mesh = new ArrayMesh(); var data = Data(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, data); Check((mesh.SurfaceGetFormat(0) & Mesh.ArrayFormat.Custom3) != 0 && ((ulong)mesh.SurfaceGetFormat(0) >> 22 & 7) == 7, "Flags infer channel presence and all packed formats.");
        var stride = MeshCustomCodec.AttributeStride(mesh.SurfaceGetFormat(0)); Check(stride == 36, "Primary UV plus four custom formats has the expected attribute stride."); var patch = new byte[stride * 2];
        for (var i = 0; i < 2; i++) { var row = patch.AsSpan(i * stride); BinaryPrimitives.WriteSingleLittleEndian(row, .2f); BinaryPrimitives.WriteSingleLittleEndian(row[4..], .3f); row[8] = 128; row[9] = 64; row[10] = 32; row[11] = 255; row[12] = 129; row[13] = 127; row[14] = 0; row[15] = 127; BinaryPrimitives.WriteUInt16LittleEndian(row[16..], 0x3800); BinaryPrimitives.WriteUInt16LittleEndian(row[18..], 0x3400); for (var component = 0; component < 4; component++) BinaryPrimitives.WriteSingleLittleEndian(row[(20 + component * 4)..], .5f); }
        BinaryPrimitives.WriteSingleLittleEndian(patch.AsSpan(stride + 20), float.NaN); var before = mesh.SurfaceGetArrays(0).Custom0!.Data; Reject<ArgumentException>(() => mesh.SurfaceUpdateAttributeRegion(0, 0, patch)); Check(mesh.SurfaceGetArrays(0).Custom0!.Data.SequenceEqual(before), "Later float custom failure rolls back earlier channels/vertices."); BinaryPrimitives.WriteSingleLittleEndian(patch.AsSpan(stride + 20), .5f); mesh.SurfaceUpdateAttributeRegion(0, 0, patch); Check(mesh.Get(0).Data.Custom1!.Values[0].X == -1 && mesh.Get(0).Data.Custom2!.Values[0].X == .5f, "Signed/half bytes refresh real prepared shader values.");
        var exact = mesh.SurfaceGetArrays(0).Custom3!.Data; mesh.SurfaceUpdateAttributeRegion(0, 21, new byte[] { exact[1] }); Check(mesh.SurfaceGetArrays(0).Custom3!.Data.SequenceEqual(exact), "Partial records preserve untouched raw bytes exactly.");
        var property = (PropertyDescriptor<ArrayMesh, byte[]>)mesh.GetPropertyList().Single(p => p.Name == "_surface_data"); var archive = property.GetValue(mesh); using var restored = new ArrayMesh(); property.SetValue(restored, archive); Check(restored.SurfaceGetArrays(0).Custom3!.Data.SequenceEqual(exact), "AMS5 stores exact custom schemas/bytes."); using var duplicate = (ArrayMesh)mesh.Duplicate(); Check(duplicate.SurfaceGetArrays(0).Custom0!.Data.SequenceEqual(mesh.SurfaceGetArrays(0).Custom0!.Data), "Resource duplicates copy custom payloads."); var broken = (byte[])archive.Clone(); BinaryPrimitives.WriteInt32LittleEndian(broken.AsSpan(60), int.MaxValue); Reject<InvalidDataException>(() => property.SetValue(restored, broken)); Check(restored.GetSurfaceCount() == 1, "Malformed archive custom schema preserves old storage.");
        using var topology = new MeshDataTool(); topology.CreateFromSurface(mesh, 0); using var output = new ArrayMesh(); topology.CommitToSurface(output); Check(output.SurfaceGetArrays(0).Custom2!.Data.SequenceEqual(mesh.SurfaceGetArrays(0).Custom2!.Data), "Topology editing carries raw custom channels through commit.");
    }
    private static void Legacy()
    {
        using var source = new ArrayMesh(); source.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshFrameTests.Data()); var property = (PropertyDescriptor<ArrayMesh, byte[]>)source.GetPropertyList().Single(p => p.Name == "_surface_data"); var current = property.GetValue(source); var legacy = new byte[current.Length - 44]; current.AsSpan(0, 8).CopyTo(legacy); current.AsSpan(12, 48).CopyTo(legacy.AsSpan(8)); current.AsSpan(100).CopyTo(legacy.AsSpan(56)); BinaryPrimitives.WriteUInt32LittleEndian(legacy, 0x33534d41); using var target = new ArrayMesh(); property.SetValue(target, legacy); Check(target.Get(0).FrameWords.SequenceEqual(source.Get(0).FrameWords) && target.SurfaceGetArrays(0).Custom0 is null, "AMS3 before custom schemas remains readable with exact normal/tangent words.");
    }
    private static void Authoring()
    {
        using var tool = new SurfaceTool(); Check(tool.GetCustomFormat(0) == MeshCustomFormat.Max, "No format initially."); Reject<ArgumentOutOfRangeException>(() => tool.GetCustomFormat(4)); tool.Begin(Mesh.PrimitiveType.Triangles); Reject<InvalidOperationException>(() => tool.SetCustom(0, Colors.White)); tool.SetCustomFormat(0, MeshCustomFormat.RGBA8UNORM); tool.SetCustom(0, new(.5f, .25f, .75f, 1)); tool.AddTriangleFan([Vector2.Zero, Vector2.Right, Vector2.Down]); Check(tool.CommitToArrays().Custom0!.Data[0] == 127, "Fan captures selected custom payload."); Reject<InvalidOperationException>(() => tool.SetCustomFormat(0, MeshCustomFormat.Max)); tool.SetCustomFormat(0, MeshCustomFormat.RGBAFloat); Check(tool.CommitToArrays().Custom0!.Format == MeshCustomFormat.RGBAFloat, "Format changes reencode existing captured values."); tool.Clear(); Check(tool.GetCustomFormat(0) == MeshCustomFormat.Max, "Clear resets selection.");
        using var source = new ArrayMesh(); source.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data()); tool.AppendFrom(source, 0, Transform.Identity); var incoming = Data(); incoming.Custom0 = Channel(MeshCustomFormat.RFloat, new(.8f, 0, 0, 1)); using var other = new ArrayMesh(); other.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, incoming); tool.AppendFrom(other, 0, Transform.Identity); Check(tool.GetCustomFormat(0) == MeshCustomFormat.RFloat && tool.CommitToArrays().Custom0!.Data.Length == 8 * 4, "Append chooses incoming present formats and reencodes both complete records.");
        tool.Begin(Mesh.PrimitiveType.Triangles); tool.AppendFrom(source, 0, Transform.Identity); tool.SetCustomFormat(0, MeshCustomFormat.RGBAFloat); using (var changed = tool.Commit()) Check(changed.SurfaceGetArrays(0).Custom0!.Format == MeshCustomFormat.RGBAFloat, "Imported format replacement updates packed format metadata before commit.");
        var huge = Data(); huge.Custom0 = Channel(MeshCustomFormat.RGBAFloat, new(float.MaxValue, 0, 0, 1)); using var hugeMesh = new ArrayMesh(); hugeMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, huge); tool.Begin(Mesh.PrimitiveType.Triangles); tool.AppendFrom(hugeMesh, 0, Transform.Identity); var before = tool.CommitToArrays().Custom0!.Data; Reject<ArgumentException>(() => tool.SetCustomFormat(0, MeshCustomFormat.RGHalf)); Check(tool.GetCustomFormat(0) == MeshCustomFormat.RGBAFloat && tool.CommitToArrays().Custom0!.Data.SequenceEqual(before), "Unrepresentable format conversion preserves the selected schema and every vertex.");
        using var incomingHalf = new ArrayMesh(); var halfData = Data(); halfData.Custom0 = Channel(MeshCustomFormat.RGHalf, new(1, 0, 0, 1)); incomingHalf.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, halfData); Reject<ArgumentException>(() => tool.AppendFrom(incomingHalf, 0, Transform.Identity)); Check(tool.CommitToArrays().Vertices.Length == 4 && tool.GetCustomFormat(0) == MeshCustomFormat.RGBAFloat, "Append schema conversion failure leaves geometry and formats unchanged.");
        var raw = Data(); raw.Custom2!.Data[0] = 1; raw.Custom2.Data[1] = 0; using var denormal = new ArrayMesh(); denormal.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, raw); tool.CreateFrom(denormal, 0); tool.Index(); using var result = tool.Commit(); Check(result.SurfaceGetArrays(0).Custom2!.Data[0] == 1, "Imported raw half subnormals survive structural authoring without codec rewrite.");
    }
    private sealed class BadFormat : Mesh
    {
        protected override int OnGetSurfaceCount() => 1;
        protected override int OnSurfaceGetArrayLen(int surfaceIndex) => 1;
        protected override int OnSurfaceGetArrayIndexLen(int surfaceIndex) => 0;
        protected override Material? OnSurfaceGetMaterial(int surfaceIndex) => null;
        protected override void OnSurfaceSetMaterial(int surfaceIndex, Material? material) => throw new NotSupportedException();
        protected override MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex) => new() { Vertices = [Vector2.Zero], Custom0 = Channel(MeshCustomFormat.RFloat, new(1, 0, 0, 1), 1) };
        protected override PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex) => PrimitiveType.Points;
        protected override ArrayFormat OnSurfaceGetFormat(int surfaceIndex) => ArrayFormat.Vertex | ArrayFormat.Use2DVertices | ArrayFormat.Custom0;
    }
    private static void Indexing()
    {
        using var tool = new SurfaceTool(); tool.Begin(Mesh.PrimitiveType.Points); tool.SetCustomFormat(0, MeshCustomFormat.RFloat); tool.SetCustom(0, new(.25f, 0, 0, 1)); tool.AddVertex(Vector2.Zero); tool.SetCustomFormat(1, MeshCustomFormat.RFloat); Reject<InvalidOperationException>(() => tool.SetCustom(1, Colors.White));
        using var source = new ArrayMesh(); source.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { Vertices = [Vector2.Zero], Custom0 = Channel(MeshCustomFormat.RFloat, new(.25f, 0, 0, 1), 1) }); tool.AppendFrom(source, 0, Transform.Identity); tool.Index(); Check(tool.CommitToArrays().Vertices.Length == 1 && tool.CommitToArrays().Indices.SequenceEqual(new[] { 0, 0 }), "Exact authored/imported records weld independently of their capture origin.");
        using var bad = new BadFormat(); Reject<ArgumentException>(() => tool.CreateFrom(bad, 0)); Check(tool.CommitToArrays().Vertices.Length == 1 && tool.GetCustomFormat(0) == MeshCustomFormat.RFloat, "Malformed custom callback format selectors preserve the previous authoring state.");
        tool.Begin(Mesh.PrimitiveType.Points); tool.SetCustomFormat(0, MeshCustomFormat.RFloat); tool.SetCustom(0, new(float.MaxValue, 0, 0, 1)); Reject<ArgumentException>(() => tool.SetCustomFormat(0, MeshCustomFormat.RGHalf)); Check(tool.GetCustomFormat(0) == MeshCustomFormat.RFloat, "Format conversion also validates the current value before any vertex exists.");
        using var halfSource = new ArrayMesh(); halfSource.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { Vertices = [Vector2.Zero], Custom0 = Channel(MeshCustomFormat.RGHalf, new(1, 0, 0, 1), 1) }); tool.AppendFrom(halfSource, 0, Transform.Identity); Reject<ArgumentException>(() => tool.AddVertex(Vector2.One)); Check(tool.CommitToArrays().Vertices.Length == 1, "Capture rejects a current value invalidated by an appended schema before changing geometry."); tool.SetCustom(0, Colors.White); tool.AddVertex(Vector2.One); Check(tool.CommitToArrays().Vertices.Length == 2, "A corrected current value permits capture retry.");
        var zeros = Channel(MeshCustomFormat.RFloat, new(0, 0, 0, 1), 2); BinaryPrimitives.WriteUInt32LittleEndian(zeros.Data.AsSpan(4), 0x80000000); tool.CreateFromArrays(new() { Vertices = [Vector2.Zero, Vector2.Zero], Custom0 = zeros }, Mesh.PrimitiveType.Points); tool.Index(); Check(tool.CommitToArrays().Vertices.Length == 2 && tool.CommitToArrays().Custom0!.Data.SequenceEqual(zeros.Data), "Distinct raw signed-zero records retain both exact packed payloads.");
    }
    private static void Fresh()
    {
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data()); using var material = new CanvasItemMaterial { BlendMode = BlendMode.Add }; mesh.SurfaceSetMaterial(0, material);
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-mesh-custom-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(mesh, path); var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(MeshCustomTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_MESH_CUSTOM"); start.Environment.Remove("ELECTRON2D_TEST_MESH_CUSTOM_NATIVE"); start.Environment["ELECTRON2D_TEST_MESH_CUSTOM_CHILD"] = path;
            using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Mesh custom child."); }
            Check(child.ExitCode == 0 && stdout.GetAwaiter().GetResult().Contains("Fresh custom archive passed"), stderr.GetAwaiter().GetResult());
        }
        finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        using var mesh = ResourceLoader.Load<ArrayMesh>(path, ResourceLoader.CacheMode.Ignore); var node = new Draw(mesh); using var root = new Node(); root.AddChild(node); using var tree = new SceneTree(root); node.PrepareCanvas(); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); node.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 6 && vertices[0].Custom0.Y == 127 / 255f && vertices[0].Custom1.Z == -1 && vertices[0].Custom2 == new Vector4(.25f, .5f, 0, 1) && vertices[0].Custom3 == new Vector4(.25f, .5f, .75f, 1) && batches[0].Blend == BlendMode.Add, "Fresh archive executes four independent decoded custom channels with its material.");
        var exact = mesh.SurfaceGetArrays(0).Custom2!.Data; mesh.RegenNormalMaps(); Check(mesh.SurfaceGetArrays(0).Custom2!.Data.SequenceEqual(exact), "Fresh archive retains custom bytes through frame regeneration."); Console.WriteLine("Fresh custom archive passed.");
    }
    private sealed class Draw(Mesh mesh) : Entity { protected override void OnDraw() => DrawMesh(mesh, null); }
    private static void Warm()
    {
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data()); var bytes = new byte[16]; for (var i = 0; i < 4; i++) BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * 4), .5f); for (var i = 0; i < 24; i++) mesh.SurfaceUpdateAttributeRegion(0, 20, bytes); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) mesh.SurfaceUpdateAttributeRegion(0, 20, bytes); Check(before == GC.GetAllocatedBytesForCurrentThread(), "Warmed custom packed updates reuse decoded storage with zero managed allocation.");
    }
}
