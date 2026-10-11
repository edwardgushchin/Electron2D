using Electron2D;

public static partial class BackendChecks
{
    private static void ShapeMutationChecks()
    {
        Solver? solver = null;
        PhysicsServerManager.RegisterServer("ShapeMutations", () => solver = new());
        using var world = new World("ShapeMutations");
        var space = world.Space;
        var body = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        var shape = PhysicsServer.CircleShapeCreate();
        using var authored = new CircleShape { Radius = 6 };
        using var replacement = new CircleShape { Radius = 9 };
        using var point = new PhysicsPointQueryParameters { CollideWithAreas = true };
        var hits = new PhysicsPointResult[2];
        try
        {
            PhysicsServer.ShapeSetData(shape, authored);
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(body, shape); PhysicsServer.AreaAddShape(area, shape);
            solver!.IdentityHook = CheckBorrow;
            PhysicsServer.BodySetSpace(body, space); PhysicsServer.AreaSetSpace(area, space);
            solver.IdentityHook = null;
            CheckUnchanged();
            solver.ShapeHook = CheckBorrow; solver.FilterHook = CheckBorrow;
            PhysicsServer.BodySetShapeTransform(body, 0, new(0, new Vector2(1, 0)));
            PhysicsServer.AreaSetShapeTransform(area, 0, new(0, new Vector2(1, 0)));
            PhysicsServer.BodySetCollisionLayer(body, 2); PhysicsServer.AreaSetCollisionLayer(area, 2);
            solver.ShapeHook = null; solver.FilterHook = null;
            Check(PhysicsServer.BodyGetShapeTransform(body, 0).Origin == new Vector2(1, 0) &&
                PhysicsServer.AreaGetShapeTransform(area, 0).Origin == new Vector2(1, 0) &&
                PhysicsServer.BodyGetCollisionLayer(body) == 2 && PhysicsServer.AreaGetCollisionLayer(area) == 2,
                "Outer shape/filter edits commit after rejecting nested edits");
            Check(solver.Bodies[body].LastShape.Transform.Origin == new Vector2(1, 0) &&
                solver.Bodies[area].LastShape.Transform.Origin == new Vector2(1, 0), "Logical transforms reach both selected attachments");
            point.Position = new(8, 0);
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0, "Rejected shared data replacement preserves numerical geometry");
            point.Position = default;
            point.CollisionMask = 1;
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0, "Selected queries observe edited collision layers");
            point.CollisionMask = 2;
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 2, "Selected shape/filter state remains queryable after rejected edits");
            PhysicsServer.BodySetShapeDisabled(body, 0, true); PhysicsServer.AreaSetShapeDisabled(area, 0, true);
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0, "Disabled slots leave the numerical implementation");
            PhysicsServer.BodySetShapeDisabled(body, 0, false); PhysicsServer.AreaSetShapeDisabled(area, 0, false);
            PhysicsServer.BodySetShapeAsOneWayCollision(body, 0, true, 3, Vector2.Left);
            Check(solver.Bodies[body].LastShape.OneWay is { Margin: 3 } oneWay && oneWay.LocalDirection == Vector2.Left,
                "Exact one-way authoring reaches the selected shape span");
            PhysicsServer.BodySetShape(body, 0, replacement.GetRID()); PhysicsServer.AreaSetShape(area, 0, replacement.GetRID());
            point.Position = new(8, 0);
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 2, "Ordinary resource replacement changes real selected geometry");
            PhysicsServer.BodySetShape(body, 0, shape); PhysicsServer.AreaSetShape(area, 0, shape);
            PhysicsServer.BodyRemoveShape(body, 0); PhysicsServer.AreaRemoveShape(area, 0);
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0, "Ordinary removals retire numerical shapes");
            PhysicsServer.BodyAddShape(body, shape); PhysicsServer.AreaAddShape(area, shape);
            PhysicsServer.BodyClearShapes(body); PhysicsServer.AreaClearShapes(area);
            Check(PhysicsServer.BodyGetShapeCount(body) == 0 && PhysicsServer.AreaGetShapeCount(area) == 0 &&
                solver.Bodies[body].Shapes == 0 && solver.Bodies[area].Shapes == 0, "Ordinary clear updates common and selected topology");
            PhysicsServer.BodyAddShape(body, shape); PhysicsServer.AreaAddShape(area, shape);
            PhysicsServer.ShapeSetData(shape, replacement);
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 2, "Ordinary shared data replacement reaches both attached users");
            PhysicsServer.ShapeSetData(shape, authored); point.Position = default;
            Collect(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) QueryAndFilter(i);
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) QueryAndFilter(i);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Registered filters/queries allocated {own}/{all} owner/all-thread B");
            Task.Run(() =>
            {
                Reject<InvalidOperationException>(() => PhysicsServer.BodyClearShapes(body));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetCollisionMask(area, 0));
            }).GetAwaiter().GetResult();
            SceneBorrowChecks();
            Console.WriteLine($"Registered shape/filter borrowing and warm queries: {own}/{all} owner/all-thread B");
        }
        finally
        {
            solver!.IdentityHook = null; solver.ShapeHook = null; solver.FilterHook = null;
            PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(shape);
        }

        void CheckBorrow(RID current)
        {
            Check((current == body ? PhysicsServer.BodyGetShapeCount(current) : PhysicsServer.AreaGetShapeCount(current)) == 1,
                "Borrowed hooks can inspect retained logical slots");
            if (current == body)
            {
                Reject<InvalidOperationException>(() => PhysicsServer.BodyClearShapes(body));
                Reject<InvalidOperationException>(() => PhysicsServer.BodyAddShape(body, shape));
                Reject<InvalidOperationException>(() => PhysicsServer.BodyRemoveShape(body, 0));
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetShape(body, 0, replacement.GetRID()));
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetShapeTransform(body, 0, new(0, new Vector2(9, 0))));
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetShapeDisabled(body, 0, true));
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetShapeAsOneWayCollision(body, 0, true, 3));
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetCollisionLayer(body, 8));
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetCollisionMask(body, 8));
            }
            else
            {
                Reject<InvalidOperationException>(() => PhysicsServer.AreaClearShapes(area));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaAddShape(area, shape));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaRemoveShape(area, 0));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetShape(area, 0, replacement.GetRID()));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetShapeTransform(area, 0, new(0, new Vector2(9, 0))));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetShapeDisabled(area, 0, true));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetCollisionLayer(area, 8));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetCollisionMask(area, 8));
            }
            Reject<InvalidOperationException>(() => PhysicsServer.ShapeSetData(shape, replacement));
            Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(shape));
        }
        void CheckUnchanged()
        {
            Check(PhysicsServer.BodyGetShapeCount(body) == 1 && PhysicsServer.AreaGetShapeCount(area) == 1 &&
                PhysicsServer.BodyGetShape(body, 0) == shape && PhysicsServer.AreaGetShape(area, 0) == shape &&
                PhysicsServer.BodyGetShapeTransform(body, 0) == Transform.Identity && PhysicsServer.AreaGetShapeTransform(area, 0) == Transform.Identity &&
                PhysicsServer.BodyGetCollisionLayer(body) == 1 && PhysicsServer.AreaGetCollisionLayer(area) == 1 &&
                PhysicsServer.BodyGetCollisionMask(body) == 1 && PhysicsServer.AreaGetCollisionMask(area) == 1,
                "Initial borrowed hooks preserve all retained shape slots and filters");
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 2, "Initial shape geometry reaches both numerical attachments");
        }
        void QueryAndFilter(int i)
        {
            var layer = (uint)(1 << (i & 1));
            PhysicsServer.BodySetCollisionLayer(body, layer); PhysicsServer.AreaSetCollisionLayer(area, layer);
            point.CollisionMask = layer;
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 2, "Warm filtered query retains both colliders");
        }
        void SceneBorrowChecks()
        {
            using var root = new SubViewport { World = world };
            var sceneBody = new StaticBody { Position = new(30, 0) }; var sceneArea = new Area { Position = new(30, 0) };
            sceneBody.AddChild(new CollisionShape { Shape = authored }); sceneArea.AddChild(new CollisionShape { Shape = authored });
            root.AddChild(sceneBody); root.AddChild(sceneArea);
            using var tree = new SceneTree(root);
            var b = sceneBody.GetRID(); var a = sceneArea.GetRID();
            solver!.IdentityHook = current =>
            {
                if (current == b)
                {
                    Reject<InvalidOperationException>(() => PhysicsServer.BodyClearShapes(b));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetShapeTransform(b, 0, Transform.Identity));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetCollisionLayer(b, 0));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetCollisionMask(b, 0));
                }
                else if (current == a)
                {
                    Reject<InvalidOperationException>(() => PhysicsServer.AreaClearShapes(a));
                    Reject<InvalidOperationException>(() => PhysicsServer.AreaSetShapeTransform(a, 0, Transform.Identity));
                    Reject<InvalidOperationException>(() => PhysicsServer.AreaSetCollisionLayer(a, 0));
                    Reject<InvalidOperationException>(() => PhysicsServer.AreaSetCollisionMask(a, 0));
                }
            };
            tree.PhysicsFrame(0); solver.IdentityHook = null;
            point.Position = sceneBody.Position; point.CollisionMask = 1;
            var found = world.DirectSpaceState.IntersectPoint(point, hits);
            Check(found == 2 && hits.Any(h => h.ColliderRID == b) && hits.Any(h => h.ColliderRID == a),
                "Scene body/Area borrowing preserves the same observable shape/filter contract");
        }
    }
}
