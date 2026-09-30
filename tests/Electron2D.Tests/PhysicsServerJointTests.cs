using Electron2D;

internal static class PhysicsServerJointTests
{
    internal static void Run()
    {
        VerifyServerRolesAndReplacement();
        VerifySceneIdentityAndProjection();
        VerifyLifetimeAndRollback();
        VerifyJointExceptionContributions();
        VerifyMixedBodiesAndPhaseGuards();
        Console.WriteLine("Physics joint RID, server/scene roles, native response, ownership and lifetime checks passed.");
    }

    private static RID CreateBody(PhysicsServer server, RID shape, Vector2 position, bool stationary = false, float mass = 1)
    {
        var body = server.BodyCreate();
        server.BodyAddShape(body, shape);
        server.BodySetTransform(body, new(0, Vector2.One, 0, position));
        server.BodySetMass(body, mass);
        server.BodySetGravityScale(body, 0);
        server.BodySetLinearDampMode(body, RigidBody.DampMode.Replace);
        server.BodySetAngularDampMode(body, RigidBody.DampMode.Replace);
        if (stationary) server.BodySetMode(body, PhysicsServer.BodyMode.Static);
        return body;
    }

    private static void VerifyServerRolesAndReplacement()
    {
        var server = PhysicsServer.Instance;
        var space = server.SpaceCreate(); var shape = server.CircleShapeCreate();
        var first = CreateBody(server, shape, Vector2.Zero, stationary: true);
        var second = CreateBody(server, shape, new(0, 100), mass: 2);
        var joint = server.JointCreate();
        try
        {
            Check(joint.IsValid() && server.JointGetType(joint) == PhysicsServer.JointType.Empty,
                "JointCreate produces a live empty identity.");
            Reject<ArgumentException>(() => server.PinJointGetMotorEnabled(joint));
            server.JointMakePin(joint, new(10_000_000, 0), first);
            Reject<ArgumentOutOfRangeException>(() => server.JointMakePin(joint, new(10_000_001, 0), first));
            Check(server.JointGetType(joint) == PhysicsServer.JointType.Pin,
                "The backend local-anchor extent is inclusive and invalid replacement preserves the role.");
            server.JointClear(joint);
            server.JointMakeDampedSpring(joint, Vector2.Zero, new(0, 100), first, second);
            Check(server.JointGetType(joint) == PhysicsServer.JointType.DampedSpring &&
                  server.DampedSpringJointGetRestLength(joint) == 100 && server.DampedSpringJointGetStiffness(joint) == 20 &&
                  server.DampedSpringJointGetDamping(joint) == 1.5f,
                "Detached spring configuration preserves backend factory defaults and sampled rest distance.");
            server.DampedSpringJointSetRestLength(joint, 50); server.DampedSpringJointSetDamping(joint, 0);
            server.BodySetSpace(first, space); server.BodySetSpace(second, space);
            server.SpaceStep(space, 1d / 60);
            var state = server.BodyGetDirectState(second)!;
            Check(MathF.Abs(state.LinearVelocity.Y + 1000f / 120) < 0.001f,
                "A server-only spring executes the same Hooke/mass response as the scene role.");
            server.DampedSpringJointSetDamping(joint, 1);
            for (var step = 0; step < 40; step++) server.SpaceStep(space, 1d / 120);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var step = 0; step < 64; step++)
            {
                server.DampedSpringJointSetStiffness(joint, 20);
                _ = server.DampedSpringJointGetDamping(joint);
                server.SpaceStep(space, 1d / 120);
            }
            Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
                "Warmed typed server joint settings and an active spring solve allocate no managed bytes.");

            state.LinearVelocity = Vector2.Zero; state.AngularVelocity = 0;
            server.BodySetTransform(second, new(0, Vector2.One, 0, new(0, 25)));
            server.JointMakeGroove(joint, Vector2.Zero, new(0, 50), new(0, 25), first, second);
            state.LinearVelocity = new(80, 120); state.AngularVelocity = 2;
            for (var step = 0; step < 40; step++) server.SpaceStep(space, 1d / 60);
            var pose = server.BodyGetTransform(second);
            Check(server.JointGetType(joint) == PhysicsServer.JointType.Groove &&
                  MathF.Abs(pose.Origin.X) < 4 && pose.Origin.Y is > 45 and < 55 && pose.Rotation > 0.5f,
                "The same RID can become a real finite groove with free rotation.");

            state.LinearVelocity = Vector2.Zero; state.AngularVelocity = 0;
            server.BodySetTransform(second, new(0, Vector2.One, 0, Vector2.Zero));
            server.JointMakePin(joint, Vector2.Zero, first, second);
            server.PinJointSetAngularLimitLower(joint, -0.4f); server.PinJointSetAngularLimitUpper(joint, 0.4f);
            server.PinJointSetAngularLimitEnabled(joint, true);
            server.PinJointSetMotorTargetVelocity(joint, 3); server.PinJointSetMotorMaxTorque(joint, 10);
            server.PinJointSetMotorEnabled(joint, true);
            for (var step = 0; step < 60; step++) server.SpaceStep(space, 1d / 60);
            Check(server.JointGetType(joint) == PhysicsServer.JointType.Pin &&
                  server.BodyGetTransform(second).Rotation is > 0.2f and < 0.55f &&
                  server.PinJointGetAngularLimitLower(joint) == -0.4f && server.PinJointGetAngularLimitUpper(joint) == 0.4f &&
                  server.PinJointGetAngularLimitEnabled(joint) && server.PinJointGetMotorEnabled(joint) &&
                  server.PinJointGetMotorTargetVelocity(joint) == 3 && server.PinJointGetMotorMaxTorque(joint) == 10,
                "Typed server pin flags, limits and motor change the actual solver response.");

            var pivot = new Vector2(100, 100);
            server.BodySetTransform(second, new(0, Vector2.One, 0, pivot));
            server.JointMakePin(joint, pivot, second);
            state.LinearVelocity = new(0, 100); state.AngularVelocity = 1;
            for (var step = 0; step < 30; step++) server.SpaceStep(space, 1d / 60);
            Check(server.BodyGetTransform(second).Origin.DistanceTo(pivot) < 1,
                "An empty second RID attaches a server pin to the fixed world.");
            server.JointDisableCollisionsBetweenBodies(joint, false);
            server.JointClear(joint);
            Check(server.JointGetType(joint) == PhysicsServer.JointType.Empty && !server.JointIsDisabledCollisionsBetweenBodies(joint),
                "Clear keeps identity and collision policy while removing the connection.");
            Reject<ArgumentException>(() => server.JointMakeDampedSpring(joint, Vector2.Zero, new(0, 10), second));
            Reject<ArgumentException>(() => server.JointMakeGroove(joint, Vector2.Zero, new(0, 10), Vector2.Zero));
        }
        finally
        {
            server.FreeRID(joint); server.FreeRID(first); server.FreeRID(second); server.FreeRID(shape); server.FreeRID(space);
        }
        Reject<ArgumentException>(() => server.JointGetType(joint));
    }

    private static void VerifySceneIdentityAndProjection()
    {
        using var shape = new CircleShape { Radius = 6 };
        var root = new Node();
        var first = new StaticBody { Name = "First" };
        var body = new RigidBody { Name = "Body", Position = new(0, 50), GravityScale = 0, CanSleep = false };
        body.AddChild(new CollisionShape { Shape = shape });
        var pin = new PinJoint { Name = "Pin", NodeA = "../First", NodeB = "../Body", Position = new(0, 50) };
        var spring = new DampedSpringJoint { Name = "Spring", NodeA = "../First", NodeB = "../Body", Stiffness = 0, Damping = 0 };
        var groove = new GrooveJoint { Name = "Groove", NodeA = "../First", NodeB = "../Body" };
        var pinRID = pin.GetRID(); var springRID = spring.GetRID(); var grooveRID = groove.GetRID();
        var server = PhysicsServer.Instance;
        Check(server.JointGetType(pinRID) == PhysicsServer.JointType.Empty && pinRID != springRID && springRID != grooveRID,
            "Each detached scene joint owns an independent stable RID.");
        root.AddChild(first); root.AddChild(body); root.AddChild(pin); root.AddChild(spring); root.AddChild(groove);
        using (var tree = new SceneTree(root))
        {
            Check(server.JointGetType(pinRID) == PhysicsServer.JointType.Pin &&
                  server.JointGetType(springRID) == PhysicsServer.JointType.DampedSpring &&
                  server.JointGetType(grooveRID) == PhysicsServer.JointType.Groove,
                "Scene configuration publishes all three concrete roles through the server identity.");
            server.PinJointSetAngularLimitLower(pinRID, -0.3f); pin.AngularLimitUpper = 0.4f;
            server.PinJointSetMotorTargetVelocity(pinRID, 2); pin.MotorEnabled = true;
            Check(pin.AngularLimitLower == -0.3f && server.PinJointGetAngularLimitUpper(pinRID) == 0.4f &&
                  pin.MotorTargetVelocity == 2 && server.PinJointGetMotorEnabled(pinRID),
                "Scene and server pin configuration share one mutable settings record.");
            Check(server.DampedSpringJointGetRestLength(springRID) == 50,
                "The server observes the resolved scene zero-rest fallback.");
            server.DampedSpringJointSetRestLength(springRID, 0);
            server.DampedSpringJointSetStiffness(springRID, 30); spring.Damping = 4;
            Check(server.DampedSpringJointGetRestLength(springRID) == 0 && spring.Stiffness == 30 &&
                  server.DampedSpringJointGetDamping(springRID) == 4,
                "Raw server spring rest zero is literal and coefficients remain bidirectional.");
            spring.RestLength = 0;
            Check(server.DampedSpringJointGetRestLength(springRID) == 50,
                "Writing scene rest zero restores the scene automatic relaxed-length policy.");
            server.JointMakeGroove(grooveRID, Vector2.Zero, new(30, 0), body.GlobalPosition, first.GetRID(), body.GetRID());
            Check(groove.HasServerOverride, "A raw same-role guide temporarily owns sampled geometry.");
            groove.Length = 60; tree.PhysicsFrame(1d / 60);
            Check(!groove.HasServerOverride && server.JointGetType(grooveRID) == PhysicsServer.JointType.Groove,
                "A scene guide length edit reclaims scene path/geometry after a raw server override.");
            server.JointDisableCollisionsBetweenBodies(pinRID, false);
            Check(!pin.DisableCollision, "Server collision policy is visible on the scene node.");
            Reject<InvalidOperationException>(() => server.FreeRID(pinRID));
            server.JointClear(pinRID); tree.PhysicsFrame(1d / 60);
            Check(pin.GetRID() == pinRID && server.JointGetType(pinRID) == PhysicsServer.JointType.Empty,
                "A server clear persists on a scene joint until a scene reconfiguration.");
            pin.NodeB = "../Missing"; pin.NodeB = "../Body"; tree.PhysicsFrame(1d / 60);
            Check(server.JointGetType(pinRID) == PhysicsServer.JointType.Pin && pin.GetRID() == pinRID,
                "Scene path edits rebuild the same RID after clear.");
            server.JointMakePin(pinRID, body.GlobalPosition, first.GetRID(), body.GetRID());
            Check(!pin.MotorEnabled && pin.AngularLimitLower == 0,
                "A server geometry replacement resets concrete parameters through the shared record.");
            Reject<InvalidOperationException>(() => server.JointMakeGroove(pinRID, Vector2.Zero, new(0, 10), Vector2.Zero,
                first.GetRID(), body.GetRID()));
            Check(server.JointGetType(pinRID) == PhysicsServer.JointType.Pin,
                "A scene owner cannot change its concrete public node role.");
            Reject<InvalidOperationException>(() => Task.Run(() => server.PinJointSetMotorEnabled(pinRID, true)).GetAwaiter().GetResult());
            root.RemoveChild(body);
            root.AddChild(body); tree.PhysicsFrame(1d / 60);
            Check(pin.GetRID() == pinRID && spring.GetRID() == springRID && groove.GetRID() == grooveRID,
                "Scene identities survive body and backend reentry.");
        }
        Reject<ArgumentException>(() => server.JointGetType(pinRID));
        Reject<ObjectDisposedException>(() => pin.GetRID());
    }

    private static void VerifyLifetimeAndRollback()
    {
        var server = PhysicsServer.Instance;
        var space = server.SpaceCreate(); var other = server.SpaceCreate(); var shape = server.CircleShapeCreate();
        var first = CreateBody(server, shape, Vector2.Zero, stationary: true);
        var second = CreateBody(server, shape, new(0, 20));
        var joint = server.JointCreate(); var area = server.AreaCreate();
        try
        {
            server.BodySetSpace(first, space); server.BodySetSpace(second, space);
            server.JointMakePin(joint, Vector2.Zero, first, second);
            Reject<ArgumentException>(() => server.JointMakePin(joint, Vector2.Zero, area, second));
            Reject<ArgumentException>(() => server.JointMakePin(joint, Vector2.Zero, first, first));
            Reject<ArgumentOutOfRangeException>(() => server.JointMakePin(joint, new(float.NaN, 0), first, second));
            Reject<ArgumentOutOfRangeException>(() => server.JointMakePin(joint, new(float.MaxValue, 0), first, second));
            Reject<ArgumentOutOfRangeException>(() => server.PinJointSetMotorTargetVelocity(joint, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => server.PinJointSetMotorMaxTorque(joint, -1));
            Reject<ArgumentException>(() => server.DampedSpringJointGetDamping(joint));
            server.PinJointSetAngularLimitLower(joint, -0.2f); server.PinJointSetAngularLimitUpper(joint, 0.2f);
            server.PinJointSetAngularLimitEnabled(joint, true);
            Reject<ArgumentOutOfRangeException>(() => server.PinJointSetAngularLimitLower(joint, 0.3f));
            Check(server.PinJointGetAngularLimitLower(joint) == -0.2f,
                "An invalid active limit preserves the stored and native lower bound.");
            Check(server.JointGetType(joint) == PhysicsServer.JointType.Pin && server.PinJointGetMotorTargetVelocity(joint) == 0,
                "Wrong kind, self-connection and nonfinite edits preserve the previous resource.");
            Reject<InvalidOperationException>(() => Task.Run(() => server.JointClear(joint)).GetAwaiter().GetResult());
            server.BodySetSpace(second, default); server.SpaceStep(space, 1d / 60);
            Reject<InvalidOperationException>(() => Task.Run(() => server.FreeRID(second)).GetAwaiter().GetResult());
            Task.Run(() =>
            {
                var workerSpace = server.SpaceCreate();
                try { Reject<InvalidOperationException>(() => server.BodySetSpace(second, workerSpace)); }
                finally { server.FreeRID(workerSpace); }
            }).GetAwaiter().GetResult();
            Check(!server.BodyGetSpace(second).IsValid(),
                "A detached body still honours the owner thread of its pending joint's other world.");
            Check(server.JointGetType(joint) == PhysicsServer.JointType.Pin,
                "A departing backend body suspends a server connection without discarding its configuration.");
            server.BodySetSpace(second, space); server.SpaceStep(space, 1d / 60);
            server.BodySetSpace(second, other);
            Reject<ArgumentException>(() => server.JointMakePin(joint, Vector2.Zero, first, second));
            server.SpaceStep(space, 1d / 60); server.SpaceStep(other, 1d / 60);
            server.BodySetSpace(second, space); server.SpaceStep(space, 1d / 60);
            Check(server.JointGetType(joint) == PhysicsServer.JointType.Pin,
                "Temporary foreign-space membership and return do not use stale native IDs.");
            server.FreeRID(second);
            Check(server.JointGetType(joint) == PhysicsServer.JointType.Empty,
                "Freeing a body clears every dependent joint before its RID can become stale.");
            var stale = second;
            second = CreateBody(server, shape, new(0, 20)); server.BodySetSpace(second, space);
            Reject<ArgumentException>(() => server.JointMakePin(joint, Vector2.Zero, first, stale));
            server.JointMakePin(joint, Vector2.Zero, first, second);
            server.FreeRID(space);
            Check(server.JointGetType(joint) == PhysicsServer.JointType.Pin,
                "World destruction releases native joints while retaining live body/local-frame configuration.");
            space = server.SpaceCreate();
            server.BodySetSpace(first, space); server.BodySetSpace(second, space); server.SpaceStep(space, 1d / 60);
            Check(server.JointGetType(joint) == PhysicsServer.JointType.Pin,
                "Detached bodies can reconnect the same joint RID in a replacement world.");
        }
        finally
        {
            server.FreeRID(joint); server.FreeRID(first); server.FreeRID(second);
            server.FreeRID(area); server.FreeRID(shape); server.FreeRID(space); server.FreeRID(other);
        }
    }

    private static void VerifyJointExceptionContributions()
    {
        using var shape = new CircleShape { Radius = 10 };
        var root = new Node();
        var first = new StaticBody { Name = "First" };
        first.AddChild(new CollisionShape { Shape = shape });
        var body = new RigidBody { Name = "Body", Position = new(0, 5), GravityScale = 0, CanSleep = false };
        body.AddChild(new CollisionShape { Shape = shape });
        root.AddChild(first); root.AddChild(body);
        using var tree = new SceneTree(root);
        var server = PhysicsServer.Instance;
        var one = server.JointCreate(); var two = server.JointCreate();
        try
        {
            server.JointMakePin(one, body.GlobalPosition, first.GetRID(), body.GetRID());
            server.JointMakeDampedSpring(two, Vector2.Zero, body.GlobalPosition, first.GetRID(), body.GetRID());
            server.DampedSpringJointSetStiffness(two, 0); server.DampedSpringJointSetDamping(two, 0);
            Check(body.GetCollisionExceptions() is [var peer] && ReferenceEquals(peer, first) &&
                  !body.TestMove(body.GlobalTransform, Vector2.Zero, recoveryAsCollision: true),
                "Joint collision suppression appears once in exception snapshots and participates in body motion tests.");
            server.JointDisableCollisionsBetweenBodies(one, false);
            body.RemoveCollisionExceptionWith(first);
            Check(body.GetCollisionExceptions().Length == 1 &&
                  !body.TestMove(body.GlobalTransform, Vector2.Zero, recoveryAsCollision: true),
                "Another joint's contribution survives both a sibling enable and explicit-exception removal.");
            server.JointDisableCollisionsBetweenBodies(two, false);
            Check(body.GetCollisionExceptions().Length == 0 &&
                  body.TestMove(body.GlobalTransform, Vector2.Zero, recoveryAsCollision: true),
                "The last joint contribution releases the pair for motion tests.");
            body.AddCollisionExceptionWith(first);
            server.JointDisableCollisionsBetweenBodies(one, true);
            Check(body.GetCollisionExceptions().Length == 1, "Explicit and derived exceptions are deduplicated in snapshots.");
            server.JointClear(one); server.JointClear(two);
            Check(body.GetCollisionExceptions().Length == 1 &&
                  !body.TestMove(body.GlobalTransform, Vector2.Zero, recoveryAsCollision: true),
                "Clearing joints leaves a separately owned explicit exception intact.");
            body.RemoveCollisionExceptionWith(first);
            Check(body.TestMove(body.GlobalTransform, Vector2.Zero, recoveryAsCollision: true),
                "Removing the remaining explicit entry restores motion response.");
        }
        finally { server.FreeRID(one); server.FreeRID(two); }
    }

    private static void VerifyMixedBodiesAndPhaseGuards()
    {
        using var shape = new CircleShape { Radius = 6 };
        var root = new Node();
        var first = new StaticBody { Name = "First" };
        var body = new RigidBody
        {
            Name = "Body",
            Position = new(0, 50),
            GravityScale = 0,
            CanSleep = false,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace
        };
        body.AddChild(new CollisionShape { Shape = shape });
        var emptySceneJoint = new PinJoint { Name = "Empty" };
        root.AddChild(first); root.AddChild(body); root.AddChild(emptySceneJoint);
        using var tree = new SceneTree(root);
        var server = PhysicsServer.Instance;
        var joint = server.JointCreate();
        try
        {
            server.JointMakeDampedSpring(joint, Vector2.Zero, body.GlobalPosition, first.GetRID(), body.GetRID());
            server.DampedSpringJointSetStiffness(joint, 0); server.DampedSpringJointSetDamping(joint, 0);
            body.LinearVelocity = new(60, 0); body.NotifyLocalTransformChanges = true;
            var rejected = false;
            Action<CanvasItem> callback = _ =>
            {
                Reject<InvalidOperationException>(() => server.JointClear(joint));
                Reject<InvalidOperationException>(() => server.FreeRID(joint));
                Reject<InvalidOperationException>(body.Dispose);
                Reject<InvalidOperationException>(emptySceneJoint.Dispose);
                rejected = true;
            };
            body.LocalTransformChanged += callback;
            tree.PhysicsFrame(1d / 60);
            body.LocalTransformChanged -= callback; body.NotifyLocalTransformChanges = false;
            Check(rejected && !body.IsDisposed && !emptySceneJoint.IsDisposed &&
                  server.JointGetType(joint) == PhysicsServer.JointType.DampedSpring,
                "In-step clear/free/disposal reject before changing joint or body ownership.");
            Action<Node> failing = _ => throw new InvalidOperationException("Joint body departure probe.");
            body.TreeExiting += failing;
            Reject<AggregateException>(() => root.RemoveChild(body));
            body.TreeExiting -= failing;
            Check(server.JointGetType(joint) == PhysicsServer.JointType.DampedSpring,
                "A failed departure callback retains reusable pending server joint configuration.");
            root.AddChild(body); tree.PhysicsFrame(1d / 60);
            root.RemoveChild(body);
            Reject<InvalidOperationException>(() => Task.Run(body.Dispose).GetAwaiter().GetResult());
            Check(!body.IsDisposed, "Pending scene-body dependencies protect disposal before object lifetime changes.");
            body.Dispose();
            Check(server.JointGetType(joint) == PhysicsServer.JointType.Empty,
                "Final scene-body disposal clears a server-owned dependent joint.");
        }
        finally { server.FreeRID(joint); }
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
