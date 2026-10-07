using Box2D.NET;
using Electron2D;

internal static class PhysicsServerStateTests
{
    internal static void Run(bool gpu = false)
    {
        using var lifetime = gpu ? new GPUPhysicsWorld() : null;
        VerifyDetached();
        VerifyDynamic(gpu);
        VerifyStaticTeleport(gpu);
        VerifyKinematic(gpu);
        VerifySceneRoles(gpu);
        Console.WriteLine($"Typed body state passed on {(gpu ? "GPU" : "CPU")}: scene/server parity, sleep, target motion, lifecycle, guards and zero warmed bytes.");
    }

    private static void VerifyDetached()
    {
        using var scene = new RigidBody();
        var server = PhysicsServer.BodyCreate();
        try
        {
            foreach (var id in new[] { scene.GetRID(), server })
            {
                Check(!PhysicsServer.BodyGetSleeping(id) && PhysicsServer.BodyGetCanSleep(id), "Detached defaults.");
                PhysicsServer.BodySetTransform(id, Pose(12, 20));
                Near(PhysicsServer.BodyGetTransform(id).Origin, new(12, 20));
                PhysicsServer.BodySetLinearVelocity(id, new(30, 40)); PhysicsServer.BodySetAngularVelocity(id, 2);
                PhysicsServer.BodySetAxisVelocity(id, new(0, -12)); Near(PhysicsServer.BodyGetLinearVelocity(id), new(30, -12));
                PhysicsServer.BodySetSleeping(id, true);
                Near(PhysicsServer.BodyGetLinearVelocity(id), Vector2.Zero); Near(PhysicsServer.BodyGetAngularVelocity(id), 0);
                PhysicsServer.BodySetCanSleep(id, false); Check(!PhysicsServer.BodyGetSleeping(id), "No-sleep wakes detached configuration.");
                PhysicsServer.BodySetSleeping(id, true); PhysicsServer.BodyApplyCentralForce(id, new(1, 0));
                Check(!PhysicsServer.BodyGetSleeping(id), "Detached force wakes both owners.");
                PhysicsServer.BodySetSleeping(id, true); PhysicsServer.BodySetLinearVelocity(id, Vector2.Zero);
                Check(!PhysicsServer.BodyGetSleeping(id), "Explicit zero velocity wakes.");
                PhysicsServer.BodySetAxisVelocity(id, new(float.MaxValue / 4, float.MaxValue / 4));
                var huge = PhysicsServer.BodyGetLinearVelocity(id);
                Check(huge.IsFinite() && huge.X > 1e37f && huge.Y > 1e37f, "Axis projection avoids float-length overflow.");
                Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetAngularVelocity(id, float.NaN));
                Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetLinearVelocity(id, new(float.PositiveInfinity, 0)));
                Reject<ArgumentException>(() => PhysicsServer.BodySetTransform(id, new(0, new(2, 1), 0, Vector2.Zero)));
                Check(PhysicsServer.BodyGetLinearVelocity(id) == huge, "Rejected state leaves velocity intact.");
            }
        }
        finally { PhysicsServer.FreeRID(server); }
        Reject<ArgumentException>(() => PhysicsServer.BodyGetSleeping(server));
        using var area = new Area(); Reject<ArgumentException>(() => PhysicsServer.BodySetCanSleep(area.GetRID(), false));
    }

    private static void VerifyDynamic(bool gpu)
    {
        using var root = new Node(); var body = new RigidBody { GravityScale = 0 }; root.AddChild(body);
        using var tree = new SceneTree(root); var space = body.Space!;
        if (gpu) space.EnableGPUSolver();
        var raw = PhysicsServer.BodyCreate(); PhysicsServer.BodySetGravityScale(raw, 0);
        PhysicsServer.BodySetSpace(raw, body.GetWorld()!.Space);
        try
        {
            foreach (var id in new[] { body.GetRID(), raw })
            {
                PhysicsServer.BodySetTransform(id, Pose(10, 0));
                PhysicsServer.BodySetLinearVelocity(id, new(60, 0)); PhysicsServer.BodySetAngularVelocity(id, 1);
                using var state = PhysicsServer.BodyGetDirectState(id)!;
                Near(state.LinearVelocity, new(60, 0)); Near(state.AngularVelocity, 1);
                tree.PhysicsFrame(1d / 60); Check(PhysicsServer.BodyGetTransform(id).Origin.X > 10.5f, "Real dynamic integration.");
                PhysicsServer.BodySetSleeping(id, true); Check(state.Sleeping, "Server setter reaches live sleep state.");
                Near(state.LinearVelocity, Vector2.Zero); Near(state.AngularVelocity, 0);
                PhysicsServer.BodySetCanSleep(id, false); Check(!state.Sleeping, "Disabling automatic sleep wakes.");
                state.Sleeping = true; Check(PhysicsServer.BodyGetSleeping(id), "Direct sleep shares state.");
                state.LinearVelocity = new(30, 0); state.AngularVelocity = 2;
                Check(!PhysicsServer.BodyGetSleeping(id), "Direct velocity wakes.");
                Near(PhysicsServer.BodyGetLinearVelocity(id), new(30, 0)); Near(PhysicsServer.BodyGetAngularVelocity(id), 2);
                Task.Run(() => Reject<InvalidOperationException>(() => PhysicsServer.BodyGetSleeping(id))).GetAwaiter().GetResult();
                var world = B2Worlds.b2GetWorldFromId(space.WorldID); world.locked = true;
                try { Reject<InvalidOperationException>(() => PhysicsServer.BodySetAngularVelocity(id, 10)); }
                finally { world.locked = false; }
                Near(PhysicsServer.BodyGetAngularVelocity(id), 2);
            }
            PhysicsServer.BodySetSleeping(raw, true); PhysicsServer.BodySetSpace(raw, default);
            Check(PhysicsServer.BodyGetSleeping(raw) && !PhysicsServer.BodyGetCanSleep(raw), "Raw detached policy/state survives.");
            PhysicsServer.BodySetSpace(raw, body.GetWorld()!.Space); Check(PhysicsServer.BodyGetSleeping(raw), "Raw reentry preserves explicit sleep.");
            PhysicsServer.BodySetSleeping(body.GetRID(), true); root.RemoveChild(body);
            Check(PhysicsServer.BodyGetSleeping(body.GetRID()), "Scene detached sleep survives.");
            root.AddChild(body); Check(PhysicsServer.BodyGetSleeping(body.GetRID()), "Scene reentry preserves explicit sleep.");
            for (var i = 0; i < 96; i++) Cycle();
            var before = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Cycle();
            var bytes = GC.GetTotalAllocatedBytes(true) - before;
            Check(bytes == 0, $"Typed state cycles allocated {bytes} bytes.");
            void Cycle()
            {
                PhysicsServer.BodySetCanSleep(raw, false); PhysicsServer.BodySetSleeping(raw, false);
                PhysicsServer.BodySetLinearVelocity(raw, new(30, 40)); PhysicsServer.BodySetAxisVelocity(raw, new(0, 20));
                PhysicsServer.BodySetAngularVelocity(raw, .1f);
                _ = PhysicsServer.BodyGetLinearVelocity(raw); _ = PhysicsServer.BodyGetAngularVelocity(raw);
                _ = PhysicsServer.BodyGetTransform(raw); _ = PhysicsServer.BodyGetCanSleep(raw); _ = PhysicsServer.BodyGetSleeping(raw);
                tree.PhysicsFrame(1d / 60);
            }
        }
        finally { PhysicsServer.FreeRID(raw); }
    }

    private static void VerifyStaticTeleport(bool gpu)
    {
        using var root = new Node(); using var floorShape = new RectangleShape { Size = new(200, 20) };
        using var box = new RectangleShape { Size = new(20, 20) };
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100) };
        var first = new RigidBody { Name = "First", Position = new(0, 80) };
        var second = new RigidBody { Name = "Second", Position = new(300, 80) };
        floor.AddChild(new CollisionShape { Shape = floorShape }); first.AddChild(new CollisionShape { Shape = box });
        second.AddChild(new CollisionShape { Shape = box }); root.AddChild(floor); root.AddChild(first); root.AddChild(second);
        using var tree = new SceneTree(root); if (gpu) floor.Space!.EnableGPUSolver();
        var raw = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(raw, floorShape.GetRID());
            PhysicsServer.BodySetTransform(raw, Pose(300, 100)); PhysicsServer.BodySetSpace(raw, floor.GetWorld()!.Space);
            for (var i = 0; i < 100; i++) tree.PhysicsFrame(1d / 60);
            first.Sleeping = true; second.Sleeping = true;
            PhysicsServer.BodySetTransform(floor.GetRID(), Pose(1000, 100)); PhysicsServer.BodySetTransform(raw, Pose(1300, 100));
            Check(!first.Sleeping && !second.Sleeping, "Moving a static support wakes its touching bodies for both owners.");
            for (var i = 0; i < 10; i++) tree.PhysicsFrame(1d / 60);
            Check(first.Position.Y > 85 && second.Position.Y > 85, "Bodies fall after their supports move away.");
        }
        finally { PhysicsServer.FreeRID(raw); }
    }

    private static void VerifyKinematic(bool gpu)
    {
        using var root = new Node(); using var shape = new RectangleShape { Size = new(10, 20) };
        var target = new RigidBody { Position = new(20, 0), GravityScale = 0 }; target.AddChild(new CollisionShape { Shape = shape }); root.AddChild(target);
        using var tree = new SceneTree(root); if (gpu) target.Space!.EnableGPUSolver();
        var body = PhysicsServer.BodyCreate(); var space = target.GetWorld()!.Space;
        try
        {
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Kinematic);
            PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetTransform(body, Pose(0, 0));
            PhysicsServer.BodySetLinearVelocity(body, new(50, 0)); PhysicsServer.BodySetAngularVelocity(body, .2f);
            PhysicsServer.BodySetSpace(body, space);
            tree.PhysicsFrame(1d / 60); Near(PhysicsServer.BodyGetTransform(body).Origin, Vector2.Zero);
            PhysicsServer.BodySetTransform(body, Pose(25, 0)); PhysicsServer.BodySetTransform(body, Pose(30, 0));
            tree.PhysicsFrame(0); Near(PhysicsServer.BodyGetTransform(body).Origin, Vector2.Zero);
            PhysicsServer.SpaceSetActive(space, false); tree.PhysicsFrame(1d / 60); Near(PhysicsServer.BodyGetTransform(body).Origin, Vector2.Zero);
            PhysicsServer.SpaceSetActive(space, true); tree.PhysicsFrame(1d / 60);
            Near(PhysicsServer.BodyGetTransform(body).Origin, new(30, 0));
            Check(target.Position.X > 25, $"Kinematic target must push across its path: {target.Position}.");
            Near(PhysicsServer.BodyGetLinearVelocity(body).X, 1850, 1);
            tree.PhysicsFrame(1d / 60); Near(PhysicsServer.BodyGetTransform(body).Origin, new(30, 0));
            Near(PhysicsServer.BodyGetLinearVelocity(body), new(50, 0)); Near(PhysicsServer.BodyGetAngularVelocity(body), .2f);
            using var view = PhysicsServer.BodyGetDirectState(body)!;
            view.Transform = new(.1f, new(40, 0)); Near(view.Transform.Origin, new(30, 0));
            PhysicsServer.BodySetSpace(body, default); Reject<ObjectDisposedException>(() => _ = view.Transform);
            PhysicsServer.BodySetSpace(body, space); tree.PhysicsFrame(1d / 60);
            Near(PhysicsServer.BodyGetTransform(body).Origin, new(40, 0)); Near(PhysicsServer.BodyGetAngularVelocity(body), 6.2f, .15f);
            tree.PhysicsFrame(1d / 60); Near(PhysicsServer.BodyGetAngularVelocity(body), .2f);
            Near(PhysicsServer.BodyGetTransform(body).Rotation, .1f, .01f);
            PhysicsServer.BodySetSleeping(body, true); Check(!PhysicsServer.BodyGetSleeping(body), "Kinematic sleep assignment is ignored.");
        }
        finally { PhysicsServer.FreeRID(body); }
    }

    private static void VerifySceneRoles(bool gpu)
    {
        using var root = new Node(); var surface = new StaticBody { Name = "Surface" }; var mover = new AnimatableBody { Name = "Mover" };
        var character = new CharacterBody { Name = "Character", Velocity = new(123, 0) };
        root.AddChild(surface); root.AddChild(mover); root.AddChild(character);
        using var tree = new SceneTree(root); if (gpu) surface.Space!.EnableGPUSolver();
        foreach (var node in new PhysicsBody[] { surface, mover, character })
        {
            var id = node.GetRID(); PhysicsServer.BodySetLinearVelocity(id, new(30, 0)); PhysicsServer.BodySetAngularVelocity(id, .2f);
            PhysicsServer.BodySetCanSleep(id, false); tree.PhysicsFrame(1d / 60);
            Near(PhysicsServer.BodyGetLinearVelocity(id), new(30, 0)); Near(PhysicsServer.BodyGetAngularVelocity(id), .2f);
            root.RemoveChild(node); Near(PhysicsServer.BodyGetLinearVelocity(id), new(30, 0)); root.AddChild(node);
            Near(PhysicsServer.BodyGetLinearVelocity(id), new(30, 0)); Check(!PhysicsServer.BodyGetCanSleep(id), "Scene policy survives reentry.");
        }
        Check(character.Velocity == new Vector2(123, 0), "Character input velocity is independent of server contact velocity.");
        PhysicsServer.BodySetTransform(mover.GetRID(), Pose(10, 0)); Near(mover.Position, Vector2.Zero);
        tree.PhysicsFrame(1d / 60); Near(mover.Position, new(10, 0)); Near(PhysicsServer.BodyGetLinearVelocity(mover.GetRID()).X, 630, .1f);
        PhysicsServer.BodySetTransform(surface.GetRID(), Pose(20, 0)); Near(surface.Position, new(20, 0));
        Check(PhysicsServer.BodyGetSleeping(surface.GetRID()), "Static body is inactive.");
    }

    private static Transform Pose(float x, float y) => new(0, Vector2.One, 0, new(x, y));
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Near(float actual, float expected, float tolerance = .02f) => Check(MathF.Abs(actual - expected) < tolerance, $"Expected {expected}, got {actual}.");
    private static void Near(Vector2 actual, Vector2 expected) => Check(actual.DistanceTo(expected) < .04f, $"Expected {expected}, got {actual}.");
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}."); }
}
