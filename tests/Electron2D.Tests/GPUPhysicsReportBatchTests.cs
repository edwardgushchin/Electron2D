using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;
using static PhysicsDebugTests;

internal static class GPUPhysicsReportBatchTests
{
    internal static void Run()
    {
        using var store = new Store { CaptureContactReports = true };
        using var circle = new CircleShape { Radius = 10 };
        using var ground = new RectangleShape { Size = new(1000, 20) };
        var floor = store.Add(new(Mode.Static, new(0, 100), 0, default, 0));
        var body = store.Add(new(Mode.Rigid, new(0, 80), 0, default, 0, Mass: 2, CanSleep: false, LockRotation: true));
        var floorShape = store.AddShape(floor, ground); var bodyShape = store.AddShape(body, circle);
        Store.BodyHandle[] bodies = [body]; int[] limits = [4], counts = new int[1];
        var states = new Store.Snapshot[1]; var reports = new Store.ContactReport[4];
        for (var mode = 0; mode < 4; mode++)
        {
            store.SeparateSimulationSubmissions = mode == 1;
            store.SetCCDMode(body, mode == 2 ? CCDMode.CastShape : CCDMode.Disabled);
            var shared = Measure(false); var separate = Measure(true);
            Console.WriteLine($"Report completion mode={mode}: shared={shared}, separate={separate} over 64 complete cycles.");
            Check(separate.Submissions - shared.Submissions == 64, "Final report freezing shares exactly one completion per full cycle");
            var savedBytes = mode is 0 or 3 ? 64 * 8 : 0;
            Check(separate.Upload - shared.Upload == savedBytes && separate.Readback - shared.Readback == savedBytes,
                "A final solver consumes the current device report count without a second status reset/download");

            (long Submissions, long Upload, long Readback) Measure(bool split)
            {
                store.SeparateReportSubmissions = split;
                for (var i = 0; i < 128; i++) Step();
                AllocationWarmupCheck.Collect();
                var submissions = store.SubmissionCount; var upload = store.UploadBytes; var readback = store.ReadbackBytes;
                var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 64; i++) Step();
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Console.WriteLine($"Report mode={mode}, split={split}: warmed owner/all={owner}/{all} B.");
                Check(owner == 0 && all == 0, "Complete warmed author/solve/state/contact-read cycles allocate zero owner/all bytes");
                return (store.SubmissionCount - submissions, store.UploadBytes - upload, store.ReadbackBytes - readback);
            }
            void Step()
            {
                store.SetPose(body, new(0, 80), 0); store.SetSolverVelocity(body, new(100, 40), 0);
                if (mode == 3) store.SolveConstraints(.02f, iterations: 16, margin: 0);
                else store.Simulate(.02f, Vector2.Zero, substeps: mode == 0 ? 1 : 4, iterations: 16, margin: 0);
                store.Read(bodies, states, new Store.ContactRead(bodies, limits, counts, reports));
                Check(counts[0] > 0, "The first and every later completed frame publishes its actual contacts");
                var total = Vector2.Zero; var velocity = new Vector2(states[0].Velocity.X, states[0].Velocity.Y);
                for (var i = 0; i < counts[0]; i++)
                {
                    var report = reports[i]; total += report.Impulse;
                    Check(report.Shape == bodyShape.Index && report.ShapeGeneration == bodyShape.Generation &&
                        report.ColliderShape == floorShape.Index && report.ColliderBody == floor.Index,
                        "Completed contacts retain receiver/collider generations and identities");
                    Check(report.Velocity.DistanceTo(velocity) < .001f && report.ColliderVelocity == Vector2.Zero,
                        "Final point velocities follow the locked body's completed state within .001 scene units/second");
                }
                Check(total.DistanceTo((velocity - new Vector2(100, 40)) * 2) < .02f,
                    "Complete normal/friction impulse equals momentum change within .02 scene momentum units");
            }
        }
        Console.WriteLine("Shared final contact completion passed for fused/split integration, CCD and solve-only work.");
    }
}
