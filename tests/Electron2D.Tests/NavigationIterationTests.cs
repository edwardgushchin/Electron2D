using Electron2D;

internal static class NavigationIterationTests
{
    internal sealed class SynchronousDefaults : IDisposable
    {
        private readonly bool _map = ProjectSettings.GetWithOverride(ProjectSettings.NavigationMapAsyncIterations), _region = ProjectSettings.GetWithOverride(ProjectSettings.NavigationRegionAsyncIterations);
        internal SynchronousDefaults() { ProjectSettings.Set(ProjectSettings.NavigationMapAsyncIterations, false); ProjectSettings.Set(ProjectSettings.NavigationRegionAsyncIterations, false); }
        public void Dispose() { ProjectSettings.Set(ProjectSettings.NavigationMapAsyncIterations, _map); ProjectSettings.Set(ProjectSettings.NavigationRegionAsyncIterations, _region); }
    }
    private sealed class HoldWorkers : IDisposable
    {
        private readonly ManualResetEventSlim _release = new(); private readonly CountdownEvent _started; private readonly long[] _tasks;
        internal HoldWorkers()
        {
            const int count = 2; // The fixture starts two lanes; later setting edits do not resize the retained pool.
            _started = new(count); _tasks = new long[count];
            for (var i = 0; i < count; i++) _tasks[i] = WorkerThreadPool.AddTask(() => { _started.Signal(); if (!_release.Wait(TimeSpan.FromSeconds(15))) throw new InvalidOperationException("Worker release timed out."); }, true);
            Check(_started.Wait(TimeSpan.FromSeconds(10)), "All configured worker lanes are occupied.");
        }
        public void Dispose() { _release.Set(); foreach (var id in _tasks) WorkerThreadPool.WaitForTaskCompletion(id); _release.Dispose(); _started.Dispose(); }
    }
    internal static void Run()
    {
        if (!WorkerThreadPool.Service.HasRuntime) ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2);
        using var polygon = new NavigationPolygon(); polygon.SetVertices([new(0, 0), new(20, 0), new(20, 20), new(0, 20)]); polygon.AddPolygon([0, 1, 2, 3]);
        var map = NavigationServer.MapCreate(); var region = NavigationServer.RegionCreate(); var callbacks = 0; var callbackThread = 0; var owner = Environment.CurrentManagedThreadId;
        Action<RID> changed = rid => { if (rid == map) { callbacks++; callbackThread = Environment.CurrentManagedThreadId; } };
        try
        {
            Check(NavigationServer.MapGetUseAsyncIterations(map) == !OperatingSystem.IsBrowser() && NavigationServer.RegionGetUseAsyncIterations(region) == !OperatingSystem.IsBrowser(), "Threaded defaults");
            Reject<ArgumentException>(() => NavigationServer.MapGetUseAsyncIterations(region)); Reject<ArgumentException>(() => NavigationServer.RegionSetUseAsyncIterations(map, true));
            NavigationServer.MapSetActive(map, true); NavigationServer.RegionSetMap(region, map); NavigationServer.RegionSetNavigationPolygon(region, polygon); NavigationServer.Synchronize(); NavigationServer.MapChanged += changed;
            if (OperatingSystem.IsBrowser()) { Reject<PlatformNotSupportedException>(() => NavigationServer.MapSetUseAsyncIterations(map, true)); return; }
            var initial = NavigationServer.MapGetIterationID(map);
            using (var hold = new HoldWorkers())
            {
                NavigationServer.RegionSetTransform(region, new Transform(0, new(20, 0))); NavigationServer.Service.SynchronizeCore();
                Check(NavigationServer.MapGetIterationID(map) == initial && NavigationServer.MapGetClosestPoint(map, new(5, 5)) == new Vector2(5, 5), "Queued region keeps the committed map and projection");
                for (var i = 0; i < 8; i++) NavigationServer.Service.SynchronizeCore(); var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) NavigationServer.Service.SynchronizeCore(); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, $"Pending owner synchronization allocates zero bytes: {GC.GetAllocatedBytesForCurrentThread() - bytes}");
                NavigationServer.RegionSetTransform(region, new Transform(0, new(40, 0))); Check(callbacks == 0, "Staged edits and queued work emit no completion signal");
            }
            PumpUntil(() => NavigationServer.MapGetIterationID(map) != initial);
            Check(NavigationServer.MapGetIterationID(map) == initial + 1 && callbacks == 1 && callbackThread == owner && NavigationServer.MapGetClosestPoint(map, new(45, 5)) == new Vector2(45, 5), "Stale inputs are discarded and only complete newest map publishes on owner");
            NavigationServer.RegionSetUseAsyncIterations(region, false); var regionVersion = NavigationServer.RegionGetIterationID(region); var mapVersion = NavigationServer.MapGetIterationID(map);
            using (var hold = new HoldWorkers())
            {
                NavigationServer.RegionSetTransform(region, new Transform(0, new(80, 0))); NavigationServer.Service.SynchronizeCore();
                Check(NavigationServer.RegionGetIterationID(region) == regionVersion + 1 && NavigationServer.MapGetIterationID(map) == mapVersion, "Synchronous region and asynchronous map policies are independent");
                Check(NavigationServer.RegionGetClosestPoint(region, new(85, 5)) == new Vector2(85, 5), "Region projection reads complete new local iteration while map retains old topology");
                NavigationServer.MapSetCellSize(map, 2); NavigationServer.MapSetUseAsyncIterations(map, false); Check(!NavigationServer.MapGetUseAsyncIterations(map), "Toggling mode preserves in-flight ownership");
            }
            PumpUntil(() => NavigationServer.MapGetIterationID(map) != mapVersion); Check(NavigationServer.MapGetCellSize(map) == 2 && NavigationServer.MapGetClosestPoint(map, new(85, 5)) == new Vector2(85, 5), "Changed map inputs supersede pending topology and synchronous retry commits");
            NavigationServer.RegionSetUseAsyncIterations(region, true); NavigationServer.MapSetUseAsyncIterations(map, true);
            NavigationServer.RegionSetTransform(region, new Transform(new(2, 0), new(0, 2), new(float.MaxValue, 0))); var preserved = NavigationServer.MapGetIterationID(map); var rejected = false;
            for (var i = 0; i < 5000 && !rejected; i++) { try { NavigationServer.Service.SynchronizeCore(); } catch (ArgumentException) { rejected = true; } if (!rejected) Thread.Sleep(1); }
            Check(rejected && NavigationServer.MapGetIterationID(map) == preserved, "Worker geometry failure surfaces on owner and preserves prior map");
            NavigationServer.RegionSetTransform(region, Transform.Identity); NavigationServer.Synchronize(); Check(NavigationServer.MapGetClosestPoint(map, new(5, 5)) == new Vector2(5, 5), "Deterministic explicit host synchronization repairs current input");
            NavigationServer.RegionSetTransform(region, new Transform(0, new(100, 0))); NavigationServer.Service.SynchronizeCore(); NavigationServer.FreeRID(region); region = default; NavigationServer.Synchronize(); Check(NavigationServer.MapGetRegions(map).Length == 0 && NavigationServer.MapGetPath(map, Vector2.Zero, Vector2.One, true).Length == 0, "RID release drains owned captured work and removes live region");
        }
        finally { NavigationServer.MapChanged -= changed; if (region.IsValid()) NavigationServer.FreeRID(region); NavigationServer.FreeRID(map); }
        if (!OperatingSystem.IsBrowser()) FailedReadyBatch();
        Console.WriteLine("Navigation iterations passed: independent worker policies, retained queries, stale-input coalescing, owner publication/failure/retry, deterministic host commit, RID retirement and pending zero allocation.");
    }
    private static void FailedReadyBatch()
    {
        using var polygon = new NavigationPolygon(); polygon.SetVertices([new(0, 0), new(20, 0), new(20, 20), new(0, 20)]); polygon.AddPolygon([0, 1, 2, 3]);
        var maps = new[] { NavigationServer.MapCreate(), NavigationServer.MapCreate() }; var regions = new[] { NavigationServer.RegionCreate(), NavigationServer.RegionCreate() }; var callbacks = 0;
        Action<RID> changed = rid => { if (maps.Contains(rid)) callbacks++; };
        try
        {
            for (var i = 0; i < 2; i++) { NavigationServer.MapSetActive(maps[i], true); NavigationServer.RegionSetMap(regions[i], maps[i]); NavigationServer.RegionSetNavigationPolygon(regions[i], polygon); NavigationServer.RegionSetUseAsyncIterations(regions[i], false); }
            NavigationServer.Synchronize(); var versions = maps.Select(NavigationServer.MapGetIterationID).ToArray(); NavigationServer.MapChanged += changed;
            using (var hold = new HoldWorkers()) { foreach (var region in regions) NavigationServer.RegionSetTransform(region, new Transform(0, new(20, 0))); NavigationServer.Service.SynchronizeCore(); }
            var field = typeof(NavigationServer).GetField("_maps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var bank = (Dictionary<RID, NavigationMapState>)field.GetValue(NavigationServer.Service)!;
            foreach (var map in maps) { var task = bank[map].Pending!.TaskID; Check(SpinWait.SpinUntil(() => WorkerThreadPool.IsTaskCompleted(task), TimeSpan.FromSeconds(10)), "Map computation completes before owner publication"); }
            // Inject a captured computation failure after both actual workers finish.
            bank[maps[1]].Pending!.Failure = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(new ArgumentException("Fixture map failure."));
            Reject<ArgumentException>(() => NavigationServer.Service.SynchronizeCore());
            Check(callbacks == 0 && maps.Select(NavigationServer.MapGetIterationID).SequenceEqual(versions), "A failed ready map batch leaves every map and notification intact");
            NavigationServer.Synchronize(); Check(callbacks == 2 && maps.Select(NavigationServer.MapGetIterationID).SequenceEqual(versions.Select(v => v + 1)), "Host retry publishes both maps and their completion notifications");
        }
        finally { NavigationServer.MapChanged -= changed; foreach (var region in regions) NavigationServer.FreeRID(region); foreach (var map in maps) NavigationServer.FreeRID(map); }
    }
    private static void PumpUntil(Func<bool> predicate)
    { for (var i = 0; i < 5000; i++) { NavigationServer.Service.SynchronizeCore(); if (predicate()) return; Thread.Sleep(1); } throw new InvalidOperationException("Navigation iteration completion timed out."); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
