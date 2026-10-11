using Electron2D;

internal static partial class PhysicsBackendOwnershipTests
{
    private static void NamedFactories(PhysicsServer.Backend backend)
    {
        var registry = new PhysicsBackendRegistry();
        Check(registry.Resolve("DEFAULT").Registration.Name == "CPU", "Named default starts with CPU without a device");
        Reject<ArgumentException>(() => registry.Resolve("Missing"));
        Reject<ArgumentException>(() => registry.SetDefaultServer("Missing", int.MaxValue));
        Reject<ArgumentException>(() => registry.RegisterServer("DEFAULT", _ => null!));
        Reject<ArgumentException>(() => registry.RegisterServer("CPU", _ => null!));
        Reject<ArgumentException>(() => registry.RegisterServer(" ", _ => null!));
        Reject<ArgumentNullException>(() => registry.RegisterServer("Null", null!));
        var creations = 0;
        ViewBackend? selected = null;
        registry.RegisterServer("Scaled", space =>
        {
            InChildContext(() => _ = registry.Resolve("Scaled"));
            creations++;
            selected = new(space, backend) { ImpulseScale = 2 };
            selected.SpaceFactory = () => new InitializationQueries(space.RID, new PhysicsDirectSpaceState(space.RID));
            selected.BodyFactory = body => new FactoryBodyState(body.RID);
            return selected;
        });
        Reject<ArgumentException>(() => registry.RegisterServer("Scaled", _ => null!));
        registry.SetDefaultServer("Scaled", 1);
        var captured = registry.Resolve("DEFAULT");
        registry.SetDefaultServer("GPU", 1);
        Check(registry.Resolve("DEFAULT").Registration.Name == "Scaled", "Equal priority preserves the first registered default");
        registry.SetDefaultServer("GPU", 2);
        registry.SetDefaultServer("CPU", 0);
        Check(registry.Resolve("DEFAULT").Registration.Name == "GPU" && captured.Registration.Name == "Scaled", "Later priority changes preserve captured selections");
        registry.SetDefaultServer("Scaled", 3);
        using var world = new World(new WorldRuntime(false, backend, selection: captured));
        using var alias = (World)world.Duplicate();
        Check(creations == 0, "Named world and aliases retain a lazy captured factory");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 };
        var scene = new RigidBody { GravityScale = 0, CanSleep = false, Position = new(-20, 0) };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
            PhysicsServer.BodySetTransform(raw, new(0, new(20, 0))); PhysicsServer.BodySetSpace(raw, world.Space);
            tree.PhysicsFrame(1d / 60);
            var space = PhysicsServer.Service.GetSceneSpace(world.Space);
            Check(creations == 1 && alias.Space == world.Space && space.RequestedBackendName == "DEFAULT" && space.ActualBackendName == "Scaled",
                "Registered selection retains request/actual name, one implementation and shared alias identity");
            PhysicsServer.BodyApplyCentralImpulse(scene.GetRID(), new(10, 0)); PhysicsServer.BodyApplyCentralImpulse(raw, new(10, 0));
            tree.PhysicsFrame(1d / 60);
            Check(MathF.Abs(scene.LinearVelocity.X - 20) < .001f && MathF.Abs(PhysicsServer.BodyGetLinearVelocity(raw).X - 20) < .001f,
                $"Registered implementation changes actual scene/raw impulse response: {scene.LinearVelocity.X}/{PhysicsServer.BodyGetLinearVelocity(raw).X}");
            var query = world.DirectSpaceState;
            using var point = new PhysicsPointQueryParameters { Position = scene.GlobalPosition };
            Check(query is InitializationQueries && PhysicsServer.BodyGetDirectState(raw) is FactoryBodyState && query.IntersectPoint(point).Any(hit => hit.ColliderRID == scene.GetRID()),
                "Named implementation supplies real selected query/body views");
            Collect();
            for (var i = 0; i < 64; i++) Cycle();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Cycle();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Named implementation intervals allocate {owner}/{all} bytes");
            Console.WriteLine($"{backend}: named priority/lazy aliases, real scaled impulses and selected views; 64 warm intervals: {owner}/{all} B.");
            void Cycle()
            {
                PhysicsServer.BodyApplyCentralImpulse(scene.GetRID(), new(.01f, 0)); PhysicsServer.BodyApplyCentralImpulse(raw, new(.01f, 0));
                tree.PhysicsFrame(1d / 60); _ = PhysicsServer.BodyGetDirectState(raw)!.LinearVelocity;
            }
        }
        finally { PhysicsServer.FreeRID(raw); }
        Check(selected is not null, "Selected named owner was exercised");
        var explicitSpace = PhysicsServer.Service.CreateRegisteredSpace(backend, false, true, selection: captured);
        try
        {
            Check(!explicitSpace.IsActive && creations == 2 && explicitSpace.ActualBackendName == "Scaled",
                "Captured registration constructs a fresh inactive explicit space");
            registry.RegisterServer("Foreign", _ => explicitSpace.BackendImplementation);
            RID rejected = default;
            registry.RegisterServer("Throw", space => { rejected = space.RID; throw new InvalidOperationException("Expected named factory failure"); });
            RequireBorrowRejection(() => PhysicsServer.Service.CreateRegisteredSpace(backend, false, true, selection: registry.Resolve("Throw")));
            Reject<ArgumentException>(() => PhysicsServer.Service.GetSceneSpace(rejected));
            RequireBorrowRejection(() => PhysicsServer.Service.CreateRegisteredSpace(backend, false, true, selection: registry.Resolve("Foreign")));
            explicitSpace.EnsureQueryAccess();
        }
        finally { PhysicsServer.FreeRID(explicitSpace.RID); }

        registry.RegisterServer("Recursive", factorySpace =>
        {
            using var nested = new World(new WorldRuntime(false, backend, selection: registry.Resolve("Recursive")));
            _ = nested.Space; return null!;
        });
        using var recursive = new World(new WorldRuntime(false, backend, selection: registry.Resolve("Recursive")));
        RequireBorrowRejection(() => _ = recursive.Space);
        registry.RegisterServer("ChildRecursive", factorySpace =>
        {
            InChildContext(() =>
            {
                using var nested = new World(new WorldRuntime(false, backend, selection: registry.Resolve("ChildRecursive")));
                _ = nested.Space;
            });
            return null!;
        });
        using var childRecursive = new World(new WorldRuntime(false, backend, selection: registry.Resolve("ChildRecursive")));
        RequireBorrowRejection(() => _ = childRecursive.Space);
        ViewBackend? initializing = null;
        registry.RegisterServer("InitializingRecursive", space => initializing = new ViewBackend(space, backend)
        {
            InitializeHook = () =>
            {
                using var nested = new World(new WorldRuntime(false, backend, selection: registry.Resolve("InitializingRecursive")));
                _ = nested.Space;
            }
        });
        using var initializingRecursive = new World(new WorldRuntime(false, backend, selection: registry.Resolve("InitializingRecursive")));
        RequireBorrowRejection(() => _ = initializingRecursive.Space);
        Reject<ObjectDisposedException>(() => initializing!.EnsureAccess());
        registry.RegisterServer("Empty", _ => null!);
        using var empty = new World(new WorldRuntime(false, backend, selection: registry.Resolve("Empty")));
        RequireBorrowRejection(() => _ = empty.Space);
        using var healthy = new World(new WorldRuntime(false, backend, selection: captured));
        Check(healthy.Space.IsValid(), "Failed named creation retires recursion state and preserves later worlds");
    }
    private static void InChildContext(Action action)
    {
        Exception? failure = null;
        var child = new Thread(() => { try { action(); } catch (Exception error) { failure = error; } }) { IsBackground = true };
        child.Start();
        if (!child.Join(TimeSpan.FromSeconds(5))) throw new TimeoutException("Factory child context did not complete outside registry locks.");
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
