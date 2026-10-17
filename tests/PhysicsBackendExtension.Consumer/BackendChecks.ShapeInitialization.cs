using Electron2D;

public static partial class BackendChecks
{
    public static void RunShapeInitialization(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ShapeInitialization", () => solver = new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("ShapeInitialization");
            Check(world.PhysicsBackendName == (backend?.ToString() ?? "ShapeInitialization") && world.PhysicsFallbackReason is null, "Initial shapes retain exact selected implementation");
            using var disk = new CircleShape { Radius = .5f };
            var root = new SubViewport { World = world }; var mover = new RigidBody { Position = new(0, -200), GravityScale = 0, CanSleep = false, CollisionLayer = 0, CollisionMask = 0, LinearVelocity = new(3, 0), LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace };
            mover.AddChild(new CollisionShape { Shape = disk }); root.AddChild(mover); using var tree = new SceneTree(root);
            (Func<RID> Create, PhysicsServer.ShapeType Kind, Shape Data, Vector2 Probe)[] cases =
            [
                (PhysicsServer.CircleShapeCreate, PhysicsServer.ShapeType.Circle, new CircleShape { Radius = 3 }, Vector2.Zero),
                (PhysicsServer.RectangleShapeCreate, PhysicsServer.ShapeType.Rectangle, new RectangleShape { Size = new(4, 6) }, Vector2.Zero),
                (PhysicsServer.CapsuleShapeCreate, PhysicsServer.ShapeType.Capsule, new CapsuleShape { Radius = 2, Height = 8 }, Vector2.Zero),
                (PhysicsServer.SegmentShapeCreate, PhysicsServer.ShapeType.Segment, new SegmentShape { A = new(-3, 0), B = new(3, 0) }, Vector2.Zero),
                (PhysicsServer.WorldBoundaryShapeCreate, PhysicsServer.ShapeType.WorldBoundary, new WorldBoundaryShape(), new(0, 1)),
                (PhysicsServer.SeparationRayShapeCreate, PhysicsServer.ShapeType.SeparationRay, new SeparationRayShape { Length = 6 }, new(0, 6)),
                (PhysicsServer.ConvexPolygonShapeCreate, PhysicsServer.ShapeType.ConvexPolygon, new ConvexPolygonShape { Points = [new(-3, -3), new(3, -3), new(0, 3)] }, Vector2.Zero),
                (PhysicsServer.ConcavePolygonShapeCreate, PhysicsServer.ShapeType.ConcavePolygon, new ConcavePolygonShape { Segments = [new(-3, 0), new(3, 0)] }, Vector2.Zero)
            ];
            try
            {
                using var query = new PhysicsShapeQueryParameters { Shape = disk, CollideWithAreas = true }; var hits = new PhysicsShapeResult[4];
                foreach (var item in cases)
                {
                    mover.Position = new(0, -200);
                    var pending = item.Create(); var spare = PhysicsServer.CircleShapeCreate(); var rawBody = PhysicsServer.BodyCreate(); var rawArea = PhysicsServer.AreaCreate();
                    var sceneBody = new StaticBody { Position = new(0, 0), CollisionLayer = 1, CollisionMask = 0 }; var sceneArea = new Area { Position = new(30, 0), CollisionLayer = 2, CollisionMask = 0 };
                    root.AddChild(sceneBody); root.AddChild(sceneArea); var ids = new[] { sceneBody.GetRID(), sceneArea.GetRID(), rawBody, rawArea };
                    try
                    {
                        Check(PhysicsServer.ShapeGetType(pending) == item.Kind, "Unconfigured RID retains its declared kind");
                        Reject<InvalidOperationException>(() => PhysicsServer.ShapeGetData(pending));
                        if (solver is not null) Reject<InvalidOperationException>(() => solver.ReadGeometry(pending));
                        using var wrong = item.Kind == PhysicsServer.ShapeType.Circle ? (Shape)new RectangleShape() : new CircleShape();
                        Reject<ArgumentException>(() => PhysicsServer.ShapeSetData(pending, wrong)); Reject<InvalidOperationException>(() => PhysicsServer.ShapeGetData(pending));
                        PhysicsServer.BodySetMode(rawBody, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetMass(rawBody, 2); PhysicsServer.BodySetGravityScale(rawBody, 0); PhysicsServer.BodySetCanSleep(rawBody, false);
                        PhysicsServer.BodySetLinearDampMode(rawBody, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(rawBody, RigidBody.DampMode.Replace);
                        PhysicsServer.BodySetLinearVelocity(rawBody, new(3, 0)); PhysicsServer.BodySetTransform(rawBody, new(0, new(60, 0))); PhysicsServer.BodySetCollisionLayer(rawBody, 4); PhysicsServer.BodySetCollisionMask(rawBody, 0);
                        PhysicsServer.AreaSetTransform(rawArea, new(0, new(90, 0))); PhysicsServer.AreaSetCollisionLayer(rawArea, 8); PhysicsServer.AreaSetCollisionMask(rawArea, 0);
                        foreach (var i in new[] { 0, 2 }) { PhysicsServer.BodyAddShape(ids[i], pending, disabled: true); PhysicsServer.BodyAddShape(ids[i], pending); }
                        foreach (var i in new[] { 1, 3 }) { PhysicsServer.AreaAddShape(ids[i], pending, disabled: true); PhysicsServer.AreaAddShape(ids[i], pending); }
                        PhysicsServer.BodySetSpace(rawBody, world.Space); PhysicsServer.AreaSetSpace(rawArea, world.Space);
                        Reject<InvalidOperationException>(() => PhysicsServer.BodySetShape(rawBody, 1, spare)); Reject<InvalidOperationException>(() => PhysicsServer.AreaSetShape(sceneArea.GetRID(), 1, spare));
                        using var map = new PhysicsSnapshotMap(world.Space); for (var i = 0; i < ids.Length; i++) map.Bind((ulong)(i + 1), 1, ids[i]); map.Bind(5, 1, mover.GetRID());
                        var before = new PhysicsSnapshot(65536); map.Capture(before); var ownerCount = sceneBody.GetShapeOwners().Length; using (var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space)) { tree.PhysicsFrame(.1); checkpoint.Restore(); }
                        map.Apply(before); CheckQueries(0);
                        using var guardView = new CircleQueries(world.Space, []);
                        using var guardQuery = new PhysicsPointQueryParameters { CollideWithAreas = true, CollisionMask = uint.MaxValue };
                        var sentinel = new PhysicsPointResult(mover.GetRID(), 0); var guardedHits = new[] { sentinel };
                        CheckPublishedSlots(false);
                        using (var invalid = new PhysicsShapeQueryParameters { ShapeRID = pending }) Reject<InvalidOperationException>(() => world.DirectSpaceState.IntersectShape(invalid, hits));
                        var view = sceneBody.ShapeOwnerGetShape(sceneBody.ShapeFindOwner(1), 0);
                        using (var invalid = new PhysicsShapeQueryParameters()) Reject<InvalidOperationException>(() => invalid.Shape = view);
                        if (solver is not null)
                        {
                            var failure = new ApplicationException("first configuration"); var injected = false;
                            solver.ShapeHook = rid => { if (rid == rawArea && !injected) { injected = true; throw failure; } };
                            try { var errors = ReleaseErrors(() => PhysicsServer.ShapeSetData(pending, item.Data)); Check(injected && errors.Contains(failure), "First configuration failure preserves its exact error"); }
                            finally { solver.ShapeHook = null; }
                            Reject<InvalidOperationException>(() => PhysicsServer.ShapeGetData(pending)); CheckQueries(0);
                        }
                        for (var i = 0; i < 128; i++) Frame(0);
                        Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                        for (var i = 0; i < 128; i++) Frame(0);
                        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                        Check(owner == 0 && all == 0, $"Unconfigured {item.Kind} full frames {owner}/{all} B");
                        PhysicsServer.ShapeSetData(pending, item.Data); using var copy = PhysicsServer.ShapeGetData(pending);
                        Check(copy.GetGeometryData().Type == item.Kind && PhysicsServer.ShapeGetType(pending) == item.Kind, "Configuration commits exact kind without changing RID");
                        if (solver is not null) Check(SameData(solver.ReadGeometry(pending), item.Data.GetGeometryData()), "Selected implementation reads exact configured canonical data");
                        tree.PhysicsFrame(.1); CheckQueries(1); CheckPublishedSlots(true);
                        Reject<System.IO.InvalidDataException>(() => map.Apply(before));
                        var configured = new PhysicsSnapshot(65536); map.Capture(configured); map.Apply(configured);
                        for (var i = 0; i < 128; i++) Frame(1);
                        var x = mover.Position.X; var rawX = PhysicsServer.BodyGetTransform(rawBody).Origin.X; Collect(); owner = GC.GetAllocatedBytesForCurrentThread(); all = GC.GetTotalAllocatedBytes(true);
                        for (var i = 0; i < 128; i++) Frame(1);
                        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                        Check(owner == 0 && all == 0 && Math.Abs(mover.Position.X - x - 38.4f) < .02f && Math.Abs(PhysicsServer.BodyGetTransform(rawBody).Origin.X - rawX - 38.4f) < .02f, $"Configured {item.Kind} motion/full frames {owner}/{all} B, from={x}, delta={mover.Position.X - x}, velocity={mover.LinearVelocity}");
                        PhysicsServer.FreeRID(pending); Check(PhysicsServer.BodyGetShapeCount(rawBody) == 0 && PhysicsServer.AreaGetShapeCount(rawArea) == 0 && sceneBody.GetShapeOwners().Length == ownerCount, "Configuration/free preserves logical membership and owner identity");
                        Console.WriteLine($"Initial {item.Kind}: preconfiguration, scene/raw Body/Area, queries, capture/replay, configuration and 2x128 full frames 0/0 B passed.");
                        void CheckPublishedSlots(bool configured)
                        {
                            foreach (var id in ids)
                            {
                                guardView.CachedPoint = new(id, 0); guardedHits[0] = sentinel;
                                Reject<InvalidOperationException>(() => guardView.IntersectPoint(guardQuery, guardedHits));
                                Check(guardedHits[0].ColliderRID == sentinel.ColliderRID && guardedHits[0].ShapeIndex == sentinel.ShapeIndex, "Rejected disabled result preserves caller output");
                                guardView.CachedPoint = new(id, 1);
                                if (configured) Check(guardView.IntersectPoint(guardQuery, guardedHits) == 1 && guardedHits[0].ColliderRID == id, "Configured slot permits valid selected query publication");
                                else
                                {
                                    Reject<InvalidOperationException>(() => guardView.IntersectPoint(guardQuery, guardedHits));
                                    Check(guardedHits[0].ColliderRID == sentinel.ColliderRID && guardedHits[0].ShapeIndex == sentinel.ShapeIndex, "Rejected pending result preserves caller output");
                                }
                            }
                        }
                        void CheckQueries(int expected)
                        {
                            for (var i = 0; i < 4; i++)
                            {
                                query.CollisionMask = 1u << i; query.Transform = new(0, item.Probe + (i == 2 ? PhysicsServer.BodyGetTransform(rawBody).Origin : new Vector2(i * 30, 0)));
                                var count = world.DirectSpaceState.IntersectShape(query, hits);
                                Check(count == expected && (count == 0 || hits[0].ColliderRID == ids[i] && hits[0].ShapeIndex == 1), "Configuration controls actual numerical participation with stable logical indices");
                            }
                        }
                        void Frame(int expected) { tree.PhysicsFrame(.1); CheckQueries(expected); }
                    }
                    finally { sceneBody.Dispose(); sceneArea.Dispose(); PhysicsServer.FreeRID(rawBody); PhysicsServer.FreeRID(rawArea); try { PhysicsServer.FreeRID(pending); } catch (ArgumentException) { } PhysicsServer.FreeRID(spare); }
                }
            }
            finally { foreach (var item in cases) item.Data.Dispose(); }
            CheckBorrowedShapeRetirement(world);
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
    public static void RunBorrowedShapeRetirement(PhysicsServer.Backend? backend = null)
    {
        if (backend is null) PhysicsServerManager.RegisterServer("BorrowedShapeRetirement", static () => new Solver());
        using var world = backend is { } selected ? new World(selected) : new World("BorrowedShapeRetirement");
        CheckBorrowedShapeRetirement(world);
    }
    private static void CheckBorrowedShapeRetirement(World world)
    {
        using var shape = new CircleShape { Radius = 2 }; var source = shape.GetRID();
        var body = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        using var point = new PhysicsPointQueryParameters { CollideWithAreas = true, CollisionMask = 0x80000000 };
        var hits = new PhysicsPointResult[4];
        try
        {
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodySetCollisionLayer(body, 0x80000000); PhysicsServer.AreaSetCollisionLayer(area, 0x80000000);
            PhysicsServer.BodyAddShape(body, source); PhysicsServer.AreaAddShape(area, source);
            PhysicsServer.BodySetSpace(body, world.Space); PhysicsServer.AreaSetSpace(area, world.Space);
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 2, "Initialized borrowed geometry is active for both raw roles");
            shape.Dispose();
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0 && PhysicsServer.BodyGetShapeCount(body) == 1 && PhysicsServer.AreaGetShapeCount(area) == 1 &&
                !PhysicsServer.BodyGetShape(body, 0).IsValid() && !PhysicsServer.AreaGetShape(area, 0).IsValid(), "Disposed borrowed geometry leaves inactive logical slots without invoking its supplier");
            PhysicsServer.BodySetShapeDisabled(body, 0, true); PhysicsServer.BodySetShapeDisabled(body, 0, false);
            PhysicsServer.AreaSetShapeDisabled(area, 0, true); PhysicsServer.AreaSetShapeDisabled(area, 0, false);
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0, "Enabling a disposed slot cannot revive geometry");
            Console.WriteLine($"Borrowed shape retirement {world.PhysicsBackendName}: active queries, retained logical slots, empty RID getters and disable/re-enable passed.");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(area); }
    }

}
