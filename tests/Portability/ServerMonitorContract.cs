using Electron2D;
using Monitor = Electron2D.Performance.Monitor;

internal static class ServerMonitorContract
{
    private static readonly Monitor[] Aggregate = [Monitor.NavigationActiveMaps, Monitor.NavigationRegionCount,
        Monitor.NavigationAgentCount, Monitor.NavigationLinkCount, Monitor.NavigationPolygonCount, Monitor.NavigationEdgeCount,
        Monitor.NavigationEdgeMergeCount, Monitor.NavigationEdgeConnectionCount, Monitor.NavigationEdgeFreeCount, Monitor.NavigationObstacleCount];
    private static readonly Monitor[] Planar = [Monitor.Navigation2DActiveMaps, Monitor.Navigation2DRegionCount,
        Monitor.Navigation2DAgentCount, Monitor.Navigation2DLinkCount, Monitor.Navigation2DPolygonCount, Monitor.Navigation2DEdgeCount,
        Monitor.Navigation2DEdgeMergeCount, Monitor.Navigation2DEdgeConnectionCount, Monitor.Navigation2DEdgeFreeCount, Monitor.Navigation2DObstacleCount];

    internal static void Run()
    {
        Check((int)Monitor.AudioOutputLatency == 23, "Audio identity.");
        for (var i = 0; i < 10; i++) Check((int)Aggregate[i] == 24 + i && (int)Planar[i] == 39 + i, "Navigation identities.");
        Check(Task.Run(() => Performance.GetMonitor(Monitor.AudioOutputLatency)).GetAwaiter().GetResult() == 0,
            "Cold audio diagnostics need no device or owner initialization.");
        NavigationServer.Synchronize();
        var baseline = new int[10];
        for (var i = 0; i < 10; i++) baseline[i] = NavigationServer.GetProcessInfo((NavigationServer.ProcessInfo)i);
        var expected = new int[10];
        var map = NavigationServer.MapCreate(); var first = NavigationServer.RegionCreate(); var second = NavigationServer.RegionCreate();
        var agent = NavigationServer.AgentCreate(); var link = NavigationServer.LinkCreate(); var obstacle = NavigationServer.ObstacleCreate();
        using var a = new NavigationPolygon(); using var b = new NavigationPolygon();
        a.SetVertices([new(0, 0), new(10, 0), new(10, 10), new(0, 10)]); a.AddPolygon([0, 1, 2]); a.AddPolygon([0, 2, 3]);
        b.SetVertices([new(10, 0), new(20, 0), new(20, 10), new(10, 10)]); b.AddPolygon([0, 1, 2, 3]);
        try
        {
            NavigationServer.MapSetActive(map, true); NavigationServer.MapSetUseEdgeConnections(map, false);
            NavigationServer.MapSetUseAsyncIterations(map, false);
            NavigationServer.RegionSetUseAsyncIterations(first, false); NavigationServer.RegionSetUseAsyncIterations(second, false);
            NavigationServer.RegionSetMap(first, map); NavigationServer.RegionSetMap(second, map);
            NavigationServer.RegionSetNavigationPolygon(first, a); NavigationServer.RegionSetNavigationPolygon(second, b);
            NavigationServer.AgentSetMap(agent, map); NavigationServer.LinkSetMap(link, map); NavigationServer.ObstacleSetMap(obstacle, map);
            Counts(baseline);
            NavigationServer.Synchronize();
            Expect([1, 2, 1, 1, 3, 9, 1, 1, 0, 1]);
            NavigationServer.RegionSetEnabled(second, false);
            Counts(expected);
            NavigationServer.Synchronize();
            Counts(expected);
            NavigationServer.MapSetUseEdgeConnections(map, true); NavigationServer.MapSetEdgeConnectionMargin(map, 0);
            NavigationServer.RegionSetNavigationPolygon(second, null);
            Counts(expected);
            NavigationServer.Synchronize();
            Expect([1, 2, 1, 1, 2, 5, 1, 0, 4, 1]);
            Task.Run(() => Counts(expected)).GetAwaiter().GetResult();
            for (var i = 0; i < 128; i++) ReadAll();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 256; i++) ReadAll();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Prepared navigation/audio reads allocate {owner}/{all} owner/all-thread bytes.");
            NavigationServer.MapSetActive(map, false); Counts(expected); NavigationServer.Synchronize(); Counts(baseline);
            NavigationServer.MapSetActive(map, true); NavigationServer.Synchronize(); Counts(expected);
        }
        finally
        {
            NavigationServer.FreeRID(agent); NavigationServer.FreeRID(link); NavigationServer.FreeRID(obstacle);
            NavigationServer.FreeRID(first); NavigationServer.FreeRID(second); NavigationServer.FreeRID(map); NavigationServer.Synchronize();
        }
        Counts(baseline);
        foreach (var invalid in new[] { -1, 4, 35, 49, 59, int.MaxValue })
        {
            try { Performance.GetMonitor((Monitor)invalid); throw new InvalidOperationException("Undefined monitor accepted."); }
            catch (ArgumentOutOfRangeException) { }
        }
        Console.WriteLine("Public server monitors: all 20 navigation identities, staged/publication/disabled/retired maps, worker reads and 256 zero-allocation batches passed.");

        void Expect(ReadOnlySpan<int> delta)
        {
            for (var i = 0; i < 10; i++) expected[i] = baseline[i] + delta[i];
            Counts(expected);
        }
    }

    private static void Counts(ReadOnlySpan<int> expected)
    {
        for (var i = 0; i < 10; i++)
            Check(Performance.GetMonitor(Aggregate[i]) == expected[i] && Performance.GetMonitor(Planar[i]) == expected[i] &&
                NavigationServer.GetProcessInfo((NavigationServer.ProcessInfo)i) == expected[i], $"Committed navigation count {i}: aggregate={Performance.GetMonitor(Aggregate[i])}, planar={Performance.GetMonitor(Planar[i])}, expected={expected[i]}");
    }

    internal static void RunAudio()
    {
        var rendering = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            Check(Task.Run(() => Performance.GetMonitor(Monitor.AudioOutputLatency)).GetAwaiter().GetResult() == 0, "Cold worker diagnostics leave output ownership available to the native session.");
            using var stream = new AudioStreamWAV
            {
                SampleFormat = AudioStreamWAV.Format.PCM16,
                MixRate = 48000,
                Data = new byte[960],
                Loop = AudioLoopMode.Forward,
                LoopEnd = 479
            };
            using var world = new World(PhysicsServer.Backend.CPU);
            using var window = new Window { Size = new(128, 96), World = world };
            var player = new AudioStreamPlayer { Stream = stream }; window.AddChild(player); var completed = false;
            window.Ready += node =>
            {
                player.Play(); AudioServer.Lock();
                try
                {
                    var latency = AudioServer.GetOutputLatency();
                    Check(latency > 0 && double.IsFinite(latency) && Performance.GetMonitor(Monitor.AudioOutputLatency) == latency,
                        "Diagnostics read the actual native output-buffer sample in seconds.");
                    Check(Task.Run(() => Performance.GetMonitor(Monitor.AudioOutputLatency)).GetAwaiter().GetResult() == latency,
                        "An active native snapshot is readable on a worker.");
                    for (var i = 0; i < 128; i++) _ = Performance.GetMonitor(Monitor.AudioOutputLatency);
                    var before = GC.GetAllocatedBytesForCurrentThread();
                    for (var i = 0; i < 256; i++) _ = Performance.GetMonitor(Monitor.AudioOutputLatency);
                    var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                    Check(bytes == 0, "Active prepared audio snapshot reads allocate zero owner bytes.");
                    Console.WriteLine($"Public active audio monitor: {latency:R} seconds, 256 reads, {bytes} owner bytes.");
                }
                finally { AudioServer.Unlock(); }
                completed = true; window.Tree!.Quit();
            };
            Check(Engine.Run(window) == 0 && completed, "Native public audio session completes.");
            Check(Performance.GetMonitor(Monitor.AudioOutputLatency) == 0, "Engine teardown retires the diagnostic output sample.");
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, rendering); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }

    private static void ReadAll()
    {
        for (var i = 0; i < 10; i++) { _ = Performance.GetMonitor(Aggregate[i]); _ = Performance.GetMonitor(Planar[i]); }
        _ = Performance.GetMonitor(Monitor.AudioOutputLatency);
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
