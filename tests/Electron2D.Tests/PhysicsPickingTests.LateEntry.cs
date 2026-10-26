using Electron2D;

internal static partial class PhysicsPickingTests
{
    internal static void LateNativeEntry(PhysicsServer.Backend backend, bool separate)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var picking = ProjectSettings.Get(ProjectSettings.PhysicsObjectPicking);
        ProjectSettings.Set(ProjectSettings.PhysicsObjectPicking, true);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        try
        {
            foreach (var authored in new[] { false, true })
            {
                using var f = new Fixture(backend, authored);
                var warm = new SubViewport { Name = "Warm", World = f.World, Size = f.View.Size, PhysicsObjectPicking = true };
                f.View.AddChild(warm);
                var moving = new RigidBody { Position = new(200, 200), GravityScale = 0, CanSleep = false, LinearVelocity = new(1, 0) };
                moving.AddChild(new CollisionShape { Shape = f.Shape }); f.View.AddChild(moving);
                using (var mouse = new InputEventMouseMotion { Position = new(20, 20) }) warm.PushInput(mouse, true);
                for (var i = 0; i < 96; i++) f.Step();
                AllocationWarmupCheck.Collect();
                var tick = PhysicsServer.SpaceGetTick(f.World.Space);
                var pose = moving.Position;
                f.Tree.EnterGUIViewport(f.View);
                var owner = GC.GetAllocatedBytesForCurrentThread(); var total = GC.GetTotalAllocatedBytes(true);
                f.Step();
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; total = GC.GetTotalAllocatedBytes(true) - total;
                Check(PhysicsServer.SpaceGetTick(f.World.Space) == tick + 1 && moving.Position.X > pose.X &&
                    f.View.Picking is { MouseKnown: true }, "Late native entry reaches picking in a complete moving physics frame");
                Console.WriteLine($"Late native picking {backend}/{(separate ? "separate" : "owner")}: {(authored ? "authored" : "project default")}, {owner}/{total} managed B.");
                Check(owner == 0 && total == 0, $"Late native entry allocated {owner}/{total} managed bytes inside the warmed physics frame");
                owner = GC.GetAllocatedBytesForCurrentThread(); total = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 64; i++)
                {
                    f.View.PhysicsObjectPicking = false; f.View.PhysicsObjectPicking = true;
                    f.Step();
                }
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; total = GC.GetTotalAllocatedBytes(true) - total;
                Check(owner == 0 && total == 0, $"Prepared picking reactivation allocated {owner}/{total} managed bytes");
            }
            using var unused = new SubViewport { PhysicsObjectPicking = true };
            var parameters = unused.Picking!.Parameters; var passive = unused.Picking.PassiveMouse;
            unused.Dispose();
            Check(parameters.IsDisposed && passive.IsDisposed, "Viewport disposal retires both prepared picking objects without requiring pointer input");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous);
            ProjectSettings.Set(ProjectSettings.PhysicsObjectPicking, picking);
        }
    }
}
