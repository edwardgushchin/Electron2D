using Electron2D;

internal static class PhysicsServerShapeSlotTests
{
    private sealed class RIDResource(RID rid) : Resource
    {
        public override RID GetRID() { _ = base.GetRID(); return rid; }
    }

    private static Transform At(float x, float y = 0, float rotation = 0) => new(rotation, Vector2.One, 0, new(x, y));

    internal static void Run()
    {
        using (var resource = new Resource()) Check(!resource.GetRID().IsValid(), "A base managed resource has no backend RID.");
        VerifyExplicitSlots();
        VerifySceneSlots();
        VerifyOneWay(false);
        VerifyOneWay(true);
        VerifyGuardsAndLifetime();
        Console.WriteLine("Physics scene/server shape slots, replacement, transforms, one-way response and lifetime passed.");
    }

    private static bool Point(RID space, RID owner, Vector2 point, int index = 0, bool areas = false)
    {
        using var query = new PhysicsPointQueryParameters2D
        {
            Position = point,
            CollideWithBodies = !areas,
            CollideWithAreas = areas
        };
        return PhysicsServer.Instance.SpaceGetDirectState(space).IntersectPoint(query, 16)
            .Any(hit => hit.ColliderRID == owner && hit.ShapeIndex == index);
    }

    private static void VerifyExplicitSlots()
    {
        var physics = PhysicsServer.Instance;
        var space = physics.SpaceCreate(); var body = physics.BodyCreate(); var area = physics.AreaCreate();
        var circle = physics.CircleShapeCreate(); var rectangle = physics.RectangleShapeCreate();
        try
        {
            using var small = new CircleShape { Radius = 5 };
            using var box = new RectangleShape { Size = new(20, 6) };
            physics.ShapeSetData(circle, small); physics.ShapeSetData(rectangle, box);
            physics.BodySetMode(body, PhysicsServer.BodyMode.Static);
            physics.BodyAddShape(body, circle); physics.BodyAddShape(body, circle, At(50));
            physics.AreaAddShape(area, circle); physics.AreaSetTransform(area, At(100));
            Check(physics.BodyGetShape(body, 0) == circle && physics.BodyGetShapeTransform(body, 0) == Transform.Identity &&
                  physics.AreaGetShape(area, 0) == circle, "Detached slot getters preserve exact RID and default pose.");
            physics.BodySetSpace(body, space); physics.AreaSetSpace(area, space);
            Check(!Point(space, body, new(8, 0)), "The original circle does not cover the replacement boundary.");
            physics.BodySetShape(body, 0, rectangle); physics.BodySetShapeTransform(body, 0, At(25));
            physics.AreaSetShape(area, 0, rectangle); physics.AreaSetShapeTransform(area, 0, At(50));
            Check(Point(space, body, new(33, 0)) && Point(space, body, new(50, 0), 1) &&
                  !Point(space, body, Vector2.Zero) && Point(space, area, new(158, 0), areas: true),
                "Replacement and local poses affect real body/Area queries while preserving neighbour indices.");
            physics.BodySetShapeDisabled(body, 0, true); physics.AreaSetShapeDisabled(area, 0, true);
            Check(physics.BodyGetShapeCount(body) == 2 && physics.AreaGetShapeCount(area) == 1 &&
                  !Point(space, body, new(25, 0)) && !Point(space, area, new(150, 0), areas: true),
                "Disabled slots retain their geometry, pose and logical count.");
            physics.BodySetShapeDisabled(body, 0, false); physics.AreaSetShapeDisabled(area, 0, false);
            box.Size = new(40, 6); physics.ShapeSetData(rectangle, box);
            Check(Point(space, body, new(42, 0)) && Point(space, area, new(167, 0), areas: true),
                "Owned shape data replacement refreshes every explicit slot user.");
            Reject<ArgumentOutOfRangeException>(() => physics.BodySetShape(body, -1, circle));
            Reject<ArgumentException>(() => physics.AreaSetShapeTransform(area, 0, new(0, new(2, 1), 0, Vector2.Zero)));
            Reject<ArgumentException>(() => physics.BodySetShape(body, 0, area));
            Check(physics.BodyGetShape(body, 0) == rectangle && physics.AreaGetShapeTransform(area, 0) == At(50),
                "Invalid index/kind/pose leaves the previous slots unchanged.");
            physics.BodyRemoveShape(body, 0);
            Check(physics.BodyGetShape(body, 0) == circle && Point(space, body, new(50, 0)),
                "Removal renumbers the surviving slot and actual query tag.");
            physics.BodyClearShapes(body); physics.AreaClearShapes(area);
            physics.BodyClearShapes(body); physics.AreaClearShapes(area);
            Check(physics.BodyGetShapeCount(body) == 0 && physics.AreaGetShapeCount(area) == 0 &&
                  !Point(space, body, new(50, 0)), "Repeated clear removes fixtures without freeing shape resources.");
            using var copy = physics.ShapeGetData(circle);
            Check(copy is CircleShape { Radius: 5 }, "The cleared resource remains independently live.");
        }
        finally
        {
            physics.FreeRID(body); physics.FreeRID(area); physics.FreeRID(circle); physics.FreeRID(rectangle); physics.FreeRID(space);
        }
    }

    private static void VerifySceneSlots()
    {
        var physics = PhysicsServer.Instance;
        using var shape = new CircleShape { Radius = 5 };
        var root = new Node(); var body = new StaticBody(); var area = new Area { Position = new(100, 0) };
        var owner = body.CreateShapeOwner(null);
        body.ShapeOwnerAddShape(owner, shape); body.ShapeOwnerAddShape(owner, shape);
        var child = new CollisionShape { Shape = shape, Position = new(50, 0) }; body.AddChild(child);
        root.AddChild(body); root.AddChild(area);
        using var tree = new SceneTree(root);
        var bodyRID = body.GetRID(); var areaRID = area.GetRID(); var space = body.GetWorld2D()!.Space;
        var serverShape = physics.CircleShapeCreate();
        try
        {
            using (Resource custom = new RIDResource(serverShape)) Check(custom.GetRID() == serverShape, "Typed resource override dispatch projects a configured backend role.");
            Check(physics.BodyGetShapeCount(bodyRID) == 3 && physics.BodyGetShape(bodyRID, 0) == shape.GetRID(),
                "Scene child/manual shapes share the server global logical indices.");
            physics.BodySetShapeTransform(bodyRID, 0, At(30));
            Check(body.ShapeOwnerGetTransform(owner) == Transform.Identity && physics.BodyGetShapeTransform(bodyRID, 1) == Transform.Identity &&
                  Point(space, bodyRID, new(30, 0)) && Point(space, bodyRID, Vector2.Zero, 1),
                "A raw slot pose does not move siblings or rewrite stored owner fields.");
            body.ShapeOwnerSetTransform(owner, At(10));
            Check(physics.BodyGetShapeTransform(bodyRID, 0) == At(10) && physics.BodyGetShapeTransform(bodyRID, 1) == At(10),
                "A group transform edit reclaims all corresponding slot poses.");
            physics.BodySetShapeTransform(bodyRID, 2, At(70));
            Check(child.Position == new Vector2(50, 0), "Raw geometry does not rewrite the child node.");
            child.Position = new(60, 0);
            Check(physics.BodyGetShapeTransform(bodyRID, 2) == At(60), "A later child transform edit reclaims its pose.");
            physics.BodySetShape(bodyRID, 0, serverShape); physics.AreaAddShape(areaRID, serverShape);
            var borrowed = body.ShapeOwnerGetShape(owner, 0);
            using var query = new PhysicsShapeQueryParameters2D { Shape = borrowed };
            Check(query.ShapeRID == serverShape && physics.AreaGetShape(areaRID, 0) == serverShape,
                "Scene slots and resource query views use the same owned shape identity.");
            Reject<InvalidOperationException>(borrowed.Dispose);
            Check(!borrowed.IsDisposed, "Borrowed server geometry cannot be disposed outside its RID owner.");
            using var larger = new CircleShape { Radius = 20 };
            using var reading = new ManualResetEventSlim();
            var reader = Task.Run(() =>
            {
                Check(borrowed.GetRID() == serverShape, "An off-owner passive shape identity read remains stable.");
                reading.Set();
                for (var step = 0; step < 100_000; step++)
                {
                    try { Check(borrowed.GetRID() == serverShape, "Concurrent retirement never manufactures another RID."); }
                    catch (ObjectDisposedException) { break; }
                }
            });
            Check(reading.Wait(TimeSpan.FromSeconds(5)), "The concurrent identity reader starts within its bounded check.");
            physics.ShapeSetData(serverShape, larger); reader.GetAwaiter().GetResult();
            Check(borrowed.IsDisposed && body.ShapeOwnerGetShape(owner, 0) is CircleShape { Radius: 20 } &&
                  Point(space, bodyRID, new(28, 0)) && Point(space, areaRID, new(118, 0), areas: true),
                "Owned data replacement refreshes scene users and retires the prior borrowed view.");
            physics.BodySetShapeDisabled(bodyRID, 0, true);
            Check(!body.IsShapeOwnerDisabled(owner) && Point(space, bodyRID, new(10, 0), 1) && !Point(space, bodyRID, new(28, 0)),
                "Per-slot disable preserves group policy and sibling geometry.");
            body.ShapeOwnerSetDisabled(owner, false);
            Check(Point(space, bodyRID, new(28, 0)), "A group disabled-policy edit reclaims raw slot overrides.");
            for (var step = 0; step < 40; step++) tree.PhysicsFrame(1d / 120);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var step = 0; step < 64; step++)
            {
                _ = physics.BodyGetShape(bodyRID, 0); _ = physics.AreaGetShape(areaRID, 0);
                physics.BodySetShapeTransform(bodyRID, 0, At(10)); physics.AreaSetShape(areaRID, 0, serverShape);
                tree.PhysicsFrame(1d / 120);
            }
            Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
                "Warmed indexed reads, unchanged writes and active solver frames allocate no managed bytes.");
            physics.FreeRID(serverShape); serverShape = default;
            Check(physics.BodyGetShapeCount(bodyRID) == 2 && physics.AreaGetShapeCount(areaRID) == 0 &&
                  physics.BodyGetShape(bodyRID, 0) == shape.GetRID() && body.ShapeOwnerGetShapeCount(owner) == 1,
                "Shape free removes scene users and reindexes the surviving group/child slots.");
            physics.BodyClearShapes(bodyRID);
            Check(body.ShapeOwnerGetShapeCount(owner) == 0 && child.Parent == body && child.Shape == shape,
                "Scene clear keeps owner IDs and configured child nodes without hidden geometry.");
            child.Shape = null; child.Shape = shape;
            Check(physics.BodyGetShapeCount(bodyRID) == 1 && Point(space, bodyRID, new(60, 0)),
                "A later child resource edit repopulates its retained owner group.");
            shape.Dispose();
            Check(physics.BodyGetShapeCount(bodyRID) == 0 || !physics.BodyGetShape(bodyRID, 0).IsValid(),
                "A disposed borrowed shape cannot produce a newly live RID.");
        }
        finally { if (serverShape.IsValid()) physics.FreeRID(serverShape); }
    }

    private static void VerifyOneWay(bool sceneMode)
    {
        var physics = PhysicsServer.Instance;
        var segment = physics.SegmentShapeCreate(); var circle = physics.CircleShapeCreate(); var moving = physics.BodyCreate();
        StaticBody? sceneBody = sceneMode ? new() { Position = new(0, 100) } : null;
        var root = new Node(); if (sceneBody is not null) root.AddChild(sceneBody);
        using var tree = new SceneTree(root);
        var space = sceneBody?.GetWorld2D()!.Space ?? physics.SpaceCreate();
        var platform = sceneBody?.GetRID() ?? physics.BodyCreate();
        try
        {
            using var line = new SegmentShape { A = new(-100, 0), B = new(100, 0) }; physics.ShapeSetData(segment, line);
            if (sceneBody is null) { physics.BodySetMode(platform, PhysicsServer.BodyMode.Static); physics.BodySetTransform(platform, At(0, 100)); }
            physics.BodyAddShape(platform, segment);
            if (sceneBody is null) physics.BodySetSpace(platform, space);
            physics.BodyAddShape(moving, circle); physics.BodySetTransform(moving, At(0, 120));
            physics.BodySetGravityScale(moving, 0); physics.BodySetSpace(moving, space);
            physics.BodySetShapeAsOneWayCollision(platform, 0, true, 1);
            using var motion = new PhysicsTestMotionParameters2D { From = At(0, 120), Motion = new(0, -50) };
            Check(!physics.BodyTestMotion(moving, motion), "A raw one-way slot permits the upward body motion test.");
            physics.BodySetLinearVelocity(moving, new(0, -80));
            for (var step = 0; step < 60; step++) { if (sceneMode) tree.PhysicsFrame(1d / 60); else physics.SpaceStep(space, 1d / 60); }
            Check(physics.BodyGetTransform(moving).Origin.Y < 60, "Raw one-way contacts permit real upward motion on scene/server platforms.");
            physics.BodySetTransform(moving, At(0, 80)); physics.BodySetLinearVelocity(moving, new(0, 80));
            for (var step = 0; step < 60; step++) { if (sceneMode) tree.PhysicsFrame(1d / 60); else physics.SpaceStep(space, 1d / 60); }
            Check(physics.BodyGetTransform(moving).Origin.Y is > 86 and < 94, "Raw one-way contacts block the opposite real solver approach.");
            physics.BodySetShapeTransform(platform, 0, At(0, rotation: MathF.PI / 2));
            motion.From = At(20, 100); motion.Motion = new(-50, 0);
            Check(physics.BodyTestMotion(moving, motion), "Rotated slot-local one-way direction blocks the right-side approach.");
            motion.From = At(-20, 100); motion.Motion = new(50, 0);
            Check(!physics.BodyTestMotion(moving, motion), "Rotated slot-local direction permits the left-side approach.");
            if (sceneBody is not null)
            {
                sceneBody.ShapeOwnerSetOneWayCollision(sceneBody.ShapeFindOwner(0), false);
                Check(physics.BodyTestMotion(moving, motion), "A group one-way edit reclaims the full raw slot policy.");
            }
            Reject<ArgumentOutOfRangeException>(() => physics.BodySetShapeAsOneWayCollision(platform, 0, false, float.NaN));
            physics.BodySetShapeAsOneWayCollision(platform, 0, false, 0);
            Check(physics.BodyTestMotion(moving, motion), "Disabling one-way policy restores two-sided body motion contacts.");
        }
        finally
        {
            physics.FreeRID(moving);
            if (!sceneMode) { physics.FreeRID(platform); physics.FreeRID(space); }
            physics.FreeRID(segment); physics.FreeRID(circle);
        }
    }

    private static void VerifyGuardsAndLifetime()
    {
        var physics = PhysicsServer.Instance; var serverShape = physics.CircleShapeCreate();
        var root = new Node(); var body = new RigidBody { GravityScale = 0, LinearVelocity = new(60, 0), NotifyLocalTransformChanges = true };
        var area = new Area(); root.AddChild(body); root.AddChild(area);
        using var tree = new SceneTree(root);
        physics.BodyAddShape(body.GetRID(), serverShape); physics.AreaAddShape(area.GetRID(), serverShape);
        try
        {
            Reject<InvalidOperationException>(() => Task.Run(() => physics.BodyClearShapes(body.GetRID())).GetAwaiter().GetResult());
            using var larger = new CircleShape { Radius = 12 };
            Reject<InvalidOperationException>(() => Task.Run(() => physics.ShapeSetData(serverShape, larger)).GetAwaiter().GetResult());
            var observed = false;
            Action<CanvasItem> callback = _ =>
            {
                Reject<InvalidOperationException>(() => physics.BodyClearShapes(body.GetRID()));
                Reject<InvalidOperationException>(() => physics.AreaSetShapeTransform(area.GetRID(), 0, At(30)));
                Reject<InvalidOperationException>(() => physics.FreeRID(serverShape)); observed = true;
            };
            physics.BodySetShapeTransform(body.GetRID(), 0, At(10));
            body.LocalTransformChanged += callback; tree.PhysicsFrame(1d / 60); body.LocalTransformChanged -= callback;
            Check(physics.BodyGetDirectState(body.GetRID())!.CenterOfMassLocal.DistanceTo(new(10, 0)) < 0.001f,
                "Automatic mass geometry uses the effective per-slot pose.");
            Check(observed && physics.BodyGetShapeCount(body.GetRID()) == 1 && physics.AreaGetShapeCount(area.GetRID()) == 1,
                "Owner/sync-phase guards run before shape or slot mutation.");
            var geometry = body.ShapeOwnerGetShape(body.GetShapeOwners()[0], 0);
            geometry.Disposed += _ => throw new InvalidOperationException("Shape retirement callback probe.");
            Reject<InvalidOperationException>(() => physics.ShapeSetData(serverShape, larger));
            Check(geometry.IsDisposed && body.ShapeOwnerGetShape(body.GetShapeOwners()[0], 0) is CircleShape { Radius: 12 },
                "A retirement callback failure leaves the new data committed under the same RID.");
            geometry = body.ShapeOwnerGetShape(body.GetShapeOwners()[0], 0);
            geometry.Disposed += _ =>
            {
                Reject<ArgumentException>(() => physics.BodyAddShape(body.GetRID(), serverShape));
                throw new InvalidOperationException("Shape lifetime callback probe.");
            };
            Reject<InvalidOperationException>(() => physics.FreeRID(serverShape));
            Check(physics.BodyGetShapeCount(body.GetRID()) == 0 && physics.AreaGetShapeCount(area.GetRID()) == 0,
                "A throwing geometry disposal observer cannot leave slot users alive.");
            Reject<ArgumentException>(() => physics.ShapeGetData(serverShape)); serverShape = default;
        }
        finally { if (serverShape.IsValid()) physics.FreeRID(serverShape); }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
