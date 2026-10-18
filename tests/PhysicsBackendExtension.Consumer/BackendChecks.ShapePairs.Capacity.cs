using Electron2D;

public static partial class BackendChecks
{
    private static void ShapePairCapacity(World world, SceneTree tree)
    {
        var edges = new Vector2[40];
        for (var i = 0; i < 20; i++) { edges[2 * i] = new(-10, i + 1); edges[2 * i + 1] = new(10, i + 1); }
        using var terrain = new ConcavePolygonShape { Segments = edges }; using var circle = new CircleShape { Radius = 50 };
        int[] capacities = [0, 1, 4, 24];
        var output = new Vector2[48]; var sentinel = new Vector2(701, 709); var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionLayer(body, 0); PhysicsServer.BodySetCollisionMask(body, 0);
            PhysicsServer.BodyAddShape(body, terrain.GetRID(), disabled: true); PhysicsServer.BodySetShapeAsOneWayCollision(body, 0, true, 2, Vector2.Up);
            Check(PhysicsServer.BodyCollideShape(body, 0, circle.GetRID(), Transform.Identity, Vector2.Zero, output, out var count) && count == 20,
                "Detached logical pairs accept caller capacity above the resource-array sixteen-pair limit");
            PhysicsServer.BodySetSpace(body, world.Space);
            for (var i = 0; i < 128; i++) Frame();
            Collect(); var tick = PhysicsServer.SpaceGetTick(world.Space); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128,
                $"Dense shape-pair caller capacities allocate {own}/{all} B");
            Console.WriteLine($"Dense shape pairs {world.PhysicsBackendName}: 20 independent edge contacts, both orders and disabled raw slot; 128 complete frames {own}/{all} B.");
        }
        finally { PhysicsServer.FreeRID(body); }

        void Frame()
        {
            foreach (var capacity in capacities)
                for (var route = 0; route < 3; route++)
                {
                    Array.Fill(output, sentinel); var result = output.AsSpan(0, 2 * capacity);
                    var hit = route == 0 ? PhysicsServer.ShapeCollide(world.Space, terrain.GetRID(), Transform.Identity, new(0, 5000),
                        circle.GetRID(), Transform.Identity, Vector2.Zero, result, out var count)
                        : route == 1 ? PhysicsServer.ShapeCollide(world.Space, circle.GetRID(), Transform.Identity, Vector2.Zero,
                            terrain.GetRID(), Transform.Identity, new(0, 5000), result, out count)
                        : PhysicsServer.BodyCollideShape(body, 0, circle.GetRID(), Transform.Identity, Vector2.Zero, result, out count);
                    var expected = Math.Min(20, capacity); uint seen = 0;
                    Check(hit && count == expected, "Caller capacity controls the exact number of retained contact pairs");
                    for (var i = 0; i < count; i++)
                    {
                        var onTerrain = output[2 * i + (route == 1 ? 1 : 0)]; var onCircle = output[2 * i + (route == 1 ? 0 : 1)];
                        var edge = (int)MathF.Round(onTerrain.Y);
                        Check(MathF.Abs(onTerrain.X) < .001f && MathF.Abs(onTerrain.Y - edge) < .001f && edge >= 1 && edge <= expected &&
                            MathF.Abs(onCircle.Length() - 50) < .001f, "Deepest contacts lie on the original analytical line/circle boundaries");
                        seen |= 1u << (edge - 1);
                    }
                    Check(seen == (1u << expected) - 1, "Capacity truncation retains distinct deepest edges");
                    for (var i = count * 2; i < output.Length; i++) Check(output[i] == sentinel, "Every unused output tail remains caller-owned");
                }
            tree.PhysicsFrame(1d / 120);
        }
    }
}
