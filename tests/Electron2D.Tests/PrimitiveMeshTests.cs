using System.Diagnostics;
using Electron2D;
using Electron2D.Examples.PrimitiveMeshes;
using static TextureArrayTests;

internal static partial class PrimitiveMeshTests
{
    private sealed class Generator : PrimitiveMesh
    {
        internal MeshSurfaceData Data = Triangle();
        internal Action? During;
        internal int Calls;
        protected override MeshSurfaceData OnCreateMeshArray() { Calls++; During?.Invoke(); return Data; }
    }
    private static MeshSurfaceData Triangle() => new()
    {
        Vertices = [new(0, 0), new(20, 0), new(0, 20)],
        Normals = [new(0, 0, 1), new(0, 0, 1), new(0, 0, 1)],
        UVs = [new(0, 0), new(1, 0), new(0, 1)],
        Indices = [0, 1, 2]
    };
    internal static void Managed()
    {
        using (var cold = new DiskMesh())
        using (var collection = new MultiMesh { Mesh = cold, InstanceCount = 1 }) { collection.SetInstanceTransform2D(0, Transform.Identity); Check(collection.GetAABB().Size.X > 0, "First instance bounds query permits initial generator publication."); }
        using var mesh = new Generator(); var changed = 0; mesh.Changed += _ => changed++;
        using var material = new CanvasItemMaterial();
        Check(!mesh.FlipFaces && !mesh.AddUV2 && mesh.UV2Padding == 2 && mesh.Material is null && mesh.CustomAABB == default, "Generator defaults.");
        mesh.RequestUpdate(); mesh.AddUV2 = true; mesh.Material = material;
        Check(mesh.Calls == 0 && changed == 0 && mesh.SurfaceGetMaterial(0) == material, "Settings and material leave first generation pending.");
        var data = mesh.GetMeshArrays(); Check(mesh.Calls == 1 && changed == 1 && mesh.GetSurfaceCount() == 1 && mesh.SurfaceGetPrimitiveType(0) == Mesh.PrimitiveType.Triangles && mesh.SurfaceGetArrayLen(0) == 3 && mesh.SurfaceGetArrayIndexLen(0) == 3, "First geometry query publishes one triangle surface.");
        Check(data.UV2s[1] == new Vector2(1 - 2f / 1024, 0) && mesh.SurfaceGetFormat(0).HasFlag(Mesh.ArrayFormat.TexUV2), "Pinned fallback padding and channel mask.");
        mesh.Data.Vertices[0] = new(5, 5); data.Vertices[1] = new(99, 99); Check(mesh.GetMeshArrays().Vertices[0] == Vector2.Zero && mesh.SurfaceGetArrays(0).Vertices[1] == new Vector2(20, 0), "Input and output arrays cannot mutate retained geometry.");
        mesh.RequestUpdate(); Check(mesh.Calls == 2 && mesh.GetMeshArrays().Vertices[0] == new Vector2(5, 5), "Later updates synchronously replace generated data.");
        var calls = mesh.Calls; mesh.Material = null; Check(mesh.Calls == calls && mesh.SurfaceGetMaterial(0) is null, "Material swap does not regenerate.");
        mesh.Data = Triangle(); mesh.FlipFaces = true; data = mesh.GetMeshArrays(); Check(data.Indices.SequenceEqual(new[] { 1, 0, 2 }) && data.Normals.All(n => n.Z < -.999f), "Indexed winding and normals reverse together.");
        mesh.Data.Normals = []; mesh.RequestUpdate(); Check(mesh.GetMeshArrays().Indices.SequenceEqual(new[] { 1, 0, 2 }), "Planar winding also reverses without optional normals.");
        mesh.Data.Indices = []; mesh.Data.Colors = [Colors.Red, Colors.Green, Colors.Blue]; mesh.RequestUpdate(); data = mesh.GetMeshArrays(); Check(data.Indices.SequenceEqual(new[] { 1, 0, 2 }) && data.Colors[0] == Colors.Red && data.UVs[1] == new Vector2(1, 0), "Sequential triangles gain indices without scrambling attributes.");
        mesh.FlipFaces = false; mesh.UV2Padding = -1024; Check(mesh.GetMeshArrays().UV2s[1].X == 2, "Negative finite padding expands UVs.");
        mesh.Data.UV2s = [new(4, 5), new(6, 7), new(8, 9)]; mesh.RequestUpdate(); mesh.AddUV2 = false; Check(mesh.GetMeshArrays().UV2s[0] == new Vector2(4, 5), "Authored UV2 survives both generator settings.");
        mesh.Data.UV2s = []; mesh.Data.UVs = []; mesh.AddUV2 = true; Check(mesh.GetMeshArrays().UV2s.Length == 0, "No fallback UV2 is invented without primary UVs.");
        mesh.Data = Triangle(); mesh.Data.Bones = new int[24]; mesh.Data.Weights = Enumerable.Repeat(.5f, 24).ToArray(); mesh.RequestUpdate(); Check(mesh.SurfaceGetFormat(0).HasFlag(Mesh.ArrayFormat.Use8BoneWeights) && mesh.GetMeshArrays().Weights[0] == 32767f / 65535, "Eight-slot storage retains ordinary quantization and format.");
        mesh.Data = Triangle(); mesh.UV2Padding = 2;
        foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }) Reject<ArgumentOutOfRangeException>(() => mesh.UV2Padding = invalid);
        foreach (var index in new[] { -1, 1 }) { Reject<ArgumentOutOfRangeException>(() => mesh.SurfaceGetArrays(index)); Reject<ArgumentOutOfRangeException>(() => mesh.SurfaceSetMaterial(index, null)); Reject<ArgumentOutOfRangeException>(() => mesh.SurfaceGetMaterial(index)); }
        var last = mesh.GetMeshArrays(); var notifications = changed;
        foreach (var bad in new MeshSurfaceData[] { null!, new(), new() { VertexCount = 3 }, new() { Vertices = [new(0, 0)] }, new() { Vertices = [new(float.NaN, 0), new(0, 0), new(1, 1)] }, new() { Vertices = [new(-float.MaxValue, 0), new(float.MaxValue, 0), new(0, 1)] } })
        {
            mesh.Data = bad; try { mesh.RequestUpdate(); throw new Exception("Invalid generator output accepted."); } catch (ArgumentException) { }
            Check(changed == notifications && mesh.GetMeshArrays().Vertices.SequenceEqual(last.Vertices), "Failed update preserves last publication and signals.");
        }
        mesh.Data = Triangle(); mesh.AddUV2 = true; mesh.UV2Padding = -float.MaxValue; mesh.Data.UVs[1] = new(float.MaxValue, 0); Reject<ArgumentException>(mesh.RequestUpdate); mesh.Data = Triangle(); mesh.UV2Padding = 2;
        mesh.During = () =>
        {
            Reject<InvalidOperationException>(mesh.RequestUpdate); Reject<InvalidOperationException>(() => mesh.FlipFaces = true); Reject<InvalidOperationException>(mesh.ResetState);
            Check(Task.Run(() => mesh.UV2Padding).Wait(3000), "Generator executes outside the settings lock.");
            Check(mesh.GetMeshArrays().Vertices.Length == 3, "Existing publication is readable during rebuild.");
        };
        mesh.RequestUpdate(); mesh.During = null;
        using (var initial = new Generator())
        {
            initial.During = () => { Reject<InvalidOperationException>(() => initial.GetSurfaceCount()); Reject<InvalidOperationException>(initial.RequestUpdate); };
            Check(initial.GetSurfaceCount() == 1, "Nested initial generation rejects without corrupting outer build.");
        }
        using (var failing = new Generator { During = () => throw new InvalidOperationException("producer failure") })
        {
            Reject<InvalidOperationException>(() => failing.GetMeshArrays()); failing.During = null; Check(failing.GetSurfaceCount() == 1 && failing.Calls == 2, "Initial callback failure is retryable.");
        }
        Action<Resource> observer = _ => throw new InvalidOperationException("observer"); mesh.Changed += observer; mesh.Data.Vertices[0] = new(2, 3); Reject<InvalidOperationException>(mesh.RequestUpdate); mesh.Changed -= observer;
        Check(mesh.GetMeshArrays().Vertices[0] == new Vector2(2, 3), "Observer failure occurs after publication.");
        mesh.Data = Triangle(); mesh.RequestUpdate(); var source = mesh.GetAABB(); mesh.CustomAABB = new(-50, -40, 100, 90);
        using var item = new Entity(); var draw = new CanvasMesh(mesh, Transform.Identity, Colors.White);
        var vertices = new List<CanvasVertex>(32); var batches = new List<CanvasBatch>(8);
        draw.Append(vertices, batches, null, Transform.Identity, Colors.White, null, BlendMode.Mix, TextureFilter.Linear, TextureRepeat.Disabled, 1, null, false);
        Check(mesh.GetAABB() == source && draw.GetDrawBounds(item) == mesh.CustomAABB && batches.Single().VisibilityBounds == mesh.CustomAABB, "Geometry, command and batch visibility roles remain separate.");
        using var multi = new MultiMesh { Mesh = mesh, InstanceCount = 2 }; multi.SetInstanceTransform2D(0, Transform.Identity); multi.SetInstanceTransform2D(1, new(0, new(200, 0))); Check(multi.GetAABB() == mesh.CustomAABB.Merge(new Transform(0, new(200, 0)) * mesh.CustomAABB), "Child visibility drives instance bounds.");
        multi.CustomAABB = new(2, 3, 4, 5); Check(multi.GetAABB() == multi.CustomAABB, "Collection override takes precedence.");
        foreach (var bad in new[] { new Rect2(0, 0, -1, 2), new Rect2(float.NaN, 0, 2, 2), new Rect2(float.MaxValue, 0, float.MaxValue, 2) }) Reject<ArgumentException>(() => mesh.CustomAABB = bad);
        var beforeReset = mesh.Calls; mesh.FlipFaces = true; mesh.Material = material; mesh.ResetState(); Check(mesh.Calls == beforeReset + 1 && mesh.FlipFaces && mesh.AddUV2 && mesh.Material == material && mesh.CustomAABB != default, "Reset drops transient cache while retaining all stored parameters.");
        vertices.Clear(); batches.Clear(); draw.Append(vertices, batches, null, Transform.Identity, Colors.White, null, BlendMode.Mix, TextureFilter.Linear, TextureRepeat.Disabled, 1, null, false);
        Check(mesh.Calls == beforeReset + 2, "Retained replay realizes the pending cache after reset before capturing its revision.");
        for (var i = 0; i < 16; i++) { vertices.Clear(); batches.Clear(); draw.Append(vertices, batches, null, Transform.Identity, Colors.White, null, BlendMode.Mix, TextureFilter.Linear, TextureRepeat.Disabled, 1, null, false); }
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { vertices.Clear(); batches.Clear(); draw.Append(vertices, batches, null, Transform.Identity, Colors.White, null, BlendMode.Mix, TextureFilter.Linear, TextureRepeat.Disabled, 1, null, false); }
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Prepared generated canvas replay allocates zero owner bytes.");
        using (var disposed = new Generator()) { disposed.During = disposed.Dispose; Reject<ObjectDisposedException>(() => disposed.GetSurfaceCount()); }
        Reject<NotSupportedException>(() => mesh.Duplicate());
        Persistence(); mesh.Dispose(); Reject<ObjectDisposedException>(mesh.RequestUpdate); Reject<ObjectDisposedException>(() => mesh.GetMeshArrays()); Check(!material.IsDisposed, "Borrowed material survives mesh disposal.");
        Console.WriteLine("PrimitiveMesh: generation, topology, channels, validation, failure/reentry, visibility, cache reset, copies/storage and zero prepared owner bytes passed.");
    }
    private static void Register() => ResourceFileTypes.RegisterResource("example.DiskMesh", DiskMesh.Create);
    private static void Persistence()
    {
        Register(); using var material = new CanvasItemMaterial(); using var disk = new DiskMesh { Radius = 28, Segments = 16, AddUV2 = true, FlipFaces = true, UV2Padding = 4, CustomAABB = new(-40, -40, 80, 80), Material = material, ResourceLocalToScene = true };
        Reject<ArgumentOutOfRangeException>(() => disk.Radius = 0); Reject<ArgumentOutOfRangeException>(() => disk.Segments = 2);
        foreach (var deep in new[] { false, true })
        {
            using var copy = (DiskMesh)disk.Duplicate(deep); Check(copy.Radius == 28 && copy.Segments == 16 && copy.FlipFaces && copy.AddUV2 && copy.UV2Padding == 4 && copy.CustomAABB == disk.CustomAABB && ReferenceEquals(copy.Material, material) != deep && copy.GetMeshArrays().Indices.Length == 48, "Consumer factory copies parameters, independent geometry and correct material identity.");
            if (deep) copy.Material!.Dispose();
        }
        using var into = new DiskMesh(); into.CopyFromResource(disk); Check(into.Radius == 28 && into.CustomAABB == disk.CustomAABB, "CopyFromResource preserves generator state.");
        using var node = new MeshInstance { Mesh = disk }; using var scene = new PackedScene(); scene.Pack(node); using var instance = (MeshInstance)scene.Instantiate(); var localized = (DiskMesh)instance.Mesh!;
        Check(!ReferenceEquals(localized, disk) && localized.Radius == 28 && localized.GetSurfaceCount() == 1, "Scene-local factory creates executable geometry.");
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "primitive-mesh-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            ResourceSaver.Save(disk, System.IO.Path.Combine(directory, "disk.e2dres")); ResourceSaver.Save(scene, System.IO.Path.Combine(directory, "disk.e2dscene")); Child(directory, false);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(PrimitiveMeshTests).Assembly.Location); start.ArgumentList.Add(directory); start.Environment["ELECTRON2D_TEST_PRIMITIVE_MESH"] = "child";
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh primitive archive."); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh primitive archives passed"), errors.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(directory, true); localized.Dispose(); }
    }
    internal static void Child(string directory, bool register = true)
    {
        if (register) Register(); using var disk = ResourceLoader.Load<DiskMesh>(System.IO.Path.Combine(directory, "disk.e2dres"), ResourceLoader.CacheMode.Ignore); using var scene = ResourceLoader.Load<PackedScene>(System.IO.Path.Combine(directory, "disk.e2dscene"), ResourceLoader.CacheMode.Ignore); using var instance = (MeshInstance)scene.Instantiate();
        foreach (var resource in new[] { disk, (DiskMesh)instance.Mesh! }) { Check(resource.Radius == 28 && resource.Segments == 16 && resource.AddUV2 && resource.FlipFaces && resource.UV2Padding == 4 && resource.CustomAABB == new Rect2(-40, -40, 80, 80) && resource.Material is CanvasItemMaterial && resource.GetMeshArrays().Indices.Length == 48, "Restored typed generator parameters produce real triangles."); resource.Material!.Dispose(); }
        ((DiskMesh)instance.Mesh!).Dispose(); Console.WriteLine("Fresh primitive archives passed.");
    }
}
