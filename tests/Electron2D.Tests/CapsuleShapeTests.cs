using Electron2D;

internal static class CapsuleShapeTests
{
    internal static void Run()
    {
        VerifyDimensionsAndCopy();
        VerifyBodyAndAreaFixtures();
        VerifyPackedResource();
        Console.WriteLine("Capsule shape dimensions, contacts, areas, packing and allocation checks passed.");
    }

    private static void VerifyDimensionsAndCopy()
    {
        using var shape = new CapsuleShape();
        var changes = 0;
        shape.Changed += _ => changes++;
        Check(shape.Radius == 10 && shape.Height == 30 && shape.MidHeight == 10 &&
              shape.GetRect() == new Rect2(-10, -15, 20, 30),
            "The default capsule has linked radius, height, middle segment and local bounds.");
        shape.Radius = 20;
        Check(shape.Radius == 20 && shape.Height == 40 && shape.MidHeight == 0 && changes == 1,
            "Increasing radius past half-height grows the full height into a circle.");
        shape.Radius = 20;
        Check(changes == 1, "Assigning an equal radius does not publish a geometry change.");
        shape.Height = 18;
        Check(shape.Radius == 9 && shape.Height == 18 && shape.MidHeight == 0 && changes == 2,
            "Reducing height below the diameter shrinks radius into a circle.");
        shape.Height = 18;
        Check(changes == 2, "Assigning an equal height does not publish a geometry change.");
        shape.MidHeight = 14;
        shape.MidHeight = 14;
        Check(shape.Radius == 9 && shape.Height == 32 && shape.MidHeight == 14 &&
              shape.GetRect() == new Rect2(-9, -16, 18, 32) && changes == 4,
            "MidHeight wraps full height and emits even on an equal assignment.");

        using var copy = (CapsuleShape)shape.Duplicate();
        shape.Radius = 4;
        Check(copy.Radius == 9 && copy.Height == 32 && copy.MidHeight == 14 &&
              copy.GetRect() == new Rect2(-9, -16, 18, 32),
            "Duplicating a capsule preserves independent coupled geometry.");
        shape.Radius = 0;
        Check(shape.Height == 32 && shape.MidHeight == 32 && shape.GetRect() == new Rect2(0, -16, 0, 32),
            "Zero radius retains a line-segment capsule.");
        shape.Height = 0;
        Check(shape.Radius == 0 && shape.MidHeight == 0 && shape.GetRect() == new Rect2(0, 0, 0, 0),
            "Zero height also reduces radius to zero.");

        Reject<ArgumentOutOfRangeException>(() => shape.Radius = -1);
        Reject<ArgumentOutOfRangeException>(() => shape.Radius = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => shape.Radius = float.MaxValue);
        Reject<ArgumentOutOfRangeException>(() => shape.Height = -1);
        Reject<ArgumentOutOfRangeException>(() => shape.Height = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => shape.MidHeight = -1);
        Reject<ArgumentOutOfRangeException>(() => shape.MidHeight = float.NaN);
        shape.Radius = 10;
        Check(shape.Radius == 10 && shape.Height == 20 && shape.MidHeight == 0,
            "Invalid dimensions reject before changing linked state.");
        using var extreme = new CapsuleShape { Radius = float.MaxValue / 4 };
        var oldHeight = extreme.Height;
        Reject<ArgumentOutOfRangeException>(() => extreme.MidHeight = float.MaxValue);
        Check(extreme.Height == oldHeight, "An overflowing middle height rejects before mutation.");
        copy.Dispose();
        Reject<ObjectDisposedException>(() => _ = copy.GetRect());
        Reject<ObjectDisposedException>(() => copy.Height = 40);
    }

    private static void VerifyBodyAndAreaFixtures()
    {
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        using var bodyShape = new CapsuleShape();
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        var body = new RigidBody { CanSleep = false, ContactMonitor = true, MaxContactsReported = 4 };
        var bodyCollision = new CollisionShape { Shape = bodyShape };
        body.AddChild(bodyCollision);
        root.AddChild(floor); root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 72 and < 78 && body.GetContactCount() > 0 &&
              body.GetCollidingBodies().Length == 1 && ReferenceEquals(body.GetCollidingBodies()[0], floor),
            "A vertical capsule collides with the static floor at its bottom end cap.");

        bodyShape.Changed += _ => throw new InvalidOperationException("Earlier user listener failed.");
        Reject<InvalidOperationException>(() => bodyShape.Height = 50);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(bodyShape.Height == 50 && body.GlobalPosition.Y is > 62 and < 68,
            "The physics owner detects a changed resource revision after a user listener throws.");
        bodyCollision.Rotation = Mathf.Pi * 0.5f;
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 77 and < 83,
            "Rotating a long capsule on its child node rotates the fixture endpoints.");
        bodyCollision.Rotation = 0;
        Reject<InvalidOperationException>(() => bodyShape.MidHeight = 0);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(bodyShape.Radius == 10 && bodyShape.Height == 20 && body.GlobalPosition.Y is > 77 and < 83,
            "A zero-middle capsule keeps a live circular fixture.");
        Reject<InvalidOperationException>(() => bodyShape.MidHeight = 0.1f);
        for (var frame = 0; frame < 8; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GetContactCount() > 0 && bodyShape.MidHeight > 0,
            "A sub-tolerance middle segment retains contact through the backend's circular fallback.");

        using var areaShape = new CapsuleShape { Height = 90 };
        var area = new Area { Position = new(0, 40) };
        area.AddChild(new CollisionShape { Shape = areaShape });
        var bodyEntries = 0; var bodyExits = 0;
        area.BodyEntered += other => { if (ReferenceEquals(other, body)) bodyEntries++; };
        area.BodyExited += other => { if (ReferenceEquals(other, body)) bodyExits++; };
        root.AddChild(area);
        tree.PhysicsFrame(1d / 60);
        Check(bodyEntries == 1 && area.GetOverlappingBodies().Contains(body),
            "A capsule sensor detects an overlapping rigid body without collision response.");
        areaShape.Radius = 0;
        tree.PhysicsFrame(1d / 60);
        Check(area.GetOverlappingBodies().Contains(body),
            "A zero-radius capsule behaves as a vertical segment in area detection.");
        areaShape.Height = 0;
        tree.PhysicsFrame(1d / 60);
        Check(bodyExits == 1 && !area.GetOverlappingBodies().Contains(body),
            "A zero-size capsule fixture removes its sensor overlap on the next step.");

        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed capsule body contact and area scans allocate no managed memory.");
    }

    private static void VerifyPackedResource()
    {
        using var shape = new CapsuleShape { Radius = 7, MidHeight = 23 };
        using var root = new Node { Name = "Root" };
        var body = new RigidBody { Name = "Body" };
        var collision = new CollisionShape { Name = "Collision", Shape = shape };
        root.AddChild(body); body.AddChild(collision);
        body.Owner = root; collision.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root);
        using var copy = packed.Instantiate();
        Check(ReferenceEquals(copy.GetNode<CollisionShape>("Body/Collision").Shape, shape) &&
              shape.Radius == 7 && shape.Height == 37,
            "PackedScene keeps the borrowed capsule resource and its coupled dimensions.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
