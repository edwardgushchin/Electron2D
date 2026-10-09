using System.Diagnostics;
using Electron2D;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class PhysicsCCDTests
{
    internal static void Run(bool gpu = false)
    {
        VerifyAPI(); VerifyScene(gpu); VerifyWall(gpu); VerifyFamilies(gpu); VerifyModes(gpu); VerifyMoving(gpu); VerifyRotation(gpu); VerifyBounce(gpu); VerifyJointMotion(gpu); VerifyForces(gpu); VerifySleepingMotor(gpu); VerifyFilters(gpu); VerifyChain(gpu); VerifyAllocation(gpu);
        Console.WriteLine($"Public per-body CCD passed on {(gpu ? "CPU host/GPU stages" : "CPU")}: modes, lifetime, geometry, relative motion and allocation.");
    }
    private sealed class Fixture : IDisposable
    {
        internal readonly RID Space = PhysicsServer.SpaceCreate();
        private readonly List<RID> _bodies = [];
        internal Fixture(bool gpu = false)
        {
            PhysicsServer.SpaceSetActive(Space, true);
            var fields = PhysicsServer.Service.GetSceneSpace(Space).DefaultAreaFields;
            fields.Gravity = 0; fields.LinearDamp = fields.AngularDamp = 0;
            if (gpu) PhysicsServer.Service.GetSceneSpace(Space).EnableGPUSolver();
        }
        internal RID Add(Shape shape, Vector2 position = default, Vector2 velocity = default, CCDMode ccd = CCDMode.Disabled, Mode mode = Mode.Rigid)
        {
            var body = PhysicsServer.BodyCreate(); _bodies.Add(body); PhysicsServer.BodySetMode(body, mode);
            PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetTransform(body, new(0, position));
            PhysicsServer.BodySetLinearVelocity(body, velocity); PhysicsServer.BodySetCanSleep(body, false);
            PhysicsServer.BodySetContinuousCollisionDetectionMode(body, ccd); PhysicsServer.BodySetSpace(body, Space); return body;
        }
        internal void Step() => PhysicsServer.SpaceStep(Space, .02);
        public void Dispose() { foreach (var body in _bodies) PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(Space); }
    }
    private static void VerifyAPI()
    {
        using var body = new RigidBody { Name = "Projectile" };
        Check(body.ContinuousCD == CCDMode.Disabled, "Scene default is discrete.");
        body.ContinuousCD = CCDMode.CastShape; Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(body.GetRID()) == CCDMode.CastShape, "Scene/server share one authored mode.");
        PhysicsServer.BodySetContinuousCollisionDetectionMode(body.GetRID(), CCDMode.CastRay); Check(body.ContinuousCD == CCDMode.CastRay, "Server writes reach scene properties.");
        Reject<ArgumentOutOfRangeException>(() => body.ContinuousCD = (CCDMode)99);
        Check(body.ContinuousCD == CCDMode.CastRay, "Invalid writes are atomic.");
        using var scene = new PackedScene(); scene.Pack(body); using var copy = (RigidBody)scene.Instantiate();
        Check(copy.ContinuousCD == CCDMode.CastRay, "CCD is stored in packed scenes.");
        using var circle = new CircleShape { Radius = 1 }; using var f = new Fixture(); var server = f.Add(circle, ccd: CCDMode.CastShape);
        PhysicsServer.BodySetSleeping(server, true); PhysicsServer.BodySetContinuousCollisionDetectionMode(server, CCDMode.CastRay);
        Check(!PhysicsServer.BodyGetSleeping(server), "A changed CCD policy wakes a body.");
        PhysicsServer.BodySetSpace(server, default); PhysicsServer.BodySetMode(server, Mode.Static); PhysicsServer.BodySetSpace(server, f.Space);
        Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(server) == CCDMode.CastRay, "Mode survives space and role changes.");
        Task.Run(() => Reject<InvalidOperationException>(() => PhysicsServer.BodyGetContinuousCollisionDetectionMode(server))).GetAwaiter().GetResult();
        var area = PhysicsServer.AreaCreate(); try { Reject<ArgumentException>(() => PhysicsServer.BodySetContinuousCollisionDetectionMode(area, CCDMode.CastShape)); } finally { PhysicsServer.FreeRID(area); }
        body.Dispose(); Reject<ObjectDisposedException>(() => _ = body.ContinuousCD);
    }
    private static void VerifyScene(bool gpu)
    {
        using var ball = new CircleShape { Radius = 1 }; using var wallShape = new RectangleShape { Size = new(.2f, 100) };
        using var root = new Node(); var body = new RigidBody
        {
            Name = "Projectile",
            ContinuousCD = CCDMode.CastShape,
            GravityScale = 0,
            CanSleep = false,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace,
            LinearVelocity = new(3000, 0),
            ContactMonitor = true,
            MaxContactsReported = 8
        };
        body.AddChild(new CollisionShape { Shape = ball }); var wall = new StaticBody { Name = "Wall", Position = new(20, 0) };
        wall.AddChild(new CollisionShape { Shape = wallShape }); root.AddChild(body); root.AddChild(wall);
        using var tree = new SceneTree(root); if (gpu) body.Space!.EnableGPUSolver(); var entered = 0; body.BodyEntered += _ => entered++;
        tree.PhysicsFrame(.02);
        Check(body.Position.X < 20 && entered == 1 && body.GetContactCount() > 0, "Scene CCD uses ordinary contact snapshots and emits one object entry.");
        body.Freeze = true; body.ContinuousCD = CCDMode.CastRay; body.Freeze = false;
        root.RemoveChild(body); root.AddChild(body);
        Check(body.ContinuousCD == CCDMode.CastRay, "Freeze and scene reentry retain configured CCD.");
    }
    private static void VerifyFamilies(bool gpu)
    {
        var points = new Vector2[12]; for (var i = 0; i < points.Length; i++) points[i] = new Vector2(10, 0).Rotated(i * Mathf.Tau / points.Length);
        Shape[] shapes = [new CapsuleShape { Radius = 1, Height = 4 }, new RectangleShape { Size = new(2, 4) },
            new ConvexPolygonShape { Points = points }, new SegmentShape { A = new(0, -2), B = new(0, 2) },
            new ConcavePolygonShape { Segments = [new(0, -2), new(0, 2)] }];
        using var wall = new RectangleShape { Size = new(.2f, 100) };
        try
        {
            foreach (var shape in shapes)
            {
                using var f = new Fixture(gpu); var a = f.Add(shape, velocity: new(3000, 0), ccd: CCDMode.CastShape); f.Add(wall, new(20, 0), mode: Mode.Static);
                f.Step(); Check(PhysicsServer.BodyGetTransform(a).Origin.X < 20.1f, $"Complete CCD geometry for {shape.GetType().Name}");
            }
        }
        finally { foreach (var shape in shapes) shape.Dispose(); }
    }
    private static void VerifyWall(bool gpu)
    {
        using var circle = new CircleShape { Radius = 1 }; using var wall = new RectangleShape { Size = new(.2f, 100) };
        foreach (var mode in Enum.GetValues<CCDMode>())
        {
            using var f = new Fixture(gpu); var a = f.Add(circle, velocity: new(3000, 0), ccd: mode); f.Add(wall, new(20, 0), mode: Mode.Static);
            PhysicsServer.BodySetMaxContactsReported(a, 4); using var state = PhysicsServer.BodyGetDirectState(a)!;
            f.Step(); Console.WriteLine($"CPU CCD {mode}: {state.Transform.Origin}, {state.LinearVelocity}");
            if (mode == CCDMode.Disabled) Check(state.Transform.Origin.X > 50, "Disabled has no implicit global CCD.");
            else
            {
                Check(state.Transform.Origin.X < 20, "CCD stops at a thin obstacle.");
                var contact = false;
                for (var i = 0; i < 4; i++) { f.Step(); contact |= state.GetContactCount() > 0; }
                Check(state.Transform.Origin.X < 20.1f && state.LinearVelocity.X < 5 && contact, $"Constraint solver stops residual motion and publishes real contacts: {state.Transform.Origin}, {state.LinearVelocity}, {contact}.");
            }
        }
    }
    private static void VerifyModes(bool gpu)
    {
        using var box = new RectangleShape { Size = new(2, 8) }; using var corner = new RectangleShape { Size = new(.2f, .4f) };
        foreach (var mode in new[] { CCDMode.CastRay, CCDMode.CastShape })
        {
            using var f = new Fixture(gpu); var a = f.Add(box, velocity: new(3000, 0), ccd: mode); var b = f.Add(corner, new(20, 3), mode: Mode.Static);
            PhysicsServer.BodySetMaxContactsReported(a, 4); f.Step(); var x = PhysicsServer.BodyGetTransform(a).Origin.X;
            using var state = PhysicsServer.BodyGetDirectState(a)!;
            Check(mode == CCDMode.CastRay ? x > 50 && state.LinearVelocity.X == 3000 :
                state.LinearVelocity.X < 2999 && MathF.Abs(state.AngularVelocity) > 1 && state.GetContactCount() > 0,
                $"Ray/shape distinction {mode}: x={x}, velocity={state.LinearVelocity}, angular={state.AngularVelocity}, contacts={state.GetContactCount()}");
            PhysicsServer.BodySetTransform(a, Transform.Identity); PhysicsServer.BodySetLinearVelocity(a, new(3000, 0)); PhysicsServer.BodyAddCollisionException(b, a); f.Step();
            Check(PhysicsServer.BodyGetTransform(a).Origin.X > 50, "Either endpoint's explicit exception rejects CCD.");
        }
    }
    private static void VerifyMoving(bool gpu)
    {
        using var circle = new CircleShape { Radius = 1 };
        foreach (var peer in new[] { CCDMode.Disabled, CCDMode.CastRay, CCDMode.CastShape })
        {
            using var f = new Fixture(gpu); var a = f.Add(circle, new(-20, 0), new(3000, 0), CCDMode.CastShape); var b = f.Add(circle, new(20, 0), new(-3000, 0), peer);
            f.Step(); Check(PhysicsServer.BodyGetTransform(a).Origin.X < PhysicsServer.BodyGetTransform(b).Origin.X, "Relative sweeps prevent exchanging sides.");
            for (var i = 0; i < 3; i++) f.Step();
            Check(PhysicsServer.BodyGetTransform(a).Origin.X < PhysicsServer.BodyGetTransform(b).Origin.X && PhysicsServer.BodyGetContinuousCollisionDetectionMode(b) == peer, "Moving peers keep their policy and react through contacts.");
        }
    }
    private static void VerifyChain(bool gpu)
    {
        using var ball = new CircleShape { Radius = 1 }; using var wall = new RectangleShape { Size = new(.2f, 100) };
        using var f = new Fixture(gpu); var front = f.Add(ball, velocity: new(3000, 0), ccd: CCDMode.CastShape);
        var back = f.Add(ball, new(-20, 0), new(3000, 0)); f.Add(wall, new(20, 0), mode: Mode.Static);
        f.Step(); var a = PhysicsServer.BodyGetTransform(front).Origin; var b = PhysicsServer.BodyGetTransform(back).Origin;
        Check(b.X < a.X, $"A stopped CCD body must not be crossed by a following peer: front={a}, back={b}");
        for (var i = 0; i < 4; i++) { f.Step(); Check(PhysicsServer.BodyGetTransform(back).Origin.X < PhysicsServer.BodyGetTransform(front).Origin.X, "Chained impact remains ordered after subsequent solver steps."); }
    }
    private static void VerifyRotation(bool gpu)
    {
        using var rod = new RectangleShape { Size = new(40, 1) }; using var target = new CircleShape { Radius = .6f };
        using var f = new Fixture(gpu); var a = f.Add(rod, ccd: CCDMode.CastShape); f.Add(target, new(14, 8), mode: Mode.Static);
        PhysicsServer.BodySetMaxContactsReported(a, 4); PhysicsServer.BodySetAngularVelocity(a, 60); f.Step();
        using var state = PhysicsServer.BodyGetDirectState(a)!;
        Check(state.GetContactCount() > 0 && MathF.Abs(state.AngularVelocity - 60) > 1,
            $"A rotating contour hits the intermediate arc and receives a constraint impulse: angle={state.Transform.Rotation}, omega={state.AngularVelocity}, contacts={state.GetContactCount()}");
    }
    private static void VerifyBounce(bool gpu)
    {
        using var ball = new CircleShape { Radius = 1 }; using var wall = new RectangleShape { Size = new(.2f, 100) };
        foreach (var mode in new[] { CCDMode.CastRay, CCDMode.CastShape })
            foreach (var bias in new[] { 0f, .8f, 1f })
            {
                using var f = new Fixture(gpu); PhysicsServer.SpaceSetContactDefaultBias(f.Space, bias); var a = f.Add(ball, velocity: new(3000, 0), ccd: mode); var b = f.Add(wall, new(20, 0), mode: Mode.Static);
                PhysicsServer.BodySetBounce(a, .5f); PhysicsServer.BodySetBounce(b, .5f); PhysicsServer.BodySetFriction(a, 0); PhysicsServer.BodySetFriction(b, 0);
                PhysicsServer.BodySetMaxContactsReported(a, 8); f.Step(); using var state = PhysicsServer.BodyGetDirectState(a)!;
                var impulse = Vector2.Zero; for (var i = 0; i < state.GetContactCount(); i++) impulse += state.GetContactImpulse(i);
                Console.WriteLine($"CCD bounce {mode}, bias={bias}: {state.Transform.Origin}, {state.LinearVelocity}, impulse={impulse}");
                Check(MathF.Abs(state.LinearVelocity.X + 3000) < 1 && state.Transform.Origin.X < -15,
                    "Restitution preserves incoming speed and consumes the rest of the tick.");
                Check(impulse.DistanceTo(new(-6000, 0)) < 2, "The frame contact snapshot contains the complete physical impulse.");
            }
    }
    private static void VerifyJointMotion(bool gpu)
    {
        using var rod = new RectangleShape { Size = new(40, 1) }; using var target = new CircleShape { Radius = .6f };
        using var f = new Fixture(gpu); var a = f.Add(rod, ccd: CCDMode.CastShape); f.Add(target, new(14, 8), mode: Mode.Static);
        PhysicsServer.BodySetInertia(a, 1); PhysicsServer.BodySetMaxContactsReported(a, 8);
        var pin = PhysicsServer.JointCreate();
        try
        {
            PhysicsServer.JointMakePin(pin, Vector2.Zero, a); PhysicsServer.PinJointSetMotorTargetVelocity(pin, -60);
            PhysicsServer.PinJointSetMotorMaxTorque(pin, 2); PhysicsServer.PinJointSetMotorEnabled(pin, true);
            f.Step(); using var state = PhysicsServer.BodyGetDirectState(a)!;
            Check(state.GetContactCount() > 0 && state.Transform.Rotation < .65f,
                $"CCD observes motion created by a motor during the solve: angle={state.Transform.Rotation}, contacts={state.GetContactCount()}");
            Check(PhysicsServer.PinJointGetMotorMaxTorque(pin) == 2, "Internal interval budgets do not mutate authored motor settings.");
        }
        finally { PhysicsServer.FreeRID(pin); }
    }
    private static void VerifyForces(bool gpu)
    {
        using var ball = new CircleShape { Radius = 1 }; using var wall = new RectangleShape { Size = new(.2f, 100) };
        using var f = new Fixture(gpu); var a = f.Add(ball, velocity: new(3000, 0), ccd: CCDMode.CastShape); var b = f.Add(wall, new(20, 0), mode: Mode.Static);
        PhysicsServer.BodySetBounce(a, .5f); PhysicsServer.BodySetBounce(b, .5f);
        var free = f.Add(ball, new(0, 1000)); PhysicsServer.BodySetConstantForce(free, new(100, 0));
        var motor = f.Add(ball, new(0, 2000)); PhysicsServer.BodySetInertia(motor, 1);
        var pin = PhysicsServer.JointCreate(); var limitedPin = PhysicsServer.JointCreate();
        var limited = f.Add(ball, new(0, 3000), new(1000, 0));
        try
        {
            PhysicsServer.JointMakePin(pin, new(0, 2000), motor); PhysicsServer.PinJointSetMotorTargetVelocity(pin, 1000);
            PhysicsServer.PinJointSetMotorMaxTorque(pin, .002f); PhysicsServer.PinJointSetMotorEnabled(pin, true);
            PhysicsServer.JointMakePin(limitedPin, new(0, 3000), limited); PhysicsServer.JointSetMaxForce(limitedPin, 50);
            f.Step(); var linear = PhysicsServer.BodyGetLinearVelocity(free).X; var angular = PhysicsServer.BodyGetAngularVelocity(motor);
            Check(MathF.Abs(linear - 2) < .0001f, $"Split impacts do not repeat or omit constant forces: {linear}");
            Check(MathF.Abs(PhysicsServer.BodyGetLinearVelocity(limited).X - 999) < .002f, "The shared linear joint force budget is applied once over the complete tick.");
            Check(MathF.Abs(angular + .4f) < .001f, $"Motor impulse budget spans the whole nominal interval: {angular}");
        }
        finally { PhysicsServer.FreeRID(pin); PhysicsServer.FreeRID(limitedPin); }
    }
    private static void VerifySleepingMotor(bool gpu)
    {
        using var ball = new CircleShape { Radius = 1 }; using var f = new Fixture(gpu);
        var a = f.Add(ball, new(-100, 0), ccd: CCDMode.CastShape); var motor = f.Add(ball, new(20, 0));
        PhysicsServer.BodySetInertia(motor, 1); PhysicsServer.BodySetCanSleep(motor, true);
        PhysicsServer.BodySetFriction(a, 0); PhysicsServer.BodySetFriction(motor, 0);
        var pin = PhysicsServer.JointCreate(); var world = Box2D.NET.B2Worlds.b2GetWorldFromId(PhysicsServer.Service.GetSceneSpace(f.Space).WorldID);
        var previous = world.preSolveFcn; var at = -1f;
        try
        {
            PhysicsServer.JointMakePin(pin, new(20, 0), motor); PhysicsServer.PinJointSetMotorTargetVelocity(pin, 1000);
            PhysicsServer.PinJointSetMotorMaxTorque(pin, .002f); PhysicsServer.PinJointSetMotorEnabled(pin, true);
            f.Step(); // Retain a nonzero motor history, then sleep the whole joint.
            PhysicsServer.BodySetSleeping(motor, true); Check(PhysicsServer.BodyGetSleeping(motor), "Motor fixture is asleep before the impact.");
            PhysicsServer.BodySetTransform(a, Transform.Identity); PhysicsServer.BodySetLinearVelocity(a, new(3000, 0));
            var backend = PhysicsServer.Service.BodyRuntime(a).Backend;
            for (var i = 0; i < backend.Shapes.Count; i++) Box2D.NET.B2Shapes.b2Shape_EnablePreSolveEvents(backend.Shapes[i], true);
            world.preSolveFcn = (first, second, point, normal, context) =>
            {
                // Observe actual first-contact time from constant pre-impact translation, without changing filtering.
                if (at < 0) at = Box2D.NET.B2Bodies.b2Body_GetPosition(backend.BodyID).X * 100 / 3000;
                return previous(first, second, point, normal, context);
            };
            f.Step(); var actual = PhysicsServer.BodyGetAngularVelocity(motor); var expected = -20 * (.02f - at);
            Check(at > .005f && MathF.Abs(actual - expected) < .003f,
                $"A newly woken motor receives only its remaining-time budget; sleeping history spends none: time={at}, angular={actual}, expected={expected}");
        }
        finally { world.preSolveFcn = previous; PhysicsServer.FreeRID(pin); }
    }
    private static void VerifyFilters(bool gpu)
    {
        using var ball = new CircleShape { Radius = 1 }; using var floor = new RectangleShape { Size = new(200, .2f) };
        foreach (var fromBelow in new[] { false, true })
        {
            using var f = new Fixture(gpu); var a = f.Add(ball, new(0, fromBelow ? 40 : 0), new(0, fromBelow ? -3000 : 3000), CCDMode.CastShape);
            var b = f.Add(floor, new(0, 20), mode: Mode.Static); PhysicsServer.BodySetShapeAsOneWayCollision(b, 0, true, 1);
            f.Step(); var y = PhysicsServer.BodyGetTransform(a).Origin.Y;
            Check(fromBelow ? y < 0 : y < 20, $"One-way CCD side: below={fromBelow}, y={y}");
        }
        using (var f = new Fixture(gpu))
        {
            var a = f.Add(ball, velocity: new(0, 3000), ccd: CCDMode.CastShape); var b = f.Add(floor, new(0, 20), mode: Mode.Static);
            PhysicsServer.BodySetCollisionMask(b, 0); f.Step(); Check(PhysicsServer.BodyGetTransform(a).Origin.Y > 50, "CCD requires reciprocal masks.");
        }
    }
    private static void VerifyAllocation(bool gpu)
    {
        using var circle = new CircleShape { Radius = 1 }; using var wall = new RectangleShape { Size = new(.2f, 200) };
        using var f = new Fixture(gpu); var a = f.Add(circle, velocity: new(3000, 0), ccd: CCDMode.CastShape); f.Add(wall, new(20, 0), mode: Mode.Static);
        for (var i = 0; i < 64; i++) { PhysicsServer.BodySetTransform(a, Transform.Identity); PhysicsServer.BodySetLinearVelocity(a, new(3000, 0)); f.Step(); }
        var times = new double[128]; var bytes = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < times.Length; i++)
        {
            var watch = Stopwatch.GetTimestamp();
            PhysicsServer.BodySetTransform(a, Transform.Identity); PhysicsServer.BodySetLinearVelocity(a, new(3000, 0)); f.Step();
            times[i] = Stopwatch.GetElapsedTime(watch).TotalMilliseconds;
        }
        bytes = GC.GetTotalAllocatedBytes(true) - bytes; Array.Sort(times);
        Check(bytes == 0, $"Warmed CCD steps allocate {bytes} bytes");
        Console.WriteLine($"Public CCD: 64 warmup/128 reset-active steps, p50/p95/p99={times[64]:F4}/{times[121]:F4}/{times[126]:F4} ms/step, {bytes} all-thread managed B.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
}
