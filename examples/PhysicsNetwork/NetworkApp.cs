using Electron2D;
using System.Text.Json;

namespace Electron2D.Examples.PhysicsNetwork;

internal static class NetworkApp
{
    internal static async Task<int> Run(string[] args)
    {
        string Value(string name, string fallback)
        {
            var at = Array.IndexOf(args, name);
            if (at < 0) return fallback;
            if (at + 1 == args.Length) throw new ArgumentException("Missing value for " + name);
            return args[at + 1];
        }
        try
        {
            if (args.Contains("--check")) return await ProcessCheck.Run(args);
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, args.Contains("--threaded"));
            var authority = args.Contains("--server");
            var windowed = args.Contains("--window");
            if (windowed && (authority || args.Contains("--require-no-gpu"))) throw new ArgumentException("Window presentation requires a graphics-capable client.");
            var backend = Value("--backend", "cpu") switch
            {
                "cpu" => PhysicsServer.Backend.CPU,
                "gpu" => PhysicsServer.Backend.GPU,
                _ => throw new ArgumentException("Backend must be cpu or gpu.")
            };
            var requireNoGPU = args.Contains("--require-no-gpu");
            var clientPollDelayMS = int.Parse(Value("--client-poll-delay-ms", "0"));
            if (clientPollDelayMS is < 0 or > 250) throw new ArgumentException("Client poll delay must be between 0 and 250 milliseconds.");
            if ((windowed || authority) && clientPollDelayMS != 0) throw new ArgumentException("Slow polling is only available to headless clients.");
            if (requireNoGPU && backend != PhysicsServer.Backend.CPU) throw new ArgumentException("GPU denial verification requires CPU selection.");
            var gpuUnavailable = requireNoGPU && VerifyGPUUnavailable();
            var port = int.Parse(Value("--port", "7777")); var ticks = int.Parse(Value("--ticks", "360"));
            if (ticks is < 360 or > 1800) throw new ArgumentException("Tick count must be between 360 and 1800.");
            var lateJoinTick = int.Parse(Value("--late-join-tick", "90"));
            if (lateJoinTick < 90 || lateJoinTick >= ticks) throw new ArgumentException("Late join must be between tick 90 and the final tick.");
            var token = Guid.Parse(Value("--token", "dccdb93a-948e-4dda-b4ec-714c7dd69a91"));
            var reportPath = Value("--report", authority ? "physics-server.json" : "physics-client.json");
            using var session = new NetworkSession(authority, backend, port, token, ticks, args.Contains("--impaired"), args.Contains("--check-progress"));
            if (authority) { Console.WriteLine($"LISTENING {session.Port}"); Console.Out.Flush(); }
            else { Console.WriteLine("PREPARED"); Console.Out.Flush(); }
            var windowReport = new SessionReport();
            if (windowed) NetworkWindow.Run(session, windowReport, Value("--renderer", "gpu"), Value("--capture", ""));
            var announcedLateJoin = false;
            while (!session.Done)
            {
                session.Poll();
                if (authority && !announcedLateJoin && session.Simulation.Tick >= (ulong)lateJoinTick)
                {
                    Console.WriteLine($"LATE_JOIN_READY {session.Simulation.Tick}"); Console.Out.Flush(); announcedLateJoin = true;
                }
                Thread.Sleep(1 + clientPollDelayMS);
            }
            var report = session.Report(); report.GPUUnavailableVerified = gpuUnavailable;
            report.ClientPollDelayMS = clientPollDelayMS;
            report.SeparateThread = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DRunOnSeparateThread);
            report.RequestedWindowVSync = windowReport.RequestedWindowVSync; report.WindowVSync = windowReport.WindowVSync; report.WindowMaxFPS = windowReport.WindowMaxFPS;
            report.Renderer = windowReport.Renderer; report.WindowFrames = windowReport.WindowFrames; report.WindowFPS = windowReport.WindowFPS;
            report.WindowFramePercentilesMS = windowReport.WindowFramePercentilesMS;
            report.WindowOwnerAllocations = windowReport.WindowOwnerAllocations; report.WindowTotalAllocations = windowReport.WindowTotalAllocations;
            report.RenderedSmoothedSamples = windowReport.RenderedSmoothedSamples; report.RenderedInterpolatedSamples = windowReport.RenderedInterpolatedSamples;
            report.RenderedPhysicsPreserved = windowReport.RenderedPhysicsPreserved;
            if ((authority || requireNoGPU) && backend == PhysicsServer.Backend.CPU && (DisplayServer.IsAvailable || RenderingServer.IsAvailable)) throw new InvalidOperationException("Dedicated CPU authority created a graphics service.");
            File.WriteAllText(reportPath, JsonSerializer.Serialize(report, ReportJSON.Default.SessionReport));
            Console.WriteLine($"COMPLETE {report.Role} {report.Backend}: tick {report.Tick}, {report.Corrections} corrections, {report.ReplayedTicks} replayed ticks");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static bool VerifyGPUUnavailable()
    {
        var space = default(RID);
        try { space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU); }
        catch (InvalidOperationException) { return true; }
        finally { if (space.IsValid()) PhysicsServer.FreeRID(space); }
        throw new InvalidOperationException("A no-GPU CPU process unexpectedly created a GPU physics space.");
    }

}
