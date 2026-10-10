using Electron2D;

internal static class PhysicsMotionTests
{
    internal static void Run()
    {
        VerifyServerMotion();
        VerifySceneMotion();
        VerifyRecoveryAndOneWay();
        VerifyMarginPacking();
        VerifyShapeOwnerIndices();
        VerifyTangentialSlopeContact();
        VerifyCompoundRecovery();
        Console.WriteLine("Physics body motion sweep and typed server/scene result checks passed.");
    }

    private static void VerifyServerMotion()
    {
        var server = PhysicsServer.Service;
        var space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(space, true);
        var mover = PhysicsServer.BodyCreate();
        var obstacle = PhysicsServer.BodyCreate();
        var circleRID = PhysicsServer.CircleShapeCreate();
        var floorRID = PhysicsServer.RectangleShapeCreate();
        using var floor = new RectangleShape { Size = new(200, 20) };
        PhysicsServer.ShapeSetData(floorRID, floor);
        PhysicsServer.BodySetMode(mover, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodySetMode(obstacle, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodyAddShape(mover, circleRID);
        PhysicsServer.BodyAddShape(obstacle, floorRID);
        PhysicsServer.BodySetTransform(obstacle, new(0, Vector2.One, 0, new(0, 100)));
        PhysicsServer.BodySetSpace(mover, space);
        PhysicsServer.BodySetSpace(obstacle, space);
        using var query = new PhysicsTestMotionParameters { Motion = new(0, 120) };
        using var result = new PhysicsTestMotionResult();
        Check(query.From == Transform.Identity && query.Margin == 0.08f &&
              !query.RecoveryAsCollision && query.ExcludeBodies.Length == 0 &&
              query.ExcludeObjects.Length == 0,
            "Motion query parameters expose pinned defaults.");
        Reject<ArgumentOutOfRangeException>(() => query.Motion = new(float.NaN, 0));
        Reject<ArgumentOutOfRangeException>(() => query.Margin = -1);
        Reject<ArgumentException>(() => query.From = new(0, new(2, 1), 0, Vector2.Zero));
        Reject<ArgumentNullException>(() => query.ExcludeBodies = null!);
        Reject<ArgumentNullException>(() => query.ExcludeObjects = null!);
        Check(query.Motion == new Vector2(0, 120) && query.Margin == 0.08f &&
              query.From == Transform.Identity,
            "Invalid parameter writes leave prior query state intact.");
        Check(PhysicsServer.BodyTestMotion(mover, query, result) && result.GetColliderRID() == obstacle &&
              result.GetCollider() is null && result.GetCollisionLocalShape() == 0 &&
              result.GetColliderShape() == 0 && result.GetCollisionNormal().Y < -0.9f &&
              result.GetCollisionPoint().Y is > 88 and < 92 &&
              result.GetCollisionSafeFraction() is > 0.6f and < 0.75f &&
              result.GetCollisionUnsafeFraction() > result.GetCollisionSafeFraction() &&
              result.GetTravel().Y is > 70 and < 90 && result.GetRemainder().Y is > 30 and < 50,
            "A server body sweep stops before a server-only floor and records both shape indices.");
        query.ExcludeBodies = [obstacle];
        var copied = query.ExcludeBodies;
        copied[0] = default;
        Check(query.ExcludeBodies[0] == obstacle,
            "RID exclusions are copied on read and assignment.");
        var excludedHit = PhysicsServer.BodyTestMotion(mover, query, result);
        Check(!excludedHit && result.GetColliderRID() == default &&
              MathF.Abs(result.GetTravel().Y - query.Motion.Y) < 0.001f &&
              result.GetRemainder() == Vector2.Zero,
            $"Excluding the obstacle permits full motion and clears the result: {excludedHit}, {result.GetColliderRID()}, {result.GetTravel()}, {result.GetRemainder()}.");
        PhysicsServer.FreeRID(mover); PhysicsServer.FreeRID(obstacle); PhysicsServer.FreeRID(circleRID);
        PhysicsServer.FreeRID(floorRID); PhysicsServer.FreeRID(space);
    }

    private static void VerifySceneMotion()
    {
        using var probe = new CircleShape();
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        var root = new Node();
        var mover = new StaticBody { Name = "Mover" };
        var localShape = new CollisionShape { Shape = probe };
        mover.AddChild(localShape);
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100) };
        var floorCollision = new CollisionShape { Shape = floorShape };
        floor.AddChild(floorCollision);
        root.AddChild(mover); root.AddChild(floor);
        using var tree = new SceneTree(root);
        using var tested = new KinematicCollision();
        Check(mover.TestMove(Transform.Identity, new(0, 120), tested) &&
              mover.Position == Vector2.Zero && tested.GetColliderRID() == floor.GetRID() &&
              ReferenceEquals(tested.GetCollider(), floor) &&
              ReferenceEquals(tested.GetLocalShape(), localShape) &&
              ReferenceEquals(tested.GetColliderShape(), floorCollision) &&
              tested.GetColliderShapeIndex() == 0 && tested.GetNormal().Y < -0.9f &&
              tested.GetAngle() < 0.1f,
            "TestMove reports scene shape owners and contact data without changing the scene pose.");
        Reject<ArgumentOutOfRangeException>(() => mover.MoveAndCollide(new(float.NaN, 0)));
        Reject<ArgumentOutOfRangeException>(() => mover.TestMove(Transform.Identity, new(0, 120), tested, -1));
        Check(mover.Position == Vector2.Zero && tested.GetColliderRID() == floor.GetRID(),
            "Invalid motion or margin leaves both body pose and earlier collision result unchanged.");
        using var wouldHit = mover.MoveAndCollide(new(0, 120), testOnly: true);
        Check(wouldHit is not null && mover.Position == Vector2.Zero &&
              wouldHit.GetTravel().Y is > 70 and < 90,
            "MoveAndCollide test-only mode returns a collision without moving the body.");
        using var hit = mover.MoveAndCollide(new(0, 120));
        Check(hit is not null && mover.Position.Y is > 70 and < 90 &&
              hit.GetRemainder().Y is > 30 and < 50 && hit.GetColliderID() == floor.InstanceID,
            "MoveAndCollide advances to its safe travel and returns the remaining displacement.");
        Check(mover.TestMove(Transform.Identity, new(0, 120), tested) &&
              mover.Position.Y is > 70 and < 90,
            "TestMove uses its supplied global origin without teleporting the current body.");
        var embedded = new Transform(0, Vector2.One, 0, new(0, 100));
        Check(mover.TestMove(embedded, new(0, 50), tested) &&
              tested.GetTravel().Y < 50,
            "An embedded body cannot traverse a solid obstacle after recovery attempts.");
        floor.CollisionLayer = 2;
        Check(!mover.TestMove(Transform.Identity, new(0, 120), tested) &&
              !tested.GetColliderRID().IsValid(),
            "Body motion obeys the moving mask against target layers and clears a stale result on a miss.");
        floor.CollisionLayer = 1;
        using var serverQuery = new PhysicsTestMotionParameters
        {
            Motion = new(0, 120)
        };
        using var serverResult = new PhysicsTestMotionResult();
        Check(PhysicsServer.BodyTestMotion(mover.GetRID(), serverQuery, serverResult) &&
              ReferenceEquals(serverResult.GetCollider(), floor) &&
              serverResult.GetColliderID() == floor.InstanceID &&
              serverResult.GetColliderRID() == floor.GetRID(),
            "A server motion query of a scene body retains both scene object and RID identity.");
        serverQuery.ExcludeObjects = [floor.InstanceID];
        var objectCopy = serverQuery.ExcludeObjects;
        objectCopy[0] = 0;
        Check(serverQuery.ExcludeObjects[0] == floor.InstanceID,
            "Instance-ID exclusions are copied on read and assignment.");
        Check(!PhysicsServer.BodyTestMotion(mover.GetRID(), serverQuery, serverResult) &&
              !serverResult.GetColliderRID().IsValid(),
            "Server motion excludes a scene collider by its managed instance ID.");
        for (var frame = 0; frame < 64; frame++) mover.TestMove(Transform.Identity, new(0, 120), tested);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) mover.TestMove(Transform.Identity, new(0, 120), tested);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0,
            $"Warmed unchanged scene body motion tests allocate no managed bytes on the owner thread: {allocated}.");
        localShape.Disabled = true;
        Check(!mover.TestMove(Transform.Identity, new(0, 120), tested),
            "A body without active shape fixtures traverses freely.");
        Reject<InvalidOperationException>(() => Task.Run(() => mover.TestMove(Transform.Identity, new(0, 120))).GetAwaiter().GetResult());
        root.RemoveChild(mover);
        Check(!mover.TestMove(Transform.Identity, new(0, 120)),
            "A detached body has no world to test and does not move.");
        Reject<InvalidOperationException>(() => mover.MoveAndCollide(new(0, 120)));
    }

    private static void VerifyRecoveryAndOneWay()
    {
        using var probe = new CircleShape();
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        var root = new Node();
        var mover = new StaticBody { Name = "MovingBody" };
        mover.AddChild(new CollisionShape { Shape = probe });
        var floor = new StaticBody { Name = "OneWayFloor", Position = new(0, 100) };
        var surface = new CollisionShape { Shape = floorShape, OneWayCollision = true };
        floor.AddChild(surface);
        root.AddChild(mover); root.AddChild(floor);
        using var tree = new SceneTree(root);
        using var result = new KinematicCollision();
        var fromAbove = new Transform(0, Vector2.One, 0, new(0, 50));
        Check(mover.TestMove(fromAbove, new(0, 80), result) &&
              result.GetColliderRID() == floor.GetRID() && result.GetNormal().Y < -0.9f,
            "A one-way floor stops a body approaching its solid side.");
        var fromBelow = new Transform(0, Vector2.One, 0, new(0, 130));
        Check(!mover.TestMove(fromBelow, new(0, -80), result) &&
              !result.GetColliderRID().IsValid(),
            "A one-way floor lets an upward body pass through its underside.");
        var overlappingTop = new Transform(0, Vector2.One, 0, new(0, 82));
        Check(!mover.TestMove(overlappingTop, Vector2.Zero, result, recoveryAsCollision: true),
            "An initial overlap deeper than the default one-way margin is omitted from recovery.");
        surface.OneWayCollisionMargin = 3;
        Check(mover.TestMove(overlappingTop, Vector2.Zero, result, recoveryAsCollision: true) &&
              result.GetTravel().Y < 0 && result.GetColliderRID() == floor.GetRID(),
            "A wider one-way margin permits shallow recovery and reports it as a collision.");
        Reject<ArgumentOutOfRangeException>(() => surface.OneWayCollisionMargin = -1);
        Check(surface.OneWayCollisionMargin == 3, "Invalid one-way margins leave the accepted value intact.");
        surface.Disabled = true;
        var polygonSurface = new CollisionPolygon
        {
            Polygon = [new(-100, -10), new(100, -10), new(100, 10), new(-100, 10)],
            OneWayCollision = true,
            OneWayCollisionMargin = 3
        };
        floor.AddChild(polygonSurface);
        Check(mover.TestMove(overlappingTop, Vector2.Zero, result, recoveryAsCollision: true) &&
              result.GetColliderRID() == floor.GetRID(),
            "An owned collision polygon supplies the same one-way recovery margin.");
    }

    private static void VerifyMarginPacking()
    {
        var root = new Node();
        var shape = new CollisionShape { Name = "Shape", OneWayCollisionMargin = 2.5f };
        var polygon = new CollisionPolygon { Name = "Polygon", OneWayCollisionMargin = 3.5f };
        root.AddChild(shape); root.AddChild(polygon);
        shape.Owner = root; polygon.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root);
        using var copy = packed.Instantiate();
        Check(copy.GetNode<CollisionShape>("Shape").OneWayCollisionMargin == 2.5f &&
              copy.GetNode<CollisionPolygon>("Polygon").OneWayCollisionMargin == 3.5f,
            "Borrowed and owned one-way geometry keep their margin through PackedScene.");
    }

    private static void VerifyShapeOwnerIndices()
    {
        using var probe = new CircleShape();
        using var floorShape = new RectangleShape { Size = new(50, 10) };
        var root = new Node();
        var mover = new StaticBody { Name = "Mover" };
        mover.AddChild(new CollisionShape { Name = "FarProbe", Shape = probe, Position = new(-100, 0) });
        var nearProbe = new CollisionShape { Name = "NearProbe", Shape = probe };
        mover.AddChild(nearProbe);
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Name = "FarFloor", Shape = floorShape, Position = new(100, 0) });
        var nearFloor = new CollisionShape { Name = "NearFloor", Shape = floorShape };
        floor.AddChild(nearFloor);
        root.AddChild(mover); root.AddChild(floor);
        using var tree = new SceneTree(root);
        using var hit = mover.MoveAndCollide(new(0, 130), testOnly: true);
        Check(hit is not null && hit.GetColliderShapeIndex() == 1 &&
              ReferenceEquals(hit.GetLocalShape(), nearProbe) &&
              ReferenceEquals(hit.GetColliderShape(), nearFloor),
            "Motion results retain direct shape-owner indices across unrelated fixture slots.");
    }

    private static void VerifyTangentialSlopeContact()
    {
        using var circle = new CircleShape();
        using var slope = new SegmentShape { A = new(-100, 20), B = new(100, -20) };
        var root = new Node();
        var mover = new StaticBody { Name = "Mover", Position = new(-80, 106) };
        mover.AddChild(new CollisionShape { Shape = circle });
        var floor = new StaticBody { Name = "Slope", Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = slope });
        root.AddChild(mover); root.AddChild(floor);
        using var tree = new SceneTree(root);
        using var hit = mover.MoveAndCollide(new(100, 0), testOnly: true);
        Check(hit is not null && hit.GetNormal().X < -0.1f &&
              hit.GetNormal().Y < -0.9f && hit.GetTravel().X < 5,
            "A body touching an ascending segment reports an inward tangential sweep instead of tunneling through it.");
    }

    private static void VerifyCompoundRecovery()
    {
        var points = new Vector2[12]; for (var i = 0; i < points.Length; i++) points[i] = new Vector2(20, 0).Rotated(-i * Mathf.Tau / points.Length);
        using var polygon = new ConvexPolygonShape { Points = points }; using var floorShape = new RectangleShape { Size = new(200, 20) };
        using var circle = new CircleShape { Radius = 5 };
        var space = PhysicsServer.SpaceCreate(); var mover = PhysicsServer.BodyCreate(); var obstacle = PhysicsServer.BodyCreate();
        using var query = new PhysicsTestMotionParameters { From = new(0, new(0, 95)), Motion = new(0, 120), RecoveryAsCollision = true };
        using var result = new PhysicsTestMotionResult();
        try
        {
            PhysicsServer.BodySetMode(mover, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(mover, polygon.GetRID()); PhysicsServer.BodySetSpace(mover, space);
            PhysicsServer.BodySetMode(obstacle, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(obstacle, floorShape.GetRID()); PhysicsServer.BodySetTransform(obstacle, new(0, new(0, 100))); PhysicsServer.BodySetSpace(obstacle, space);
            Check(PhysicsServer.BodyTestMotion(mover, query, result) && result.GetTravel().Y < -18 && result.GetCollisionNormal().Y < -.9f,
                "A whole convex contour recovers outward instead of oscillating between its internal pieces.");
            for (var i = 0; i < 64; i++) PhysicsServer.BodyTestMotion(mover, query, result);
            var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) PhysicsServer.BodyTestMotion(mover, query, result);
            Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Warmed full-contour recovery reuses collision scratch.");
            PhysicsServer.BodySetShape(mover, 0, circle.GetRID()); PhysicsServer.BodySetShape(obstacle, 0, polygon.GetRID()); PhysicsServer.BodySetTransform(obstacle, Transform.Identity);
            query.From = new(0, new(0, -5)); query.Motion = Vector2.Zero;
            Check(PhysicsServer.BodyTestMotion(mover, query, result) && result.GetTravel().Length() > 10 && result.GetCollisionNormal().Y < -.7f,
                "A compound target also uses its outer contour for recovery.");
            using var ray = new SeparationRayShape { Length = 70, SlideOnSlope = true };
            query.From = new(0, new(8, -3)); query.CollideSeparationRay = true;
            foreach (var reverse in new[] { false, true })
            {
                PhysicsServer.BodySetShape(mover, 0, (reverse ? (Shape)polygon : ray).GetRID());
                PhysicsServer.BodySetShape(obstacle, 0, (reverse ? (Shape)ray : polygon).GetRID());
                foreach (var motion in new[] { Vector2.Zero, new Vector2(0, 5) })
                {
                    query.Motion = motion;
                    var collided = PhysicsServer.BodyTestMotion(mover, query, result);
                    Check(!collided && result.GetTravel().DistanceTo(motion) < .00001f,
                        $"A ray origin inside a compound query or target rejects every internal seam, including the swept region: reverse={reverse}, motion={motion}, hit={collided}, travel={result.GetTravel()}.");
                }
            }

        }
        finally { PhysicsServer.FreeRID(mover); PhysicsServer.FreeRID(obstacle); PhysicsServer.FreeRID(space); }
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
