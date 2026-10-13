using Electron2D;

internal static class PhysicsPointGravityRangeTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var region = new CircleShape { Radius = 20 }; using var circle = new CircleShape { Radius = 1 };
        using var root = new SubViewport { World = world };
        var area = new Area { GravityPoint = true, GravitySpaceOverride = Area.SpaceOverride.Replace, Monitoring = false };
        Add(area, region); root.AddChild(area);
        var rigid = new RigidBody { Name = "Rigid", CanSleep = false, CollisionMask = 0 }; Add(rigid, circle); root.AddChild(rigid);
        var character = new CharacterBody { Name = "Character", CollisionMask = 0 }; Add(character, circle); root.AddChild(character);
        using var tree = new SceneTree(root); Check(PhysicsServer.SpaceGetBackend(world.Space) == backend, "Explicit field backend.");
        var body = PhysicsServer.BodyCreate(); var shape = PhysicsServer.CircleShapeCreate(); var rawArea = PhysicsServer.AreaCreate(); var regionShape = PhysicsServer.CircleShapeCreate();
        try
        {
            PhysicsServer.ShapeSetData(shape, circle); PhysicsServer.BodyAddShape(body, shape); PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid);
            PhysicsServer.BodySetCollisionMask(body, 0); PhysicsServer.BodySetCanSleep(body, false); PhysicsServer.BodySetSpace(body, world.Space);
            PhysicsServer.ShapeSetData(regionShape, region); PhysicsServer.AreaAddShape(rawArea, regionShape); PhysicsServer.AreaSetSpace(rawArea, world.Space);
            PhysicsServer.AreaSetGravityPoint(rawArea, true);
            var cases = new (Vector2 Center, float Gravity, float UnitDistance, Vector2 Expected)[]
            {
                (new(1e-25f, 0), 10, 0, new(10, 0)),
                (new(1e-25f, 0), -10, -1, new(-10, 0)),
                (new(1e-25f, 0), 10, 1e-25f, new(10, 0)),
                (new(1e20f, 0), 10, 1e20f, new(10, 0)),
                (new(1e20f, 1e20f), 10, 1e20f, new(3.5355339f, 3.5355339f)),
                (new(float.MaxValue, float.MaxValue), 10, 0, new(7.0710678f, 7.0710678f)),
                (new(1e-30f, 0), 1e-30f, 1e-10f, new(1e10f, 0)),
                (new(1e-25f, 0), 0, float.MaxValue, Vector2.Zero),
                (Vector2.Zero, float.MaxValue, float.MaxValue, Vector2.Zero)
            };
            for (var profile = 0; profile < 3; profile++)
            {
                area.GravitySpaceOverride = profile == 0 ? Area.SpaceOverride.Replace : Area.SpaceOverride.Disabled;
                PhysicsServer.AreaSetGravitySpaceOverride(rawArea, profile == 1 ? Area.SpaceOverride.Replace : Area.SpaceOverride.Disabled);
                PhysicsServer.AreaSetGravityPoint(world.Space, profile == 2);
                PhysicsServer.AreaSetGravity(world.Space, 0);
                foreach (var sample in cases)
                {
                    area.GravityPointCenter = sample.Center; area.Gravity = sample.Gravity; area.GravityPointUnitDistance = sample.UnitDistance;
                    PhysicsServer.AreaSetGravityVector(rawArea, sample.Center); PhysicsServer.AreaSetGravity(rawArea, sample.Gravity); PhysicsServer.AreaSetGravityPointUnitDistance(rawArea, sample.UnitDistance);
                    if (profile == 2) { PhysicsServer.AreaSetGravityVector(world.Space, sample.Center); PhysicsServer.AreaSetGravity(world.Space, sample.Gravity); PhysicsServer.AreaSetGravityPointUnitDistance(world.Space, sample.UnitDistance); }
                    Reset(rigid, body); tree.PhysicsFrame(.001);
                    Near(rigid.GetGravity(), sample.Expected, "Scene rigid field"); Near(character.GetGravity(), sample.Expected, "Character field");
                    Near(PhysicsServer.BodyGetDirectState(body)!.TotalGravity, sample.Expected, "Raw body field");
                    Check(rigid.Position.IsFinite() && rigid.LinearVelocity.IsFinite() && PhysicsServer.BodyGetTransform(body).Origin.IsFinite(), "The complete solver interval remains finite.");
                }
            }
            area.GravitySpaceOverride = Area.SpaceOverride.Replace; PhysicsServer.AreaSetGravitySpaceOverride(rawArea, Area.SpaceOverride.Disabled);
            area.GravityPointCenter = new(1e20f, 0); area.Gravity = 10; area.GravityPointUnitDistance = 1e20f;
            if (backend == PhysicsServer.Backend.CPU)
            {
                area.GravityPointCenter = new(1e-25f, 0); area.Gravity = float.MaxValue;
                Reset(rigid, body); var prior = rigid.GetGravity();
                var rejected = false;
                try { tree.PhysicsFrame(.001); } catch (AggregateException) { rejected = true; }
                Check(rejected && rigid.GetGravity() == prior && rigid.GetGravity().IsFinite(), "Genuinely unrepresentable force rejects before field-cache publication.");
                area.GravityPointCenter = new(1e20f, 0); area.Gravity = 10;
            }
            for (var i = 0; i < 128; i++) { Reset(rigid, body); tree.PhysicsFrame(.001); }
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) { Reset(rigid, body); tree.PhysicsFrame(.001); }
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
            Check(ownerBytes == 0 && allBytes == 0, "Warmed authored-reset/full field frames allocate zero owner/all-thread bytes.");
            Console.WriteLine($"Point gravity range: backend={backend}, profiles=3, cases={cases.Length}, receivers=3, warmFrames=128, ownerBytes={ownerBytes}, allBytes={allBytes}.");
        }
        finally { PhysicsServer.FreeRID(rawArea); PhysicsServer.FreeRID(regionShape); PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(shape); }
    }
    private static void Reset(RigidBody rigid, RID body)
    {
        rigid.Position = Vector2.Zero; rigid.LinearVelocity = Vector2.Zero;
        PhysicsServer.BodySetTransform(body, Transform.Identity); PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero);
    }
    private static void Add(CollisionObject body, Shape shape) { var owner = body.CreateShapeOwner(null); body.ShapeOwnerAddShape(owner, shape); }
    private static void Near(Vector2 actual, Vector2 expected, string message)
    {
        var tolerance = MathF.Max(.002f, MathF.Max(MathF.Abs(expected.X), MathF.Abs(expected.Y)) * .0002f);
        Check(actual.IsFinite() && MathF.Abs(actual.X - expected.X) <= tolerance && MathF.Abs(actual.Y - expected.Y) <= tolerance, message + $": {actual} vs {expected}");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
