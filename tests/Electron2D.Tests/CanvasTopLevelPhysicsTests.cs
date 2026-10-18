using Electron2D;

internal static class CanvasTopLevelPhysicsTests
{
    internal static void Run(PhysicsServer.Backend backend, bool threaded) => Run(() => new World(backend), backend.ToString(), threaded);
    internal static void Run(Func<World> create, string label, bool threaded)
    {
        var previous = ProjectSettings.Get(ProjectSettings.PhysicsInterpolation);
        var previousThreaded = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, true);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            using var world = create(); using var root = new SubViewport { World = world };
            using var shape = new CircleShape { Radius = 1 };
            var parent = new Probe(); var independent = new Probe { TopLevel = true }; var neutral = new Node(); var nested = new Probe();
            var body = new Body { GravityScale = 0, CanSleep = false, LinearVelocity = new(3, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
            body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(parent); parent.AddChild(body);
            parent.AddChild(independent); parent.AddChild(neutral); neutral.AddChild(nested);
            using var tree = new SceneTree(root);
            var rid = body.GetRID(); var changes = 0;
            tree.PhysicsFrameStarted += _ =>
            {
                var position = body.Position; var velocity = body.LinearVelocity; var tick = PhysicsServer.SpaceGetTick(world.Space);
                var resets = (body.Resets, independent.Resets, nested.Resets);
                parent.TopLevel = !parent.TopLevel;
                Check(body.Resets == resets.Item1 + 1 && independent.Resets == resets.Item2 + 1 && nested.Resets == resets.Item3 + 1,
                    "A physical subtree resets exactly once through spatial, TopLevel and neutral boundaries");
                Check(body.Position == position && body.LinearVelocity == velocity && body.GetRID() == rid && PhysicsServer.SpaceGetTick(world.Space) == tick,
                    "Presentation reset preserves the exact body pose, velocity, RID and current world tick");
                changes++;
            };
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
            var tick = PhysicsServer.SpaceGetTick(world.Space); var start = body.Position.X; var count = changes;
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0 && changes == count + 128 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128 && body.Position.X > start,
                $"128 complete moving/rebinding physics frames retain strict 0/0 owner/all-thread managed bytes: {owner}/{all}");
            var resetsBefore = body.Resets; parent.TopLevel = parent.TopLevel;
            Check(body.Resets == resetsBefore, "Equal TopLevel assignment is a true no-op");
            tree.PhysicsInterpolation = false; resetsBefore = body.Resets; parent.TopLevel = !parent.TopLevel;
            Check(body.Resets == resetsBefore, "Disabled global interpolation does not emit a rebinding reset");
            tree.PhysicsInterpolation = true;
            Reject<InvalidOperationException>(() => Task.Run(() => parent.TopLevel = !parent.TopLevel).GetAwaiter().GetResult());
            Console.WriteLine($"TopLevel physics {label}/{threaded}: exact recursive resets, live pose/velocity/RID/tick preservation and 128 moving full frames 0/0 B.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previousThreaded);
            ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, previous);
        }
    }
    private sealed class Probe : Entity
    {
        internal int Resets;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationResetPhysicsInterpolation) Resets++; }
    }
    private sealed class Body : RigidBody
    {
        internal int Resets;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationResetPhysicsInterpolation) Resets++; }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}");
    }
}
