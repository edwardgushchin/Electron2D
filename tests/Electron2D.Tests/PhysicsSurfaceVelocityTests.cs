using Box2D.NET;
using Electron2D;

internal sealed class PhysicsSurfaceVelocityTests(bool gpu, PhysicsServer.Backend backend)
{
    internal static void Run(bool gpu = false) => new PhysicsSurfaceVelocityTests(gpu, PhysicsServer.Backend.CPU).RunCore();
    internal static void Run(PhysicsServer.Backend backend) => new PhysicsSurfaceVelocityTests(false, backend).RunCore();
    private void RunCore()
    {
        using var computeLifetime = gpu ? new GPUPhysicsWorld() : null;
        VerifyStorageAndLifecycle();
        foreach (var mode in new[] { 0, 1, 2 }) VerifyResponse(gpu, mode);
        VerifyServer(gpu);
        VerifyAnimatedAndCharacter(gpu);
        Console.WriteLine($"Stationary linear/angular surfaces passed on {(gpu ? "CPU host/GPU stages" : backend.ToString())}: response, queries, waking, lifecycle, packing and warmed allocation.");
    }

    private void VerifyStorageAndLifecycle()
    {
        using var selectedWorld = new World(backend); using var root = new SubViewport { Name = "Root", World = selectedWorld };
        var floor = new StaticBody { Name = "Floor" }; root.AddChild(floor); floor.Owner = root;
        Check(floor.ConstantLinearVelocity == Vector2.Zero && floor.ConstantAngularVelocity == 0, "Surface defaults.");
        floor.ConstantLinearVelocity = new(30, -4); floor.ConstantAngularVelocity = .5f;
        Reject<ArgumentOutOfRangeException>(() => floor.ConstantLinearVelocity = new(float.NaN, 0));
        Reject<ArgumentOutOfRangeException>(() => floor.ConstantAngularVelocity = float.PositiveInfinity);
        using var packed = new PackedScene(); packed.Pack(root);
        using var restored = packed.Instantiate(); var copy = restored.GetNode<StaticBody>("Floor");
        Check(copy.ConstantLinearVelocity == new Vector2(30, -4) && copy.ConstantAngularVelocity == .5f, "Packed surface state.");
        using var tree = new SceneTree(root);
        using var view = PhysicsServer.BodyGetDirectState(floor.GetRID())!;
        Near(view.LinearVelocity, new(30, -4)); Near(view.AngularVelocity, .5f);
        Near(view.GetVelocityAtLocalPosition(new(20, 0)), new(30, 6));
        Task.Run(() => Reject<InvalidOperationException>(() => floor.ConstantAngularVelocity = 2)).GetAwaiter().GetResult();
        if (backend == PhysicsServer.Backend.CPU)
        {
            var world = B2Worlds.b2GetWorldFromId(floor.Space!.WorldID); world.locked = true;
            try { Reject<InvalidOperationException>(() => floor.ConstantLinearVelocity = Vector2.One); }
            finally { world.locked = false; }
        }
        root.RemoveChild(floor); Reject<ObjectDisposedException>(() => _ = view.LinearVelocity);
        root.AddChild(floor);
        using var next = PhysicsServer.BodyGetDirectState(floor.GetRID())!;
        Near(next.LinearVelocity, new(30, -4)); Near(next.AngularVelocity, .5f);
        next.AngularVelocity = 2; next.LinearVelocity = new(40, 0);
        Check(floor.ConstantLinearVelocity == new Vector2(40, 0) && floor.ConstantAngularVelocity == 2, "Direct view projects to scene properties.");
        PhysicsServer.BodySetLinearVelocity(floor.GetRID(), new(50, 0)); Near(next.LinearVelocity, new(50, 0));
        floor.Dispose(); Reject<ObjectDisposedException>(() => floor.ConstantAngularVelocity = 0);
    }

    private void VerifyResponse(bool gpu, int mode)
    {
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld }; using var floorShape = new RectangleShape { Size = new(10000, 20) };
        using var box = new RectangleShape { Size = new(20, 20) };
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100) };
        var body = new RigidBody { Name = "Box", Position = new(mode == 2 ? -100 : 0, 80), LockRotation = true, MaxContactsReported = 4 };
        floor.AddChild(new CollisionShape { Shape = floorShape }); body.AddChild(new CollisionShape { Shape = box });
        root.AddChild(floor); root.AddChild(body); using var tree = new SceneTree(root);
        if (gpu) floor.Space!.EnableGPUSolver();
        if (mode == 0)
        {
            for (var i = 0; i < 100; i++) tree.PhysicsFrame(1d / 60);
            body.Sleeping = true;
            floor.ConstantLinearVelocity = new(120, 0);
            Check(!body.Sleeping, "A changed belt wakes its touching sleeping body.");
        }
        else
        {
            body.GravityScale = 0;
            if (mode == 1) floor.ConstantLinearVelocity = new(0, -120);
            else floor.ConstantAngularVelocity = 2;
        }
        tree.PhysicsFrame(1d / 60);
        using var view = PhysicsServer.BodyGetDirectState(body.GetRID())!;
        Check(view.GetContactCount() > 0, "The surface creates real contacts.");
        var point = view.GetContactColliderPosition(0) - floor.GlobalPosition;
        var expected = floor.ConstantLinearVelocity + new Vector2(-point.Y, point.X) * floor.ConstantAngularVelocity;
        Near(view.GetContactColliderVelocityAtPosition(0), expected);
        for (var i = 0; i < 90; i++) tree.PhysicsFrame(1d / 60);
        Check(floor.Position == new Vector2(0, 100) && floor.Rotation == 0, "Virtual velocity never moves the surface pose.");
        if (mode == 0)
        {
            Check(body.LinearVelocity.X > 70 && body.Position.X > 50, $"Belt response: {body.LinearVelocity}, {body.Position}.");
            for (var i = 0; i < 96; i++) tree.PhysicsFrame(1d / 60);
            var before = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            var bytes = GC.GetTotalAllocatedBytes(true) - before;
            Check(bytes == 0, $"Surface contact frames allocated {bytes} managed bytes.");
        }
        else Check(body.Position.Y < 65 && body.LinearVelocity.Y < -5, $"Normal/angular surface response: {body.Position}, {body.LinearVelocity}.");
    }

    private void VerifyServer(bool gpu)
    {
        var space = PhysicsServer.SpaceCreate(backend); var floor = PhysicsServer.BodyCreate();
        var shape = PhysicsServer.RectangleShapeCreate();
        try
        {
            using var geometry = new RectangleShape { Size = new(200, 20) }; PhysicsServer.ShapeSetData(shape, geometry);
            PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(floor, shape); PhysicsServer.BodySetLinearVelocity(floor, new(70, 0));
            PhysicsServer.BodySetSpace(floor, space); PhysicsServer.SpaceSetActive(space, true);
            if (gpu) PhysicsServer.Service.GetSceneSpace(space).EnableGPUSolver();
            using (var state = PhysicsServer.BodyGetDirectState(floor)!)
            {
                state.AngularVelocity = 3; Near(state.GetVelocityAtLocalPosition(new(10, 0)), new(70, 30));
                PhysicsServer.SpaceStep(space, 1d / 60); Check(state.Transform.Origin == Vector2.Zero, "Server surface stays fixed.");
            }
            PhysicsServer.BodySetSpace(floor, default); PhysicsServer.BodySetSpace(floor, space);
            using var restored = PhysicsServer.BodyGetDirectState(floor)!;
            Near(restored.LinearVelocity, new(70, 0)); Near(restored.AngularVelocity, 3);
            PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Rigid);
            Near(restored.LinearVelocity, new(70, 0)); Near(restored.AngularVelocity, 3);
            PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Kinematic);
            Near(restored.LinearVelocity, Vector2.Zero); Near(restored.AngularVelocity, 0);
        }
        finally { PhysicsServer.FreeRID(floor); PhysicsServer.FreeRID(shape); PhysicsServer.FreeRID(space); }
    }

    private void VerifyAnimatedAndCharacter(bool gpu)
    {
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld }; using var floorShape = new RectangleShape { Size = new(1000, 20) };
        using var circle = new CircleShape { Radius = 10 };
        var floor = new AnimatableBody { Name = "Floor", Position = new(0, 100), ConstantLinearVelocity = new(60, 0), ConstantAngularVelocity = 0 };
        var character = new CharacterBody { Name = "Character", Position = new(0, 78), Velocity = new(0, 600), PhysicsProcessEnabled = true };
        floor.AddChild(new CollisionShape { Shape = floorShape }); character.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(floor); root.AddChild(character); using var tree = new SceneTree(root);
        if (gpu) floor.Space!.EnableGPUSolver();
        tree.PhysicsFrame(1d / 60);
        character.MoveAndSlide(); Check(character.IsOnFloor(), "Character finds the conveyor floor.");
        Near(character.GetPlatformVelocity(), new(60, 0));
        character.Velocity = Vector2.Zero; var oldX = character.Position.X;
        character.MoveAndSlide(); Check(character.Position.X > oldX + .5f, "Character inherits belt motion.");
        floor.Position = new(10, 100); tree.PhysicsFrame(1d / 60);
        Near(floor.Position, new(10, 100));
        using var state = PhysicsServer.BodyGetDirectState(floor.GetRID())!;
        Near(state.LinearVelocity.X, 660, .1f);
        state.LinearVelocity = new(80, 0); Near(state.LinearVelocity, new(80, 0));
        state.AngularVelocity = .2f; Near(state.AngularVelocity, .2f);
        floor.Rotation = .1f; tree.PhysicsFrame(1d / 60);
        Near(floor.Rotation, .1f, .01f); Near(state.AngularVelocity, 6.2f, .15f);
        tree.PhysicsFrame(1d / 60); Near(state.LinearVelocity, new(80, 0)); Near(state.AngularVelocity, .2f);
        Near(floor.Position, new(10, 100)); Near(floor.Rotation, .1f, .01f);
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Near(float actual, float expected, float tolerance = .01f) => Check(MathF.Abs(actual - expected) <= tolerance, $"Expected {expected}, got {actual}.");
    private static void Near(Vector2 actual, Vector2 expected) => Check(actual.DistanceTo(expected) < .03f, $"Expected {expected}, got {actual}.");
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}.");
    }
}
