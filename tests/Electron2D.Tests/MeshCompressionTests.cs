using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;
using Electron2D;
using static TextureArrayTests;

internal static class MeshCompressionTests
{
    internal static ArrayMesh Mesh(bool shapes = false)
    {
        var data = MeshCustomTests.Data(); data.UV2s = (Vector2[])data.UVs.Clone(); data.Colors = Enumerable.Repeat(Colors.White, data.Vertices.Length).ToArray(); var mesh = new ArrayMesh(); if (shapes) mesh.AddBlendShape("move"); mesh.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, data, shapes ? [MeshMorphTests.Pose(data, new(8, 0))] : null, new Dictionary<float, int[]> { [1] = [0, 1, 2] }, Electron2D.Mesh.ArrayFormat.CompressAttributes); return mesh;
    }
    internal static void Run()
    {
        Precision(); Oracle(); Updates(); Archives(); Integration(); Morph(); Warm(); Fresh(); Console.WriteLine("Compression: finite packed UV/frame precision, exact partial updates, transactional errors, morph/LOD/skin/custom preservation, archives/fresh scenes and zero warmed edits passed.");
    }
    private static void Precision()
    {
        using var mesh = Mesh(); var s = mesh.Get(0); var data = mesh.SurfaceGetArrays(0); Check(s.Compression is not null && s.FrameWords.Length == 0 && s.Compression.Frames.Length == data.Vertices.Length * 6 && s.Compression.UVs.Length == data.Vertices.Length * 4, "Canonical UV/frame storage uses four/six-byte records."); Check(MeshCustomCodec.AttributeStride(s.Format) == MeshCustomCodec.AttributeStride(s.Format & ~Electron2D.Mesh.ArrayFormat.CompressAttributes) - 8, "Both UV channels save four bytes each."); Check(data.Normals.All(n => n.Z < -.999f) && data.Tangents.All(t => t.X > .999f && t.W > 0), "Compressed numeric frame survives the 2D vertex profile.");
        using var scaled = new ArrayMesh(); var input = MeshCustomTests.Data(); input.UVs = [new(-2, 0), new(2, 0), new(2, 0), new(-2, 0)]; input.UV2s = [new(0, -3), new(0, 3), new(0, 3), new(0, -3)]; scaled.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, input, flags: Electron2D.Mesh.ArrayFormat.CompressAttributes); var result = scaled.SurfaceGetArrays(0); Check(result.UVs.All(v => v.IsFinite() && v.Y == 0) && result.UV2s.All(v => v.IsFinite() && v.X == 0), "Scaled zero components avoid division by zero and remain zero."); for (var i = 0; i < 4; i++) Check(Math.Abs(input.UVs[i].X - result.UVs[i].X) <= 4f / 65535 && Math.Abs(input.UV2s[i].Y - result.UV2s[i].Y) <= 6f / 65535, "Scaled coordinates meet one-quantum error bounds.");
        input.UVs[0] = new(float.MaxValue, 0); Reject<ArgumentException>(() => scaled.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, input, flags: Electron2D.Mesh.ArrayFormat.CompressAttributes)); Check(scaled.GetSurfaceCount() == 1, "Unrepresentable compression scale rejects before geometry commit."); input = MeshCustomTests.Data(); input.Normals = []; Reject<ArgumentException>(() => scaled.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, input, flags: Electron2D.Mesh.ArrayFormat.CompressAttributes));
        input = MeshCustomTests.Data(); input.Tangents = []; using var normalOnly = new ArrayMesh(); normalOnly.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, input, flags: Electron2D.Mesh.ArrayFormat.CompressAttributes); Check(normalOnly.SurfaceGetArrays(0).Tangents.Length == 4, "Normal-only imports synthesize a valid compressed tangent frame.");
        Span<byte> bytes = stackalloc byte[6];
        foreach (var angle in new[] { 0f, .000001f, .2f, 1f, MathF.PI - .000001f, MathF.PI }) foreach (var sign in new[] { -1f, 1f })
            {
                var normal = new Vector3(0, 0, 1); var tangent = new Vector4(MathF.Cos(angle), MathF.Sin(angle), 0, sign); MeshCompression.WriteFrame(normal, tangent, bytes); MeshCompression.ReadFrame(bytes, out var n, out var t); Check(n.IsFinite() && t.IsFinite() && t.W == sign && Math.Abs(n.Length() - 1) < .00001f && Math.Abs(new Vector3(t.X, t.Y, t.Z).Length() - 1) < .00001f, "Identity/pi/handedness frame endpoints stay finite and normalized."); Check((n - normal).Length() < .001f && (new Vector3(t.X, t.Y, t.Z) - new Vector3(tangent.X, tangent.Y, tangent.Z)).Length() < .001f, "Joint angle/axis compression retains a bounded frame error.");
            }
    }
    private static void Oracle()
    {
        using var stream = typeof(MeshCompressionTests).Assembly.GetManifestResourceStream("Electron2D.Mesh.CompressionOracle")!; using var json = JsonDocument.Parse(stream); Span<byte> bytes = stackalloc byte[6]; Span<byte> reference = stackalloc byte[6]; var count = 0;
        foreach (var row in json.RootElement.GetProperty("records").EnumerateArray())
        {
            var n = row.GetProperty("normal"); var t = row.GetProperty("tangent"); MeshCompression.WriteFrame(new(n[0].GetSingle(), n[1].GetSingle(), n[2].GetSingle()), new(t[0].GetSingle(), t[1].GetSingle(), t[2].GetSingle(), t[3].GetSingle()), bytes); var word = BinaryPrimitives.ReadUInt32LittleEndian(bytes); var expected = row.GetProperty("axis_word").GetUInt32(); var angle = BinaryPrimitives.ReadUInt16LittleEndian(bytes[4..]);
            var dx = Math.Abs((long)(word & 65535) - (expected & 65535)); var dy = Math.Abs((long)(word >> 16) - (expected >> 16)); var da = Math.Abs(angle - row.GetProperty("angle_word").GetInt32());
            Check(dx <= 8 && dy <= 8 && da <= 1, "Reference words: axis cancellation near pi is bounded to eight quanta; angle to one.");
            BinaryPrimitives.WriteUInt32LittleEndian(reference, expected); BinaryPrimitives.WriteUInt16LittleEndian(reference[4..], (ushort)row.GetProperty("angle_word").GetInt32()); MeshCompression.ReadFrame(reference, out var rn, out var rt); MeshCompression.ReadFrame(bytes, out var an, out var at); Check((rn - an).Length() < .001f && (new Vector3(rt.X, rt.Y, rt.Z) - new Vector3(at.X, at.Y, at.Z)).Length() < .001f && rt.W == at.W, "Pinned reference reconstructed frame precision: " + count); Check((an - new Vector3(n[0].GetSingle(), n[1].GetSingle(), n[2].GetSingle())).Length() < .001f && (new Vector3(at.X, at.Y, at.Z) - new Vector3(t[0].GetSingle(), t[1].GetSingle(), t[2].GetSingle())).Length() < .001f, "Authored orthonormal frame error stays below 0.001."); count++;
        }
        Check(count == 70, "Independent pinned frame fixture count.");
    }
    private static void Updates()
    {
        using var mesh = Mesh(); var s = mesh.Get(0); var stride = MeshCustomCodec.AttributeStride(s.Format); var patch = new byte[stride * 2]; var original = s.Compression!.UVs.ToArray();
        for (var i = 0; i < 2; i++) { var row = patch.AsSpan(i * stride); row[3] = 255; BinaryPrimitives.WriteUInt16LittleEndian(row[4..], 32767); BinaryPrimitives.WriteUInt16LittleEndian(row[6..], 65535); row[11] = 128; for (var c = 0; c < 4; c++) BinaryPrimitives.WriteSingleLittleEndian(row[(24 + c * 4)..], .5f); }
        BinaryPrimitives.WriteSingleLittleEndian(patch.AsSpan(stride + 24), float.NaN); Reject<ArgumentException>(() => mesh.SurfaceUpdateAttributeRegion(0, 0, patch)); Check(s.Compression.UVs.SequenceEqual(original) && mesh.SurfaceGetArrays(0).Colors[0] == Colors.White, "Later custom NaN rolls back earlier compressed UV/color records."); BinaryPrimitives.WriteSingleLittleEndian(patch.AsSpan(stride + 24), .5f); mesh.SurfaceUpdateAttributeRegion(0, 0, patch); Check(s.Data.UVs[0].X == 32767 / 65535f && s.Data.UVs[0].Y == 1 && s.Data.Custom3!.Values[0].X == .5f, "Compressed offsets and custom channels update through the packed stride.");
        var before = s.Compression.Frames.ToArray(); mesh.SurfaceUpdateVertexRegion(0, 32 + 5, new byte[] { (byte)(before[5] ^ 1) }); Check(s.Compression.Frames[5] == (byte)(before[5] ^ 1) && s.Compression.Frames[4] == before[4], "Partial six-byte frame patch preserves adjacent canonical bytes."); var invalid = new byte[10]; BinaryPrimitives.WriteSingleLittleEndian(invalid, float.NaN); invalid[8] = 0; var frame = s.Compression.Frames.ToArray(); Reject<ArgumentException>(() => mesh.SurfaceUpdateVertexRegion(0, 24, invalid)); Check(s.Compression.Frames.SequenceEqual(frame), "Invalid final position rolls back a following compressed frame.");
        mesh.RegenNormalMaps(); Check(mesh.Get(0).Compression!.UVs.SequenceEqual(s.Compression.UVs) && mesh.SurfaceGetLODs(0).Count == 1, "Regeneration preserves exact packed UV metadata and LODs.");
    }
    private static void Archives()
    {
        using var mesh = Mesh(true); var p = (PropertyDescriptor<ArrayMesh, byte[]>)mesh.GetPropertyList().Single(p => p.Name == "_surface_data"); var bytes = p.GetValue(mesh); Check(BinaryPrimitives.ReadUInt32LittleEndian(bytes) == 0x38534d41, "AMS8 version."); using var copy = new ArrayMesh(); p.SetValue(copy, bytes); Check(copy.Get(0).Compression!.Frames.SequenceEqual(mesh.Get(0).Compression!.Frames) && copy.Get(0).Compression!.UVs.SequenceEqual(mesh.Get(0).Compression!.UVs) && copy.GetBlendShapeCount() == 1 && copy.SurfaceGetLODs(0).Count == 1, "Archive preserves exact canonical records, shapes and levels."); using var duplicate = (ArrayMesh)mesh.Duplicate(); Check(duplicate.Get(0).Compression!.Frames.SequenceEqual(mesh.Get(0).Compression!.Frames), "Duplication preserves exact compressed bits."); Reject<InvalidDataException>(() => p.SetValue(copy, bytes[..^1])); var broken = (byte[])bytes.Clone(); var scaleOffset = 12 + 4 + "move".Length * 2 + 80 + 12; BinaryPrimitives.WriteSingleLittleEndian(broken.AsSpan(scaleOffset), float.NaN); Reject<InvalidDataException>(() => p.SetValue(copy, broken)); Check(copy.Get(0).Compression!.Frames.SequenceEqual(mesh.Get(0).Compression!.Frames), "Malformed metadata retains old geometry.");
    }
    private static void Integration()
    {
        using var custom = new Custom(); var command = new CanvasMesh(custom, Transform.Identity, Colors.White); var vertices = new List<CanvasVertex>(); command.Append(vertices, [], null, Transform.Identity, Colors.White, null, BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false); Check(vertices[0].UV.X == 32767 / 65535f, "Custom format callbacks execute compression during cold preparation."); custom.Fail = true; custom.EmitChanged(); vertices.Clear(); Reject<InvalidOperationException>(() => command.Append(vertices, [], null, Transform.Identity, Colors.White, null, BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false)); custom.Fail = false; command.Append(vertices, [], null, Transform.Identity, Colors.White, null, BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false); Check(vertices.Count > 0, "Format callback failure permits stable snapshot recovery.");
        using var mesh = Mesh(true); using var tool = new SurfaceTool(); tool.CreateFromBlendShape(mesh, 0, "move"); var pose = tool.CommitToArrays(); Check(pose.Vertices[0].X == 8 && pose.UV2s.SequenceEqual(mesh.SurfaceGetArrays(0).UV2s) && pose.Custom3!.Data.SequenceEqual(mesh.SurfaceGetArrays(0).Custom3!.Data), "Compressed target extraction preserves the raw target and decoded base attributes.");
        using var authored = tool.Commit(flags: Electron2D.Mesh.ArrayFormat.CompressAttributes); Check(authored.Get(0).Compression is not null && authored.SurfaceGetArrays(0).Normals[0].Z > .999f, "SurfaceTool compressed commit executes shared storage.");
        using var editor = new MeshDataTool(); editor.CreateFromSurface(mesh, 0); using var edited = new ArrayMesh(); editor.CommitToSurface(edited, Electron2D.Mesh.ArrayFormat.CompressAttributes); Check(edited.Get(0).Compression is not null && edited.SurfaceGetArrays(0).Custom3!.Data.SequenceEqual(mesh.SurfaceGetArrays(0).Custom3!.Data), "MeshDataTool accepts and recommits decoded compressed channels.");
        using var normalOnly = new ArrayMesh(); normalOnly.AddBlendShape("normal"); var data = MeshFrameTests.Data(); data.Tangents = []; var shape = MeshMorphTests.Pose(data, Vector2.Zero); normalOnly.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, data, [shape], flags: Electron2D.Mesh.ArrayFormat.CompressAttributes); Check(normalOnly.SurfaceGetArrays(0).Tangents.Length == 4 && normalOnly.SurfaceGetBlendShapeArrays(0)[0].Tangents.Length == 0, "Compressed normal-only targets retain the authored channel mask.");
        using var ordinary = new ArrayMesh(); ordinary.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, MeshFrameTests.Data()); var property = (PropertyDescriptor<ArrayMesh, byte[]>)ordinary.GetPropertyList().Single(p => p.Name == "_surface_data"); var current = property.GetValue(ordinary); var ams6 = new byte[current.Length - 16]; current.AsSpan(0, 104).CopyTo(ams6); current.AsSpan(120).CopyTo(ams6.AsSpan(104)); BinaryPrimitives.WriteUInt32LittleEndian(ams6, 0x36534d41); using var legacy = new ArrayMesh(); property.SetValue(legacy, ams6); Check(legacy.Get(0).FrameWords.SequenceEqual(ordinary.Get(0).FrameWords), "AMS6 remains readable with exact uncompressed frame words.");
        foreach (var primitive in Enum.GetValues<Electron2D.Mesh.PrimitiveType>())
        {
            data = MeshFrameTests.Data(); data.Indices = primitive == Electron2D.Mesh.PrimitiveType.Lines ? [0, 1, 2, 3] : primitive == Electron2D.Mesh.PrimitiveType.TriangleStrip ? [0, 1, 3, 2] : primitive == Electron2D.Mesh.PrimitiveType.Triangles ? data.Indices : []; using var topology = new ArrayMesh(); topology.AddSurfaceFromArrays(primitive, data, flags: Electron2D.Mesh.ArrayFormat.CompressAttributes); Check(MeshLODTests.Draw(topology, Transform.Identity).Count > 0, "Compressed channels execute every topology.");
        }
        using var observed = Mesh(); Action<Resource> fail = _ => throw new InvalidOperationException("listener"); observed.Changed += fail; var patch = new byte[2]; BinaryPrimitives.WriteUInt16LittleEndian(patch, 12345); Reject<InvalidOperationException>(() => observed.SurfaceUpdateAttributeRegion(0, 4, patch)); observed.Changed -= fail; Check(observed.Get(0).Compression!.UVs[0] == patch[0], "Listener failure preserves an already committed compressed edit."); observed.Dispose(); Reject<ObjectDisposedException>(() => observed.SurfaceUpdateAttributeRegion(0, 4, patch));
        data = MeshFrameTests.Data(); data.UVs = Enumerable.Repeat(new Vector2(-.000001f, 0), 4).ToArray(); using var tiny = new ArrayMesh(); tiny.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, data, flags: Electron2D.Mesh.ArrayFormat.CompressAttributes); Check(tiny.SurfaceGetArrays(0).UVs.All(v => v == Vector2.Zero), "Whole-vector approximate-zero scale retains pinned tiny-coordinate clamp policy.");
    }
    private static void Morph()
    {
        using var mesh = Mesh(true); using var root = new Node(); var instance = new MeshInstance { Mesh = mesh }; root.AddChild(instance); using var tree = new SceneTree(root); instance.SetBlendShapeValue(0, .5f); instance.PrepareCanvas(); var vertices = new List<CanvasVertex>(); instance.AppendCanvas(vertices, [], Transform.Identity); Check(vertices[0].Position.X == 4 && vertices[0].Normal.Z < -.999f, "Independent morph output keeps compressed base frame and attributes.");
    }
    private static void Warm()
    {
        using var mesh = Mesh(true); using var root = new Node(); var node = new MeshInstance { Mesh = mesh }; root.AddChild(node); using var tree = new SceneTree(root); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); var patch = new byte[2];
        void Step(int i) { BinaryPrimitives.WriteUInt16LittleEndian(patch, (ushort)(i % 2 == 0 ? 32767 : 65535)); mesh.SurfaceUpdateAttributeRegion(0, 4, patch); node.SetBlendShapeValue(0, i % 2 == 0 ? .5f : 1); node.PrepareCanvas(); vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity); }
        for (var i = 0; i < 24; i++) Step(i); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Step(i); Check(before == GC.GetAllocatedBytesForCurrentThread(), "Warmed compressed edits/morph preparation/replay allocate zero managed bytes.");
    }
    private static void Fresh()
    {
        using var mesh = Mesh(true); using var root = new Node(); var node = new MeshInstance { Name = "Mesh", Mesh = mesh }; root.AddChild(node); node.Owner = root; node.SetBlendShapeValue(0, .5f); using var scene = new PackedScene(); scene.Pack(root); var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-compress-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try { ResourceSaver.Save(scene, path); var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(MeshCompressionTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_MESH_COMPRESSION"); start.Environment.Remove("ELECTRON2D_TEST_MESH_COMPRESSION_NATIVE"); start.Environment["ELECTRON2D_TEST_MESH_COMPRESSION_CHILD"] = path; using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Compression scene child."); } Check(child.ExitCode == 0 && stdout.GetAwaiter().GetResult().Contains("Fresh compressed scene passed"), stderr.GetAwaiter().GetResult()); } finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var root = scene.Instantiate(); var node = (MeshInstance)root.GetNode("Mesh"); using var tree = new SceneTree(root); node.PrepareCanvas(); var vertices = new List<CanvasVertex>(); node.AppendCanvas(vertices, [], Transform.Identity); Check(((ArrayMesh)node.Mesh!).Get(0).Compression is not null && vertices[0].Position.X == 4 && vertices[0].Normal.Z < -.999f, "Fresh scene restores compressed data and actual morph drawing."); Console.WriteLine("Fresh compressed scene passed.");
    }
    internal sealed class Custom : Electron2D.Mesh
    {
        internal bool Fail;
        private readonly MeshSurfaceData _data;
        internal Custom() { _data = MeshCustomTests.Data(); _data.UVs = Enumerable.Repeat(new Vector2(.5f, .5f), 4).ToArray(); _data.UV2s = (Vector2[])_data.UVs.Clone(); }
        protected override int OnGetSurfaceCount() => 1;
        protected override MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex) => _data.Copy();
        protected override int OnSurfaceGetArrayLen(int surfaceIndex) => 4;
        protected override int OnSurfaceGetArrayIndexLen(int surfaceIndex) => 6;
        protected override PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex) => PrimitiveType.Triangles;
        protected override ArrayFormat OnSurfaceGetFormat(int surfaceIndex) => Fail ? throw new InvalidOperationException("format callback") : ArrayMesh.Surface.Channels(_data) | ArrayFormat.CompressAttributes;
        protected override Material? OnSurfaceGetMaterial(int surfaceIndex) => null;
        protected override void OnSurfaceSetMaterial(int surfaceIndex, Material? material) { }
    }

}
