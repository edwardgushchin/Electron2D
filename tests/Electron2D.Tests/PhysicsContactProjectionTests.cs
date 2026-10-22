using Electron2D;

internal static class PhysicsContactProjectionTests
{
    internal static void Run(PhysicsServer.Backend backend, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { Exercise(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
    private static void Exercise(PhysicsServer.Backend backend, bool threaded)
    {
        using var world = new World(backend);
        using var circle = new CircleShape { Radius = 10 };
        using var floorShape = new RectangleShape { Size = new(600, 20) };
        using var root = new SubViewport { World = world, Size = new(600, 400) };
        using var tree = new SceneTree(root);
        var floor = new StaticBody { Position = new(0, 100) }; floor.AddChild(new CollisionShape { Shape = floorShape }); root.AddChild(floor);
        var temporary = new RID[48]; RID raw = default, shape = default;
        try
        {
            for (var i = 0; i < temporary.Length; i++) { temporary[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetSpace(temporary[i], world.Space); }
            var scene = new RigidBody { Position = new(0, 80), CanSleep = false, MaxContactsReported = 8 };
            scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
            shape = PhysicsServer.CircleShapeCreate(); PhysicsServer.ShapeSetData(shape, circle); raw = NewRaw();
            for (var i = 0; i < temporary.Length; i++) { PhysicsServer.FreeRID(temporary[i]); temporary[i] = default; }
            Step(90);
            var sceneView = PhysicsServer.BodyGetDirectState(scene.GetRID())!; var rawView = PhysicsServer.BodyGetDirectState(raw)!;
            var direction = 1f;
            Measure(Active, "active publication");
            scene.CanSleep = true; scene.Sleeping = true; rawView.Sleeping = true;
            Measure(Idle, "idle publication");
            scene.CanSleep = false; scene.Sleeping = false; rawView.Sleeping = false; Step(2);
            var sceneContacts = Read(sceneView); var rawContacts = Read(rawView);
            Check(sceneContacts.Length > 0 && rawContacts.Length > 0, "Both sparse-index receivers have real solved contacts.");
            Check(sceneContacts.All(c => c.Collider == floor.GetRID() && ReferenceEquals(c.Object, floor) && ReferenceEquals(c.TypedObject, floor)) && rawContacts.All(c => c.Collider == floor.GetRID() && ReferenceEquals(c.Object, floor) && ReferenceEquals(c.TypedObject, floor)), "Scene and raw contact identities name their actual floor.");
            using (var touching = PhysicsServer.SpaceCreateCheckpoint(world.Space))
            {
                scene.GlobalPosition = new(0, -200); PhysicsServer.BodySetTransform(raw, new Transform(0, new Vector2(60, -200))); Step(2);
                Check(sceneView.GetContactCount() == 0 && rawView.GetContactCount() == 0, "A completed separated interval removes old report ranges.");
                using var empty = PhysicsServer.SpaceCreateCheckpoint(world.Space);
                for (var i = 0; i < 64; i++) Cycle();
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
                var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 64; i++) Cycle();
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Console.WriteLine($"Contact projection {backend}, threaded={threaded}: sparse scene/raw receivers, 64 populated/empty replay cycles, owner/all={owner}/{all} B.");
                Check(owner == 0 && all == 0, "Prepared populated/empty contact replay and public getters allocate no managed bytes.");
                touching.Restore();
                void Cycle()
                {
                    touching.Restore(); Check(ReferenceEquals(sceneView, PhysicsServer.BodyGetDirectState(scene.GetRID())) && ReferenceEquals(rawView, PhysicsServer.BodyGetDirectState(raw)), "Restoration preserves both direct-view identities.");
                    Match(sceneView, sceneContacts); Match(rawView, rawContacts);
                    empty.Restore(); Check(sceneView.GetContactCount() == 0 && rawView.GetContactCount() == 0, "Empty restoration cannot expose a later populated projection.");
                }
            }
            PhysicsServer.FreeRID(raw); raw = default; raw = NewRaw();
            var replacement = PhysicsServer.BodyGetDirectState(raw)!;
            Check(!ReferenceEquals(replacement, rawView) && replacement.GetContactCount() == 0, "A replacement body begins without retired contact observations.");
            Step(90); var contacts = Read(replacement);
            Check(contacts.Length > 0 && contacts.All(c => c.Collider == floor.GetRID()), "The reused backend slot receives only the new body's real contacts.");
            Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Numerical contact replay does not start graphics services.");
            void Active()
            {
                direction = -direction; scene.LinearVelocity = new(direction, 0); rawView.LinearVelocity = new(-direction, 0); Step(1);
                Check(sceneView.GetContactCount() > 0 && rawView.GetContactCount() > 0, "Active publication retains both actual contact receivers.");
            }
            void Idle()
            { Step(1); Check(scene.Sleeping && rawView.Sleeping, "Idle publication preserves sleeping bodies."); }
            void Measure(Action cycle, string label)
            {
                for (var i = 0; i < 64; i++) cycle();
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
                var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 64; i++) cycle();
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Console.WriteLine($"Contact projection {backend}, threaded={threaded}: 64 {label} frames, owner/all={owner}/{all} B.");
                Check(owner == 0 && all == 0, "Prepared active/idle contact publication allocates no managed bytes.");
            }
            RID NewRaw()
            {
                var body = PhysicsServer.BodyCreate();
                try
                {
                    PhysicsServer.BodyAddShape(body, shape); PhysicsServer.BodySetMaxContactsReported(body, 8);
                    PhysicsServer.BodySetTransform(body, new Transform(0, new Vector2(60, 80))); PhysicsServer.BodySetSpace(body, world.Space); return body;
                }
                catch { PhysicsServer.FreeRID(body); throw; }
            }
            void Step(int count) { for (var i = 0; i < count; i++) tree.PhysicsFrame(1d / 60); }
        }
        finally
        {
            if (raw.IsValid()) PhysicsServer.FreeRID(raw);
            if (shape.IsValid()) PhysicsServer.FreeRID(shape);
            foreach (var body in temporary) if (body.IsValid()) PhysicsServer.FreeRID(body);
        }
    }
    private readonly record struct Contact(RID Collider, ulong ID, int OwnShape, int OtherShape, Vector2 Position, Vector2 OtherPosition, Vector2 Normal, Vector2 Velocity, Vector2 OtherVelocity, Vector2 Impulse, CollisionObject? Object, StaticBody? TypedObject);
    private static Contact At(PhysicsDirectBodyState view, int index) => new(view.GetContactCollider(index), view.GetContactColliderID(index), view.GetContactLocalShape(index), view.GetContactColliderShape(index), view.GetContactLocalPosition(index), view.GetContactColliderPosition(index), view.GetContactLocalNormal(index), view.GetContactLocalVelocityAtPosition(index), view.GetContactColliderVelocityAtPosition(index), view.GetContactImpulse(index), view.GetContactColliderObject(index), view.GetContactColliderObject<StaticBody>(index));
    private static Contact[] Read(PhysicsDirectBodyState view)
    { var result = new Contact[view.GetContactCount()]; for (var i = 0; i < result.Length; i++) result[i] = At(view, i); return result; }
    private static void Match(PhysicsDirectBodyState view, Contact[] contacts)
    {
        Check(view.GetContactCount() == contacts.Length, "Restored contact count is exact.");
        for (var i = 0; i < contacts.Length; i++) Check(At(view, i) == contacts[i], "Every contact field, identity, shape slot and solved impulse restores exactly.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
