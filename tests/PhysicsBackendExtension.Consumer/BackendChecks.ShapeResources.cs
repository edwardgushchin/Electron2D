using Electron2D;

public static partial class BackendChecks
{
    public static void RunShapeResources(PhysicsServer.Backend? backend = null)
    {
        ShapeFactoryChecks(backend);
        if (backend is not null) return;
        Solver? solver = null;
        PhysicsServerManager.RegisterServer("ShapeResources", () => solver = new());
        using var world = new World("ShapeResources");
        using var root = new SubViewport { World = world };
        var scene = new StaticBody { Name = "RollbackView" }; root.AddChild(scene);
        using var tree = new SceneTree(root);
        var first = PhysicsServer.BodyCreate(); var second = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        var shape = PhysicsServer.CircleShapeCreate();
        using var original = new CircleShape { Radius = 3 }; using var next = new CircleShape { Radius = 8 };
        Shape? temporary = null; var restoring = false; var restoredLater = 0;
        try
        {
            PhysicsServer.ShapeSetData(shape, original);
            PhysicsServer.BodyAddShape(scene.GetRID(), shape);
            var oldView = scene.ShapeOwnerGetShape(scene.GetShapeOwners().Single(), 0);
            PhysicsServer.BodyAddShape(first, shape); PhysicsServer.BodyAddShape(second, shape); PhysicsServer.AreaAddShape(area, shape);
            PhysicsServer.BodySetSpace(first, world.Space); PhysicsServer.BodySetSpace(second, world.Space); PhysicsServer.AreaSetSpace(area, world.Space);
            solver!.ShapeHook = id =>
            {
                if (!restoring && id == second)
                {
                    temporary = solver.Bodies[first].LastShape.Shape;
                    temporary.Disposed += _ => throw new ApplicationException("Rejected view retirement failure");
                    restoring = true;
                    throw new ApplicationException("Shape apply failure");
                }
                if (restoring && id == first) throw new InvalidOperationException("Shape first rollback failure");
                if (restoring) restoredLater++;
            };
            var errors = ReleaseErrors(() => PhysicsServer.ShapeSetData(shape, next)); solver.ShapeHook = null;
            Check(errors.Any(error => error.Message == "Shape apply failure") && errors.Any(error => error.Message == "Shape first rollback failure") && errors.Any(error => error.Message == "Rejected view retirement failure"),
                "Failed shared geometry replacement retains apply and rollback errors");
            Check(!oldView.IsDisposed && ReferenceEquals(oldView, scene.ShapeOwnerGetShape(scene.GetShapeOwners().Single(), 0)), "Failed replacement restores the original held scene geometry view");
            Check(restoredLater == 2 && temporary is { IsDisposed: true }, "Rollback attempts later borrowers and retires the rejected private geometry");
            using var point = new PhysicsPointQueryParameters { Position = new(5, 0), CollideWithAreas = true };
            var hits = new PhysicsPointResult[4];
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0 && solver.Bodies.Values.All(body => body.Radius == 3),
                "Public query preparation retries incomplete rollback before returning original geometry");
            PhysicsServer.ShapeSetData(shape, next);
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 4, "A later successful replacement reaches every borrower after rollback failure");
            var committed = scene.ShapeOwnerGetShape(scene.GetShapeOwners().Single(), 0);
            committed.Disposed += _ => throw new ApplicationException("Committed old-view retirement failure");
            var retirement = ReleaseErrors(() => PhysicsServer.ShapeSetData(shape, original));
            Check(retirement.Any(error => error.Message == "Committed old-view retirement failure") && committed.IsDisposed &&
                world.DirectSpaceState.IntersectPoint(point, hits) == 0, "Old-view retirement failure preserves an already committed replacement");
            var single = new ApplicationException("Single apply failure"); var failOnce = true;
            solver.ShapeHook = id => { if (id == second && failOnce) { failOnce = false; throw single; } };
            var recovered = ReleaseErrors(() => PhysicsServer.ShapeSetData(shape, next)); solver.ShapeHook = null;
            Check(recovered.Length == 1 && ReferenceEquals(recovered[0], single) && world.DirectSpaceState.IntersectPoint(point, hits) == 0,
                "Successful rollback rethrows the original single exception and preserves exact query geometry");
            Console.WriteLine("Shared shape rollback: apply/rollback errors, all borrowers, rejected-view retirement and public query retry passed.");
        }
        finally { if (solver is not null) solver.ShapeHook = null; PhysicsServer.FreeRID(first); PhysicsServer.FreeRID(second); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(shape); }
    }
}
