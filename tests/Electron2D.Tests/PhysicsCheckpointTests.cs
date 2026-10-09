using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsCheckpointTests
{
    internal static void Run(bool gpu = false)
    {
        var backend = gpu ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
        ServerTicks(backend); SceneReplay(backend); Warm(backend);
        Console.WriteLine($"Public {backend} checkpoints: tick/empty/inactive/callback semantics, local replay, identity/lifetime and zero warmed allocation passed.");
    }
    private static void ServerTicks(PhysicsServer.Backend backend)
    {
        var space = PhysicsServer.SpaceCreate(backend); var other = PhysicsServer.SpaceCreate(backend); var body = default(RID);
        PhysicsCheckpoint? point = null;
        try
        {
            Check(PhysicsServer.SpaceGetTick(space) == 0, "New worlds start at tick zero");
            PhysicsServer.SpaceStep(space, 1d / 60); Check(PhysicsServer.SpaceGetTick(space) == 0, "Inactive world has no tick");
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceSetActive(other, true);
            PhysicsServer.SetActive(false);
            try { PhysicsServer.SpaceStep(space, 1d / 60); Check(PhysicsServer.SpaceGetTick(space) == 0, "Global suspension has no tick"); }
            finally { PhysicsServer.SetActive(true); }
            PhysicsServer.SpaceStep(space, 0); Check(PhysicsServer.SpaceGetTick(space) == 0, "Zero duration has no tick");
            PhysicsServer.SpaceStep(space, 1d / 60); Check(PhysicsServer.SpaceGetTick(space) == 1, "An empty active world still advances");
            point = PhysicsServer.SpaceCreateCheckpoint(space); Check(point.Tick == 1 && !point.IsDisposed, "Capture records the actual tick without advancing it");
            PhysicsServer.SpaceStep(space, 1d / 60); PhysicsServer.SpaceStep(other, 1d / 60); point.Restore();
            Check(PhysicsServer.SpaceGetTick(space) == 1 && PhysicsServer.SpaceGetTick(other) == 1, "Rollback only changes the owning world");
            PhysicsServer.SpaceStep(space, .1); Check(PhysicsServer.SpaceGetTick(space) == 2, "One variable-duration interval is still one tick");
            point.Capture(); Check(point.Tick == 2, "Recapture replaces tick metadata");
            body = PhysicsServer.BodyCreate(); PhysicsServer.BodySetSpace(body, space);
            Reject<InvalidOperationException>(point.Restore); Check(PhysicsServer.SpaceGetTick(space) == 2, "Invalid topology does not rewind the tick");
            var callbacks = 0;
            PhysicsServer.BodySetStateSyncCallback(body, _ =>
            {
                callbacks++; Check(PhysicsServer.SpaceGetTick(space) == 3, "Completed tick is visible inside result callback");
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceCreateCheckpoint(space));
                throw new InvalidOperationException("Expected callback failure");
            });
            Reject<AggregateException>(() => PhysicsServer.SpaceStep(space, 1d / 60));
            Check(callbacks == 1 && PhysicsServer.SpaceGetTick(space) == 3, "Callback failure does not undo a solved interval");
            PhysicsServer.BodySetStateSyncCallback(body, null); point.Capture();
            Check(OffOwner(point.Capture), "Capture requires the owner thread");
            Check(OffOwner(point.Dispose), "Foreign disposal cannot release the checkpoint");
            Check(!point.IsDisposed, "Failed disposal preserves state");
            PhysicsServer.SpaceSetActive(space, false); point.Restore(); Check(!PhysicsServer.SpaceIsActive(space) && point.Tick == 3, "Restore preserves scheduler activity policy");
            PhysicsServer.FreeRID(body); body = default; PhysicsServer.FreeRID(space); space = default;
            Check(point.IsDisposed, "World disposal invalidates its public checkpoints"); Reject<ObjectDisposedException>(point.Restore); Reject<ObjectDisposedException>(() => _ = point.Tick); point.Dispose();
            Reject<ArgumentException>(() => PhysicsServer.SpaceGetTick(default)); Reject<ArgumentException>(() => PhysicsServer.SpaceCreateCheckpoint(default));
        }
        finally { point?.Dispose(); if (body.IsValid()) PhysicsServer.FreeRID(body); if (space.IsValid()) PhysicsServer.FreeRID(space); PhysicsServer.FreeRID(other); }
    }
    private static void SceneReplay(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 10 };
        var body = new RigidBody { Name = "Body", GravityScale = 0, CanSleep = false }; body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var i = 0; i < 3; i++) tree.PhysicsFrame(1d / 60);
        using var point = PhysicsServer.SpaceCreateCheckpoint(world.Space);
        Check(point.Tick == 3, "Scene-owned spaces use the same tick contract");
        Advance(); var expectedPose = body.Position; var expectedVelocity = body.LinearVelocity;
        Check(PhysicsServer.SpaceGetTick(world.Space) == 15, "Scene replay fixture advanced twelve real intervals");
        point.Restore(); Check(PhysicsServer.SpaceGetTick(world.Space) == 3 && body.Position.Length() < .0001f, "Public rollback restores tick and physical pose");
        Advance();
        // Float unit-conversion tolerance: no contacts in this controlled input replay.
        Check(body.Position.DistanceTo(expectedPose) < .001f && body.LinearVelocity.DistanceTo(expectedVelocity) < .001f, "The same numbered inputs reproduce the trajectory");
        void Advance()
        {
            for (var i = 0; i < 12; i++)
            {
                var nextTick = PhysicsServer.SpaceGetTick(world.Space) + 1;
                if (nextTick == 4) body.ApplyCentralImpulse(new(30, 10));
                if (nextTick == 8) body.ApplyCentralForce(new(-12, 4));
                tree.PhysicsFrame(1d / 60);
            }
        }
    }
    private static bool OffOwner(Action action)
    {
        var rejected = false;
        var thread = new Thread(() => { try { action(); } catch (InvalidOperationException) { rejected = true; } });
        thread.Start(); thread.Join(); return rejected;
    }
    private static void Warm(PhysicsServer.Backend backend)
    {
        var space = PhysicsServer.SpaceCreate(backend); var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetSpace(body, space); PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceStep(space, 1d / 60);
            using var point = PhysicsServer.SpaceCreateCheckpoint(space);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
            GC.WaitForPendingFinalizers();
            _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 96; i++) { point.Capture(); point.Restore(); _ = PhysicsServer.SpaceGetTick(space); _ = point.Tick; }
            var all = GC.GetTotalAllocatedBytes(true); var own = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) { point.Capture(); point.Restore(); _ = PhysicsServer.SpaceGetTick(space); _ = point.Tick; }
            var bytes = GC.GetAllocatedBytesForCurrentThread() - own; var total = GC.GetTotalAllocatedBytes(true) - all;
            Check(bytes == 0 && total == 0, $"Public prepared capture/restore/tick: {bytes}/{total} owner/all bytes");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
    }
}
