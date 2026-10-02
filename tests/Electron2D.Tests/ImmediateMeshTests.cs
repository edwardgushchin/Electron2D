using Electron2D;

internal static class ImmediateMeshTests
{
    internal static void Run()
    {
        using var mesh = new ImmediateMesh();
        Check(mesh.GetSurfaceCount() == 0 && mesh.GetFaces().Length == 0, "Empty immediate mesh.");
        Reject<InvalidOperationException>(() => mesh.SurfaceAddVertex2D(Vector2.Zero));
        Reject<InvalidOperationException>(() => mesh.SurfaceSetColor(Colors.Red));
        Reject<InvalidOperationException>(() => mesh.SurfaceSetUV(Vector2.Zero));
        Reject<InvalidOperationException>(mesh.SurfaceEnd);
        Reject<ArgumentOutOfRangeException>(() => mesh.SurfaceBegin((Mesh.PrimitiveType)5));
        using var material = new CanvasItemMaterial(); material.Dispose();
        Reject<ObjectDisposedException>(() => mesh.SurfaceBegin(Mesh.PrimitiveType.Points, material));
        var changed = 0; mesh.Changed += _ => changed++;
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        Reject<InvalidOperationException>(() => mesh.SurfaceBegin(Mesh.PrimitiveType.Points));
        Reject<ArgumentException>(mesh.SurfaceEnd);
        Reject<ArgumentException>(() => mesh.SurfaceAddVertex2D(new(float.NaN, 0)));
        Reject<ArgumentException>(() => mesh.SurfaceSetColor(new(float.NaN, 0, 0)));
        Reject<ArgumentException>(() => mesh.SurfaceSetUV(new(float.PositiveInfinity, 0)));
        mesh.SurfaceAddVertex2D(new(0, 0));
        mesh.SurfaceSetColor(new(2, -.1f, .5f)); mesh.SurfaceSetUV(new(.25f, .75f));
        mesh.SurfaceAddVertex2D(new(20, 0)); mesh.SurfaceSetColor(Colors.Blue); mesh.SurfaceSetUV(new(1, 0));
        Check(mesh.GetSurfaceCount() == 0 && changed == 0, "Draft and attributes are invisible and silent.");
        Reject<ArgumentException>(mesh.SurfaceEnd);
        mesh.SurfaceAddVertex2D(new(0, 20)); mesh.SurfaceEnd();
        Check(mesh.GetSurfaceCount() == 1 && changed == 1, "Completed surface publishes exactly once.");
        var data = mesh.SurfaceGetArrays(0);
        Check(data.Vertices.Length == 3 && data.Indices.Length == 0 && mesh.SurfaceGetArrayLen(0) == 3 && mesh.SurfaceGetArrayIndexLen(0) == 0, "Sequential typed geometry and counts.");
        Check(data.Colors[0] == new Color(1, 0, 127 / 255f) && data.Colors[1] == data.Colors[0] && data.Colors[2] == Colors.Blue, "First color backfills; later colors capture and quantize.");
        Check(data.UVs[0] == new Vector2(.25f, .75f) && data.UVs[1] == data.UVs[0] && data.UVs[2] == new Vector2(1, 0), "First UV backfills; subsequent UV capture.");
        data.Vertices[0] = Vector2.One; Check(mesh.SurfaceGetArrays(0).Vertices[0] == Vector2.Zero, "Returned arrays are independent.");
        mesh.SurfaceBegin(Mesh.PrimitiveType.Points); mesh.SurfaceAddVertex2D(Vector2.One); mesh.SurfaceEnd();
        Check(mesh.SurfaceGetArrays(1).Colors.Length == 0 && mesh.SurfaceGetArrays(1).UVs.Length == 0 && mesh.SurfaceGetFormat(1) == (Mesh.ArrayFormat.Vertex | Mesh.ArrayFormat.Use2DVertices), "Each surface resets channel activation.");
        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines); mesh.SurfaceAddVertex2D(Vector2.Zero);
        using var duplicate = (ImmediateMesh)mesh.Duplicate();
        Check(duplicate.GetSurfaceCount() == 2, "Duplicates copy completed surfaces without a draft.");
        duplicate.SurfaceBegin(Mesh.PrimitiveType.Points); duplicate.SurfaceAddVertex2D(Vector2.One); duplicate.SurfaceEnd();
        mesh.ClearSurfaces(); Reject<InvalidOperationException>(mesh.SurfaceEnd);
        Check(mesh.GetSurfaceCount() == 0 && duplicate.GetSurfaceCount() == 3 && changed == 3, "Clear cancels draft, emits once and preserves copies.");
        VerifyTopologies(mesh); VerifyFailures(mesh); VerifyGraph(); VerifyReplay();
        var rid = mesh.GetRID(); Check(rid == mesh.GetRID() && ReferenceEquals(RenderingMeshRegistry.Resolve(rid), mesh), "Stable borrowed mesh identity.");
        mesh.Dispose(); Reject<ArgumentException>(() => RenderingMeshRegistry.Resolve(rid));
        Reject<ObjectDisposedException>(() => mesh.SurfaceBegin(Mesh.PrimitiveType.Points)); Reject<ObjectDisposedException>(mesh.ClearSurfaces);
        Reject<ObjectDisposedException>(() => mesh.SurfaceSetColor(Colors.Red)); Reject<ObjectDisposedException>(() => mesh.SurfaceSetUV(Vector2.Zero));
        Reject<ObjectDisposedException>(() => mesh.SurfaceAddVertex2D(Vector2.Zero)); Reject<ObjectDisposedException>(mesh.SurfaceEnd);
        Console.WriteLine("ImmediateMesh drafts, attribute capture, topology, rollback, callbacks, copies, lifetime and warmed replay passed.");
    }
    private static void VerifyTopologies(ImmediateMesh mesh)
    {
        foreach (var primitive in Enum.GetValues<Mesh.PrimitiveType>())
        {
            mesh.SurfaceBegin(primitive);
            mesh.SurfaceAddVertex2D(Vector2.Zero); mesh.SurfaceAddVertex2D(new(20, 0));
            if (primitive is Mesh.PrimitiveType.Triangles or Mesh.PrimitiveType.TriangleStrip) mesh.SurfaceAddVertex2D(new(0, 20));
            mesh.SurfaceEnd();
            Check(mesh.SurfaceGetPrimitiveType((int)primitive) == primitive, "Every topology preserves ordering.");
        }
        Check(mesh.GetFaces().Length == 6, "Triangle and strip faces use inherited extraction.");
        Reject<ArgumentOutOfRangeException>(() => mesh.SurfaceGetArrays(-1)); Reject<ArgumentOutOfRangeException>(() => mesh.SurfaceGetMaterial(5));
        Reject<ArgumentOutOfRangeException>(() => mesh.SurfaceGetArrayIndexLen(5));
    }
    private static void VerifyFailures(ImmediateMesh mesh)
    {
        using var material = new CanvasItemMaterial();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Points, material); mesh.SurfaceAddVertex2D(Vector2.Zero); material.Dispose();
        Reject<ObjectDisposedException>(mesh.SurfaceEnd); Check(mesh.GetSurfaceCount() == 5, "Disposed draft material does not commit.");
        mesh.ClearSurfaces();
        Action<Resource> fail = _ => throw new ApplicationException("Changed listener failure"); mesh.Changed += fail;
        mesh.SurfaceBegin(Mesh.PrimitiveType.Points); mesh.SurfaceAddVertex2D(Vector2.Zero);
        Reject<ApplicationException>(mesh.SurfaceEnd); Check(mesh.GetSurfaceCount() == 1, "Listener failure leaves committed geometry.");
        mesh.SurfaceBegin(Mesh.PrimitiveType.Points); Reject<ApplicationException>(mesh.ClearSurfaces);
        mesh.Changed -= fail; mesh.SurfaceBegin(Mesh.PrimitiveType.Points); mesh.SurfaceAddVertex2D(Vector2.Zero); mesh.SurfaceEnd();
        using var live = new CanvasItemMaterial(); mesh.Changed += fail;
        Reject<ApplicationException>(() => mesh.SurfaceSetMaterial(0, live)); Check(ReferenceEquals(mesh.SurfaceGetMaterial(0), live), "Material replacement commits before callback failure.");
        mesh.Changed -= fail;
        Action<Resource>? reenter = null;
        reenter = _ => { mesh.Changed -= reenter; Task.Run(() => Check(mesh.GetSurfaceCount() == 1, "Changed runs outside the storage lock.")).Wait(); mesh.ClearSurfaces(); };
        mesh.Changed += reenter; mesh.SurfaceSetMaterial(0, null);
        Check(mesh.GetSurfaceCount() == 0 && !live.IsDisposed, "Reentrant clear and borrowed material ownership.");
    }
    private static void VerifyGraph()
    {
        using var material = new CanvasItemMaterial(); using var mesh = new ImmediateMesh();
        Triangle(mesh, material); Triangle(mesh, material);
        using var shallow = (ImmediateMesh)mesh.Duplicate(); using var deep = (ImmediateMesh)mesh.Duplicate(true);
        Check(ReferenceEquals(shallow.SurfaceGetMaterial(0), material), "Shallow copy borrows material.");
        var copiedMaterial = deep.SurfaceGetMaterial(0)!;
        Check(!ReferenceEquals(copiedMaterial, material) && ReferenceEquals(copiedMaterial, deep.SurfaceGetMaterial(1)), "Deep copy preserves shared material aliases.");
        copiedMaterial.Dispose();
        mesh.ResourceLocalToScene = true;
        using var node = new MeshInstance { Mesh = mesh }; using var scene = new PackedScene(); scene.Pack(node);
        using var restored = (MeshInstance)scene.Instantiate();
        var local = (ImmediateMesh)restored.Mesh!;
        Check(!ReferenceEquals(local, mesh) && local.GetSurfaceCount() == 2, "Scene-local immediate mesh copies geometry.");
        local.ClearSurfaces(); Check(mesh.GetSurfaceCount() == 2, "Scene-local edits are independent."); local.Dispose();
        mesh.Dispose(); Check(!material.IsDisposed && shallow.GetSurfaceCount() == 2, "Disposal releases storage and retains borrowed material/copies.");
    }
    private static void VerifyReplay()
    {
        using var mesh = new ImmediateMesh(); using var root = new Node();
        var node = new DrawNode(mesh); root.AddChild(node); using var tree = new SceneTree(root);
        var vertices = new List<CanvasVertex>(128); var batches = new List<CanvasBatch>(8);
        node.Record(); Triangle(mesh); node.Replay(vertices, batches); Check(vertices.Count == 3, "Already recorded draw sees a later commit.");
        mesh.ClearSurfaces(); vertices.Clear(); batches.Clear(); node.Replay(vertices, batches); Check(vertices.Count == 0, "Recorded draw sees clear without rerecording.");
        Triangle(mesh); using var material = new CanvasItemMaterial();
        for (var i = 0; i < 20; i++) { mesh.SurfaceSetMaterial(0, i % 2 == 0 ? material : null); node.Position = new(i % 2, 0); node.Record(); vertices.Clear(); batches.Clear(); node.Replay(vertices, batches); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { mesh.SurfaceSetMaterial(0, i % 2 == 0 ? material : null); node.Position = new(i % 2, 0); node.Record(); vertices.Clear(); batches.Clear(); node.Replay(vertices, batches); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "64 warmed active material/transform/record/replay cycles allocate zero bytes.");
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { vertices.Clear(); batches.Clear(); node.Replay(vertices, batches); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "64 warmed idle replays allocate zero bytes.");
    }
    internal static void Triangle(ImmediateMesh mesh, Material? material = null)
    {
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles, material); mesh.SurfaceAddVertex2D(Vector2.Zero);
        mesh.SurfaceAddVertex2D(new(20, 0)); mesh.SurfaceAddVertex2D(new(0, 20)); mesh.SurfaceEnd();
    }
    private sealed class DrawNode(Mesh mesh) : Entity
    {
        protected override void OnDraw() => DrawMesh(mesh);
        internal void Record() { QueueRedraw(); PrepareCanvas(); }
        internal void Replay(List<CanvasVertex> vertices, List<CanvasBatch> batches) => AppendCanvas(vertices, batches, Transform.Identity);
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
