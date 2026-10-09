using System.Diagnostics;
using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsGPUPublicationPerformance
{
    internal static void Run()
    {
        var counts = (Environment.GetEnvironmentVariable("ELECTRON2D_PUBLICATION_COUNTS") ?? "512,4096,16384,65536").Split(',');
        var previous = PhysicsSpace.ProfilingEnabled;
        PhysicsSpace.ProfilingEnabled = Environment.GetEnvironmentVariable("ELECTRON2D_PUBLICATION_PROFILE") == "1";
        try
        {
            foreach (var text in counts)
            {
                var count = int.Parse(text); Check(count is >= 2 and <= 65536 && count % 2 == 0, "Use an even body count from 2 through 65536");
                Measure(PhysicsServer.Backend.CPU, count); Measure(PhysicsServer.Backend.GPU, count);
            }
        }
        finally { PhysicsSpace.ProfilingEnabled = previous; }
    }
    private static void Measure(PhysicsServer.Backend backend, int count)
    {
        const int warmup = 64, samples = 64;
        var pairs = count / 2; var columns = (int)Math.Ceiling(Math.Sqrt(pairs));
        var space = PhysicsServer.SpaceCreate(backend); using var shape = new CircleShape { Radius = 4 };
        var fixedBodies = new RID[pairs]; var dynamicBodies = new RID[pairs]; var poses = new Transform[pairs];
        var data = PhysicsServer.Service.GetSceneSpace(space); var gpu = data.GPUStore;
        try
        {
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.AreaSetGravity(space, 0);
            PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
            for (var i = 0; i < pairs; i++)
            {
                var fixedPosition = new Vector2(i % columns * 12, i / columns * 20);
                fixedBodies[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(fixedBodies[i], PhysicsServer.BodyMode.Static);
                PhysicsServer.BodySetTransform(fixedBodies[i], new(0, fixedPosition));
                PhysicsServer.BodyAddShape(fixedBodies[i], shape.GetRID()); PhysicsServer.BodySetSpace(fixedBodies[i], space);
                poses[i] = new(0, fixedPosition + new Vector2(0, 7));
                dynamicBodies[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetCanSleep(dynamicBodies[i], false);
                PhysicsServer.BodySetTransform(dynamicBodies[i], poses[i]); PhysicsServer.BodyAddShape(dynamicBodies[i], shape.GetRID()); PhysicsServer.BodySetSpace(dynamicBodies[i], space);
            }
            for (var i = 0; i < warmup; i++) { Reset(); PhysicsServer.SpaceStep(space, 1d / 60); }
            var phases = new double[8]; var preparation = new double[4];
            var total = new double[samples]; var reset = new double[samples]; var steps = new double[samples];
            var up = gpu?.UploadBytes ?? 0; var down = gpu?.ReadbackBytes ?? 0; var submits = gpu?.SubmissionCount ?? 0; var wait = gpu?.WaitMS ?? 0; var publications = gpu?.ChangePublicationCount ?? 0;
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < samples; i++)
            {
                var start = Stopwatch.GetTimestamp(); Reset(); var split = Stopwatch.GetTimestamp(); reset[i] = Stopwatch.GetElapsedTime(start, split).TotalMilliseconds;
                PhysicsServer.SpaceStep(space, 1d / 60); var end = Stopwatch.GetTimestamp();
                steps[i] = Stopwatch.GetElapsedTime(split, end).TotalMilliseconds; total[i] = Stopwatch.GetElapsedTime(start, end).TotalMilliseconds;
                if (PhysicsSpace.ProfilingEnabled)
                {
                    for (var phase = 0; phase < phases.Length; phase++) phases[phase] += data.ProfileMS[phase];
                    preparation[0] += data.GPUPrepareBodiesMS; preparation[1] += data.GPUPrepareReportsMS;
                    preparation[2] += data.GPUPrepareWakesMS; preparation[3] += data.GPUPrepareWakeWaitMS;
                }
            }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            up = (gpu?.UploadBytes ?? 0) - up; down = (gpu?.ReadbackBytes ?? 0) - down; submits = (gpu?.SubmissionCount ?? 0) - submits; wait = (gpu?.WaitMS ?? 0) - wait; publications = (gpu?.ChangePublicationCount ?? 0) - publications;
            Check(owner == 0 && all == 0, $"Complete mass-world steps allocated {owner}/{all} managed B");
            Check(gpu is null || gpu.Count == count, "The resident world retains the actual requested body population");
            foreach (var index in new[] { 0, pairs / 2, pairs - 1 })
            {
                var pose = PhysicsServer.BodyGetTransform(dynamicBodies[index]);
                Check(pose.IsFinite() && pose.Origin.Y > poses[index].Origin.Y + .1f && Math.Abs(pose.Origin.X - poses[index].Origin.X) < .01f,
                    "Sampled real contacts separate the circles by at least .1 scene units without tangential drift above .01");
            }
            Array.Sort(total); Array.Sort(reset); Array.Sort(steps);
            Console.WriteLine($"Public publication world {backend}, {count} bodies/{pairs} independent colliding pairs: {warmup} warmup/{samples} samples, all dynamic poses/linear velocities reset + full 1/60 s step, 4 substeps/16 iterations, sleeping disabled; whole p50/p95/p99={total[32]:F4}/{total[60]:F4}/{total[63]:F4} ms, reset/step p50={reset[32]:F4}/{steps[32]:F4}; {owner}/{all} owner/all managed B; GPU up/down={up / samples}/{down / samples} B, submissions/publications={submits / samples}/{publications / samples}, wait={wait / samples:F4} ms/tick.");
            if (backend == PhysicsServer.Backend.GPU && PhysicsSpace.ProfilingEnabled)
            {
                Console.WriteLine($"  GPU preparation detail means: parameters/motion/joints {preparation[0] / samples:F4}, report selection {preparation[1] / samples:F4}, command/wake publication {preparation[2] / samples:F4} ms (included wake wait {preparation[3] / samples:F4} ms).");
                var names = new[] { "attachments", "pre-publication/fields", "body/joint preparation/wakes", "resident simulation/debug", "post-publication/reports", "scene/server completion", "contacts/areas/views", "callbacks/events" };
                for (var i = 0; i < phases.Length; i++) Console.WriteLine($"  GPU phase {names[i]}: mean {phases[i] / samples:F4} ms.");
            }
        }
        finally
        {
            foreach (var body in dynamicBodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
            foreach (var body in fixedBodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
            PhysicsServer.FreeRID(space);
        }
        void Reset()
        { for (var i = 0; i < pairs; i++) { PhysicsServer.BodySetTransform(dynamicBodies[i], poses[i]); PhysicsServer.BodySetLinearVelocity(dynamicBodies[i], Vector2.Zero); } }
    }
}
