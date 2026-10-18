using Electron2D;

public static partial class BackendChecks
{
    private sealed class BorrowedGeometryShape : Shape
    {
        internal Action? Read;
        internal int Reads;
        internal void Arm(Action? read) { EnsureGeometryChangeAccess(); Read = read; EmitGeometryChanged(); }
        protected override ShapeGeometry GetGeometryCore() { Reads++; Read?.Invoke(); return ShapeGeometry.Circle(3); }
    }

    public static void RunGeometryInputs(PhysicsServer.Backend? backend = null, bool threaded = false, bool poolOwner = false)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("GeometryInputs", () => new Solver());
        try { if (poolOwner) { var task = WorkerThreadPool.AddTask(Run); WorkerThreadPool.WaitForTaskCompletion(task); } else Run(); }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver); }
        void Run()
        {
            using var world = backend is { } selected ? new World(selected) : new World("GeometryInputs");
            using var other = backend is { } second ? new World(second) : new World("GeometryInputs");
            using (var finalShape = new BorrowedGeometryShape())
            {
                var finalBody = PhysicsServer.BodyCreate();
                try
                {
                    PhysicsServer.BodySetSpace(finalBody, world.Space);
                    finalShape.Arm(() => Reject<InvalidOperationException>(world.Dispose));
                    PhysicsServer.BodyAddShape(finalBody, finalShape.GetRID());
                    Check(finalShape.Reads == 1 && !world.IsDisposed, "A cold supplier cannot release its world's final resource");
                }
                finally { PhysicsServer.FreeRID(finalBody); }
            }
            using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 2 };
            var moving = new RigidBody
            {
                Position = new(0, 300),
                LinearVelocity = new(1, 0),
                GravityScale = 0,
                CanSleep = false,
                CollisionMask = 0,
                LinearDampMode = RigidBody.DampMode.Replace,
                LinearDamp = 0
            };
            moving.AddChild(new CollisionShape { Shape = circle }); root.AddChild(moving); using var tree = new SceneTree(root);
            using var point = new PhysicsPointQueryParameters { CollideWithAreas = true }; var hits = new PhysicsPointResult[8];
            var roles = new (RID ID, bool Area, CollisionObject? Scene, Vector2 Position)[4]; var shapes = new List<BorrowedGeometryShape>(); var reads = 0; var blocked = 0;
            var bias = PhysicsServer.SpaceGetConstraintDefaultBias(world.Space);
            try
            {
                for (var i = 0; i < roles.Length; i++)
                {
                    var sensor = i % 2 != 0; var position = new Vector2(20 + i * 20, 0); CollisionObject? scene = i < 2 ? null : sensor ? new Area() : new StaticBody();
                    RID id;
                    if (scene is not null) { scene.Position = position; root.AddChild(scene); id = scene.GetRID(); }
                    else
                    {
                        id = sensor ? PhysicsServer.AreaCreate() : PhysicsServer.BodyCreate();
                        if (sensor) { PhysicsServer.AreaSetTransform(id, new(0, position)); PhysicsServer.AreaSetSpace(id, world.Space); }
                        else { PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetTransform(id, new(0, position)); PhysicsServer.BodySetSpace(id, world.Space); }
                    }
                    roles[i] = (id, sensor, scene, position); Add(circle.GetRID(), disabled: true);
                    var first = NewShape(); first.Arm(Inspect); Add(first.GetRID()); Hit(); Check(first.Reads == 1, "Added geometry is sampled once under its using world"); first.Read = null;
                    var replacement = NewShape(); replacement.Arm(Inspect); Set(replacement.GetRID()); Hit(); Check(replacement.Reads == 1, "Replacement geometry is sampled under the same world guard"); replacement.Read = null;
                    replacement.Arm(Inspect); SetTransform(new(0, new(.25f, 0))); Hit(); Check(replacement.Reads == 2, "Dirty transform preparation borrows its cold geometry supplier"); replacement.Read = null;
                    var failure = new IOException("geometry-input-supplier"); replacement.Arm(() => throw failure);
                    Check(ReleaseErrors(Hit).Any(e => ReferenceEquals(e, failure)), "Geometry preparation preserves its original supplier failure");
                    Check(!world.IsDisposed && Count() == 2 && PhysicsServer.SpaceGetConstraintDefaultBias(world.Space) == bias, "Supplier failure leaves world and logical slots intact");
                    replacement.Read = null; Hit(); Check(replacement.Reads == 4, "A failed geometry supplier can retry without a poisoned world");

                    BorrowedGeometryShape NewShape() { var shape = new BorrowedGeometryShape(); shapes.Add(shape); return shape; }
                    int Count() => sensor ? PhysicsServer.AreaGetShapeCount(id) : PhysicsServer.BodyGetShapeCount(id);
                    void Add(RID shape, bool disabled = false) { if (sensor) PhysicsServer.AreaAddShape(id, shape, disabled: disabled); else PhysicsServer.BodyAddShape(id, shape, disabled: disabled); }
                    void Set(RID shape) { if (sensor) PhysicsServer.AreaSetShape(id, 1, shape); else PhysicsServer.BodySetShape(id, 1, shape); }
                    void SetTransform(Transform value) { if (sensor) PhysicsServer.AreaSetShapeTransform(id, 1, value); else PhysicsServer.BodySetShapeTransform(id, 1, value); }
                    void Hit() { point.Position = position; Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == id && hits[0].ShapeIndex == 1, "The sampled procedural shape participates at its actual logical slot"); }
                    void Inspect()
                    {
                        reads++; var count = Count(); var initial = sensor ? PhysicsServer.AreaGetShape(id, 0) : PhysicsServer.BodyGetShape(id, 0);
                        var operationIndex = 0; foreach (var operation in new Action[]
                        {
                            () => root.World = other, () => PhysicsServer.FreeRID(world.Space), () => PhysicsServer.FreeRID(id), tree.Dispose,
                            () => { if (sensor) PhysicsServer.AreaClearShapes(id); else PhysicsServer.BodyClearShapes(id); },
                            () => { if (sensor) PhysicsServer.AreaSetSpace(id, other.Space); else PhysicsServer.BodySetSpace(id, other.Space); },
                            () => PhysicsServer.BodySetLinearVelocity(moving.GetRID(), Vector2.Zero),
                            () => PhysicsServer.SpaceSetConstraintDefaultBias(world.Space, .75f)
                        }) { try { operation(); } catch (InvalidOperationException) { blocked++; operationIndex++; continue; } throw new InvalidOperationException($"Geometry supplier reentry {operationIndex} succeeded for role {i}, read {reads}."); }
                        if (scene is not null) { Reject<InvalidOperationException>(scene.Dispose); blocked++; }
                        Check(!world.IsDisposed && !tree.IsDisposed && Count() == count && initial == circle.GetRID() &&
                            (sensor ? PhysicsServer.AreaGetSpace(id) : PhysicsServer.BodyGetSpace(id)) == world.Space,
                            "All rejected supplier reentry preserves exact owner, slot and resource identities");
                    }
                }
                Check(reads == 12 && blocked == 102 && moving.LinearVelocity == new Vector2(1, 0), "All raw/scene Body/Area supplier paths reject before numerical authoring");
                for (var i = 0; i < 128; i++) Frame(); Collect(); for (var i = 0; i < 128; i++) Frame();
                var start = moving.GlobalPosition; var tick = PhysicsServer.SpaceGetTick(world.Space); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame(); own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128 && MathF.Abs(moving.GlobalPosition.X - start.X - 1.28f) < .001f,
                    $"128 edited geometry-input/physical/query frames allocate {own}/{all} B and preserve actual motion");
                Check(world.RequestedPhysicsBackendName == world.PhysicsBackendName && world.PhysicsFallbackReason is null, "Geometry input guards retain the selected numerical implementation");
                Console.WriteLine($"Geometry inputs {backend?.ToString() ?? "registered"}/{threaded}/{poolOwner}: 13 suppliers, 103 rejected reentries and 128 moving edited full frames {own}/{all} B passed.");
                void Frame()
                {
                    var offset = new Transform(0, new((PhysicsServer.SpaceGetTick(world.Space) & 1) == 0 ? .25f : .5f, 0));
                    foreach (var role in roles) { if (role.Area) PhysicsServer.AreaSetShapeTransform(role.ID, 1, offset); else PhysicsServer.BodySetShapeTransform(role.ID, 1, offset); }
                    tree.PhysicsFrame(.01); point.Position = moving.GlobalPosition;
                    Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == moving.GetRID(), "Edited input publication retains the moving numerical collider");
                }
            }
            finally
            {
                foreach (var role in roles) { if (role.Scene is not null) role.Scene.Dispose(); else if (role.ID.IsValid()) PhysicsServer.FreeRID(role.ID); }
                foreach (var shape in shapes) shape.Dispose();
            }
        }
    }
}
