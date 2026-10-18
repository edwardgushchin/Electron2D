using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;

internal static partial class MeshBoundsTests
{
    internal static void Run()
    {
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad());
        var geometry = mesh.GetAABB(); var manual = new Rect2(-40, -20, 100, 80); var changed = 0; mesh.Changed += _ => changed++;
        Check(mesh.CustomAABB == default, "Automatic visibility initially.");
        mesh.CustomAABB = manual; mesh.CustomAABB = manual;
        Check(changed == 2 && mesh.CustomAABB == manual && mesh.GetAABB() == geometry, "Repeated assignments notify without rewriting geometry bounds.");
        using var item = new Entity(); var draw = new CanvasMesh(mesh, new Transform(.4f, new(8, 12)), Colors.White);
        Check(draw.GetDrawBounds(item) == new Transform(.4f, new(8, 12)) * manual && draw.GetLocalBounds() == geometry, "Only draw bounds include the manual rectangle and draw-local basis.");
        foreach (var bad in new[] { new Rect2(float.NaN, 0, 1, 1), new Rect2(0, 0, -1, 1), new Rect2(0, 0, 1, float.PositiveInfinity), new Rect2(float.MaxValue, 0, float.MaxValue, 1) })
            Reject<ArgumentException>(() => mesh.CustomAABB = bad);
        Check(changed == 2 && mesh.CustomAABB == manual, "Invalid bounds preserve state and notifications.");
        var descriptor = mesh.GetPropertyList().OfType<PropertyDescriptor<ArrayMesh, Rect2>>().Single(p => p.Name == nameof(ArrayMesh.CustomAABB));
        Check(descriptor.IsStored && descriptor.GetValue(mesh) == manual, "Typed stored bounds descriptor.");
        descriptor.SetValue(mesh, default); Check(mesh.CustomAABB == default && draw.GetDrawBounds(item) == new Transform(.4f, new(8, 12)) * geometry, "Zero restores automatic drawing.");
        mesh.SetBlockSignals(true); mesh.CustomAABB = manual; Check(changed == 3 && mesh.CustomAABB == manual, "Signal blocking retains authored bounds."); mesh.SetBlockSignals(false);
        Action<Resource> failure = _ => throw new InvalidOperationException("bounds observer"); mesh.Changed += failure;
        Reject<InvalidOperationException>(() => mesh.CustomAABB = new(4, 5, 6, 7)); mesh.Changed -= failure;
        Check(mesh.CustomAABB == new Rect2(4, 5, 6, 7), "Observer failure retains committed bounds."); mesh.CustomAABB = manual;
        foreach (var deep in new[] { false, true }) { using var copy = (ArrayMesh)mesh.Duplicate(deep); Check(copy.CustomAABB == manual && copy.GetAABB() == geometry, "Both duplication policies retain separate geometry and visibility."); copy.CustomAABB = default; Check(mesh.CustomAABB == manual, "Copy edits are independent."); }
        var entered = false; Action<Resource> reentry = _ => { if (!entered) { entered = true; mesh.CustomAABB = new(3, 4, 5, 6); } };
        mesh.Changed += reentry; mesh.CustomAABB = manual; mesh.Changed -= reentry; Check(entered && mesh.CustomAABB == new Rect2(3, 4, 5, 6), "Reentrant listeners observe committed bounds and can replace them."); mesh.CustomAABB = manual;
        mesh.ResourceLocalToScene = true;
        using (var root = new MeshInstance { Mesh = mesh }) using (var scene = new PackedScene())
        {
            scene.Pack(root); using var local = (MeshInstance)scene.Instantiate(); var localMesh = (ArrayMesh)local.Mesh!;
            Check(!ReferenceEquals(localMesh, mesh) && localMesh.CustomAABB == manual && localMesh.GetAABB() == geometry, "Local scene resources retain copied visibility."); local.Dispose(); localMesh.Dispose();
        }
        mesh.ResourceLocalToScene = false;
        using var placeholder = mesh.CreatePlaceholder(); Check(placeholder.AABB == geometry, "Placeholder uses source geometry bounds.");
        using var multi = new MultiMesh { Mesh = mesh, InstanceCount = 2 }; multi.SetInstanceTransform2D(0, Transform.Identity); multi.SetInstanceTransform2D(1, new(0, new(200, 0)));
        Check(multi.GetAABB() == manual.Merge(new Transform(0, new(200, 0)) * manual), "Automatic instance bounds use mesh visibility.");
        multi.CustomAABB = new(2, 3, 4, 5); Check(multi.GetAABB() == multi.CustomAABB, "Whole-instance override takes precedence."); multi.CustomAABB = default;
        var packed = mesh.ExportPacked(0); packed.AABB = new(-80, -60, 200, 160);
        using var imported = new ArrayMesh(); imported.AppendPacked(ArrayMesh.PreparePacked(packed, 0)); imported.CustomAABB = manual;
        Check(imported.GetAABB() == packed.AABB && new CanvasMesh(imported, Transform.Identity, Colors.White).GetDrawBounds(item) == manual && imported.ExportPacked(0).AABB == packed.AABB, "Mesh override preserves packed surface bounds.");
        using var shaped = new ArrayMesh(); shaped.AddBlendShape("shift"); var basis = MeshTests.Quad(); var delta = new MeshSurfaceData { Vertices = Enumerable.Repeat(new Vector2(2, 0), 4).ToArray() };
        shaped.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, basis, [delta]); shaped.CustomAABB = manual;
        using var pose = new MeshMorphPose(); var output = pose.Prepare(shaped, [.5f]); Check(output.CustomAABB == manual && output.GetAABB().Position == new Vector2(1, 0), "Morph output retains independent manual bounds.");
        shaped.CustomAABB = default; Check(pose.Prepare(shaped, [.5f]).CustomAABB == default, "Live reset reaches prepared morph output.");
        for (var pass = 0; pass < 2; pass++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 128; i++) { mesh.CustomAABB = (i & 1) == 0 ? manual : default; _ = draw.GetDrawBounds(item); _ = multi.GetAABB(); }
            if (pass != 0) Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared bounds edits and mesh/instance queries allocate zero owner bytes.");
        }
        mesh.CustomAABB = manual;
        using (var other = (ArrayMesh)mesh.Duplicate()) using (var root = new Node())
        {
            other.CustomAABB = new(200, 20, 30, 40); var mixed = new MixedDraw(mesh, other); root.AddChild(mixed); using var tree = new SceneTree(root);
            var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
            foreach (var reverse in new[] { false, true })
            {
                mixed.Reverse = reverse; mixed.QueueRedraw(); mixed.PrepareCanvas(); vertices.Clear(); batches.Clear(); mixed.AppendCanvas(vertices, batches, Transform.Identity);
                Check(batches.Count == 3 && batches.Count(batch => batch.VisibilityBounds == manual) == 1 && batches.Count(batch => batch.VisibilityBounds == other.CustomAABB) == 1 && batches.Count(batch => batch.VisibilityBounds is null) == 1, "Mixed draw order preserves each manual and ordinary batch policy.");
            }
        }
        Fresh(mesh);
        mesh.ClearSurfaces(); Check(mesh.CustomAABB == manual && mesh.GetAABB() == default, "Clearing surfaces preserves independent visibility policy.");
        mesh.Dispose(); Reject<ObjectDisposedException>(() => _ = mesh.CustomAABB); Reject<ObjectDisposedException>(() => mesh.CustomAABB = default);
        Reject<InvalidOperationException>(() => RenderingServer.MeshGetCustomAABB(default)); Reject<InvalidOperationException>(() => RenderingServer.MeshSetCustomAABB(default, default));
        Console.WriteLine("Mesh bounds: visibility/geometry separation, validation, notifications, copies, morph/instances, typed storage and zero prepared owner bytes passed.");
    }
    private sealed class MixedDraw(ArrayMesh first, ArrayMesh second) : Entity
    {
        internal bool Reverse;
        protected override void OnDraw()
        {
            if (Reverse) DrawRect(new(400, 0, 8, 8), Colors.White);
            DrawMesh(first); DrawMesh(second);
            if (!Reverse) DrawRect(new(400, 0, 8, 8), Colors.White);
        }
    }
    private static void Fresh(ArrayMesh mesh)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mesh-bounds-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(mesh, path); using var copy = ResourceLoader.Load<ArrayMesh>(path, ResourceLoader.CacheMode.Ignore);
            Check(copy.CustomAABB == mesh.CustomAABB && copy.GetAABB() == mesh.GetAABB(), "Typed archive restores independent visibility bounds.");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(MeshBoundsTests).Assembly.Location);
            start.Environment.Remove("ELECTRON2D_TEST_MESH_BOUNDS"); start.Environment["ELECTRON2D_TEST_MESH_BOUNDS_CHILD"] = path;
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh mesh bounds archive."); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh mesh bounds passed"), errors.GetAwaiter().GetResult());
        }
        finally { File.Delete(path); }
    }
    internal static void Child(string path)
    {
        using var mesh = ResourceLoader.Load<ArrayMesh>(path, ResourceLoader.CacheMode.Ignore);
        Check(mesh.CustomAABB == new Rect2(-40, -20, 100, 80) && mesh.GetAABB() == new Rect2(0, 0, 20, 20), "Fresh persisted custom and source bounds.");
        Console.WriteLine("Fresh mesh bounds passed.");
    }
}
