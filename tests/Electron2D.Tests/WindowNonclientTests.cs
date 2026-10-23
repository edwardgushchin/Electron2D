using Electron2D;
using SDL3;
using System.Diagnostics;
using static TextureArrayTests;
using IOPath = System.IO.Path;

internal static class WindowNonclientTests
{
    private sealed class Receiver(List<string> log) : Node
    {
        internal bool Handle;
        internal Action? Observe;
        internal Receiver Enable() { InputEnabled = true; return this; }
        protected override void OnInput(InputEvent input) { log.Add("input"); Observe?.Invoke(); if (Handle) Tree!.SetInputAsHandled(); }
    }
    internal static bool Child()
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_NONCLIENT_CHILD") is not { } path) return false;
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep);
        using var root = packed.Instantiate(); Check(((Window)root).NonclientArea == new Rect2i(2, 3, 80, 20), "Fresh archived decoration rectangle.");
        using var host = new SubViewport { Size = new(200, 160), GUIEmbedSubwindows = true }; host.AddChild(root);
        using var tree = new SceneTree(host); var calls = 0; ((Window)root).NonclientWindowInput += _ => calls++;
        Engine.Start(tree);
        try { using var motion = new InputEventMouseMotion { Position = new(5, 5) }; Input.ParseInputEvent(motion); Input.FlushBufferedEvents(); Check(calls == 1, "Freshly loaded public headless window executes nonclient input."); }
        finally { Engine.Stop(); }
        return true;
    }
    internal static void RunManaged()
    {
        var log = new List<string>(); using var host = new SubViewport { Size = new(400, 300), GUIEmbedSubwindows = true };
        var window = new Window { Name = "Decorated", Borderless = true, GUIEmbedSubwindows = true, Position = new(20, 30), Size = new(160, 120), ContentScaleFactor = 2 };
        var receiver = new Receiver(log).Enable(); window.AddChild(receiver); host.AddChild(window); using var tree = new SceneTree(host);
        Check(window.NonclientArea == default, "Empty initial custom rectangle."); window.NonclientArea = new(2, 3, 80, 20);
        var rect = (PropertyDescriptor<Window, Rect2i>)window.GetPropertyList().Single(p => p.Name == nameof(Window.NonclientArea));
        Check(rect.IsStored && rect.GetValue(window) == window.NonclientArea, "Typed authoring descriptor stores the current rectangle.");
        var expectedPoint = new Vector2(5, 5);
        InputEvent? borrowed = null; window.NonclientWindowInput += input => { log.Add("nonclient"); borrowed = input; Check(((InputEventMouse)input).Position == expectedPoint, "Embedded custom decoration uses client coordinates before content scaling."); };
        window.WindowInput += _ => log.Add("window"); using var click = new InputEventMouseButton { Position = new(25, 35), ButtonIndex = MouseButton.Left, Pressed = true };
        receiver.Handle = true; host.PushInput(click, true); Check(!log.Contains("nonclient"), "No blocked input means no custom decoration signal; ordinary client handling remains active."); receiver.Handle = false;
        var modal = new Window { Name = "Modal", Position = new(40, 50), Size = new(40, 30), Borderless = true, Transient = true, Exclusive = true }; window.AddChild(modal); modal.Owner = window;
        modal.WindowInput += _ => log.Add("modal"); modal.AddChild(new Receiver(log).Enable());
        log.Clear(); host.PushInput(click, true); Check(log.SequenceEqual(new[] { "nonclient", "window" }), "Custom decoration is delivered before embedding reception while the modal child blocks client input.");
        Check(borrowed!.IsDisposed && !click.IsDisposed && click.Position == new Vector2(25, 35), "Localized borrowed event expires while the embedder event remains unchanged.");
        log.Clear(); window.PushInput(click, true); Check(log.Count == 0, "Direct injection emits no reception signal and cannot bypass exclusive-child client ownership.");
        using var release = new InputEventMouseButton { Position = new(350, 200), ButtonIndex = MouseButton.Left, Pressed = false }; host.PushInput(release, true);
        var observed = false; var observer = new Receiver(log).Enable(); observer.Observe = () => { observed = true; Check(!window.IsInputHandled(), "Blocked direct input clears the previous handled flag, observed during a later active input dispatch."); }; host.AddChild(observer);
        using var outside = new InputEventMouseMotion { Position = new(350, 200) }; host.PushInput(outside, true); Check(observed, "The separate embedder receives the later input."); host.RemoveChild(observer); observer.Dispose();
        window.SetBlockSignals(true); log.Clear(); host.PushInput(click, true); Check(log.Count == 0, "Signal blocking preserves exclusive input ownership."); window.SetBlockSignals(false);
        tree.Paused = true; log.Clear(); host.PushInput(click, true); Check(log.SequenceEqual(new[] { "nonclient", "window" }), "Pause does not suppress decoration reception."); tree.Paused = false;
        window.GUIDisableInput = true; log.Clear(); host.PushInput(click, true); Check(log.SequenceEqual(new[] { "nonclient", "window" }), "Reception occurs before the receiving viewport's input-disable guard."); window.GUIDisableInput = false;
        using var key = new InputEventKey { Keycode = Key.A, Pressed = true }; log.Clear(); host.PushInput(key, true); Check(!log.Contains("nonclient"), "Only mouse roles enter the custom rectangle.");
        foreach (var disabled in new[] { default(Rect2i), new Rect2i(2, 3, 0, 20), new Rect2i(2, 3, -10, 20) }) { window.NonclientArea = disabled; log.Clear(); host.PushInput(click, true); Check(!log.Contains("nonclient"), "Empty or negative-area rectangles do not capture."); }
        window.NonclientArea = new(2, 3, 80, 20);
        using var start = new InputEventMouseButton { Position = new(22, 33), ButtonIndex = MouseButton.Left, Pressed = true }; expectedPoint = new(2, 3);
        log.Clear(); host.PushInput(start, true); Check(log.Contains("nonclient"), "Top and left edges are included."); expectedPoint = new(5, 5);
        using var edge = new InputEventMouseButton { Position = new(102, 35), ButtonIndex = MouseButton.Left, Pressed = true }; log.Clear(); host.PushInput(edge, true); Check(!log.Contains("nonclient"), "Right edge is excluded.");
        using var internalClick = new InputEventMouseButton { Device = InputEvent.DeviceIdInternal, Position = new(25, 35), ButtonIndex = MouseButton.Left, Pressed = true };
        log.Clear(); host.PushInput(internalClick, true); Check(log.SequenceEqual(new[] { "nonclient" }), "Internal mouse reception reaches decoration while the ordinary WindowInput filter remains active.");
        using var touch = new InputEventScreenTouch { Position = new(25, 35), Pressed = true }; log.Clear(); host.PushInput(touch, true); Check(!log.Contains("nonclient"), "Touch is not a mouse decoration event.");
        Action<InputEvent> fail = _ => throw new ArithmeticException("decoration observer"); window.NonclientWindowInput += fail;
        log.Clear(); Reject<AggregateException>(() => host.PushInput(click, true)); Check(log.SequenceEqual(new[] { "nonclient", "window" }), "Observer failure retains ordinary embedding reception and blocks client callbacks."); window.NonclientWindowInput -= fail;
        Action<InputEvent> reenter = _ => Reject<InvalidOperationException>(() => host.PushInput(click, true)); window.NonclientWindowInput += reenter; host.PushInput(click, true); window.NonclientWindowInput -= reenter;
        Task.Run(() => Reject<InvalidOperationException>(() => window.NonclientArea = default)).GetAwaiter().GetResult();
        Task.Run(() => Reject<InvalidOperationException>(() => _ = window.NonclientArea)).GetAwaiter().GetResult();
        using var internalMouse = new InputEventMouseMotion { Device = InputEvent.DeviceIdInternal, Position = new(5, 5) };
        log.EnsureCapacity(4096); for (var i = 0; i < 128; i++) window.EmitNonclientWindowInput(internalMouse); log.Clear();
        using var miss = new InputEventMouseMotion(); var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1024; i++) { window.EmitNonclientWindowInput(internalMouse); window.EmitNonclientWindowInput(miss); }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0 && log.Count == 1024, "Prepared custom decoration hit/emission and idle misses allocate zero owner-thread managed bytes; internal mouse events remain eligible.");
        Console.WriteLine("Decoration dispatch helper: 1024 hits and 1024 misses, managed owner bytes=" + allocated);
        VerifyArchive();
        foreach (var sibling in new Window[] { new PopupPanel(), new AcceptDialog(), new ConfirmationDialog() })
            using (sibling)
            {
                sibling.NonclientArea = new(2, 3, 80, 20); using var saved = new PackedScene(); saved.Pack(sibling); using var restored = (Window)saved.Instantiate();
                Check(restored.GetType() == sibling.GetType() && restored.NonclientArea == sibling.NonclientArea, "Derived popup/dialog roles preserve inherited decoration storage.");
            }
        Action<InputEvent> close = _ => modal.Hide(); window.NonclientWindowInput += close; log.Clear(); host.PushInput(click, true);
        Check(!modal.Visible && log.SequenceEqual(new[] { "nonclient", "window", "input" }), "Embedded modal closure commits before subsequent ordinary routing."); window.NonclientWindowInput -= close; modal.Show();
        window.NonclientWindowInput += _ => window.Dispose(); host.PushInput(click, true); Check(window.IsDisposed && !host.IsDisposed && !click.IsDisposed, "Retirement from decoration reception skips stale callbacks and retains caller ownership.");
        Reject<ObjectDisposedException>(() => window.NonclientArea = default);
        Console.WriteLine("Custom decoration input: managed ownership, typed archive, ordering, guards, mutation and cleanup passed.");
    }
    private static void VerifyArchive()
    {
        using var root = new Window { NonclientArea = new(2, 3, 80, 20), GUIEmbedSubwindows = true, Borderless = true };
        var modal = new Window { Position = new(40, 50), Size = new(40, 30), Borderless = true, Transient = true, Exclusive = true }; root.AddChild(modal); modal.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root); using var copy = (Window)packed.Instantiate(); Check(copy.NonclientArea == root.NonclientArea, "In-memory scene reconstruction retains the decoration rectangle.");
        var folder = IOPath.Combine(IOPath.GetTempPath(), "e2d-nonclient-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = IOPath.Combine(folder, "scene.e2dscene"); ResourceSaver.SaveScene(root, path, SaverFlags.Compress | SaverFlags.SaveBigEndian);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(WindowNonclientTests).Assembly.Location);
            start.Environment["ELECTRON2D_TEST_WINDOW_INPUT"] = "managed"; start.Environment["ELECTRON2D_TEST_NONCLIENT_CHILD"] = path;
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(entireProcessTree: true); throw new TimeoutException("Fresh nonclient fixture timed out."); }
            Task.WaitAll(output, error); Check(child.ExitCode == 0, "Fresh archived input failed: " + output.Result + error.Result);
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        foreach (var embedded in new[] { true, false })
        {
            var root = new Window { Title = "Custom decoration", Size = new(160, 120), GUIEmbedSubwindows = embedded, NonclientArea = new(0, 0, 80, 20) };
            var modal = new Window { Size = new(40, 30), Borderless = true, Transient = true, Exclusive = true }; root.AddChild(modal);
            var decoration = new ColorRect { Size = new(80, 20), Color = Colors.Red }; root.AddChild(decoration);
            var nonclient = 0; var client = 0; uint id = 0; float density = 1; var frames = 0;
            root.NonclientWindowInput += input => { if (input is InputEventMouseMotion mouse && mouse.Position.DistanceTo(new(5, 5)) < 1) { nonclient++; decoration.Color = Colors.Lime; } };
            root.WindowInput += input => { if (input is InputEventMouseMotion mouse && mouse.Position.DistanceTo(new(5, 5)) < 1) client++; };
            root.Ready += _ =>
            {
                var handles = SDL.GetWindows(out var count); Check(count == (embedded ? 1 : 2), "Expected actual native surface count.");
                for (var i = 0; i < count; i++) if (SDL.GetWindowTitle(handles![i]) == "Custom decoration") { id = SDL.GetWindowID(handles[i]); density = SDL.GetWindowPixelDensity(handles[i]); }
                Check(id != 0, "Actual native root identity.");
                RenderingServer.FramePostDraw += Captured;
                var input = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = id, X = 5 / density, Y = 5 / density } }; Check(SDL.PushEvent(ref input), "Native queue accepted blocked-parent mouse input.");
            };
            Check(Engine.Run(root) == 0 && frames == 2 && nonclient == 1 && client == (embedded ? 1 : 0), "Actual blocked-parent native/embedded reception, pixels and cleanup.");
            Check(root.IsDisposed && modal.IsDisposed && Engine.MainLoop is null && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Native scene and services retired.");
            void Captured()
            {
                using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(70, 5); Check(pixel.G > .9f && pixel.R < .1f, "Custom decoration input changes actual framebuffer pixels.");
                if (++frames == 2) root.Tree!.Quit();
            }
        }
        var failing = new Window { Size = new(96, 80), Title = "Failing decoration", NonclientArea = new(0, 0, 80, 20) };
        var child = new Window { Size = new(40, 30), Transient = true, Exclusive = true }; failing.AddChild(child); var attempts = 0;
        failing.NonclientWindowInput += _ => { attempts++; throw new ArithmeticException("native decoration observer"); };
        failing.Ready += _ =>
        {
            var handle = SDL.GetWindows(out var count)!.Single(w => SDL.GetWindowTitle(w) == "Failing decoration"); var density = SDL.GetWindowPixelDensity(handle);
            var input = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = SDL.GetWindowID(handle), X = 5 / density, Y = 5 / density } }; Check(SDL.PushEvent(ref input), "Native failure event queued.");
        };
        Reject<AggregateException>(() => Engine.Run(failing));
        Check(attempts == 1 && failing.IsDisposed && child.IsDisposed && Engine.MainLoop is null && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Native decoration observer failure retires the real modal host and services.");
        Console.WriteLine("Custom decoration native " + backend + ": embedded/native modal ownership and actual green framebuffers passed.");
    }
}
