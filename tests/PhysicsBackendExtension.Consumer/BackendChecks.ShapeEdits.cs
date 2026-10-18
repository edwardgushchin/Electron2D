using System.Runtime.CompilerServices;
using Electron2D;

public static partial class BackendChecks
{
    public static void RunShapeEdits(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ShapeEdits", () => solver = new());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("ShapeEdits");
            using var root = new SubViewport { World = world };
            using var circle = new CircleShape { Radius = 2 }; using var rectangle = new RectangleShape { Size = new(4, 4) };
            using var replacement = new CircleShape { Radius = 5 }; using var identity = new Node();
            var moving = new RigidBody
            {
                Position = new(0, 500),
                GravityScale = 0,
                CanSleep = false,
                CollisionLayer = 0,
                LinearVelocity = new(6, 0),
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            moving.AddChild(new CollisionShape { Shape = circle }); root.AddChild(moving);
            var anchor = new StaticBody { Position = new(12, 100) }; anchor.AddChild(new CollisionShape { Shape = circle }); root.AddChild(anchor);
            var receiver = new Area { Position = new(12, 0), CollisionLayer = 0, CollisionMask = 0x80000000 };
            receiver.AddChild(new CollisionShape { Shape = circle }); root.AddChild(receiver);
            using var tree = new SceneTree(root);
            var body = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate(); var circleRID = circle.GetRID(); var rectangleRID = rectangle.GetRID();
            const ulong canvas = 0xFEDCBA9876543210;
            var bodyEvents = 0; var areaEvents = 0; var faults = 0;
            PhysicsServer.AreaSetMonitorCallback(receiver.GetRID(), (_, rid, _, _, _) => { if (rid == body) bodyEvents++; });
            PhysicsServer.AreaSetMonitorCallback(area, (_, rid, _, _, _) => { if (rid == anchor.GetRID()) areaEvents++; });
            using var point = new PhysicsPointQueryParameters { CollisionMask = 0x80000000, CanvasInstanceID = canvas, CollideWithAreas = true };
            var hits = new PhysicsPointResult[4];
            try
            {
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionMask(body, 0);
                PhysicsServer.AreaSetTransform(area, new(0, new Vector2(0, 100))); PhysicsServer.AreaSetCollisionMask(area, 1);
                PhysicsServer.BodySetCollisionLayer(body, 0x80000000); PhysicsServer.AreaSetCollisionLayer(area, 0x80000000);
                PhysicsServer.BodyAttachObject(body, identity); PhysicsServer.AreaAttachObject(area, identity);
                PhysicsServer.BodyAttachCanvasInstanceID(body, canvas); PhysicsServer.AreaAttachCanvasInstanceID(area, canvas);
                Reset(false); Reset(true); PhysicsServer.BodySetSpace(body, world.Space); PhysicsServer.AreaSetSpace(area, world.Space);
                Check(world.PhysicsBackendName == (backend?.ToString() ?? "ShapeEdits"), "The selected real implementation owns shape editing");
                foreach (var sensor in new[] { false, true })
                {
                    var operations = sensor ? 7 : 8;
                    for (var operation = 0; operation < operations; operation++)
                    {
                        Reset(sensor); tree.PhysicsFrame(1d / 60); Original(sensor);
                        if (solver is not null)
                        {
                            for (var failures = 1; failures <= 3; failures++)
                            {
                                var rid = sensor ? area : body; var attempts = 0;
                                var apply = new ApplicationException("shape-apply"); var restore = new InvalidOperationException("shape-restore");
                                var retry = new ApplicationException("shape-retry"); var oldEvents = sensor ? areaEvents : bodyEvents;
                                solver.ShapeHook = id =>
                                {
                                    if (id != rid || ++attempts > failures) return;
                                    solver.Bodies[id].Shapes = 0;
                                    if (attempts == 1) throw apply;
                                    if (attempts == 2) throw restore;
                                    throw retry;
                                };
                                var errors = ReleaseErrors(() => Edit(sensor, operation));
                                Check(errors.Length == (failures == 1 ? 1 : 2) && ReferenceEquals(errors[0], apply) &&
                                    (failures == 1 || ReferenceEquals(errors[1], restore)), "Failed shape edits retain the original and rollback errors in order");
                                Authored(sensor);
                                if (failures == 3)
                                {
                                    var retried = ReleaseErrors(() => Hit(sensor, 12, 2));
                                    Check(retried.Length == 1 && ReferenceEquals(retried[0], retry), "A failed pending rebuild propagates before publishing a query");
                                    Authored(sensor);
                                }
                                Original(sensor); solver.ShapeHook = null;
                                if (!sensor) Check(solver.Bodies[body].LastShape.OneWay is null, "Rollback restores the previous one-way policy");
                                tree.PhysicsFrame(1d / 60);
                                Check((sensor ? areaEvents : bodyEvents) == oldEvents, "Rollback preserves logical shape history without duplicate sensor effects");
                                faults++;
                            }
                        }
                        Edit(sensor, operation); Committed(sensor, operation);
                    }
                    Reset(sensor); var idempotent = 0;
                    if (solver is not null) solver.ShapeHook = _ => idempotent++;
                    SetShape(sensor, 2, circleRID); SetTransform(sensor, 2, new(0, new Vector2(12, 0))); SetDisabled(sensor, 2, false);
                    if (solver is not null) solver.ShapeHook = null;
                    Check(idempotent == 0, "Equal slot authoring does not invoke the numerical hook");
                    Reject<ArgumentOutOfRangeException>(() => Remove(sensor, -1)); Original(sensor);
                    Task.Run(() => Reject<InvalidOperationException>(() => Clear(sensor))).GetAwaiter().GetResult(); Original(sensor);
                }
                PhysicsServer.BodySetSpace(body, default);
                var weak = RetiredShapeEditResource(body, world.Space, false); Collect(); Check(!weak.TryGetTarget(out _), "Completed body clear does not retain a retired geometry resource");
                PhysicsServer.AreaSetSpace(area, default);
                weak = RetiredShapeEditResource(area, world.Space, true); Collect(); Check(!weak.TryGetTarget(out _), "Completed Area clear releases sensor geometry and backup references");
                Reset(false); PhysicsServer.BodySetSpace(body, world.Space); Reset(true);
                PhysicsServer.AreaSetSpace(area, world.Space);
                var movingRID = moving.GetRID(); var callbacks = 0; PhysicsServer.BodySetForceIntegrationCallback(movingRID, _ => callbacks++);
                for (var i = 0; i < 128; i++) Frame(i);
                var before = callbacks; var events = bodyEvents + areaEvents; var start = moving.Position.X; Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame(i);
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(owner == 0 && all == 0 && callbacks == before + 128 && bodyEvents + areaEvents == events && moving.GetRID() == movingRID && Math.Abs(moving.Position.X - start - 12.8f) < .01f,
                    $"128 edited shape/query/full frames: {owner}/{all} B, callbacks={callbacks - before}, motion={moving.Position.X - start}");
                GeometryEdits();
                SceneShapeEditChecks(world, root, tree, solver, circle, replacement);
                Console.WriteLine($"Shape edits {backend?.ToString() ?? "registered"}, threaded={threaded}: 15 ordinary edits, {faults} failure cases, old geometry/history recovery; 128 full frames {owner}/{all} B.");
                void Frame(int i)
                {
                    for (var kind = 0; kind < 2; kind++)
                    {
                        var sensor = kind != 0;
                        SetTransform(sensor, 2, new(0, new Vector2(12 + (i & 1), 0))); SetDisabled(sensor, 1, (i & 1) == 0);
                        Hit(sensor, 12, 2);
                    }
                    tree.PhysicsFrame(1d / 60);
                }
                void Reset(bool sensor)
                {
                    Clear(sensor); Add(sensor, circleRID, new(0, new Vector2(-12, 0)));
                    Add(sensor, rectangleRID, Transform.Identity, true); Add(sensor, circleRID, new(0, new Vector2(12, 0)));
                }
                void Authored(bool sensor)
                {
                    var rid = sensor ? area : body;
                    Check(Count(sensor) == 3 && GetShape(sensor, 0) == circleRID && GetShape(sensor, 1) == rectangleRID && GetShape(sensor, 2) == circleRID &&
                        GetTransform(sensor, 0).Origin == new Vector2(-12, 0) && GetTransform(sensor, 1) == Transform.Identity && GetTransform(sensor, 2).Origin == new Vector2(12, 0),
                        "Failed edits restore every logical slot, resource and transform");
                    Check((sensor ? PhysicsServer.AreaGetObjectInstanceID(rid) : PhysicsServer.BodyGetObjectInstanceID(rid)) == identity.InstanceID &&
                        (sensor ? PhysicsServer.AreaGetCanvasInstanceID(rid) : PhysicsServer.BodyGetCanvasInstanceID(rid)) == canvas, "Shape failures preserve object and full-width canvas associations");
                }
                void Original(bool sensor) { Authored(sensor); Hit(sensor, -12, 0); Hit(sensor, 0, -1); Hit(sensor, 12, 2); Hit(sensor, 16, -1); }
                void Hit(bool sensor, float x, int shapeIndex)
                {
                    point.Position = new(x, sensor ? 100 : 0); var count = world.DirectSpaceState.IntersectPoint(point, hits);
                    Check(count == (shapeIndex < 0 ? 0 : 1) && (count == 0 || hits[0].ColliderRID == (sensor ? area : body) && hits[0].ShapeIndex == shapeIndex),
                        "Public point query observes the exact restored/committed geometry and logical shape index");
                }
                void Edit(bool sensor, int operation)
                {
                    switch (operation)
                    {
                        case 0: Clear(sensor); break;
                        case 1: Add(sensor, circleRID, new(0, new Vector2(24, 0))); break;
                        case 2: SetShape(sensor, 2, replacement.GetRID()); break;
                        case 3: SetTransform(sensor, 2, new(0, new Vector2(24, 0))); break;
                        case 4: SetDisabled(sensor, 2, true); break;
                        case 5: SetDisabled(sensor, 1, false); break;
                        case 6: Remove(sensor, 0); break;
                        case 7: PhysicsServer.BodySetShapeAsOneWayCollision(body, 2, true, 3, Vector2.Left); break;
                    }
                }
                void Committed(bool sensor, int operation)
                {
                    Check(Count(sensor) == (operation == 0 ? 0 : operation == 1 ? 4 : operation == 6 ? 2 : 3), "A successful edit commits the new logical count");
                    if (operation == 0) { Hit(sensor, -12, -1); Hit(sensor, 12, -1); }
                    else if (operation == 1) Hit(sensor, 24, 3);
                    else if (operation == 2) Hit(sensor, 16, 2);
                    else if (operation == 3) { Hit(sensor, 12, -1); Hit(sensor, 24, 2); }
                    else if (operation == 4) Hit(sensor, 12, -1);
                    else if (operation == 5) Hit(sensor, 0, 1);
                    else if (operation == 6) { Hit(sensor, -12, -1); Hit(sensor, 12, 1); }
                    else if (solver is not null) Check(solver.Bodies[body].LastShape.OneWay is { Margin: 3 } way && way.LocalDirection == Vector2.Left,
                        "Successful one-way edit reaches the selected shape configuration");
                }
                void Clear(bool sensor) { if (sensor) PhysicsServer.AreaClearShapes(area); else PhysicsServer.BodyClearShapes(body); }
                void Add(bool sensor, RID shape, Transform transform, bool disabled = false) { if (sensor) PhysicsServer.AreaAddShape(area, shape, transform, disabled); else PhysicsServer.BodyAddShape(body, shape, transform, disabled); }
                void SetShape(bool sensor, int index, RID shape) { if (sensor) PhysicsServer.AreaSetShape(area, index, shape); else PhysicsServer.BodySetShape(body, index, shape); }
                void SetTransform(bool sensor, int index, Transform transform) { if (sensor) PhysicsServer.AreaSetShapeTransform(area, index, transform); else PhysicsServer.BodySetShapeTransform(body, index, transform); }
                void SetDisabled(bool sensor, int index, bool disabled) { if (sensor) PhysicsServer.AreaSetShapeDisabled(area, index, disabled); else PhysicsServer.BodySetShapeDisabled(body, index, disabled); }
                void Remove(bool sensor, int index) { if (sensor) PhysicsServer.AreaRemoveShape(area, index); else PhysicsServer.BodyRemoveShape(body, index); }
                int Count(bool sensor) => sensor ? PhysicsServer.AreaGetShapeCount(area) : PhysicsServer.BodyGetShapeCount(body);
                RID GetShape(bool sensor, int index) => sensor ? PhysicsServer.AreaGetShape(area, index) : PhysicsServer.BodyGetShape(body, index);
                Transform GetTransform(bool sensor, int index) => sensor ? PhysicsServer.AreaGetShapeTransform(area, index) : PhysicsServer.BodyGetShapeTransform(body, index);
                void GeometryEdits()
                {
                    var points = Enumerable.Range(0, 12).Select(i => Vector2.FromAngle(i * Mathf.Tau / 12) * 20).ToArray();
                    Shape[] geometry = [new CircleShape { Radius = 2 }, new RectangleShape { Size = new(4, 4) },
                        new CapsuleShape { Radius = 1, Height = 4 }, new SegmentShape { A = new(-2, 0), B = new(2, 0) },
                        new ConvexPolygonShape { Points = points }, new ConcavePolygonShape { Segments = [new(-2, 0), new(2, 0), new(0, -2), new(0, 2)] },
                        new WorldBoundaryShape { Normal = Vector2.Up }, new SeparationRayShape { Length = 4 }];
                    using var query = new PhysicsShapeQueryParameters { Shape = circle, CollisionMask = 0x80000000 };
                    var results = new PhysicsShapeResult[4];
                    var sampleBefore = GC.GetAllocatedBytesForCurrentThread(); var sample = ShapeEditPolicySample();
                    var policyBytes = GC.GetAllocatedBytesForCurrentThread() - sampleBefore; GC.KeepAlive(sample);
                    long stepOwner = 0, stepAll = 0;
                    var directed = false;
                    try
                    {
                        foreach (var shape in geometry)
                        {
                            directed = shape is SeparationRayShape;
                            Reset(false); Reset(true); SetShape(false, 2, shape.GetRID()); SetShape(true, 2, shape.GetRID());
                            PhysicsServer.BodySetShapeAsOneWayCollision(body, 2, true, 3, Vector2.Left);
                            var held = solver?.Bodies[body].LastShape.OneWay; var heldDirection = held?.LocalDirection ?? Vector2.Zero;
                            for (var i = 0; i < 128; i++) GeometryFrame(i);
                            var observed = bodyEvents + areaEvents; var count = callbacks; var x = moving.Position.X;
                            stepOwner = stepAll = 0;
                            Collect(); var local = GC.GetAllocatedBytesForCurrentThread(); var total = GC.GetTotalAllocatedBytes(true);
                            for (var i = 0; i < 128; i++) GeometryFrame(i, true);
                            local = GC.GetAllocatedBytesForCurrentThread() - local; total = GC.GetTotalAllocatedBytes(true) - total;
                            Check(policyBytes > 0 && local == 128 * policyBytes && total == local && stepOwner == 0 && stepAll == 0 &&
                                bodyEvents + areaEvents == observed && callbacks == count + 128 && Math.Abs(moving.Position.X - x - 12.8f) < .01f &&
                                (held is null || held.LocalDirection == heldDirection),
                                $"{shape.ClassName} rotating edits: {local}/{total} B, one immutable policy/frame={policyBytes} B; physics={stepOwner}/{stepAll} B, events={bodyEvents + areaEvents - observed}");
                            Console.WriteLine($"Shape edit geometry {shape.ClassName}: 128 physical frames {stepOwner}/{stepAll} B; immutable one-way authoring {local}/{total} B.");
                        }
                    }
                    finally { Clear(false); Clear(true); foreach (var shape in geometry) shape.Dispose(); }
                    void GeometryFrame(int i, bool measured = false)
                    {
                        for (var kind = 0; kind < 2; kind++)
                        {
                            var sensor = kind != 0; var origin = new Vector2(12 + (i & 1), 0);
                            SetTransform(sensor, 2, new(i * .01f, origin)); SetDisabled(sensor, 1, (i & 1) == 0);
                            query.CollideWithAreas = sensor; query.CollideWithBodies = !sensor;
                            // A directed ray starting inside a filled query shape has no surface hit.
                            var offset = directed ? Vector2.Down.Rotated(i * .01f) * 3 : Vector2.Zero;
                            query.Transform = new(0, origin + offset + new Vector2(0, sensor ? 100 : 0));
                            var found = world.DirectSpaceState.IntersectShape(query, results);
                            Check(found == 1 && results[0].ColliderRID == (sensor ? area : body) && results[0].ShapeIndex == 2,
                                "Rotated/compound sensor and body geometry preserve logical query indices");
                        }
                        if (!measured) { tree.PhysicsFrame(1d / 60); return; }
                        var own = GC.GetAllocatedBytesForCurrentThread(); var allThreads = GC.GetTotalAllocatedBytes(true);
                        tree.PhysicsFrame(1d / 60);
                        stepOwner += GC.GetAllocatedBytesForCurrentThread() - own; stepAll += GC.GetTotalAllocatedBytes(true) - allThreads;
                    }
                }
            }
            finally { if (solver is not null) solver.ShapeHook = null; PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(area); }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static OneWayContactData ShapeEditPolicySample() => new(Vector2.Left, 3);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<CircleShape> RetiredShapeEditResource(RID body, RID space, bool sensor)
    {
        if (sensor) PhysicsServer.AreaClearShapes(body); else PhysicsServer.BodyClearShapes(body);
        var shape = new CircleShape { Radius = 2 }; var weak = new WeakReference<CircleShape>(shape);
        if (sensor)
        {
            PhysicsServer.AreaAddShape(body, shape.GetRID()); PhysicsServer.AreaSetSpace(body, space); PhysicsServer.AreaClearShapes(body);
            PhysicsServer.AreaSetSpace(body, default);
        }
        else
        {
            PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetSpace(body, space); PhysicsServer.BodyClearShapes(body);
            PhysicsServer.BodySetSpace(body, default);
        }
        shape.Dispose(); return weak;
    }
}
