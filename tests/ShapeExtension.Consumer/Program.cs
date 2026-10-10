using Electron2D;

var backend = args.FirstOrDefault() == "gpu" ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
if (args.Contains("draw")) { DrawChecks.Run(backend); return; }
GeometryFactories(backend);
using var world = new World(backend); using var root = new SubViewport { World = world };
var vertices = new Vector2[12];
for (var i = 0; i < vertices.Length; i++) vertices[i] = new Vector2(MathF.Cos(i * Mathf.Tau / 12), MathF.Sin(i * Mathf.Tau / 12)) * 5;
var geometry = ShapeGeometry.ConvexPolygon(vertices); Array.Fill(vertices, new Vector2(1000, 1000));
using var shape = new ProceduralShape(geometry) { CustomSolverBias = .3f };
using var floorShape = new ProceduralShape(ShapeGeometry.Rectangle(new(220, 4)));
using var areaShape = new ProceduralShape(ShapeGeometry.Circle(50));
var scene = new RigidBody { CanSleep = false, Mass = 2, MaxContactsReported = 4 };
scene.AddChild(new CollisionShape { Shape = shape }); root.AddChild(scene);
var floor = new StaticBody { Position = new(40, 30) }; floor.AddChild(new CollisionShape { Shape = floorShape }); root.AddChild(floor);
var area = new Area { Position = new(0, 25) }; area.AddChild(new CollisionShape { Shape = areaShape }); root.AddChild(area);
var entries = 0; area.BodyShapeEntered += (_, _, _, _) => entries++;
using var tree = new SceneTree(root);
var raw = PhysicsServer.BodyCreate(); var rawArea = PhysicsServer.AreaCreate();
try
{
    PhysicsServer.BodyAddShape(raw, shape.GetRID()); PhysicsServer.BodySetMass(raw, 2); PhysicsServer.BodySetCanSleep(raw, false);
    PhysicsServer.BodySetMaxContactsReported(raw, 4); PhysicsServer.BodySetTransform(raw, new(0, new(80, 0))); PhysicsServer.BodySetSpace(raw, world.Space);
    PhysicsServer.AreaAddShape(rawArea, areaShape.GetRID()); PhysicsServer.AreaSetTransform(rawArea, new(0, new(80, 25)));
    var rawEntries = 0; PhysicsServer.AreaSetMonitorCallback(rawArea, (status, _, _, _, _) => { if (status == PhysicsServer.AreaBodyStatus.Added) rawEntries++; });
    PhysicsServer.AreaSetSpace(rawArea, world.Space);
    for (var i = 0; i < 180; i++) tree.PhysicsFrame(1d / 60);
    Check(entries > 0 && rawEntries > 0, "Custom scene/raw Area geometry publishes actual overlap events");
    foreach (var rid in new[] { scene.GetRID(), raw })
    {
        var view = PhysicsServer.BodyGetDirectState(rid)!;
        Check(MathF.Abs(view.Transform.Origin.Y - 23) < 1 && view.InverseMass is > .49f and < .51f && view.InverseInertia > 0,
            "Complete custom convex contour solves floor contact and automatic mass/inertia");
        Check(view.GetContactCount() > 0 && view.GetContactCollider(0) == floor.GetRID() && view.GetContactImpulse(0).IsFinite(),
            "Actual solved custom contact retains collider identity and impulse data");
    }
    Check(shape.Builds == 1 && ReferenceEquals(shape.GetGeometryData(), geometry) && PhysicsServer.ShapeGetType(shape.GetRID()) == PhysicsServer.ShapeType.ConvexPolygon && shape.GetRect().Size.X == 10,
        "All consumers share one immutable copied contour and its true primitive type");
    using var copy = (ProceduralShape)shape.Duplicate(); Check(copy.CustomSolverBias == .3f && copy.GetRect() == shape.GetRect(), "Resource duplication preserves geometry and solver policy");
    var point = new PhysicsPointQueryParameters { Position = PhysicsServer.BodyGetTransform(raw).Origin };
    using var pointOwner = point; var results = new PhysicsPointResult[8];
    var direct = world.DirectSpaceState;
    Check(direct.IntersectPoint(point, results) > 0, "Custom geometry participates in real direct queries");
    using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space); checkpoint.Capture();
    var initial = PhysicsServer.BodyGetTransform(raw); PhysicsServer.BodyApplyCentralImpulse(raw, new(30, -10)); tree.PhysicsFrame(1d / 60);
    checkpoint.Restore(); Check(PhysicsServer.BodyGetTransform(raw).Origin.DistanceTo(initial.Origin) < .001f, "Private checkpoint restores custom-shape body state");
    using var map = new PhysicsSnapshotMap(world.Space);
    map.Bind(1, 1, scene.GetRID()); map.Bind(2, 1, floor.GetRID()); map.Bind(3, 1, area.GetRID()); map.Bind(4, 1, raw); map.Bind(5, 1, rawArea);
    var snapshot = new PhysicsSnapshot(); map.Capture(snapshot); PhysicsServer.BodySetLinearVelocity(raw, new(60, 0)); map.Apply(snapshot);
    Check(PhysicsServer.BodyGetLinearVelocity(raw).Length() < 1, "Portable snapshot applies custom geometry without a consumer type on the wire");
    var tick = PhysicsServer.SpaceGetTick(world.Space);
    shape.Invalidate(); shape.OnBuild = () => throw new InvalidOperationException("Expected attached geometry construction failure");
    Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
    Check(PhysicsServer.SpaceGetTick(world.Space) == tick, "Rejected geometry preparation commits no solved tick");
    shape.OnBuild = null; tree.PhysicsFrame(1d / 60);
    Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1 && PhysicsServer.BodyGetDirectState(raw)!.GetContactCount() > 0,
        "Both selected attachments recover after a geometry hook failure before fixture replacement");
    for (var i = 0; i < 64; i++) Cycle();
    var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
    for (var i = 0; i < 64; i++) Cycle();
    owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
    Check(owner == 0 && all == 0, $"Warm custom geometry step/query/replay allocates {owner}/{all} owner/all-thread bytes");
    var identity = shape.GetRID(); var beforeBuilds = shape.Builds;
    shape.Update(ShapeGeometry.Rectangle(new(30, 10))); tree.PhysicsFrame(1d / 60);
    Check(shape.GetRID() == identity && PhysicsServer.ShapeGetType(identity) == PhysicsServer.ShapeType.Rectangle && shape.Builds == beforeBuilds + 1,
        "Changing represented geometry rebuilds both borrowers once and preserves resource RID");
    Check(PhysicsServer.BodyGetInertia(raw) > 100, "Changed geometry recomputes automatic inertia");
    using var probe = new ProceduralShape(ShapeGeometry.Circle(3));
    probe.OnBuild = () => Reject<InvalidOperationException>(() => probe.GetRect()); _ = probe.GetRect();
    probe.Update(ShapeGeometry.Circle(4)); probe.OnBuild = () => Reject<InvalidOperationException>(probe.Dispose); _ = probe.GetRect();
    probe.Update(ShapeGeometry.Circle(5)); probe.OnBuild = () => throw new InvalidOperationException("Expected geometry failure");
    Reject<InvalidOperationException>(() => probe.GetRect()); probe.OnBuild = null; Check(probe.GetRect().Size.X == 10, "Hook failure leaves a retryable empty cache");
    probe.Invalidate(); probe.ReturnNull = true; Reject<InvalidOperationException>(() => probe.GetRect()); probe.ReturnNull = false;
    probe.OnBuild = () => Reject<InvalidOperationException>(probe.Invalidate); _ = probe.GetRect();
    shape.Dispose(); tree.PhysicsFrame(1d / 60);
    Reject<ObjectDisposedException>(() => shape.GetRID());
    Check(PhysicsServer.BodyGetDirectState(raw)!.GetContactCount() == 0, "Resource retirement clears borrowers' collision geometry");
    Console.WriteLine($"{backend}: public-only exact procedural shapes, immutable data, scene/raw contacts/mass/Areas/queries, mutation/duplicate/lifetime, checkpoint/snapshot and 64 warm/64 measured step/query/replay cycles: {owner}/{all} B passed.");
    void Cycle() { tree.PhysicsFrame(1d / 60); _ = direct.IntersectPoint(point, results); map.Capture(snapshot); checkpoint.Capture(); checkpoint.Restore(); }
}
finally { PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(rawArea); }

static void GeometryFactories(PhysicsServer.Backend backend)
{
    ShapeGeometry[] all = [ShapeGeometry.WorldBoundary(new(0, -2), 4), ShapeGeometry.SeparationRay(10), ShapeGeometry.Segment(new(-5, 2), new(5, 3)),
        ShapeGeometry.Circle(4), ShapeGeometry.Rectangle(new(8, 10)), ShapeGeometry.Capsule(3, 10),
        ShapeGeometry.ConvexPolygon([new(-4, -4), new(4, -4), new(4, 4), new(-4, 4)]), ShapeGeometry.ConcavePolygon([new(-4, 0), new(4, 0)])];
    for (var i = 0; i < all.Length; i++)
    {
        using var shape = new ProceduralShape(all[i]); Check((int)PhysicsServer.ShapeGetType(shape.GetRID()) == i && shape.GetRect() == all[i].Bounds, "Every exact supported data family reaches the inherited shape API");
        using var peer = new CircleShape { Radius = 4 };
        Check(shape.Collide(Transform.Identity, peer, new(0, i == 1 ? new(0, 8) : Vector2.Zero)), "Every procedural geometry family participates in standalone collision");
    }
    using (var firstEdges = new ProceduralShape(all[7]))
    using (var secondEdges = new ProceduralShape(all[7]))
        Check(!firstEdges.Collide(Transform.Identity, secondEdges, Transform.Identity), "Two hollow contours preserve excluded resource-pair semantics");
    var querySpace = PhysicsServer.SpaceCreate(backend); var peerBody = PhysicsServer.BodyCreate();
    using var peerGeometry = new CircleShape { Radius = 4 };
    try
    {
        PhysicsServer.BodySetMode(peerBody, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(peerBody, peerGeometry.GetRID());
        PhysicsServer.BodySetSpace(peerBody, querySpace); var direct = PhysicsServer.SpaceGetDirectState(querySpace);
        foreach (var data in all)
        {
            using var source = new ProceduralShape(data); using var query = new PhysicsShapeQueryParameters { Shape = source };
            PhysicsServer.BodySetTransform(peerBody, new(0, data.Type == PhysicsServer.ShapeType.SeparationRay ? new(0, 8) : Vector2.Zero));
            Check(direct.IntersectShape(query).Length == 1, "All exact custom geometry families execute the selected direct-query implementation");
        }
    }
    finally { PhysicsServer.FreeRID(peerBody); PhysicsServer.FreeRID(querySpace); }
    Check(all[0].GetWorldBoundaryPlane() == (Vector2.Up, 2f) && all[1].GetSeparationRayParameters() == (10f, false) &&
        all[2].GetSegmentEndpoints() == (new Vector2(-5, 2), new Vector2(5, 3)) && all[3].GetCircleRadius() == 4 &&
        all[4].GetRectangleSize() == new Vector2(8, 10) && all[5].GetCapsuleDimensions() == (3f, 10f) &&
        all[6].GetConvexPolygonPoints().Length == 4 && all[7].GetConcavePolygonSegments().Length == 2, "Complete typed data accessors preserve factory parameters");
    Reject<InvalidOperationException>(() => all[4].GetCircleRadius());
    Reject<ArgumentOutOfRangeException>(() => ShapeGeometry.Circle(float.NaN)); Reject<ArgumentOutOfRangeException>(() => ShapeGeometry.Capsule(4, 2));
    Reject<ArgumentOutOfRangeException>(() => ShapeGeometry.Rectangle(Vector2.Zero)); Reject<ArgumentOutOfRangeException>(() => ShapeGeometry.SeparationRay(float.MaxValue));
    Reject<ArgumentOutOfRangeException>(() => ShapeGeometry.WorldBoundary(Vector2.Zero, 0)); Reject<ArgumentException>(() => ShapeGeometry.Segment(Vector2.Zero, new(float.NaN, 0)));
    Reject<ArgumentException>(() => ShapeGeometry.ConvexPolygon([Vector2.Zero, Vector2.One])); Reject<ArgumentException>(() => ShapeGeometry.ConcavePolygon([Vector2.One]));
}
static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
