using Electron2D;

internal static class RigidBodyContactTests
{
    internal static void Run()
    {
        VerifyContactSnapshotsAndSleep();
        VerifyMultipleShapesDeduplicate();
        VerifyMonitoringPolicyAndPacking();
        VerifyCallbackMutationAndFailure();
        Console.WriteLine("Rigid-body contact snapshots, events, sleep and allocation checks passed.");
    }

    private static void VerifyMultipleShapesDeduplicate()
    {
        using var floorGeometry = new RectangleShape { Size = new(200, 20) };
        using var circle = new CircleShape();
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorGeometry });
        var body = new RigidBody { ContactMonitor = true, MaxContactsReported = 4, LockRotation = true };
        var left = new CollisionShape { Name = "Left", Shape = circle, Position = new(-15, 0) };
        var right = new CollisionShape { Name = "Right", Shape = circle, Position = new(15, 0) };
        body.AddChild(left); body.AddChild(right);
        root.AddChild(floor); root.AddChild(body);
        var enters = 0; var exits = 0;
        body.BodyEntered += _ => enters++;
        body.BodyExited += _ => exits++;
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(enters == 1 && exits == 0 && body.GetCollidingBodies().Length == 1 &&
              body.GetContactCount() >= 2,
            "Two touching shape pairs count separately but report one body-level entry.");
        left.Disabled = true;
        tree.PhysicsFrame(1d / 60);
        Check(exits == 0 && body.GetCollidingBodies().Length == 1,
            "Removing one shape pair keeps the body-level contact while another pair remains.");
        right.Disabled = true;
        tree.PhysicsFrame(1d / 60);
        Check(exits == 1 && body.GetCollidingBodies().Length == 0,
            "Removing the final shape pair reports one body-level exit.");
        left.Disabled = false;
        for (var frame = 0; frame < 4; frame++) tree.PhysicsFrame(1d / 60);
        Check(enters == 2, "Restoring one shape reports one new body-level entry.");
    }

    private static void VerifyContactSnapshotsAndSleep()
    {
        using var floorGeometry = new RectangleShape { Size = new(200, 20) };
        using var bodyGeometry = new RectangleShape { Size = new(20, 20) };
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorGeometry });
        var body = new RigidBody { ContactMonitor = true, MaxContactsReported = 2 };
        body.AddChild(new CollisionShape { Shape = bodyGeometry });
        root.AddChild(floor); root.AddChild(body);
        var enters = 0; var exits = 0; var sleepEvents = 0;
        body.BodyEntered += other => { Check(ReferenceEquals(other, floor), "Contact enter identifies the other body."); enters++; };
        body.BodyExited += other => { Check(ReferenceEquals(other, floor), "Contact exit identifies the other body."); exits++; };
        body.SleepingStateChanged += changed => { Check(ReferenceEquals(changed, body), "Sleep event identifies its body."); sleepEvents++; };
        using var tree = new SceneTree(root);
        Check(body.GetContactCount() == 0 && body.GetCollidingBodies().Length == 0,
            "Contact state starts empty before the first fixed step.");
        for (var frame = 0; frame < 180; frame++) tree.PhysicsFrame(1d / 60);
        Check(enters == 1 && exits == 0 && body.GetContactCount() is >= 1 and <= 2 &&
              body.GetCollidingBodies().Length == 1 && ReferenceEquals(body.GetCollidingBodies()[0], floor),
            "A falling body reports a single object entry and capped contact points on a floor.");
        body.MaxContactsReported = 1;
        tree.PhysicsFrame(1d / 60);
        Check(body.GetContactCount() == 1 && enters == 1,
            "Reducing the point cap changes contact count without replaying the body entry.");
        body.MaxContactsReported = 2;
        tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed resting contact monitoring allocates no managed memory.");
        Check(body.Sleeping, "The resting body reached solver sleep before the wake check.");
        var beforeWake = sleepEvents;
        body.AddConstantCentralForce(new(0, -1));
        tree.PhysicsFrame(1d / 60);
        Check(!body.Sleeping && sleepEvents == beforeWake + 1,
            "A persistent force waking a sleeping body reports the solver state transition.");
        body.ConstantForce = Vector2.Zero;
        body.CanSleep = false;
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed active contact monitoring allocates no managed memory.");
        Check(sleepEvents >= 1, "Solver sleep transitions report at least one state change.");
        var previousSleepEvents = sleepEvents;
        body.Sleeping = false;
        Check(sleepEvents == previousSleepEvents,
            "An explicit Sleeping property assignment does not emit the solver event.");

        floor.CollisionLayer = 0;
        floor.CollisionMask = 0;
        body.Sleeping = false;
        tree.PhysicsFrame(1d / 60);
        Check(exits == 1 && body.GetCollidingBodies().Length == 0 && body.GetContactCount() == 0,
            "Rejecting both directions ends the body-level contact on the next fixed step.");
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed no-contact monitoring allocates no managed memory.");
        body.CanSleep = true;
        body.Sleeping = true;
        body.AddConstantCentralForce(new(1, 0));
        Check(!body.Sleeping, "A stored force wakes the live body immediately.");
        root.RemoveChild(body);
        Check(!body.Sleeping && body.GetContactCount() == 0,
            "Detaching before the next step retains the live wake state and clears contact data.");
        body.Dispose();
    }

    private static void VerifyMonitoringPolicyAndPacking()
    {
        using var floorGeometry = new RectangleShape { Size = new(200, 20) };
        using var bodyGeometry = new CircleShape();
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorGeometry });
        var body = new RigidBody();
        body.AddChild(new CollisionShape { Shape = bodyGeometry });
        root.AddChild(floor); root.AddChild(body);
        var enters = 0;
        body.BodyEntered += _ => enters++;
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GetContactCount() == 0 && enters == 0 && body.GetCollidingBodies().Length == 0,
            "Zero max contacts and disabled monitoring report no contacts by default.");
        Reject<ArgumentOutOfRangeException>(() => body.MaxContactsReported = -1);
        Check(body.MaxContactsReported == 0, "A negative contact cap rejects before mutation.");
        body.MaxContactsReported = 1;
        tree.PhysicsFrame(1d / 60);
        Check(body.GetContactCount() == 1 && enters == 0 && body.GetCollidingBodies().Length == 0,
            "Contact count can report points while object monitoring is disabled.");
        body.ContactMonitor = true;
        tree.PhysicsFrame(1d / 60);
        Check(enters == 1 && body.GetCollidingBodies().Length == 1,
            "Enabling monitoring while touching reports the existing body on the next step.");
        body.ContactMonitor = false;
        Check(body.GetCollidingBodies().Length == 0,
            "Disabling monitoring clears its body snapshot without changing the contact cap.");
        tree.PhysicsFrame(1d / 60);
        Check(body.GetContactCount() == 1 && enters == 1,
            "Contact count remains available while object monitoring is disabled.");
        Reject<InvalidOperationException>(() => Task.Run(body.GetContactCount).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => body.ContactMonitor = true).GetAwaiter().GetResult());
        Check(!body.ContactMonitor, "Off-thread monitoring edits preserve owner-thread state.");

        using var packedRoot = new Node { Name = "Root" };
        var packedBody = new RigidBody { Name = "Body", ContactMonitor = true, MaxContactsReported = 7 };
        packedRoot.AddChild(packedBody);
        packedBody.Owner = packedRoot;
        using var packed = new PackedScene(); packed.Pack(packedRoot);
        using var copy = packed.Instantiate();
        var restored = copy.GetNode<RigidBody>("Body");
        Check(restored.ContactMonitor && restored.MaxContactsReported == 7,
            "PackedScene restores monitoring and its point cap.");
    }

    private static void VerifyCallbackMutationAndFailure()
    {
        using var floorGeometry = new RectangleShape { Size = new(200, 20) };
        using var bodyGeometry = new CircleShape();
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorGeometry });
        var body = new RigidBody
        {
            Position = new(0, 80),
            GravityScale = 0,
            ContactMonitor = true,
            MaxContactsReported = 1
        };
        body.AddChild(new CollisionShape { Shape = bodyGeometry });
        root.AddChild(floor); root.AddChild(body);
        var enters = 0; var exits = 0;
        body.BodyEntered += _ =>
        {
            enters++;
            Reject<InvalidOperationException>(() => body.ContactMonitor = false);
            if (enters == 1) root.RemoveChild(floor);
        };
        body.BodyExited += _ => exits++;
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        Check(enters == 1 && exits == 1 && body.GetCollidingBodies().Length == 0,
            "A contact callback can remove the collider and receive one exit without stale state.");
        floor.Dispose();

        var secondFloor = new StaticBody { Name = "SecondFloor", Position = new(0, 100) };
        secondFloor.AddChild(new CollisionShape { Shape = floorGeometry });
        var thirdFloor = new StaticBody { Name = "ThirdFloor", Position = new(0, 100) };
        thirdFloor.AddChild(new CollisionShape { Shape = floorGeometry });
        root.AddChild(secondFloor);
        root.AddChild(thirdFloor);
        body.MaxContactsReported = 2;
        body.BodyEntered += _ => throw new InvalidOperationException("User callback failure.");
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        Check(enters == 3 && body.GetCollidingBodies().Length == 2,
            "Throwing callbacks leave both new contacts committed and continue later events.");
        tree.PhysicsFrame(1d / 60);
        Check(enters == 3, "Failed contact callbacks do not replay committed entries.");
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
