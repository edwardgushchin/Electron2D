using Electron2D;
using System.Runtime.InteropServices;
using SDL = SDL3.SDL;

internal static class DisplayWindowEventsTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static uint NativeID(DisplayServer context)
    { var window = context.AcquireRenderingWindow(); try { return SDL.GetWindowID(window.DangerousGetHandle()); } finally { context.ReleaseRenderingWindow(); } }
    private static void Push(SDL.Event value) => Check(SDL.PushEvent(ref value), "Queue a native event.");
    private static void WindowEvent(uint window, SDL.EventType kind, int x = 0, int y = 0) => Push(new() { Window = new() { Type = kind, WindowID = window, Data1 = x, Data2 = y } });
    private static void Text(uint window, string text, bool editing = false)
    {
        var memory = Marshal.StringToCoTaskMemUTF8(text);
        try
        {
            if (editing) Push(new() { Edit = new() { Type = SDL.EventType.TextEditing, WindowID = window, Text = memory, Start = 1, Length = 1 } });
            else Push(new() { Text = new() { Type = SDL.EventType.TextInput, WindowID = window, Text = memory } });
            DisplayServer.ProcessEvents();
        }
        finally { Marshal.FreeCoTaskMem(memory); }
    }
    private sealed class Receiver(List<string> order) : Node
    {
        internal bool CountOnly;
        internal int Calls;
        internal Receiver Enable() { InputEnabled = true; return this; }
        protected override void OnInput(InputEvent value)
        { Calls++; if (!CountOnly) order.Add("scene"); }
    }

    internal static void Run()
    {
        Reject<InvalidOperationException>(() => DisplayServer.GetWindowEvents());
        using (var display = DisplayServer.Open("Scoped events", new(96, 80), hidden: true))
        using (var first = display.CreateChildWindow("First", new(96, 80)))
        using (var second = display.CreateChildWindow("Second", new(96, 80)))
        {
            var main = DisplayServer.GetWindowEvents(); var a = DisplayServer.GetWindowEvents(first.WindowID); var b = DisplayServer.GetWindowEvents(second.WindowID);
            Check(ReferenceEquals(main, DisplayServer.GetWindowEvents(0)) && ReferenceEquals(a, DisplayServer.GetWindowEvents(first.WindowID)) && a.WindowID == first.WindowID, "One stable live publisher belongs to each native identity.");
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.GetWindowEvents(-1));
            Task.Run(() => Reject<InvalidOperationException>(() => DisplayServer.GetWindowEvents())).GetAwaiter().GetResult();
            Task.Run(() => Reject<InvalidOperationException>(() => a.CloseRequested += static () => { })).GetAwaiter().GetResult();
            Task.Run(() => Reject<InvalidOperationException>(() => a.Dispose())).GetAwaiter().GetResult();
            Check(!a.IsDisposed, "Wrong-thread disposal rejects before retiring a publisher.");
            DisplayServer.ProcessEvents();
            VerifyInput(main, a, b, NativeID(first));
            b = DisplayServer.GetWindowEvents(second.WindowID);
            VerifyNotifications(display, first, second, main, a, b);
            main = DisplayServer.GetWindowEvents();
            var retired = a.WindowID; first.Dispose();
            Check(a.IsDisposed && a.WindowID == retired && !b.IsDisposed && !main.IsDisposed, "Native retirement disposes only its own publisher and preserves diagnostic identity.");
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.GetWindowEvents(retired));
            Reject<ObjectDisposedException>(() => a.CloseRequested += static () => { });
            a.CloseRequested -= static () => { };
            using var next = display.CreateChildWindow("Next", new(96, 80));
            Check(next.WindowID > second.WindowID && !ReferenceEquals(a, DisplayServer.GetWindowEvents(next.WindowID)), "Reopened native windows get fresh IDs and publishers.");
        }
        VerifyRetirementFailures();
        using (var reopened = DisplayServer.Open("New session", new(48, 48), hidden: true))
            Check(DisplayServer.GetWindowEvents().WindowID == 0, "A later display session publishes a fresh main endpoint.");
        Console.WriteLine("Per-window events: identity, all typed channels, parser ordering/broadcast, signals, failure continuation, replacement, owner, retirement and prepared zero-allocation dispatch passed.");
    }

    private static void VerifyInput(DisplayWindowEvents main, DisplayWindowEvents a, DisplayWindowEvents b, uint nativeID)
    {
        var order = new List<string>(); var receiver = new Receiver(order).Enable(); using var tree = new SceneTree(receiver);
        using var key = new InputEventKey { Keycode = Key.A, PhysicalKeycode = Key.A, Pressed = true, WindowID = a.WindowID };
        using var global = new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true };
        var oldMouse = Input.EmulateMouseFromTouch; var oldTouch = Input.EmulateTouchFromMouse;
        Input.EmulateMouseFromTouch = false; Input.EmulateTouchFromMouse = false;
        Action<InputEvent> m = _ => order.Add("main"); Action<InputEvent> one = _ => { Check(Input.IsPhysicalKeyPressed(Key.A), "Raw state commits before window input."); order.Add("first"); }; Action<InputEvent> two = _ => order.Add("second");
        main.Input += m; a.Input += one; b.Input += two; Engine.Start(tree);
        try
        {
            Input.ParseInputEvent(key); Check(order.SequenceEqual(new[] { "first", "scene" }), "A tagged event selects one publisher before ordinary scene routing.");
            order.Clear();
            Push(new() { Key = new() { Type = SDL.EventType.KeyDown, WindowID = nativeID, Key = SDL.Keycode.A, Scancode = SDL.Scancode.A, Down = true } });
            DisplayServer.ProcessEvents();
            Check(order.SequenceEqual(new[] { "first", "scene" }), "The native queue selects the same publisher before scene input.");
            using (var touch = new InputEventScreenTouch { Index = 0, Pressed = true, WindowID = a.WindowID })
            {
                Input.EmulateMouseFromTouch = true; order.Clear(); Input.ParseInputEvent(touch);
                Check(order.SequenceEqual(new[] { "first", "scene", "first", "scene" }), "Emulated mouse and source touch retain their window and independent delivery phases.");
                touch.Pressed = false; Input.ParseInputEvent(touch); Input.EmulateMouseFromTouch = false;
            }
            order.Clear(); Input.ParseInputEvent(global); Check(order.SequenceEqual(new[] { "main", "first", "second", "scene" }), "An untagged event broadcasts in stable native-window order then routes to the scene once.");
            order.Clear(); a.SetBlockSignals(true); Input.ParseInputEvent(key); a.SetBlockSignals(false);
            Check(order.SequenceEqual(new[] { "scene" }), "Publisher signal blocking preserves direct scene input.");
            a.Input -= one; Action<InputEvent> throwing = _ => throw new InvalidOperationException("window input"); a.Input += throwing;
            order.Clear(); var before = receiver.Calls; Reject<AggregateException>(() => Input.ParseInputEvent(global));
            Check(order.SequenceEqual(new[] { "main", "second", "scene" }) && receiver.Calls == before + 1, "A failed publisher preserves other windows and the ordinary scene phase.");
            a.Input -= throwing; a.Input += one;
            b.Input -= two; a.Input -= one;
            Action<InputEvent> late = _ => b.Input += two; a.Input += late;
            order.Clear(); Input.ParseInputEvent(global); Check(!order.Contains("second"), "New publisher recipients wait for the next parsed event.");
            a.Input -= late; a.Input += one;
            using var forbidden = new InputEventKey { Keycode = Key.Z, PhysicalKeycode = Key.Z, Pressed = true };
            Task.Run(() => Reject<InvalidOperationException>(() => Input.ParseInputEvent(forbidden))).GetAwaiter().GetResult();
            Check(!Input.IsPhysicalKeyPressed(Key.Z), "Wrong-thread parsing rejects before input state mutation.");
            main.Input -= m; a.Input -= one; b.Input -= two;
            receiver.CountOnly = true; var calls = 0; Action<InputEvent> counter = _ => calls++; a.Input += counter;
            for (var i = 0; i < 64; i++) { key.Pressed = (i & 1) != 0; Input.ParseInputEvent(key); }
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) { key.Pressed = (i & 1) != 0; Input.ParseInputEvent(key); }
            var ownerDelta = GC.GetAllocatedBytesForCurrentThread() - owner; var allDelta = GC.GetTotalAllocatedBytes(true) - all;
            Check(calls == 128 && ownerDelta == 0 && allDelta == 0, $"Prepared window input dispatch: owner={ownerDelta}, all={allDelta} bytes.");
            a.Input -= counter;
            var staleCalls = 0; Action<InputEvent> stale = _ => staleCalls++; b.Input += stale;
            Action<InputEvent> retire = _ => b.Dispose(); a.Input += retire;
            Input.ParseInputEvent(global); a.Input -= retire;
            Check(b.IsDisposed && staleCalls == 0, "Retired publishers in a captured broadcast are skipped without closing their native window.");
            var previousSceneCalls = receiver.Calls; Action<InputEvent> stop = _ => Engine.Stop(); a.Input += stop;
            Input.ParseInputEvent(key); a.Input -= stop;
            Check(Engine.MainLoop is null && receiver.Calls == previousSceneCalls, "A window callback that stops its main loop never dispatches into the retired scene.");
        }
        finally { main.Input -= m; a.Input -= one; b.Input -= two; if (Engine.MainLoop is not null) Engine.Stop(); Input.ReleasePressedEvents(); Input.EmulateMouseFromTouch = oldMouse; Input.EmulateTouchFromMouse = oldTouch; }
    }

    private static void VerifyNotifications(DisplayServer display, DisplayServer first, DisplayServer second, DisplayWindowEvents main, DisplayWindowEvents a, DisplayWindowEvents b)
    {
        var id = NativeID(first); var other = NativeID(second); var rootID = NativeID(display);
        var close = 0; var focus = 0; var enter = 0; var exit = 0; var dpi = 0; var rects = new List<Rect2i>(); var commits = new List<string>(); var edits = new List<string>();
        a.CloseRequested += () => close++; a.FocusChanged += _ => focus++; a.MouseEntered += () => enter++; a.MouseExited += () => exit++; a.DPIChanged += () => dpi++;
        a.RectChanged += r => rects.Add(r); a.TextInput += s => commits.Add(s); a.TextEditing += (s, selection) => { Check(selection == new Vector2i(1, 1), "Composition uses codepoint selection."); edits.Add(s); };
        var otherCommits = 0; b.TextInput += _ => otherCommits++;
        var back = 0; main.GoBackRequested += () => back++;
        Check(DisplayServer.TryQueueSystemGoBack(), "Queue a native-host style system-back request."); DisplayServer.ProcessEvents();
        Check(back == 1, "System back reaches the requested main publisher.");
        WindowEvent(id, SDL.EventType.WindowMouseEnter); WindowEvent(id, SDL.EventType.WindowMouseLeave); WindowEvent(id, SDL.EventType.WindowDisplayScaleChanged);
        WindowEvent(id, SDL.EventType.WindowFocusGained); WindowEvent(id, SDL.EventType.WindowFocusLost); WindowEvent(id, SDL.EventType.WindowCloseRequested);
        WindowEvent(id, SDL.EventType.WindowPixelSizeChanged, 111, 92); WindowEvent(id, SDL.EventType.WindowResized, 111, 92); DisplayServer.ProcessEvents();
        Text(id, "a🙂", editing: true); Text(id, "é👩‍🚀"); Text(other, "別");
        Check(close == 1 && focus == 2 && enter == 1 && exit == 1 && dpi == 1 && rects.Any(r => r.Size == new Vector2i(111, 92)), "All supported lifecycle/pointer/scale/rectangle channels route independently.");
        Check(commits.SequenceEqual(new[] { "é👩‍🚀" }) && edits.SequenceEqual(new[] { "a🙂" }) && otherCommits == 1, "Text and composition remain whole and window-specific.");
        IReadOnlyList<string>? drop = null; a.FilesDropped += paths => drop = paths;
        var left = Marshal.StringToCoTaskMemUTF8("/tmp/first é"); var right = Marshal.StringToCoTaskMemUTF8("/tmp/second 漢");
        try
        {
            Push(new() { Drop = new() { Type = SDL.EventType.DropBegin, WindowID = id } });
            Push(new() { Drop = new() { Type = SDL.EventType.DropFile, WindowID = id, Data = left } });
            Push(new() { Drop = new() { Type = SDL.EventType.DropFile, WindowID = id, Data = right } });
            Push(new() { Drop = new() { Type = SDL.EventType.DropComplete, WindowID = id } }); DisplayServer.ProcessEvents();
        }
        finally { Marshal.FreeCoTaskMem(left); Marshal.FreeCoTaskMem(right); }
        Check(drop is not null && drop.SequenceEqual(new[] { "/tmp/first é", "/tmp/second 漢" }), "Completed drop paths survive native memory retirement in their original order.");
        Action<string> broken = _ => throw new InvalidOperationException("legacy text"); DisplayServer.TextInput += broken;
        var scoped = 0; main.TextInput += _ => scoped++;
        try { Reject<AggregateException>(() => Text(rootID, "failure")); Check(scoped == 1, "A legacy callback failure does not suppress the scoped channel."); }
        finally { DisplayServer.TextInput -= broken; }
        DisplayWindowEvents? replacement = null; var oldCalls = 0; var newCalls = 0; main.TextInput += _ => oldCalls++;
        Action<string> replace = _ => { main.Dispose(); replacement = DisplayServer.GetWindowEvents(); replacement.TextInput += _ => newCalls++; };
        DisplayServer.TextInput += replace;
        try { Text(rootID, "replace"); } finally { DisplayServer.TextInput -= replace; }
        Check(main.IsDisposed && replacement is not null && oldCalls == 0 && newCalls == 0, "Replacing a publisher during legacy delivery invokes neither retired nor newly created scoped receivers.");
        Text(rootID, "later"); Check(newCalls == 1, "Replacement starts with the next native event.");
    }

    private static void VerifyRetirementFailures()
    {
        var display = DisplayServer.Open("Retirement", new(48, 48), hidden: true);
        var first = display.CreateChildWindow("Retirement A", new(48, 48)); var second = display.CreateChildWindow("Retirement B", new(48, 48));
        var main = DisplayServer.GetWindowEvents(); var a = DisplayServer.GetWindowEvents(first.WindowID); var b = DisplayServer.GetWindowEvents(second.WindowID);
        main.Disposed += _ => throw new InvalidOperationException("main endpoint"); a.Disposed += _ => throw new InvalidOperationException("child endpoint");
        Reject<AggregateException>(() => display.Dispose());
        Check(!DisplayServer.IsAvailable && display.IsDisposed && first.IsDisposed && second.IsDisposed && main.IsDisposed && a.IsDisposed && b.IsDisposed, "Publisher retirement failures preserve complete native child/root teardown.");
    }
}
