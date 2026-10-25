using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;
using static PhysicsDebugTests;

internal static class GPUPhysicsCCDBatchTests
{
    internal static void Run()
    {
        using var circle = new CircleShape { Radius = 1 };
        using var store = new Store();
        var mover = store.Add(new(Mode.Rigid, Vector2.Zero, 0, new(100, 0), 0, CanSleep: false, ContinuousMode: CCDMode.CastShape));
        var other = store.Add(new(Mode.Static, new(3, 1.9f), 0, default, 0));
        store.AddShape(mover, circle); var obstacle = store.AddShape(other, circle);
        var combined = Measure(false); var separate = Measure(true);
        Console.WriteLine($"CCD pair/sweep completion: combined={combined}, separate={separate} over 64 complete reset/simulation cycles.");
        Check(separate.Queries > 0 && separate.Submissions - combined.Submissions == separate.Queries,
            "Fresh swept-pair discovery and its CCD query share exactly one completion");
        Check(combined.Readback <= separate.Readback + 64 * 4 * 8 && combined.Upload <= separate.Upload + 64 * 4 * 8,
            "Empty-pair CCD status adds at most eight bytes per nominal interval without a new buffer");
        store.SeparateCCDSubmissions = false;
        store.SetPose(other, new(3000, 0), 0); Step();
        Check(store.PairCount == 0, "A previously populated pair buffer does not expose stale pairs to an empty sweep");
        store.RemoveShape(obstacle); store.AddShape(other, circle); Step();
        Check(store.PairCount == 0, "Reused shape generations retain empty CCD behavior");
        VerifyColdRecovery();
        Console.WriteLine("CCD shared completion, empty/stale pairs, cold capacity recovery and force cadence passed.");

        (long Submissions, long Upload, long Readback, long Queries) Measure(bool split)
        {
            store.SeparateCCDSubmissions = split;
            for (var i = 0; i < 128; i++) Step();
            AllocationWarmupCheck.Collect();
            var submissions = store.SubmissionCount; var upload = store.UploadBytes; var readback = store.ReadbackBytes; var queries = store.CCDQueryCount;
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Step();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Console.WriteLine($"CCD split={split}: warmed owner/all={owner}/{all} B.");
            Check(owner == 0 && all == 0, "Complete warmed reset/simulation cycles allocate zero owner/all managed bytes");
            var measured = (store.SubmissionCount - submissions, store.UploadBytes - upload, store.ReadbackBytes - readback, store.CCDQueryCount - queries);
            Span<Store.Snapshot> state = stackalloc Store.Snapshot[1]; store.Read([mover], state);
            Check(state[0].Position.DistanceTo(new(2, 0)) < .0001f && new Vector2(state[0].Velocity.X, state[0].Velocity.Y) == new Vector2(100, 0),
                "The AABB candidate is a genuine geometric miss; .0001 scene units bounds four float position additions");
            return measured;
        }
        void Step()
        {
            store.SetPose(mover, Vector2.Zero, 0); store.SetSolverVelocity(mover, new(100, 0), 0);
            store.Simulate(.02f, Vector2.Zero, substeps: 4, iterations: 16, margin: 0);
        }
    }

    private static void VerifyColdRecovery()
    {
        using var store = new Store(); using var circle = new CircleShape { Radius = 1 };
        using var wall = new RectangleShape { Size = new(.2f, 2) };
        var movers = new Store.BodyHandle[128];
        for (var i = 0; i < movers.Length; i++)
        {
            movers[i] = store.Add(new(Mode.Rigid, new(0, i * 10), 0, new(3000, 0), 0, CanSleep: false, ContinuousMode: CCDMode.CastShape));
            store.AddShape(movers[i], circle, friction: 0, bounce: .5f);
            var fixedBody = store.Add(new(Mode.Static, new(20, i * 10), 0, default, 0)); store.AddShape(fixedBody, wall, friction: 0, bounce: .5f);
        }
        var free = store.Add(new(Mode.Rigid, new(0, 10000), 0, default, 0, CanSleep: false)); store.SetConstantForce(free, new(100, 0));
        store.Simulate(.02f, Vector2.Zero, substeps: 1, iterations: 32, margin: 0, bounceThreshold: 0);
        Check(store.PairCapacityRetries > 0, "Swept pairs grow beyond the initially empty contact batch");
        Span<Store.Snapshot> state = stackalloc Store.Snapshot[1];
        foreach (var body in movers)
        {
            store.Read([body], state);
            Check(MathF.Abs(state[0].Velocity.X + 3000) < .01f && MathF.Abs(state[0].Position.X + 22.2f) < .02f,
                "Every recovered candidate keeps the elastic thin-wall bounce; .01 velocity/.02 position allow float impact refinement");
        }
        store.Read([free], state);
        Check(MathF.Abs(state[0].Velocity.X - 2) < .0001f && MathF.Abs(state[0].Position.X - .04f) < .0001f,
            "Pair-capacity retries never repeat the outer force interval; .0001 bounds float integration");
    }
}
