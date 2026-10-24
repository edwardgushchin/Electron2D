using Electron2D;

internal static class PhysicsPointGravityFailureTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        FiniteLimit(backend); ReplacedOverflow(backend);
        foreach (var role in new[] { "scene", "server", "world" })
            foreach (var sign in new[] { 1f, -1f })
                foreach (var center in new[] { new Vector2(2, 0), new Vector2(0, 2), new Vector2(2, 2) })
                    Failure(backend, role, sign, center);
        Console.WriteLine($"Point field failure contract {backend}: 18 scene/server/world overflow cases, finite maxima on both axes/signs and priority replacement passed.");
    }
    private static void Failure(PhysicsServer.Backend backend, string role, float sign, Vector2 center)
    {
        using var world = new World(backend); using var shape = new CircleShape { Radius = 20 }; using var receiver = new CircleShape { Radius = 1 };
        using var root = new SubViewport { World = world }; var body = new RigidBody { CanSleep = false, CollisionMask = 0 };
        body.AddChild(new CollisionShape { Shape = receiver }); root.AddChild(body);
        var area = new Area { GravitySpaceOverride = role == "scene" ? Area.SpaceOverride.Replace : Area.SpaceOverride.Disabled, GravityPoint = true, Gravity = sign * 10, GravityPointCenter = center, GravityPointUnitDistance = 2, Monitoring = false };
        area.AddChild(new CollisionShape { Shape = shape }); root.AddChild(area); using var tree = new SceneTree(root);
        var raw = PhysicsServer.AreaCreate(); var rawShape = PhysicsServer.CircleShapeCreate();
        try
        {
            PhysicsServer.AreaSetGravity(world.Space, 0); PhysicsServer.ShapeSetData(rawShape, shape); PhysicsServer.AreaAddShape(raw, rawShape); PhysicsServer.AreaSetSpace(raw, world.Space);
            var target = role == "world" ? world.Space : raw;
            PhysicsServer.AreaSetGravityPoint(target, true); PhysicsServer.AreaSetGravityVector(target, center); PhysicsServer.AreaSetGravity(target, sign * 10); PhysicsServer.AreaSetGravityPointUnitDistance(target, 2);
            if (role == "server") PhysicsServer.AreaSetGravitySpaceOverride(raw, Area.SpaceOverride.Replace);
            tree.PhysicsFrame(.001); var before = body.GetGravity(); Check(before.IsFinite() && before.Length() > 1, "Baseline point field is observed.");
            body.GlobalPosition = Vector2.Zero; body.LinearVelocity = Vector2.Zero;
            if (role == "scene") area.GravityPointUnitDistance = float.MaxValue; else PhysicsServer.AreaSetGravityPointUnitDistance(target, float.MaxValue);
            Reject<AggregateException>(() => tree.PhysicsFrame(.001)); Check(body.GetGravity() == before, "Overflow preserves last published field.");
            if (backend == PhysicsServer.Backend.GPU)
            {
                Reject<AggregateException>(() => tree.PhysicsFrame(.001));
                if (role == "scene") Reject<InvalidOperationException>(() => area.GravityPointUnitDistance = 2); else Reject<InvalidOperationException>(() => PhysicsServer.AreaSetGravityPointUnitDistance(target, 2));
                Check(world.PhysicsBackend == backend && world.RequestedPhysicsBackend == backend && world.PhysicsFallbackReason == null, "Started field failure retains backend without replay.");
            }
            else
            {
                if (role == "scene") area.GravityPointUnitDistance = 2; else PhysicsServer.AreaSetGravityPointUnitDistance(target, 2);
                tree.PhysicsFrame(.001); Check(body.GetGravity().IsEqualApprox(before), "CPU overflow can be corrected.");
            }
        }
        finally { PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(rawShape); }
    }
    private static void FiniteLimit(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world }; var body = new CharacterBody(); root.AddChild(body); using var tree = new SceneTree(root);
        PhysicsServer.AreaSetGravityPoint(world.Space, true); PhysicsServer.AreaSetGravityPointUnitDistance(world.Space, 1); PhysicsServer.AreaSetGravity(world.Space, float.MaxValue);
        foreach (var direction in new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) })
        {
            PhysicsServer.AreaSetGravityVector(world.Space, direction);
            tree.PhysicsFrame(.001); Check(body.GetGravity() == direction * float.MaxValue, "Maximum finite point force is representable and retained on both axes and signs.");
        }
    }
    private static void ReplacedOverflow(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var shape = new CircleShape { Radius = 20 }; using var root = new SubViewport { World = world };
        var body = new CharacterBody(); body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
        var high = new Area { Name = "High", Priority = 10, GravityPoint = true, GravityPointCenter = new(2, 0), GravityPointUnitDistance = float.MaxValue, Gravity = 100, GravitySpaceOverride = Area.SpaceOverride.Combine, Monitoring = false };
        var low = new Area { Name = "Low", Priority = 5, GravityDirection = new(0, -1), Gravity = 20, GravitySpaceOverride = Area.SpaceOverride.Replace, Monitoring = false };
        high.AddChild(new CollisionShape { Shape = shape }); low.AddChild(new CollisionShape { Shape = shape }); root.AddChild(high); root.AddChild(low); using var tree = new SceneTree(root);
        tree.PhysicsFrame(.001); Check(body.GetGravity() == new Vector2(0, -20), "Later Replace discards an invalid earlier gravity contribution.");
        low.GravitySpaceOverride = Area.SpaceOverride.Combine; Reject<AggregateException>(() => tree.PhysicsFrame(.001));
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
