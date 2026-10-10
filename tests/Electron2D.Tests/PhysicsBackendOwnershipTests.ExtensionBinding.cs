using Electron2D;

internal static partial class PhysicsBackendOwnershipTests
{
    private static void ExtensionBinding(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 3 };
        var scene = new RigidBody { CanSleep = false, GravityScale = 0 };
        scene.AddChild(new CollisionShape { Shape = shape }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        try
        {
            Reject<ArgumentException>(() => new ControlledState(area));
            Reject<ArgumentException>(() => new ControlledState(default));
            Reject<InvalidOperationException>(() => new ControlledState(raw));
            PhysicsServer.BodyAddShape(raw, shape.GetRID()); PhysicsServer.BodySetSpace(raw, world.Space);
            foreach (var body in new[] { scene.GetRID(), raw })
            {
                PhysicsServer.BodySetMass(body, 2); PhysicsServer.BodySetInertia(body, 4);
                var runtime = PhysicsServer.Service.BodyRuntime(body);
                Check(runtime.View is null, "Binding fixture starts without a cached body view");
                var prepared = false;
                using var extension = new ControlledState(body, () =>
                {
                    Check(runtime.View is null, "Extension binding must not bootstrap the built-in body view");
                    Check(runtime.Backend.ShapeCount == 1 && MathF.Abs(runtime.Backend.InverseMass - .5f) < .0001f &&
                        MathF.Abs(runtime.Backend.InverseInertia - .25f) < .0001f, "Binding prepares selected geometry and authored mass before user construction");
                    prepared = true;
                });
                Check(prepared && ReferenceEquals(runtime.View, PhysicsServer.BodyGetDirectState(body)), "Explicit acquisition caches the ordinary view independently");
                extension.ApplyCentralImpulse(new(20, 0));
                Check(MathF.Abs(extension.LinearVelocity.X - 20) < .001f, "Bound extension executes a real selected-owner impulse");
                var disposed = runtime.View!; disposed.Dispose();
                using var reopened = new ControlledState(body, () =>
                    Check(ReferenceEquals(runtime.View, disposed) && disposed.IsDisposed, "Binding does not reopen a disposed built-in view"));
                Check(!ReferenceEquals(runtime.View, disposed) && MathF.Abs(extension.LinearVelocity.X - 20) < .001f,
                    "Explicit built-in reopening preserves independently bound extension lifetime");
                Task.Run(() => Reject<InvalidOperationException>(() => new ControlledState(body))).GetAwaiter().GetResult();
                if (backend == PhysicsServer.Backend.CPU)
                {
                    var native = Box2D.NET.B2Worlds.b2GetWorldFromId(PhysicsServer.Service.GetSceneSpace(world.Space).WorldID);
                    native.locked = true;
                    try { Reject<InvalidOperationException>(() => new ControlledState(body)); }
                    finally { native.locked = false; }
                }
                if (body == raw) PhysicsServer.BodySetSpace(body, default); else root.RemoveChild(scene);
                Reject<ObjectDisposedException>(() => _ = extension.LinearVelocity);
                Reject<InvalidOperationException>(() => new ControlledState(body));
                if (body == raw) PhysicsServer.BodySetSpace(body, world.Space); else root.AddChild(scene);
                Reject<ObjectDisposedException>(() => _ = extension.LinearVelocity);
                using var rebound = new ControlledState(body, () => Check(runtime.View is null, "Reattachment binds without a stale cached view"));
                Check(rebound.InverseMass > 0, "New attachment permits a fresh extension");
            }
            Console.WriteLine($"{backend}: independent scene/raw extension binding, prepared mass/geometry, cache disposal, owner and attachment guards passed.");
        }
        finally { PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(area); }
    }
}
