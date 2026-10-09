using Electron2D;

internal static class PhysicsDebugTests
{
    internal static void Run(bool includeGPU = true)
    {
        var color = ProjectSettings.Get(ProjectSettings.DebugCollisionShapeColor); var outlines = ProjectSettings.Get(ProjectSettings.DebugCollisionDrawOutlines);
        try
        {
            ProjectSettings.Set(ProjectSettings.DebugCollisionShapeColor, Colors.Green);
            Defaults(); Verify(PhysicsServer.Backend.CPU);
            if (Environment.GetEnvironmentVariable("ELECTRON2D_DEBUG_CPU_ONLY") == "1") Check(!RenderingServer.IsAvailable && !DisplayServer.IsAvailable, "CPU debug recording needs neither renderer nor display");
            else if (includeGPU) Verify(PhysicsServer.Backend.GPU);
        }
        finally { ProjectSettings.Set(ProjectSettings.DebugCollisionShapeColor, color); ProjectSettings.Set(ProjectSettings.DebugCollisionDrawOutlines, outlines); }
    }
    private static void Defaults()
    {
        using var shape = new CircleShape(); using var body = new StaticBody();
        var child = new CollisionShape { Name = "Shape", Shape = shape }; body.AddChild(child); child.Owner = body;
        Check(child.DebugColor == Colors.Green, "Default diagnostic color samples project settings");
        ProjectSettings.Set(ProjectSettings.DebugCollisionShapeColor, Colors.Blue);
        Check(child.DebugColor == Colors.Green, "Existing nodes keep their sampled color");
        child.DebugColor = Colors.Pink;
        using var scene = new PackedScene(); scene.Pack(body); using var copy = (StaticBody)scene.Instantiate();
        Check(((CollisionShape)copy.GetChild(0)).DebugColor == Colors.Pink, "Stored debug color survives scene packing");
        Reject<ArgumentException>(() => child.DebugColor = new(float.NaN, 0, 0));
        Reject<ArgumentException>(() => shape.Draw(default, new(float.NaN, 0, 0)));
        Reject<InvalidOperationException>(() => shape.Draw(default, Colors.Red));
        ProjectSettings.Set(ProjectSettings.DebugCollisionShapeColor, Colors.Green);
    }
    private static void Verify(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world, Size = new(400, 300) };
        using var circle = new CircleShape { Radius = 10 };
        var body = new StaticBody { Name = "Body", Position = new(40, 40) };
        var collision = new CollisionShape { Name = "Collision", Shape = circle }; body.AddChild(collision); root.AddChild(body);
        var ray = new RayCast { Name = "Ray", Position = new(40, 10), TargetPosition = new(0, 60) }; root.AddChild(ray);
        var cast = new ShapeCast { Name = "Cast", Shape = circle, Enabled = false, Position = new(150, 50), TargetPosition = new(0, 40) }; root.AddChild(cast);
        var polygon = new CollisionPolygon { Name = "Polygon", Polygon = [new(0, 0), new(20, 0), new(10, 10)], OneWayCollision = true }; root.AddChild(polygon);
        var pin = new PinJoint { Name = "Pin" }; var groove = new GrooveJoint { Name = "Groove" }; var spring = new DampedSpringJoint { Name = "Spring" };
        root.AddChild(pin); root.AddChild(groove); root.AddChild(spring);
        using var tree = new SceneTree(root);
        var vertices = new List<CanvasVertex>(65536); var batches = new List<CanvasBatch>(1024);
        CanvasItem[] nodes = [collision, ray, cast, polygon, pin, groove, spring];
        Check(!tree.DebugCollisionsHint, "Collision diagnostics start disabled");
        foreach (var node in nodes) { Record(node, vertices, batches); Check(vertices.Count == 0, "All diagnostic families are silent by default"); }
        tree.DebugCollisionsHint = true;
        foreach (var node in nodes) { Record(node, vertices, batches); Check(vertices.Count > 0, "Enabled diagnostic families record geometry"); }
        Record(collision, vertices, batches); Check(vertices[0].Color == Colors.Green, "Shape color reaches fill vertices");
        collision.Disabled = true; Record(collision, vertices, batches);
        Check(vertices[0].Color == new Color(1, 1, 1, .5f), "Disabled color is half-alpha gray"); collision.Disabled = false;
        collision.OneWayCollision = true; collision.OneWayCollisionDirection = Vector2.Right; Record(collision, vertices, batches);
        Check(collision.RenderBounds().End.X >= 28, "One-way marker follows local direction"); collision.OneWayCollision = false;
        Task.Run(() => circle.Radius = 15).GetAwaiter().GetResult(); Record(collision, vertices, batches);
        Check(collision.RenderBounds().Size.X >= 30, "Resource edits from a worker invalidate drawing");
        Action<Resource> fail = _ => throw new ApplicationException("shape edit callback");
        circle.Changed += fail; Reject<ApplicationException>(() => circle.Radius = 16); circle.Changed -= fail;
        Record(collision, vertices, batches); Check(collision.RenderBounds().Size.X >= 32, "Committed geometry redraws even when a change subscriber throws");
        Reject<InvalidOperationException>(() => Task.Run(() => tree.DebugCollisionsHint = false).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => collision.DebugColor = Colors.Red).GetAwaiter().GetResult());
        Action<CanvasItem> cover = node => node.DrawRect(new(-2, -2, 4, 4), Colors.Blue);
        pin.Draw += cover; pin.QueueRedraw(); Record(pin, vertices, batches); Check(vertices[^1].Color == Colors.Blue, "User drawing follows built-in joint diagnostics"); pin.Draw -= cover;
        groove.Length = 80; groove.InitialOffset = 90; Record(groove, vertices, batches); Check(groove.RenderBounds().End.Y > 90, "Guide extent/offset edits redraw");
        spring.Length = 100; Record(spring, vertices, batches); Check(spring.RenderBounds().End.Y > 100, "Spring extent edit redraws");
        ray.ForceRaycastUpdate(); Record(ray, vertices, batches); Check(vertices[0].Color == new Color(1, .01f, 0), "Cached ray hit changes diagnostic color");
        body.Position = new(200, 200); ray.ForceRaycastUpdate(); Record(ray, vertices, batches); Check(vertices[0].Color == Colors.Green, "A later miss redraws the cached state");
        cast.Shape = null; Record(cast, vertices, batches); Check(vertices.Count == 0, "Removing the cast shape clears retained geometry"); cast.Shape = circle;
        using var tiny = new CircleShape { Radius = .00001f }; cast.Shape = tiny; cast.TargetPosition = new(1e9f, 0);
        Reject<InvalidOperationException>(() => Record(cast, vertices, batches)); Check(!cast.HasCanvasCommands, "Excessive diagnostic work fails without partial commands");
        cast.Shape = circle; cast.TargetPosition = new(0, 40);
        tree.DebugCollisionsHint = false;
        foreach (var node in nodes) { Record(node, vertices, batches); Check(vertices.Count == 0, "Disabling diagnostics clears every family"); }
        tree.DebugCollisionsHint = true;
        var shapes = Shapes(); var painters = new Painter[shapes.Length];
        try
        {
            for (var i = 0; i < shapes.Length; i++) { painters[i] = new Painter(shapes[i]) { Name = "Shape" + i }; root.AddChild(painters[i]); Record(painters[i], vertices, batches); Check(vertices.Count > 0, "Every applicable shape family records"); }
            ProjectSettings.Set(ProjectSettings.DebugCollisionDrawOutlines, true); collision.QueueRedraw(); Record(collision, vertices, batches); var withOutlines = vertices.Count;
            ProjectSettings.Set(ProjectSettings.DebugCollisionDrawOutlines, false); collision.QueueRedraw(); Record(collision, vertices, batches); Check(vertices.Count < withOutlines, "Project outlines affect next recording");
            using var zero = new CapsuleShape { Radius = 0, Height = 0 }; using var empty = new ConvexPolygonShape();
            using var degenerate = new Painter(zero); Record(degenerate, vertices, batches); degenerate.Shape = empty; degenerate.InvalidateCanvas(); Record(degenerate, vertices, batches); Check(vertices.Count == 0, "Empty geometry has no fill");
            ProjectSettings.Set(ProjectSettings.DebugCollisionDrawOutlines, true);
            tree.PhysicsFrame(1d / 60); var store = PhysicsServer.Service.GetSceneSpace(world.Space).GPUStore;
            for (var frame = 0; frame < 96; frame++) Cycle();
            var read = store?.ReadbackBytes ?? 0; var submits = store?.SubmissionCount ?? 0;
            var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
            for (var frame = 0; frame < 64; frame++) Cycle();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Warmed rerecord/replay allocates {owner}/{all} managed B");
            Check((store?.ReadbackBytes ?? 0) == read && (store?.SubmissionCount ?? 0) == submits, "Drawing performs no GPU physics work or readback");
            Console.WriteLine($"Physics debug {backend}: all shapes/casts/joints, live edits, packing, ownership, 96 warmup/64 rerecord+replay samples, {owner}/{all} owner/all-thread managed B, 0 physics submissions/readback.");
            void Cycle()
            {
                foreach (var node in nodes) { node.QueueRedraw(); Record(node, vertices, batches); }
                foreach (var node in painters) { node.QueueRedraw(); Record(node, vertices, batches); }
            }
        }
        finally { foreach (var shape in shapes) shape.Dispose(); }
        circle.Dispose(); Record(collision, vertices, batches); Record(cast, vertices, batches); Check(vertices.Count == 0 && !collision.HasCanvasCommands, "Disposed borrowed geometry clears nodes");
    }
    internal static Shape[] Shapes() => [new CircleShape { Radius = 8 }, new CapsuleShape { Radius = 5, Height = 24 }, new CapsuleShape { Radius = 6, Height = 12 },
        new RectangleShape { Size = new(20, 14) }, new SegmentShape { A = new(-10, 0), B = new(10, 0) },
        new ConvexPolygonShape { Points = [new(-10, 6), new(0, -10), new(10, 6)] },
        new ConcavePolygonShape { Segments = [new(-10, 0), new(0, -10), new(0, -10), new(10, 0)] },
        new SeparationRayShape { Length = 24 }, new WorldBoundaryShape { Normal = new(0, -2), Distance = 4 }];
    private static void Record(CanvasItem node, List<CanvasVertex> vertices, List<CanvasBatch> batches)
    { node.PrepareCanvas(); vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity); }
    private sealed class Painter(Shape shape) : Entity
    {
        internal Shape Shape = shape;
        protected override void OnDraw() => Shape.DrawToCanvas(this, Colors.Pink);
    }
    internal static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    internal static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
