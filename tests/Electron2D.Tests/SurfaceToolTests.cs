using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;

internal static class SurfaceToolTests
{
    internal static Vector2[] Quad(float size = 16) => [new(0, 0), new(size, 0), new(size, size), new(0, size)];
    internal static ArrayMesh Mesh(bool skin = false)
    {
        using var tool = new SurfaceTool(); if (skin) tool.SetSkinWeightCount(SurfaceTool.SkinWeightCount.Eight); tool.Begin(Electron2D.Mesh.PrimitiveType.Triangles); tool.SetColor(Colors.White); tool.SetUV(new(.25f, 0)); tool.SetUV2(new(.75f, 0));
        if (skin) { tool.SetBones([0, 0, 0, 0, 0, 5, 0, 0]); tool.SetWeights([0, 0, 0, 0, 0, 1, 0, 0]); }
        tool.AddTriangleFan(Quad()); tool.Index(); return tool.Commit();
    }
    internal static void Run()
    {
        Basics(); Skin(); Imports(); Cache(); Storage(); Warm();
        Console.WriteLine("SurfaceTool finite/state/channel boundaries, paired four/eight influence selection, atomic publication, copied imports/fans/indexing/cache ordering and warm replay passed.");
    }
    private static void Basics()
    {
        using var tool = new SurfaceTool(); Check(tool.GetPrimitiveType() == Electron2D.Mesh.PrimitiveType.Lines && tool.GetSkinWeightCount() == SurfaceTool.SkinWeightCount.Four && tool.GetAABB() == default, "Empty authoring defaults."); Reject<NotSupportedException>(() => tool.Commit(flags: (Electron2D.Mesh.ArrayFormat)268435456)); using var empty = tool.Commit(); Check(empty.GetSurfaceCount() == 0 && tool.CommitToArrays().Vertices.Length == 0, "Empty commit is an owned empty mesh."); Reject<InvalidOperationException>(() => tool.AddVertex(Vector2.Zero)); Reject<InvalidOperationException>(() => tool.SetUV2(Vector2.One)); Reject<ArgumentOutOfRangeException>(() => tool.Begin((Electron2D.Mesh.PrimitiveType)99));
        tool.Begin(Electron2D.Mesh.PrimitiveType.Triangles); tool.SetColor(Colors.Red); tool.SetUV(new(.25f, 0)); tool.SetUV2(Vector2.One); tool.AddIndex(0); tool.AddIndex(1); tool.AddIndex(2); tool.AddVertex(Vector2.Zero); tool.SetUV2(new(.5f, 0)); tool.AddVertex(new(16, 0)); Reject<ArgumentException>(() => tool.Commit(empty)); Check(empty.GetSurfaceCount() == 0, "Incomplete topology preserves target and retryable authoring."); tool.AddVertex(new(0, 16)); var data = tool.CommitToArrays(); Check(data.UV2s.SequenceEqual(new[] { Vector2.One, new Vector2(.5f, 0), new Vector2(.5f, 0) }), "Current UV2 changes affect subsequent vertices."); data.Vertices[0] = new(900, 900); Check(tool.GetAABB() == new Rect2(0, 0, 16, 16), "Snapshots own their channels.");
        using var material = new CanvasItemMaterial(); tool.SetMaterial(material); var notifications = 0; empty.Changed += _ => { notifications++; Check(empty.SurfaceGetMaterial(empty.GetSurfaceCount() - 1) == material, "Changed observes complete geometry and material together."); }; Check(ReferenceEquals(tool.Commit(empty), empty) && ReferenceEquals(tool.Commit(empty), empty) && empty.GetSurfaceCount() == 2 && notifications == 2, "Commit preserves authoring and existing identity with one publication each.");
        using var target = new ArrayMesh(); target.Changed += _ => throw new Probe(); Reject<Probe>(() => tool.Commit(target)); Check(target.GetSurfaceCount() == 1 && ReferenceEquals(target.SurfaceGetMaterial(0), material), "Observer failure retains complete publication."); tool.Begin(Electron2D.Mesh.PrimitiveType.Points); tool.AddVertex(Vector2.Zero); Reject<InvalidOperationException>(() => tool.SetColor(Colors.Red)); Reject<InvalidOperationException>(() => tool.SetUV(Vector2.Zero)); Reject<InvalidOperationException>(() => tool.SetUV2(Vector2.Zero)); Reject<ArgumentException>(() => tool.AddVertex(new(float.NaN, 0))); Check(tool.CommitToArrays().Vertices.Length == 1 && tool.CommitToArrays().Colors.Length == 0, "Late activation/nonfinite rejection preserves authoring.");
        tool.Clear(); Check(tool.GetPrimitiveType() == Electron2D.Mesh.PrimitiveType.Lines && tool.GetSkinWeightCount() == SurfaceTool.SkinWeightCount.Four && !material.IsDisposed, "Clear restores defaults and borrows material."); tool.Begin(Electron2D.Mesh.PrimitiveType.Triangles); tool.SetUV2(Vector2.One); tool.AddTriangleFan(Quad(), uv2s: [Vector2.One, Vector2.One, Vector2.One, Vector2.One]); tool.Index(); Check(tool.CommitToArrays().Vertices.Length == 4 && tool.CommitToArrays().Indices.SequenceEqual(new[] { 0, 1, 2, 0, 2, 3 }), "Fan indexing preserves ordered exact records."); tool.AddTriangleFan(Quad(8)); Check(tool.CommitToArrays().Indices.Length == 12, "Fan append remains drawable after indexing."); var before = tool.CommitToArrays(); Reject<ArgumentException>(() => tool.AddTriangleFan(Quad(), uv2s: [new(float.NaN, 0)])); Check(tool.CommitToArrays().Vertices.SequenceEqual(before.Vertices) && tool.CommitToArrays().Indices.SequenceEqual(before.Indices), "Invalid fan is atomic."); tool.Deindex(); Check(tool.CommitToArrays().Vertices.Length == 12 && tool.CommitToArrays().Indices.Length == 0, "Deindex expands all attributes in draw order.");
        tool.Begin(Electron2D.Mesh.PrimitiveType.Points); tool.AddVertex(Vector2.Zero); tool.SetSmoothGroup(1); tool.AddVertex(Vector2.Zero); tool.SetSmoothGroup(-1); tool.AddVertex(Vector2.Zero); tool.Index(); Check(tool.CommitToArrays().Vertices.Length == 3, "Smooth groups remain exact deduplication identities."); tool.Begin(Electron2D.Mesh.PrimitiveType.Points); tool.AddIndex(999); tool.AddVertex(Vector2.Zero); Reject<ArgumentException>(tool.Deindex); Check(tool.CommitToArrays().Indices[0] == 999 && tool.CommitToArrays().Vertices.Length == 1, "Deindex preflight retains invalid pending state.");
        tool.Begin(Electron2D.Mesh.PrimitiveType.Points); tool.AddVertex(new(-float.MaxValue, 0)); tool.AddVertex(new(float.MaxValue, 0)); Reject<ArgumentException>(() => tool.GetAABB());
        foreach (var primitive in Enum.GetValues<Electron2D.Mesh.PrimitiveType>()) { tool.Begin(primitive); var count = primitive is Electron2D.Mesh.PrimitiveType.Lines or Electron2D.Mesh.PrimitiveType.LineStrip ? 2 : primitive == Electron2D.Mesh.PrimitiveType.Points ? 1 : 3; for (var i = 0; i < count; i++) tool.AddVertex(new(i, i % 2)); using var mesh = tool.Commit(); Check(mesh.SurfaceGetPrimitiveType(0) == primitive, "Every topology executes through shared surface storage."); }
        tool.Dispose(); Reject<ObjectDisposedException>(() => tool.Clear()); Reject<ObjectDisposedException>(() => tool.GetPrimitiveType()); Check(!material.IsDisposed, "Builder disposal retains borrowed material.");
    }
    private static void Skin()
    {
        using var tool = new SurfaceTool(); tool.SetSkinWeightCount(SurfaceTool.SkinWeightCount.Eight); tool.Begin(Electron2D.Mesh.PrimitiveType.Points); Check(tool.GetSkinWeightCount() == SurfaceTool.SkinWeightCount.Eight, "Begin retains configured eight influences."); tool.SetBones([5]); Reject<InvalidOperationException>(() => tool.AddVertex(Vector2.Zero)); tool.SetWeights([.5f]); tool.AddVertex(Vector2.Zero); var data = tool.CommitToArrays(); Check(data.Bones.Length == 8 && data.Bones[0] == 5 && data.Weights[0] == .5f && data.Weights[7] == 0, "Short lists pad rather than normalize."); Reject<InvalidOperationException>(() => tool.SetSkinWeightCount(SurfaceTool.SkinWeightCount.Four)); Reject<ArgumentOutOfRangeException>(() => tool.SetBones([65536])); Reject<ArgumentException>(() => tool.SetWeights([float.NaN])); using var mesh = tool.Commit(); Check((mesh.SurfaceGetFormat(0) & Electron2D.Mesh.ArrayFormat.Use8BoneWeights) != 0 && mesh.SurfaceGetArrays(0).Bones.Length == 8, "Eight-mode commit infers the real storage flag.");
        tool.Begin(Electron2D.Mesh.PrimitiveType.Points); tool.SetSkinWeightCount(SurfaceTool.SkinWeightCount.Four); tool.SetBones([0, 1, 2, 3, 4, 5]); tool.SetWeights([1, 8, 2, 8, 3, 4]); tool.AddVertex(Vector2.Zero); data = tool.CommitToArrays(); Check(data.Bones.SequenceEqual(new[] { 1, 3, 5, 4 }) && Math.Abs(data.Weights.Sum() - 1) < 1e-6f, "Stable strongest selection and overfull normalization.");
        tool.Begin(Electron2D.Mesh.PrimitiveType.Points); tool.SetBones([0, 1, 2, 3, 4]); tool.SetWeights([float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue, 0]); tool.AddVertex(Vector2.Zero); Check(tool.CommitToArrays().Weights.All(w => w == .25f), "Finite normalization avoids float sum overflow."); tool.Clear(); Reject<ArgumentOutOfRangeException>(() => tool.SetSkinWeightCount((SurfaceTool.SkinWeightCount)8)); tool.Begin(Electron2D.Mesh.PrimitiveType.Points); tool.SetBones([0, 1]); tool.SetWeights([1]); Reject<ArgumentException>(() => tool.AddVertex(Vector2.Zero)); tool.SetWeights([0, 0]); tool.AddVertex(Vector2.Zero); Check(tool.CommitToArrays().Weights.All(w => w == 0), "Mismatch retry and zero influence values remain valid authoring.");
    }
    private static void Imports()
    {
        using var source = Mesh(); using var material = new CanvasItemMaterial(); source.SurfaceSetMaterial(0, material); using var tool = new SurfaceTool(); tool.CreateFrom(source, 0); Check(tool.GetPrimitiveType() == Electron2D.Mesh.PrimitiveType.Triangles && tool.CommitToArrays().UV2s[0].X == .75f, "Copied source import."); Reject<InvalidOperationException>(() => tool.AddVertex(Vector2.Zero)); using var copy = tool.Commit(); Check(ReferenceEquals(copy.SurfaceGetMaterial(0), material), "Import borrows surface material."); tool.Deindex(); tool.AppendFrom(source, 0, new(0, new(20, 0))); var data = tool.CommitToArrays(); Check(data.Indices.Length == 12 && data.Vertices.Length == 10 && data.Vertices[6].X == 20, "Mixed sequential/indexed append retains the whole prefix."); using var merged = tool.Commit(); Check(merged.GetFaces().Length == 12, "Appended data executes complete faces.");
        using var simple = new ArrayMesh(); simple.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, new() { Vertices = [new(0, 0), new(4, 0), new(0, 4)] }); tool.CreateFrom(source, 0); tool.AppendFrom(simple, 0, new(0, new(24, 0))); data = tool.CommitToArrays(); Check(data.Colors[^1] == Colors.White && data.UVs[^1] == Vector2.Zero && data.UV2s[^1] == Vector2.Zero && data.Indices.Length == 9, "Missing appended channels use actual drawing defaults."); var before = tool.CommitToArrays(); Reject<ArgumentException>(() => tool.AppendFrom(source, 0, new Transform(new(float.MaxValue, 0), Vector2.Down, Vector2.Zero))); Check(tool.CommitToArrays().Vertices.SequenceEqual(before.Vertices), "Transform overflow preserves destination."); var broken = new MeshSurfaceData { Vertices = [Vector2.Zero] }; Reject<ArgumentException>(() => tool.CreateFromArrays(broken)); Check(tool.CommitToArrays().Vertices.SequenceEqual(before.Vertices), "Invalid bulk import preserves prior state."); using var skinned = Mesh(true); Reject<ArgumentException>(() => tool.AppendFrom(skinned, 0, Transform.Identity));
        using var callback = new Callback(); callback.Action = () => { Reject<InvalidOperationException>(tool.Clear); Reject<InvalidOperationException>(tool.Dispose); }; tool.CreateFrom(callback, 0); callback.Action = callback.Change; Reject<InvalidOperationException>(() => tool.CreateFrom(callback, 0)); callback.Action = null; tool.CreateFrom(callback, 0); Check(tool.GetAABB() == new Rect2(0, 0, 1, 1), "Source callback guard, revision rejection and retry.");
    }
    private sealed class Probe : Exception { }
    private sealed class Callback : Electron2D.Mesh
    {
        internal Action? Action;
        internal void Change() => EmitChanged();
        protected override int OnGetSurfaceCount() => 1;
        protected override MeshSurfaceData OnSurfaceGetArrays(int index) { Action?.Invoke(); return new() { Vertices = [Vector2.Zero, Vector2.Right, Vector2.Down] }; }
        protected override int OnSurfaceGetArrayLen(int index) => 3;
        protected override int OnSurfaceGetArrayIndexLen(int index) => 0;
        protected override PrimitiveType OnSurfaceGetPrimitiveType(int index) => PrimitiveType.Triangles;
        protected override ArrayFormat OnSurfaceGetFormat(int index) => ArrayFormat.Vertex | ArrayFormat.Use2DVertices;
        protected override Material? OnSurfaceGetMaterial(int index) => null;
        protected override void OnSurfaceSetMaterial(int index, Material? material) { }
    }
    internal static MeshSurfaceData Grid(int width)
    {
        var data = new MeshSurfaceData { Vertices = new Vector2[(width + 1) * (width + 1)], Colors = new Color[(width + 1) * (width + 1)], Indices = new int[width * width * 6] };
        for (var y = 0; y <= width; y++) for (var x = 0; x <= width; x++) { data.Vertices[y * (width + 1) + x] = new(-.9f + 1.8f * x / width, -.9f + 1.8f * y / width); data.Colors[y * (width + 1) + x] = Colors.Red; }
        var random = new System.Random(71); var order = Enumerable.Range(0, width * width).ToArray(); random.Shuffle(order);
        for (var i = 0; i < order.Length; i++) { var x = order[i] % width; var y = order[i] / width; var a = y * (width + 1) + x; int[] face = [a, a + 1, a + width + 2, a, a + width + 2, a + width + 1]; face.CopyTo(data.Indices, i * 6); }
        return data;
    }
    internal static long Misses(int[] indices)
    {
        var cache = new List<int>(16); long misses = 0; foreach (var i in indices) { var p = cache.IndexOf(i); if (p < 0) { misses++; if (cache.Count == 16) cache.RemoveAt(15); } else cache.RemoveAt(p); cache.Insert(0, i); }
        return misses;
    }
    private static void Cache()
    {
        using var tool = new SurfaceTool(); Reject<InvalidOperationException>(tool.OptimizeIndicesForCache); var source = Grid(48); tool.CreateFromArrays(source); tool.OptimizeIndicesForCache(); var result = tool.CommitToArrays(); var originalFaces = Enumerable.Range(0, source.Indices.Length / 3).Select(i => string.Join(',', source.Indices.AsSpan(i * 3, 3).ToArray())).Order().ToArray(); var faces = Enumerable.Range(0, result.Indices.Length / 3).Select(i => string.Join(',', result.Indices.AsSpan(i * 3, 3).ToArray())).Order().ToArray(); Check(originalFaces.SequenceEqual(faces) && result.Vertices.SequenceEqual(source.Vertices) && Misses(result.Indices) < Misses(source.Indices), "Cache reordering preserves all wound triangles and improves the explicit model."); Console.WriteLine("SurfaceTool 48x48 cache model misses: " + Misses(source.Indices) + " -> " + Misses(result.Indices)); tool.CreateFromArrays(Grid(256)); tool.OptimizeIndicesForCache(); Check(tool.CommitToArrays().Indices.Length == 256 * 256 * 6, "Large connected geometry completes bounded adjacency traversal.");
    }
    private static void Storage()
    {
        using var mesh = Mesh(true); using var material = new CanvasItemMaterial { BlendMode = BlendMode.Add }; mesh.SurfaceSetMaterial(0, material); var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-surface-tool-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try { ResourceSaver.Save(mesh, path); var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(SurfaceToolTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_SURFACE_TOOL_CHILD"] = path; using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("SurfaceTool child."); } Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh SurfaceTool output passed"), error.GetAwaiter().GetResult()); }
        finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        using var mesh = ResourceLoader.Load<ArrayMesh>(path, ResourceLoader.CacheMode.Ignore); using var tool = new SurfaceTool(); tool.CreateFrom(mesh, 0); var data = tool.CommitToArrays(); Check(tool.GetSkinWeightCount() == SurfaceTool.SkinWeightCount.Eight && data.Bones[5] == 5 && data.Weights[5] == 1 && data.UV2s[0].X == .75f && mesh.SurfaceGetMaterial(0) is CanvasItemMaterial { BlendMode: BlendMode.Add }, "Fresh output factory retains indexed UV2, eighth-mode influences and material."); tool.Deindex(); tool.Index(); using var output = tool.Commit(); var node = new Draw(output); using var root = new Node(); root.AddChild(node); using var tree = new SceneTree(root); node.Record(); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); node.AppendCanvas(vertices, batches, Transform.Identity); Check(vertices.Count == 6 && vertices[0].UV2.X == .75f && vertices[0].UV.X == .25f && batches[0].Blend == BlendMode.Add, "Fresh imported/edited output executes retained geometry and material."); Console.WriteLine("Fresh SurfaceTool output passed.");
    }
    private sealed class Draw(Electron2D.Mesh mesh) : Entity { internal void Record() { QueueRedraw(); PrepareCanvas(); } protected override void OnDraw() => DrawMesh(mesh, null); }
    private static void Warm()
    {
        using var tool = new SurfaceTool(); tool.Begin(Electron2D.Mesh.PrimitiveType.Triangles); tool.AddTriangleFan(Quad()); tool.Index(); using var mesh = tool.Commit(); var node = new Draw(mesh); using var root = new Node(); root.AddChild(node); using var tree = new SceneTree(root); node.Record(); var vertices = new List<CanvasVertex>(64); var batches = new List<CanvasBatch>(16);
        void Replay() { vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity); }
        for (var i = 0; i < 24; i++) Replay(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { tool.SetSmoothGroup(i); tool.GetAABB(); tool.GetPrimitiveType(); Replay(); }
        Check(before == GC.GetAllocatedBytesForCurrentThread(), "Prepared committed mesh replay and scalar builder state/query changes allocate zero managed bytes.");
    }
}
