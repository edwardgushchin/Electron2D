using Electron2D;

internal static class MultiMeshTests
{
    internal static void Run()
    {
        Resources(); Bounds(); CallbackFailures(); Interpolation(); Replay(); SurfaceOrder(); Lifetime();
        Console.WriteLine("MultiMesh copied packed state, finite rollback, tick/interpolation/reset, surface/instance replay, scene ownership and warm active/idle checks passed.");
    }
    private static void Resources()
    {
        using var resource = new MultiMesh();
        Check(resource.InstanceCount == 0 && resource.VisibleInstanceCount == -1 && !resource.UseColors && !resource.UseCustomData && resource.Mesh is null && resource.Buffer.Length == 0, "Defaults.");
        Reject<ArgumentOutOfRangeException>(() => resource.InstanceCount = -1); Reject<ArgumentOutOfRangeException>(() => resource.InstanceCount = int.MaxValue);
        Reject<ArgumentOutOfRangeException>(() => resource.VisibleInstanceCount = 1); Reject<ArgumentOutOfRangeException>(() => resource.VisibleInstanceCount = -2);
        Reject<ArgumentOutOfRangeException>(() => resource.PhysicsInterpolationQualityMode = (MultiMesh.PhysicsInterpolationQuality)2);
        resource.UseColors = resource.UseCustomData = true; resource.InstanceCount = 2;
        Reject<InvalidOperationException>(() => resource.UseColors = true); Reject<InvalidOperationException>(() => resource.UseCustomData = false);
        Check(resource.Buffer.Length == 32 && resource.Buffer.All(v => v == 0), "Allocated transforms and optional channels are cleared.");
        var pose = new Transform(new(2, 3), new(-4, 5), new(6, 7)); resource.SetInstanceTransform2D(0, pose);
        resource.SetInstanceColor(0, new(.1f, .2f, .3f, .4f)); resource.SetInstanceCustomData(0, new(-1, 2, 3, 4));
        var packed = resource.Buffer;
        Check(packed.Take(8).SequenceEqual(new float[] { 2, -4, 0, 6, 3, 5, 0, 7 }) && packed.Skip(12).Take(4).SequenceEqual(new float[] { -1, 2, 3, 4 }), "Independent pinned row-major 2D packing and raw custom components.");
        packed[0] = 20; Check(resource.GetInstanceTransform2D(0) == pose, "Queries return copies."); resource.Buffer = packed; packed[0] = 30; Check(resource.GetInstanceTransform2D(0).X.X == 20, "Assignments copy caller storage.");
        var original = resource.Buffer; var invalid = (float[])original.Clone(); invalid[^1] = float.NaN;
        Reject<ArgumentException>(() => resource.Buffer = invalid); Check(resource.Buffer.SequenceEqual(original), "Later invalid records do not partially mutate storage.");
        Reject<ArgumentException>(() => resource.SetBufferInterpolated(original, invalid)); Check(resource.Buffer.SequenceEqual(original), "Both explicit snapshots preflight before copy.");
        Reject<ArgumentException>(() => resource.SetInstanceTransform2D(1, new(new(float.NaN, 0), Vector2.Up, Vector2.Zero)));
        Reject<ArgumentException>(() => resource.SetInstanceColor(1, new(1, float.PositiveInfinity, 0, 1)));
        Reject<ArgumentOutOfRangeException>(() => resource.GetInstanceTransform2D(2)); Reject<ArgumentOutOfRangeException>(() => resource.GetInstanceCustomData(-1));
        resource.Transform2DArray = [Vector2.Right, Vector2.Down, new(10, 11), Vector2.Left, Vector2.Down, new(12, 13)];
        Check(resource.GetInstanceTransform2D(1).Origin == new Vector2(12, 13), "Legacy transform columns.");
        resource.ColorArray = [Colors.Red, Colors.Blue]; resource.CustomDataArray = [new(2, 3, 4, 5), new(6, 7, 8, 9)];
        Check(resource.GetInstanceColor(1) == Colors.Blue && resource.GetInstanceCustomData(1).A == 9, "Legacy channel arrays.");
        var colors = resource.ColorArray; colors[0] = Colors.White; Check(resource.GetInstanceColor(0) == Colors.Red, "Legacy exports copy.");
        original = resource.Buffer; Reject<ArgumentException>(() => resource.ColorArray = [Colors.White, new(float.NaN, 0, 0, 1)]); Check(resource.Buffer.SequenceEqual(original), "Array mutations preflight every record.");
        resource.ColorArray = []; resource.Transform2DArray = []; resource.VisibleInstanceCount = 2; resource.InstanceCount = 1;
        Check(resource.VisibleInstanceCount == 1 && resource.Buffer.All(v => v == 0), "Resize clears all records and bounds the visible prefix.");
        resource.InstanceCount = 0; resource.UseColors = resource.UseCustomData = false; resource.InstanceCount = 1;
        Reject<InvalidOperationException>(() => resource.GetInstanceColor(0)); Reject<InvalidOperationException>(() => resource.SetInstanceCustomData(0, Colors.White));
        Check(resource.ColorArray.Length == 0 && resource.CustomDataArray.Length == 0, "Disabled arrays are empty.");
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad()); resource.Mesh = mesh;
        resource.SetInstanceTransform2D(0, Transform.Identity); using var shallow = (MultiMesh)resource.Duplicate(); using var deep = (MultiMesh)resource.Duplicate(true);
        Check(ReferenceEquals(shallow.Mesh, mesh) && !ReferenceEquals(deep.Mesh, mesh) && deep.Mesh is ArrayMesh && deep.Buffer.SequenceEqual(resource.Buffer), "Mesh graph policy and independent instance storage.");
        shallow.SetInstanceTransform2D(0, new Transform(0, new(40, 0))); Check(resource.GetInstanceTransform2D(0) == Transform.Identity, "Copies do not alias records.");
        void Failing(Resource _) => throw new ApplicationException("Injected Changed failure."); resource.Changed += Failing;
        Reject<ApplicationException>(() => resource.VisibleInstanceCount = 1); Check(resource.VisibleInstanceCount == 1, "Callbacks fail after state commitment."); resource.Changed -= Failing;
    }
    private static void Bounds()
    {
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad());
        Check(mesh.GetAABB() == new Rect2(Vector2.Zero, new(20, 20)), "Mesh bounds include stored two-dimensional vertices.");
        using var resource = new MultiMesh { Mesh = mesh, InstanceCount = 2 };
        resource.SetInstanceTransform2D(0, new Transform(0, new(10, 20))); resource.SetInstanceTransform2D(1, new Transform(0, new(40, 30)));
        Check(resource.GetAABB() == new Rect2(new(10, 20), new(50, 30)), "Merged local bounds use the visible instance prefix."); resource.VisibleInstanceCount = 1;
        Check(resource.GetAABB() == new Rect2(new(10, 20), new(20, 20)), "Visibility truncates computed bounds.");
        var custom = new Rect2(new(-2, -3), new(5, 7)); resource.CustomAABB = custom; Check(resource.GetAABB() == custom, "Manual bounds bypass automatic computation.");
        Reject<ArgumentException>(() => resource.CustomAABB = new(Vector2.Zero, new(-1, 2))); Check(resource.CustomAABB == custom, "Invalid bounds do not mutate the override."); resource.CustomAABB = default;
        for (var i = 0; i < 20; i++) resource.GetAABB(); var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) resource.GetAABB();
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed computed bounds queries allocate zero bytes.");
        var drawing = new CanvasMultiMesh(resource); var vertices = new List<CanvasVertex>(16); var batches = new List<CanvasBatch>(2);
        drawing.Append(vertices, batches, null, Transform.Identity, Colors.White, null, BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, new(new(90, 90), new(10, 10)), false, 1, new(100, 100));
        Check(vertices.Count == 0, "Scissor rectangle culls the complete visible instance bounds.");
        using var point = new ArrayMesh(); point.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { Vertices = [new(-.25f, 4)] });
        resource.Mesh = point; resource.SetInstanceTransform2D(0, Transform.Identity);
        drawing.Append(vertices, batches, null, Transform.Identity, Colors.White, null, BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false, 1, new(100, 100));
        Check(vertices.Count == 6, "Culling conservatively retains framebuffer-width points whose centers sit outside the viewport.");
    }
    private static void Interpolation()
    {
        using var resource = new MultiMesh { UseColors = true, UseCustomData = true, InstanceCount = 1 };
        resource.SetInstanceTransform2D(0, Transform.Identity); resource.SetInstanceColor(0, Colors.Red); resource.SetInstanceCustomData(0, new(0, 0, 0, 0)); resource.GetRID();
        var current = resource.Buffer; current[0] = -1; current[5] = -1; current[3] = 20; current[8] = 0; current[10] = 1; current[12] = 8;
        resource.SetBufferInterpolated(current, resource.Buffer);
        resource.Presentation(0, .5f, out var fast, out var color, out var custom);
        Check(fast.X == Vector2.Zero && fast.Y == Vector2.Zero && fast.Origin.X == 10 && color == new Color(.5f, 0, .5f, 1) && custom.R == 4, "Fast basis, translation, color and custom-data interpolation.");
        resource.PhysicsInterpolationQualityMode = MultiMesh.PhysicsInterpolationQuality.High;
        resource.Presentation(0, .5f, out var high, out _, out _); Check(Math.Abs(high.X.Length() - 1) < 1e-6f && Math.Abs(high.X.X) < 1e-6f && Math.Abs(high.X.Y) > .99f && high.Origin.X == 10, "High rotates without a collapsing halfway basis.");
        resource.ResetInstancePhysicsInterpolation(0); resource.Presentation(0, .2f, out var reset, out _, out _); Check(reset == resource.GetInstanceTransform2D(0), "Per-instance reset.");
        RenderingMultiMeshRegistry.PhysicsTick(true); resource.SetInstanceTransform2D(0, new Transform(0, new(40, 0))); RenderingMultiMeshRegistry.PhysicsTick(false);
        resource.Presentation(0, .5f, out var tick, out _, out _); Check(tick.Origin.X == 30, "Actual registry tick captures previous before user edits.");
        resource.SetInstanceTransform2D(0, new Transform(0, new(80, 0))); resource.Presentation(0, .1f, out reset, out _, out _); Check(reset.Origin.X == 80, "Outside-tick ordinary edits reset previous state.");
        using var root = new Node(); var toggle = new ToggleNode(); root.AddChild(toggle); using var tree = new SceneTree(root) { PhysicsInterpolation = true }; tree.PhysicsFrame(.01);
        resource.SetInstanceTransform2D(0, new Transform(0, new(100, 0))); resource.Presentation(0, .1f, out reset, out _, out _); Check(reset.Origin.X == 100, "Disabling interpolation inside the tick still closes resource capture.");
        var previous = resource.Buffer; current = (float[])previous.Clone(); previous[3] = -float.MaxValue; current[3] = float.MaxValue; resource.SetBufferInterpolated(current, previous); resource.Presentation(0, .5f, out var huge, out _, out _); Check(huge.Origin.X == 0, "Widened interpolation retains representable cancellation.");
        resource.ResetInstancesPhysicsInterpolation(); using var copy = (MultiMesh)resource.Duplicate(); copy.Presentation(0, 0, out reset, out _, out _); Check(reset == copy.GetInstanceTransform2D(0), "Resource copies reset transient previous snapshots.");
        using var localRoot = new Node(); var consumer = new MultiMeshInstance { MultiMesh = resource }; localRoot.AddChild(consumer);
        using var localTree = new SceneTree(localRoot) { PhysicsInterpolation = true };
        resource.SetBufferInterpolated(current, previous); localRoot.PhysicsInterpolationMode = PhysicsInterpolationMode.Off;
        resource.Presentation(0, .5f, out reset, out _, out _); Check(reset.Origin.X == float.MaxValue, "Inherited Off updates shared resource interpolation policy.");
    }
    private static void CallbackFailures()
    {
        using var mesh = new CallbackMesh(); using var resource = new MultiMesh { Mesh = mesh, InstanceCount = 1 }; resource.SetInstanceTransform2D(0, Transform.Identity);
        var draw = new CanvasMultiMesh(resource); var vertices = new List<CanvasVertex>(16); var batches = new List<CanvasBatch>(2);
        void Replay() { vertices.Clear(); batches.Clear(); draw.Append(vertices, batches, null, Transform.Identity, Colors.White, null, BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false, 1, new(100, 100)); }
        mesh.Read = () => resource.VisibleInstanceCount = 0;
        Reject<InvalidOperationException>(Replay); Check(vertices.Count == 0, "Custom geometry changes cannot evade revision rejection through culling.");
        mesh.Read = null; resource.VisibleInstanceCount = -1; Replay(); Check(vertices.Count == 6, "Failed custom preparation recovers on retry.");
        mesh.Notify(); mesh.Read = () => mesh.GetAABB(); Reject<InvalidOperationException>(() => mesh.GetAABB());
        mesh.Read = null; Check(mesh.GetAABB().Size == new Vector2(20, 20), "Recursive bounds preparation rejects and recovers.");
        mesh.DisposeDuringBounds = true; Reject<ObjectDisposedException>(() => mesh.GetAABB());
    }
    private sealed class CallbackMesh : Mesh
    {
        internal Action? Read; internal bool DisposeDuringBounds;
        internal void Notify() => EmitChanged();
        protected override Rect2 OnGetAABB() { if (DisposeDuringBounds) { Dispose(); return default; } return base.OnGetAABB(); }
        protected override int OnGetSurfaceCount() => 1;
        protected override MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex) { Read?.Invoke(); return MeshTests.Quad(); }
        protected override int OnSurfaceGetArrayLen(int surfaceIndex) => 4;
        protected override int OnSurfaceGetArrayIndexLen(int surfaceIndex) => 6;
        protected override PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex) => PrimitiveType.Triangles;
        protected override ArrayFormat OnSurfaceGetFormat(int surfaceIndex) => ArrayFormat.Vertex | ArrayFormat.Use2DVertices | ArrayFormat.Index | ArrayFormat.Color | ArrayFormat.TexUV;
        protected override Material? OnSurfaceGetMaterial(int surfaceIndex) => null;
        protected override void OnSurfaceSetMaterial(int surfaceIndex, Material? material) => throw new NotSupportedException();
    }
    private sealed class ToggleNode : Node
    {
        internal ToggleNode() { PhysicsProcessEnabled = true; }
        protected override void OnPhysicsProcess(double delta) => Tree!.PhysicsInterpolation = false;
    }
    private static void Replay()
    {
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad(Colors.White));
        using var resource = new MultiMesh { Mesh = mesh, UseColors = true, UseCustomData = true, InstanceCount = 2 };
        resource.SetInstanceTransform2D(0, Transform.Identity); resource.SetInstanceTransform2D(1, new Transform(0, new(30, 0))); resource.SetInstanceColor(0, Colors.Red); resource.SetInstanceColor(1, Colors.Blue); resource.SetInstanceCustomData(1, new(2, 3, 4, 5));
        var node = new DrawNode(resource); using var root = new Node(); root.AddChild(node); using var tree = new SceneTree(root);
        Reject<InvalidOperationException>(() => node.DrawMultiMesh(resource)); node.PrepareCanvas();
        var vertices = new List<CanvasVertex>(4096); var batches = new List<CanvasBatch>(64); node.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 12 && batches.Count == 1 && vertices[0].Color == Colors.Red && vertices[6].Color == Colors.Blue && vertices[6].Position.X == 30 && vertices[6].InstanceCustom == new Color(2, 3, 4, 5), "One batch carries copied transforms/colors/custom values per visible instance.");
        for (var i = 0; i < 20; i++) Cycle(); var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) Cycle(); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed active setters, recording and replay allocate zero managed bytes.");
        resource.VisibleInstanceCount = 0; node.PrepareCanvas(); for (var i = 0; i < 20; i++) Idle(); bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Idle(); Check(GC.GetAllocatedBytesForCurrentThread() == bytes && vertices.Count == 0, "64 warmed idle replays allocate zero bytes and emit no vertices.");
        void Cycle() { resource.SetInstanceTransform2D(1, new Transform(0, new(30, 0))); resource.SetInstanceColor(1, Colors.Blue); node.QueueRedraw(); node.PrepareCanvas(); vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity); }
        void Idle() { vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity); }
    }
    private sealed class DrawNode(MultiMesh resource) : Entity { protected override void OnDraw() => DrawMultiMesh(resource); }
    private static void SurfaceOrder()
    {
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad(Colors.Red));
        var blue = MeshTests.Quad(Colors.Blue); for (var i = 0; i < blue.Vertices.Length; i++) blue.Vertices[i].X += 20;
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, blue);
        using var resource = new MultiMesh { Mesh = mesh, InstanceCount = 2 }; resource.SetInstanceTransform2D(0, Transform.Identity); resource.SetInstanceTransform2D(1, new Transform(0, new(10, 0)));
        var drawing = new DrawNode(resource); using var root = new Node(); root.AddChild(drawing); using var tree = new SceneTree(root); drawing.PrepareCanvas();
        var vertices = new List<CanvasVertex>(32); var batches = new List<CanvasBatch>(2); drawing.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 24 && vertices[6].Color == Colors.Red && vertices[12].Color == Colors.Blue && vertices[12].Position.X == 20, "Every instance of the first surface precedes the second surface, preserving overlapping draw order.");
        var plain = new MeshInstance { Mesh = mesh }; var instances = new MultiMeshInstance { MultiMesh = resource }; root.AddChild(plain); root.AddChild(instances);
        Task.Run(() => { mesh.SurfaceSetName(0, "worker"); resource.SetInstanceTransform2D(0, new Transform(0, new(2, 0))); }).GetAwaiter().GetResult();
        Check(resource.GetInstanceTransform2D(0).Origin.X == 2 && mesh.SurfaceGetName(0) == "worker", "Both mesh consumers accept committed worker resource changes through owner-safe invalidation.");
    }
    private static void Lifetime()
    {
        using var mesh = new ArrayMesh(); using var resource = new MultiMesh { Mesh = mesh, ResourceLocalToScene = true, InstanceCount = 1 }; resource.SetInstanceTransform2D(0, Transform.Identity);
        using var node = new MultiMeshInstance { MultiMesh = resource }; using var scene = new PackedScene(); scene.Pack(node); using var copy = (MultiMeshInstance)scene.Instantiate();
        Check(copy.MultiMesh is not null && !ReferenceEquals(copy.MultiMesh, resource) && copy.MultiMesh.InstanceCount == 1 && !resource.IsDisposed, "Typed scene factory and scene-local resource graph.");
        var events = 0; node.TextureChanged += () => events++; node.Texture = null; Check(events == 0, "Equal texture replacement is silent.");
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        void FailTexture() => throw new ApplicationException("Injected texture listener failure."); node.TextureChanged += FailTexture;
        Reject<ApplicationException>(() => node.Texture = texture); Check(ReferenceEquals(node.Texture, texture) && events == 1, "Texture replacement commits before listener failure."); node.TextureChanged -= FailTexture;
        node.Texture = texture; Check(events == 1, "Equal live texture replacement stays silent."); node.Dispose(); Check(!texture.IsDisposed && !resource.IsDisposed, "Node disposal preserves both borrowed resources.");
        var rid = resource.GetRID(); Check(rid == resource.GetRID() && ReferenceEquals(RenderingMultiMeshRegistry.Resolve(rid), resource), "Stable weak borrowed identity.");
        resource.Dispose(); Reject<ArgumentException>(() => RenderingMultiMeshRegistry.Resolve(rid)); Check(!mesh.IsDisposed, "Disposal releases only owned buffers/identity.");
        Reject<ObjectDisposedException>(() => _ = resource.Buffer); Reject<ObjectDisposedException>(() => resource.GetInstanceTransform2D(0));
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
