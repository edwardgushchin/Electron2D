using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Electron2D;
using SDL3;
using Edge = Electron2D.DisplayServer.WindowResizeEdge;

internal static unsafe partial class WindowsWindowGestureTests
{
    private const uint Move = 0x0216, Size = 0x0214, Enter = 0x0231, Exit = 0x0232;
    private const uint MouseMove = 0x0200, ButtonDown = 0x0201, ButtonUp = 0x0202, KeyDown = 0x0100, Cancel = 0x001F;

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        internal int Left, Top, Right, Bottom;
        internal readonly Rect2i Bounds => new(Left, Top, Right - Left, Bottom - Top);
    }

    private sealed class Observation : IDisposable
    {
        internal readonly ManualResetEventSlim Entered = new();
        internal readonly ManualResetEventSlim Exited = new();
        internal int Entries, Exits, Direction;
        internal Rect2i Candidate;
        internal bool MotionBeforeEntry;

        public void Dispose()
        {
            Exited.Dispose();
            Entered.Dispose();
        }
    }

    internal static void Run()
    {
        Check(OperatingSystem.IsWindows(), "The native gesture profile needs Windows.");
        var desktop = CreateDesktop($"Electron2D_Gestures_{Environment.ProcessId}", 0, 0, 0, 0x10000000, 0);
        Check(desktop != 0, "Create an isolated Win32 desktop.");
        if (SetThreadDesktop(desktop) == 0)
        {
            CloseDesktop(desktop);
            throw new InvalidOperationException("Run SDL and the fixture on the private desktop.");
        }

        // This profile has its own process. Windows retires the desktop with its attached thread.
        VerifyDisplay(desktop);
        VerifyScene(desktop);
        Console.WriteLine("Win32 window gestures passed on an isolated desktop.");
    }

    private static void VerifyDisplay(nint desktop)
    {
        using var display = DisplayServer.Open("Win32 gesture fixture", new(360, 240));
        DisplayServer.WindowSetFlag(WindowFlag.Borderless, true);
        var window = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
        var events = DisplayServer.GetWindowEvents();
        var nativeWindows = SDL.GetWindows(out var count);
        Check(count == 1 && nativeWindows is not null, "The input fixture owns one SDL window.");
        var sdlID = SDL.GetWindowID(nativeWindows![0]);
        Input.UseAccumulatedInput = true;
        Action? gesture = null;
        Observation? motionProbe = null;
        var pressed = 0;
        var canceled = 0;
        var normalReleases = 0;
        events.Input += value =>
        {
            if (motionProbe is { } probe && value is InputEventMouseMotion motion && motion.Position == new Vector2(51, 63))
                probe.MotionBeforeEntry = !probe.Entered.IsSet;
            if (value is not InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
                return;
            if (button.IsPressed())
            {
                pressed++;
                gesture?.Invoke();
            }
            else if (button.Canceled)
                canceled++;
            else if (button.IsReleased())
                normalReleases++;
        };
        DisplayServer.ProcessEvents();
        var initial = Bounds(window);
        DisplayServer.WindowStartDrag();
        DisplayServer.ProcessEvents();
        Check(Bounds(window) == initial, "A request without a press leaves the rectangle alone.");

        var after = Exercise(null, 0, 0x27);
        Check(after.Position != initial.Position && after.Size == initial.Size, "The native move preserves the size.");

        foreach (var (edge, direction, key) in new (Edge, int, uint)[]
        {
            (Edge.Left, 1, 0x25), (Edge.Right, 2, 0x27), (Edge.Top, 3, 0x26),
            (Edge.TopLeft, 4, 0x25), (Edge.TopRight, 5, 0x27), (Edge.Bottom, 6, 0x28),
            (Edge.BottomLeft, 7, 0x25), (Edge.BottomRight, 8, 0x27),
        })
        {
            var before = Bounds(window);
            after = Exercise(edge, direction, key);
            Check(after.Size != before.Size, $"Native {edge} changes the client size from {before} to {after}.");
            if (edge is Edge.Left or Edge.TopLeft or Edge.BottomLeft)
                Check(after.End.X == before.End.X, $"{edge} preserves the opposite horizontal edge.");
            else
                Check(after.Position.X == before.Position.X, $"{edge} preserves the horizontal origin.");
            if (edge is Edge.Top or Edge.TopLeft or Edge.TopRight)
                Check(after.End.Y == before.End.Y, $"{edge} preserves the opposite vertical edge.");
            else
                Check(after.Position.Y == before.Position.Y, $"{edge} preserves the vertical origin.");
        }

        gesture = () => DisplayServer.WindowStartDrag();
        SendButton(window, true);
        SendButton(window, false);
        DisplayServer.ProcessEvents();
        Check(Bounds(window) == after && canceled == 9 && normalReleases == 1,
            "Releasing the button before handoff preserves the ordinary click.");

        gesture = null;
        var baseline = pressed;
        SendButton(window, true);
        DisplayServer.ProcessEvents();
        Check(pressed == baseline + 1 && Input.IsMouseButtonPressed(MouseButton.Left), "Ordinary input follows a gesture.");
        DisplayServer.WindowStartDrag();
        SendButton(window, false);
        DisplayServer.ForceProcessAndDropEvents();
        Check(!Input.IsMouseButtonPressed(MouseButton.Left) && canceled == 9 && normalReleases == 1,
            "Dropping queued input discards a pending native request and releases managed input.");
        Check(Bounds(window) == after, "A dropped request leaves the native rectangle alone.");

        DisplayServer.WindowSetFlag(WindowFlag.ResizeDisabled, true);
        gesture = () => DisplayServer.WindowStartResize(Edge.Right);
        SendButton(window, true);
        DisplayServer.ProcessEvents();
        SendButton(window, false);
        DisplayServer.ProcessEvents();
        Check(Bounds(window) == after && canceled == 9 && normalReleases == 2,
            "Resize-disabled windows retain the ordinary release and their geometry.");

        Rect2i Exercise(Edge? edge, int direction, uint key)
        {
            gesture = edge is null ? () => DisplayServer.WindowStartDrag() : () => DisplayServer.WindowStartResize(edge.Value);
            var expectedPress = pressed + 1;
            var expectedRelease = canceled + 1;
            var before = Bounds(window);
            using var observation = new Observation();
            using var driver = new GestureDriver(desktop, window, observation, key);
            try
            {
                SendButton(window, true);
                if (edge is null)
                {
                    motionProbe = observation;
                    SDL.PumpEvents();
                    var queued = new SDL.Event
                    {
                        Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = sdlID, X = 51, Y = 63 },
                    };
                    Check(SDL.PushEvent(ref queued), "Queue client motion after the Win32 press.");
                }
                DisplayServer.ProcessEvents();
            }
            finally { SetLeftKeyState(false); motionProbe = null; }
            driver.Wait();
            if (edge is null)
                Check(observation.MotionBeforeEntry, "Accumulated client motion is delivered before entering the Win32 loop.");
            var current = Bounds(window);
            Check(pressed == expectedPress && canceled == expectedRelease && !Input.IsMouseButtonPressed(MouseButton.Left),
                $"The system loop consumes one press and its canceled release ({edge}, press {pressed}, canceled {canceled}).");
            Check(observation.Entries == 1 && observation.Exits == 1 && observation.Direction == direction,
                $"Win32 observed the requested move or sizing direction ({edge}, enter {observation.Entries}, exit {observation.Exits}, edge {observation.Direction}).");
            Check(current == observation.Candidate && current != before,
                $"The window rectangle commits the native move/size message ({edge}, before {before}, actual {current}, native {observation.Candidate}).");
            Check(DisplayServer.WindowGetSize() == current.Size, "SDL publishes the native client size.");
            return current;
        }
    }

    private static void VerifyScene(nint desktop)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var root = new Window { Size = new(320, 240), Borderless = true, Title = "Win32 gesture scene" };
        root.AddChild(new ColorRect { Size = new(64, 64), Color = Colors.Red });
        using var observation = new Observation();
        GestureDriver? driver = null;
        var frames = 0;
        var released = 0;
        var window = (nint)0;
        var before = default(Rect2i);
        var completed = false;
        root.WindowInput += value =>
        {
            if (value is not InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
                return;
            if (button.IsPressed())
                root.StartDrag();
            else if (button.Canceled)
                released++;
        };
        root.Ready += _ => RenderingServer.FramePostDraw += Drawn;
        try
        {
            Check(Engine.Run(root) == 0 && completed, "The rendered scene completes its native gesture.");
        }
        finally
        {
            SetLeftKeyState(false);
            driver?.Dispose();
        }
        Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "The native scene host retires.");

        void Drawn()
        {
            Check(++frames < 600, "The Win32 scene resumes rendering after the modal move.");
            if (driver is null)
            {
                window = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
                before = Bounds(window);
                driver = new GestureDriver(desktop, window, observation, 0x27);
                SendButton(window, true);
                return;
            }
            if (released == 0)
                return;

            driver.Wait();
            Check(released == 1 && observation.Entries == 1 && observation.Exits == 1 &&
                Bounds(window).Position != before.Position && Bounds(window).Size == before.Size,
                "The scene window receives the native move and one canceled release.");
            using var image = root.GetTexture().GetImage();
            Check(image is not null && image.GetPixel(8, 8) == Colors.Red, "The moved scene still renders the expected pixel.");
            completed = true;
            driver.Dispose();
            driver = null;
            RenderingServer.FramePostDraw -= Drawn;
            root.Tree!.Quit();
        }
    }

    private sealed class GestureDriver : IDisposable
    {
        private readonly nint _window;
        private readonly Thread _actor;
        private GCHandle _handle;
        private Exception? _failure;

        internal GestureDriver(nint desktop, nint window, Observation observation, uint key)
        {
            _window = window;
            _handle = GCHandle.Alloc(observation);
            if (SetWindowSubclass(window, &Observe, 1, (nuint)GCHandle.ToIntPtr(_handle)) == 0)
            {
                _handle.Free();
                throw new InvalidOperationException("Observe actual Win32 move/size messages.");
            }
            _actor = new Thread(() =>
            {
                try
                {
                    Check(SetThreadDesktop(desktop) != 0, "Attach the input fixture to its private desktop.");
                    if (!observation.Entered.Wait(TimeSpan.FromMilliseconds(350)))
                        PostMessage(window, MouseMove, 1, 40 | (50 << 16));
                    Check(observation.Entered.Wait(TimeSpan.FromSeconds(2)), "Win32 enters its move/size loop.");
                    Check(PostMessage(window, KeyDown, key, 1) != 0, "Drive the native loop with a private arrow message.");
                    Check(PostMessage(window, ButtonUp, 0, 30 | (40 << 16)) != 0, "Release the initiating button.");
                    Check(observation.Exited.Wait(TimeSpan.FromSeconds(2)), "Win32 leaves its move/size loop.");
                }
                catch (Exception error)
                {
                    _failure = error;
                    PostMessage(window, Cancel, 0, 0);
                    PostMessage(window, KeyDown, 0x1B, 1);
                }
            })
            { IsBackground = true };
            _actor.Start();
        }

        internal void Wait()
        {
            Check(_actor.Join(TimeSpan.FromSeconds(5)), "The private input fixture retires.");
            if (_failure is not null)
                throw new InvalidOperationException("The Win32 gesture fixture failed.", _failure);
        }

        public void Dispose()
        {
            Check(_actor.Join(TimeSpan.FromSeconds(5)), "The private input fixture retires.");
            RemoveWindowSubclass(_window, &Observe, 1);
            _handle.Free();
        }
    }
    private static void SendButton(nint window, bool down)
    {
        SetLeftKeyState(down);
        Check(PostMessage(window, down ? ButtonDown : ButtonUp, down ? 1u : 0, 30 | (40 << 16)) != 0,
            "Send a mouse message to the isolated window.");
    }

    private static void SetLeftKeyState(bool down)
    {
        byte* keys = stackalloc byte[256];
        Check(GetKeyboardState(keys) != 0, "Read the fixture thread's input state.");
        var key = GetSystemMetrics(23) == 0 ? 1 : 2;
        keys[key] = down ? (byte)0x80 : (byte)0;
        Check(SetKeyboardState(keys) != 0, "Set the isolated thread's button state.");
    }

    private static Rect2i Bounds(nint window)
    {
        Check(GetWindowRect(window, out var bounds) != 0, "Read native geometry independently of the display service.");
        return bounds.Bounds;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint Observe(nint window, uint message, nuint wParam, nint lParam, nuint id, nuint data)
    {
        var observation = (Observation)GCHandle.FromIntPtr((nint)data).Target!;
        if (message == Enter)
        {
            observation.Entries++;
            observation.Entered.Set();
        }
        else if (message == Exit)
        {
            observation.Exits++;
            observation.Exited.Set();
        }
        var result = DefSubclassProc(window, message, wParam, lParam);
        if (message is Move or Size)
        {
            observation.Direction = message == Move ? 0 : (int)wParam;
            observation.Candidate = ((Rect*)lParam)->Bounds;
        }
        return result;
    }

    [LibraryImport("user32.dll", EntryPoint = "CreateDesktopW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint CreateDesktop(string name, nint device, nint mode, uint flags, uint access, nint attributes);
    [LibraryImport("user32.dll")] private static partial int SetThreadDesktop(nint desktop);
    [LibraryImport("user32.dll")] private static partial int CloseDesktop(nint desktop);
    [LibraryImport("user32.dll")] private static partial int GetSystemMetrics(int index);
    [LibraryImport("user32.dll")] private static partial int GetKeyboardState(byte* state);
    [LibraryImport("user32.dll")] private static partial int SetKeyboardState(byte* state);
    [LibraryImport("user32.dll")] private static partial int GetWindowRect(nint window, out Rect rect);
    [LibraryImport("user32.dll", EntryPoint = "PostMessageW")]
    private static partial int PostMessage(nint window, uint message, nuint wParam, nint lParam);
    [LibraryImport("comctl32.dll")]
    private static partial int SetWindowSubclass(nint window, delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nuint, nuint, nint> callback, nuint id, nuint data);
    [LibraryImport("comctl32.dll")]
    private static partial int RemoveWindowSubclass(nint window, delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nuint, nuint, nint> callback, nuint id);
    [LibraryImport("comctl32.dll")]
    private static partial nint DefSubclassProc(nint window, uint message, nuint wParam, nint lParam);
}
