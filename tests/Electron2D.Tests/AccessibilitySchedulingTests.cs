using System.Diagnostics;
using Electron2D;
using Path = System.IO.Path;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using ActionKind = Electron2D.AccessibilityServer.AccessibilityAction;

internal static class AccessibilitySchedulingTests
{
    private static int _checks;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private sealed class Probe : Button
    {
        internal readonly long[] Times = new long[512];
        internal int Updates;
        internal bool Fail;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what != NotificationAccessibilityUpdate) return;
            if (Fail) throw new InvalidOperationException("Scheduled producer failure");
            if (Updates < Times.Length) Times[Updates] = Stopwatch.GetTimestamp();
            Updates++;
        }
    }
    internal static void Managed()
    {
        var folder = Path.Combine(Path.GetTempPath(), "electron2d-accessibility-schedule-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            using var registry = new ProjectSettingsRegistry(folder, Path.Combine(folder, "user"));
            Check(registry.Get(ProjectSettings.AccessibilityDriver) == "atspi" && registry.Get(ProjectSettings.AccessibilityUpdatesPerSecond) == 60, "Permanent defaults.");
            Check(!registry.IsBasic(ProjectSettings.AccessibilityDriver) && !registry.IsBasic(ProjectSettings.AccessibilityUpdatesPerSecond) && registry.IsRestartRequired(ProjectSettings.AccessibilityDriver) && registry.IsRestartRequired(ProjectSettings.AccessibilityUpdatesPerSecond), "Nonbasic restart metadata is registered.");
            Reject<ArgumentOutOfRangeException>(() => registry.Set(ProjectSettings.AccessibilityDriver, "AT-SPI"));
            Reject<ArgumentOutOfRangeException>(() => registry.Set(ProjectSettings.AccessibilityUpdatesPerSecond, 0));
            Reject<ArgumentOutOfRangeException>(() => registry.Set(ProjectSettings.AccessibilityUpdatesPerSecond, -1));
            Reject<ArgumentNullException>(() => registry.Set(ProjectSettings.AccessibilityDriver, null!));
            Reject<InvalidOperationException>(() => registry.Unregister(ProjectSettings.AccessibilityDriver));
            Reject<InvalidOperationException>(() => registry.Unregister(ProjectSettings.AccessibilityUpdatesPerSecond));
            registry.Set(ProjectSettings.AccessibilityDriver, "dummy"); registry.Set(ProjectSettings.AccessibilityUpdatesPerSecond, int.MaxValue); registry.Save();
            using var loaded = new ProjectSettingsRegistry(folder, Path.Combine(folder, "user")); loaded.Load();
            Check(loaded.Get(ProjectSettings.AccessibilityDriver) == "dummy" && loaded.Get(ProjectSettings.AccessibilityUpdatesPerSecond) == int.MaxValue, "Saved settings retain names and full positive range.");
            loaded.SetFeatureOverride(ProjectSettings.AccessibilityDriver, "schedule-probe", "atspi");
            loaded.SetFeatureOverride(ProjectSettings.AccessibilityUpdatesPerSecond, "schedule-probe", 1); loaded.AddCustomFeature("schedule-probe");
            Check(loaded.GetWithOverride(ProjectSettings.AccessibilityDriver) == "atspi" && loaded.GetWithOverride(ProjectSettings.AccessibilityUpdatesPerSecond) == 1, "Feature overrides resolve both settings.");
            using var root = new Node(); using var tree = new SceneTree(root);
            tree.ProcessFrame(0); tree.ProcessFrame(0);
            Check(!root.GetAccessibilityElement().IsValid(), "No simulated scene bridge in headless processing.");
        }
        finally { Directory.Delete(folder, true); }
        Console.WriteLine($"PASS {_checks} managed accessibility scheduling contracts.");
    }
    internal static void Native(string backend)
    {
        Managed();
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D Scheduling Probe");
        ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always);
        ProjectSettings.Set(ProjectSettings.AccessibilityDriver, "dummy"); ProjectSettings.Set(ProjectSettings.AccessibilityUpdatesPerSecond, 1);
        ProjectSettings.SetFeatureOverride(ProjectSettings.AccessibilityDriver, "schedule-native", "atspi"); ProjectSettings.SetFeatureOverride(ProjectSettings.AccessibilityUpdatesPerSecond, "schedule-native", 10); ProjectSettings.AddCustomFeature("schedule-native");
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var window = new Window { Size = new(360, 180) };
        var probe = new Probe { Text = "Activate", AccessibilityName = "Scheduled action", Size = new(160, 40), Position = new(8, 8) };
        var label = new Label { Text = "Initial", Size = new(340, 36), Position = new(8, 64) };
        var victim = new Button { AccessibilityName = "Retire immediately", Position = new(180, 8), Size = new(160, 40) };
        window.AddChild(probe); window.AddChild(label); window.AddChild(victim);
        var clicks = 0; probe.Pressed += () => { clicks++; label.Text = "Clicked"; };
        RID controller = default, retired = default, initial = default;
        Process? client = null; Task<string>? output = null, error = null;
        var previousFPS = Engine.MaxFPS; Engine.MaxFPS = 120;
        var idleUpdates = 0; var activeUpdates = 0; var scheduleStart = 0;
        var commands = 0; var complete = false; var clientDone = false; var post = 0; var measuring = false; long before = 0, idle = 0, active = 0;
        void Command(int command)
        {
            commands++;
            switch (command)
            {
                case 0: for (var i = 0; i < 100; i++) probe.QueueAccessibilityUpdate(); label.Text = "Last burst value"; break;
                case 1: ProjectSettings.SetFeatureOverride(ProjectSettings.AccessibilityUpdatesPerSecond, "schedule-native", int.MaxValue); ProjectSettings.SetFeatureOverride(ProjectSettings.AccessibilityDriver, "schedule-native", "dummy"); Check(AccessibilityServer.IsSupported(), "Live display keeps its selected driver."); break;
                case 2: probe.Disabled = true; probe.Hide(); break;
                case 3: probe.Disabled = false; probe.Show(); break;
                case 4: retired = victim.GetAccessibilityElement(); window.RemoveChild(victim); victim.Dispose(); Check(!AccessibilityServer.HasElement(retired), "Retirement does not wait for the next scheduled pass."); break;
                case 5: window.AddChild(new Label { Text = "Late source", Size = new(300, 24), Position = new(8, 108) }); break;
                case 6: window.Tree!.Paused = true; label.Text = "Paused scene"; break;
                case 99: complete = true; window.Tree!.Paused = false; label.Text = "Measuring"; break;
                default: throw new InvalidOperationException("Unknown scheduling command");
            }
            AccessibilityServer.UpdateSetName(controller, "command-" + command);
        }
        using var texture = window.GetTexture();
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend && AccessibilityServer.IsSupported(), "Actual renderer and native driver.");
            initial = probe.GetAccessibilityElement();
            controller = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Button); AccessibilityServer.UpdateSetName(controller, "Scheduling controller");
            AccessibilityServer.UpdateAddAction(controller, ActionKind.Custom, (int id) => Command(id));
            foreach (var id in new[] { 0, 1, 2, 3, 4, 5, 6, 99 }) AccessibilityServer.UpdateAddCustomAction(controller, id, "schedule-" + id);
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/scheduling.py")); client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync();
            window.Tree!.ProcessFrameStarted += _ =>
            {
                measuring = complete && post >= 16 && (post < 80 || post >= 96 && post < 160); if (measuring) before = GC.GetAllocatedBytesForCurrentThread();
                probe.QueueAccessibilityUpdate();
                if (complete && post >= 80) probe.AccessibilityName = (post & 1) == 0 ? "Even" : "Odd";
            };
            RenderingServer.FramePostDraw += AfterDraw;
        };
        void AfterDraw()
        {
            if (measuring) { var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (post < 80) idle += bytes; else active += bytes; }
            if (!complete) return;
            if (post == 16) { idleUpdates = probe.Updates; scheduleStart = probe.Updates; }
            if (post == 79) Check(probe.Updates > idleUpdates, "Measured idle frames include actual scheduled passes.");
            if (post == 96) activeUpdates = probe.Updates;
            if (++post == 8 && Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder)
            { Directory.CreateDirectory(folder); using var image = texture.GetImage()!; image.SavePNG(Path.Combine(folder, "scheduling-" + backend + ".png")); }
            if (post != 160) return;
            Check(idle == 0 && active == 0, $"Scheduled idle/active owner allocations {idle}/{active}.");
            Check(probe.Updates > 5 && probe.Updates < probe.Times.Length, "Real repeated semantic passes execute.");
            Check(probe.Updates > activeUpdates, "Measured active frames include actual scheduled passes.");
            for (var i = scheduleStart + 1; i < probe.Updates; i++) Check(Stopwatch.GetElapsedTime(probe.Times[i - 1], probe.Times[i]).TotalSeconds >= .08, "Scene creation freezes the ten-Hz schedule; no frame-rate or catch-up burst.");
        }
        var started = Stopwatch.GetTimestamp();
        try
        {
            Check(Engine.Run((_, _) =>
            {
                if (!clientDone && client?.HasExited == true) { Console.Write(output!.GetAwaiter().GetResult()); Console.Write(error!.GetAwaiter().GetResult()); Check(client.ExitCode == 0 && complete && commands == 8 && clicks == 2, "Independent native client exercises coalescing and actual actions."); clientDone = true; }
                if (clientDone && post >= 160) window.Tree!.Quit();
                if (Stopwatch.GetElapsedTime(started).TotalSeconds > 40) throw new TimeoutException("Scheduling native client timeout");
            }, window) == 0, "Scheduled display completes.");
        }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; if (client is { HasExited: false }) client.Kill(true); client?.Dispose(); Engine.MaxFPS = previousFPS; }
        Check(!AccessibilityServer.HasElement(controller) && !AccessibilityServer.HasElement(initial), "Display closes all scheduled identities.");
        Disabled(); Failure();
        Console.WriteLine($"PASS {_checks} native accessibility driver/scheduling contracts on {backend}; 64 idle/64 active owner bytes {idle}/{active}.");
    }
    private static void Disabled()
    {
        var disabled = new Window { Size = new(120, 80) };
        disabled.Ready += _ =>
        {
            Check(!AccessibilityServer.IsSupported() && !disabled.GetAccessibilityElement().IsValid(), "Dummy driver disables automatic and explicit output even with Always support.");
            Reject<NotSupportedException>(() => AccessibilityServer.GetWindowRoot(disabled.GetWindowID()));
            ProjectSettings.SetFeatureOverride(ProjectSettings.AccessibilityDriver, "schedule-native", "atspi"); Check(!AccessibilityServer.IsSupported(), "Driver changes wait for a fresh display."); disabled.Tree!.Quit();
        };
        Check(Engine.Run(disabled) == 0, "Disabled display closes.");
    }
    private static void Failure()
    {
        ProjectSettings.SetFeatureOverride(ProjectSettings.AccessibilityUpdatesPerSecond, "schedule-native", 1);
        var failed = false; var window = new Window { Size = new(120, 80) }; window.AddChild(new Probe { Fail = true });
        try { Engine.Run(window); } catch (AggregateException errors) { failed = errors.Flatten().InnerExceptions.Any(e => e.Message == "Scheduled producer failure"); }
        Check(failed && !AccessibilityServer.IsSupported(), "Scheduled producer failures aggregate and retire the display.");
        ProjectSettings.SetFeatureOverride(ProjectSettings.AccessibilityUpdatesPerSecond, "schedule-native", int.MaxValue);
        var fresh = new Window { Size = new(120, 80) }; var probe = new Probe(); fresh.AddChild(probe); var frames = 0;
        Check(Engine.Run((_, _) => { if (++frames == 4) { Check(AccessibilityServer.IsSupported() && probe.Updates >= 2, "Fresh display starts a new high-frequency schedule after failure."); fresh.Tree!.Quit(); } probe.QueueAccessibilityUpdate(); }, fresh) == 0, "Fresh display completes.");
    }
}
