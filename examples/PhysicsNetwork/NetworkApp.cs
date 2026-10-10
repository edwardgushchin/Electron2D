using Electron2D;
using System.Text.Json;

namespace Electron2D.Examples.PhysicsNetwork;

internal static class NetworkApp
{
    internal static async Task<int> Run(string[] args)
    {
        if (args.Contains("--check")) return await ProcessCheck.Run(args);
        string Value(string name, string fallback)
        {
            var at = Array.IndexOf(args, name);
            if (at < 0) return fallback;
            if (at + 1 == args.Length) throw new ArgumentException("Missing value for " + name);
            return args[at + 1];
        }
        try
        {
            var authority = args.Contains("--server");
            var backend = Value("--backend", "cpu") switch
            {
                "cpu" => PhysicsServer.Backend.CPU,
                "gpu" => PhysicsServer.Backend.GPU,
                _ => throw new ArgumentException("Backend must be cpu or gpu.")
            };
            var port = int.Parse(Value("--port", "7777")); var ticks = int.Parse(Value("--ticks", "360"));
            if (ticks is < 360 or > 1800) throw new ArgumentException("Tick count must be between 360 and 1800.");
            var token = Guid.Parse(Value("--token", "dccdb93a-948e-4dda-b4ec-714c7dd69a91"));
            var reportPath = Value("--report", authority ? "physics-server.json" : "physics-client.json");
            using var session = new NetworkSession(authority, backend, port, token, ticks, args.Contains("--impaired"));
            if (authority) { Console.WriteLine($"LISTENING {session.Port}"); Console.Out.Flush(); }
            while (!session.Done) { session.Poll(); Thread.Sleep(1); }
            var report = session.Report();
            if (authority && backend == PhysicsServer.Backend.CPU && (DisplayServer.IsAvailable || RenderingServer.IsAvailable)) throw new InvalidOperationException("Dedicated CPU authority created a graphics service.");
            File.WriteAllText(reportPath, JsonSerializer.Serialize(report, ReportJSON.Default.SessionReport));
            Console.WriteLine($"COMPLETE {report.Role} {report.Backend}: tick {report.Tick}, {report.Corrections} corrections, {report.ReplayedTicks} replayed ticks");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
