using Electron2D;
using SDL3;

internal static class WindowInputTests
{
    private sealed class ProbeWindow(List<string> log) : Window
    {
        internal Action? DuringVisibility;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationVisibilityChanged) { log.Add("notification:" + Visible); DuringVisibility?.Invoke(); }
        }
    }
    private sealed class Receiver(List<string> log) : Node
    {
        internal Vector2 Position;
        internal bool Handle;
        internal Receiver Enable() { InputEnabled = true; return this; }
        protected override void OnInput(InputEvent input)
        {
            log.Add("input");
            if (input is InputEventMouse mouse) Position = mouse.Position;
            if (Handle) Tree!.SetInputAsHandled();
        }
    }
    internal static void RunManaged()
    {
        var log = new List<string>();
        using (var detached = new ProbeWindow(log))
        {
            detached.VisibilityChanged += () => log.Add("signal:" + detached.Visible);
            detached.Hide(); detached.Hide(); detached.Show();
            Check(log.SequenceEqual(["notification:False", "signal:False", "notification:True", "signal:True"]), "Visibility commits before notification 30, followed by exactly one signal.");
            detached.SetBlockSignals(true); log.Clear(); detached.Hide();
            Check(log.SequenceEqual(["notification:False"]), "Signal blocking preserves numeric visibility delivery.");
            detached.SetBlockSignals(false); detached.DuringVisibility = () => throw new InvalidOperationException("visibility observer"); log.Clear();
            Reject<AggregateException>(detached.Show); Check(detached.Visible && log.SequenceEqual(["notification:True", "signal:True"]), "Visibility errors preserve commitment and later signal delivery.");
        }
        var retiring = new ProbeWindow(log); retiring.DuringVisibility = retiring.Dispose; retiring.Hide(); Check(retiring.IsDisposed, "Disposal in visibility notification suppresses stale event and canvas delivery.");
        log.Clear();
        using var host = new SubViewport { Size = new(400, 300), GUIEmbedSubwindows = true };
        var window = new Window { Name = "Target", Position = new(40, 30), Size = new(160, 120), Borderless = true, ContentScaleFactor = 2 };
        var receiver = new Receiver(log).Enable(); window.AddChild(receiver); host.AddChild(window);
        using var tree = new SceneTree(host);
        InputEvent? borrowed = null; var signalPosition = Vector2.Zero;
        Action<InputEvent> observer = input => { log.Add("signal"); borrowed = input; if (input is InputEventMouse mouse) signalPosition = mouse.Position; };
        window.WindowInput += observer;
        using var motion = new InputEventMouseMotion { Position = new(80, 70), GlobalPosition = new(80, 70) };
        host.PushInput(motion, true);
        Check(log.SequenceEqual(["signal", "input"]) && signalPosition == new Vector2(40, 40) && receiver.Position == new Vector2(20, 20), "Embedded signal precedes input and final viewport scaling.");
        Check(borrowed!.IsDisposed && !motion.IsDisposed && motion.Position == new Vector2(80, 70), "Localized signal event expires after dispatch, caller event remains unchanged.");
        log.Clear(); window.PushInput(motion, true); Check(log.SequenceEqual(["input"]), "Direct viewport injection bypasses its own window signal.");
        receiver.Handle = true; log.Clear(); host.PushInput(motion, true); Check(log.SequenceEqual(["signal", "input"]), "A handled scene event has already emitted its window signal."); receiver.Handle = false;
        window.SetBlockSignals(true); log.Clear(); host.PushInput(motion, true); Check(log.SequenceEqual(["input"]), "Blocking a window signal leaves ordinary input active."); window.SetBlockSignals(false);
        tree.Paused = true; log.Clear(); host.PushInput(motion, true); Check(log.SequenceEqual(["signal"]), "Window signal remains active while scene processing is paused."); tree.Paused = false;
        window.GUIDisableInput = true; log.Clear(); host.PushInput(motion, true); Check(log.SequenceEqual(["signal"]), "Window reception precedes viewport input disabling."); window.GUIDisableInput = false;
        host.GUIDisableInput = true; log.Clear(); host.PushInput(motion, true); Check(log.Count == 0, "Disabled embedder does not forward input to child windows."); host.GUIDisableInput = false;
        using var internalMotion = new InputEventMouseMotion { Device = -2, Position = motion.Position, GlobalPosition = motion.GlobalPosition };
        log.Clear(); host.PushInput(internalMotion, true); Check(log.Count == 0, "Internal pointer refresh is absent from window and scene user callbacks.");
        Action<InputEvent> fail = _ => throw new InvalidOperationException("window observer"); window.WindowInput += fail;
        log.Clear(); Reject<AggregateException>(() => host.PushInput(motion, true)); Check(log.SequenceEqual(["signal", "input"]), "Signal failure still dispatches the scene event."); window.WindowInput -= fail;
        Action<InputEvent> mutate = input => { if (input is InputEventMouse mouse) mouse.Position = new(60, 40); };
        window.WindowInput += mutate; host.PushInput(motion, true); Check(receiver.Position == new Vector2(30, 20), "Signal event edits reach the subsequent viewport transform."); window.WindowInput -= mutate;
        Action<InputEvent> reenter = _ => Reject<InvalidOperationException>(() => host.PushInput(motion, true)); window.WindowInput += reenter; host.PushInput(motion, true); window.WindowInput -= reenter;
        Task.Run(() => Reject<InvalidOperationException>(() => host.PushInput(motion, true))).GetAwaiter().GetResult();
        using var key = new InputEventKey { Keycode = Key.Space, Pressed = true };
        window.WindowInput += input => { if (input is InputEventKey) Check(ReferenceEquals(input, key), "Nonpositional event stays borrowed."); };
        host.PushInput(key, true); Check(!key.IsDisposed, "Borrowed key survives dispatch.");
        var inner = new Window { Name = "Inner", Position = new(5, 5), Size = new(40, 30), Borderless = true }; window.GUIEmbedSubwindows = true; window.AddChild(inner);
        var innerCalls = 0; inner.WindowInput += input => { innerCalls++; Check(((InputEventMouse)input).Position == new Vector2(15, 15), "Nested receiver gets coordinates in its window."); };
        host.PushInput(motion, true); Check(innerCalls == 1, "Every actual embedded window receiver emits before forwarding.");
        window.RemoveChild(inner); inner.Dispose();
        var hoverExits = 0; var button = new Button { Size = new(50, 50) }; button.MouseExited += () => hoverExits++; window.AddChild(button); host.PushInput(motion, true);
        window.WindowInput += _ => window.Dispose(); host.PushInput(motion, true);
        Check(button.IsDisposed && hoverExits == 1, "Retirement also releases an actual hovered GUI child exactly once.");
        Check(window.IsDisposed && !host.IsDisposed && !motion.IsDisposed, "Retirement during signal delivery skips stale viewport dispatch and releases only owned copies.");
        var panel = new SubViewport { Size = new(160, 120), GUIEmbedSubwindows = true }; var first = new Window { Borderless = true }; var last = new Window { Borderless = true }; panel.AddChild(first); panel.AddChild(last); host.AddChild(panel); last.GrabFocus(); host.RemoveChild(panel); panel.Dispose(); Check(first.IsDisposed && last.IsDisposed, "Departing viewport clears focus without raising another departing child.");
        VerifyPopupInput(host, tree);
        Console.WriteLine("Window reception, visibility notification, ordering, coordinates, mutation, pause, guards and cleanup checks passed.");
    }
    private static void VerifyPopupInput(Viewport host, SceneTree tree)
    {
        using var popup = new PopupPanel { Size = new(100, 80) }; host.AddChild(popup);
        var calls = 0; popup.WindowInput += _ => calls++;
        using var cancel = new InputEventAction { Action = "ui_cancel", Pressed = true };
        popup.Popup(); popup.GUIDisableInput = true; popup.PushInput(cancel, true); tree.FlushDeferred();
        Check(popup.Visible && calls == 0, "Disabled GUI suppresses directly injected popup actions without emitting a window signal.");
        popup.GUIDisableInput = false; popup.PushInput(cancel, true); tree.FlushDeferred();
        Check(!popup.Visible && calls == 0 && !cancel.IsDisposed, "Direct popup input executes cancellation without its own window signal or caller-event disposal.");
        popup.Popup(); host.PushInput(cancel, true); tree.FlushDeferred();
        Check(!popup.Visible && calls == 1, "Forwarded popup input executes cancellation and emits exactly one reception signal.");
        popup.GUIEmbedSubwindows = true; popup.Popup();
        using var child = new PopupPanel { Size = new(40, 30) }; popup.AddChild(child); child.Popup();
        var childCalls = 0; child.WindowInput += _ => childCalls++;
        host.PushInput(cancel, true); tree.FlushDeferred();
        Check(popup.Visible && !child.Visible && calls == 2 && childCalls == 1, "Only the final nested popup consumes cancellation; ancestor reception does not close its popup.");
        Action<InputEvent> change = input => ((InputEventAction)input).Action = "ignored_popup_action";
        popup.WindowInput += change; host.PushInput(cancel, true); tree.FlushDeferred(); popup.WindowInput -= change;
        Check(popup.Visible && cancel.Action == "ignored_popup_action", "Reception edits precede the selected popup's ordinary input policy.");
        cancel.Action = "ui_cancel";
    }
    private sealed class NativeReceiver(List<string> log) : Node
    {
        internal NativeReceiver Enable() { InputEnabled = true; return this; }
        protected override void OnInput(InputEvent input) { if (input is InputEventKey { Keycode: Key.A, Pressed: true }) log.Add("input"); }
    }
    internal static void RunNative(bool fail)
    {
        var log = new List<string>(); var visibility = new List<string>(); var frames = 0;
        var root = new Window { Size = new(320, 240), GUIEmbedSubwindows = true, Title = "Window input checks" };
        var child = new ProbeWindow(visibility) { Size = new(80, 60), Position = new(40, 30), Borderless = true };
        child.AddChild(new NativeReceiver(log).Enable()); child.AddChild(new Polygon { Vertices = [new(0, 0), new(40, 0), new(40, 40), new(0, 40)], Color = Colors.Lime }); root.AddChild(child);
        root.WindowInput += input =>
        {
            if (input is not InputEventKey { Keycode: Key.A, Pressed: true }) return;
            Check(Input.IsKeyPressed(Key.A), "Native input state commits before window reception."); log.Add("root");
            if (fail) throw new InvalidOperationException("native window observer");
        };
        child.WindowInput += input => { if (input is InputEventKey { Keycode: Key.A, Pressed: true }) log.Add("child"); };
        child.VisibilityChanged += () => visibility.Add("signal:" + child.Visible);
        root.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == "gpu", "The effective renderer is GPU.");
            child.GrabFocus();
            var handles = SDL.GetWindows(out var count); Check(count == 1, "One actual native root.");
            var input = new SDL.Event { Key = new SDL.KeyboardEvent { Type = SDL.EventType.KeyDown, WindowID = SDL.GetWindowID(handles![0]), Key = SDL.Keycode.A, Scancode = SDL.Scancode.A, Down = true } };
            Check(SDL.PushEvent(ref input), "The native event queue accepted keyboard input.");
            RenderingServer.FramePostDraw += () =>
            {
                Check(log.SequenceEqual(["root", "child", "input"]), "Native root and embedded reception precede ordinary input.");
                using var image = root.GetTexture().GetImage()!; var color = image.GetPixel(44, 34); Check(color.G > .99f && color.R < .01f, "The receiving child renders actual green pixels.");
                if (frames++ == 0) { visibility.Clear(); child.Hide(); child.Show(); Check(visibility.SequenceEqual(["notification:False", "signal:False", "notification:True", "signal:True"]), "Native-hosted embedded visibility preserves both delivery phases."); }
                else root.Tree!.Quit();
            };
        };
        if (fail) Reject<AggregateException>(() => Engine.Run(root)); else Check(Engine.Run(root) == 0 && frames == 2, "Native window event lifecycle.");
        Check(log.SequenceEqual(["root", "child", "input"]) && root.IsDisposed && child.IsDisposed && Engine.MainLoop is null && !RenderingServer.IsAvailable && !DisplayServer.IsAvailable && !Input.IsKeyPressed(Key.A), "Failure and success release native services and held input after completing receivers.");
        Console.WriteLine("Native window input, observed pixels, visibility and cleanup passed; failure=" + fail);
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException(typeof(T).Name + " expected."); }
}
