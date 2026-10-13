using Electron2D;
using System.Runtime.CompilerServices;

internal static class CustomMonitorTests
{
    internal static void Run()
    {
        var prefix = "test.monitors/" + Guid.NewGuid().ToString("N"); var ids = new List<string>();
        try
        {
            var before = Performance.GetMonitorModificationTime(); var calls = 0; long value = 9007199254740993;
            var count = Performance.AddCustomMonitor(prefix + "/count", () => { calls++; return value; }, Performance.MonitorType.Memory); ids.Add(count.ID);
            Check(calls == 0 && count.Type == Performance.MonitorType.Memory && count.ID == ids[0], "Registration identity/metadata without sampling.");
            Check(Performance.GetCustomMonitor(count) == value && calls == 1, "Exact Int64 precision and captured arguments."); value++;
            Check(Performance.GetCustomMonitor(count) == value, "Reads invoke current provider state.");
            var modified = Performance.GetMonitorModificationTime(); Check(modified >= before, "Monotonic modification tick.");
            Reject<InvalidOperationException>(() => Performance.AddCustomMonitor(count.ID, static () => 1)); Reject<ArgumentOutOfRangeException>(() => Performance.AddCustomMonitor(prefix, static () => 1, (Performance.MonitorType)4));
            Reject<ArgumentNullException>(() => Performance.AddCustomMonitor<int>(prefix, null!)); Reject<ArgumentNullException>(() => Performance.HasCustomMonitor(null!));
            Reject<KeyNotFoundException>(() => Performance.RemoveCustomMonitor(prefix)); Check(Performance.GetMonitorModificationTime() == modified, "Failures and reads do not change modification time.");
            var ratio = Performance.AddCustomMonitor(prefix + "/ratio", static () => -.25f, Performance.MonitorType.Percentage); ids.Add(ratio.ID); Check(Performance.GetCustomMonitor(ratio) == -.25f, "Raw typed callback value is not presentation-clamped.");
            var arguments = Performance.AddCustomMonitor(prefix + "/arguments", static ((long Value, int Extra) state) => state.Value + state.Extra, (value, 2)); ids.Add(arguments.ID); Check(Performance.GetCustomMonitor(arguments) == value + 2, "Explicit typed tuple arguments preserve exact values.");
            var state = new object(); var reference = Performance.AddCustomMonitor(prefix + "/state", () => state); ids.Add(reference.ID); Check(ReferenceEquals(Performance.GetCustomMonitor(reference), state), "Reference identity is returned unchanged.");
            var names = Performance.GetCustomMonitorNames(); var types = Performance.GetCustomMonitorTypes(); var at = Array.IndexOf(names, count.ID);
            Check(at >= 0 && names[at + 1] == ratio.ID && types[at] == Performance.MonitorType.Memory && types[at + 1] == Performance.MonitorType.Percentage, "Aligned insertion-order snapshots.");
            names[at] = "altered"; types[at] = Performance.MonitorType.Time; Check(Performance.GetCustomMonitorNames().Contains(count.ID) && Performance.GetCustomMonitorTypes()[at] == Performance.MonitorType.Memory, "Snapshot containers are independent.");
            Performance.RemoveCustomMonitor(count.ID); var replacement = Performance.AddCustomMonitor(count.ID, static () => 99L); Reject<KeyNotFoundException>(() => Performance.GetCustomMonitor(count)); Check(Performance.GetCustomMonitor(replacement) == 99, "Stale identity does not select replacement.");
            Check(Performance.GetCustomMonitorNames()[^1] == replacement.ID, "Readded ID moves to the end.");
            var fail = true; var failure = Performance.AddCustomMonitor(prefix + "/failure", () => fail ? throw new ApplicationException("fixture") : 7); ids.Add(failure.ID); Reject<ApplicationException>(() => Performance.GetCustomMonitor(failure)); fail = false; Check(Performance.GetCustomMonitor(failure) == 7, "Callback failure releases the recursion guard.");
            Performance.CustomMonitor<int>? cycle = null; cycle = Performance.AddCustomMonitor(prefix + "/cycle", () => Performance.GetCustomMonitor(cycle!)); ids.Add(cycle.ID); Reject<InvalidOperationException>(() => Performance.GetCustomMonitor(cycle));
            var nested = Performance.AddCustomMonitor(prefix + "/nested", () => Performance.GetCustomMonitor(replacement) + 1); ids.Add(nested.ID); Check(Performance.GetCustomMonitor(nested) == 100, "Nested acyclic reads.");
            for (var i = 0; i < 100; i++) Performance.GetCustomMonitor(nested); var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 10000; i++) { Check(Performance.GetCustomMonitor(nested) == 100 && Performance.HasCustomMonitor(replacement.ID), "Warm typed read."); }
            Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Zero warmed managed bytes for typed/nested reads.");
            var selfID = prefix + "/self"; var self = Performance.AddCustomMonitor(selfID, () => { Performance.RemoveCustomMonitor(selfID); return 3; }); ids.Add(selfID);
            Check(Performance.GetCustomMonitor(self) == 3 && !Performance.HasCustomMonitor(selfID), "Provider may remove itself and complete."); Reject<KeyNotFoundException>(() => Performance.GetCustomMonitor(self));
            Concurrent(prefix, ids); Lifetime(prefix, ids);
            var empty = Performance.AddCustomMonitor("", static () => 0); ids.Add(empty.ID); Check(Performance.HasCustomMonitor("") && Performance.GetCustomMonitor(empty) == 0, "Exact empty ID identity.");
        }
        finally { foreach (var id in ids) if (Performance.HasCustomMonitor(id)) Performance.RemoveCustomMonitor(id); }
        Console.WriteLine("Custom monitors: exact typed values, metadata/order, mutation ticks, stale keys, failure/reentry, concurrent removal, capture release and zero warmed allocations passed.");
    }
    private static void Concurrent(string prefix, List<string> ids)
    {
        using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        var key = Performance.AddCustomMonitor(prefix + "/race", () => { entered.Set(); if (!release.Wait(10000)) throw new TimeoutException(); return 12; }); ids.Add(key.ID);
        var read = Task.Run(() => Performance.GetCustomMonitor(key)); Check(entered.Wait(10000), "Provider entered outside registry lock.");
        try { Performance.RemoveCustomMonitor(key.ID); var replacement = Performance.AddCustomMonitor(key.ID, static () => 21); Check(Performance.GetCustomMonitor(replacement) == 21, "New generation reads during prior in-flight provider."); }
        finally { release.Set(); }
        Check(read.Wait(10000) && read.Result == 12, "Removal permits already-started read to finish."); Reject<KeyNotFoundException>(() => Performance.GetCustomMonitor(key));
    }
    private static void Lifetime(string prefix, List<string> ids)
    {
        var key = AddWeak(prefix + "/weak", out var weak); ids.Add(key.ID); Check(Performance.GetCustomMonitor(key) == 42, "Retained captured receiver."); Performance.RemoveCustomMonitor(key.ID);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); Check(!Alive(weak), "Removed monitor clears callback while stale handle is retained."); GC.KeepAlive(key);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Performance.CustomMonitor<int> AddWeak(string id, out WeakReference<Probe> weak)
    { var probe = new Probe(); weak = new(probe); return Performance.AddCustomMonitor(id, static (Probe argument) => argument.Read(), probe); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static bool Alive(WeakReference<Probe> weak) => weak.TryGetTarget(out _);
    private sealed class Probe { internal int Read() => 42; }
    internal static void RunNative()
    {
        var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); var permanent = Performance.AddCustomMonitor("test.native/permanent", static () => 17); var identity = Engine.GetSingleton("Performance");
        try
        {
            foreach (var backend in new[] { "gpu", "compatibility" })
            {
                ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); var window = new Window { Size = new(80, 64), Title = "Electron2D custom runtime monitors" }; var box = new Box { Position = new(16, 16) }; window.AddChild(box); var key = Performance.AddCustomMonitor("test.native/position", () => box.Position); var stages = 0;
                try
                {
                    box.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Actual renderer."); RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () => { var position = Performance.GetCustomMonitor(key); using var image = RenderingServer.Service!.Readback(); Check(position == new Vector2(16 + stages * 8, 16) && image.GetPixel((int)position.X, 16).R > .9f, "Monitor samples current scene data matching real pixels."); Check(Performance.GetCustomMonitor(permanent) == 17 && ReferenceEquals(Engine.GetSingleton("Performance"), identity), "Retained registry and service identity across sessions."); stages++; if (stages == 4) window.Tree!.Quit(); else box.Position = new(16 + stages * 8, 16); }; };
                    Check(Engine.Run(window) == 0 && stages == 4 && window.IsDisposed, "Native monitor host cleanup."); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "custom-monitors", backend, stages, cleaned = window.IsDisposed }));
                }
                finally { Performance.RemoveCustomMonitor(key.ID); if (!window.IsDisposed) window.Dispose(); }
            }
        }
        finally { Performance.RemoveCustomMonitor(permanent.ID); ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private sealed class Box : Entity { protected override void OnDraw() => DrawRect(new(0, 0, 6, 6), Colors.Red); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
