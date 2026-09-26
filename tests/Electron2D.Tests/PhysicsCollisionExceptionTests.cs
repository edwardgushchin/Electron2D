using Electron2D;

internal static class PhysicsCollisionExceptionTests
{
    internal static void Run()
    {
        VerifySceneBodyExceptions();
        VerifyServerBodyExceptions();
        Console.WriteLine("Physics body exception contacts, motion and lifecycle checks passed.");
    }

    private static void VerifySceneBodyExceptions()
    {
        using var circle = new CircleShape();
        using var rectangle = new RectangleShape { Size = new(200, 20) };
        var root = new Node();
        var mover = new RigidBody
        {
            Name = "Mover",
            GravityScale = 0,
            LinearVelocity = new(0, 80)
        };
        mover.AddChild(new CollisionShape { Shape = circle });
        var floor = new StaticBody { Name = "Floor", Position = new(0, 80) };
        floor.AddChild(new CollisionShape { Shape = rectangle });
        root.AddChild(mover); root.AddChild(floor);
        using var tree = new SceneTree(root);

        Check(mover.GetCollisionExceptions().Length == 0,
            "A new body has no explicit collision exceptions.");
        mover.AddCollisionExceptionWith(floor);
        mover.AddCollisionExceptionWith(floor);
        Check(mover.GetCollisionExceptions() is [var listed] && ReferenceEquals(listed, floor) &&
              floor.GetCollisionExceptions().Length == 0,
            "A duplicate scene exception is idempotent and stored only on its owning body.");
        using var probe = new KinematicCollision2D();
        Check(!mover.TestMove(Transform.Identity, new(0, 100), probe) &&
              !floor.TestMove(floor.GlobalTransform, new(0, -100)),
            "A one-sided entry suppresses motion contacts in both body directions.");
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(mover.Position.Y > 100,
            "The exception also suppresses regular solver response while the body crosses the floor.");

        mover.RemoveCollisionExceptionWith(floor);
        mover.RemoveCollisionExceptionWith(floor);
        Check(mover.GetCollisionExceptions().Length == 0 &&
              mover.TestMove(Transform.Identity, new(0, 100), probe) &&
              probe.GetColliderRID() == floor.GetRID(),
            "Removing an entry restores direct motion contact without duplicate-removal failure.");
        mover.Position = Vector2.Zero;
        mover.LinearVelocity = new(0, 80);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(mover.Position.Y is > 45 and < 65,
            "The ordinary solver again stops the moving body at the floor.");

        mover.AddCollisionExceptionWith(floor);
        mover.LinearVelocity = new(0, 80);
        for (var frame = 0; frame < 90; frame++) tree.PhysicsFrame(1d / 60);
        Check(mover.Position.Y > 100,
            "Adding an exception to an already touching pair releases its next solver response.");
        mover.RemoveCollisionExceptionWith(floor);

        Reject<ArgumentNullException>(() => mover.AddCollisionExceptionWith(null!));
        Reject<ArgumentNullException>(() => mover.RemoveCollisionExceptionWith(null!));
        PhysicsServer.Instance.BodyAddCollisionException(mover.GetRID(), default);
        Check(mover.GetCollisionExceptions() is [null],
            "An opaque server exception can retain an empty RID without a matching scene body.");
        PhysicsServer.Instance.BodyRemoveCollisionException(mover.GetRID(), default);
        Check(mover.GetCollisionExceptions().Length == 0,
            "Invalid exception writes leave the prior list unchanged.");
        Reject<InvalidOperationException>(() => Task.Run(() => mover.AddCollisionExceptionWith(floor)).GetAwaiter().GetResult());
        mover.AddCollisionExceptionWith(floor);
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed steady physics frames with an active exception allocate no managed bytes.");
        root.RemoveChild(mover);
        root.AddChild(mover);
        Check(!mover.TestMove(Transform.Identity, new(0, 100)),
            "A body exception survives scene exit and recreates filtered fixtures on reentry.");
        Reject<InvalidOperationException>(() => Task.Run(mover.GetCollisionExceptions).GetAwaiter().GetResult());
        var server = PhysicsServer.Instance;
        var serverBody = server.BodyCreate();
        server.BodySetMode(serverBody, PhysicsServer.BodyMode.Static);
        server.BodySetSpace(serverBody, mover.GetWorld2D()!.Space);
        server.BodyAddCollisionException(mover.GetRID(), serverBody);
        Check(mover.GetCollisionExceptions() is [var sceneEntry, null] &&
              ReferenceEquals(sceneEntry, floor),
            "A server-only exception keeps its RID slot while scene enumeration returns null for its missing node.");
        server.FreeRID(serverBody);
        Check(mover.GetCollisionExceptions() is [var liveEntry, null] && ReferenceEquals(liveEntry, floor),
            "A freed exception target remains an inert, readable RID slot until explicitly removed.");
        server.BodyRemoveCollisionException(mover.GetRID(), serverBody);
    }

    private static void VerifyServerBodyExceptions()
    {
        var server = PhysicsServer.Instance;
        var space = server.SpaceCreate();
        var mover = server.BodyCreate();
        var floor = server.BodyCreate();
        var sensor = server.AreaCreate();
        var circle = server.CircleShapeCreate();
        var rectangle = server.RectangleShapeCreate();
        using var floorGeometry = new RectangleShape { Size = new(200, 20) };
        server.ShapeSetData(rectangle, floorGeometry);
        server.BodyAddShape(mover, circle);
        server.BodyAddShape(floor, rectangle);
        server.BodySetMode(mover, PhysicsServer.BodyMode.Rigid);
        server.BodySetMode(floor, PhysicsServer.BodyMode.Static);
        server.BodySetTransform(floor, new(0, Vector2.One, 0, new(0, 80)));
        server.BodySetSpace(mover, space);
        server.BodySetSpace(floor, space);
        server.AreaSetSpace(sensor, space);
        Reject<ArgumentException>(() => server.BodyAddCollisionException(default, floor));
        Reject<ArgumentException>(() => server.BodyAddCollisionException(sensor, floor));
        server.BodyAddCollisionException(mover, sensor);
        server.BodyRemoveCollisionException(mover, sensor);
        server.BodyAddCollisionException(mover, floor);
        using var query = new PhysicsTestMotionParameters2D { Motion = new(0, 100) };
        using var reverseQuery = new PhysicsTestMotionParameters2D
        {
            From = new(0, Vector2.One, 0, new(0, 80)),
            Motion = new(0, -100)
        };
        Check(!server.BodyTestMotion(mover, query) &&
              !server.BodyTestMotion(floor, reverseQuery),
            "An exception owned by one server body suppresses either direction of body motion.");
        server.BodySetLinearVelocity(mover, new(0, 80));
        for (var frame = 0; frame < 120; frame++) server.SpaceStep(space, 1d / 60);
        Check(server.BodyGetTransform(mover).Origin.Y > 100,
            "An explicit server world's regular solver lets the excepted body cross the floor.");
        server.BodyRemoveCollisionException(mover, floor);
        Check(server.BodyTestMotion(mover, query),
            "Removing the server exception restores motion contact.");
        server.BodySetTransform(mover, Transform.Identity);
        server.BodySetLinearVelocity(mover, new(0, 80));
        for (var frame = 0; frame < 120; frame++) server.SpaceStep(space, 1d / 60);
        Check(server.BodyGetTransform(mover).Origin.Y is > 45 and < 65,
            "Removing a server exception re-enables solver contact in the same space.");
        server.FreeRID(sensor); server.FreeRID(mover); server.FreeRID(floor);
        server.FreeRID(circle); server.FreeRID(rectangle); server.FreeRID(space);
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
