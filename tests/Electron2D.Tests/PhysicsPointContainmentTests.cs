using Electron2D;

internal static class PhysicsPointContainmentTests
{
    internal static Vector2[] Polygon(bool clockwise)
    {
        var points = new Vector2[12];
        for (var i = 0; i < points.Length; i++) points[i] = new Vector2(12, 0).Rotated(Mathf.Tau * i / points.Length);
        if (clockwise) Array.Reverse(points);
        return points;
    }

    internal static void Run(bool gpu)
    {
        RunBackend(PhysicsServer.Backend.CPU);
        if (gpu) RunBackend(PhysicsServer.Backend.GPU);
    }

    internal static void RunBackend(PhysicsServer.Backend backend)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var display = DisplayServer.IsAvailable; var rendering = RenderingServer.IsAvailable;
        try
        {
            foreach (var separate in OperatingSystem.IsBrowser() ? new[] { false } : new[] { false, true })
            {
                ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
                foreach (var raw in new[] { false, true })
                    foreach (var clockwise in new[] { false, true })
                        foreach (var offset in new[] { false, true }) Verify(backend, raw, clockwise, offset, separate);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Check(DisplayServer.IsAvailable == display && RenderingServer.IsAvailable == rendering, "Point queries preserve display/rendering service state.");
    }

    private static void Verify(PhysicsServer.Backend backend, bool raw, bool clockwise, bool offset, bool separate)
    {
        var points = Polygon(clockwise);
        var origin = new Vector2(1800, 80); const float angle = .37f;
        var local = (new Vector2(1790.1703f, 73.80258f) - origin).Rotated(-angle);
        var normal = Vector2.Zero; var distance = float.NegativeInfinity;
        for (var i = 0; i < points.Length; i++)
        {
            var edge = points[(i + 1) % points.Length] - points[i];
            var n = new Vector2(edge.Y, -edge.X).Normalized() * (clockwise ? -1 : 1);
            var gap = (local - points[i]).Dot(n);
            if (gap > distance) { distance = gap; normal = n; }
        }
        Check(distance is > .001f and < .002f, "The regression point is independently outside the authored contour.");
        var slot = offset ? new Transform(-.18f, new Vector2(3, -2)) : Transform.Identity;
        var shapeAngle = angle + (offset ? -.18f : 0);
        var shapeOrigin = origin + slot.Origin.Rotated(angle);
        normal = normal.Rotated(shapeAngle);
        var outside = offset ? shapeOrigin + local.Rotated(shapeAngle) : new Vector2(1790.1703f, 73.80258f);
        var inside = outside - normal * .01f;
        using var polygon = new ConvexPolygonShape { Points = points };
        using var world = new World(backend);
        using var root = new SubViewport { World = world };
        using var tree = new SceneTree(root);
        RID body, area;
        if (raw)
        {
            body = PhysicsServer.BodyCreate(); area = PhysicsServer.AreaCreate();
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodySetCollisionLayer(body, 1); PhysicsServer.AreaSetCollisionLayer(area, 2);
            PhysicsServer.BodySetCollisionMask(body, 0); PhysicsServer.AreaSetCollisionMask(area, 0);
            PhysicsServer.BodySetTransform(body, new(angle, origin)); PhysicsServer.AreaSetTransform(area, new(angle, origin));
            PhysicsServer.BodyAddShape(body, polygon.GetRID(), slot); PhysicsServer.AreaAddShape(area, polygon.GetRID(), slot);
            PhysicsServer.BodySetSpace(body, world.Space); PhysicsServer.AreaSetSpace(area, world.Space);
        }
        else
        {
            var node = new StaticBody { Position = origin, Rotation = angle, CollisionLayer = 1, CollisionMask = 0 };
            var sensor = new Area { Position = origin, Rotation = angle, CollisionLayer = 2, CollisionMask = 0 };
            node.AddChild(new CollisionShape { Shape = polygon, Transform = slot }); sensor.AddChild(new CollisionShape { Shape = polygon, Transform = slot });
            root.AddChild(node); root.AddChild(sensor); body = node.GetRID(); area = sensor.GetRID();
        }
        try
        {
            var view = world.DirectSpaceState;
            using var point = new PhysicsPointQueryParameters { CollideWithAreas = true, CollisionMask = 3 };
            using var ray = new PhysicsRayQueryParameters { CollisionMask = 1 };
            using var rayShape = new SeparationRayShape { Length = 1 };
            using var shape = new PhysicsShapeQueryParameters { Shape = rayShape, CollisionMask = 1, Margin = 0 };
            var hits = new PhysicsPointResult[2]; var shapeHits = new PhysicsShapeResult[1];
            tree.PhysicsFrame(1d / 60);
            Cycle();
            point.Position = inside; point.CollisionMask = 1;
            Check(view.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == body, "Point masks retain the body identity.");
            point.CollisionMask = 2;
            Check(view.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == area, "Point masks retain the Area identity.");
            point.CollisionMask = 3; point.Exclude = [body];
            Check(view.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == area, "Point exclusions retain the other logical collider.");
            point.Exclude = [];
            PhysicsServer.BodySetShapeDisabled(body, 0, true); PhysicsServer.AreaSetShapeDisabled(area, 0, true);
            Check(view.IntersectPoint(point, hits) == 0, "Disabled logical slots are absent.");
            PhysicsServer.BodySetShapeDisabled(body, 0, false); PhysicsServer.AreaSetShapeDisabled(area, 0, false);
            for (var i = 0; i < 64; i++) Cycle();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
            for (var i = 0; i < 32; i++) Cycle();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Cycle();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, "Warmed complete frames and point/ray/shape queries allocate zero managed bytes.");
            Console.WriteLine($"Point containment {backend}, raw={raw}, clockwise={clockwise}, offset={offset}, separate={separate}: outside/inside/filter/ray origins, owner/all={owner}/{all} B.");

            void Cycle()
            {
                tree.PhysicsFrame(1d / 60);
                point.Position = outside;
                Check(view.IntersectPoint(point, hits) == 0, "A point outside a compound polygon cannot hit a body or Area.");
                point.Position = inside;
                Check(view.IntersectPoint(point, hits) == 2 && hits[0].ColliderRID != hits[1].ColliderRID && hits[0].ShapeIndex == 0 && hits[1].ShapeIndex == 0,
                    "Inside points deduplicate polygon pieces into two logical collider slots.");
                ray.From = outside; ray.To = outside - normal;
                for (var policy = 0; policy < 2; policy++)
                {
                    ray.HitFromInside = policy != 0;
                    var hit = view.IntersectRay(ray);
                    Check(hit is { } value && value.ColliderRID == body && value.Normal.DistanceTo(normal) < .00002f &&
                        value.Position.DistanceTo(outside) is > .0004f and < .004f, "An outside ray gets the physical edge hit under either inside-start policy.");
                }
                ray.From = inside; ray.To = outside + normal;
                ray.HitFromInside = false; Check(view.IntersectRay(ray) is null, "An inside ray skips the entire compound slot.");
                ray.HitFromInside = true;
                Check(view.IntersectRay(ray) is { } start && start.Position == inside && start.Normal == Vector2.Zero, "Inside hit samples the exact origin and zero normal.");
                shape.Transform = new(MathF.Atan2(-normal.Y, -normal.X) - Mathf.Pi / 2, outside);
                Check(view.IntersectShape(shape, shapeHits) == 1 && shapeHits[0].ColliderRID == body, "A directed shape query starting outside retains the compound target.");
                shape.Transform = new(shape.Transform.Rotation, inside);
                Check(view.IntersectShape(shape, shapeHits) == 0, "A directed shape query starting inside skips internal seams.");
            }
        }
        finally { if (raw) { PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(body); } }
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
