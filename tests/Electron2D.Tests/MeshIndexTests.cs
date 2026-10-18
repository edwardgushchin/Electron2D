using System.Buffers.Binary;
using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;

internal static class MeshIndexTests
{
    private static MeshSurfaceData Data(int count = 6)
    {
        var vertices = new Vector2[count];
        vertices[0] = new(0, 0); vertices[1] = new(12, 0); vertices[2] = new(0, 12);
        vertices[count - 3] = new(20, 0); vertices[count - 2] = new(32, 0); vertices[count - 1] = new(20, 12);
        return new() { Vertices = vertices, Indices = [0, 1, 2] };
    }
    private static byte[] Indices(int count, bool right)
    {
        var stride = count <= 65536 ? 2 : 4; var bytes = new byte[stride * 3];
        for (var i = 0; i < 3; i++)
            if (stride == 2) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i * stride), (ushort)(right ? count - 3 + i : i));
            else BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * stride), (uint)(right ? count - 3 + i : i));
        return bytes;
    }
    internal static void Run()
    {
        foreach (var count in new[] { 6, 65536, 65537 })
        {
            using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data(count));
            var changed = 0; mesh.Changed += _ => changed++;
            var right = Indices(count, true); var left = Indices(count, false); var stride = right.Length / 3;
            mesh.UpdateIndexRegion(0, 0, right);
            Check(mesh.SurfaceGetArrays(0).Indices.SequenceEqual(new[] { count - 3, count - 2, count - 1 }) && changed == 1, "Both widths publish current topology.");
            mesh.UpdateIndexRegion(0, 1, left.AsSpan(1, stride));
            var expected = (byte[])right.Clone(); left.AsSpan(1, stride).CopyTo(expected.AsSpan(1)); var actual = mesh.SurfaceGetArrays(0).Indices;
            for (var i = 0; i < 3; i++) Check(actual[i] == (stride == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(expected.AsSpan(i * stride)) : BinaryPrimitives.ReadUInt32LittleEndian(expected.AsSpan(i * stride))), "Unaligned edits preserve untouched bytes across records.");
            mesh.UpdateIndexRegion(0, 0, left); var before = changed; var bad = (byte[])right.Clone(); bad.AsSpan(stride * 2).Fill(255);
            if (count != 65536) Reject<ArgumentOutOfRangeException>(() => mesh.UpdateIndexRegion(0, 0, bad));
            Check(changed == before && mesh.SurfaceGetArrays(0).Indices.SequenceEqual(new[] { 0, 1, 2 }), "A later invalid index preserves the complete prior buffer and notifications.");
            Reject<ArgumentOutOfRangeException>(() => mesh.UpdateIndexRegion(0, -1, [])); Reject<ArgumentOutOfRangeException>(() => mesh.UpdateIndexRegion(0, int.MaxValue, [1]));
            Reject<ArgumentOutOfRangeException>(() => mesh.UpdateIndexRegion(0, left.Length, [1])); Reject<ArgumentOutOfRangeException>(() => mesh.UpdateIndexRegion(1, 0, left));
            mesh.UpdateIndexRegion(0, left.Length, []); Check(changed == before + 1, "Empty valid regions follow sibling notification policy.");
            Action<Resource> failure = _ => throw new InvalidOperationException("index observer"); mesh.Changed += failure;
            Reject<InvalidOperationException>(() => mesh.UpdateIndexRegion(0, 0, right)); mesh.Changed -= failure;
            Check(mesh.SurfaceGetArrays(0).Indices[2] == count - 1, "Observer failure retains committed indices.");
            using var copy = (ArrayMesh)mesh.Duplicate(); mesh.UpdateIndexRegion(0, 0, left); Check(copy.SurfaceGetArrays(0).Indices[2] == count - 1, "Duplication owns independent edited indices.");
            for (var i = 0; i < 128; i++) mesh.UpdateIndexRegion(0, 0, (i & 1) == 0 ? right : left);
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) mesh.UpdateIndexRegion(0, 0, (i & 1) == 0 ? right : left);
            Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all, "128 prepared updates allocate zero owner/all-thread managed bytes.");
        }
        foreach (var primitive in Enum.GetValues<Mesh.PrimitiveType>())
        {
            using var mesh = new ArrayMesh(); var data = Data(); if (primitive == Mesh.PrimitiveType.Lines) data.Indices = [0, 1];
            mesh.AddSurfaceFromArrays(primitive, data); mesh.UpdateIndexRegion(0, 0, [3]); Check(mesh.SurfaceGetPrimitiveType(0) == primitive && mesh.SurfaceGetArrays(0).Indices[0] == 3, "All topologies retain their format/count.");
        }
        using var unindexed = new ArrayMesh(); var sequential = Data(); sequential.Indices = []; unindexed.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, sequential);
        Reject<InvalidOperationException>(() => unindexed.UpdateIndexRegion(0, 0, []));
        using var lod = new ArrayMesh(); var baseData = Data(); baseData.Indices = [0, 1, 2, 3, 4, 5];
        lod.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, baseData, lods: new Dictionary<float, int[]> { [2] = [0, 1, 2] }); lod.UpdateIndexRegion(0, 0, [3]);
        Check(lod.SurfaceGetLODs(0)[2][0] == 0, "Base updates preserve independent LOD indices."); Fresh(lod);
        unindexed.Dispose(); Reject<ObjectDisposedException>(() => unindexed.UpdateIndexRegion(0, 0, []));
        Console.WriteLine("Mesh index contracts: both widths, byte boundaries, all topologies, atomic failure, LODs, archives and 128 zero-byte updates passed.");
    }
    private static void Fresh(ArrayMesh mesh)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-indices-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(mesh, path); var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(typeof(MeshIndexTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_MESH_INDICES"); start.Environment.Remove("ELECTRON2D_TEST_MESH_INDICES_NATIVE"); start.Environment["ELECTRON2D_TEST_MESH_INDICES_CHILD"] = path;
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Mesh index archive child."); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh mesh indices passed"), error.GetAwaiter().GetResult());
        }
        finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        using var mesh = ResourceLoader.Load<ArrayMesh>(path, ResourceLoader.CacheMode.Ignore); Check(mesh.SurfaceGetArrays(0).Indices[0] == 3 && mesh.SurfaceGetLODs(0)[2][0] == 0, "Fresh files preserve edited base and independent LOD indices."); Console.WriteLine("Fresh mesh indices passed.");
    }
    internal static void Native(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var window = new Window { Size = new(112, 92) }; var frame = 0; var rows = new RID[2]; var right = new[] { Indices(6, true), Indices(65537, true) }; var left = new[] { Indices(6, false), Indices(65537, false) };
        var draws = new List<Draw>(); var recorded = new int[3]; var before = 0L; var active = 0L; var idle = 0L;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested backend executes without fallback."); RenderingServer.SetDefaultClearColor(Colors.Black); Layout();
            for (var i = 0; i < 2; i++) { rows[i] = RenderingServer.MeshCreate(); RenderingServer.MeshAddSurfaceFromArrays(rows[i], Mesh.PrimitiveType.Triangles, Data(i == 0 ? 6 : 65537)); var draw = new Draw(rows[i], false) { Position = new(4, 4 + i * 24) }; draws.Add(draw); window.AddChild(draw); }
            var multi = RenderingServer.MultiMeshCreate(); RenderingServer.MultiMeshAllocateData(multi, 2); RenderingServer.MultiMeshSetMesh(multi, rows[0]); RenderingServer.MultiMeshInstanceSetTransform2D(multi, 0, Transform.Identity); RenderingServer.MultiMeshInstanceSetTransform2D(multi, 1, new Transform(0, new(44, 0)));
            var instances = new Draw(multi, true) { Position = new(4, 52) }; draws.Add(instances); window.AddChild(instances);
            using var borrowed = new ArrayMesh(); borrowed.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data()); Reject<InvalidOperationException>(() => RenderingServer.MeshSurfaceUpdateIndexRegion(borrowed.GetRID(), 0, 0, right[0]));
            Reject<ArgumentException>(() => RenderingServer.MeshSurfaceUpdateIndexRegion(default, 0, 0, right[0])); Task.Run(() => Reject<InvalidOperationException>(() => RenderingServer.MeshSurfaceUpdateIndexRegion(rows[0], 0, 0, right[0]))).GetAwaiter().GetResult();
            window.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); if (frame < 80) for (var i = 0; i < 2; i++) RenderingServer.MeshSurfaceUpdateIndexRegion(rows[i], 0, 0, (frame & 1) != 0 ? right[i] : left[i]); };
            RenderingServer.FramePostDraw += () =>
            {
                var allocated = GC.GetAllocatedBytesForCurrentThread() - before; if (frame >= 16 && frame < 80) active += allocated; else if (frame >= 80) idle += allocated;
                if (frame is 0 or 1 or 80) { using var image = window.GetTexture().GetImage()!; var shifted = frame != 0; foreach (var y in new[] { 7, 31, 55 }) { Pixel(image, 7, y, !shifted); Pixel(image, 27, y, shifted); } Pixel(image, 51, 55, !shifted); Pixel(image, 71, 55, shifted); }
                if (frame == 15) for (var i = 0; i < draws.Count; i++) recorded[i] = draws[i].Calls;
                if (++frame == 144) window.Tree!.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && frame == 144 && draws.Select((draw, i) => draw.Calls > 0 && draw.Calls == recorded[i]).All(unchanged => unchanged), $"Retained base and MultiMesh commands: frames={frame}, drawings={string.Join(',', draws.Select(draw => draw.Calls))}.");
        Check(active == 0 && idle == 0, $"64 active/64 idle complete owner frames allocate zero managed bytes: {active}/{idle}."); Reject<InvalidOperationException>(() => RenderingServer.MeshSurfaceGetFormatIndexStride(Mesh.ArrayFormat.Index, 3));
        Console.WriteLine($"Mesh index native {backend}: both widths, MultiMesh, live pixels, no prepared rerecording, 64 active/64 idle owner frames 0 B passed.");
    }
    private static void Layout()
    {
        const Mesh.ArrayFormat all = Mesh.ArrayFormat.Vertex | Mesh.ArrayFormat.Use2DVertices | Mesh.ArrayFormat.Normal | Mesh.ArrayFormat.Tangent | Mesh.ArrayFormat.Color | Mesh.ArrayFormat.TexUV | Mesh.ArrayFormat.TexUV2 | Mesh.ArrayFormat.Bones | Mesh.ArrayFormat.Weights | Mesh.ArrayFormat.Index;
        foreach (var count in new[] { 0, 1, 65535, 65536, 65537 }) Check(RenderingServer.MeshSurfaceGetFormatIndexStride(Mesh.ArrayFormat.Index, count) == (count is > 0 and <= 65536 ? 2 : 4), "Exact index-width transition.");
        Check(RenderingServer.MeshSurfaceGetFormatIndexStride(Mesh.ArrayFormat.None, 6) == 0, "Absent indices have zero stride.");
        var channels = new[] { Mesh.ArrayFormat.Vertex, Mesh.ArrayFormat.Normal, Mesh.ArrayFormat.Tangent, Mesh.ArrayFormat.Color, Mesh.ArrayFormat.TexUV, Mesh.ArrayFormat.TexUV2, Mesh.ArrayFormat.Bones, Mesh.ArrayFormat.Weights, Mesh.ArrayFormat.Index }; var expected = new[] { 0, 32, 36, 0, 4, 12, 0, 8, 0 };
        for (var i = 0; i < channels.Length; i++) Check(RenderingServer.MeshSurfaceGetFormatOffset(all, 4, channels[i]) == expected[i], "Offsets address independent buffers.");
        Check(RenderingServer.MeshSurfaceGetFormatOffset(all | Mesh.ArrayFormat.CompressAttributes, 4, Mesh.ArrayFormat.TexUV2) == 8 && RenderingServer.MeshSurfaceGetFormatOffset(all | Mesh.ArrayFormat.Use8BoneWeights, 4, Mesh.ArrayFormat.Weights) == 16, "Compressed UV and eight-slot offsets match storage.");
        var custom = all | Mesh.ArrayFormat.Custom0 | Mesh.ArrayFormat.Custom1 | Mesh.ArrayFormat.Custom2 | Mesh.ArrayFormat.Custom3 | (Mesh.ArrayFormat)((ulong)MeshCustomFormat.RGBFloat << 13 | (ulong)MeshCustomFormat.RGBAHalf << 16 | (ulong)MeshCustomFormat.RGFloat << 19);
        Check(RenderingServer.MeshSurfaceGetFormatOffset(custom, 4, Mesh.ArrayFormat.Custom3) == 48 && RenderingServer.MeshSurfaceGetFormatOffset(Mesh.ArrayFormat.None, 4, Mesh.ArrayFormat.Normal) == 0, "Custom layouts and absent channels.");
        var owned = RenderingServer.MeshCreate(); var data = Data(); data.Colors = Enumerable.Repeat(Colors.White, 6).ToArray(); data.UVs = new Vector2[6]; data.UV2s = new Vector2[6]; data.Normals = Enumerable.Repeat(new Vector3(0, 0, 1), 6).ToArray();
        RenderingServer.MeshAddSurfaceFromArrays(owned, Mesh.PrimitiveType.Triangles, data); var format = RenderingServer.MeshSurfaceGetFormat(owned, 0); var patch = new byte[8];
        BinaryPrimitives.WriteSingleLittleEndian(patch, .25f); BinaryPrimitives.WriteSingleLittleEndian(patch.AsSpan(4), .75f);
        RenderingServer.MeshSurfaceUpdateAttributeRegion(owned, 0, RenderingServer.MeshSurfaceGetFormatOffset(format, 6, Mesh.ArrayFormat.TexUV2), patch);
        BinaryPrimitives.WriteUInt32LittleEndian(patch, 0x7fffffff);
        RenderingServer.MeshSurfaceUpdateVertexRegion(owned, 0, RenderingServer.MeshSurfaceGetFormatOffset(format, 6, Mesh.ArrayFormat.Normal), patch.AsSpan(0, 4));
        var result = RenderingServer.MeshSurfaceGetArrays(owned, 0); Check(result.UV2s[0] == new Vector2(.25f, .75f) && result.UVs[0] == Vector2.Zero && result.Normals[0].X > .99f && result.Vertices[0] == Vector2.Zero, "Queried offsets edit real independent buffers without changing positions or primary UV."); RenderingServer.FreeRID(owned);
        Reject<ArgumentOutOfRangeException>(() => RenderingServer.MeshSurfaceGetFormatOffset(all, 4, Mesh.ArrayFormat.BlendShapeMask)); Reject<OverflowException>(() => RenderingServer.MeshSurfaceGetFormatOffset(all, int.MaxValue, Mesh.ArrayFormat.Normal));
        Reject<ArgumentOutOfRangeException>(() => RenderingServer.MeshSurfaceGetFormatIndexStride(all, -1)); Reject<NotSupportedException>(() => RenderingServer.MeshSurfaceGetFormatIndexStride(Mesh.ArrayFormat.Vertex, 4));
    }
    private static void Pixel(Image image, int x, int y, bool white) { var color = image.GetPixel(x, y); Check(white ? color.R > .9f && color.G > .9f : color.R < .05f && color.G < .05f, $"Index pixel ({x},{y}) white={white}: {color}."); }
    private sealed class Draw(RID mesh, bool multi) : Entity
    {
        internal int Calls;
        protected override void OnDraw() { Calls++; if (multi) DrawMultiMesh(mesh); else DrawMesh(mesh); }
    }
}
