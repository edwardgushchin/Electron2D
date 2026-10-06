using Electron2D;

internal static class PhysicsShapeQueryTests
{
    internal static void Run()
    {
        VerifyParametersAndBorrowedShapeRID();
        VerifyOverlapAndMotion();
        VerifyContactsAndRestInfo();
        VerifyContactPairFamilies();
        VerifyCompoundAndHollowQueries();
        VerifySceneShapeQueries();
        VerifyCallerOwnedResults();
        Console.WriteLine("Physics shape-query overlap, motion, contacts and rest checks passed.");
    }

    private static void VerifyCallerOwnedResults()
    {
        using var root = new SubViewport();
        using var tree = new SceneTree(root);
        using var box = new RectangleShape { Size = new(40, 40) };
        var body = new StaticBody();
        body.AddChild(new CollisionShape { Shape = box });
        body.AddChild(new CollisionShape { Name = "Second", Shape = box });
        root.AddChild(body);
        var direct = body.GetWorld()!.DirectSpaceState;
        using var shape = new PhysicsShapeQueryParameters { Shape = box };
        using var point = new PhysicsPointQueryParameters();
        using var ray = PhysicsRayQueryParameters.Create(new(-60, 0), new(60, 0), exclude: [body.GetRID()]);
        var shapeBuffer = new PhysicsShapeResult[8];
        var pointBuffer = new PhysicsPointResult[8];
        var contacts = new Vector2[33];
        var expectedShape = direct.IntersectShape(shape);
        var expectedPoint = direct.IntersectPoint(point);
        var expectedContacts = direct.CollideShape(shape);
        Check(direct.IntersectShape(shape, shapeBuffer) == expectedShape.Length && shapeBuffer.AsSpan(0, expectedShape.Length).SequenceEqual(expectedShape), "Shape spans preserve compound owner order and identity.");
        Check(direct.IntersectPoint(point, pointBuffer) == expectedPoint.Length && pointBuffer.AsSpan(0, expectedPoint.Length).SequenceEqual(expectedPoint), "Point spans preserve ordered owner hits.");
        contacts[^1] = new(999, 999);
        Check(direct.CollideShape(shape, contacts) * 2 == expectedContacts.Length && contacts.AsSpan(0, expectedContacts.Length).SequenceEqual(expectedContacts) && contacts[^1] == new Vector2(999, 999), "Contact spans preserve pairs and the unused odd tail.");
        Check(direct.IntersectShape(shape, shapeBuffer.AsSpan(0, 1)) == 1 && direct.IntersectPoint(point, pointBuffer.AsSpan(0, 1)) == 1 && direct.CollideShape(shape, contacts.AsSpan(0, 3)) == 1, "Destination capacity truncates only after stable ordering and keeps complete pairs.");
        Check(direct.IntersectShape(shape, Span<PhysicsShapeResult>.Empty) == 0 && direct.IntersectPoint(point, Span<PhysicsPointResult>.Empty) == 0 && direct.CollideShape(shape, contacts.AsSpan(0, 1)) == 0, "Empty or sub-pair capacity writes no results.");
        for (var i = 0; i < 128; i++) { direct.IntersectShape(shape, shapeBuffer); direct.IntersectPoint(point, pointBuffer); direct.CollideShape(shape, contacts); direct.IntersectRay(ray); }
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++)
        {
            direct.IntersectShape(shape, shapeBuffer); direct.IntersectPoint(point, pointBuffer); direct.CollideShape(shape, contacts); direct.IntersectRay(ray);
            shape.Transform = new Transform(0, new Vector2(i % 2 == 0 ? 100 : 0, 0));
            point.Position = shape.Transform.Origin;
        }
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Warmed active/miss point, shape, contact and excluded-ray queries allocate zero managed bytes.");
        shape.Exclude = [body.GetRID()]; point.Exclude = [body.GetRID()];
        Check(direct.IntersectShape(shape, shapeBuffer) == 0 && direct.IntersectPoint(point, pointBuffer) == 0 && direct.CollideShape(shape, contacts) == 0, "Span query exclusions are shared with the copied-result contract.");
        Reject<ArgumentNullException>(() => direct.IntersectShape(null!, shapeBuffer));
        Reject<ArgumentNullException>(() => direct.IntersectPoint(null!, pointBuffer));
        Reject<ArgumentNullException>(() => direct.CollideShape(null!, contacts));
    }

    private static void VerifyParametersAndBorrowedShapeRID()
    {
        var server = PhysicsServer.Service;
        using var query = new PhysicsShapeQueryParameters();
        Check(query.Shape is null && !query.ShapeRID.IsValid() && query.Transform == Transform.Identity &&
              query.Motion == Vector2.Zero && query.Margin == 0 && query.CollisionMask == uint.MaxValue &&
              query.Exclude.Length == 0 && query.CollideWithBodies && !query.CollideWithAreas,
            "Shape query parameters expose pinned defaults.");
        Reject<ArgumentNullException>(() => query.Shape = null);
        Reject<ArgumentOutOfRangeException>(() => query.Margin = -1);
        Reject<ArgumentOutOfRangeException>(() => query.Motion = new(float.NaN, 0));
        Reject<ArgumentException>(() => query.Transform = new(0, new(2, 1), 0, Vector2.Zero));
        Reject<ArgumentNullException>(() => query.Exclude = null!);
        Check(query.Shape is null && query.Margin == 0 && query.Transform == Transform.Identity,
            "Invalid writes reject before changing query state.");

        using var circle = new CircleShape();
        query.Shape = circle;
        var borrowedRID = query.ShapeRID;
        Check(borrowedRID.IsValid() && ReferenceEquals(query.Shape, circle),
            "Assigning a resource retains its identity and a live server RID.");
        query.ShapeRID = borrowedRID;
        Check(ReferenceEquals(query.Shape, circle), "An equal RID assignment retains the borrowed resource.");
        Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(borrowedRID));
        Reject<InvalidOperationException>(() => PhysicsServer.ShapeSetData(borrowedRID, circle));
        var space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(space, true);
        var body = PhysicsServer.BodyCreate();
        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodyAddShape(body, borrowedRID);
        PhysicsServer.BodySetTransform(body, new(0, Vector2.One, 0, new(0, 50)));
        PhysicsServer.BodySetSpace(body, space);
        var direct = PhysicsServer.SpaceGetDirectState(space);
        using var ray = PhysicsRayQueryParameters.Create(new(0, 0), new(0, 100));
        Check(direct.IntersectRay(ray)?.Position.Y is > 39 and < 41,
            "A server collider can borrow a managed Shape RID for live geometry.");
        circle.Radius = 20;
        Check(direct.IntersectRay(ray)?.Position.Y is > 29 and < 31,
            "A resource edit invalidates server fixtures before the next direct query.");
        circle.Changed += _ => throw new InvalidOperationException("Earlier listener failed.");
        Reject<InvalidOperationException>(() => circle.Radius = 15);
        Check(direct.IntersectRay(ray)?.Position.Y is > 34 and < 36,
            "A failed user Changed listener cannot prevent a committed geometry edit reaching the server world.");

        var explicitRID = PhysicsServer.CircleShapeCreate();
        query.ShapeRID = explicitRID;
        Check(query.Shape is null && query.ShapeRID == explicitRID,
            "Assigning a different server RID clears the borrowed Shape reference.");
        query.Exclude = [body];
        var copied = query.Exclude;
        copied[0] = default;
        Check(query.Exclude[0] == body, "Exclusion arrays are copied on read and assignment.");
        PhysicsServer.FreeRID(explicitRID);
        circle.Dispose();
        Check(borrowedRID.IsValid(), "A held RID remains nonzero after its Shape resource is disposed.");
        Reject<ArgumentException>(() => PhysicsServer.ShapeGetData(borrowedRID));
        Check(direct.IntersectRay(ray) is null,
            "Disposing a borrowed Shape removes its fixture before a subsequent query.");
        PhysicsServer.FreeRID(body);
        PhysicsServer.FreeRID(space);
    }

    private static void VerifyOverlapAndMotion()
    {
        var server = PhysicsServer.Service;
        var space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(space, true);
        var body = PhysicsServer.BodyCreate();
        var floorRID = PhysicsServer.RectangleShapeCreate();
        using var floor = new RectangleShape { Size = new(200, 10) };
        using var probe = new RectangleShape { Size = new(20, 20) };
        PhysicsServer.ShapeSetData(floorRID, floor);
        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodyAddShape(body, floorRID);
        PhysicsServer.BodySetTransform(body, new(0, Vector2.One, 0, new(0, 100)));
        PhysicsServer.BodySetSpace(body, space);
        var direct = PhysicsServer.SpaceGetDirectState(space);
        using var query = new PhysicsShapeQueryParameters
        {
            Shape = probe,
            Transform = new(0, Vector2.One, 0, new(0, 70)),
            Motion = new(0, 40)
        };
        Check(direct.IntersectShape(query) is [var swept] && swept.ColliderRID == body,
            "IntersectShape includes a collider reached during the requested motion.");
        var cast = direct.CastMotion(query);
        Check(cast.SafeFraction is > 0.3f and < 0.45f &&
              cast.UnsafeFraction > cast.SafeFraction && cast.UnsafeFraction - cast.SafeFraction < 0.005f,
            "Motion fractions bracket the first collision in eight sweep refinements.");
        Check(direct.CollideShape(query).Length >= 2 &&
              direct.GetRestInfo(query) is { } sweptRest && sweptRest.ColliderRID == body &&
              sweptRest.Normal.Y < -0.9f,
            "Contact pairs and rest info resolve a collision reached during motion.");
        query.Margin = 5;
        var widened = direct.CastMotion(query);
        Check(widened.SafeFraction < cast.SafeFraction && widened.SafeFraction > 0.15f,
            "A positive query margin advances the contact point along the same sweep.");
        query.Margin = 0;

        query.Transform = new(0, Vector2.One, 0, new(0, 100));
        var overlaps = direct.IntersectShape(query);
        Check(overlaps is [var hit] && hit.ColliderRID == body && hit.Collider is null && hit.ShapeIndex == 0,
            "A deep filled-shape overlap returns one typed server collider result.");
        cast = direct.CastMotion(query);
        Check(cast == (1f, 1f), "CastMotion ignores a shape already colliding at its origin.");
        var secondBody = PhysicsServer.BodyCreate();
        PhysicsServer.BodySetMode(secondBody, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodyAddShape(secondBody, floorRID);
        PhysicsServer.BodySetTransform(secondBody, new(0, Vector2.One, 0, new(0, 140)));
        PhysicsServer.BodySetSpace(secondBody, space);
        Check(direct.IntersectShape(query).Length == 2 &&
              direct.IntersectShape(query, 1) is [var firstHit] && firstHit.ColliderRID == body &&
              direct.CastMotion(query).SafeFraction is > 0.55f and < 0.7f,
            "An initial overlap is skipped by the cast while a later collider remains hittable; shape caps follow RID order.");
        PhysicsServer.FreeRID(secondBody);
        query.Exclude = [body];
        Check(direct.IntersectShape(query).Length == 0 && direct.CastMotion(query) == (1f, 1f),
            "RID exclusions apply to both overlap and motion queries.");
        query.Exclude = [];
        PhysicsServer.BodySetCollisionLayer(body, 2);
        query.CollisionMask = 1;
        Check(direct.IntersectShape(query).Length == 0,
            "Shape queries test the collider's layer rather than its mask.");
        query.CollisionMask = 2;
        Check(direct.IntersectShape(query).Length == 1, "A matching layer restores the overlap.");
        query.CollisionMask = uint.MaxValue;
        Check(direct.IntersectShape(query, 0).Length == 0, "A zero cap returns an empty overlap array.");
        Reject<ArgumentOutOfRangeException>(() => direct.IntersectShape(query, -1));

        var area = PhysicsServer.AreaCreate();
        var areaRID = PhysicsServer.CircleShapeCreate();
        PhysicsServer.AreaAddShape(area, areaRID);
        PhysicsServer.AreaSetTransform(area, new(0, Vector2.One, 0, new(0, 40)));
        PhysicsServer.AreaSetSpace(area, space);
        query.Transform = new(0, Vector2.One, 0, new(0, 40));
        Check(direct.IntersectShape(query).Length == 0,
            "Area sensors are omitted by default from shape overlap queries.");
        query.CollideWithAreas = true;
        Check(direct.IntersectShape(query) is [var areaHit] && areaHit.ColliderRID == area,
            "Enabling Area filtering includes server-only sensor geometry.");
        Check(direct.GetRestInfo(query) is { } areaRest && areaRest.ColliderRID == area &&
              areaRest.LinearVelocity == Vector2.Zero && direct.CollideShape(query).Length >= 2,
            "Area contacts report zero collider velocity and paired contact points.");
        query.CollideWithAreas = false;

        var shapeRID = PhysicsServer.RectangleShapeCreate();
        PhysicsServer.ShapeSetData(shapeRID, probe);
        query.ShapeRID = shapeRID;
        query.Transform = new(0, Vector2.One, 0, new(0, 100));
        Check(query.Shape is null && direct.IntersectShape(query) is [var ridHit] && ridHit.ColliderRID == body,
            "The shape RID path executes without retaining the original managed resource.");
        query.Transform = new(0, Vector2.One, 0, new(0, 70));
        for (var frame = 0; frame < 64; frame++) direct.CastMotion(query);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) direct.CastMotion(query);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed unchanged shape casts allocate no managed memory on the owner thread.");
        PhysicsServer.FreeRID(shapeRID);
        Reject<ArgumentException>(() => direct.IntersectShape(query));
        PhysicsServer.FreeRID(area);
        PhysicsServer.FreeRID(areaRID);
        PhysicsServer.FreeRID(body);
        PhysicsServer.FreeRID(floorRID);
        PhysicsServer.FreeRID(space);
    }

    private static void VerifyContactsAndRestInfo()
    {
        var server = PhysicsServer.Service;
        var space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(space, true);
        var body = PhysicsServer.BodyCreate();
        var floorRID = PhysicsServer.RectangleShapeCreate();
        using var floor = new RectangleShape { Size = new(200, 10) };
        using var circle = new CircleShape();
        PhysicsServer.ShapeSetData(floorRID, floor);
        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodyAddShape(body, floorRID);
        PhysicsServer.BodySetTransform(body, new(0, Vector2.One, 0, new(0, 100)));
        PhysicsServer.BodySetSpace(body, space);
        var direct = PhysicsServer.SpaceGetDirectState(space);
        using var query = new PhysicsShapeQueryParameters
        {
            Shape = circle,
            Transform = new(0, Vector2.One, 0, new(0, 70))
        };
        Check(direct.CollideShape(query).Length == 0 && direct.GetRestInfo(query) is null,
            "Separated shapes have no contact-point pairs or rest result.");
        query.Transform = new(0, Vector2.One, 0, new(0, 90));
        var pairs = direct.CollideShape(query);
        var rest = direct.GetRestInfo(query);
        Check(pairs.Length >= 2 && (pairs.Length & 1) == 0 &&
              pairs[0].Y is > 97 and < 102 && pairs[1].Y is > 93 and < 98,
            "Circle-floor contact points pair the query surface with the collider surface.");
        Check(rest is { } hit && hit.ColliderRID == body && hit.Collider is null &&
              hit.ShapeIndex == 0 && hit.Normal.Y < -0.9f &&
              hit.Point.Y is > 93 and < 98 && hit.LinearVelocity == Vector2.Zero,
            "Rest info reports the server-only collider, outward normal, point and zero static velocity.");
        for (var frame = 0; frame < 64; frame++) direct.GetRestInfo(query);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) direct.GetRestInfo(query);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed unchanged rest queries allocate no managed memory on the owner thread.");
        Check(direct.CollideShape(query, 0).Length == 0, "Zero max results produces no contact pairs.");
        Reject<ArgumentOutOfRangeException>(() => direct.CollideShape(query, -1));
        using var boxProbe = new RectangleShape { Size = new(20, 20) };
        query.Shape = boxProbe;
        var twoPointContact = direct.CollideShape(query);
        Check(twoPointContact.Length >= 4 && direct.CollideShape(query, 1).Length == 2,
            "A polygon manifold can report two point pairs and a one-pair cap truncates it.");
        query.Shape = circle;

        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Kinematic);
        PhysicsServer.BodySetLinearVelocity(body, new(0, 40));
        rest = direct.GetRestInfo(query);
        Check(rest is { } moving && moving.LinearVelocity.Y is > 39 and < 41,
            "Rest info samples a moving collider's velocity at the contact point.");
        PhysicsServer.FreeRID(body);
        PhysicsServer.FreeRID(floorRID);
        PhysicsServer.FreeRID(space);
    }

    private static void VerifyContactPairFamilies()
    {
        var server = PhysicsServer.Service;
        var space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(space, true);
        var direct = PhysicsServer.SpaceGetDirectState(space);
        using var circle = new CircleShape();
        using var capsule = new CapsuleShape();
        using var rectangle = new RectangleShape { Size = new(20, 20) };
        Shape[] families = [circle, capsule, rectangle];
        using var query = new PhysicsShapeQueryParameters
        {
            Transform = new(0, Vector2.One, 0, new(0, -15))
        };
        foreach (var candidateShape in families)
        {
            var body = PhysicsServer.BodyCreate();
            var shapeRID = candidateShape switch
            {
                CircleShape => PhysicsServer.CircleShapeCreate(),
                CapsuleShape => PhysicsServer.CapsuleShapeCreate(),
                _ => PhysicsServer.RectangleShapeCreate()
            };
            PhysicsServer.ShapeSetData(shapeRID, candidateShape);
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(body, shapeRID);
            PhysicsServer.BodySetSpace(body, space);
            foreach (var probeShape in families)
            {
                query.Shape = probeShape;
                var hits = direct.IntersectShape(query);
                var pairs = direct.CollideShape(query);
                var rest = direct.GetRestInfo(query);
                Check(hits is [var overlap] && overlap.ColliderRID == body &&
                      pairs.Length >= 2 && rest is { } info && info.ColliderRID == body &&
                      info.Normal.IsFinite() && info.Normal.Length() > 0.5f,
                    "Circle, capsule and rectangle combinations retain overlap, contact pair and finite normal.");
            }
            PhysicsServer.FreeRID(body);
            PhysicsServer.FreeRID(shapeRID);
        }
        PhysicsServer.FreeRID(space);
    }

    private static void VerifyCompoundAndHollowQueries()
    {
        var server = PhysicsServer.Service;
        var space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(space, true);
        var body = PhysicsServer.BodyCreate();
        var circleRID = PhysicsServer.CircleShapeCreate();
        using var circle = new CircleShape { Radius = 5 };
        PhysicsServer.ShapeSetData(circleRID, circle);
        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodyAddShape(body, circleRID);
        PhysicsServer.BodySetSpace(body, space);
        var direct = PhysicsServer.SpaceGetDirectState(space);
        var vertices = new Vector2[12];
        for (var index = 0; index < vertices.Length; index++)
        {
            var angle = index * Mathf.Tau / vertices.Length;
            vertices[index] = new(MathF.Cos(angle) * 20, MathF.Sin(angle) * 20);
        }
        using var convex = new ConvexPolygonShape { Points = vertices };
        using var query = new PhysicsShapeQueryParameters { Shape = convex };
        Check(direct.IntersectShape(query) is [var compound] && compound.ColliderRID == body &&
              compound.ShapeIndex == 0,
            "A many-vertex convex query deduplicates compound proxy pieces by collider shape owner.");

        using var hollow = new ConcavePolygonShape
        {
            Segments = [new(-20, -20), new(20, -20), new(20, -20), new(20, 20),
                new(20, 20), new(-20, 20), new(-20, 20), new(-20, -20)]
        };
        query.Shape = hollow;
        Check(direct.IntersectShape(query).Length == 0,
            "A circle wholly inside paired hollow edges does not overlap them.");
        PhysicsServer.BodySetTransform(body, new(0, Vector2.One, 0, new(20, 0)));
        Check(direct.IntersectShape(query) is [var edge] && edge.ColliderRID == body &&
              direct.CollideShape(query).Length >= 2 && direct.GetRestInfo(query) is { },
            "A circle touching one hollow edge produces overlap and contact information.");

        using var segment = new SegmentShape { A = new(-20, 0), B = new(20, 0) };
        query.Shape = segment;
        Check(direct.CollideShape(query).Length >= 2 && direct.GetRestInfo(query) is { },
            "A two-sided segment query can contact a circle without a solid interior.");
        PhysicsServer.BodySetTransform(body, Transform.Identity);
        segment.A = Vector2.Zero;
        segment.B = Vector2.Zero;
        Check(direct.IntersectShape(query) is [var point] && point.ColliderRID == body,
            "A sub-slop segment uses the same zero-radius point-query fallback as a body fixture.");
        PhysicsServer.FreeRID(body);
        PhysicsServer.FreeRID(circleRID);
        PhysicsServer.FreeRID(space);
    }

    private static void VerifySceneShapeQueries()
    {
        using var floorShape = new RectangleShape { Size = new(200, 10) };
        using var sensorShape = new CircleShape();
        using var probe = new CircleShape();
        var root = new Node();
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100), CollisionMask = 0 };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        var sensor = new Area { Name = "Sensor", Position = new(0, 50) };
        sensor.AddChild(new CollisionShape { Shape = sensorShape });
        root.AddChild(floor); root.AddChild(sensor);
        using var tree = new SceneTree(root);
        var direct = floor.GetWorld()!.DirectSpaceState;
        using var query = new PhysicsShapeQueryParameters
        {
            Shape = probe,
            Transform = new(0, Vector2.One, 0, new(0, 90))
        };
        var hits = direct.IntersectShape(query);
        var rest = direct.GetRestInfo(query);
        Check(hits is [var hit] && hit.ColliderRID == floor.GetRID() && ReferenceEquals(hit.Collider, floor) &&
              hit.ColliderID == floor.InstanceID && hit.ShapeIndex == 0 &&
              rest is { } info && info.ColliderRID == floor.GetRID() && info.Normal.Y < -0.9f,
            "Shape queries before the first step share scene fixtures and retain collider identity even with mask zero.");
        query.Transform = new(0, Vector2.One, 0, new(0, 70));
        query.Motion = new(0, 40);
        Check(direct.CastMotion(query).SafeFraction is > 0.3f and < 0.45f,
            "The same direct state casts a scene query shape toward a scene body.");
        query.Exclude = [floor.GetRID()];
        Check(direct.CastMotion(query) == (1f, 1f), "Scene collider RID exclusions also apply to casts.");
        query.Exclude = [];
        query.Transform = new(0, Vector2.One, 0, new(0, 50));
        query.Motion = Vector2.Zero;
        Check(direct.IntersectShape(query).Length == 0, "Area sensors are omitted by default.");
        query.CollideWithAreas = true;
        Check(direct.IntersectShape(query) is [var areaHit] && areaHit.ColliderRID == sensor.GetRID(),
            "Area-enabled shape queries include scene sensors in the shared world.");
        Reject<InvalidOperationException>(() => Task.Run(() => direct.CastMotion(query)).GetAwaiter().GetResult());
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
