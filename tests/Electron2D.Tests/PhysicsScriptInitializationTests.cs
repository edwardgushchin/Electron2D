using Electron2D;

internal static class PhysicsScriptInitializationTests
{
    internal static void Run(PhysicsServer.Backend backend, bool threaded)
    {
        ScriptTests.Register();
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            using var source = ResourceLoader.Load<Script>(RequiredScriptActor.SourcePath, ResourceLoader.CacheMode.Ignore);
            using var shape = new CircleShape { Radius = 1 };
            using var world = new World(backend);
            using var root = new SubViewport { World = world };
            using var actor = source.New(() => new RequiredScriptActor(3));
            actor.Name = "RequiredActor"; actor.AddChild(new CollisionShape { Shape = shape }); root.AddChild(actor);
            using var tree = new SceneTree(root);
            Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "explicit physical backend is active");
            Check(actor.Initializations == 1 && actor.ConstructorSource == source && actor.ConstructorIdentity == actor.InstanceID, "argument-only compiled initialization precedes world attachment");
            var identity = actor.GetRID(); var calls = actor.Frames;
            source.Dispose();
            RejectDefault(source);
            using var query = new PhysicsPointQueryParameters(); var results = new PhysicsPointResult[2];
            for (var i = 0; i < 128; i++) Frame();
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            var tick = PhysicsServer.SpaceGetTick(world.Space); var frames = actor.Frames;
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Initialized full physical frames allocate {owner}/{all} B");
            Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 128 && actor.Frames == frames + 128 && actor.Frames > calls && actor.Initializations == 1,
                "all full intervals advance without rerunning compiled initialization after source disposal");
            Console.WriteLine($"Compiled initialization {backend}/{threaded}: 128 full frames and point queries, owner/all={owner}/{all} B.");
            void Frame()
            {
                var position = actor.Position.X; var before = actor.Frames;
                tree.PhysicsFrame(.01); tree.ProcessFrame(.01);
                query.Position = actor.Position;
                Check(actor.Frames == before + 1 && actor.GetRID() == identity && actor.GetScript() == source && MathF.Abs(actor.Position.X - position - .03f) < .0001f &&
                    world.DirectSpaceState.IntersectPoint(query, results) == 1 && results[0].ColliderRID == identity,
                    "typed constructor state drives actual stable scene physics and queries after asset disposal");
            }
        }
        finally { RequiredScriptActor.Captured = null; ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
    private static void RejectDefault(Script source)
    {
        try { source.New<RequiredScriptActor>(); }
        catch (ObjectDisposedException) { return; }
        throw new InvalidOperationException("A disposed source cannot start new initialization.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
