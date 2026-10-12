using System.Buffers.Binary;
using System.Diagnostics;
using Electron2D;
using Hebron.Runtime;
using MikkTSpaceSharp;
using static TextureArrayTests;

internal static class MeshFrameTests
{
    internal static MeshSurfaceData Data() => new()
    {
        Vertices = [new(0, 0), new(16, 0), new(16, 16), new(0, 16)],
        Normals = [new(0, 0, -1), new(0, 0, -1), new(0, 0, -1), new(0, 0, -1)],
        Tangents = [new(1, 0, 0, 1), new(1, 0, 0, 1), new(1, 0, 0, 1), new(1, 0, 0, 1)],
        UVs = [Vector2.Zero, Vector2.Right, Vector2.One, Vector2.Down],
        Indices = [0, 1, 2, 0, 2, 3]
    };
    private static bool Near(Vector3 a, Vector3 b) => a.DistanceTo(b) < .0002f;
    private static bool Near(Vector4 a, Vector4 b) => a.DistanceTo(b) < .0002f;
    internal static void Run()
    {
        Import(); Authoring(); Generation(); MeshFrameReferenceTests.Run(); Storage(); Legacy(); Fresh(); AllocationFailures(); Warm();
        Console.WriteLine("Mesh frames: typed copies, octahedral bytes, transactional edits, authoring/generation, archives and allocation cleanup passed.");
    }
    private static void Import()
    {
        using var mesh = new ArrayMesh(); var input = Data(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, input); input.Normals[0] = default; input.Tangents[0] = default;
        var result = mesh.SurfaceGetArrays(0); Check(Near(result.Normals[0], new(0, 0, -1)) && Near(result.Tangents[0], new(1, 0, 0, 1)), "Normal/tangent import copies and quantizes directions.");
        result.Normals[0] = default; Check(Near(mesh.SurfaceGetArrays(0).Normals[0], new(0, 0, -1)), "Queries own their normal/tangent arrays.");
        var absent = Data(); absent.Normals = []; absent.Tangents = []; Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, absent, flags: Mesh.ArrayFormat.Normal));
        var bad = Data(); bad.Normals = [Vector3.Zero]; Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, bad)); bad = Data(); bad.Tangents[3] = new(float.NaN, 0, 0, 1); Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, bad)); bad = Data(); bad.Normals = null!; Reject<ArgumentNullException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, bad));
        var automatic = Data(); automatic.Tangents = []; using var generated = new ArrayMesh(); generated.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, automatic); Check(generated.SurfaceGetArrays(0).Tangents.Length == 4 && (generated.SurfaceGetFormat(0) & Mesh.ArrayFormat.Tangent) != 0, "Normal-only input supplies the actual fallback tangent storage.");
        var words = mesh.Get(0).FrameWords; Check(words[0] == 0xffffffffu && words[1] == 0xbfffFFFFu, "Canonical negative Z and positive X/orientation have pinned UNORM16 words.");
        var patch = new byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(patch, 0x7fff7fffu); mesh.SurfaceUpdateVertexRegion(0, 32, patch); Check(Near(mesh.SurfaceGetArrays(0).Normals[0], new(0, 0, 1)), "Normal region follows the position block.");
        var unchanged = mesh.Get(0).FrameWords[1]; patch[0] = 18; mesh.SurfaceUpdateVertexRegion(0, 33, patch.AsSpan(0, 1)); Check(mesh.Get(0).FrameWords[1] == unchanged && (mesh.Get(0).FrameWords[0] & 0xffff00ffu) == (0x7fff7fffu & 0xffff00ffu), "Partial normal bytes preserve untouched words and bits exactly.");
        var crossing = new byte[12]; BinaryPrimitives.WriteSingleLittleEndian(crossing, float.NaN); BinaryPrimitives.WriteUInt32LittleEndian(crossing.AsSpan(4), 0); var before = mesh.Get(0).FrameWords[0]; Reject<ArgumentException>(() => mesh.SurfaceUpdateVertexRegion(0, 28, crossing)); Check(mesh.Get(0).FrameWords[0] == before, "Invalid preceding position rolls back following packed frame changes."); Reject<ArgumentOutOfRangeException>(() => mesh.SurfaceUpdateVertexRegion(0, 65, []));
        using var editor = new MeshDataTool(); editor.CreateFromSurface(mesh, 0); editor.SetVertexNormal(0, new(0, 1, 0)); editor.SetVertexTangent(0, new(0, 0, -1, -1)); using var copy = new ArrayMesh(); editor.CommitToSurface(copy); Check(Near(copy.SurfaceGetArrays(0).Tangents[0], new(0, 0, -1, -1)) && Near(editor.GetVertexNormal(0), new(0, 1, 0)), "Topology editing commits both new channels and orientation.");
    }
    private static void Authoring()
    {
        using var tool = new SurfaceTool(); tool.Begin(Mesh.PrimitiveType.Triangles); tool.SetNormal(new(0, 0, -1)); tool.SetTangent(new(1, 0, 0, -1)); tool.AddTriangleFan([Vector2.Zero, Vector2.Right, Vector2.One, Vector2.Down], normals: [new(0, 0, 1)], tangents: [new(0, 1, 0, 1)]); var fan = tool.CommitToArrays(); Check(fan.Normals.Length == 6 && fan.Tangents.Length == 6 && fan.Normals.All(n => n.Z == 1), "Fan optional normal/tangent payloads retain current traversal values.");
        tool.Clear(); tool.Begin(Mesh.PrimitiveType.Points); tool.AddVertex(Vector2.Zero); Reject<InvalidOperationException>(() => tool.SetNormal(new(0, 0, 1))); Reject<InvalidOperationException>(() => tool.SetTangent(new(1, 0, 0, 1))); Check(tool.CommitToArrays().Normals.Length == 0, "Late channel activation preserves the original schema.");
        using var immediate = new ImmediateMesh(); immediate.SurfaceBegin(Mesh.PrimitiveType.Triangles); immediate.SurfaceAddVertex2D(Vector2.Zero); immediate.SurfaceSetNormal(new(0, 0, -1)); immediate.SurfaceSetTangent(new(1, 0, 0, -1)); immediate.SurfaceAddVertex2D(Vector2.Right); immediate.SurfaceAddVertex2D(Vector2.Down); immediate.SurfaceEnd(); Check(immediate.SurfaceGetArrays(0).Normals.All(n => Near(n, new(0, 0, -1))) && immediate.SurfaceGetArrays(0).Tangents.All(t => t.W == -1), "Immediate first activation backfills earlier vertices.");
        immediate.SurfaceBegin(Mesh.PrimitiveType.Points); immediate.SurfaceAddVertex2D(Vector2.Zero); immediate.SurfaceEnd(); Check(immediate.SurfaceGetArrays(1).Normals.Length == 0, "End resets the transient frame schema.");
        using var source = new ArrayMesh(); source.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data()); tool.Clear(); tool.AppendFrom(source, 0, new Transform(new(-2, 0), new(0, 3), Vector2.Zero)); var appended = tool.CommitToArrays(); Check(appended.Tangents[0].X < -.999f && appended.Tangents[0].W == -1 && appended.Normals[0].Z < -.999f, "Append transforms direction and reflection handedness."); var count = appended.Vertices.Length; Reject<ArgumentException>(() => tool.AppendFrom(source, 0, new Transform(Vector2.Zero, Vector2.Zero, Vector2.Zero))); Check(tool.CommitToArrays().Vertices.Length == count, "Singular normal-bearing append preserves the old geometry.");
    }
    private static void Generation()
    {
        using var tool = new SurfaceTool(); tool.CreateFromArrays(Data()); tool.GenerateNormals(); var data = tool.CommitToArrays(); Check(data.Normals.All(n => n == new Vector3(0, 0, -1)) && data.Indices.Length == 6, "Indexed normals use clockwise-front winding and retain indices."); tool.GenerateNormals(true); Check(tool.CommitToArrays().Normals.All(n => n.Z == 1), "Flip reverses generated normals."); tool.GenerateNormals(); tool.GenerateTangents(); data = tool.CommitToArrays(); Check(data.Tangents.All(t => Near(t, new(1, 0, 0, 1))) && MikkTSpaceSharpMemoryStats.Allocations == 0, "MikkTSpace generates actual UV frame and frees temporary storage.");
        data = Data(); data.UVs = data.UVs.Select(uv => new Vector2(-uv.X, uv.Y)).ToArray(); tool.CreateFromArrays(data); tool.GenerateTangents(); Check(tool.CommitToArrays().Tangents.All(t => t.X < -.999f && t.W == -1), "Mirrored UVs invert tangent direction and orientation.");
        tool.Clear(); tool.Begin(Mesh.PrimitiveType.Triangles); tool.SetSmoothGroup(-1); tool.AddVertex(Vector2.Zero); tool.AddVertex(Vector2.Right); tool.AddVertex(Vector2.Down); tool.AddVertex(Vector2.Zero); tool.AddVertex(Vector2.Down); tool.AddVertex(Vector2.Right); tool.GenerateNormals(); Check(tool.CommitToArrays().Normals.Take(3).All(n => n.Z == -1) && tool.CommitToArrays().Normals.Skip(3).All(n => n.Z == 1), "Flat smoothing keeps opposite face normals separate.");
        tool.Clear(); tool.Begin(Mesh.PrimitiveType.Triangles); foreach (var p in new[] { Vector2.Zero, Vector2.Right, Vector2.Down, Vector2.Zero, Vector2.Down, Vector2.Right }) tool.AddVertex(p); tool.GenerateNormals(); Check(tool.CommitToArrays().Normals.All(n => n == Vector3.Zero), "Equal smooth groups cancel opposing unit face contributions."); Reject<InvalidOperationException>(() => tool.GenerateTangents());
        data = Data(); data.Vertices = [new(-float.MaxValue, -float.MaxValue), new(float.MaxValue, -float.MaxValue), new(float.MaxValue, float.MaxValue), new(-float.MaxValue, float.MaxValue)]; tool.CreateFromArrays(data); tool.GenerateNormals(); tool.GenerateTangents(); Check(tool.CommitToArrays().Tangents.All(t => t.IsFinite() && t.X > .999f), "Widened normals and calibrated finite extremes preserve the generated direction.");
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data()); mesh.SurfaceSetName(0, "frame"); using var material = new CanvasItemMaterial(); mesh.SurfaceSetMaterial(0, material); var words = mesh.Get(0).FrameWords.Where((_, i) => i % 2 == 0).ToArray(); var events = 0; mesh.Changed += _ => events++; mesh.RegenNormalMaps(); Check(events == 1 && mesh.SurfaceGetName(0) == "frame" && ReferenceEquals(mesh.SurfaceGetMaterial(0), material) && mesh.Get(0).FrameWords.Where((_, i) => i % 2 == 0).SequenceEqual(words), "Regeneration publishes once and preserves names/materials/exact normals.");
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [Vector2.Zero, Vector2.Right, Vector2.Down] }); var old = mesh.Get(0); Reject<InvalidOperationException>(() => mesh.RegenNormalMaps()); Check(ReferenceEquals(mesh.Get(0), old) && mesh.GetSurfaceCount() == 2, "A later invalid surface preserves the entire prior mesh.");
    }
    private static void Storage()
    {
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data()); var descriptor = (PropertyDescriptor<ArrayMesh, byte[]>)mesh.GetPropertyList().Single(p => p.Name == "_surface_data"); var bytes = descriptor.GetValue(mesh); using var restored = new ArrayMesh(); descriptor.SetValue(restored, bytes); Check(restored.Get(0).FrameWords.SequenceEqual(mesh.Get(0).FrameWords), "AMS3 keeps exact packed frame words.");
        using var duplicate = (ArrayMesh)mesh.Duplicate(); Check(duplicate.Get(0).FrameWords.SequenceEqual(mesh.Get(0).FrameWords), "Resource duplication keeps exact frame words."); var bad = (byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(44), int.MaxValue); Reject<InvalidDataException>(() => descriptor.SetValue(restored, bad)); Check(restored.Get(0).FrameWords.SequenceEqual(mesh.Get(0).FrameWords), "Malformed frame counts retain prior storage.");
    }
    private static void Legacy()
    {
        using var mesh = new ArrayMesh(); var descriptor = (PropertyDescriptor<ArrayMesh, byte[]>)mesh.GetPropertyList().Single(p => p.Name == "_surface_data");
        using var stream = new MemoryStream(); using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) { writer.Write(0x32534d41u); writer.Write(1); writer.Write((int)Mesh.PrimitiveType.Points); writer.Write((ulong)(Mesh.ArrayFormat.Vertex | Mesh.ArrayFormat.Use2DVertices | Mesh.ArrayFormat.TexUV2)); writer.Write(0); writer.Write(1); writer.Write(0); writer.Write(0); writer.Write(1); writer.Write(0); writer.Write(0); writer.Write(3f); writer.Write(4f); writer.Write(.25f); writer.Write(.5f); }
        descriptor.SetValue(mesh, stream.ToArray()); var data = mesh.SurfaceGetArrays(0); Check(data.Vertices[0] == new Vector2(3, 4) && data.UV2s[0] == new Vector2(.25f, .5f) && data.Normals.Length == 0 && data.Tangents.Length == 0, "Legacy AMS2 preserves its UV2 data without introducing frame channels.");
    }
    private static void Fresh()
    {
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data()); using var material = new CanvasItemMaterial { BlendMode = BlendMode.Add }; mesh.SurfaceSetMaterial(0, material);
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-mesh-frames-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(mesh, path); var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(MeshFrameTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_MESH_FRAMES"); start.Environment.Remove("ELECTRON2D_TEST_MESH_FRAMES_NATIVE"); start.Environment["ELECTRON2D_TEST_MESH_FRAMES_CHILD"] = path;
            using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Mesh frame child."); }
            Check(child.ExitCode == 0 && stdout.GetAwaiter().GetResult().Contains("Fresh frame archive passed"), stderr.GetAwaiter().GetResult());
        }
        finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        using var mesh = ResourceLoader.Load<ArrayMesh>(path, ResourceLoader.CacheMode.Ignore); var node = new Draw(mesh); using var root = new Node(); root.AddChild(node); using var tree = new SceneTree(root); node.Record(); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); node.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 6 && Near(vertices[0].Normal, new(0, 0, -1)) && Near(vertices[0].Tangent, new(1, 0, 0, 1)) && batches[0].Blend == BlendMode.Add, "Fresh archive preserves actual frame/material canvas emission."); mesh.RegenNormalMaps(); Check(mesh.GetSurfaceCount() == 1, "Fresh output retains the executable generation path."); Console.WriteLine("Fresh frame archive passed.");
    }
    private sealed class Draw(Mesh mesh) : Entity { internal void Record() { QueueRedraw(); PrepareCanvas(); } protected override void OnDraw() => DrawMesh(mesh, null); }
    private static void AllocationFailures()
    {
        var before = MikkTSpaceSharpMemoryStats.Allocations; var context = new MikkTSpace.SMikkTSpaceContext { m_getNumFaces = () => 1, m_getNumVerticesOfFace = _ => 3, m_getPosition = (_, _) => throw new Probe(), m_getNormal = (_, _) => default, m_getTexCoord = (_, _) => default, m_setTSpaceBasic = (_, _, _, _) => { } }; Reject<Probe>(() => MikkTSpace.genTangSpaceDefault(context)); Check(before == MikkTSpaceSharpMemoryStats.Allocations, "Vendored callback failure frees every temporary native block.");
    }
    private static void Warm()
    {
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data()); var bytes = new byte[8]; BinaryPrimitives.WriteUInt32LittleEndian(bytes, 0xffffffffu); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 0xbfffffffu); using var editor = new MeshDataTool(); editor.CreateFromSurface(mesh, 0);
        void Update() { mesh.SurfaceUpdateVertexRegion(0, 32, bytes); editor.SetVertexNormal(0, new(0, 0, -1)); editor.SetVertexTangent(0, new(1, 0, 0, 1)); _ = editor.GetVertexNormal(0); _ = editor.GetVertexTangent(0); }
        for (var i = 0; i < 32; i++) Update(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Update(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed packed frame edits and topology frame access allocate zero managed bytes.");
    }
    private sealed class Probe : Exception { }
}
