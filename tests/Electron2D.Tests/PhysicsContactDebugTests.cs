using System.Diagnostics;
using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsContactDebugTests
{
    internal static void Run(bool includeGPU = true)
    {
        Settings();
        Verify(PhysicsServer.Backend.CPU); ScenePolicy(PhysicsServer.Backend.CPU); Measure(PhysicsServer.Backend.CPU, false); Measure(PhysicsServer.Backend.CPU, true);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_CONTACT_DEBUG_CPU_ONLY") == "1")
            Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "CPU contact capture needs no display/renderer");
        else if (includeGPU)
        { Verify(PhysicsServer.Backend.GPU); ScenePolicy(PhysicsServer.Backend.GPU); FailedGPUCleanup(); Measure(PhysicsServer.Backend.GPU, false); Measure(PhysicsServer.Backend.GPU, true); }
    }
    private static void Settings()
    {
        var color = ProjectSettings.Get(ProjectSettings.DebugCollisionContactColor);
        var limit = ProjectSettings.Get(ProjectSettings.DebugCollisionMaxContacts);
        Reject<System.Text.Json.JsonException>(() => ProjectSettings.Set(ProjectSettings.DebugCollisionContactColor, new Color(float.NaN, 0, 0)));
        Reject<ArgumentException>(() => ProjectSettings.Set(ProjectSettings.DebugCollisionMaxContacts, -1));
        Check(ProjectSettings.Get(ProjectSettings.DebugCollisionContactColor) == color && ProjectSettings.Get(ProjectSettings.DebugCollisionMaxContacts) == limit, "Invalid project policy preserves stored values");
    }
    private static void FailedGPUCleanup()
    {
        using var f = new Fixture(PhysicsServer.Backend.GPU, 1);
        f.Data.SetDebugContacts(8); f.Step(); PhysicsServer.AreaSetGravity(f.Space, 980);
        Reject<AggregateException>(() => PhysicsServer.SpaceStep(f.Space, float.MaxValue));
        Check(f.Data.HasBackendFailure, "Started GPU failure remains terminal with diagnostics enabled");
        f.Data.SetDebugContacts(0);
        Check(f.Data.DebugContactLimit == 0, "Disabling diagnostics remains available for failed-world cleanup");
        Reject<InvalidOperationException>(() => _ = f.Data.DebugContacts.Length);
        Reject<InvalidOperationException>(() => f.Data.SetDebugContacts(8));
        Reject<InvalidOperationException>(f.Step);
        Console.WriteLine("Failed GPU diagnostics disable and release without authorizing further reads or steps.");
    }
    private static void Verify(PhysicsServer.Backend backend)
    {
        using var f = new Fixture(backend, 1); var space = f.Data;
        f.Step(); Check(space.DebugContacts.IsEmpty, "Contact capture is off by default, independently of solver contacts");
        space.SetDebugContacts(16); f.Reset(); f.Step();
        Check(space.DebugContacts.Length == 2, "One circle manifold has two boundary samples");
        var end = PhysicsServer.BodyGetTransform(f.Dynamic[0]).Origin; var fixedBoundary = false;
        foreach (var point in space.DebugContacts)
        {
            Check(point.IsFinite() && Math.Abs(point.X) < .02f && point.Y >= 5 - .02f && point.Y <= 10 + .02f,
                "Samples lie on the normal between initial penetrating surfaces, within .02 scene units");
            fixedBoundary |= Math.Abs(point.Y - 10) < .02f;
            if (Math.Abs(point.Y - 10) >= .02f) Check(point.Y <= end.Y - 10 + .02f, "Moving surface sample precedes final pose advancement");
        }
        Check(fixedBoundary, "The fixed circle contributes its exact surface point");
        Check(PhysicsServer.BodyGetMaxContactsReported(f.Dynamic[0]) == 0, "Debugging does not alter gameplay contact reporting");
        space.SetDebugContacts(1); f.Reset(); f.Step(); Check(space.DebugContacts.Length == 1, "Odd cap bounds points, not pairs");
        Reject<ArgumentOutOfRangeException>(() => space.SetDebugContacts(int.MaxValue));
        Reject<ArgumentOutOfRangeException>(() => space.SetDebugContacts(-1)); Check(space.DebugContactLimit == 1, "Invalid cap preserves state");
        Reject<InvalidOperationException>(() => Task.Run(() => space.SetDebugContacts(8)).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => _ = space.DebugContacts.Length).GetAwaiter().GetResult());
        space.SetDebugContacts(16); f.Reset(); f.Step(); var version = space.DebugContactRevision;
        PhysicsServer.SpaceStep(f.Space, 0); Check(space.DebugContactRevision == version && space.DebugContacts.Length == 2, "Zero time preserves the last completed snapshot");
        PhysicsServer.BodySetCanSleep(f.Dynamic[0], true); PhysicsServer.BodySetSleeping(f.Dynamic[0], true); f.Step(); Check(space.DebugContacts.IsEmpty, "Sleeping pairs are not active diagnostic contacts");
        PhysicsServer.BodySetSleeping(f.Dynamic[0], false); f.Reset(); f.Step(); Check(space.DebugContacts.Length == 2, "Wake restores diagnostic contacts");
        PhysicsServer.BodySetCollisionMask(f.Dynamic[0], 0); PhysicsServer.BodySetCollisionMask(f.Static[0], 0); f.Step(); Check(space.DebugContacts.IsEmpty, "Collision filtering removes contacts");
        PhysicsServer.BodySetCollisionMask(f.Dynamic[0], uint.MaxValue); PhysicsServer.BodySetCollisionMask(f.Static[0], uint.MaxValue);
        PhysicsServer.BodySetTransform(f.Dynamic[0], new(0, new Vector2(100, 100))); f.Step(); Check(space.DebugContacts.IsEmpty, "Separated bodies clear old points");
        var area = PhysicsServer.AreaCreate();
        try
        {
            PhysicsServer.AreaAddShape(area, f.Shape.GetRID()); PhysicsServer.AreaSetTransform(area, new(0, new Vector2(100, 100))); PhysicsServer.AreaSetSpace(area, f.Space);
            f.Step(); Check(space.DebugContacts.IsEmpty, "Sensors do not create contact markers");
        }
        finally { PhysicsServer.FreeRID(area); }
        f.Reset(); f.Step(); space.SetDebugContacts(0); Check(space.DebugContacts.IsEmpty, "Disable clears snapshot immediately");
        var reads = space.GPUStore?.DebugContactReadCount ?? 0; var captures = space.GPUStore?.DebugContactCaptureCount ?? 0; f.Step(); Check((space.GPUStore?.DebugContactReadCount ?? 0) == reads && (space.GPUStore?.DebugContactCaptureCount ?? 0) == captures, "Disabled capture submits no debug GPU work");
        space.SetDebugContacts(16); PhysicsServer.FreeRID(f.Dynamic[0]); f.Dynamic[0] = default; f.Step(); Check(space.DebugContacts.IsEmpty, "Body removal clears samples");
        Console.WriteLine($"Contact diagnostics {backend}: boundary coordinates, limits, no gameplay reporting changes, zero-time, sleep/wake, filtering/sensors, removal and guards passed.");
    }
    private static void ScenePolicy(PhysicsServer.Backend backend)
    {
        var saved = ProjectSettings.Get(ProjectSettings.DebugCollisionMaxContacts);
        try
        {
            ProjectSettings.Set(ProjectSettings.DebugCollisionMaxContacts, 3);
            using var world = new World(backend); using var view = new SubViewport { World = world };
            using var shape = new CircleShape { Radius = 10 };
            var floor = new StaticBody { Name = "Floor" }; floor.AddChild(new CollisionShape { Shape = shape }); view.AddChild(floor);
            var body = new RigidBody { Name = "Body", Position = new(0, 15), GravityScale = 0, CanSleep = false }; body.AddChild(new CollisionShape { Shape = shape }); view.AddChild(body);
            using (var tree = new SceneTree(view))
            {
                ProjectSettings.Set(ProjectSettings.DebugCollisionMaxContacts, 0);
                tree.DebugCollisionsHint = true; tree.PhysicsFrame(1d / 60);
                var space = PhysicsServer.Service.GetSceneSpace(world.Space);
                Check(space.DebugContactLimit == 3 && space.DebugContacts.Length == 2, "Tree retains construction-time limit, including an odd cap");
                tree.DebugCollisionsHint = false; Check(space.DebugContacts.IsEmpty && space.DebugContactLimit == 0, "Tree toggle disables capture immediately");
                tree.DebugCollisionsHint = true; tree.PhysicsFrame(1d / 60);
            }
            Check(PhysicsServer.Service.GetSceneSpace(world.Space).DebugContactLimit == 0, "Surviving caller world loses the tree's diagnostic request on detach");
            using var zeroView = new SubViewport(); using var zeroTree = new SceneTree(zeroView);
            zeroTree.DebugCollisionsHint = true; zeroTree.PhysicsFrame(1d / 60);
            Check(zeroTree.DebugContactLimit == 0, "A new tree samples zero to keep shape diagnostics without contact capture");
            ProjectSettings.Set(ProjectSettings.DebugCollisionMaxContacts, int.MaxValue);
            using var invalidView = new SubViewport { World = world };
            var invalidBody = new StaticBody(); invalidBody.AddChild(new CollisionShape { Shape = shape }); invalidView.AddChild(invalidBody);
            using var invalidTree = new SceneTree(invalidView);
            Reject<ArgumentOutOfRangeException>(() => invalidTree.DebugCollisionsHint = true);
            Check(!invalidTree.DebugCollisionsHint && PhysicsServer.Service.GetSceneSpace(world.Space).DebugContactLimit == 0, "Failed preparation leaves the tree and world diagnostics disabled");
        }
        finally { ProjectSettings.Set(ProjectSettings.DebugCollisionMaxContacts, saved); }
    }
    private static void Measure(PhysicsServer.Backend backend, bool debug)
    {
        using var f = new Fixture(backend, 256); f.Data.SetDebugContacts(debug ? 128 : 0);
        for (var i = 0; i < 96; i++) { f.Reset(); f.Step(); }
        var samples = new double[64]; var resetTimes = new double[64]; var stepTimes = new double[64]; var gpu = f.Data.GPUStore;
        long resetSubmissions = 0;
        var up = gpu?.UploadBytes ?? 0; var down = gpu?.ReadbackBytes ?? 0; var wait = gpu?.WaitMS ?? 0;
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < samples.Length; i++)
        {
            var submits = gpu?.SubmissionCount ?? 0; var start = Stopwatch.GetTimestamp(); f.Reset();
            resetTimes[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; resetSubmissions += (gpu?.SubmissionCount ?? 0) - submits;
            var stepStart = Stopwatch.GetTimestamp(); f.Step();
            stepTimes[i] = Stopwatch.GetElapsedTime(stepStart).TotalMilliseconds;
            samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, $"Contact diagnostic complete steps allocate {owner}/{all} managed B");
        Check(f.Data.DebugContacts.Length == (debug ? 128 : 0), "All benchmark samples retain the configured population and cap");
        Array.Sort(samples); Array.Sort(resetTimes); Array.Sort(stepTimes);
        Console.WriteLine($"Contact debug {backend}/{debug}: 256 independent circle pairs, 96 warmup/64 samples, reset poses/velocities + full 1/60 s step; p50/p95/p99={samples[32]:F4}/{samples[60]:F4}/{samples[63]:F4} ms, reset/step p50={resetTimes[32]:F4}/{stepTimes[32]:F4} ms, reset submissions={resetSubmissions / 64}, {owner}/{all} owner/all-thread managed B, GPU up/down={((gpu?.UploadBytes ?? 0) - up) / 64}/{((gpu?.ReadbackBytes ?? 0) - down) / 64} B, wait={((gpu?.WaitMS ?? 0) - wait) / 64:F4} ms.");
    }
    private sealed class Fixture : IDisposable
    {
        internal readonly RID Space;
        internal readonly PhysicsSpace Data;
        internal readonly CircleShape Shape = new() { Radius = 10 };
        internal readonly RID[] Static, Dynamic;
        internal Fixture(PhysicsServer.Backend backend, int count)
        {
            Space = PhysicsServer.SpaceCreate(backend); Data = PhysicsServer.Service.GetSceneSpace(Space);
            PhysicsServer.SpaceSetActive(Space, true); PhysicsServer.AreaSetGravity(Space, 0);
            Static = new RID[count]; Dynamic = new RID[count];
            for (var i = 0; i < count; i++)
            {
                Static[i] = PhysicsServer.BodyCreate(); Dynamic[i] = PhysicsServer.BodyCreate();
                PhysicsServer.BodySetMode(Static[i], PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCanSleep(Dynamic[i], false);
                PhysicsServer.BodySetTransform(Static[i], new(0, new Vector2(i * 50, 0)));
                PhysicsServer.BodyAddShape(Static[i], Shape.GetRID()); PhysicsServer.BodyAddShape(Dynamic[i], Shape.GetRID());
                PhysicsServer.BodySetSpace(Static[i], Space); PhysicsServer.BodySetSpace(Dynamic[i], Space);
            }
            Reset();
        }
        internal void Reset() { for (var i = 0; i < Dynamic.Length; i++) { PhysicsServer.BodySetTransform(Dynamic[i], new(0, new Vector2(i * 50, 15))); PhysicsServer.BodySetLinearVelocity(Dynamic[i], Vector2.Zero); } }
        internal void Step() => PhysicsServer.SpaceStep(Space, 1d / 60);
        public void Dispose() { foreach (var rid in Dynamic) if (rid.IsValid()) PhysicsServer.FreeRID(rid); foreach (var rid in Static) PhysicsServer.FreeRID(rid); PhysicsServer.FreeRID(Space); Shape.Dispose(); }
    }
}
