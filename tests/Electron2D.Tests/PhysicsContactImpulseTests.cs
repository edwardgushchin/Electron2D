using Box2D.NET;
using Electron2D;

internal static class PhysicsContactImpulseTests
{
    internal static void Run(bool gpu = false)
    {
        using var lifetime = gpu ? new GPUPhysicsWorld() : null;
        VerifyLimits();
        foreach (var raw in new[] { false, true })
        {
            VerifyMomentum(gpu, raw, false);
            VerifyMomentum(gpu, raw, true);
            VerifySelection(gpu, raw);
        }
        VerifyPairAndSleep(gpu);
        VerifyTransient(gpu);
        VerifyRotatingNormal(gpu);
        VerifyWarm(gpu);
        Console.WriteLine($"Contact impulses passed on {(gpu ? "GPU" : "CPU")}: momentum, subdivided/transient contacts, rotating normals, paired signs, sleep reset, depth caps and zero warmed bytes.");
    }

    private static void VerifyLimits()
    {
        using var body = new RigidBody(); var raw = PhysicsServer.BodyCreate();
        try
        {
            foreach (var id in new[] { body.GetRID(), raw })
            {
                PhysicsServer.BodySetMaxContactsReported(id, 4095);
                Check(PhysicsServer.BodyGetMaxContactsReported(id) == 4095, "Largest accepted reporting limit.");
                foreach (var invalid in new[] { -1, 4096, int.MaxValue })
                {
                    try { PhysicsServer.BodySetMaxContactsReported(id, invalid); throw new Exception("Accepted an invalid reporting limit."); }
                    catch (ArgumentOutOfRangeException) { }
                    Check(PhysicsServer.BodyGetMaxContactsReported(id) == 4095, "Rejected limit preserves configuration.");
                }
                PhysicsServer.BodySetMaxContactsReported(id, 0);
                Check(PhysicsServer.BodyGetMaxContactsReported(id) == 0, "Zero disables reporting.");
            }
        }
        finally { PhysicsServer.FreeRID(raw); }
    }

    private static AnimatableBody Mover(Node root, Shape shape)
    {
        var mover = new AnimatableBody { Name = "Mover", Position = new(2000, -1000) };
        mover.AddChild(new CollisionShape { Shape = shape }); root.AddChild(mover); return mover;
    }

    private static void Configure(RID id)
    {
        PhysicsServer.BodySetMass(id, 2); PhysicsServer.BodySetGravityScale(id, 0);
        PhysicsServer.BodySetLinearDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(id, 0);
        PhysicsServer.BodySetAngularDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(id, 0);
        PhysicsServer.BodySetCanSleep(id, false); PhysicsServer.BodySetMaxContactsReported(id, 8);
    }

    private static Vector2 Impulse(PhysicsDirectBodyState state)
    {
        var impulse = Vector2.Zero;
        for (var i = 0; i < state.GetContactCount(); i++) impulse += state.GetContactImpulse(i);
        return impulse;
    }

    private static void VerifyMomentum(bool gpu, bool raw, bool subdivided)
    {
        using var root = new Node(); using var floorShape = new RectangleShape { Size = new(1000, 20) };
        using var shape = new RectangleShape { Size = new(20, 20) };
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100) }; floor.AddChild(new CollisionShape { Shape = floorShape }); root.AddChild(floor);
        var mover = Mover(root, shape);
        var body = new RigidBody { Name = "Body", Position = new(0, 80), LockRotation = true };
        body.AddChild(new CollisionShape { Shape = shape });
        if (!raw) root.AddChild(body);
        using var tree = new SceneTree(root); if (gpu) floor.Space!.EnableGPUSolver();
        var id = raw ? PhysicsServer.BodyCreate() : body.GetRID();
        try
        {
            if (raw)
            {
                PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.RigidLinear);
                PhysicsServer.BodyAddShape(id, shape.GetRID()); PhysicsServer.BodySetTransform(id, new(0, new(0, 80)));
                PhysicsServer.BodySetSpace(id, floor.GetWorld()!.Space);
            }
            Configure(id); PhysicsServer.BodySetLinearVelocity(id, new(120, 180));
            using var state = PhysicsServer.BodyGetDirectState(id)!;
            var before = state.LinearVelocity;
            if (subdivided) mover.Position += new Vector2(20, 0);
            var world = B2Worlds.b2GetWorldFromId(floor.Space!.WorldID); var step = world.stepIndex;
            tree.PhysicsFrame(1d / 60);
            Check(state.GetContactCount() > 0, "Impact retains real contact points.");
            Check(subdivided ? world.stepIndex > step + 1 : world.stepIndex == step + 1, "The intended native interval count executes.");
            var impulse = Impulse(state); var expected = (state.LinearVelocity - before) * 2;
            Near(impulse, expected, .08f, $"raw={raw}, subdivided={subdivided}");
            Check(impulse.X < -1 && impulse.Y < -1, "Both normal and friction impulses are observable.");
            var retained = state.GetContactImpulse(0); PhysicsServer.BodySetTransform(id, new(0, new(0, -100)));
            Near(state.GetContactImpulse(0), retained, .001f, "Snapshot survives a later transform mutation.");
        }
        finally { if (raw) { PhysicsServer.FreeRID(id); body.Dispose(); } }
    }

    private static void VerifyPairAndSleep(bool gpu)
    {
        using var root = new Node(); using var shape = new RectangleShape { Size = new(20, 20) };
        var a = new RigidBody { Name = "A", Position = new(-10, 0), LockRotation = true };
        var b = new RigidBody { Name = "B", Position = new(10, 0), LockRotation = true };
        a.AddChild(new CollisionShape { Shape = shape }); b.AddChild(new CollisionShape { Shape = shape });
        root.AddChild(a); root.AddChild(b); var mover = Mover(root, shape);
        using var tree = new SceneTree(root); if (gpu) a.Space!.EnableGPUSolver();
        Configure(a.GetRID()); Configure(b.GetRID()); b.Mass = 3;
        a.LinearVelocity = new(100, 50); b.LinearVelocity = new(-80, 0);
        using var va = PhysicsServer.BodyGetDirectState(a.GetRID())!; using var vb = PhysicsServer.BodyGetDirectState(b.GetRID())!;
        var oldA = va.LinearVelocity; var oldB = vb.LinearVelocity; mover.Position += new Vector2(20, 0);
        tree.PhysicsFrame(1d / 60);
        Check(va.GetContactCount() > 0 && vb.GetContactCount() > 0, "Both ends report the same real collision.");
        Near(Impulse(va), (va.LinearVelocity - oldA) * 2, .08f, "First paired impulse.");
        Near(Impulse(vb), (vb.LinearVelocity - oldB) * 3, .08f, "Second paired impulse.");
        Near(Impulse(va) + Impulse(vb), Vector2.Zero, .02f, "Equal and opposite, without counting both reporters twice.");
        a.Sleeping = true; b.Sleeping = true; mover.Position += new Vector2(20, 0);
        tree.PhysicsFrame(1d / 60);
        Check(a.Sleeping && b.Sleeping, "The resting pair stayed asleep.");
        Near(Impulse(va), Vector2.Zero, .001f, "Sleeping subdivided frame cannot replay old impulses.");
        tree.PhysicsFrame(1d / 60);
        Near(Impulse(vb), Vector2.Zero, .001f, "Sleeping single-interval frame cannot replay old impulses.");
    }

    private static void VerifyTransient(bool gpu)
    {
        using var root = new Node(); using var shape = new RectangleShape { Size = new(20, 20) };
        var a = new RigidBody { Name = "A", Position = new(-10, 0), LockRotation = true, ContactMonitor = true };
        var b = new RigidBody { Name = "B", Position = new(10, 0), LockRotation = true, ContactMonitor = true };
        a.AddChild(new CollisionShape { Shape = shape }); b.AddChild(new CollisionShape { Shape = shape });
        root.AddChild(a); root.AddChild(b); var mover = Mover(root, shape);
        using var tree = new SceneTree(root); if (gpu) a.Space!.EnableGPUSolver();
        Configure(a.GetRID()); Configure(b.GetRID());
        PhysicsServer.BodySetBounce(a.GetRID(), 1); PhysicsServer.BodySetBounce(b.GetRID(), 1);
        PhysicsServer.BodySetFriction(a.GetRID(), 0); PhysicsServer.BodySetFriction(b.GetRID(), 0);
        a.LinearVelocity = new(300, 0); b.LinearVelocity = new(-300, 0);
        using var va = PhysicsServer.BodyGetDirectState(a.GetRID())!; using var vb = PhysicsServer.BodyGetDirectState(b.GetRID())!;
        var oldA = va.LinearVelocity; var oldB = vb.LinearVelocity; var entered = 0; var exited = 0;
        a.BodyEntered += _ => entered++; a.BodyExited += _ => exited++;
        mover.Position += new Vector2(40, 0); tree.PhysicsFrame(1d / 60);
        Check(b.Position.X - a.Position.X > 24, "The bounce separates the pair before the end of the outer frame.");
        Check(va.GetContactCount() > 0 && vb.GetContactCount() > 0 && entered == 1, "A transient inner-interval collision is still reported once.");
        Near(Impulse(va), (va.LinearVelocity - oldA) * 2, .1f, "Transient first impulse.");
        Near(Impulse(vb), (vb.LinearVelocity - oldB) * 2, .1f, "Transient second impulse.");
        mover.Position += new Vector2(40, 0); tree.PhysicsFrame(1d / 60);
        Check(va.GetContactCount() == 0 && exited == 1, "A later frame does not replay a departed contact.");
    }

    private static void VerifyRotatingNormal(bool gpu)
    {
        using var root = new Node(); using var floorShape = new RectangleShape { Size = new(200, 20) };
        using var circle = new CircleShape { Radius = 10 }; using var moverShape = new RectangleShape { Size = new(20, 20) };
        var floor = new AnimatableBody { Name = "Floor", Position = new(0, 100) };
        var body = new RigidBody { Name = "Body", Position = new(40, 80), LockRotation = true };
        floor.AddChild(new CollisionShape { Shape = floorShape }); body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(floor); root.AddChild(body); var mover = Mover(root, moverShape);
        using var tree = new SceneTree(root); if (gpu) floor.Space!.EnableGPUSolver(); Configure(body.GetRID());
        body.LinearVelocity = new(0, 40); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
        var before = state.LinearVelocity; floor.Rotation = -.1f; mover.Position += new Vector2(40, 0);
        tree.PhysicsFrame(1d / 60);
        Check(state.GetContactCount() > 0 && MathF.Abs(state.GetContactLocalNormal(0).X) > .02f, "The contact normal rotates within the frame.");
        Near(Impulse(state), (state.LinearVelocity - before) * 2, .1f, "Rotating normal requires vector accumulation.");
    }

    private static void VerifySelection(bool gpu, bool raw)
    {
        using var root = new Node(); using var shape = new CircleShape { Radius = 10 };
        var body = new RigidBody { Name = "Body", GravityScale = 0, MaxContactsReported = 2, ContactMonitor = true };
        body.AddChild(new CollisionShape { Shape = shape }); if (!raw) root.AddChild(body);
        var floors = new StaticBody[6];
        for (var i = 0; i < floors.Length; i++)
        {
            floors[i] = new StaticBody { Name = "Floor" + i, Position = new(0, 15 + i * .1f) };
            floors[i].AddChild(new CollisionShape { Shape = shape }); root.AddChild(floors[i]);
        }
        using var tree = new SceneTree(root); if (gpu) floors[0].Space!.EnableGPUSolver();
        var id = raw ? PhysicsServer.BodyCreate() : body.GetRID();
        try
        {
            if (raw)
            {
                PhysicsServer.BodyAddShape(id, shape.GetRID()); PhysicsServer.BodySetGravityScale(id, 0);
                PhysicsServer.BodySetMaxContactsReported(id, 2); PhysicsServer.BodySetSpace(id, floors[0].GetWorld()!.Space);
            }
            tree.PhysicsFrame(1d / 60);
            var world = B2Worlds.b2GetWorldFromId(floors[0].Space!.WorldID);
            var native = B2Bodies.b2GetBodyFullId(world, PhysicsServer.Service.BodyRuntime(id).BodyID);
            float[] depths = [1, 5, 3, 4, 5, 5]; var colliders = new RID[6]; var count = 0;
            // Feed known depths through actual fixture identities to isolate the bounded selector from solver separation.
            for (var key = native.headContactKey; key != -1;)
            {
                var contact = world.contacts.data[key >> 1]; var edge = key & 1; key = contact.edges[edge].nextKey;
                if ((contact.flags & (uint)B2ContactFlags.b2_contactTouchingFlag) == 0) continue;
                var sim = B2Contacts.b2GetContactSim(world, contact);
                sim.manifold.pointCount = 1; sim.manifold.points[0].separation = -depths[count];
                colliders[count++] = world.shapes.data[edge == 0 ? contact.shapeIdB : contact.shapeIdA].userData.GetRef<PhysicsFixtureTag>()!.ColliderRID;
            }
            Check(count == 6, "Six real contact candidates exist.");
            using var state = PhysicsServer.BodyGetDirectState(id)!;
            if (raw) state.CaptureContacts(); else body.CollectContacts(state);
            Check(state.GetContactCount() == 2 && state.GetContactCollider(0) == colliders[4] && state.GetContactCollider(1) == colliders[1],
                "Later deeper candidates replace the first shallowest slot; equal depths preserve existing slots.");
            if (!raw)
            {
                var monitored = body.GetCollidingBodies(); Check(monitored.Length == 2 && body.GetContactCount() == 2, "Monitor uses the selected point set.");
                Check(monitored.All(n => n is PhysicsBody p && (p.GetRID() == colliders[4] || p.GetRID() == colliders[1])), "Discarded candidates cannot produce monitored objects.");
            }
            PhysicsServer.BodySetMaxContactsReported(id, 1);
            Check(state.GetContactCount() == 0 && (raw || body.GetContactCount() == 0), "Changing the limit clears the old point count.");
            if (raw) state.CaptureContacts(); else body.CollectContacts(state);
            Check(state.GetContactCount() == 1 && state.GetContactCollider(0) == colliders[1], "A smaller cap retains the first of equally deep candidates.");
        }
        finally { if (raw) { PhysicsServer.FreeRID(id); body.Dispose(); } }
    }

    private static void VerifyWarm(bool gpu)
    {
        using var root = new Node(); using var floorShape = new RectangleShape { Size = new(10000, 20) };
        using var shape = new RectangleShape { Size = new(20, 20) };
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100), ConstantLinearVelocity = new(120, 0) };
        var body = new RigidBody { Name = "Body", Position = new(0, 80), CanSleep = false, LockRotation = true, MaxContactsReported = 4, ContactMonitor = true };
        floor.AddChild(new CollisionShape { Shape = floorShape }); body.AddChild(new CollisionShape { Shape = shape });
        root.AddChild(floor); root.AddChild(body); var mover = Mover(root, shape);
        using var tree = new SceneTree(root); if (gpu) floor.Space!.EnableGPUSolver();
        using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
        for (var i = 0; i < 96; i++) Tick();
        var before = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++) Tick();
        var bytes = GC.GetTotalAllocatedBytes(true) - before;
        Check(bytes == 0, $"Contact selection/subdivision snapshots allocated {bytes} bytes.");
        Check(state.GetContactCount() > 0 && body.LinearVelocity.X > 50, "Warm interval remains active contact work.");
        void Tick() { mover.Position += new Vector2(20, 0); tree.PhysicsFrame(1d / 60); _ = Impulse(state); }
    }

    private static void Near(Vector2 actual, Vector2 expected, float tolerance, string message) =>
        Check(actual.DistanceTo(expected) < tolerance, $"{message} Expected {expected}, got {actual}.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
