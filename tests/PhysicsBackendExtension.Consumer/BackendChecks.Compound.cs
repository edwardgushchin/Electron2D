using Electron2D;

public static partial class BackendChecks
{
    public static void RunCompound(PhysicsServer.Backend? backend = null)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("Compound", () => solver = new Solver());
        using var world = backend is { } selected ? new World(selected) : new World("Compound");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 1 };
        using var large = new CircleShape { Radius = 2 };
        var scene = new StaticBody { Position = new(100, 20), Rotation = Mathf.Pi / 2 };
        scene.AddChild(new CollisionShape { Name = "Disabled", Shape = large, Disabled = true });
        scene.AddChild(new CollisionShape { Name = "Upper", Shape = circle, Position = new(8, 0) });
        scene.AddChild(new CollisionShape { Name = "Lower", Shape = large, Position = new(-8, 0) }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[4];
        Point(new(100, 28), 1); Point(new(100, 12), 2);
        point.Position = scene.Position; Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0, "Compound gaps and disabled slots retain empty geometry");
        PhysicsServer.BodySetShapeDisabled(scene.GetRID(), 1, true);
        Point(new(100, 12), 2);
        point.Position = new(100, 28); Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0, "Disabled nonzero slots leave numerical geometry without renumbering siblings");
        PhysicsServer.BodySetShapeDisabled(scene.GetRID(), 1, false);
        using var ray = new PhysicsRayQueryParameters { From = new(100, 40), To = new(100, 0) };
        var rayHit = world.DirectSpaceState.IntersectRay(ray);
        Check(rayHit is { ShapeIndex: 1 } && rayHit.Value.ColliderRID == scene.GetRID() && rayHit.Value.Position.DistanceTo(new(100, 29)) < .002f,
            "Compound ray selects the first transformed slot and its surface");
        using var query = new PhysicsShapeQueryParameters { Shape = circle, Transform = new(0, new Vector2(100, 14.5f)), Margin = 0 };
        var shapeHits = new PhysicsShapeResult[4]; var contacts = new Vector2[8];
        Check(world.DirectSpaceState.IntersectShape(query, shapeHits) == 1 && shapeHits[0].ShapeIndex == 2, "Shape intersection preserves the transformed logical slot");
        Check(world.DirectSpaceState.CollideShape(query, contacts) > 0, "Compound contact query reaches the transformed slot");
        Check(world.DirectSpaceState.GetRestInfo(query) is { ShapeIndex: 2 }, "Rest information preserves the transformed logical slot");
        query.Transform = new(0, new Vector2(100, 40)); query.Motion = new(0, -12);
        var cast = world.DirectSpaceState.CastMotion(query);
        Check(cast.SafeFraction <= 10f / 12 + .00001f && cast.UnsafeFraction >= 10f / 12 - .00001f &&
            cast.UnsafeFraction - cast.SafeFraction <= 3f / 512 + .00001f && MathF.Abs(cast.UnsafeFraction - 10f / 12) <= 1f / 256 + .00001f, $"Shape cast selects the nearest compound feature: {cast.SafeFraction}/{cast.UnsafeFraction}");
        query.Motion = Vector2.Zero;
        if (backend is not null) GeneralCasts(world, root);
        CompoundResponse(world, tree, root, backend, solver);
        Console.WriteLine($"Compound {backend?.ToString() ?? "registered"}: transformed slots, gaps and disabled indices passed.");
        void Point(Vector2 position, int index)
        {
            point.Position = position; var count = world.DirectSpaceState.IntersectPoint(point, hits);
            Check(count == 1 && hits[0].ColliderRID == scene.GetRID() && hits[0].ShapeIndex == index,
                $"Compound transformed point {position} selects logical slot {index}, count={count}");
        }
    }
}
