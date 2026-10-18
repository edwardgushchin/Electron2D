using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;

internal static partial class ProceduralMeshTests
{
    private const Mesh.ArrayFormat Empty = Mesh.ArrayFormat.UseEmptyVertexArray;
    private static MeshSurfaceData Data(Mesh.PrimitiveType primitive = Mesh.PrimitiveType.Triangles, int count = 4) => new()
    {
        VertexCount = count,
        Indices = primitive switch { Mesh.PrimitiveType.Points => [0, 3], Mesh.PrimitiveType.Lines => [0, 1], Mesh.PrimitiveType.LineStrip => [0, 1, 3], Mesh.PrimitiveType.TriangleStrip => [0, 1, 2, 3], _ => [0, 1, 2, 1, 3, 2] }
    };
    private static ArrayMesh CreateMesh(Mesh.PrimitiveType primitive = Electron2D.Mesh.PrimitiveType.Triangles, MeshSurfaceData? data = null, Mesh.ArrayFormat flags = Empty)
    { var mesh = new ArrayMesh { CustomAABB = new(-1, -1, 28, 28) }; mesh.AddSurfaceFromArrays(primitive, data ?? Data(primitive), flags: flags); return mesh; }
    private static byte[] Module(string name) { using var source = typeof(ProceduralMeshTests).Assembly.GetManifestResourceStream("Electron2D.Device." + name)!; using var memory = new MemoryStream(); source.CopyTo(memory); return memory.ToArray(); }
    private static Shader Program(string language, bool modern = false)
    { var code = Module($"DeviceProcedural{language}.vert.spv"); return Shader.CreateFromSPIRV(modern ? MeshVertexIDTests.Modern(code) : code, Module($"DeviceVertexID{language}.frag.spv")); }
    internal static void Run()
    {
        using var mesh = CreateMesh(); Check(mesh.SurfaceGetArrayLen(0) == 4 && mesh.SurfaceGetArrays(0).Vertices.Length == 0 && (mesh.SurfaceGetFormat(0) & Empty) != 0, "Count and absent positions are independent.");
        Check(mesh.GetFaces().Length == 0 && mesh.GetAABB() == default && mesh.CustomAABB.HasArea(), "Procedural geometry has no fabricated CPU faces or bounds.");
        using var copy = (ArrayMesh)mesh.Duplicate(true); Check(copy.SurfaceGetArrays(0).VertexCount == 4 && copy.SurfaceGetArrays(0).Vertices.Length == 0, "Copies preserve absent positions and count.");
        var archive = mesh.SaveSurfaces(); using var restored = new ArrayMesh(); restored.LoadSurfaces(archive); Check(restored.SurfaceGetArrayLen(0) == 4 && restored.SurfaceGetArrays(0).Vertices.Length == 0, "AMS8 preserves counts without position records.");
        var payload = mesh.ExportPacked(0); Check(payload.VertexCount == 4 && payload.VertexData.Length == 0, "Packed position buffer stays empty."); using var packed = new ArrayMesh(); packed.AppendPacked(ArrayMesh.PreparePacked(payload, 0)); Check(packed.SurfaceGetArrayLen(0) == 4 && packed.SurfaceGetArrays(0).Vertices.Length == 0, "Packed count roundtrip.");
        using var indexed = CreateMesh(data: new() { Indices = [0, 1, 2] }); var zero = indexed.ExportPacked(0); Check(zero.VertexCount == 0 && zero.IndexData.Length == 6, "Index-only geometry preserves the native zero count/16-bit layout."); using var zeroPacked = new ArrayMesh(); zeroPacked.AppendPacked(ArrayMesh.PreparePacked(zero, 0)); zeroPacked.UpdateIndexRegion(0, 0, [2, 0]); Check(zeroPacked.SurfaceGetArrays(0).Indices[0] == 2, "Index-only live updates retain valid 16-bit identities.");
        using var zeroArchive = new ArrayMesh(); zeroArchive.LoadSurfaces(indexed.SaveSurfaces()); Check(zeroArchive.SurfaceGetArrayLen(0) == 0 && zeroArchive.SurfaceGetArrays(0).Indices.SequenceEqual(new[] { 0, 1, 2 }), "Index-only archives retain invocation order.");
        using var declared = CreateMesh(data: new() { Indices = [0, 1, 2] }, flags: Empty | Electron2D.Mesh.ArrayFormat.Normal | Electron2D.Mesh.ArrayFormat.Color | Electron2D.Mesh.ArrayFormat.Custom0 | (Electron2D.Mesh.ArrayFormat)((ulong)MeshCustomFormat.RFloat << 13));
        using var declaredCopy = (ArrayMesh)declared.Duplicate(); using var declaredArchive = new ArrayMesh(); declaredArchive.LoadSurfaces(declared.SaveSurfaces());
        Check(declaredCopy.SurfaceGetFormat(0) == declared.SurfaceGetFormat(0) && declaredArchive.SurfaceGetFormat(0) == declared.SurfaceGetFormat(0) && declaredArchive.SurfaceGetArrays(0).Custom0 is { Data.Length: 0 }, "Zero-count channel declarations retain schema without fabricated records.");
        using var sequential = CreateMesh(data: new() { VertexCount = 6 }); Check(sequential.SurfaceGetArrayLen(0) == 6 && sequential.SurfaceGetArrayIndexLen(0) == 0, "Sequential generated invocations need no index or position storage.");
        var channels = Data(); channels.UVs = [Vector2.Zero, Vector2.Right, Vector2.Down, Vector2.One]; channels.Normals = Enumerable.Repeat(new Vector3(0, 0, 1), 4).ToArray();
        using var attributes = CreateMesh(data: channels); var encoded = attributes.ExportPacked(0); Check(encoded.VertexData.Length == 32 && encoded.AttributeData.Length == 32, "Uncompressed direction frames and UVs use the explicit count."); attributes.SurfaceUpdateVertexRegion(0, 0, [0, 0, 0, 0]); attributes.SurfaceUpdateAttributeRegion(0, 0, new byte[8]);
        using var channelsCopy = new ArrayMesh(); channelsCopy.LoadSurfaces(attributes.SaveSurfaces()); Check(channelsCopy.SurfaceGetArrays(0).Normals.Length == 4 && channelsCopy.SurfaceGetArrays(0).UVs.Length == 4, "Direction/attribute archive roundtrip.");
        var before = mesh.GetSurfaceCount(); Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, Data())); Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, new() { VertexCount = -1 }, flags: Empty)); Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, new() { VertexCount = 4, Colors = [Colors.White] }, flags: Empty)); Reject<ArgumentOutOfRangeException>(() => mesh.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, new() { Indices = [0, 1, 65536] }, flags: Empty)); Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, channels, flags: Empty | Electron2D.Mesh.ArrayFormat.CompressAttributes)); Check(mesh.GetSurfaceCount() == before, "Invalid procedural authoring preserves prior surfaces.");
        using var edit = new MeshDataTool(); Reject<ArgumentException>(() => edit.CreateFromSurface(mesh, 0)); using var tool = new SurfaceTool(); tool.Begin(Electron2D.Mesh.PrimitiveType.Triangles); tool.AddVertex(Vector2.Zero); tool.AddVertex(Vector2.Right); tool.AddVertex(Vector2.Down); var old = tool.CommitToArrays(); Reject<ArgumentException>(() => tool.CreateFrom(mesh, 0)); Check(tool.CommitToArrays().Vertices.SequenceEqual(old.Vertices), "Geometry-only builders reject missing positions atomically."); using var marked = tool.Commit(flags: Empty); Check((marked.SurfaceGetFormat(0) & Empty) != 0 && marked.GetFaces().Length == 0, "Existing SurfaceTool policy flags are retained.");
        Producers(); Lifecycle();
        foreach (var language in new[] { "GLSL", "HLSL" }) foreach (var modern in new[] { false, true })
            {
                using var shader = Program(language, modern);
                if (Environment.GetEnvironmentVariable("ELECTRON2D_VERTEX_DUMP") is { } folder)
                { Directory.CreateDirectory(folder); for (var i = 0; i < 8; i++) File.WriteAllBytes(System.IO.Path.Combine(folder, $"procedural-{language}-{(modern ? 15 : 10)}-{i}.spv"), shader.GetProgram().VertexCode((i & 1) != 0, (i & 2) != 0, (i & 4) != 0)!); }
            }
        Console.WriteLine("Procedural mesh: explicit/index-only/sequential counts, copied/packed/AMS8 channel storage, live updates, rejection rollback and shader variants passed.");
    }
}
