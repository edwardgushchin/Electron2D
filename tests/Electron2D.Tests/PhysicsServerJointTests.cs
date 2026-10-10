using Electron2D;

internal sealed class PhysicsServerJointTests(PhysicsServer.Backend backend)
{
    internal static void Run(PhysicsServer.Backend backend = PhysicsServer.Backend.CPU) => new PhysicsServerJointTests(backend).RunCore();
    private void RunCore()
    {
        VerifyServerRolesAndReplacement();
        VerifySceneIdentityAndProjection();
        VerifyLifetimeAndRollback();
        VerifyJointExceptionContributions();
        VerifyMixedBodiesAndPhaseGuards();
        VerifyFrameReattachmentCore(false);
        Console.WriteLine("Physics joint RID, server/scene roles, native response, ownership and lifetime checks passed.");
    }

    internal static void VerifyFrameReattachment(bool gpu) => new PhysicsServerJointTests(PhysicsServer.Backend.CPU).VerifyFrameReattachmentCore(gpu);
    private void VerifyFrameReattachmentCore(bool gpu)
    {
        var server = PhysicsServer.Service;
        var source = PhysicsServer.SpaceCreate(backend);
        var replacement = PhysicsServer.SpaceCreate(backend);
        var shape = PhysicsServer.CircleShapeCreate();
        var first = CreateBody(server, shape, Vector2.Zero, stationary: true);
        var second = CreateBody(server, shape, Vector2.Zero);
        var joint = PhysicsServer.JointCreate();
        try
        {
            PhysicsServer.SpaceSetActive(source, true); PhysicsServer.SpaceSetActive(replacement, true);
            if (gpu)
            {
                server.GetSceneSpace(source).EnableGPUSolver();
                server.GetSceneSpace(replacement).EnableGPUSolver();
            }
            var initialA = new Transform(0.37f, new Vector2(13.4f, 27.6f));
            var initialB = new Transform(-0.61f, new Vector2(80.3f, 80.7f));
            var anchor = new Vector2(45.2f, 60.8f);
            var localA = initialA.AffineInverse() * anchor;
            var localB = initialB.AffineInverse() * anchor;
            var initialAngle = initialB.Rotation - initialA.Rotation;
            PhysicsServer.BodySetTransform(first, initialA); PhysicsServer.BodySetTransform(second, initialB);
            PhysicsServer.BodySetCanSleep(second, false);
            PhysicsServer.BodySetSpace(first, source); PhysicsServer.BodySetSpace(second, source);
            PhysicsServer.JointMakePin(joint, anchor, first, second);
            PhysicsServer.PinJointSetAngularLimitLower(joint, -0.15f);
            PhysicsServer.PinJointSetAngularLimitUpper(joint, 0.15f);
            PhysicsServer.PinJointSetAngularLimitEnabled(joint, true);
            PhysicsServer.PinJointSetMotorTargetVelocity(joint, 0.8f);
            PhysicsServer.PinJointSetMotorEnabled(joint, true);
            VerifyMotion(source);

            var transfer = new Transform(1.1f, new Vector2(240.5f, -130.2f));
            var poseA = transfer * PhysicsServer.BodyGetTransform(first);
            var poseB = transfer * PhysicsServer.BodyGetTransform(second);
            PhysicsServer.BodySetSpace(first, default); PhysicsServer.BodySetSpace(second, default);
            PhysicsServer.BodySetTransform(first, poseA); PhysicsServer.BodySetTransform(second, poseB);
            PhysicsServer.BodySetLinearVelocity(second, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(second, 0);
            PhysicsServer.BodySetSpace(first, replacement); PhysicsServer.BodySetSpace(second, replacement);
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.JointMakePin(joint, new(float.MaxValue, 0), first, second));
            Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Pin && PhysicsServer.PinJointGetAngularLimitEnabled(joint) &&
                PhysicsServer.PinJointGetMotorTargetVelocity(joint) == 0.8f, "Reattachment and rejected replacement preserve joint identity and authored settings.");
            VerifyMotion(replacement);

            void VerifyMotion(RID space)
            {
                for (var i = 0; i < 120; i++) PhysicsServer.SpaceStep(space, 1d / 120);
                var a = PhysicsServer.BodyGetTransform(first);
                var b = PhysicsServer.BodyGetTransform(second);
                var angle = b.Rotation - a.Rotation - initialAngle;
                angle = MathF.Atan2(MathF.Sin(angle), MathF.Cos(angle));
                // One scene unit allows the 0.5-unit linear solver slop plus integration error;
                // 0.05 rad beyond the 0.15-rad limit allows the angular solver tolerance.
                Check((a * localA).DistanceTo(b * localB) < 1 && angle is > 0.02f and < 0.2f,
                    "Rotated off-center anchors and angular reference remain effective across world replacement.");
            }
        }
        finally
        {
            PhysicsServer.FreeRID(joint); PhysicsServer.FreeRID(first); PhysicsServer.FreeRID(second);
            PhysicsServer.FreeRID(shape); PhysicsServer.FreeRID(source); PhysicsServer.FreeRID(replacement);
        }
        Console.WriteLine($"Joint sampled frames and world replacement passed on {(gpu ? "CPU host/GPU stages" : backend.ToString())}.");
    }

    private static RID CreateBody(PhysicsServer server, RID shape, Vector2 position, bool stationary = false, float mass = 1)
    {
        var body = PhysicsServer.BodyCreate();
        PhysicsServer.BodyAddShape(body, shape);
        PhysicsServer.BodySetTransform(body, new(0, Vector2.One, 0, position));
        PhysicsServer.BodySetMass(body, mass);
        PhysicsServer.BodySetGravityScale(body, 0);
        PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace);
        PhysicsServer.BodySetAngularDampMode(body, RigidBody.DampMode.Replace);
        if (stationary) PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
        return body;
    }

    private void VerifyServerRolesAndReplacement()
    {
        var server = PhysicsServer.Service;
        var space = PhysicsServer.SpaceCreate(backend); PhysicsServer.SpaceSetActive(space, true); var shape = PhysicsServer.CircleShapeCreate();
        var first = CreateBody(server, shape, Vector2.Zero, stationary: true);
        var second = CreateBody(server, shape, new(0, 100), mass: 2);
        var joint = PhysicsServer.JointCreate();
        try
        {
            Check(joint.IsValid() && PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Empty,
                "JointCreate produces a live empty identity.");
            Reject<ArgumentException>(() => PhysicsServer.PinJointGetMotorEnabled(joint));
            PhysicsServer.JointMakePin(joint, new(10_000_000, 0), first);
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.JointMakePin(joint, new(10_000_001, 0), first));
            Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Pin,
                "The backend local-anchor extent is inclusive and invalid replacement preserves the role.");
            PhysicsServer.JointClear(joint);
            PhysicsServer.JointMakeDampedSpring(joint, Vector2.Zero, new(0, 100), first, second);
            Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.DampedSpring &&
                  PhysicsServer.DampedSpringJointGetRestLength(joint) == 100 && PhysicsServer.DampedSpringJointGetStiffness(joint) == 20 &&
                  PhysicsServer.DampedSpringJointGetDamping(joint) == 1.5f,
                "Detached spring configuration preserves backend factory defaults and sampled rest distance.");
            PhysicsServer.DampedSpringJointSetRestLength(joint, 50); PhysicsServer.DampedSpringJointSetDamping(joint, 0);
            PhysicsServer.BodySetSpace(first, space); PhysicsServer.BodySetSpace(second, space);
            PhysicsServer.SpaceStep(space, 1d / 60);
            var state = PhysicsServer.BodyGetDirectState(second)!;
            DampedSpringJointTests.VerifyInitialHooke(state.LinearVelocity.Y, 2, .5f);
            PhysicsServer.DampedSpringJointSetDamping(joint, 1);
            for (var step = 0; step < 40; step++) PhysicsServer.SpaceStep(space, 1d / 120);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var step = 0; step < 64; step++)
            {
                PhysicsServer.DampedSpringJointSetStiffness(joint, 20);
                _ = PhysicsServer.DampedSpringJointGetDamping(joint);
                PhysicsServer.SpaceStep(space, 1d / 120);
            }
            Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
                "Warmed typed server joint settings and an active spring solve allocate no managed bytes.");

            state.LinearVelocity = Vector2.Zero; state.AngularVelocity = 0;
            PhysicsServer.BodySetTransform(second, new(0, Vector2.One, 0, new(0, 25)));
            PhysicsServer.JointMakeGroove(joint, Vector2.Zero, new(0, 50), new(0, 25), first, second);
            state.LinearVelocity = new(80, 120); state.AngularVelocity = 2;
            for (var step = 0; step < 40; step++) PhysicsServer.SpaceStep(space, 1d / 60);
            var pose = PhysicsServer.BodyGetTransform(second);
            Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Groove &&
                  MathF.Abs(pose.Origin.X) < 4 && pose.Origin.Y is > 45 and < 55 && pose.Rotation > 0.5f,
                "The same RID can become a real finite groove with free rotation.");

            state.LinearVelocity = Vector2.Zero; state.AngularVelocity = 0;
            PhysicsServer.BodySetTransform(second, new(0, Vector2.One, 0, Vector2.Zero));
            PhysicsServer.JointMakePin(joint, Vector2.Zero, first, second);
            PhysicsServer.PinJointSetAngularLimitLower(joint, -0.4f); PhysicsServer.PinJointSetAngularLimitUpper(joint, 0.4f);
            PhysicsServer.PinJointSetAngularLimitEnabled(joint, true);
            PhysicsServer.PinJointSetMotorTargetVelocity(joint, 3); PhysicsServer.PinJointSetMotorMaxTorque(joint, 10);
            PhysicsServer.PinJointSetMotorEnabled(joint, true);
            for (var step = 0; step < 60; step++) PhysicsServer.SpaceStep(space, 1d / 60);
            Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Pin &&
                  PhysicsServer.BodyGetTransform(second).Rotation is > 0.2f and < 0.55f &&
                  PhysicsServer.PinJointGetAngularLimitLower(joint) == -0.4f && PhysicsServer.PinJointGetAngularLimitUpper(joint) == 0.4f &&
                  PhysicsServer.PinJointGetAngularLimitEnabled(joint) && PhysicsServer.PinJointGetMotorEnabled(joint) &&
                  PhysicsServer.PinJointGetMotorTargetVelocity(joint) == 3 && PhysicsServer.PinJointGetMotorMaxTorque(joint) == 10,
                "Typed server pin flags, limits and motor change the actual solver response.");

            var pivot = new Vector2(100, 100);
            PhysicsServer.BodySetTransform(second, new(0, Vector2.One, 0, pivot));
            PhysicsServer.JointMakePin(joint, pivot, second);
            state.LinearVelocity = new(0, 100); state.AngularVelocity = 1;
            for (var step = 0; step < 30; step++) PhysicsServer.SpaceStep(space, 1d / 60);
            Check(PhysicsServer.BodyGetTransform(second).Origin.DistanceTo(pivot) < 1,
                "An empty second RID attaches a server pin to the fixed world.");
            PhysicsServer.JointDisableCollisionsBetweenBodies(joint, false);
            PhysicsServer.JointClear(joint);
            Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Empty && !PhysicsServer.JointIsDisabledCollisionsBetweenBodies(joint),
                "Clear keeps identity and collision policy while removing the connection.");
            Reject<ArgumentException>(() => PhysicsServer.JointMakeDampedSpring(joint, Vector2.Zero, new(0, 10), second));
            Reject<ArgumentException>(() => PhysicsServer.JointMakeGroove(joint, Vector2.Zero, new(0, 10), Vector2.Zero));
        }
        finally
        {
            PhysicsServer.FreeRID(joint); PhysicsServer.FreeRID(first); PhysicsServer.FreeRID(second); PhysicsServer.FreeRID(shape); PhysicsServer.FreeRID(space);
        }
        Reject<ArgumentException>(() => PhysicsServer.JointGetType(joint));
    }

    private void VerifySceneIdentityAndProjection()
    {
        using var shape = new CircleShape { Radius = 6 };
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld };
        var first = new StaticBody { Name = "First" };
        var body = new RigidBody { Name = "Body", Position = new(0, 50), GravityScale = 0, CanSleep = false };
        body.AddChild(new CollisionShape { Shape = shape });
        var pin = new PinJoint { Name = "Pin", NodeA = "../First", NodeB = "../Body", Position = new(0, 50) };
        var spring = new DampedSpringJoint { Name = "Spring", NodeA = "../First", NodeB = "../Body", Stiffness = 0, Damping = 0 };
        var groove = new GrooveJoint { Name = "Groove", NodeA = "../First", NodeB = "../Body" };
        var pinRID = pin.GetRID(); var springRID = spring.GetRID(); var grooveRID = groove.GetRID();
        var server = PhysicsServer.Service;
        Check(PhysicsServer.JointGetType(pinRID) == PhysicsServer.JointType.Empty && pinRID != springRID && springRID != grooveRID,
            "Each detached scene joint owns an independent stable RID.");
        root.AddChild(first); root.AddChild(body); root.AddChild(pin); root.AddChild(spring); root.AddChild(groove);
        using (var tree = new SceneTree(root))
        {
            Check(PhysicsServer.JointGetType(pinRID) == PhysicsServer.JointType.Pin &&
                  PhysicsServer.JointGetType(springRID) == PhysicsServer.JointType.DampedSpring &&
                  PhysicsServer.JointGetType(grooveRID) == PhysicsServer.JointType.Groove,
                "Scene configuration publishes all three concrete roles through the server identity.");
            PhysicsServer.PinJointSetAngularLimitLower(pinRID, -0.3f); pin.AngularLimitUpper = 0.4f;
            PhysicsServer.PinJointSetMotorTargetVelocity(pinRID, 2); pin.MotorEnabled = true;
            Check(pin.AngularLimitLower == -0.3f && PhysicsServer.PinJointGetAngularLimitUpper(pinRID) == 0.4f &&
                  pin.MotorTargetVelocity == 2 && PhysicsServer.PinJointGetMotorEnabled(pinRID),
                "Scene and server pin configuration share one mutable settings record.");
            Check(PhysicsServer.DampedSpringJointGetRestLength(springRID) == 50,
                "The server observes the resolved scene zero-rest fallback.");
            PhysicsServer.DampedSpringJointSetRestLength(springRID, 0);
            PhysicsServer.DampedSpringJointSetStiffness(springRID, 30); spring.Damping = 4;
            Check(PhysicsServer.DampedSpringJointGetRestLength(springRID) == 0 && spring.Stiffness == 30 &&
                  PhysicsServer.DampedSpringJointGetDamping(springRID) == 4,
                "Raw server spring rest zero is literal and coefficients remain bidirectional.");
            spring.RestLength = 0;
            Check(PhysicsServer.DampedSpringJointGetRestLength(springRID) == 50,
                "Writing scene rest zero restores the scene automatic relaxed-length policy.");
            PhysicsServer.JointMakeGroove(grooveRID, Vector2.Zero, new(30, 0), body.GlobalPosition, first.GetRID(), body.GetRID());
            Check(groove.HasServerOverride, "A raw same-role guide temporarily owns sampled geometry.");
            groove.Length = 60; tree.PhysicsFrame(1d / 60);
            Check(!groove.HasServerOverride && PhysicsServer.JointGetType(grooveRID) == PhysicsServer.JointType.Groove,
                "A scene guide length edit reclaims scene path/geometry after a raw server override.");
            PhysicsServer.JointDisableCollisionsBetweenBodies(pinRID, false);
            Check(!pin.DisableCollision, "Server collision policy is visible on the scene node.");
            Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(pinRID));
            PhysicsServer.JointClear(pinRID); tree.PhysicsFrame(1d / 60);
            Check(pin.GetRID() == pinRID && PhysicsServer.JointGetType(pinRID) == PhysicsServer.JointType.Empty,
                "A server clear persists on a scene joint until a scene reconfiguration.");
            pin.NodeB = "../Missing"; pin.NodeB = "../Body"; tree.PhysicsFrame(1d / 60);
            Check(PhysicsServer.JointGetType(pinRID) == PhysicsServer.JointType.Pin && pin.GetRID() == pinRID,
                "Scene path edits rebuild the same RID after clear.");
            PhysicsServer.JointMakePin(pinRID, body.GlobalPosition, first.GetRID(), body.GetRID());
            Check(!pin.MotorEnabled && pin.AngularLimitLower == 0,
                "A server geometry replacement resets concrete parameters through the shared record.");
            Reject<InvalidOperationException>(() => PhysicsServer.JointMakeGroove(pinRID, Vector2.Zero, new(0, 10), Vector2.Zero,
                first.GetRID(), body.GetRID()));
            Check(PhysicsServer.JointGetType(pinRID) == PhysicsServer.JointType.Pin,
                "A scene owner cannot change its concrete public node role.");
            Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.PinJointSetMotorEnabled(pinRID, true)).GetAwaiter().GetResult());
            root.RemoveChild(body);
            root.AddChild(body); tree.PhysicsFrame(1d / 60);
            Check(pin.GetRID() == pinRID && spring.GetRID() == springRID && groove.GetRID() == grooveRID,
                "Scene identities survive body and backend reentry.");
        }
        Reject<ArgumentException>(() => PhysicsServer.JointGetType(pinRID));
        Reject<ObjectDisposedException>(() => pin.GetRID());
    }

    private void VerifyLifetimeAndRollback()
    {
        var server = PhysicsServer.Service;
        var space = PhysicsServer.SpaceCreate(backend); PhysicsServer.SpaceSetActive(space, true); var other = PhysicsServer.SpaceCreate(backend); PhysicsServer.SpaceSetActive(other, true); var shape = PhysicsServer.CircleShapeCreate();
        var first = CreateBody(server, shape, Vector2.Zero, stationary: true);
        var second = CreateBody(server, shape, new(0, 20));
        var joint = PhysicsServer.JointCreate(); var area = PhysicsServer.AreaCreate();
        try
        {
            PhysicsServer.BodySetSpace(first, space); PhysicsServer.BodySetSpace(second, space);
            PhysicsServer.JointMakePin(joint, Vector2.Zero, first, second);
            Reject<ArgumentException>(() => PhysicsServer.JointMakePin(joint, Vector2.Zero, area, second));
            Reject<ArgumentException>(() => PhysicsServer.JointMakePin(joint, Vector2.Zero, first, first));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.JointMakePin(joint, new(float.NaN, 0), first, second));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.JointMakePin(joint, new(float.MaxValue, 0), first, second));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.PinJointSetMotorTargetVelocity(joint, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.PinJointSetMotorMaxTorque(joint, -1));
            Reject<ArgumentException>(() => PhysicsServer.DampedSpringJointGetDamping(joint));
            PhysicsServer.PinJointSetAngularLimitLower(joint, -0.2f); PhysicsServer.PinJointSetAngularLimitUpper(joint, 0.2f);
            PhysicsServer.PinJointSetAngularLimitEnabled(joint, true);
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.PinJointSetAngularLimitLower(joint, 0.3f));
            Check(PhysicsServer.PinJointGetAngularLimitLower(joint) == -0.2f,
                "An invalid active limit preserves the stored and native lower bound.");
            Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Pin && PhysicsServer.PinJointGetMotorTargetVelocity(joint) == 0,
                "Wrong kind, self-connection and nonfinite edits preserve the previous resource.");
            Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.JointClear(joint)).GetAwaiter().GetResult());
            PhysicsServer.BodySetSpace(second, default); PhysicsServer.SpaceStep(space, 1d / 60);
            Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.FreeRID(second)).GetAwaiter().GetResult());
            Task.Run(() =>
            {
                var workerSpace = PhysicsServer.SpaceCreate(backend); PhysicsServer.SpaceSetActive(workerSpace, true);
                try { Reject<InvalidOperationException>(() => PhysicsServer.BodySetSpace(second, workerSpace)); }
                finally { PhysicsServer.FreeRID(workerSpace); }
            }).GetAwaiter().GetResult();
            Check(!PhysicsServer.BodyGetSpace(second).IsValid(),
                "A detached body still honours the owner thread of its pending joint's other world.");
            Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Pin,
                "A departing backend body suspends a server connection without discarding its configuration.");
            PhysicsServer.BodySetSpace(second, space); PhysicsServer.SpaceStep(space, 1d / 60);
            PhysicsServer.BodySetSpace(second, other);
            Reject<ArgumentException>(() => PhysicsServer.JointMakePin(joint, Vector2.Zero, first, second));
            PhysicsServer.SpaceStep(space, 1d / 60); PhysicsServer.SpaceStep(other, 1d / 60);
            PhysicsServer.BodySetSpace(second, space); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Pin,
                "Temporary foreign-space membership and return do not use stale native IDs.");
            PhysicsServer.FreeRID(second);
            Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Empty,
                "Freeing a body clears every dependent joint before its RID can become stale.");
            var stale = second;
            second = CreateBody(server, shape, new(0, 20)); PhysicsServer.BodySetSpace(second, space);
            Reject<ArgumentException>(() => PhysicsServer.JointMakePin(joint, Vector2.Zero, first, stale));
            PhysicsServer.JointMakePin(joint, Vector2.Zero, first, second);
            PhysicsServer.FreeRID(space);
            Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Pin,
                "World destruction releases native joints while retaining live body/local-frame configuration.");
            space = PhysicsServer.SpaceCreate(backend); PhysicsServer.SpaceSetActive(space, true);
            PhysicsServer.BodySetSpace(first, space); PhysicsServer.BodySetSpace(second, space); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Pin,
                "Detached bodies can reconnect the same joint RID in a replacement world.");
        }
        finally
        {
            PhysicsServer.FreeRID(joint); PhysicsServer.FreeRID(first); PhysicsServer.FreeRID(second);
            PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(shape); PhysicsServer.FreeRID(space); PhysicsServer.FreeRID(other);
        }
    }

    private void VerifyJointExceptionContributions()
    {
        using var shape = new CircleShape { Radius = 10 };
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld };
        var first = new StaticBody { Name = "First" };
        first.AddChild(new CollisionShape { Shape = shape });
        var body = new RigidBody { Name = "Body", Position = new(0, 5), GravityScale = 0, CanSleep = false };
        body.AddChild(new CollisionShape { Shape = shape });
        root.AddChild(first); root.AddChild(body);
        using var tree = new SceneTree(root);
        var server = PhysicsServer.Service;
        var one = PhysicsServer.JointCreate(); var two = PhysicsServer.JointCreate();
        try
        {
            PhysicsServer.JointMakePin(one, body.GlobalPosition, first.GetRID(), body.GetRID());
            PhysicsServer.JointMakeDampedSpring(two, Vector2.Zero, body.GlobalPosition, first.GetRID(), body.GetRID());
            PhysicsServer.DampedSpringJointSetStiffness(two, 0); PhysicsServer.DampedSpringJointSetDamping(two, 0);
            Check(body.GetCollisionExceptions() is [var peer] && ReferenceEquals(peer, first) &&
                  !body.TestMove(body.GlobalTransform, Vector2.Zero, recoveryAsCollision: true),
                "Joint collision suppression appears once in exception snapshots and participates in body motion tests.");
            Check(!first.TestMove(first.GlobalTransform, Vector2.Zero, recoveryAsCollision: true) &&
                !body.TestMove(new(0, new(-100, 0)), new(200, 0)), "Joint veto applies from either endpoint and to supplied-pose sweeps");
            for (var i = 0; i < 32; i++) body.TestMove(body.GlobalTransform, Vector2.Zero, recoveryAsCollision: true);
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread(); var allBytes = GC.GetTotalAllocatedBytes(true); var hit = false;
            for (var i = 0; i < 64; i++) hit |= body.TestMove(body.GlobalTransform, Vector2.Zero, recoveryAsCollision: true);
            allBytes = GC.GetTotalAllocatedBytes(true) - allBytes; ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBytes;
            Check(!hit && ownerBytes == 0 && allBytes == 0, $"Warm joint-filtered motion: {ownerBytes}/{allBytes} owner/all-thread bytes");
            PhysicsServer.JointDisableCollisionsBetweenBodies(one, false);
            body.RemoveCollisionExceptionWith(first);
            Check(body.GetCollisionExceptions().Length == 1 &&
                  !body.TestMove(body.GlobalTransform, Vector2.Zero, recoveryAsCollision: true),
                "Another joint's contribution survives both a sibling enable and explicit-exception removal.");
            PhysicsServer.JointDisableCollisionsBetweenBodies(two, false);
            Check(body.GetCollisionExceptions().Length == 0 &&
                  body.TestMove(body.GlobalTransform, Vector2.Zero, recoveryAsCollision: true),
                "The last joint contribution releases the pair for motion tests.");
            body.AddCollisionExceptionWith(first);
            PhysicsServer.JointDisableCollisionsBetweenBodies(one, true);
            Check(body.GetCollisionExceptions().Length == 1, "Explicit and derived exceptions are deduplicated in snapshots.");
            PhysicsServer.JointClear(one); PhysicsServer.JointClear(two);
            Check(body.GetCollisionExceptions().Length == 1 &&
                  !body.TestMove(body.GlobalTransform, Vector2.Zero, recoveryAsCollision: true),
                "Clearing joints leaves a separately owned explicit exception intact.");
            body.RemoveCollisionExceptionWith(first);
            Check(body.TestMove(body.GlobalTransform, Vector2.Zero, recoveryAsCollision: true),
                "Removing the remaining explicit entry restores motion response.");
        }
        finally { PhysicsServer.FreeRID(one); PhysicsServer.FreeRID(two); }
    }

    private void VerifyMixedBodiesAndPhaseGuards()
    {
        using var shape = new CircleShape { Radius = 6 };
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld };
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
        var server = PhysicsServer.Service;
        var joint = PhysicsServer.JointCreate();
        try
        {
            PhysicsServer.JointMakeDampedSpring(joint, Vector2.Zero, body.GlobalPosition, first.GetRID(), body.GetRID());
            PhysicsServer.DampedSpringJointSetStiffness(joint, 0); PhysicsServer.DampedSpringJointSetDamping(joint, 0);
            body.LinearVelocity = new(60, 0); body.NotifyLocalTransformChanges = true;
            var rejected = false;
            Action<CanvasItem> callback = _ =>
            {
                Reject<InvalidOperationException>(() => PhysicsServer.JointClear(joint));
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(joint));
                Reject<InvalidOperationException>(() => PhysicsServer.JointSetBias(joint, 0.5f));
                Reject<InvalidOperationException>(() => PhysicsServer.JointSetMaxForce(joint, 0));
                Reject<InvalidOperationException>(() => PhysicsServer.PinJointSetSoftness(emptySceneJoint.GetRID(), 2));
                Reject<InvalidOperationException>(() => emptySceneJoint.MaxBias = 0);
                Reject<InvalidOperationException>(body.Dispose);
                Reject<InvalidOperationException>(emptySceneJoint.Dispose);
                rejected = true;
            };
            body.LocalTransformChanged += callback;
            tree.PhysicsFrame(1d / 60);
            body.LocalTransformChanged -= callback; body.NotifyLocalTransformChanges = false;
            Check(rejected && !body.IsDisposed && !emptySceneJoint.IsDisposed &&
                  PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.DampedSpring,
                "In-step clear/free/disposal reject before changing joint or body ownership.");
            Action<Node> failing = _ => throw new InvalidOperationException("Joint body departure probe.");
            body.TreeExiting += failing;
            Reject<AggregateException>(() => root.RemoveChild(body));
            body.TreeExiting -= failing;
            Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.DampedSpring,
                "A failed departure callback retains reusable pending server joint configuration.");
            root.AddChild(body); tree.PhysicsFrame(1d / 60);
            root.RemoveChild(body);
            Reject<InvalidOperationException>(() => Task.Run(body.Dispose).GetAwaiter().GetResult());
            Check(!body.IsDisposed, "Pending scene-body dependencies protect disposal before object lifetime changes.");
            body.Dispose();
            Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Empty,
                "Final scene-body disposal clears a server-owned dependent joint.");
        }
        finally { PhysicsServer.FreeRID(joint); }
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
