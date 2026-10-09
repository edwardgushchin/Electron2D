using Electron2D;

internal sealed class SeparationRayShapeTests(PhysicsServer.Backend backend)
{
    internal static void Run(PhysicsServer.Backend backend = PhysicsServer.Backend.CPU) => new SeparationRayShapeTests(backend).RunCore();
    private void RunCore()
    {
        VerifyResource();
        VerifyQueries();
        VerifyShapeFamilies();
        VerifyMotionAndSnap();
        VerifyArea();
        SeparationRayDynamicsTests.Run(backend);
        Console.WriteLine($"Separation ray resource, directed queries, recovery, snap and allocation checks passed on {backend}.");
    }

    private void VerifyResource()
    {
        using var ray = new SeparationRayShape();
        var padding = MathF.Sqrt(0.5f) * 4;
        Check(ray.Length == 20 && !ray.SlideOnSlope &&
            ray.GetRect() == new Rect2(0, 0, 0, 20).Grow(padding), "Defaults and padded drawing bounds.");
        var changes = 0;
        ray.Changed += _ => changes++;
        ray.Length = 20; ray.SlideOnSlope = false;
        Check(changes == 0, "Equal assignments are silent.");
        ray.Length = 30; ray.SlideOnSlope = true;
        using var copy = (SeparationRayShape)ray.Duplicate();
        ray.Length = 0;
        Check(changes == 3 && copy.Length == 30 && copy.SlideOnSlope &&
            ray.GetRect() == new Rect2(0, 0, 0, 0).Grow(padding), "Independent duplicate and zero length.");
        Reject<ArgumentOutOfRangeException>(() => ray.Length = -1);
        Reject<ArgumentOutOfRangeException>(() => ray.Length = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => ray.Length = float.MaxValue);
        Check(ray.Length == 0 && changes == 3, "Invalid writes reject before mutation.");
        var node = new StaticBody();
        var collision = new CollisionShape { Shape = copy };
        node.AddChild(collision);
        collision.Owner = node;
        using var packed = new PackedScene();
        packed.Pack(node);
        using var instance = packed.Instantiate();
        Check(((CollisionShape)instance.GetChild(0)).Shape is SeparationRayShape { Length: 30, SlideOnSlope: true },
            "Packed scene retains ray state.");
        node.Dispose();
        copy.Dispose();
        Reject<ObjectDisposedException>(() => copy.Length = 1);
        Reject<ObjectDisposedException>(() => _ = copy.GetRect());
    }

    private void VerifyQueries()
    {
        var server = PhysicsServer.Service;
        var space = PhysicsServer.SpaceCreate(backend); PhysicsServer.SpaceSetActive(space, true);
        var floor = PhysicsServer.BodyCreate();
        var floorRID = PhysicsServer.RectangleShapeCreate();
        using var rectangle = new RectangleShape { Size = new(200, 10) };
        PhysicsServer.ShapeSetData(floorRID, rectangle);
        PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodyAddShape(floor, floorRID);
        PhysicsServer.BodySetTransform(floor, new(0, Vector2.One, 0, new(0, 25)));
        PhysicsServer.BodySetSpace(floor, space);
        using var ray = new SeparationRayShape { Length = 30 };
        using var query = new PhysicsShapeQueryParameters { Shape = ray };
        var direct = PhysicsServer.SpaceGetDirectState(space);
        var rest = direct.GetRestInfo(query);
        var points = direct.CollideShape(query);
        Check(direct.IntersectShape(query) is [var hit] && hit.ColliderRID == floor &&
            rest is { Normal.Y: < -0.99f, Point.Y: > 19.9f and < 20.1f } &&
            points.Length == 2 && MathF.Abs(points[0].Y - 30) < 0.01f && MathF.Abs(points[1].Y - 20) < 0.01f,
            $"Directed ray reports endpoint and surface contact: {rest?.Point}, {rest?.Normal}, {string.Join(';', points)}.");
        query.Transform = new(0, Vector2.One, 0, new(0, 23));
        Check(direct.IntersectShape(query).Length == 0, "A ray starting inside a filled shape has no surface hit.");
        query.Transform = new(Mathf.Pi, Vector2.One, 0, new(0, 50));
        Check(direct.GetRestInfo(query) is { Normal.Y: > 0.99f }, "Reversed ray hits the opposite face.");
        query.Transform = Transform.Identity;
        ray.Length = 10;
        query.Motion = new(0, 20);
        var fractions = direct.CastMotion(query);
        Check(fractions.SafeFraction is > 0.49f and < 0.51f && fractions.UnsafeFraction > fractions.SafeFraction,
            "Ray motion extends its endpoint along the axis.");
        query.Motion = new(200, 0);
        Check(direct.CastMotion(query) == (1f, 1f) && direct.IntersectShape(query).Length == 0,
            "Transverse motion does not turn a ray into a swept solid segment.");
        query.Motion = Vector2.Zero;
        query.Margin = 11;
        Check(direct.GetRestInfo(query) is { Normal.Y: < -0.99f }, "Margin extends the ray endpoint.");
        query.Margin = 0;
        ray.Length = 30;
        PhysicsServer.BodySetTransform(floor, new(0.3f, Vector2.One, 0, new(0, 25)));
        Check(direct.GetRestInfo(query) is { Normal.X: > -0.01f and < 0.01f, Normal.Y: < -0.99f },
            "Without slide, slope separation remains opposite the ray axis.");
        ray.SlideOnSlope = true;
        Check(direct.GetRestInfo(query) is { Normal.X: > 0.2f, Normal.Y: < -0.9f },
            "Slide mode uses the rotated surface normal.");

        var rayRID = PhysicsServer.SeparationRayShapeCreate();
        using var defaultData = PhysicsServer.ShapeGetData(rayRID);
        Check(defaultData is SeparationRayShape { Length: 20, SlideOnSlope: false }, "Server creation defaults.");
        PhysicsServer.ShapeSetData(rayRID, ray);
        var holder = PhysicsServer.BodyCreate();
        PhysicsServer.BodySetMode(holder, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodyAddShape(holder, rayRID);
        PhysicsServer.BodySetSpace(holder, space);
        using var cast = PhysicsRayQueryParameters.Create(new(-5, 10), new(5, 10));
        using var point = new PhysicsPointQueryParameters { Position = new(0, 10) };
        Check(direct.IntersectRay(cast) is null && direct.IntersectPoint(point).Length == 0,
            "Separation rays cannot be intersected by ray or point queries.");
        query.Exclude = [floor];
        Check(direct.IntersectShape(query).Length == 0, "Ray-ray pairs never contact.");
        using var circle = new CircleShape { Radius = 3 };
        using var solidQuery = new PhysicsShapeQueryParameters
        {
            Shape = circle,
            Exclude = [floor],
            Transform = new(0, Vector2.One, 0, new(-20, 15)),
            Motion = new(40, 0)
        };
        Check(direct.IntersectShape(solidQuery) is [var reverse] && reverse.ColliderRID == holder &&
            direct.CastMotion(solidQuery).SafeFraction is > 0.42f and < 0.43f &&
            direct.GetRestInfo(solidQuery) is not null,
            "A moving circle sweeps against a stationary separation ray.");
        using var square = new RectangleShape { Size = new(6, 6) };
        solidQuery.Shape = square;
        Check(direct.CastMotion(solidQuery).SafeFraction is > 0.42f and < 0.43f,
            "A moving polygon sweeps against a stationary separation ray.");
        for (var index = 0; index < 64; index++) { direct.CastMotion(solidQuery); direct.GetRestInfo(solidQuery); }
        var before = GC.GetTotalAllocatedBytes(true);
        for (var index = 0; index < 64; index++) { direct.CastMotion(solidQuery); direct.GetRestInfo(solidQuery); }
        Check(GC.GetTotalAllocatedBytes(true) == before, "Warmed reverse ray casts/rest queries allocate no managed bytes.");
        PhysicsServer.FreeRID(holder); PhysicsServer.FreeRID(rayRID); PhysicsServer.FreeRID(floor);
        PhysicsServer.FreeRID(floorRID); PhysicsServer.FreeRID(space);
    }

    private void VerifyMotionAndSnap()
    {
        using var ray = new SeparationRayShape();
        using var ground = new RectangleShape { Size = new(200, 10) };
        using var world = new World(backend); var root = new SubViewport { World = world };
        var floor = new StaticBody { Position = new(0, 30) };
        floor.AddChild(new CollisionShape { Shape = ground });
        var mover = new CharacterBody { FloorSnapLength = 10 };
        mover.AddChild(new CollisionShape { Shape = ray });
        root.AddChild(floor); root.AddChild(mover);
        using var tree = new SceneTree(root);
        using var parameters = new PhysicsTestMotionParameters { Motion = new(0, 10), Margin = 0 };
        using var result = new PhysicsTestMotionResult();
        var server = PhysicsServer.Service;
        Check(!parameters.CollideSeparationRay && !PhysicsServer.BodyTestMotion(mover.GetRID(), parameters, result),
            "Non-sliding rays are ignored by default during the sweep.");
        parameters.CollideSeparationRay = true;
        Check(PhysicsServer.BodyTestMotion(mover.GetRID(), parameters, result) &&
            result.GetTravel().Y is > 4.9f and < 5.1f && result.GetCollisionPoint().Y is > 24.9f and < 25.1f,
            "Explicit ray participation brackets floor impact and reports surface point.");
        parameters.From = new(0, Vector2.One, 0, new(0, 5));
        parameters.Motion = new(0, -10);
        Check(!PhysicsServer.BodyTestMotion(mover.GetRID(), parameters, result) && result.GetTravel().Y < -9.9f,
            "A touching ray can move away from its floor.");
        parameters.From = Transform.Identity;
        parameters.Motion = new(0, 10);
        parameters.CollideSeparationRay = false;
        ray.SlideOnSlope = true;
        Check(PhysicsServer.BodyTestMotion(mover.GetRID(), parameters, result), "Sliding rays participate without the flag.");
        mover.PhysicsProcessEnabled = true;
        tree.PhysicsFrame(1d / 60);
        mover.Velocity = new(0, 600);
        Check(mover.MoveAndSlide() && mover.IsOnFloor() && mover.Velocity.Y == 0 &&
            mover.GetSlideCollision(0).GetColliderRID() == floor.GetRID(),
            "Sliding rays classify their floor through CharacterBody movement.");
        mover.Velocity = new(0, -600);
        mover.MoveAndSlide();
        mover.MoveAndSlide();
        mover.Velocity = Vector2.Zero;
        mover.GlobalPosition = Vector2.Zero;
        ray.SlideOnSlope = false;
        parameters.From = new(0, Vector2.One, 0, new(0, 10));
        parameters.Motion = Vector2.Zero;
        parameters.RecoveryAsCollision = true;
        Check(PhysicsServer.BodyTestMotion(mover.GetRID(), parameters, result) && result.GetTravel().Y < -4 &&
            result.GetCollisionNormal().Y < -0.99f, "Recovery always separates an overlapping ray.");
        mover.ApplyFloorSnap();
        Check(mover.IsOnFloor() && mover.GlobalPosition.Y is > 4.8f and < 5.1f,
            $"Character snap explicitly enables non-sliding floor rays: {mover.IsOnFloor()}, {mover.GlobalPosition}.");
        ray.Changed += _ => throw new InvalidOperationException("User subscriber failed.");
        Reject<InvalidOperationException>(() => ray.Length = 30);
        parameters.From = Transform.Identity;
        Check(PhysicsServer.BodyTestMotion(mover.GetRID(), parameters, result) && result.GetTravel().Y < -4,
            "Committed ray edit reaches motion fixtures despite a failed Changed subscriber.");
        for (var index = 0; index < 64; index++) PhysicsServer.BodyTestMotion(mover.GetRID(), parameters, result);
        var before = GC.GetTotalAllocatedBytes(true);
        for (var index = 0; index < 64; index++) PhysicsServer.BodyTestMotion(mover.GetRID(), parameters, result);
        Check(GC.GetTotalAllocatedBytes(true) == before, "Warmed ray recovery queries allocate no managed bytes.");
    }

    private void VerifyArea()
    {
        using var ray = new SeparationRayShape { Length = 30 };
        using var rectangle = new RectangleShape { Size = new(200, 10) };
        using var world = new World(backend); var root = new SubViewport { World = world };
        var area = new Area();
        area.AddChild(new CollisionShape { Shape = ray });
        var body = new StaticBody { Position = new(0, 25) };
        body.AddChild(new CollisionShape { Shape = rectangle });
        root.AddChild(area); root.AddChild(body);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        Check(area.OverlapsBody(body), "Area ray detects a directed surface crossing.");
        for (var index = 0; index < 64; index++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetTotalAllocatedBytes(true);
        for (var index = 0; index < 64; index++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetTotalAllocatedBytes(true) == before, "Warmed directed Area frames allocate no managed bytes.");
        area.Position = new(0, 23);
        tree.PhysicsFrame(1d / 60);
        Check(!area.OverlapsBody(body), "Area ray starting inside solid does not overlap.");
    }

    private void VerifyShapeFamilies()
    {
        using var ray = new SeparationRayShape { Length = 30 };
        using var circle = new CircleShape { Radius = 5 };
        using var capsule = new CapsuleShape { Radius = 5, Height = 10 };
        using var segment = new SegmentShape { A = new(-5, 0), B = new(5, 0) };
        using var concave = new ConcavePolygonShape();
        concave.Segments = [new(-5, 0), new(5, 0)];
        using var convex = new ConvexPolygonShape { Points = [new(-5, -5), new(5, -5), new(5, 5), new(-5, 5)] };
        using var world = new World(backend); var root = new SubViewport { World = world };
        var body = new StaticBody { Position = new(0, 25) };
        var collision = new CollisionShape { Shape = circle };
        body.AddChild(collision); root.AddChild(body);
        using var tree = new SceneTree(root);
        var direct = body.GetWorld()!.DirectSpaceState;
        using var query = new PhysicsShapeQueryParameters { Shape = ray };
        foreach (var shape in new Shape[] { circle, capsule, segment, concave, convex })
        {
            collision.Shape = shape;
            Check(direct.GetRestInfo(query) is { Normal.Y: < -0.99f }, $"Ray contacts {shape.GetType().Name}.");
        }
        query.Transform = new(-Mathf.Pi / 2, Vector2.One, 0, new(-25, 25));
        Check(direct.GetRestInfo(query) is { Normal.X: < -0.99f }, "Query rotation and offset preserve the directed axis.");
        ray.Length = 0;
        Check(direct.IntersectShape(query).Length == 0, "Zero-length ray has no contact.");
        ray.Length = 0.25f;
        collision.Shape = segment;
        body.Position = new(0, 0.2f);
        query.Transform = Transform.Identity;
        Check(direct.GetRestInfo(query) is not null, "A short ray retains its exact directed query extent.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
