using Electron2D;

internal sealed class PhysicsCollisionExceptionTests(PhysicsServer.Backend backend)
{
    internal static void Run(PhysicsServer.Backend backend = PhysicsServer.Backend.CPU) => new PhysicsCollisionExceptionTests(backend).RunCore();
    private void RunCore()
    {
        _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
        VerifySceneBodyExceptions();
        VerifyServerBodyExceptions();
        Console.WriteLine($"Physics body exception contacts, motion, lifecycle and 0/0 warm owner/all-thread byte checks passed on {backend}.");
    }

    private void VerifySceneBodyExceptions()
    {
        using var circle = new CircleShape();
        using var rectangle = new RectangleShape { Size = new(200, 20) };
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld };
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
        using var tree = new SceneTree(root); Check(PhysicsServer.SpaceGetBackend(selectedWorld.Space) == backend, "Exception fixture uses the explicitly selected backend.");

        Check(mover.GetCollisionExceptions().Length == 0,
            "A new body has no explicit collision exceptions.");
        mover.AddCollisionExceptionWith(floor);
        mover.AddCollisionExceptionWith(floor);
        Check(mover.GetCollisionExceptions() is [var listed] && ReferenceEquals(listed, floor) &&
              floor.GetCollisionExceptions().Length == 0,
            "A duplicate scene exception is idempotent and stored only on its owning body.");
        using var probe = new KinematicCollision();
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
        PhysicsServer.BodyAddCollisionException(mover.GetRID(), default);
        Check(mover.GetCollisionExceptions() is [null],
            "An opaque server exception can retain an empty RID without a matching scene body.");
        PhysicsServer.BodyRemoveCollisionException(mover.GetRID(), default);
        Check(mover.GetCollisionExceptions().Length == 0,
            "Invalid exception writes leave the prior list unchanged.");
        Reject<InvalidOperationException>(() => Task.Run(() => mover.AddCollisionExceptionWith(floor)).GetAwaiter().GetResult());
        mover.AddCollisionExceptionWith(floor);
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread(); var allBefore = GC.GetTotalAllocatedBytes(true);
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - before; var allBytes = GC.GetTotalAllocatedBytes(true) - allBefore;
        Check(ownerBytes == 0 && allBytes == 0, $"Warm active exception frames allocated {ownerBytes}/{allBytes} owner/all-thread bytes.");
        root.RemoveChild(mover);
        root.AddChild(mover);
        Check(!mover.TestMove(Transform.Identity, new(0, 100)),
            "A body exception survives scene exit and recreates filtered fixtures on reentry.");
        Reject<InvalidOperationException>(() => Task.Run(mover.GetCollisionExceptions).GetAwaiter().GetResult());
        var serverBody = PhysicsServer.BodyCreate();
        PhysicsServer.BodySetMode(serverBody, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodySetSpace(serverBody, mover.GetWorld()!.Space);
        PhysicsServer.BodyAddCollisionException(mover.GetRID(), serverBody);
        Check(mover.GetCollisionExceptions() is [var sceneEntry, null] &&
              ReferenceEquals(sceneEntry, floor),
            "A server-only exception keeps its RID slot while scene enumeration returns null for its missing node.");
        PhysicsServer.FreeRID(serverBody);
        Check(mover.GetCollisionExceptions() is [var liveEntry, null] && ReferenceEquals(liveEntry, floor),
            "A freed exception target remains an inert, readable RID slot until explicitly removed.");
        PhysicsServer.BodyRemoveCollisionException(mover.GetRID(), serverBody);
    }

    private void VerifyServerBodyExceptions()
    {
        var space = PhysicsServer.SpaceCreate(backend); Check(PhysicsServer.SpaceGetBackend(space) == backend, "Raw exceptions use the explicitly selected backend."); PhysicsServer.SpaceSetActive(space, true);
        var mover = PhysicsServer.BodyCreate();
        var floor = PhysicsServer.BodyCreate();
        var sensor = PhysicsServer.AreaCreate();
        var circle = PhysicsServer.CircleShapeCreate();
        var rectangle = PhysicsServer.RectangleShapeCreate();
        using var floorGeometry = new RectangleShape { Size = new(200, 20) };
        try
        {
            PhysicsServer.ShapeSetData(rectangle, floorGeometry);
            PhysicsServer.BodyAddShape(mover, circle);
            PhysicsServer.BodyAddShape(floor, rectangle);
            PhysicsServer.BodySetMode(mover, PhysicsServer.BodyMode.Rigid);
            PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodySetTransform(floor, new(0, Vector2.One, 0, new(0, 80)));
            PhysicsServer.BodySetSpace(mover, space);
            PhysicsServer.BodySetSpace(floor, space);
            PhysicsServer.AreaSetSpace(sensor, space);
            Reject<ArgumentException>(() => PhysicsServer.BodyAddCollisionException(default, floor));
            Reject<ArgumentException>(() => PhysicsServer.BodyAddCollisionException(sensor, floor));
            PhysicsServer.BodyAddCollisionException(mover, sensor);
            PhysicsServer.BodyRemoveCollisionException(mover, sensor);
            PhysicsServer.BodyAddCollisionException(mover, floor);
            using var query = new PhysicsTestMotionParameters { Motion = new(0, 100) };
            using var reverseQuery = new PhysicsTestMotionParameters
            {
                From = new(0, Vector2.One, 0, new(0, 80)),
                Motion = new(0, -100)
            };
            Check(!PhysicsServer.BodyTestMotion(mover, query) &&
                  !PhysicsServer.BodyTestMotion(floor, reverseQuery),
                "An exception owned by one server body suppresses either direction of body motion.");
            PhysicsServer.BodySetLinearVelocity(mover, new(0, 80));
            for (var frame = 0; frame < 120; frame++) PhysicsServer.SpaceStep(space, 1d / 60);
            Check(PhysicsServer.BodyGetTransform(mover).Origin.Y > 100,
                "An explicit server world's regular solver lets the excepted body cross the floor.");
            PhysicsServer.BodyRemoveCollisionException(mover, floor);
            Check(PhysicsServer.BodyTestMotion(mover, query),
                "Removing the server exception restores motion contact.");
            PhysicsServer.BodySetTransform(mover, Transform.Identity);
            PhysicsServer.BodySetLinearVelocity(mover, new(0, 80));
            for (var frame = 0; frame < 120; frame++) PhysicsServer.SpaceStep(space, 1d / 60);
            Check(PhysicsServer.BodyGetTransform(mover).Origin.Y is > 45 and < 65,
                "Removing a server exception re-enables solver contact in the same space.");
            PhysicsServer.BodySetMaxContactsReported(mover, 2); using var state = PhysicsServer.BodyGetDirectState(mover)!;
            var cycle = 0;
            for (var i = 0; i < 96; i++) Cycle();
            var ownerBefore = GC.GetAllocatedBytesForCurrentThread(); var allBefore = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Cycle();
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBefore; var allBytes = GC.GetTotalAllocatedBytes(true) - allBefore;
            Check(ownerBytes == 0 && allBytes == 0, $"Warm changed raw exception frames allocated {ownerBytes}/{allBytes} owner/all-thread bytes.");
            void Cycle()
            {
                PhysicsServer.BodySetTransform(mover, new(0, new(0, 60))); PhysicsServer.BodySetLinearVelocity(mover, new(0, 80));
                var excluded = (cycle++ & 1) == 0;
                if (excluded) PhysicsServer.BodyAddCollisionException(mover, floor);
                else PhysicsServer.BodyRemoveCollisionException(mover, floor);
                PhysicsServer.SpaceStep(space, 1d / 60);
                Check(excluded ? state.GetContactCount() == 0 && state.LinearVelocity.Y > 40 : state.GetContactCount() > 0 && state.LinearVelocity.Y < 40,
                    "Every changed exception cycle alternates actual contact response and free motion.");
            }
        }
        finally
        {
            PhysicsServer.FreeRID(sensor); PhysicsServer.FreeRID(mover); PhysicsServer.FreeRID(floor);
            PhysicsServer.FreeRID(circle); PhysicsServer.FreeRID(rectangle); PhysicsServer.FreeRID(space);
        }
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
