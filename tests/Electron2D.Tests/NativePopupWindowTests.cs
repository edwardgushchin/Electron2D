using Electron2D;
using SDL = SDL3.SDL;

internal static partial class NativePopupWindowTests
{
    private static void Lifecycle(string backend)
    {
        if (backend is not "gpu" and not "compatibility") throw new ArgumentException("Unknown renderer.", nameof(backend));
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        var root = new Window { Title = "Native popup root", Size = new(320, 220) };
        var popup = new Popup { Title = "Native popup first", Size = new(96, 64) };
        var nested = new Popup { Title = "Native popup nested", Size = new(48, 32) };
        popup.AddChild(new ColorRect { Color = Colors.Red, Size = new(96, 64) }); nested.AddChild(new ColorRect { Color = Colors.Green, Size = new(48, 32) });
        root.AddChild(popup); popup.AddChild(nested);
        var independent = new Window { Title = "Independent", Visible = false, Size = new(80, 60) }; root.AddChild(independent);
        var forcedCloseSignals = 0; nested.CloseRequested += () => forcedCloseSignals++;
        var scopedForced = 0; DisplayWindowEvents? nestedEvents = null;
        var firstOpening = true;
        popup.AboutToPopup += () => { if (firstOpening) { firstOpening = false; popup.Position = new(900, 900); } };
        var focusWait = 0; var frames = 0; var oldID = -1; var hides = 0; popup.PopupHide += () => hides++;
        try
        {
            root.Ready += _ =>
            {
                popup.PopupOnParent(new(12, 16, 96, 64));
                RenderingServer.FramePostDraw += () =>
                {
                    // Wayland acknowledges popup grabs asynchronously; nest or close only after that setup completes.
                    if (SDL.GetCurrentVideoDriver() == "wayland" && frames is 0 or 1)
                    {
                        var awaiting = frames == 0 ? popup : nested;
                        Check(awaiting.Visible, "The compositor dismissed a popup before its setup completed.");
                        if (SDL.GetKeyboardFocus() != Native(awaiting))
                        {
                            if (++focusWait > 240) throw new TimeoutException("The compositor did not grant popup keyboard focus.");
                            return;
                        }
                    }
                    focusWait = 0;
                    frames++;
                    if (frames == 1)
                    {
                        Check(popup.GetWindowID() > 0 && popup.NativeDisplay!.NativePopupParent == root.NativeDisplay, "A public popup opens against the live native parent, including Ready.");
                        Check(SDL.GetWindowParent(Native(popup)) == Native(root), "SDL retains the actual popup parent.");
                        Check(DisplayServer.WindowGetActivePopup() == popup.GetWindowID(), "The public active-popup query observes native ownership.");
                        Pixel(popup, Colors.Red); oldID = popup.GetWindowID(); nested.PopupOnParent(new(8, 8, 48, 32));
                    }
                    else if (frames == 2)
                    {
                        Check(nested.GetWindowID() > oldID && nested.NativeDisplay!.NativePopupParent == popup.NativeDisplay, "Nested native popup parenting preserves one shared renderer.");
                        Pixel(nested, Colors.Green);
                        nestedEvents = DisplayServer.GetWindowEvents(nested.GetWindowID());
                        nestedEvents.ForceCloseRequested += () => { Check(nested.Visible, "The forced native request precedes automatic hiding."); scopedForced++; };
                        var close = new SDL.Event { Window = new SDL.WindowEvent { Type = SDL.EventType.WindowCloseRequested, WindowID = SDL.GetWindowID(Native(nested)) } };
                        Check(SDL.PushEvent(ref close), "The native queue accepts popup dismissal.");
                    }
                    else if (frames == 3)
                    {
                        Check(scopedForced == 1 && nestedEvents!.IsDisposed, "Native popup dismissal publishes one scoped forced request, then retires its publisher.");
                        Check(!nested.Visible && nested.GetWindowID() == -1 && popup.Visible && forcedCloseSignals == 0, $"Native popup-done force-hides its lifetime without a user close request: nested={nested.Visible}/{nested.GetWindowID()}, parent={popup.Visible}/{popup.GetWindowID()}, closeRequests={forcedCloseSignals}.");
                        popup.Hide(); Check(hides == 1, "PopupHide follows native retirement."); popup.PopupOnParent(new(12, 16, 96, 64));
                    }
                    else if (frames == 4)
                    {
                        Check(popup.GetWindowID() > oldID, "A hidden popup recreates a fresh native identity."); Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowGetPopupSafeRect(oldID)); Pixel(popup, Colors.Red);
                        var press = new SDL.Event { Button = new SDL.MouseButtonEvent { Type = SDL.EventType.MouseButtonDown, WindowID = SDL.GetWindowID(Native(root)), Button = 1, Down = true, X = 0, Y = 0 } };
                        var release = new SDL.Event { Button = new SDL.MouseButtonEvent { Type = SDL.EventType.MouseButtonUp, WindowID = SDL.GetWindowID(Native(root)), Button = 1, Down = false, X = 0, Y = 0 } };
                        Check(SDL.PushEvent(ref press) && SDL.PushEvent(ref release), "The native queue accepts an outside press/release.");
                    }
                    else if (frames == 5)
                    {
                        Check(!popup.Visible && hides == 2 && DisplayServer.WindowGetActivePopup() == -1 && !Input.IsMouseButtonPressed(MouseButton.Left), "Outside dismissal retires popup ownership and preserves global release pairing.");
                        popup.Unfocusable = true; popup.PopupOnParent(new(12, 16, 96, 64));
                        Check(DisplayServer.WindowGetFlag(WindowFlag.NoFocus, popup.GetWindowID()) && !popup.HasFocus(), "An unfocusable native popup retains its real no-focus role.");
                        Contracts(popup);
                        nested.PopupOnParent(new(8, 8, 48, 32)); independent.Show();
                        nestedEvents = DisplayServer.GetWindowEvents(nested.GetWindowID());
                        nestedEvents.ForceCloseRequested += () => { scopedForced++; throw new InvalidOperationException("scoped forced popup observer"); };
                    }
                    else
                    {
                        Action failure = () => throw new InvalidOperationException("popup observer"); nested.PopupHide += failure;
                        Reject<AggregateException>(() => root.Hide());
                        Check(!root.Visible && !popup.Visible && !nested.Visible && independent.Visible && independent.GetWindowID() > 0, "Parent hide retires every popup despite observer failure and preserves independent native children.");
                        Check(DisplayServer.WindowGetActivePopup() == -1, "The popup stack retires after parent hide.");
                        Check(scopedForced == 2 && nestedEvents!.IsDisposed, "Parent retirement delivers and retires scoped publishers despite forced-request callback failure.");
                        root.Tree!.Quit();
                    }
                };
            };
            Check(Engine.Run(root) == 0 && frames == 6, "Native popup lifecycle completes.");
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
        Console.WriteLine($"Native nested popup parenting, public queries, dismissal and actual pixels passed on {backend}.");
    }

    internal static void Run(string backend)
    {
        var previousFPS = Engine.MaxFPS; Engine.MaxFPS = 60;
        try { Lifecycle(backend); InputPolicy(backend); Menus(backend); Authoring(backend); }
        finally { Engine.MaxFPS = previousFPS; }
    }

    private static void Contracts(Popup popup)
    {
        var id = popup.GetWindowID();
        Check(DisplayServer.WindowGetPopupSafeRect(id) == default, "A new popup starts without a safe rectangle.");
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowSetPopupSafeRect(id, new(0, 0, -1, 3)));
        Reject<InvalidOperationException>(() => popup.PopupWindow = false);
        Reject<InvalidOperationException>(() => popup.PopupWMHint = false);
        Reject<NotSupportedException>(() => popup.Borderless = false);
        Reject<NotSupportedException>(() => popup.Unresizable = false);
        Reject<NotSupportedException>(() => popup.Mode = WindowMode.Maximized);
        Check(popup.Borderless && popup.Unresizable && popup.Mode == WindowMode.Windowed, "Rejected role changes preserve accepted configuration.");
        DisplayServer.WindowSetTitle("Popup metadata", id); Check(popup.Title == "Popup metadata", "Undecorated popup titles retain their logical metadata.");
        DisplayServer.WindowSetFlag(WindowFlag.Popup, false, id); Check(DisplayServer.WindowGetActivePopup() == -1, "The display popup policy removes its input registration.");
        DisplayServer.WindowSetFlag(WindowFlag.Popup, true, id); Check(DisplayServer.WindowGetActivePopup() == id, "Restoring display popup policy registers the live window.");
        if (SDL.GetCurrentVideoDriver() == "wayland")
        {
            Reject<NotSupportedException>(() => DisplayServer.WindowSetPopupSafeRect(id, new(0, 0, 20, 20)));
            Reject<NotSupportedException>(() => { _ = popup.Position; });
        }
        else
        {
            var safe = new Rect2i(popup.Position, new(20, 20)); DisplayServer.WindowSetPopupSafeRect(id, safe);
            Check(DisplayServer.WindowGetPopupSafeRect(id) == safe, "The public safe area preserves actual global coordinates.");
        }
        DisplayServer.WindowSetPopupSafeRect(id, default);
        Task.Run(() => Reject<InvalidOperationException>(() => DisplayServer.WindowGetActivePopup())).GetAwaiter().GetResult();
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowGetPopupSafeRect(int.MaxValue));
    }

    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }

    private static nint Native(Window window)
    {
        var handle = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, window.GetWindowID());
        var wayland = SDL.GetCurrentVideoDriver() == "wayland";
        return SDL.GetWindows(out _)!.Single(native => wayland
            ? SDL.GetPointerProperty(SDL.GetWindowProperties(native), SDL.Props.WindowWaylandSurfacePointer, 0) == handle
            : (nint)SDL.GetNumberProperty(SDL.GetWindowProperties(native), SDL.Props.WindowX11WindowNumber, 0) == handle);
    }
    private static void Pixel(Window window, Color expected)
    {
        using var image = window.GetTexture().GetImage()!; var actual = image.GetPixel(16, 16);
        Check(MathF.Abs(actual.R - expected.R) < .03f && MathF.Abs(actual.G - expected.G) < .03f && MathF.Abs(actual.B - expected.B) < .03f, $"Popup pixels: expected {expected}, actual {actual}.");
        using var present = RenderingServer.Service!.ReadbackNativeWindow(window.GetWindowID()); var displayed = present.GetPixel(16, 16);
        Check(MathF.Abs(displayed.R - expected.R) < .03f && MathF.Abs(displayed.G - expected.G) < .03f && MathF.Abs(displayed.B - expected.B) < .03f, "The native popup surface presents its authoritative pixels.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
