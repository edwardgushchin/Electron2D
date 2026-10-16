using System.Reflection;
using Electron2D;
using SDL3;

internal static class WindowNotificationTests
{
    private sealed class ProbeWindow(List<string> log, string name) : Window
    {
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what is >= 1002 and <= 1013 && what is not 1010 and not 1011) { log.Add(name + ":" + what); }
        }
    }
    private sealed class ProbeBody(List<string> log, string name) : StaticBody
    {
        internal Action<int>? During { get; set; }
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what is >= 1002 and <= 1013 && what is not 1010 and not 1011) { log.Add(name + ":" + what); During?.Invoke(what); }
        }
    }
    private static Action Bind(Window window, string name) => typeof(Window).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<Action>(window);
    private static Action<bool> Focus(Window window) => typeof(Window).GetMethod("HandleFocus", BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<Action<bool>>(window);
    public static void Run()
    {
        var log = new List<string>(4096);
        using var world = new World(PhysicsServer.Backend.CPU);
        using var host = new SubViewport { Size = new(400, 300), GUIEmbedSubwindows = true, World = world };
        var window = new ProbeWindow(log, "window") { Size = new(100, 100), Borderless = true, World = world };
        var first = new ProbeBody(log, "first") { Name = "First" }; var last = new ProbeBody(log, "last") { Name = "Last" };
        var nested = new ProbeWindow(log, "nested") { World = world }; nested.AddChild(new ProbeBody(log, "nested-body"));
        window.AddChild(first); window.AddChild(nested); window.AddChild(last); host.AddChild(window);
        using var tree = new SceneTree(host);
        log.Clear(); window.FocusEntered += () => log.Add("signal-in"); window.FocusExited += () => log.Add("signal-out");
        var focus = Focus(window); focus(true);
        Check(log.SequenceEqual(["window:1004", "first:1004", "last:1004", "signal-in"]), "Focus propagates parent-first to physics descendants, stops at nested windows and precedes the signal.");
        log.Clear(); first.During = what => { if (what == 1005) throw new InvalidOperationException("observer"); };
        Reject<AggregateException>(() => focus(false));
        Check(log.SequenceEqual(["window:1005", "first:1005", "last:1005", "signal-out"]), "A failed notification preserves later nodes and the typed signal."); first.During = null;
        log.Clear(); first.During = what => { if (what == 1004) window.RemoveChild(last); };
        Reject<AggregateException>(() => focus(true));
        Check(last.Parent == window && log.Contains("last:1004"), "Propagation rejects direct-child mutation before it commits."); first.During = null;
        nested.GrabFocus(); log.Clear(); window.GrabFocus();
        Check(log.TakeLast(4).SequenceEqual(["window:1004", "first:1004", "last:1004", "signal-in"]), "Public embedded focus uses the same notification bridge.");
        log.Clear(); window.Size = new(110, 100);
        Check(log.SequenceEqual(["window:1008"]), "Window size notifies only its own node.");
        window.Position = new(20, 20); log.Clear();
        var rect = typeof(Window).GetMethod("HandleRect", BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<Action<Rect2i>>(window);
        rect(new(new(30, 20), window.Size));
        Check(log.SequenceEqual(["window:1012", "first:1012", "last:1012"]), "Observed native position commits before subtree notification.");
        log.Clear(); rect(new(window.Position, window.Size)); window.Size = window.Size;
        Check(log.Count == 0, "Equal rectangle writes are silent.");
        window.CloseRequested += () => log.Add("signal-close"); log.Clear(); window.RequestEmbeddedClose();
        Check(log.SequenceEqual(["window:1006", "first:1006", "last:1006", "signal-close"]) && !tree.QuitRequested, "Embedded close propagates without quitting the host.");
        window.MouseEntered += () => log.Add("signal-enter"); window.MouseExited += () => log.Add("signal-exit");
        log.Clear(); window.NotifyMouseEntered(); window.NotifyMouseEntered();
        Check(log.SequenceEqual(["signal-enter"]), "Viewport-only entry does not fabricate a window notification.");
        log.Clear(); window.NotifyMouseExited();
        Check(log.SequenceEqual(["signal-exit"]), "Viewport-only exit keeps its independent identity.");
        using var motion = new InputEventMouseMotion { Position = new(50, 40), GlobalPosition = new(50, 40) };
        log.Clear(); host.PushInput(motion, true); host.PushInput(motion, true);
        Check(log.SequenceEqual(["window:1002", "first:1002", "last:1002", "signal-enter"]), "Actual embedded pointer entry emits window notification before content signal, once.");
        log.Clear(); motion.Position = new(300, 200); motion.GlobalPosition = motion.Position; host.PushInput(motion, true);
        Check(log.SequenceEqual(["signal-exit", "window:1003", "first:1003", "last:1003"]), "Actual embedded exit emits content signal before containing-window notification.");
        window.Borderless = false; log.Clear(); motion.Position = new(50, 10); motion.GlobalPosition = motion.Position; host.PushInput(motion, true);
        Check(log.SequenceEqual(["window:1002", "first:1002", "last:1002"]), "Entering window decoration does not enter its viewport.");
        log.Clear(); motion.Position = new(50, 40); motion.GlobalPosition = motion.Position; host.PushInput(motion, true);
        Check(log.SequenceEqual(["signal-enter"]), "Moving from decoration into content does not repeat window entry.");
        log.Clear(); motion.Position = new(300, 200); motion.GlobalPosition = motion.Position; host.PushInput(motion, true);
        Check(log.SequenceEqual(["signal-exit", "window:1003", "first:1003", "last:1003"]), "Decorated window exit keeps viewport/window ordering.");
        first.During = what => { if (what == 1002) throw new InvalidOperationException("window hover observer"); };
        log.Clear(); motion.Position = new(50, 40); motion.GlobalPosition = motion.Position;
        Reject<AggregateException>(() => host.PushInput(motion, true));
        Check(log.Contains("last:1002") && log.Contains("signal-enter"), "Embedded window notification failure still attempts content entry.");
        first.During = null; motion.Position = new(300, 200); motion.GlobalPosition = motion.Position; host.PushInput(motion, true);
        first.During = what => { if (what == 1002) window.Hide(); };
        log.Clear(); motion.Position = new(50, 40); motion.GlobalPosition = motion.Position; host.PushInput(motion, true);
        Check(!window.Visible && !log.Contains("signal-enter") && !log.Contains("signal-exit"), "Hiding from window entry prevents a later stale content-entry signal.");
        first.During = null; window.Show(); motion.Position = new(300, 200); motion.GlobalPosition = motion.Position; host.PushInput(motion, true);
        window.DpiChanged += () => log.Add("signal-dpi"); log.Clear(); Bind(window, "HandleDPIChanged")();
        Check(log.SequenceEqual(["window:1009", "first:1009", "last:1009", "signal-dpi"]), "Native content-scale bridge retains notification ordering.");
        log.Clear(); first.During = what => { if (what == 1006) throw new InvalidOperationException("close observer"); };
        Reject<AggregateException>(window.RequestEmbeddedClose);
        Check(log.Contains("last:1006") && log.Contains("signal-close"), "Close failure continues delivery."); first.During = null;
        var addedSignal = 0; Action added = () => addedSignal++;
        first.During = what => { if (what == 1009) window.DpiChanged += added; };
        Bind(window, "HandleDPIChanged")();
        Check(addedSignal == 1, "A signal subscription from a notification participates in the ensuing event.");
        first.During = null; window.DpiChanged -= added;
        Task.Run(() => Reject<InvalidOperationException>(window.RequestEmbeddedClose)).GetAwaiter().GetResult();
        var quiet = new Window { Name = "Quiet", World = world, Borderless = true, Position = new(170, 150) }; quiet.AddChild(new StaticBody { Name = "QuietBody" }); host.AddChild(quiet);
        var quietFocus = Focus(quiet); var delivered = 0; quiet.FocusEntered += () => delivered++;
        for (var i = 0; i < 128; i++) quietFocus(true);
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 1024; i++) quietFocus(true);
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownerBytes == 0 && allBytes == 0 && delivered == 1152, "Warmed complete notification plus event path allocates zero owner/all-thread bytes.");
        Console.WriteLine($"Window notification warm gate: operations=1024, ownerBytes={ownerBytes}, allBytes={allBytes}.");
        var hover = typeof(SceneTree).GetMethod("UpdateEmbeddedWindowHover", BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<Action<Viewport, Vector2>>(tree);
        for (var i = 0; i < 128; i++) { hover(host, new(200, 170)); hover(host, new(399, 299)); }
        owner = GC.GetAllocatedBytesForCurrentThread(); all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) { hover(host, new(200, 170)); hover(host, new(399, 299)); }
        ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownerBytes == 0 && allBytes == 0, "Warmed active embedded hover, content signals and physical notifications allocate zero owner/all-thread bytes.");
        Console.WriteLine($"Embedded window pointer warm gate: operations=256, ownerBytes={ownerBytes}, allBytes={allBytes}.");
        NodeTreeDiagnosticsTests.Run();
        tree.Dispose(); host.Dispose();
        NativeBridge();
        Console.WriteLine("Window notifications: inherited physical receivers, embedded/public and native bridges, nested boundaries, errors, mutation and owner checks passed.");
    }
    private static void NativeBridge()
    {
        var log = new List<string>();
        using var world = new World(PhysicsServer.Backend.CPU);
        var window = new ProbeWindow(log, "native") { World = world, Visible = false };
        var body = new ProbeBody(log, "body"); window.AddChild(body);
        var afterFailure = false; var positionSupported = false;
        window.FocusEntered += () => throw new InvalidOperationException("native event observer");
        window.FocusEntered += () => afterFailure = true;
        window.CloseRequested += () => log.Add("signal-close");
        body.Ready += _ =>
        {
            window.Show();
            log.Clear();
            var windows = SDL.GetWindows(out var count); Check(count == 1, "Native host owns one window.");
            Check(SDL.SetWindowSize(windows![0], 320, 240) && SDL.SyncWindow(windows[0]), "Native resize commits a real changed client size before its queued notification.");
            positionSupported = SDL.GetCurrentVideoDriver() != "wayland";
            if (positionSupported) Check(SDL.SetWindowPosition(windows[0], 200, 300) && SDL.SyncWindow(windows[0]), "Position-capable host commits a real move before its queued notification.");
            foreach (var type in new[] { SDL.EventType.WindowFocusGained, SDL.EventType.WindowFocusLost, SDL.EventType.WindowMouseEnter, SDL.EventType.WindowMouseLeave, SDL.EventType.WindowDisplayScaleChanged, SDL.EventType.WindowMoved, SDL.EventType.WindowResized, SDL.EventType.WindowCloseRequested })
            {
                var input = new SDL.Event { Window = new SDL.WindowEvent { Type = type, WindowID = SDL.GetWindowID(windows![0]), Data1 = 127, Data2 = 109 } };
                Check(SDL.PushEvent(ref input), "Native queue accepts the window event.");
            }
        };
        Reject<AggregateException>(() => Engine.Run(window));
        foreach (var what in new[] { 1002, 1003, 1004, 1005, 1006, 1009 })
        {
            var owner = log.IndexOf("native:" + what); var receiver = log.IndexOf("body:" + what);
            Check(owner >= 0 && receiver == owner + 1, $"Native queued notification {what} reaches physical descendants in order: {string.Join(",", log)}.");
        }
        if (positionSupported) Check(log.IndexOf("native:1012") >= 0 && log.IndexOf("body:1012") == log.IndexOf("native:1012") + 1, "Native position notification follows an actual supported move.");
        Check(log.Contains("native:1008") && !log.Contains("body:1008"), "Native size stays on Window.");
        Check(afterFailure && log.Contains("signal-close") && window.IsDisposed && body.IsDisposed && !DisplayServer.IsAvailable && Engine.MainLoop is null, "Native callback failure retains later delegates/events and full teardown.");
        Console.WriteLine("Native queue: supported geometry, focus, pointer, scale and close notifications, handler failure and teardown passed.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
