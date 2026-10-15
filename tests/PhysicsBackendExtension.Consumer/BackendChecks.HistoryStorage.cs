using System.Runtime.CompilerServices;
using Electron2D;

public static partial class BackendChecks
{
    public static void RunHistoryStorage(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            if (backend is null) PhysicsServerManager.RegisterServer("HistoryStorage", () => new Solver());
            foreach (var raw in new[] { false, true })
                foreach (var area in new[] { false, true })
                    for (var variant = 0; variant < 10; variant++)
                    {
                        var operation = variant % 5; var empty = variant >= 5;
                        using var world = backend is { } selected ? new World(selected) : new World("HistoryStorage");
                        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
                            "History storage exercises the requested owner without fallback");
                        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root); _ = root.FindWorld();
                        using CollisionObject scene = area ? new Area() : new RigidBody { GravityScale = 0 };
                        var rid = raw ? area ? PhysicsServer.AreaCreate() : PhysicsServer.BodyCreate() : scene.GetRID();
                        if (raw)
                        {
                            if (area) PhysicsServer.AreaSetSpace(rid, world.Space); else PhysicsServer.BodySetSpace(rid, world.Space);
                        }
                        else root.AddChild(scene);
                        try
                        {
                            using var head = new CircleShape { Radius = 2 };
                            var owner = scene.CreateShapeOwner(null); Add(head);
                            using var map = new PhysicsSnapshotMap(world.Space); map.Bind(1, 1, rid);
                            var (shape, checkpoint) = RetireTail(map, world.Space, scene, owner, rid, raw, area, operation, empty);
                            using (checkpoint)
                            {
                                Collect(); var alive = shape.TryGetTarget(out _);
                                Check(!alive, $"History retained a removed shape: backend={backend}, raw={raw}, area={area}, operation={operation}, empty={empty}, separate={threaded}");
                                var snapshot = new PhysicsSnapshot();
                                for (var i = 0; i < 64; i++) Cycle();
                                Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                                for (var i = 0; i < 64; i++) Cycle();
                                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                                Check(own == 0 && all == 0, $"Prepared retained history allocated {own}/{all} B");
                                Console.WriteLine($"History storage {backend?.ToString() ?? "registered"}, raw={raw}, area={area}, operation={operation}, empty={empty}, separate={threaded}: removed shape released, 64 cycles {own}/{all} B.");
                                GC.KeepAlive(map); GC.KeepAlive(checkpoint);
                                void Cycle() { if (checkpoint is not null) { checkpoint.Capture(); checkpoint.Restore(); } else { map.Capture(snapshot); map.Apply(snapshot); } }
                            }
                            void Add(Shape shape)
                            {
                                if (!raw) scene.ShapeOwnerAddShape(owner, shape);
                                else if (area) PhysicsServer.AreaAddShape(rid, shape.GetRID());
                                else PhysicsServer.BodyAddShape(rid, shape.GetRID());
                            }
                        }
                        finally { if (raw) PhysicsServer.FreeRID(rid); }
                    }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference<Shape>, PhysicsCheckpoint?) RetireTail(PhysicsSnapshotMap map, RID space, CollisionObject scene,
        uint owner, RID rid, bool raw, bool area, int operation, bool empty)
    {
        using var shape = new CircleShape { Radius = 3 };
        var weak = new WeakReference<Shape>(shape);
        if (!raw) scene.ShapeOwnerAddShape(owner, shape);
        else if (area) PhysicsServer.AreaAddShape(rid, shape.GetRID());
        else PhysicsServer.BodyAddShape(rid, shape.GetRID());
        var snapshot = new PhysicsSnapshot(); PhysicsCheckpoint? checkpoint = null;
        if (operation == 0) checkpoint = PhysicsServer.SpaceCreateCheckpoint(space); else map.Capture(snapshot);
        if (!raw) { if (empty) scene.ShapeOwnerClearShapes(owner); else scene.ShapeOwnerRemoveShape(owner, 1); }
        else if (area) { if (empty) PhysicsServer.AreaClearShapes(rid); else PhysicsServer.AreaRemoveShape(rid, 1); }
        else { if (empty) PhysicsServer.BodyClearShapes(rid); else PhysicsServer.BodyRemoveShape(rid, 1); }
        switch (operation)
        {
            case 0: checkpoint!.Capture(); break;
            case 1: map.Capture(snapshot); break;
            case 2: Reject<InvalidOperationException>(() => map.Capture(new PhysicsSnapshot(48))); break;
            case 3: Reject<InvalidDataException>(() => map.Apply(snapshot)); break;
            default:
                using (var fresh = new PhysicsSnapshotMap(space)) { fresh.Bind(1, 1, rid); fresh.Capture(snapshot); }
                map.Apply(snapshot); break;
        }
        return (weak, checkpoint);
    }
}
