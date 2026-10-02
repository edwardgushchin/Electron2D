using System.Buffers.Binary;
using Electron2D;

internal static class MeshTests
{
    internal static void Run()
    {
        VerifyBoundaries(); VerifyCustomMesh();
        using var mesh = new ArrayMesh(); Check(mesh.GetSurfaceCount() == 0, "Empty mesh.");
        var data = Quad(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, data, flags: Mesh.ArrayFormat.UseDynamicUpdate);
        data.Vertices[0] = new(500, 500); var copy = mesh.SurfaceGetArrays(0); Check(copy.Vertices[0] == Vector2.Zero && copy.Indices.Length == 6, "Copied surface data."); copy.Vertices[0] = new(99, 99); Check(mesh.SurfaceGetArrays(0).Vertices[0] == Vector2.Zero, "Queries copy data.");
        mesh.SurfaceSetName(0, "front"); Check(mesh.SurfaceFindByName("front") == 0 && mesh.SurfaceFindByName("missing") == -1 && mesh.SurfaceGetArrayLen(0) == 4 && mesh.SurfaceGetArrayIndexLen(0) == 6, "Surface metadata.");
        var bytes = new byte[8]; BinaryPrimitives.WriteSingleLittleEndian(bytes, 2); BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(4), 3); mesh.SurfaceUpdateVertexRegion(0, 0, bytes); Check(mesh.SurfaceGetArrays(0).Vertices[0] == new Vector2(2, 3), "Live vertex regions.");
        BinaryPrimitives.WriteSingleLittleEndian(bytes, float.NaN); Reject<ArgumentException>(() => mesh.SurfaceUpdateVertexRegion(0, 0, bytes)); Check(mesh.SurfaceGetArrays(0).Vertices[0] == new Vector2(2, 3), "Malformed update rollback.");
        mesh.SurfaceUpdateVertexRegion(0, 0, []);
        var partial = new byte[] { 0x80, 0x3f }; mesh.SurfaceUpdateVertexRegion(0, 2, partial); Check(mesh.SurfaceGetArrays(0).Vertices[0].X == 1, "Partial byte position updates preserve untouched bytes.");
        mesh.SurfaceUpdateAttributeRegion(0, 0, [0, 0, 255, 255]); Check(mesh.SurfaceGetArrays(0).Colors[0] == Colors.Blue, "Packed RGBA8 attribute updates."); Reject<ArgumentOutOfRangeException>(() => mesh.SurfaceUpdateVertexRegion(0, 32, bytes));
        var changed = 0; mesh.Changed += _ => changed++; mesh.SurfaceSetName(0, "front"); Check(changed == 1, "Equal name reports change.");
        Action<Resource> failure = _ => throw new ApplicationException("Changed failure"); mesh.Changed += failure; BinaryPrimitives.WriteSingleLittleEndian(bytes, 4);
        try { Reject<ApplicationException>(() => mesh.SurfaceUpdateVertexRegion(0, 0, bytes)); Check(mesh.SurfaceGetArrays(0).Vertices[0].X == 4, "Committed update survives listener failure."); } finally { mesh.Changed -= failure; }
        using var duplicated = (ArrayMesh)mesh.Duplicate(); Check(duplicated.SurfaceGetName(0) == "front" && duplicated.SurfaceGetArrays(0).Vertices[0].X == 4, "Mesh duplication.");
        foreach (var type in Enum.GetValues<Mesh.PrimitiveType>())
        {
            using var primitive = new ArrayMesh(); primitive.AddSurfaceFromArrays(type, new() { Vertices = type == Mesh.PrimitiveType.Triangles ? [new(0, 0), new(8, 0), new(0, 8)] : [new(0, 0), new(8, 0), new(0, 8), new(8, 8)] });
            Check(primitive.SurfaceGetPrimitiveType(0) == type, "Every primitive policy.");
        }
        Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [Vector2.Zero, Vector2.One] }));
        Reject<ArgumentOutOfRangeException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { Vertices = [Vector2.Zero], Indices = [1] }));
        var rid = mesh.GetRID(); Check(rid.IsValid() && rid == mesh.GetRID(), "Stable borrowed mesh identity.");
        mesh.ResourcePath = "res://test-mesh"; using var node = new MeshInstance { Mesh = mesh }; using var scene = new PackedScene(); scene.Pack(node); using var restored = (MeshInstance)scene.Instantiate(); Check(ReferenceEquals(restored.Mesh, mesh), "Packed node retains external borrowed mesh.");
        var textures = 0; node.TextureChanged += () => textures++; node.Texture = null; Check(textures == 0, "Equal texture is silent.");
        var window = new Node(); var drawing = new DrawNode(mesh); window.AddChild(drawing); using var tree = new SceneTree(window);
        var vertices = new List<CanvasVertex>(2048); var batches = new List<CanvasBatch>(128); drawing.Record();
        for (var i = 0; i < 20; i++) { vertices.Clear(); batches.Clear(); drawing.Replay(vertices, batches); }
        BinaryPrimitives.WriteSingleLittleEndian(bytes, 0); var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { mesh.SurfaceUpdateVertexRegion(0, 0, bytes); vertices.Clear(); batches.Clear(); drawing.Record(); drawing.Replay(vertices, batches); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed active region/draw replay zero allocation.");
        drawing.Dispose(); mesh.Dispose(); Reject<ArgumentException>(() => RenderingMeshRegistry.Resolve(rid)); Check(duplicated.GetSurfaceCount() == 1, "Disposal invalidates only owned mesh identity.");
        Console.WriteLine("Mesh copied geometry, metadata, updates, callbacks, primitives, ownership and warm replay passed.");
    }
    private static void VerifyBoundaries()
    {
        using var mesh = new ArrayMesh();
        Reject<ArgumentNullException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { Vertices = null! }));
        Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { Vertices = [new(float.PositiveInfinity, 0)] }));
        Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { Vertices = [Vector2.Zero], Colors = [Colors.Red, Colors.Blue] }));
        Reject<NotSupportedException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { Vertices = [Vector2.Zero] }, flags: (Mesh.ArrayFormat)2));
        Reject<NotSupportedException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { Vertices = [Vector2.Zero] }, blendShapes: [new()]));
        Reject<NotSupportedException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { Vertices = [Vector2.Zero] }, lods: new Dictionary<float, int[]> { [1] = [0] }));
        Check(mesh.GetSurfaceCount() == 0, "Rejected additions preserve surface count.");
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Quad());
        var bytes = new byte[24]; BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(4), .25f); BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(16), float.NaN);
        Reject<ArgumentException>(() => mesh.SurfaceUpdateAttributeRegion(0, 0, bytes));
        Check(mesh.SurfaceGetArrays(0).Colors[0] == Colors.Red && mesh.SurfaceGetArrays(0).UVs[0] == Vector2.Zero, "Invalid later UV rolls back earlier color and UV records.");
        Array.Clear(bytes); BinaryPrimitives.WriteSingleLittleEndian(bytes, 5); BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(8), float.NaN);
        Reject<ArgumentException>(() => mesh.SurfaceUpdateVertexRegion(0, 0, bytes.AsSpan(0, 16)));
        Check(mesh.SurfaceGetArrays(0).Vertices[0] == Vector2.Zero, "Invalid later position rolls back earlier record.");
        Reject<ArgumentOutOfRangeException>(() => mesh.SurfaceGetArrays(-1)); Reject<ArgumentOutOfRangeException>(() => mesh.SurfaceRemove(1));
        mesh.SurfaceUpdateVertexRegion(0, 32, []); mesh.SurfaceUpdateAttributeRegion(0, 48, []);
        var faces = mesh.GetFaces(); Check(faces.Length == 6 && faces[3] == Vector2.Zero, "Indexed triangle faces."); faces[0] = Vector2.One; Check(mesh.GetFaces()[0] == Vector2.Zero, "Face copies are independent.");
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.TriangleStrip, new() { Vertices = [new(0, 0), new(1, 0), new(0, 1), new(1, 1)] });
        faces = mesh.GetFaces(); Check(faces.Length == 12 && faces[9] == new Vector2(0, 1) && faces[10] == new Vector2(1, 0), "Strip faces preserve alternating winding.");
        mesh.SurfaceSetName(0, "same"); mesh.SurfaceSetName(1, "same"); Check(mesh.SurfaceFindByName("same") == 0, "Duplicate names select first surface.");
        mesh.SurfaceRemove(0); Check(mesh.SurfaceGetPrimitiveType(0) == Mesh.PrimitiveType.TriangleStrip && mesh.SurfaceGetArrayIndexLen(0) == 0, "Removal shifts metadata and geometry together.");
        using var material = new CanvasItemMaterial(); mesh.SurfaceSetMaterial(0, material);
        using var duplicate = (ArrayMesh)mesh.Duplicate(true); Check(!ReferenceEquals(duplicate.SurfaceGetMaterial(0), material), "Deep copy duplicates borrowed subresources.");
        duplicate.SurfaceGetMaterial(0)!.Dispose(); mesh.ClearSurfaces(); Check(!material.IsDisposed && mesh.GetFaces().Length == 0, "Clear borrows material and empties face query.");
        using var node = new MeshInstance(); Check(node.GetConfigurationWarnings().Length == 1, "Missing mesh warning."); node.Mesh = mesh; Check(node.GetConfigurationWarnings().Length == 0, "Assigned empty mesh clears warning.");
        using var emptyAtlas = new AtlasTexture();
        using (var root = new Node())
        {
            var drawing = new DrawNode(mesh, emptyAtlas); root.AddChild(drawing); using var tree = new SceneTree(root); drawing.Record();
        }
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        node.TextureChanged += () => throw new ApplicationException("Texture callback failure"); Reject<ApplicationException>(() => node.Texture = texture); Check(ReferenceEquals(node.Texture, texture), "Texture failure retains committed borrowed assignment.");
        node.Dispose(); Check(!mesh.IsDisposed && !texture.IsDisposed, "Node disposal retains borrowed resources.");
        mesh.Dispose(); Reject<ObjectDisposedException>(() => mesh.SurfaceGetArrayLen(0)); Reject<ObjectDisposedException>(() => mesh.GetFaces());
    }
    private static void VerifyCustomMesh()
    {
        using var mesh = new CustomMesh();
        var vertices = new List<CanvasVertex>(128); var batches = new List<CanvasBatch>(8); var draw = new CanvasMesh(mesh, Transform.Identity, Colors.White);
        void Replay() { vertices.Clear(); batches.Clear(); draw.Append(vertices, batches, null, Transform.Identity, Colors.White, null, BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false); }
        Replay(); Check(vertices.Count == 6 && mesh.Reads == 1, "Custom surface callbacks prepare geometry.");
        Replay(); Check(mesh.Reads == 1, "Unchanged custom replay reuses prepared snapshot.");
        mesh.Failure = true; mesh.Notify(); Reject<ApplicationException>(Replay); mesh.Failure = false; Replay(); Check(vertices.Count == 6, "Custom callback failure recovers on retry.");
        mesh.Mutate = true; mesh.Notify(); Reject<InvalidOperationException>(Replay); mesh.Mutate = false; Replay();
        mesh.Count = -1; mesh.Notify(); Reject<InvalidOperationException>(Replay); mesh.Count = 0; mesh.Notify(); Replay(); Check(vertices.Count == 0, "Invalid custom count rejects; removal clears cached surfaces.");
        mesh.Count = 1; mesh.Notify(); Replay(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Replay(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared custom idle replay allocates zero managed bytes.");
        mesh.Dispose(); Reject<ObjectDisposedException>(Replay);
        using var line = new ArrayMesh(); line.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, new() { Vertices = [new(-1e30f, 0), new(1e30f, 0)] });
        var large = new CanvasMesh(line, Transform.Identity, Colors.White); vertices.Clear(); batches.Clear();
        large.Append(vertices, batches, null, Transform.Identity, Colors.White, null, BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false);
        Check(vertices.Count == 6 && Math.Abs(vertices[0].Position.Y) == .5f, "Large finite line spans preserve framebuffer width without squared-length overflow.");
    }
    private sealed class CustomMesh : Mesh
    {
        internal int Count = 1, Reads; internal bool Failure, Mutate;
        internal void Notify() => EmitChanged();
        protected override int OnGetSurfaceCount() => Count;
        protected override MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex) { Reads++; if (Failure) throw new ApplicationException("Custom geometry failure"); if (Mutate) EmitChanged(); return Quad(); }
        protected override int OnSurfaceGetArrayLen(int surfaceIndex) => 4;
        protected override int OnSurfaceGetArrayIndexLen(int surfaceIndex) => 6;
        protected override PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex) => PrimitiveType.Triangles;
        protected override ArrayFormat OnSurfaceGetFormat(int surfaceIndex) => ArrayFormat.Vertex | ArrayFormat.Use2DVertices | ArrayFormat.Index | ArrayFormat.Color | ArrayFormat.TexUV;
        protected override Material? OnSurfaceGetMaterial(int surfaceIndex) => null;
        protected override void OnSurfaceSetMaterial(int surfaceIndex, Material? material) => throw new NotSupportedException();
    }
    internal static MeshSurfaceData Quad(Color? color = null) => new() { Vertices = [new(0, 0), new(20, 0), new(20, 20), new(0, 20)], Indices = [0, 1, 2, 0, 2, 3], Colors = [color ?? Colors.Red, color ?? Colors.Red, color ?? Colors.Red, color ?? Colors.Red], UVs = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)] };
    private sealed class DrawNode(Mesh mesh, Texture? texture = null) : Entity
    {
        protected override void OnDraw() => DrawMesh(mesh, texture);
        internal void Record() { QueueRedraw(); PrepareCanvas(); }
        internal void Replay(List<CanvasVertex> vertices, List<CanvasBatch> batches) => AppendCanvas(vertices, batches, Transform.Identity);
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
