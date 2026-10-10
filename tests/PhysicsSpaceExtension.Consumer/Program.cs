using Electron2D;
using System.Diagnostics;

var backend = args.FirstOrDefault() == "gpu" ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
using var world = new World(backend); using var shape = new CircleShape { Radius = 5 }; using var queryShape = new CircleShape { Radius = 4 };
using var root = new SubViewport { World = world };
var scene = new StaticBody(); scene.AddChild(new CollisionShape { Shape = shape }); root.AddChild(scene);
using var tree = new SceneTree(root);
var raw = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate(); var foreignSpace = PhysicsServer.SpaceCreate(backend); var foreign = PhysicsServer.BodyCreate();
try
{
    PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(raw, shape.GetRID());
    PhysicsServer.BodySetTransform(raw, new(0, new(30, 0))); PhysicsServer.BodySetCollisionLayer(raw, 2); PhysicsServer.BodySetSpace(raw, world.Space);
    PhysicsServer.AreaAddShape(area, shape.GetRID()); PhysicsServer.AreaSetTransform(area, new(0, new(60, 0))); PhysicsServer.AreaSetSpace(area, world.Space);
    PhysicsServer.AreaAttachCanvasInstanceID(area, 7);
    PhysicsServer.BodyAddShape(foreign, shape.GetRID()); PhysicsServer.BodySetSpace(foreign, foreignSpace);
    using var extension = new CircleQueries(world.Space, [new(scene.GetRID(), shape, false), new(raw, shape, false), new(area, shape, true)], queryShape);
    PhysicsDirectSpaceState view = extension;
    using var point = new PhysicsPointQueryParameters { Position = new(30, 0), CollisionMask = 2 };
    using var ray = new PhysicsRayQueryParameters { From = new(-20, 0), To = new(80, 0) };
    using var overlap = new PhysicsShapeQueryParameters { Shape = queryShape, Transform = new(0, new(26, 0)), CollisionMask = 2 };
    using var sweep = new PhysicsShapeQueryParameters { Shape = queryShape, Transform = new(0, new(12, 0)), Motion = new(24, 0), CollisionMask = 2 };
    var points = new PhysicsPointResult[2]; var shapes = new PhysicsShapeResult[2]; var contacts = new Vector2[5];
    Check(view.IntersectRay(ray) is { ColliderRID: var first } && first == scene.GetRID(), "External analytic ray sees the real scene body.");
    extension.Suppressed = scene.GetRID();
    Check(view.IntersectRay(ray) is { ColliderRID: var second, Position: var position } && second == raw && position.DistanceTo(new(25, 0)) < .0001f &&
        PhysicsServer.SpaceGetDirectState(world.Space).IntersectRay(ray)!.Value.ColliderRID == scene.GetRID(), "External query policy changes the hit without replacing the cached built-in view.");
    Check(view.IntersectPoint(point).Single().ColliderRID == raw && view.IntersectShape(overlap).Single().ColliderRID == raw &&
        view.CollideShape(overlap).Length == 2, "Inherited array queries invoke the supplied algorithms.");
    Evaluate();
    using var areaPoint = new PhysicsPointQueryParameters { Position = new(60, 0), CollideWithBodies = false, CollideWithAreas = true, CanvasInstanceID = 7 };
    Check(view.IntersectPoint(areaPoint, points) == 1 && points[0].ColliderRID == area, "Area/canvas selection reaches the typed point hook.");
    areaPoint.CanvasInstanceID = 0; Check(view.IntersectPoint(areaPoint, points) == 0, "Zero is the default canvas filter.");
    point.Exclude = [raw]; Check(view.IntersectPoint(point, points) == 0 && !extension.IsBodyExcludedFromQuery(raw), "Scoped exclusions end with the invocation."); point.Exclude = [];
    using var inner = new PhysicsPointQueryParameters { Position = new(30, 0), CollisionMask = 2, Exclude = [raw] };
    point.Exclude = [scene.GetRID()]; var depth = 0;
    extension.OnQuery = () =>
    {
        if (depth++ == 0)
        {
            Check(extension.IsBodyExcludedFromQuery(scene.GetRID()) && !extension.IsBodyExcludedFromQuery(raw), "Outer exclusions.");
            Check(view.IntersectPoint(inner, points) == 0, "Same-family nested query has separate scratch.");
            Check(extension.IsBodyExcludedFromQuery(scene.GetRID()) && !extension.IsBodyExcludedFromQuery(raw), "Outer exclusions restored after nesting.");
            extension.Throw = true;
            Reject<InvalidOperationException>(() => view.IntersectPoint(inner, points)); extension.Throw = false;
            Check(!extension.IsBodyExcludedFromQuery(raw), "Outer exclusions restored after nested failure.");
            Reject<InvalidOperationException>(extension.Dispose);
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceCreateCheckpoint(world.Space)); Reject<InvalidOperationException>(() => root.World = null);
            Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        }
        depth--;
    };
    Check(view.IntersectPoint(point, points) == 1 && points[0].ColliderRID == raw, "Nested failure preserves the outer result.");
    extension.OnQuery = null; point.Exclude = [];
    points[0] = new(raw, 0); points[1] = new(raw, 0); var sentinel = points[0];
    extension.BadCount = -1; Reject<InvalidOperationException>(() => view.IntersectPoint(point, points));
    extension.BadCount = 3; Reject<InvalidOperationException>(() => view.IntersectPoint(point, points)); extension.BadCount = 0;
    extension.Duplicate = true; Reject<InvalidOperationException>(() => view.IntersectPoint(point, points)); extension.Duplicate = false;
    extension.Injected = foreign; Reject<InvalidOperationException>(() => view.IntersectPoint(point, points)); extension.Injected = default;
    Check(points[0].ColliderRID == sentinel.ColliderRID && points[1].ColliderRID == raw, "Rejected counts and foreign identity do not publish partial output.");
    extension.BadFractions = true; Reject<InvalidOperationException>(() => view.CastMotion(sweep)); extension.BadFractions = false;
    extension.BadPoints = true; contacts[0] = new(999, 999); Reject<InvalidOperationException>(() => view.CollideShape(overlap, contacts));
    Check(contacts[0] == new Vector2(999, 999), "Invalid contact geometry preserves output."); extension.BadPoints = false;
    using var missing = new PhysicsShapeQueryParameters(); Reject<ArgumentException>(() => view.IntersectShape(missing, shapes));
    Reject<ArgumentException>(() => new CircleQueries(default, []));
    extension.CachedPoint = new(raw, 0);
    using var association = new Node(); PhysicsServer.BodyAttachObject(raw, association);
    Check(view.IntersectPoint(point, points) == 1 && points[0].ColliderID == association.InstanceID && view.GetRestInfo(overlap)!.Value.ColliderID == association.InstanceID,
        "The library resamples the current association even when the hook returns a stale cached result.");
    extension.CachedPoint = null;
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
    var warmup = Stopwatch.StartNew(); var warmCycles = 0;
    do
    {
        for (var i = 0; i < 64; i++) Evaluate();
        warmCycles += 64;
    } while (warmup.Elapsed.TotalSeconds < 1);
    _ = GC.GetTotalAllocatedBytes(true); _ = GC.GetAllocatedBytesForCurrentThread();
    var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
    for (var i = 0; i < 64; i++) Evaluate();
    owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
    Check(owner == 0 && all == 0, $"Warmed external queries allocated {owner}/{all} owner/all-thread bytes.");
    Task.Run(() => Reject<InvalidOperationException>(() => view.IntersectPoint(point, points))).GetAwaiter().GetResult();
    PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
    PhysicsServer.BodySetLinearVelocity(raw, new(60, 0)); tree.PhysicsFrame(1d / 60);
    var moved = PhysicsServer.BodyGetTransform(raw).Origin;
    Check(moved.X > 30.5f, "Selected physical solver advances the real raw body."); point.Position = moved;
    Check(view.IntersectPoint(point, points) == 1, "External queries read current physical motion.");
    extension.Injected = raw; PhysicsServer.BodySetShapeDisabled(raw, 0, true);
    Reject<InvalidOperationException>(() => view.IntersectPoint(point, points)); PhysicsServer.BodySetShapeDisabled(raw, 0, false);
    PhysicsServer.BodySetSpace(raw, foreignSpace); Reject<InvalidOperationException>(() => view.IntersectPoint(point, points));
    PhysicsServer.BodySetSpace(raw, world.Space); extension.Injected = default;
    using var owned = new CircleQueries(foreignSpace, [new(foreign, shape, false)], queryShape);
    using var ownedPoint = new PhysicsPointQueryParameters();
    PhysicsServer.SpaceSetActive(foreignSpace, true);
    owned.OnQuery = () =>
    {
        Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(foreignSpace));
        Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(foreignSpace, 1d / 60));
        Reject<InvalidOperationException>(() => PhysicsServer.SpaceCreateCheckpoint(foreignSpace));
    };
    Check(owned.IntersectPoint(ownedPoint, points) == 1, "Caller-owned world release/step/checkpoint guards preserve real query state.");
    owned.OnQuery = null;
    if (backend == PhysicsServer.Backend.GPU)
    {
        Reject<AggregateException>(() => PhysicsServer.SpaceStep(foreignSpace, float.MaxValue));
        Reject<InvalidOperationException>(() => owned.IntersectPoint(ownedPoint, points));
    }
    Console.WriteLine($"{backend}: external public-only query inheritance, real scene/raw/Area geometry, policy, filters/identity, nested exclusions/failure, borrowed lifetime, atomic output and {warmCycles} warm cycles/64 measured six-query cycles: {owner}/{all} B passed.");
    tree.Dispose(); world.Dispose(); Reject<ArgumentException>(() => view.IntersectPoint(point, points));
    extension.Dispose(); Reject<ObjectDisposedException>(() => view.IntersectRay(ray));

    void Evaluate()
    {
        Check(view.IntersectRay(ray) is { ColliderRID: var rid } && rid == raw, "Ray core.");
        Check(view.IntersectPoint(point, points) == 1 && points[0].ColliderRID == raw, "Point core.");
        Check(view.IntersectShape(overlap, shapes) == 1 && shapes[0].ColliderRID == raw, "Shape core.");
        var bracket = view.CastMotion(sweep); Check(MathF.Abs(bracket.SafeFraction - .375f) < .0001f && bracket.UnsafeFraction == bracket.SafeFraction, "Analytic collision bracket within .0001.");
        contacts[^1] = new(777, 777); Check(view.CollideShape(overlap, contacts) == 1 && contacts[0].DistanceTo(new(30, 0)) < .0001f &&
            contacts[1].DistanceTo(new(25, 0)) < .0001f && contacts[^1] == new Vector2(777, 777), "Complete contact pairs preserve odd tail.");
        Check(view.GetRestInfo(overlap) is { ColliderRID: var rest, Point: var location } && rest == raw && location.DistanceTo(new(25, 0)) < .0001f, "Rest core.");
    }
}
finally { PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(foreign); PhysicsServer.FreeRID(foreignSpace); }
static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
