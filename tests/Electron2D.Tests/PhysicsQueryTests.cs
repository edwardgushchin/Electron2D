using Electron2D;

internal static class PhysicsQueryTests
{
    internal static void Run()
    {
        VerifyParameters();
        VerifySceneRayAndRID();
        VerifyServerResourcesAndPointQueries();
        VerifyServerShapeFamilies();
        Console.WriteLine("Physics RID, scene/server ray and point query checks passed.");
    }

    private static void VerifyParameters()
    {
        using var ray = new PhysicsRayQueryParameters2D();
        Check(ray.From == Vector2.Zero && ray.To == Vector2.Zero &&
              ray.CollisionMask == uint.MaxValue && ray.Exclude.Length == 0 &&
              ray.CollideWithBodies && !ray.CollideWithAreas && !ray.HitFromInside,
            "Ray parameters expose their complete default query policy.");
        var savedRID = RID.Allocate();
        var excluded = new[] { savedRID };
        ray.Exclude = excluded;
        excluded[0] = RID.Allocate();
        var read = ray.Exclude;
        read[0] = RID.Allocate();
        Check(ray.Exclude is [var held] && held == savedRID,
            "Ray exclusions have independent setter and getter arrays.");
        Reject<ArgumentOutOfRangeException>(() => ray.From = new(float.NaN, 0));
        Reject<ArgumentOutOfRangeException>(() => ray.To = new(float.PositiveInfinity, 0));
        Reject<ArgumentNullException>(() => ray.Exclude = null!);
        Check(ray.From == Vector2.Zero && ray.To == Vector2.Zero && ray.Exclude.Length == 1,
            "Invalid ray edits reject before mutation.");
        using var point = new PhysicsPointQueryParameters2D();
        Check(point.Position == Vector2.Zero && point.CollisionMask == uint.MaxValue &&
              point.Exclude.Length == 0 && point.CollideWithBodies && !point.CollideWithAreas,
            "Point parameters expose their complete defaults.");
        Reject<ArgumentOutOfRangeException>(() => point.Position = new(float.NaN, 0));
        Reject<ArgumentNullException>(() => point.Exclude = null!);
        Check(point.Position == Vector2.Zero && point.Exclude.Length == 0,
            "Invalid point edits reject before mutation.");
    }

    private static void VerifySceneRayAndRID()
    {
        var empty = new RID();
        Check(!empty.IsValid() && empty.GetID() == 0 && empty == default,
            "An empty RID has identity zero.");
        using var floorShape = new RectangleShape { Size = new(100, 20) };
        using var sensorShape = new RectangleShape { Size = new(100, 20) };
        var root = new Node();
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100), CollisionMask = 0 };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        var area = new Area { Name = "Area", Position = new(0, 50) };
        area.AddChild(new CollisionShape { Shape = sensorShape });
        root.AddChild(floor); root.AddChild(area);
        Check(floor.GetWorld2D() is null, "A detached canvas item has no physics world.");
        var bodyRID = floor.GetRID();
        Check(bodyRID.IsValid() && new RID(bodyRID) == bodyRID && bodyRID > empty &&
              bodyRID.GetID() == new RID(bodyRID).GetID(),
            "A scene collision object has a copyable, ordered, nonempty RID.");
        using var tree = new SceneTree(root);
        var world = floor.GetWorld2D() ?? throw new InvalidOperationException("Attached body has no world.");
        Check(ReferenceEquals(world, area.GetWorld2D()) && world.Space.IsValid(),
            "Canvas items in one tree share a physics world and space RID.");
        var direct = world.DirectSpaceState;
        Check(ReferenceEquals(direct, PhysicsServer.SpaceGetDirectState(world.Space)),
            "World and server access return the same live direct-space view.");
        Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(bodyRID));
        Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(world.Space));

        using var ray = PhysicsRayQueryParameters2D.Create(new(0, 0), new(0, 200));
        var hit = direct.IntersectRay(ray);
        Check(hit is { } result && result.ColliderRID == bodyRID && ReferenceEquals(result.Collider, floor) &&
              result.ColliderID == floor.InstanceID && result.ShapeIndex == 0 &&
              result.Position.Y is > 89 and < 91 && result.Normal.Y < -0.9f,
            "A scene ray hits the floor despite its collision mask being zero.");
        ray.CollideWithAreas = true;
        hit = direct.IntersectRay(ray);
        Check(hit is { } areaHit && areaHit.ColliderRID == area.GetRID() && areaHit.Position.Y is > 39 and < 41,
            "Enabling Area hits selects the nearest sensor before the body.");
        ray.Exclude = [area.GetRID()];
        hit = direct.IntersectRay(ray);
        Check(hit is { } excludedHit && excludedHit.ColliderRID == bodyRID,
            "RID exclusion skips the Area and still reports the body.");
        var copiedExclusions = ray.Exclude;
        copiedExclusions[0] = bodyRID;
        Check(ray.Exclude[0] == area.GetRID(), "Excluded RID arrays have independent read copies.");
        ray.CollisionMask = 2;
        Check(direct.IntersectRay(ray) is null, "A mismatched query layer finds no hit.");
        ray.CollisionMask = uint.MaxValue;
        ray.From = new(-float.MaxValue, 0);
        ray.To = new(float.MaxValue, 0);
        Reject<ArgumentOutOfRangeException>(() => direct.IntersectRay(ray));
        ray.From = new(0, 100);
        ray.To = new(0, 200);
        ray.CollideWithAreas = false;
        ray.Exclude = [];
        Check(direct.IntersectRay(ray) is null, "A ray starting inside a body skips it by default.");
        ray.HitFromInside = true;
        hit = direct.IntersectRay(ray);
        Check(hit is { } insideHit && insideHit.ColliderRID == bodyRID &&
              insideHit.Position == new Vector2(0, 100) && insideHit.Normal == Vector2.Zero,
            "HitFromInside reports the origin with a zero normal.");

        floorShape.Size = new(120, 20);
        tree.PhysicsFrame(1d / 60);
        Check(floor.GetRID() == bodyRID && direct.IntersectRay(ray)?.ShapeIndex == 0,
            "A live fixture rebuild preserves the collider RID and shape-owner index.");
        using var point = new PhysicsPointQueryParameters2D { Position = new(0, 100) };
        Check(direct.IntersectPoint(point).Any(candidate => candidate.ColliderRID == bodyRID),
            "A point query detects a scene body even when its collision mask is zero.");

        var server = PhysicsServer.Service;
        var serverBody = PhysicsServer.BodyCreate();
        var serverShape = PhysicsServer.CircleShapeCreate();
        PhysicsServer.BodySetMode(serverBody, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodyAddShape(serverBody, serverShape);
        PhysicsServer.BodySetTransform(serverBody, new(0, Vector2.One, 0, new(50, 50)));
        PhysicsServer.BodySetSpace(serverBody, world.Space);
        point.Position = new(50, 50);
        Check(direct.IntersectPoint(point) is [var serverHit] &&
              serverHit.ColliderRID == serverBody && serverHit.Collider is null,
            "A server-created collider shares the SceneTree solver space and typed query view.");
        PhysicsServer.FreeRID(serverBody);
        PhysicsServer.FreeRID(serverShape);
        Reject<InvalidOperationException>(() => Task.Run(() => direct.IntersectRay(ray)).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => PhysicsServer.Service.Dispose());
        direct.Dispose();
        var reopened = world.DirectSpaceState;
        Check(!ReferenceEquals(direct, reopened) && reopened.IntersectRay(ray)?.ColliderRID == bodyRID,
            "A disposed direct view is recreated for the same live space.");
        var sceneSpaceRID = world.Space;
        world.Dispose();
        var reopenedWorld = floor.GetWorld2D();
        Check(reopenedWorld is not null && !ReferenceEquals(world, reopenedWorld) &&
              reopenedWorld.Space == sceneSpaceRID,
            "A disposed World2D wrapper is recreated without replacing the SceneTree space.");
        tree.Dispose();
        Check(bodyRID.IsValid(), "An externally held RID remains nonzero after its object is freed.");
        Reject<ObjectDisposedException>(() => floor.GetRID());
        Reject<ArgumentException>(() => reopened.IntersectRay(ray));
    }

    private static void VerifyServerResourcesAndPointQueries()
    {
        var server = PhysicsServer.Service;
        var space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(space, true);
        var body = PhysicsServer.BodyCreate();
        var shape = PhysicsServer.CircleShapeCreate();
        using var circle = new CircleShape();
        PhysicsServer.ShapeSetData(shape, circle);
        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodyAddShape(body, shape);
        PhysicsServer.BodySetTransform(body, new(0, Vector2.One, 0, new(0, 50)));
        PhysicsServer.BodySetSpace(body, space);
        Check(PhysicsServer.BodyGetSpace(body) == space && PhysicsServer.BodyGetMode(body) == PhysicsServer.BodyMode.Static,
            "A server-created body is attached to its explicit space with its requested mode.");
        using var ray = PhysicsRayQueryParameters2D.Create(new(0, 0), new(0, 100));
        var direct = PhysicsServer.SpaceGetDirectState(space);
        var hit = direct.IntersectRay(ray);
        Check(hit is { } bodyHit && bodyHit.ColliderRID == body && bodyHit.Collider is null &&
              bodyHit.ColliderID == 0 && bodyHit.ShapeIndex == 0 && bodyHit.Position.Y is > 39 and < 41,
            "A server-only shape is queried in the same world with its RID and stable index.");
        using var point = new PhysicsPointQueryParameters2D { Position = new(0, 50) };
        var pointHits = direct.IntersectPoint(point);
        Check(pointHits.Length == 1 && pointHits[0].ColliderRID == body && pointHits[0].Collider is null,
            "Point queries return a typed result for server-only filled geometry.");
        PhysicsServer.BodyAddShape(body, shape, disabled: true);
        Check(PhysicsServer.BodyGetShapeCount(body) == 2 && direct.IntersectPoint(point).Length == 1,
            "A disabled body slot is indexed but contributes no fixture.");
        PhysicsServer.BodySetShapeDisabled(body, 1, false);
        pointHits = direct.IntersectPoint(point);
        Check(pointHits.Length == 2 && pointHits[0].ShapeIndex == 0 && pointHits[1].ShapeIndex == 1,
            "Enabling the slot creates a second stable query shape index.");
        PhysicsServer.BodySetShapeDisabled(body, 1, true);
        PhysicsServer.BodyRemoveShape(body, 1);
        Check(PhysicsServer.BodyGetShapeCount(body) == 1 && direct.IntersectPoint(point).Length == 1,
            "Removing the slot releases its fixture without changing the first slot.");
        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetShapeDisabled(body, 9, true));
        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodyRemoveShape(body, 9));

        var area = PhysicsServer.AreaCreate();
        var areaShape = PhysicsServer.RectangleShapeCreate();
        using var rectangle = new RectangleShape { Size = new(40, 10) };
        PhysicsServer.ShapeSetData(areaShape, rectangle);
        PhysicsServer.AreaAddShape(area, areaShape, disabled: true);
        PhysicsServer.AreaSetTransform(area, new(0, Vector2.One, 0, new(0, 25)));
        PhysicsServer.AreaSetSpace(area, space);
        Check(PhysicsServer.AreaGetSpace(area) == space && PhysicsServer.AreaGetShapeCount(area) == 1,
            "A server-only Area joins the same live solver space with its disabled slot.");
        ray.CollideWithAreas = true;
        Check(direct.IntersectRay(ray)?.ColliderRID == body,
            "A disabled server Area sensor cannot answer rays.");
        PhysicsServer.AreaSetShapeDisabled(area, 0, false);
        hit = direct.IntersectRay(ray);
        Check(hit is { } areaHit && areaHit.ColliderRID == area && areaHit.Position.Y is > 19 and < 21,
            "Enabling Area rays selects the closer server-only sensor.");
        point.CollideWithAreas = true;
        point.Position = new(0, 25);
        pointHits = direct.IntersectPoint(point);
        Check(pointHits.Length == 1 && pointHits[0].ColliderRID == area,
            "Point queries include server-only Areas only when enabled.");
        PhysicsServer.AreaSetCollisionLayer(area, 2);
        point.CollisionMask = 1;
        Check(direct.IntersectPoint(point).Length == 0,
            "A point query checks the Area layer rather than its collision mask.");
        point.CollisionMask = 2;
        Check(direct.IntersectPoint(point) is [var filtered] && filtered.ColliderRID == area,
            "A matching layer restores the Area point result.");
        point.CollisionMask = uint.MaxValue;

        var secondBody = PhysicsServer.BodyCreate();
        PhysicsServer.BodySetMode(secondBody, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodyAddShape(secondBody, shape);
        PhysicsServer.BodySetTransform(secondBody, new(0, Vector2.One, 0, new(0, 50)));
        PhysicsServer.BodySetSpace(secondBody, space);
        point.Position = new(0, 50);
        pointHits = direct.IntersectPoint(point, 1);
        Check(pointHits.Length == 1 && pointHits[0].ColliderRID == body,
            "Result caps apply after deterministic RID ordering.");
        pointHits = direct.IntersectPoint(point, 2);
        Check(pointHits.Length == 2 && pointHits[0].ColliderRID == body && pointHits[1].ColliderRID == secondBody,
            "Two overlapping owners are ordered by stable RID before capping.");
        point.Exclude = [body];
        Check(direct.IntersectPoint(point) is [var secondHit] && secondHit.ColliderRID == secondBody,
            "RID exclusions also apply to point queries.");
        point.Exclude = [body, secondBody];
        Check(direct.IntersectPoint(point).Length == 0, "All excluded owners disappear from point results.");
        Reject<ArgumentOutOfRangeException>(() => direct.IntersectPoint(point, -1));
        Check(direct.IntersectPoint(point, 0).Length == 0, "A zero maximum returns no point results.");
        point.Exclude = [];

        circle.Radius = 20;
        Check(direct.IntersectRay(new PhysicsRayQueryParameters2D { From = new(0, 0), To = new(0, 100) })?.Position.Y is > 39 and < 41,
            "Caller resource edits do not alter server-owned shape data.");
        PhysicsServer.ShapeSetData(shape, circle);
        ray.CollideWithAreas = false;
        hit = direct.IntersectRay(ray);
        Check(hit is { } resizedHit && resizedHit.Position.Y is > 29 and < 31 && resizedHit.ShapeIndex == 0,
            "Replacing shared server shape data rebuilds referenced fixtures with stable owner indices.");
        using var copy = PhysicsServer.ShapeGetData(shape);
        Check(copy is CircleShape copied && copied.Radius == 20 && !ReferenceEquals(copy, circle),
            "ShapeGetData returns an independent typed resource copy.");
        using var replacement = new CircleShape { Radius = 15 };
        Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.ShapeSetData(shape, replacement)).GetAwaiter().GetResult());
        using var retained = PhysicsServer.ShapeGetData(shape);
        Check(retained is CircleShape retainedCircle && retainedCircle.Radius == 20,
            "An off-owner attached shape update rejects without changing server geometry.");
        Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.BodyGetSpace(body)).GetAwaiter().GetResult());
        Reject<ArgumentException>(() => PhysicsServer.ShapeSetData(shape, rectangle));
        Reject<ArgumentException>(() => PhysicsServer.BodySetSpace(shape, space));
        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetMode(body, (PhysicsServer.BodyMode)99));
        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.SpaceStep(space, -1));

        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid);
        PhysicsServer.SpaceStep(space, 1d / 60);
        var movedPosition = PhysicsServer.BodyGetTransform(body).Origin.Y;
        Check(movedPosition > 50,
            "Explicit server spaces advance a dynamic body through the real solver.");
        var transientSpace = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(transientSpace, true);
        PhysicsServer.BodySetSpace(body, transientSpace);
        Check(MathF.Abs(PhysicsServer.BodyGetTransform(body).Origin.Y - movedPosition) < 0.001f &&
              PhysicsServer.SpaceGetDirectState(transientSpace).IntersectRay(ray)?.ColliderRID == body,
            "A dynamic body carries its solved pose and geometry into another explicit space.");
        PhysicsServer.BodySetSpace(body, space);
        PhysicsServer.FreeRID(transientSpace);
        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
        Check(MathF.Abs(PhysicsServer.BodyGetTransform(body).Origin.Y - movedPosition) < 0.001f,
            "Switching to static mode preserves the solved pose.");
        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Kinematic);
        PhysicsServer.SpaceStep(space, 1d / 60);
        Check(MathF.Abs(PhysicsServer.BodyGetTransform(body).Origin.Y - movedPosition) < 0.001f,
            "Entering kinematic mode clears earlier dynamic velocity.");
        PhysicsServer.BodySetLinearVelocity(body, new(0, 60));
        PhysicsServer.SpaceStep(space, 1d / 60);
        var kinematicPosition = PhysicsServer.BodyGetTransform(body).Origin.Y;
        Check(kinematicPosition > movedPosition + 0.5f,
            "A server kinematic body advances by its velocity and reports the solved pose.");
        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.RigidLinear);
        Check(PhysicsServer.BodyGetMode(body) == PhysicsServer.BodyMode.RigidLinear,
            "RigidLinear remains an executable distinct mode with rotation locking.");
        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
        var otherSpace = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(otherSpace, true);
        PhysicsServer.BodySetSpace(body, otherSpace);
        Check(PhysicsServer.BodyGetSpace(body) == otherSpace &&
              PhysicsServer.SpaceGetDirectState(otherSpace).IntersectRay(ray)?.ColliderRID == body,
            "A server body moves between spaces without losing its shape or pose.");
        PhysicsServer.BodySetSpace(body, space);
        PhysicsServer.FreeRID(otherSpace);
        PhysicsServer.FreeRID(shape);
        Check(direct.IntersectRay(ray) is null, "Freeing a shape removes geometry from its users.");
        Check(shape.IsValid(), "Freeing a resource does not turn its copied RID value into zero.");
        Reject<ArgumentException>(() => PhysicsServer.ShapeGetData(shape));
        var newer = PhysicsServer.CircleShapeCreate();
        Check(newer > shape, "A freed RID is never reassigned to a later shape.");
        PhysicsServer.FreeRID(newer);
        PhysicsServer.FreeRID(space);
        Check(!PhysicsServer.BodyGetSpace(body).IsValid() && !PhysicsServer.AreaGetSpace(area).IsValid(),
            "Freeing a space detaches its server-created colliders.");
        Reject<ArgumentException>(() => direct.IntersectRay(ray));
        PhysicsServer.FreeRID(body);
        PhysicsServer.FreeRID(secondBody);
        PhysicsServer.AreaRemoveShape(area, 0);
        Check(PhysicsServer.AreaGetShapeCount(area) == 0, "AreaRemoveShape clears its indexed sensor slot.");
        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetShapeDisabled(area, 0, true));
        PhysicsServer.FreeRID(areaShape);
        PhysicsServer.FreeRID(area);
    }

    private static void VerifyServerShapeFamilies()
    {
        var server = PhysicsServer.Service;
        var space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(space, true);
        var body = PhysicsServer.BodyCreate();
        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
        var capsuleRID = PhysicsServer.CapsuleShapeCreate();
        var segmentRID = PhysicsServer.SegmentShapeCreate();
        var convexRID = PhysicsServer.ConvexPolygonShapeCreate();
        var concaveRID = PhysicsServer.ConcavePolygonShapeCreate();
        using var capsule = new CapsuleShape();
        using var segment = new SegmentShape { A = new(0, -10), B = new(0, 10) };
        var vertices = new Vector2[12];
        for (var index = 0; index < vertices.Length; index++)
        {
            var angle = index * Mathf.Tau / vertices.Length;
            vertices[index] = new(MathF.Cos(angle) * 20, MathF.Sin(angle) * 20);
        }
        using var convex = new ConvexPolygonShape { Points = vertices };
        using var concave = new ConcavePolygonShape { Segments = [new(-10, 0), new(10, 0)] };
        PhysicsServer.ShapeSetData(capsuleRID, capsule);
        PhysicsServer.ShapeSetData(segmentRID, segment);
        PhysicsServer.ShapeSetData(convexRID, convex);
        PhysicsServer.ShapeSetData(concaveRID, concave);
        PhysicsServer.BodyAddShape(body, capsuleRID);
        PhysicsServer.BodyAddShape(body, segmentRID, new(0, Vector2.One, 0, new(100, 0)));
        PhysicsServer.BodyAddShape(body, convexRID, new(0, Vector2.One, 0, new(200, 0)));
        PhysicsServer.BodyAddShape(body, concaveRID, new(0, Vector2.One, 0, new(300, 0)));
        PhysicsServer.BodySetSpace(body, space);
        var direct = PhysicsServer.SpaceGetDirectState(space);
        using var ray = PhysicsRayQueryParameters2D.Create(new(0, -50), new(0, 50));
        Check(direct.IntersectRay(ray)?.ShapeIndex == 0, "A server capsule fixture answers a ray query.");
        ray.From = new(50, 0); ray.To = new(150, 0);
        Check(direct.IntersectRay(ray)?.ShapeIndex == 1, "A server segment fixture answers a ray query.");
        using var point = new PhysicsPointQueryParameters2D { Position = new(200, 0) };
        Check(direct.IntersectPoint(point) is [var convexHit] && convexHit.ShapeIndex == 2,
            "Compound convex server fixtures keep one stable public shape-owner index.");
        ray.From = new(300, -50); ray.To = new(300, 50);
        Check(direct.IntersectRay(ray)?.ShapeIndex == 3,
            "A server concave pair answers a ray query without a solid interior.");
        PhysicsServer.FreeRID(body);
        PhysicsServer.FreeRID(capsuleRID);
        PhysicsServer.FreeRID(segmentRID);
        PhysicsServer.FreeRID(convexRID);
        PhysicsServer.FreeRID(concaveRID);
        PhysicsServer.FreeRID(space);
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
