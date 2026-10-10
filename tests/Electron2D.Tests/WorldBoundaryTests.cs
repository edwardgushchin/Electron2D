using System.Diagnostics;
using Electron2D;

internal sealed class WorldBoundaryTests(bool gpu, PhysicsServer.Backend backend)
{
    internal static void Run(bool gpu = false) => new WorldBoundaryTests(gpu, PhysicsServer.Backend.CPU).RunCore();
    internal static void Run(PhysicsServer.Backend backend) => new WorldBoundaryTests(false, backend).RunCore();
    private void RunCore()
    {
        VerifyAPI();
        using var plane = new WorldBoundaryShape { Normal = new(0, -2), Distance = 10 };
        using var ball = new CircleShape { Radius = 2 };
        var space = PhysicsServer.SpaceCreate(backend); var floor = PhysicsServer.BodyCreate(); var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.SpaceSetActive(space, true);
            if (gpu) PhysicsServer.Service.GetSceneSpace(space).EnableGPUSolver();
            PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(floor, plane.GetRID()); PhysicsServer.BodySetTransform(floor, new(0, new(0, 40))); PhysicsServer.BodySetSpace(floor, space);
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid);
            PhysicsServer.BodyAddShape(body, ball.GetRID()); PhysicsServer.BodySetTransform(body, new(0, new(1_000_000, 0)));
            PhysicsServer.BodySetMaxContactsReported(body, 4); PhysicsServer.BodySetSpace(body, space);
            for (var i = 0; i < 90; i++) PhysicsServer.SpaceStep(space, 1d / 60);
            using var state = PhysicsServer.BodyGetDirectState(body)!;
            Check(state.Transform.Origin.Y is > 32 and < 34 && state.GetContactCount() > 0, $"Infinite floor response: {state.Transform.Origin}, {state.GetContactCount()}.");
            using var direct = PhysicsServer.SpaceGetDirectState(space)!;
            using var ray = PhysicsRayQueryParameters.Create(new(-1_000_000, 0), new(-1_000_000, 60));
            var hit = direct.IntersectRay(ray);
            Check(hit is { } h && MathF.Abs(h.Position.Y - 35) < .001f && h.Normal.Y < -.99f, "Infinite plane ray intersection and nonunit equation.");
            using var point = new PhysicsPointQueryParameters { Position = new(2_000_000, 36) };
            Check(direct.IntersectPoint(point).Length == 1, "Infinite solid side contains points.");
            using var shape = new PhysicsShapeQueryParameters { Shape = ball, Transform = new(0, new(2_000_000, 34)) };
            Check(direct.IntersectShape(shape).Length == 1 && direct.GetRestInfo(shape) is { Normal.Y: < -.99f }, "Infinite plane shape contact.");
            shape.Transform = new(0, new(2_000_000, 0)); shape.Motion = new(0, 60);
            var cast = direct.CastMotion(shape); Check(cast.SafeFraction is > .54f and < .56f, $"Plane cast: {cast.SafeFraction}.");
            using var parameters = new PhysicsTestMotionParameters { From = new(0, new(3_000_000, 0)), Motion = new(0, 60) };
            using var motion = new PhysicsTestMotionResult();
            Check(PhysicsServer.BodyTestMotion(body, parameters, motion) && motion.GetTravel().Y is > 32 and < 34, "Plane body motion.");
            PhysicsServer.AreaSetGravity(space, 0);
            foreach (var mode in new[] { CCDMode.CastRay, CCDMode.CastShape })
            {
                PhysicsServer.BodySetTransform(body, new(0, new(1_000_000, 0))); PhysicsServer.BodySetLinearVelocity(body, new(0, 6000));
                PhysicsServer.BodySetContinuousCollisionDetectionMode(body, mode); PhysicsServer.SpaceStep(space, 1d / 60);
                Check(state.Transform.Origin.Y < 34 && state.LinearVelocity.Y < 1, $"Plane CCD {mode}: {state.Transform.Origin}, {state.LinearVelocity}.");
            }
            Check(plane.Collide(new(0, new(0, 40)), ball, new(0, new(4_000_000, 34))), "Standalone infinite collision.");
            Check(!plane.CollideWithMotion(new(0, new(0, 40)), new(0, -100), ball, new(0, new(4_000_000, 34)), new(0, -10)), "Boundary resource tests the other endpoint.");
            PhysicsServer.BodySetContinuousCollisionDetectionMode(body, CCDMode.Disabled); PhysicsServer.BodySetCanSleep(body, false);
            PhysicsServer.AreaSetGravity(space, 980);
            for (var i = 0; i < 128; i++) PhysicsServer.SpaceStep(space, 1d / 60);
            var samples = new long[128]; var allocated = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < samples.Length; i++) { var start = Stopwatch.GetTimestamp(); PhysicsServer.SpaceStep(space, 1d / 60); samples[i] = Stopwatch.GetTimestamp() - start; }
            allocated = GC.GetTotalAllocatedBytes(true) - allocated; Report(gpu ? "CPU host/GPU stages" : backend.ToString(), samples, allocated);
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(floor); PhysicsServer.FreeRID(space); }
        VerifyRotation(gpu); VerifyScene(gpu); VerifyDiscreteRotation(gpu); VerifyFamilies(false, gpu, backend);
        Console.WriteLine($"World boundary response and direct queries passed ({(gpu ? "CPU host/GPU stages" : backend.ToString())}).");
    }
    internal static void RunResident()
    {
        using var plane = new WorldBoundaryShape { Normal = new(0, -2), Distance = 10 };
        using var ball = new CircleShape { Radius = 2 };
        using var store = new GPUPhysicsBodyStore();
        var floor = store.Add(new(PhysicsServer.BodyMode.Static, new(0, 40), 0, Vector2.Zero, 0));
        var body = store.Add(new(PhysicsServer.BodyMode.Rigid, new(1_000_000, 0), 0, Vector2.Zero, 0, CanSleep: false));
        var floorShape = store.AddShape(floor, plane); store.AddShape(body, ball);
        for (var i = 0; i < 90; i++) store.Simulate(1f / 60, new(0, 980));
        var states = new GPUPhysicsBodyStore.Snapshot[1]; store.Read([body], states);
        Check(states[0].Position.Y is > 32 and < 34, $"Resident infinite floor: {states[0].Position}.");
        var hits = new GPUPhysicsBodyStore.QueryHit[4]; var counts = new int[2];
        store.Query([new(new(-1_000_000, 0), new(-1_000_000, 60), Ray: true, Limit: 1), new(new(2_000_000, 36), Limit: 3)], [], counts, hits);
        Check(counts[0] == 1 && MathF.Abs(hits[0].Position.Y - 35) < .001f && counts[1] == 1, "Resident infinite ray/point queries.");
        using var queryGeometry = store.RetainQueryGeometry(ball);
        var shapeHits = new GPUPhysicsBodyStore.ShapeQueryHit[2];
        store.QueryShapes([new(queryGeometry, new(0, new(2_000_000, 34)), Mode: GPUPhysicsBodyStore.ShapeQueryMode.Rest),
            new(queryGeometry, new(0, new(2_000_000, 0)), new(0, 60), Mode: GPUPhysicsBodyStore.ShapeQueryMode.Cast)], [], counts, shapeHits);
        Check(counts[0] == 1 && shapeHits[0].Normal.Y < -.99f && counts[1] == 1 && shapeHits[1].SafeFraction is > .53f and < .57f, $"Resident infinite shape queries: {counts[0]}, {counts[1]}, {shapeHits[1].SafeFraction}.");
        var motions = new GPUPhysicsBodyStore.MotionQueryResult[1];
        store.TestMotion([new(body, new(0, new(3_000_000, 0)), new(0, 60))], [], [], motions);
        Check(motions[0].Collided && motions[0].Travel.Y is > 32 and < 34, $"Resident infinite motion query: {motions[0].Travel}.");
        foreach (var mode in new[] { CCDMode.CastRay, CCDMode.CastShape })
        {
            store.SetPose(body, new(1_000_000, 0), 0); store.SetVelocity(body, new(0, 6000), 0); store.SetCCDMode(body, mode);
            store.Simulate(1f / 60, Vector2.Zero); store.Read([body], states);
            Check(states[0].Position.Y < 34 && states[0].Velocity.Y < 1, $"Resident plane CCD {mode}: {states[0].Position}, {states[0].Velocity}.");
        }
        store.SetCCDMode(body, CCDMode.Disabled);
        for (var i = 0; i < 128; i++) store.Simulate(1f / 60, new(0, 980));
        var upload = store.UploadBytes; var uniforms = store.UniformBytes; var readback = store.ReadbackBytes; var wait = store.WaitMS;
        var samples = new long[128]; var allocated = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < samples.Length; i++) { var start = Stopwatch.GetTimestamp(); store.Simulate(1f / 60, new(0, 980)); samples[i] = Stopwatch.GetTimestamp() - start; }
        allocated = GC.GetTotalAllocatedBytes(true) - allocated; Report("resident GPU", samples, allocated);
        Console.WriteLine($"Boundary resident GPU traffic/frame: upload={(store.UploadBytes - upload) / 128}, uniforms={(store.UniformBytes - uniforms) / 128}, readback={(store.ReadbackBytes - readback) / 128} B; wait={(store.WaitMS - wait) / 128:F4} ms; {store.Driver}, {store.DeviceName}.");
        VerifyResidentRotation(); VerifyResidentLifetime(); VerifyFamilies(true);
        Console.WriteLine("Resident world boundary response, queries, CCD, sleep and shape lifetime passed.");
    }
    private static void Report(string backend, long[] samples, long bytes)
    {
        Array.Sort(samples); var ms = 1000d / Stopwatch.Frequency;
        Console.WriteLine($"Boundary {backend}: 1 active body + floor, 128 warmup/128 frames at 1/60 s; p50/p95/p99={samples[64] * ms:F4}/{samples[121] * ms:F4}/{samples[126] * ms:F4} ms, {bytes} all-thread managed B.");
        Check(bytes == 0, "Prepared boundary frames allocate zero managed bytes.");
    }
    private static void VerifyAPI()
    {
        using var plane = new WorldBoundaryShape();
        Check(plane.Normal == Vector2.Up && plane.Distance == 0 && plane.GetRect() == new Rect2(-100, -30, 200, 30), "Boundary defaults and marker.");
        plane.Normal = new(0, -2); plane.Distance = 10; using var copy = (WorldBoundaryShape)plane.Duplicate();
        plane.Distance = 20; Check(copy.Normal == new Vector2(0, -2) && copy.Distance == 10, "Copy retains nonunit authoring data independently.");
        var rid = PhysicsServer.WorldBoundaryShapeCreate();
        try
        {
            PhysicsServer.ShapeSetData(rid, copy); using var data = (WorldBoundaryShape)PhysicsServer.ShapeGetData(rid);
            Check(data.Normal == copy.Normal && data.Distance == 10 && PhysicsServer.ShapeGetType(rid) == PhysicsServer.ShapeType.WorldBoundary, "Server shape data/type.");
        }
        finally { PhysicsServer.FreeRID(rid); }
        Shape[] family = [new SeparationRayShape(), new SegmentShape(), new CircleShape(), new RectangleShape(), new CapsuleShape(), new ConvexPolygonShape(), new ConcavePolygonShape()];
        try { for (var i = 0; i < family.Length; i++) Check((int)PhysicsServer.ShapeGetType(family[i].GetRID()) == i + 1, "Logical shape types retain their identities."); }
        finally { foreach (var shape in family) shape.Dispose(); }
        Reject<ArgumentOutOfRangeException>(() => plane.Normal = Vector2.Zero);
        Reject<ArgumentOutOfRangeException>(() => plane.Distance = float.NaN);
        Check(plane.Normal == new Vector2(0, -2) && plane.Distance == 20, "Invalid boundary edits are atomic.");
        Reject<ArgumentException>(() => PhysicsServer.ShapeGetType(rid));
    }
    private void VerifyRotation(bool gpu)
    {
        using var plane = new WorldBoundaryShape(); using var ray = new SeparationRayShape();
        var space = PhysicsServer.SpaceCreate(backend); var floor = PhysicsServer.BodyCreate(); var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.SpaceSetActive(space, true); var world = PhysicsServer.Service.GetSceneSpace(space); PhysicsServer.AreaSetGravity(space, 0);
            if (gpu) world.EnableGPUSolver();
            PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetMass(floor, 1e9f); PhysicsServer.BodySetInertia(floor, 1e9f);
            PhysicsServer.BodyAddShape(floor, plane.GetRID()); PhysicsServer.BodySetAngularVelocity(floor, -20); PhysicsServer.BodySetSpace(floor, space);
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodyAddShape(body, ray.GetRID());
            PhysicsServer.BodySetTransform(body, new(0, new(80, -20.3f))); PhysicsServer.BodySetContinuousCollisionDetectionMode(body, CCDMode.CastShape); PhysicsServer.BodySetSpace(body, space);
            PhysicsServer.SpaceStep(space, .02);
            var pose = PhysicsServer.BodyGetTransform(floor); var tip = PhysicsServer.BodyGetTransform(body) * new Vector2(0, 20);
            var normal = Vector2.Up.Rotated(pose.Rotation); var gap = normal.Dot(tip - pose.Origin);
            Check(gap > -.75f, $"Rotating infinite boundary CCD: gap={gap} u (0.5 u solver slop + 0.25 u advancement allowance).");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(floor); PhysicsServer.FreeRID(space); }
    }
    private void VerifyScene(bool gpu)
    {
        using var plane = new WorldBoundaryShape(); using var areaPlane = new WorldBoundaryShape();
        using var ball = new CircleShape { Radius = 2 }; using var zero = new SeparationRayShape { Length = 0 };
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld }; var floor = new StaticBody { Name = "Floor", Position = new(0, 40) };
        floor.AddChild(new CollisionShape { Shape = plane });
        var body = new RigidBody { Name = "Ball", Position = new(10000, 0), ContactMonitor = true, MaxContactsReported = 4 };
        body.AddChild(new CollisionShape { Shape = ball });
        var area = new Area { Name = "HalfSpace", Position = new(0, 20) }; area.AddChild(new CollisionShape { Shape = areaPlane });
        var empty = new StaticBody { Name = "EmptyRay", Position = new(20, 30) }; empty.AddChild(new CollisionShape { Shape = zero });
        root.AddChild(floor); root.AddChild(body); root.AddChild(area); root.AddChild(empty);
        using var tree = new SceneTree(root); if (gpu) body.Space!.EnableGPUSolver();
        var contacts = 0; var overlaps = 0;
        body.BodyEntered += other => { if (ReferenceEquals(other, floor)) contacts++; };
        area.BodyEntered += other => { if (ReferenceEquals(other, body)) overlaps++; };
        for (var i = 0; i < 180; i++) tree.PhysicsFrame(1d / 60);
        Check(contacts == 1 && overlaps == 1 && area.OverlapsBody(body) && !area.OverlapsBody(empty), "Half-plane scene contacts, Area sensing and zero rays.");
        Check(body.Sleeping, "A body resting on an infinite floor sleeps.");
        plane.Distance = 10;
        for (var i = 0; i < 60; i++) tree.PhysicsFrame(1d / 60);
        Check(body.Position.Y is > 27 and < 30, "Live half-plane edits wake and reposition a supported body.");
        floor.QueueFree(); tree.PhysicsFrame(1d / 60);
        for (var i = 0; i < 30; i++) tree.PhysicsFrame(1d / 60);
        Check(body.Position.Y > 60, "Removing a boundary clears its broad-phase registration and wakes neighbours.");
    }
    private static void VerifyResidentRotation()
    {
        using var plane = new WorldBoundaryShape(); using var ray = new SeparationRayShape(); using var store = new GPUPhysicsBodyStore();
        var floor = store.Add(new(PhysicsServer.BodyMode.Rigid, Vector2.Zero, 0, Vector2.Zero, -20, Mass: 1e9f, Inertia: 1e9f, CanSleep: false));
        var body = store.Add(new(PhysicsServer.BodyMode.Rigid, new(80, -20.3f), 0, Vector2.Zero, 0, CanSleep: false, ContinuousMode: CCDMode.CastShape));
        store.AddShape(floor, plane); store.AddShape(body, ray); store.Simulate(.02f, Vector2.Zero);
        Span<GPUPhysicsBodyStore.Snapshot> state = stackalloc GPUPhysicsBodyStore.Snapshot[2]; store.Read([floor, body], state);
        var tip = state[1].Position + new Vector2(0, 20).Rotated(state[1].Rotation);
        var gap = Vector2.Up.Rotated(state[0].Rotation).Dot(tip - state[0].Position);
        Check(gap > -.75f, $"Resident rotating infinite boundary CCD: gap={gap} u.");
    }
    private static void VerifyFamilies(bool resident, bool stages = false, PhysicsServer.Backend backend = PhysicsServer.Backend.CPU)
    {
        Shape[] shapes = [new CircleShape { Radius = 3 }, new RectangleShape { Size = new(8, 6) }, new CapsuleShape { Radius = 2, Height = 8 },
            new SegmentShape { A = new(-4, 0), B = new(4, 0) }, new ConvexPolygonShape { Points = [new(-4, -3), new(4, -3), new(4, 3), new(-4, 3)] },
            new ConcavePolygonShape { Segments = [new(-4, 0), new(4, 0)] }, new SeparationRayShape()];
        using var plane = new WorldBoundaryShape();
        Span<GPUPhysicsBodyStore.Snapshot> stateBuffer = stackalloc GPUPhysicsBodyStore.Snapshot[1];
        try
        {
            foreach (var shape in shapes)
            {
                if (resident)
                {
                    using var store = new GPUPhysicsBodyStore();
                    var floor = store.Add(new(PhysicsServer.BodyMode.Static, Vector2.Zero, 0, Vector2.Zero, 0));
                    var body = store.Add(new(PhysicsServer.BodyMode.RigidLinear, new(100, -30), 0, Vector2.Zero, 0, CanSleep: false));
                    store.AddShape(floor, plane); store.AddShape(body, shape);
                    for (var i = 0; i < 60; i++) store.Simulate(1f / 60, new(0, 980));
                    store.Read([body], stateBuffer);
                    Check(stateBuffer[0].Position.Y is >= -30 and < 1 && store.FindContacts(2) > 0, $"Resident boundary response for {shape.GetType().Name}: {stateBuffer[0].Position}.");
                }
                else
                {
                    var space = PhysicsServer.SpaceCreate(backend); var floor = PhysicsServer.BodyCreate(); var body = PhysicsServer.BodyCreate();
                    try
                    {
                        PhysicsServer.SpaceSetActive(space, true); if (stages) PhysicsServer.Service.GetSceneSpace(space).EnableGPUSolver();
                        PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(floor, plane.GetRID()); PhysicsServer.BodySetSpace(floor, space);
                        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.RigidLinear); PhysicsServer.BodySetCanSleep(body, false);
                        PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetTransform(body, new(0, new(100, -30)));
                        PhysicsServer.BodySetMaxContactsReported(body, 4); PhysicsServer.BodySetSpace(body, space);
                        for (var i = 0; i < 60; i++) PhysicsServer.SpaceStep(space, 1d / 60);
                        using var state = PhysicsServer.BodyGetDirectState(body)!;
                        Check(state.Transform.Origin.Y is >= -30 and < 1 && state.GetContactCount() > 0, $"CPU boundary response for {shape.GetType().Name}: {state.Transform.Origin}.");
                    }
                    finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(floor); PhysicsServer.FreeRID(space); }
                }
            }
        }
        finally { foreach (var shape in shapes) shape.Dispose(); }
    }
    private void VerifyDiscreteRotation(bool gpu)
    {
        using var plane = new WorldBoundaryShape(); using var circle = new CircleShape { Radius = 2 };
        foreach (var dynamicPlane in new[] { false, true })
        {
            var space = PhysicsServer.SpaceCreate(backend); var floor = PhysicsServer.BodyCreate(); var body = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.SpaceSetActive(space, true); PhysicsServer.AreaSetGravity(space, 0);
                PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
                if (gpu) PhysicsServer.Service.GetSceneSpace(space).EnableGPUSolver();
                PhysicsServer.BodySetMode(floor, dynamicPlane ? PhysicsServer.BodyMode.Rigid : PhysicsServer.BodyMode.Static);
                PhysicsServer.BodySetMass(floor, 1e9f); PhysicsServer.BodySetInertia(floor, 1e9f); PhysicsServer.BodySetCanSleep(floor, false);
                PhysicsServer.BodyAddShape(floor, plane.GetRID()); PhysicsServer.BodySetSpace(floor, space);
                PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetTransform(body, new(0, new(100, -50)));
                PhysicsServer.BodySetCanSleep(body, false); PhysicsServer.BodySetMaxContactsReported(body, 4); PhysicsServer.BodySetSpace(body, space);
                PhysicsServer.SpaceStep(space, .01);
                using var state = PhysicsServer.BodyGetDirectState(body)!;
                Check(state.GetContactCount() == 0, "Plane free side starts without contact.");
                if (dynamicPlane) PhysicsServer.BodySetAngularVelocity(floor, -10);
                else PhysicsServer.BodySetTransform(floor, new(-MathF.PI / 2, Vector2.Zero));
                var contacted = false;
                for (var i = 0; i < 10; i++) { PhysicsServer.SpaceStep(space, .01); contacted |= state.GetContactCount() > 0; }
                Check(contacted, $"Rotating plane must renew broad-phase candidates even when its bookkeeping AABB is unchanged: dynamic={dynamicPlane}.");
            }
            finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(floor); PhysicsServer.FreeRID(space); }
        }
    }
    private static void VerifyResidentLifetime()
    {
        using var plane = new WorldBoundaryShape(); using var ball = new CircleShape { Radius = 2 };
        using var areaPlane = new WorldBoundaryShape(); using var zero = new SeparationRayShape { Length = 0 };
        using var store = new GPUPhysicsBodyStore();
        var floor = store.Add(new(PhysicsServer.BodyMode.Static, new(0, 40), 0, Vector2.Zero, 0));
        var body = store.Add(new(PhysicsServer.BodyMode.Rigid, new(10000, 0), 0, Vector2.Zero, 0));
        var area = store.Add(new(PhysicsServer.BodyMode.Static, new(0, 20), 0, Vector2.Zero, 0));
        var empty = store.Add(new(PhysicsServer.BodyMode.Static, new(20, 30), 0, Vector2.Zero, 0));
        var support = store.AddShape(floor, plane); store.AddShape(body, ball);
        var sensor = store.AddShape(area, areaPlane, sensor: true); var emptyShape = store.AddShape(empty, zero);
        for (var i = 0; i < 180; i++) store.Simulate(1f / 60, new(0, 980));
        Span<GPUPhysicsBodyStore.Snapshot> state = stackalloc GPUPhysicsBodyStore.Snapshot[1]; store.Read([body], state);
        Check(state[0].Sleeping, "Resident boundary supports sleeping bodies.");
        Span<GPUPhysicsBodyStore.ContactPoint> contacts = stackalloc GPUPhysicsBodyStore.ContactPoint[8];
        var count = store.ReadContacts(contacts); var areaContact = false;
        foreach (var contact in contacts[..count])
        {
            Check(contact.ShapeA != emptyShape.Index && contact.ShapeB != emptyShape.Index, "Resident zero rays have no plane contact.");
            areaContact |= contact.ShapeA == sensor.Index || contact.ShapeB == sensor.Index;
        }
        Check(areaContact, "Resident half-plane sensor keeps exact overlaps.");
        plane.Distance = 10;
        for (var i = 0; i < 60; i++) store.Simulate(1f / 60, new(0, 980));
        store.Read([body], state); Check(state[0].Position.Y is > 27 and < 30, "Resident boundary revision wakes and moves supported body.");
        store.RemoveShape(support);
        for (var i = 0; i < 30; i++) store.Simulate(1f / 60, new(0, 980));
        store.Read([body], state); Check(state[0].Position.Y > 60, "Resident boundary removal retires contacts and wakes body.");
        Reject<ArgumentException>(() => store.RemoveShape(support));
    }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
    private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
}
