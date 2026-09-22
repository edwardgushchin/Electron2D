using System.Diagnostics;
using Electron2D;
using SDL3;

internal static class WindowRuntimeTests
{
    public static void Run()
    {
        var engine = Engine.Instance;
        var oldLimit = engine.MaxFps;
        Reject<ArgumentOutOfRangeException>(() => engine.MaxFps = -1);
        engine.MaxFps = 20;
        try
        {
            using (var manualTree = new SceneTree(new Node()))
            {
                manualTree.Quit(1);
                manualTree.Quit(2);
                Check(manualTree.Process(0) && manualTree.PhysicsProcess(0) && manualTree.ExitCode == 2,
                    "Manual loop lanes report quit and keep the latest accepted code.");
                manualTree.Dispose();
                Reject<ObjectDisposedException>(() => manualTree.Quit());
            }
            using (var detached = new Window())
            {
                Check(detached.Size == new Vector2I(100, 100) && detached.Title == "" && detached.Visible &&
                      detached.MinSize == Vector2I.Zero && detached.MaxSize == Vector2I.Zero && detached.GetWindowId() == -1,
                    "Window defaults and detached identity are explicit.");
                Reject<ArgumentOutOfRangeException>(() => detached.Size = Vector2I.Zero);
                Reject<ArgumentException>(() => detached.Title = "bad\0title");
                Reject<InvalidOperationException>(() => detached.GrabFocus());
                Reject<InvalidOperationException>(() => detached.IsInputHandled());
                Reject<InvalidOperationException>(() => new SceneTree(detached));
                Check(!detached.IsDisposed && detached.Tree is null, "Rejected manual activation keeps caller ownership.");
                using var parent = new Node();
                Reject<NotSupportedException>(() => parent.AddChild(detached));
                Check(parent.ChildCount == 0 && detached.Parent is null, "Unsupported child viewport rejects before mutation.");
                var captured = detached.BeginSceneCapture();
                try
                {
                    Reject<InvalidOperationException>(() => engine.Run(detached));
                    Check(!detached.IsDisposed && DisplayServer.Instance is null && engine.MainLoop is null,
                        "Capture rejects activation before native acquisition or ownership transfer.");
                }
                finally { Node.EndSceneCapture(captured); }
                detached.MinSize = new Vector2I(70, 60);
                Reject<ArgumentOutOfRangeException>(() => detached.MaxSize = new Vector2I(69, 80));
                Check(detached.MaxSize == Vector2I.Zero, "Rejected constraints preserve state.");
                detached.MaxSize = new Vector2I(400, 300);
                detached.Title = "Packed window";
                using var packed = new PackedScene();
                packed.Pack(detached);
                using var copy = (Window)packed.Instantiate();
                Check(copy.Title == detached.Title && copy.MinSize == detached.MinSize && copy.MaxSize == detached.MaxSize,
                    "Packed windows reconstruct detached native-independent configuration.");
            }

            var order = new List<string>();
            var window = NewWindow();
            var probe = new Probe
            {
                ReadyAction = node =>
                {
                    Check(ReferenceEquals(engine.MainLoop, node.Tree) && window.Tree!.Root == window && node.GetWindow() == window && node.GetViewport() == window,
                        "Scene children discover their actual root Window/Viewport before ready.");
                    Check(window.GetWindowId() == 0 && DisplayServer.Instance is not null,
                        "Native window is open before scene ready.");
                    window.Title = "Live title";
                    Check(window.Title == "Live title", "Title round trips through the native backend.");
                    var native = NativeWindow();
                    ((Node)window).Hide();
                    Check(!window.Visible && (SDL.GetWindowFlags(native) & SDL.WindowFlags.Hidden) != 0,
                        "Visibility through Node hides the native window.");
                    ((Node)window).Show();
                    Check(window.Visible && (SDL.GetWindowFlags(native) & SDL.WindowFlags.Hidden) == 0,
                        "Visibility through Node shows the native window.");
                    window.Position = new Vector2(9, 11);
                    Check(window.GetVisibleRect().Position == Vector2.Zero, "Viewport client origin is independent of Node.Position.");
                    Reject<InvalidOperationException>(() => engine.AdvanceFrame(0));
                    Reject<InvalidOperationException>(engine.Stop);
                    using var other = new Window();
                    Reject<InvalidOperationException>(() => engine.Run(other));
                    Check(!other.IsDisposed, "Rejected concurrent Run retains argument ownership.");
                    PushKey();
                },
                InputAction = (node, input) =>
                {
                    if (input is not InputEventKey { Keycode: Key.A, Pressed: true }) return;
                    Check(Input.Instance.IsKeyPressed(Key.A), "Native input commits before scene dispatch.");
                    order.Add("input");
                    window.SetInputAsHandled();
                    Check(window.IsInputHandled(), "Viewport shares the scene's current input boundary.");
                },
                PhysicsAction = (_, _) => order.Add("physics"),
                ProcessAction = (node, delta) =>
                {
                    Check(double.IsFinite(delta) && delta >= 0, "Frame delta is finite and nonnegative.");
                    Check(order.Contains("input"), "Native input precedes the frame.");
                    order.Add("process");
                    if (node.Frames == 2) node.Tree!.Quit(17);
                },
                ExitAction = node =>
                {
                    Check(window.GetWindowId() == 0, "The native window outlives scene exit.");
                    using var other = new Window();
                    Reject<InvalidOperationException>(() => engine.Run(other));
                }
            };
            window.AddChild(probe);
            var clock = Stopwatch.StartNew();
            Check(engine.Run(window) == 17 && clock.ElapsedMilliseconds >= 40 && probe.Frames == 2 &&
                  order.IndexOf("physics") < order.LastIndexOf("process"), "Run enforces monotonic frame limiting and returns the requested code.");
            AssertReleased(window, probe);

            window = NewWindow();
            probe = new Probe { ReadyAction = _ => PushClose() };
            window.AddChild(probe);
            var closeSignals = 0;
            var pushedInputs = 0;
            probe.InputAction = (_, input) =>
            {
                if (input is not InputEventKey { Keycode: Key.B }) return;
                Check(!Input.Instance.IsKeyPressed(Key.B), "PushInput does not alter global polling state.");
                pushedInputs++;
                window.SetInputAsHandled();
            };
            window.CloseRequested += () =>
            {
                closeSignals++;
                Reject<InvalidOperationException>(window.Tree!.Dispose);
                using var borrowed = new InputEventKey { Keycode = Key.B, Pressed = true };
                window.PushInput(borrowed);
                Check(!borrowed.IsDisposed, "Viewport borrows rather than disposes pushed input.");
            };
            Check(engine.Run(window) == 0 && closeSignals == 1 && pushedInputs == 1 && probe.Frames == 0, "Default close quits before the first frame after signaling.");
            AssertReleased(window, probe);

            window = NewWindow();
            probe = new Probe
            {
                ReadyAction = _ => PushClose(),
                ProcessAction = (node, _) => node.Tree!.Quit(23)
            };
            window.CloseRequested += () => window.Tree!.AutoAcceptQuit = false;
            window.AddChild(probe);
            Check(engine.Run(window) == 23 && probe.Frames == 1, "A close handler can override default quit behavior.");
            AssertReleased(window, probe);

            foreach (var phase in new[] { "ready", "input", "process", "exit", "close" })
            {
                window = NewWindow();
                probe = new Probe
                {
                    ReadyAction = node => { if (phase == "ready") Fail(phase); if (phase == "close") PushClose(); else PushKey(); },
                    InputAction = (_, input) => { if (phase == "input" && input is InputEventKey) Fail(phase); },
                    ProcessAction = (node, _) => { if (phase == "process") Fail(phase); node.Tree!.Quit(); },
                    ExitAction = _ => { if (phase == "exit") Fail(phase); }
                };
                if (phase == "close") window.CloseRequested += () => Fail(phase);
                window.AddChild(probe);
                try { engine.Run(window); throw new Exception("Expected injected failure: " + phase); }
                catch (Exception error) when (error.ToString().Contains("injected " + phase, StringComparison.Ordinal)) { }
                AssertReleased(window, probe);
            }

            window = NewWindow();
            window.Visible = false;
            var sizes = 0;
            window.SizeChanged += () =>
            {
                sizes++;
                Check(window.GetVisibleRect().Size == new Vector2(window.Size.X, window.Size.Y),
                    "Size notification observes the committed viewport dimensions.");
            };
            window.AddChild(new Probe
            {
                ReadyAction = _ =>
                {
                    Check((SDL.GetWindowFlags(NativeWindow()) & SDL.WindowFlags.Hidden) != 0, "Hidden startup preserves configured visibility.");
                    window.Show();
                    var input = new SDL.Event
                    {
                        Window = new SDL.WindowEvent
                        {
                            Type = SDL.GetCurrentVideoDriver() == "wayland" ? SDL.EventType.WindowPixelSizeChanged : SDL.EventType.WindowResized,
                            WindowID = SDL.GetWindowID(NativeWindow()),
                            Data1 = 180,
                            Data2 = 110
                        }
                    };
                    Check(SDL.PushEvent(ref input), "The backend accepts the size event.");
                },
                ProcessAction = (node, _) => { Check(sizes > 0, "Client resize notifies before frame processing."); node.Tree!.Quit(); }
            });
            engine.Run(window);
            AssertReleased(window);

            if (Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "wayland")
            {
                window = NewWindow();
                window.ScreenPosition = Vector2I.Zero;
                Reject<NotSupportedException>(() => engine.Run(window));
                AssertReleased(window);
            }

            // A second native owner must survive a rejected startup and remain usable.
            using (var existing = DisplayServer.Open("Existing owner", new Vector2I(100, 100), hidden: true))
            {
                window = NewWindow();
                Reject<InvalidOperationException>(() => engine.Run(window));
                Check(window.IsDisposed && ReferenceEquals(DisplayServer.Instance, existing), "Failed startup releases only transferred resources.");
                existing.WindowSetTitle("Still alive");
            }

            window = NewWindow();
            window.AddChild(new Probe { ReadyAction = node => Task.Run(() => node.Tree!.Quit(31)).GetAwaiter().GetResult() });
            Check(engine.Run(window) == 31 && window.IsDisposed, "Cross-thread quit during ready works and startup observes it before frames.");
            Console.WriteLine("Window runtime checks passed.");
        }
        finally { engine.MaxFps = oldLimit; }
    }

    private static Window NewWindow() => new() { Title = "Window runtime checks", Size = new Vector2I(160, 100) };
    private static void Fail(string phase) => throw new InvalidOperationException("injected " + phase);
    private static void AssertReleased(Window window, Node? child = null) => Check(window.IsDisposed &&
        (child is null || child.IsDisposed) && Engine.Instance.MainLoop is null && DisplayServer.Instance is null &&
        !Input.Instance.IsKeyPressed(Key.A), "Run releases scene, window, loop attachment, and native input state.");
    private static nint NativeWindow()
    {
        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is { Length: 1 }, "Exactly one native window exists.");
        return windows![0];
    }
    private static void PushClose()
    {
        var input = new SDL.Event { Window = new SDL.WindowEvent { Type = SDL.EventType.WindowCloseRequested, WindowID = SDL.GetWindowID(NativeWindow()) } };
        Check(SDL.PushEvent(ref input), "The backend accepts a close event.");
    }
    private static void PushKey()
    {
        var input = new SDL.Event { Key = new SDL.KeyboardEvent { Type = SDL.EventType.KeyDown, WindowID = SDL.GetWindowID(NativeWindow()), Key = SDL.Keycode.A, Scancode = SDL.Scancode.A, Down = true } };
        Check(SDL.PushEvent(ref input), "The backend accepts a key event.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
    private sealed class Probe : Node
    {
        public Action<Probe>? ReadyAction;
        public Action<Probe, InputEvent>? InputAction;
        public Action<Probe, double>? ProcessAction;
        public Action<Probe, double>? PhysicsAction;
        public Action<Probe>? ExitAction;
        public int Frames;
        protected override void OnReady() { InputEnabled = ProcessEnabled = PhysicsProcessEnabled = true; ReadyAction?.Invoke(this); }
        protected override void OnInput(InputEvent input) => InputAction?.Invoke(this, input);
        protected override void OnProcess(double delta) { Frames++; ProcessAction?.Invoke(this, delta); }
        protected override void OnPhysicsProcess(double delta) => PhysicsAction?.Invoke(this, delta);
        protected override void OnExitTree() => ExitAction?.Invoke(this);
    }
}
