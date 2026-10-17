using Electron2D;

public static partial class BackendChecks
{
    public static void RunShapePolicy(PhysicsServer.Backend? backend = null)
    {
        using var shape = new CircleShape { Radius = 3, CustomSolverBias = .2f };
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ShapePolicy", () =>
        {
            solver = new();
            solver.IdentityHook = _ => Reject<InvalidOperationException>(() => shape.CustomSolverBias = .2f);
            return solver;
        });
        using var world = backend is { } builtin ? new World(builtin) : new World("ShapePolicy");
        using var root = new SubViewport { World = world };
        var scene = new RigidBody { GravityScale = 0, CanSleep = false };
        scene.AddChild(new CollisionShape { Shape = shape }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        if (solver is not null) solver.IdentityHook = null;
        var geometry = shape.GetGeometryData(); var id = shape.GetRID(); var changes = 0;
        shape.Changed += _ => changes++;
        Foreign(() => { Reject<InvalidOperationException>(() => shape.CustomSolverBias = .4f); Reject<InvalidOperationException>(() => shape.CustomSolverBias = .2f); });
        Check(shape.CustomSolverBias == .2f && changes == 0 && shape.GetRID() == id && ReferenceEquals(shape.GetGeometryData(), geometry),
            "Rejected foreign-thread shape policy preserves geometry, identity and observers");
        var raw = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate(); var ownedID = PhysicsServer.CircleShapeCreate();
        using (var initialShapeData = new CircleShape()) PhysicsServer.ShapeSetData(ownedID, initialShapeData);
        var bridge = new StaticBody { Position = new(100, 0) };
        root.AddChild(bridge);
        PhysicsServer.BodyAddShape(bridge.GetRID(), ownedID);
        var owned = bridge.ShapeOwnerGetShape(bridge.GetShapeOwners()[0], 0);
        var ownedGeometry = owned.GetGeometryData(); var ownedChanges = 0;
        owned.Changed += _ => ownedChanges++;
        try
        {
            PhysicsServer.BodyAddShape(raw, ownedID); PhysicsServer.AreaAddShape(area, ownedID);
            PhysicsServer.BodySetTransform(raw, new(0, new(200, 0))); PhysicsServer.BodySetCanSleep(raw, false);
            PhysicsServer.BodySetSpace(raw, world.Space); PhysicsServer.AreaSetSpace(area, world.Space);
            Foreign(Guard);
            if (solver is not null)
            {
                solver.BodyHook = solver.ShapeHook = solver.FilterHook = _ => Guard();
                try
                {
                    PhysicsServer.BodySetLinearVelocity(raw, Vector2.Right);
                    PhysicsServer.BodySetShapeDisabled(raw, 0, true); PhysicsServer.BodySetShapeDisabled(raw, 0, false);
                    PhysicsServer.BodySetCollisionLayer(raw, 2);
                }
                finally { solver.BodyHook = solver.ShapeHook = solver.FilterHook = null; }
                solver.StepHook = Guard;
                try { tree.PhysicsFrame(1d / 120); }
                finally { solver.StepHook = null; }
                solver.StatusHook = () =>
                {
                    Reject<InvalidOperationException>(() => shape.CustomSolverBias = .2f);
                    Reject<InvalidOperationException>(() => shape.Dispose());
                    ShapeGeometry? read = null;
                    var worker = new Thread(() => read = shape.GetGeometryData()) { IsBackground = true };
                    worker.Start();
                    Check(worker.Join(TimeSpan.FromSeconds(2)) && ReferenceEquals(read, geometry),
                        "Selected policy access must not hold the resource geometry lock across user hooks");
                };
                try { shape.CustomSolverBias = .2f; }
                finally { solver.StatusHook = null; }
            }
            Check(changes == 0 && ownedChanges == 0 && owned.CustomSolverBias == 0,
                "Rejected owner/borrowed/reentrant policy leaves both resources and observers unchanged");
            var callbacks = 0;
            PhysicsServer.BodySetStateSyncCallback(raw, _ => { callbacks++; shape.CustomSolverBias = .4f; owned.CustomSolverBias = .5f; });
            tree.PhysicsFrame(1d / 120); PhysicsServer.BodySetStateSyncCallback(raw, null);
            Check(callbacks == 1 && changes == 1 && ownedChanges == 1 && shape.CustomSolverBias == .4f && owned.CustomSolverBias == .5f &&
                ReferenceEquals(shape.GetGeometryData(), geometry) && ReferenceEquals(owned.GetGeometryData(), ownedGeometry),
                "Ordinary post-solver authoring publishes policy without replacing geometry");
            if (solver is not null)
                Check(ReferenceEquals(solver.Bodies[scene.GetRID()].LastShape.Shape, shape) &&
                    ReferenceEquals(solver.Bodies[raw].LastShape.Shape, owned) && solver.Bodies[raw].LastShape.Shape.CustomSolverBias == .5f,
                    "Selected shape descriptors retain current scene/raw resource policy");
            var before = changes;
            shape.CustomSolverBias = .4f;
            Reject<ArgumentOutOfRangeException>(() => shape.CustomSolverBias = float.NaN);
            Reject<ArgumentOutOfRangeException>(() => owned.CustomSolverBias = 1.1f);
            Check(changes == before && owned.CustomSolverBias == .5f, "Equal and invalid policy preserve observer state");
            Action<Resource> fail = _ => throw new InvalidOperationException("Policy observer failure");
            shape.Changed += fail;
            Reject<InvalidOperationException>(() => shape.CustomSolverBias = .6f);
            shape.Changed -= fail;
            Check(shape.CustomSolverBias == .6f && ReferenceEquals(shape.GetGeometryData(), geometry), "Observer failure retains an already committed policy edit");
            for (var i = 0; i < 256; i++) Frame(i);
            Collect(); GC.WaitForPendingFinalizers(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame(i);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Shape policy full frames allocated {own}/{all} bytes");
            Console.WriteLine($"Shape policy {backend?.ToString() ?? "registered"}: owner/borrow/observer/identity guards and 128 active full frames {own}/{all} B passed.");
        }
        finally { PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(ownedID); }
        ShapeConstructionPolicyGuard();

        void Guard()
        {
            Check(shape.CustomSolverBias == .2f && owned.CustomSolverBias == 0, "Borrowed numerical authored policy reads remain available");
            Reject<InvalidOperationException>(() => shape.CustomSolverBias = .4f);
            Reject<InvalidOperationException>(() => shape.CustomSolverBias = .2f);
            Reject<InvalidOperationException>(() => owned.CustomSolverBias = .5f);
            Reject<InvalidOperationException>(() => owned.CustomSolverBias = 0);
        }
        void Frame(int i)
        {
            shape.CustomSolverBias = (i & 1) == 0 ? .4f : .6f;
            owned.CustomSolverBias = (i & 1) == 0 ? .3f : .5f;
            tree.PhysicsFrame(1d / 120);
        }
        static void Foreign(Action action)
        {
            Exception? error = null;
            var thread = new Thread(() => { try { action(); } catch (Exception caught) { error = caught; } });
            thread.Start(); thread.Join();
            if (error is not null) throw error;
        }
    }

    private static void ShapeConstructionPolicyGuard()
    {
        Shape? shape = null;
        using (shape = new CallbackShape(ShapeGeometry.Circle(2), () =>
        {
            Reject<InvalidOperationException>(() => shape!.CustomSolverBias = .2f);
            Reject<InvalidOperationException>(() => shape!.CustomSolverBias = 0);
        }))
        {
            _ = shape.GetGeometryData();
            Check(shape.CustomSolverBias == 0, "Geometry supplier cannot mutate correction policy before publication");
            shape.CustomSolverBias = .2f;
            Check(shape.CustomSolverBias == .2f, "A completed geometry hook releases ordinary policy access");
        }
    }
}
