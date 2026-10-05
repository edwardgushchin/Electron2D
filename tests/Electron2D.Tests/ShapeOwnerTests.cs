using Electron2D;

internal static class ShapeOwnerTests
{
    internal static void Run()
    {
        VerifyRegistry();
        VerifyManualFixturesAndMotion();
        VerifyChildBridgeAndPacking();
        VerifyAreaAndOneWay();
        VerifyLifetimeAndAllocation();
        Console.WriteLine("Collision owner groups, logical indices, manual fixtures and allocation checks passed.");
    }

    private static Transform At(float x, float y, float rotation = 0) => new(rotation, Vector2.One, 0, new(x, y));

    private static void VerifyRegistry()
    {
        using var body = new StaticBody();
        using var owner = new Resource();
        using var circle = new CircleShape();
        using var box = new RectangleShape();
        var a = body.CreateShapeOwner(owner); var b = body.CreateShapeOwner(null);
        Check(a == 0 && b == 1 && body.GetShapeOwners().SequenceEqual(new uint[] { 0, 1 }) &&
            body.ShapeOwnerGetTransform(a) == Transform.Identity && !body.IsShapeOwnerDisabled(a) &&
            !body.IsShapeOwnerOneWayCollisionEnabled(a) && body.GetShapeOwnerOneWayCollisionMargin(a) == 0 &&
            body.GetShapeOwnerOneWayCollisionDirection(a) == Vector2.Down && ReferenceEquals(body.ShapeOwnerGetOwner(a), owner),
            "Owner defaults, weak identity and sorted IDs are independent of a SceneTree.");
        body.ShapeOwnerAddShape(a, circle); body.ShapeOwnerAddShape(b, box); body.ShapeOwnerAddShape(a, box);
        Check(body.ShapeOwnerGetShapeCount(a) == 2 && body.ShapeOwnerGetShapeIndex(a, 0) == 0 &&
            body.ShapeOwnerGetShapeIndex(b, 0) == 1 && body.ShapeOwnerGetShapeIndex(a, 1) == 2 &&
            body.ShapeFindOwner(2) == a && ReferenceEquals(body.ShapeOwnerGetShape(a, 1), box),
            "Interleaved owner additions preserve global append order and local group order.");
        body.ShapeOwnerSetDisabled(a, true);
        Check(body.IsShapeOwnerDisabled(a) && body.ShapeOwnerGetShapeCount(a) == 2, "Disabled groups retain logical slots.");
        body.ShapeOwnerRemoveShape(a, 0);
        Check(body.ShapeOwnerGetShapeIndex(b, 0) == 0 && body.ShapeOwnerGetShapeIndex(a, 0) == 1 && body.ShapeFindOwner(0) == b,
            "Removal renumbers all later slots across groups.");
        var copy = body.GetShapeOwners(); copy[0] = 999;
        Check(body.GetShapeOwners()[0] == 0, "Returned owner arrays are caller-owned.");
        owner.Dispose();
        Check(body.ShapeOwnerGetOwner(a) is null && body.ShapeOwnerGetShapeCount(a) == 1,
            "Disposed owner identity becomes null without removing its shape group.");
        body.ShapeOwnerClearShapes(a); body.RemoveShapeOwner(b);
        Check(body.ShapeOwnerGetShapeCount(a) == 0 && body.CreateShapeOwner(null) == 1,
            "Clear preserves owner configuration and highest removed IDs may be reused.");
        Reject<ArgumentOutOfRangeException>(() => body.ShapeFindOwner(0));
        Reject<ArgumentOutOfRangeException>(() => body.ShapeOwnerGetShape(a, -1));
        Reject<ArgumentOutOfRangeException>(() => body.RemoveShapeOwner(99));
        Reject<ArgumentNullException>(() => body.ShapeOwnerAddShape(a, null!));
        Reject<ArgumentException>(() => body.ShapeOwnerSetTransform(a, new(0, new(2, 1), 0, Vector2.Zero)));
        body.ShapeOwnerSetOneWayCollisionDirection(a, Vector2.Zero);
        Check(body.GetShapeOwnerOneWayCollisionDirection(a) == Vector2.Zero, "Zero direction remains zero after normalization.");
        Reject<ArgumentOutOfRangeException>(() => body.ShapeOwnerSetOneWayCollisionDirection(a, new(float.NaN, 0)));
        body.ShapeOwnerSetOneWayCollisionDirection(a, Vector2.Down);
        Reject<ArgumentOutOfRangeException>(() => body.ShapeOwnerSetOneWayCollisionMargin(a, -1));
        Check(body.ShapeOwnerGetTransform(a) == Transform.Identity && body.GetShapeOwnerOneWayCollisionDirection(a) == Vector2.Down,
            "Malformed owner mutations reject before replacing stored configuration.");
    }

    private static void VerifyManualFixturesAndMotion()
    {
        using var circle = new CircleShape();
        using var box = new RectangleShape { Size = new(200, 20) };
        using var identity = new Resource();
        var root = new Node();
        var mover = new StaticBody { Name = "Mover" };
        var localOwner = mover.CreateShapeOwner(identity); mover.ShapeOwnerAddShape(localOwner, circle);
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100) };
        var floorOwner = floor.CreateShapeOwner(null); floor.ShapeOwnerAddShape(floorOwner, box);
        root.AddChild(mover); root.AddChild(floor);
        using var tree = new SceneTree(root);
        using var collision = new KinematicCollision();
        Check(mover.TestMove(Transform.Identity, new(0, 120), collision) &&
            ReferenceEquals(collision.GetLocalShape(), identity) && collision.GetColliderShape() is null &&
            floor.ShapeFindOwner(collision.GetColliderShapeIndex()) == floorOwner,
            "Manual geometry executes motion with arbitrary owner object identity.");
        var direct = floor.GetWorld()!.DirectSpaceState;
        using var point = new PhysicsPointQueryParameters { Position = new(0, 100) };
        Check(direct.IntersectPoint(point) is [var hit] && hit.ShapeIndex == floor.ShapeOwnerGetShapeIndex(floorOwner, 0),
            "Direct query results expose global logical indices rather than owner IDs.");
        floor.ShapeOwnerSetTransform(floorOwner, At(300, 0));
        Check(direct.IntersectPoint(point).Length == 0, "Owner transform changes reach backend geometry before the next query.");
        floor.ShapeOwnerSetTransform(floorOwner, Transform.Identity); floor.ShapeOwnerSetDisabled(floorOwner, true);
        Check(!mover.TestMove(Transform.Identity, new(0, 120), collision), "Disabled manual owners remove motion response.");
        floor.ShapeOwnerSetDisabled(floorOwner, false);
        circle.Changed += _ => throw new InvalidOperationException("User listener failed.");
        Reject<InvalidOperationException>(() => circle.Radius = 20);
        Check(mover.TestMove(Transform.Identity, new(0, 120), collision) && collision.GetTravel().Y < 71,
            "Borrowed revision changes reach manual fixtures despite a failed Changed subscriber.");
    }

    private static void VerifyChildBridgeAndPacking()
    {
        using var circle = new CircleShape();
        using var box = new RectangleShape();
        using var body = new StaticBody { Name = "Body" };
        var child = new CollisionShape { Name = "Collision", Shape = circle, Position = new(20, 0) };
        body.AddChild(child); child.Owner = body;
        var id = body.GetShapeOwners().Single();
        Check(ReferenceEquals(body.ShapeOwnerGetOwner(id), child) && ReferenceEquals(body.ShapeOwnerGetShape(id, 0), circle) &&
            body.ShapeOwnerGetTransform(id).Origin == child.Position, "Direct children establish registry groups at parenting.");
        body.ShapeOwnerSetTransform(id, At(80, 0)); body.ShapeOwnerSetDisabled(id, true);
        child.OneWayCollision = true;
        Check(body.ShapeOwnerGetTransform(id).Origin == new Vector2(80, 0) && body.IsShapeOwnerDisabled(id),
            "A child policy edit preserves independent owner transform and disabled overrides.");
        child.Position = new(40, 0);
        Check(body.ShapeOwnerGetTransform(id).Origin == new Vector2(80, 0) && body.IsShapeOwnerDisabled(id),
            "Detached child transforms do not send active local-transform notifications.");
        child.Shape = box;
        Check(body.ShapeOwnerGetShapeCount(id) == 1 && ReferenceEquals(body.ShapeOwnerGetShape(id, 0), box),
            "Changing a child resource replaces that group's borrowed shapes.");
        using var packed = new PackedScene(); packed.Pack(body);
        using var copied = (StaticBody)packed.Instantiate();
        var copiedID = copied.GetShapeOwners().Single();
        Check(ReferenceEquals(copied.ShapeOwnerGetOwner(copiedID), copied.GetChild(0)) && copied.ShapeOwnerGetShapeCount(copiedID) == 1,
            "Packed scene child groups rebuild from configured nodes rather than registry internals.");
        var root = new Node(); root.AddChild(body);
        using var tree = new SceneTree(root);
        Check(body.ShapeOwnerGetTransform(id).Origin == child.Position && !body.IsShapeOwnerDisabled(id),
            "Tree entry resynchronizes the child's configured transform and policy.");
        body.ShapeOwnerSetDisabled(id, true); child.Position = new(60, 0);
        Check(body.ShapeOwnerGetTransform(id).Origin == child.Position && body.IsShapeOwnerDisabled(id),
            "An active child transform notification updates only its transform.");
        root.RemoveChild(body);
        Check(body.GetShapeOwners().Single() == id, "A collision object retains owner groups while detached from the tree.");
        root.AddChild(body); body.RemoveChild(child);
        Check(body.GetShapeOwners().Length == 0, "Unparenting the child removes its group and slots.");
        child.Dispose();
    }

    private static void VerifyAreaAndOneWay()
    {
        using var circle = new CircleShape();
        using var platformShape = new RectangleShape { Size = new(200, 10) };
        var root = new Node();
        var area = new Area { Name = "Sensor" }; var areaOwner = area.CreateShapeOwner(null); area.ShapeOwnerAddShape(areaOwner, circle);
        area.ShapeOwnerSetOneWayCollision(areaOwner, true); area.ShapeOwnerSetOneWayCollisionMargin(areaOwner, 100);
        area.ShapeOwnerSetOneWayCollisionDirection(areaOwner, Vector2.Left);
        Check(!area.IsShapeOwnerOneWayCollisionEnabled(areaOwner) && area.GetShapeOwnerOneWayCollisionMargin(areaOwner) == 0 &&
            area.GetShapeOwnerOneWayCollisionDirection(areaOwner) == Vector2.Down, "Area one-way setters preserve their no-effect contract.");
        var body = new StaticBody { Name = "Probe" }; var bodyOwner = body.CreateShapeOwner(null); body.ShapeOwnerAddShape(bodyOwner, circle);
        var platform = new StaticBody { Name = "Platform", Position = new(0, 100) };
        var platformOwner = platform.CreateShapeOwner(null); platform.ShapeOwnerAddShape(platformOwner, platformShape);
        platform.ShapeOwnerSetOneWayCollision(platformOwner, true); platform.ShapeOwnerSetOneWayCollisionMargin(platformOwner, 5);
        platform.ShapeOwnerSetOneWayCollisionDirection(platformOwner, new(0, 4));
        root.AddChild(area); root.AddChild(body); root.AddChild(platform);
        using var tree = new SceneTree(root); tree.PhysicsFrame(1d / 60);
        Check(area.OverlapsBody(body), "Manual owner geometry participates in Area sensing.");
        Check(body.TestMove(Transform.Identity, new(0, 120)) && !body.TestMove(At(0, 130), new(0, -60)),
            "Manual one-way body owner blocks its solid side and permits reverse traversal.");
        area.ShapeOwnerSetDisabled(areaOwner, true); tree.PhysicsFrame(1d / 60);
        Check(!area.OverlapsBody(body), "Disabled owner geometry is removed from overlap snapshots.");
    }

    private static void VerifyLifetimeAndAllocation()
    {
        using var circle = new CircleShape();
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        var root = new Node();
        var body = new RigidBody { Name = "Body", CanSleep = false }; var bodyOwner = body.CreateShapeOwner(null); body.ShapeOwnerAddShape(bodyOwner, circle);
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100) }; var floorOwner = floor.CreateShapeOwner(null); floor.ShapeOwnerAddShape(floorOwner, floorShape);
        root.AddChild(body); root.AddChild(floor);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 75 and < 85, "Manual owner fixtures provide ordinary rigid-body solver response.");
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed owner fixtures and solver frames allocate no managed bytes.");
        Check(Task.Run(() => Capture(() => body.ShapeOwnerGetShapeCount(bodyOwner))).GetAwaiter().GetResult() is InvalidOperationException,
            "Attached owner registry access rejects a foreign thread.");
        circle.Dispose(); tree.PhysicsFrame(1d / 60);
        Check(body.ShapeOwnerGetShapeCount(bodyOwner) == 1 && ReferenceEquals(body.ShapeOwnerGetShape(bodyOwner, 0), circle),
            "Resource disposal removes fixture activity without silently changing logical indices.");
    }

    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { if (Capture(action) is not T) throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
