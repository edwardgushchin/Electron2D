using Electron2D;
using System.Runtime.CompilerServices;

internal static class ObjectCensusChecks
{
    private static double Read(Performance.Monitor monitor) => Performance.GetMonitor(monitor);
    private static double Objects => Read(Performance.Monitor.ObjectCount);
    private static double Resources => Read(Performance.Monitor.ObjectResourceCount);
    private static double Nodes => Read(Performance.Monitor.ObjectNodeCount);
    private static double Orphans => Read(Performance.Monitor.ObjectOrphanNodeCount);
    internal static void Run(string[] args)
    {
        if (args.Length > 0) { Native(args[0], args[1]); return; }
        using (var warm = new ElectronObject()) { _ = Objects; _ = Resources; _ = Nodes; _ = Orphans; }
        using (var warm = new Node()) { }
        using (var warm = new Resource()) { }
        Collect();
        var count = Objects;
        var plain = new ElectronObject(); Check(Objects == count + 1, "Construction publishes an object exactly once.");
        plain.Dispose(); plain.Dispose(); Check(Objects == count, "Idempotent disposal excludes a retained object.");
        var canceled = new Canceling();
        canceled.Before = Objects;
        canceled.Dispose(); Check(!canceled.IsDisposed && Objects == canceled.Before, "Pre-delete cancellation keeps registration.");
        canceled.Cancel = false; canceled.Dispose(); Check(Objects == count, "Later committed disposal retires canceled identity.");
        var throwing = new Throwing();
        try { throwing.Dispose(); throw new InvalidOperationException("Expected disposal error."); } catch (NotSupportedException) { }
        Check(throwing.IsDisposed && Objects == count, "Throwing cleanup commits terminal identity.");
        WeakLifetime();
        CacheLifetime();
        SceneLifetime();
        ConcurrentLifetime();
        for (var i = 0; i < 32; i++) Query();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 4096; i++) Query();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
        Check(allocated == 0, "Prepared census reads allocate zero managed bytes.");
        Check(!RenderingServer.IsAvailable, "Managed diagnostics never open a rendering device.");
        Console.WriteLine($"Object census passed; warmed owner bytes={allocated}; objects={Objects}; cached resources={Resources}; attached nodes={Nodes}; orphans={Orphans}.");
        GC.KeepAlive(plain); GC.KeepAlive(canceled); GC.KeepAlive(throwing);
    }
    private static void Query() { _ = Objects; _ = Resources; _ = Nodes; _ = Orphans; }
    private static void WeakLifetime()
    {
        Collect(); var count = Objects; var orphans = Orphans;
        var weak = Abandon(count, orphans);
        Collect(); Check(!Alive(weak) && Objects == count && Orphans == orphans, "Census never retains an abandoned graph or invokes cleanup.");
        var failed = Fail(); Collect(); Check(!Alive(failed) && Objects == count, "Failed external construction is reclaimed without finalization.");
        for (var i = 0; i < 4096; i++) new ElectronObject().Dispose();
        Check(Objects == count, "Large disposed cohorts do not inflate counts.");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<ElectronObject> Abandon(double count, double orphans)
    {
        var a = new Node(); var b = new Node(); a.AddChild(b);
        Check(Objects >= count + 2 && Orphans >= orphans + 1, "Reachable undisposed objects remain observable.");
        var weak = new WeakReference<ElectronObject>(a); GC.KeepAlive(a); return weak;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<ElectronObject> Fail()
    {
        WeakReference<ElectronObject> weak = null!;
        try { _ = new Failing(out weak); } catch (NotSupportedException) { }
        return weak;
    }
    [MethodImpl(MethodImplOptions.NoInlining)] private static bool Alive(WeakReference<ElectronObject> weak) => weak.TryGetTarget(out _);
    private static void Collect() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
    private static void CacheLifetime()
    {
        Collect(); var count = Resources;
        using var first = new Resource(); using var second = new Resource();
        Check(Resources == count, "Uncached resources are not path cache entries.");
        var path = "local://census/" + Guid.NewGuid().ToString("N");
        first.ResourcePath = path; Check(Resources == count + 1, "Registered resource counted.");
        second.SetPathCache(path); Check(Resources == count + 1, "Unregistered logical aliases are excluded.");
        second.TakeOverPath(path); Check(Resources == count + 1 && first.ResourcePath == "", "Takeover replaces rather than duplicates an entry.");
        second.ResourcePath = path + "/moved"; Check(Resources == count + 1, "Path movement preserves count.");
        second.ResourcePath = ""; Check(Resources == count, "Clearing a registered path retires the entry.");
        first.ResourcePath = path; first.Dispose(); Check(Resources == count, "Retained disposed cache owner is excluded.");
        var abandoned = AbandonCached(path); Collect(); Check(!Alive(abandoned) && Resources == count, "Dead weak cache entries are excluded.");
    }
    [MethodImpl(MethodImplOptions.NoInlining)] private static WeakReference<ElectronObject> AbandonCached(string path) { var resource = new Resource { ResourcePath = path }; return new(resource); }
    private static void SceneLifetime()
    {
        Collect(); var orphans = Orphans; Check(Nodes == 0, "No attached SceneTree.");
        using var root = new Node(); var child = new Node(); root.AddChild(child);
        using var independentRoot = new Node(); using var independent = new SceneTree(independentRoot);
        using var tree = new SceneTree(root);
        Check(Nodes == 0 && Orphans == orphans + 3, "Independent active trees remain outside the Engine loop.");
        Engine.Start(tree);
        try
        {
            Check(Nodes == 2 && Orphans == orphans + 1, "Only attached hierarchy counted; independent root is an orphan relative to it.");
            Check(Task.Run(() => Nodes == 2 && Orphans == orphans + 1).GetAwaiter().GetResult(), "Workers read without scene ownership.");
            root.RemoveChild(child); Check(Nodes == 1 && Orphans == orphans + 2, "Detachment changes membership.");
            root.AddChild(child); child.QueueFree(); Check(Nodes == 2, "Queued deletion retains a live member.");
            Engine.AdvanceFrame(1d / 60); Check(child.IsDisposed && Nodes == 1, "Committed deferred deletion retires membership.");
        }
        finally { Engine.Stop(); }
        Check(Nodes == 0, "Stopped Engine releases its active node count.");
    }
    private static void ConcurrentLifetime()
    {
        Collect(); var count = Objects;
        var tasks = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 512; i++) { using var value = new ElectronObject(); Check(Objects >= 1 && Resources >= 0, "Concurrent creation, retirement and scans."); }
        })).ToArray();
        Task.WaitAll(tasks); Check(Objects == count, "Concurrent disposed cohorts retire exactly.");
    }
    private static void Native(string backend, string output)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var window = new Window { Title = "Object lifetime monitors", Size = new(96, 64) };
        window.AddChild(new Capture(output, backend)); Engine.Run(window);
        Check(Nodes == 0 && window.IsDisposed, "Native teardown retires active hierarchy.");
    }
    private sealed class Capture(string output, string backend) : Node
    {
        private int _frames;
        private Entity? _box;
        protected override void OnReady() { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested backend executes."); RenderingServer.SetDefaultClearColor(Colors.Black); Parent!.AddChild(_box = new Box()); }
        protected override void OnProcess(double delta)
        {
            if (++_frames < 3) return;
            Check(Nodes == 3, "Native Window, observer and drawing node belong to attached tree.");
            Check(Task.Run(() => Nodes).GetAwaiter().GetResult() == 3, "Native worker census.");
            for (var i = 0; i < 32; i++) Query();
            var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1024; i++) Query();
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Native prepared census has zero owner managed allocations.");
            using var image = ((Window)Parent!).GetTexture().GetImage()!;
            Check(image.GetPixel(16, 16).R > .9f, "Counted live drawing node produces actual native pixels."); image.SavePNG(output);
            Parent!.RemoveChild(_box!); Check(Nodes == 2, "Native detachment is observable."); _box!.Dispose(); _box = null; Tree!.Quit();
        }
        protected override void Dispose(bool disposing) { _box?.Dispose(); base.Dispose(disposing); }
    }
    private sealed class Box : Entity { protected override void OnDraw() => DrawRect(new(8, 8, 24, 24), Colors.Red); }
    private sealed class Canceling : ElectronObject
    {
        internal bool Cancel = true; internal double Before;
        protected override void OnNotification(int what) { if (what == NotificationPreDelete) { Check(Objects == Before, "Pending pre-delete remains counted."); if (Cancel) CancelDisposal(); } base.OnNotification(what); }
    }
    private sealed class Throwing : ElectronObject { protected override void Dispose(bool disposing) => throw new NotSupportedException(); }
    private sealed class Failing : ElectronObject { internal Failing(out WeakReference<ElectronObject> weak) { weak = new(this); throw new NotSupportedException(); } }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
