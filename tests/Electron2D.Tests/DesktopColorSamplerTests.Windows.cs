using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Electron2D;

internal static partial class DesktopColorSamplerTests
{
    internal static void RunWindows(string[] args)
    {
        Check(OperatingSystem.IsWindows() && args.Length is 1 or 2, "Provide the native Windows color sampling actor.");
        var backend = args.Length == 2 ? args[1] : "compatibility";
        Check(backend is "gpu" or "compatibility", "Select a renderer for the Windows color sampler.");
        Check(GetCursorPos(out var original) != 0, "Save the original pointer position.");
        var start = new ProcessStartInfo(args[0]) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
        start.ArgumentList.Add("830");
        start.ArgumentList.Add("160");
        using var actor = Process.Start(start) ?? throw new InvalidOperationException("Could not launch the color sampling actor.");
        nint target = 0;
        try
        {
            var reply = actor.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            target = nint.Parse(reply!, CultureInfo.InvariantCulture);
            Check(GetWindowThreadProcessId(target, out var pid) != 0 && pid == (uint)actor.Id, "The pixel source belongs to the independent native actor.");
            WindowsScene(target, backend);
        }
        finally
        {
            if (target != 0) _ = PostMessageW(target, 0x0010, 0, 0);
            if (!actor.WaitForExit(3000)) { actor.Kill(); actor.WaitForExit(3000); }
            _ = SetCursorPos(original.X, original.Y);
        }
    }

    private static void WindowsScene(nint target, string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 30;
        var root = new Window { Title = "Windows desktop color sampler", Position = new(40, 60), Size = new(650, 730), Borderless = true };
        var button = new ColorPickerButton { Position = new(100, 65), Size = new(100, 35), Color = Colors.Red };
        root.AddChild(button);
        button.GetPopup().Transparent = false;
        var picker = button.GetPicker();
        var pick = (Button)ColorPickerTests.Find(picker, "Pick");
        var changes = 0;
        button.ColorChanged += _ => changes++;
        var point = ScreenPoint(target, new(70, 60));
        var deadline = Stopwatch.StartNew();
        var phase = 0;
        var quitWithSampler = false;
        var edge = Vector2i.Zero;

        root.Ready += _ => RenderingServer.FramePostDraw += Drawn;
        void Drawn()
        {
            var preview = picker.GetNodeOrNull("_desktop_color_sampler") as Window;
            Check(deadline.Elapsed < TimeSpan.FromSeconds(24),
                $"The Windows sampler stalled at phase {phase} with color {picker.Color}, preview {preview?.Visible}, error {(picker.GetNodeOrNull("_color_error") as AcceptDialog)?.DialogText}.");
            switch (phase)
            {
                case 0 when WindowFromPoint(new(point)) == target && DisplayServer.ScreenGetPixel(point) == Colors.Green:
                    Check(DisplayServer.HasFeature(DisplayServer.Feature.ScreenExcludeFromCapture), "This fixture needs Windows capture exclusion.");
                    ClickControl(button);
                    break;
                case 1 when button.GetPopup().Visible:
                    Check(pick.TooltipText == "Pick a color from the desktop.", "The picker advertises desktop sampling.");
                    Move(ControlPoint(pick));
                    break;
                case 2 when pick.IsHovered():
                    ClickControl(pick);
                    break;
                case 3 when preview?.Visible == true:
                    edge = DisplayServer.ScreenGetPosition(DisplayServer.ScreenPrimary);
                    Move(edge);
                    break;
                case 4 when preview?.Visible == true && preview.Position == edge:
                    Move(point);
                    break;
                case 5 when picker.Color == Colors.Green && preview?.Visible == true && PreviewMatches(preview, Colors.Green):
                    Check(button.Color == Colors.Red && changes == 0, "Preview updates silently.");
                    var native = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, preview.GetWindowID());
                    Check(WindowFromPoint(new(point)) == native, "The magnifier is composed over the point being sampled.");
                    Check(DisplayServer.WindowGetFlag(WindowFlag.ExcludeFromCapture, preview.GetWindowID()) &&
                        DisplayServer.ScreenGetPixel(point) == Colors.Green, "The magnifier is excluded from its own desktop sample.");
                    Paint(target, 0xff0000);
                    break;
                case 6 when picker.Color == Colors.Blue && preview?.Visible == true && PreviewMatches(preview, Colors.Blue):
                    var outside = point + new Vector2i(70, 20);
                    Check(WindowFromPoint(new(outside)) == target, "The select click is outside the preview, over the foreign window.");
                    Click(outside, 0x0002, 0x0004);
                    Move(point - new Vector2i(22, 0));
                    Thread.Sleep(1100);
                    break;
                case 7 when changes == 1:
                    Check(preview?.Visible == false && button.Color == Colors.Blue && picker.Color == Colors.Blue &&
                        picker.GetRecentPresets().Contains(Colors.Blue), "The click coordinate commits once even after the pointer moves.");
                    Check(Hits(target) == 0, "The external app receives no selection events.");
                    Paint(target, 0x00ff00);
                    ClickControl(pick);
                    break;
                case 8 when preview?.Visible == true:
                    Move(point);
                    break;
                case 9 when picker.Color == Colors.Green:
                    var cancelAt = point + new Vector2i(70, 20);
                    Check(WindowFromPoint(new(cancelAt)) == target, "The cancel click is outside the preview, over the foreign window.");
                    Click(cancelAt, 0x0008, 0x0010);
                    break;
                case 10 when preview?.Visible == false:
                    Check(picker.Color == Colors.Blue && changes == 1 && Hits(target) == 0, "Right-click cancels without publishing or forwarding the click.");
                    ClickControl(pick);
                    break;
                case 11 when preview?.Visible == true:
                    Move(point);
                    break;
                case 12 when picker.Color == Colors.Green:
                    keybd_event(0x1b, 0, 0, 0);
                    keybd_event(0x1b, 0, 2, 0);
                    break;
                case 13 when preview?.Visible == false:
                    Check(picker.Color == Colors.Blue && changes == 1 && Hits(target) == 0, "Escape restores without publishing.");
                    ClickControl(pick);
                    break;
                case 14 when preview?.Visible == true:
                    Move(point);
                    break;
                case 15 when picker.Color == Colors.Green:
                    quitWithSampler = true;
                    RenderingServer.FramePostDraw -= Drawn;
                    root.Tree!.Quit();
                    break;
                default:
                    return;
            }
            phase++;
        }

        Check(Engine.Run(root) == 0 && quitWithSampler, "The scene retires while sampling.");
        Click(point, 0x0002, 0x0004);
        Check(SpinWait.SpinUntil(() => Hits(target) == 2, TimeSpan.FromSeconds(2)), "The actor receives a complete click after teardown.");
        Console.WriteLine($"Windows {backend}: live foreign pixels, capture-excluded magnifier, click commit, right/Escape cancel and hook retirement passed.");
    }

    private static bool PreviewMatches(Window preview, Color color)
    {
        using var image = preview.GetTexture().GetImage();
        return image is { Width: >= 55, Height: >= 72 } &&
            image.GetPixel(8, 8) == Colors.Red && image.GetPixel(42, 8) == color && image.GetPixel(25, 60) == color;
    }

    private static void ClickControl(Control control) => Click(ControlPoint(control), 0x0002, 0x0004);

    private static Vector2i ControlPoint(Control control)
    {
        var window = control.GetWindow()!;
        var native = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, window.GetWindowID());
        var point = ScreenPoint(native, (Vector2i)(control.GetGlobalTransformWithCanvas() * (control.Size / 2)));
        var hit = WindowFromPoint(new(point));
        Check(hit == native, $"Expected {control.Name} at {point} in HWND {native}, got {hit}. Window geometry {window.Position}/{window.Size}, main HWND {DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle)}.");
        return point;
    }

    private static Vector2i ScreenPoint(nint window, Vector2i point)
    {
        var native = new WindowsPoint(point);
        Check(ClientToScreen(window, ref native) != 0, "Convert fixture coordinates through the actual native client.");
        return new(native.X, native.Y);
    }

    private static void Move(Vector2i point) => Check(SetCursorPos(point.X, point.Y) != 0, "Move the test pointer.");

    private static unsafe void Click(Vector2i point, uint down, uint up)
    {
        Move(point);
        MouseInputEvent* input = stackalloc MouseInputEvent[2];
        input[0] = new() { Mouse = new() { Flags = down } };
        input[1] = new() { Mouse = new() { Flags = up } };
        Check(SendInput(2, input, sizeof(MouseInputEvent)) == 2, "Inject the physical-style click.");
    }

    private static void Paint(nint window, nuint color) => Check(ActorMessage(window, 0x8001, color) == 1, "The actor changes its native paint.");
    private static nuint Hits(nint window) => ActorMessage(window, 0x8002, 0);
    private static nuint ActorMessage(nint window, uint message, nuint data)
    {
        Check(SendMessageTimeoutW(window, message, data, 0, 2, 1000, out var result) != 0, "The independent actor responds.");
        return result;
    }

    private const string User32 = "user32.dll";

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowsPoint(Vector2i point) { internal int X = point.X, Y = point.Y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput { internal int X, Y; internal uint Data, Flags, Time; internal nuint Extra; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInputEvent { internal uint Type; internal MouseInput Mouse; }

    [LibraryImport(User32)] private static partial int GetCursorPos(out WindowsPoint point);
    [LibraryImport(User32)] private static partial int SetCursorPos(int x, int y);
    [LibraryImport(User32)] private static partial int ClientToScreen(nint window, ref WindowsPoint point);
    [LibraryImport(User32)] private static partial nint WindowFromPoint(WindowsPoint point);
    [LibraryImport(User32)] private static partial uint GetWindowThreadProcessId(nint window, out uint pid);
    [LibraryImport(User32)] private static partial nint SendMessageTimeoutW(nint window, uint message, nuint wparam, nint lparam, uint flags, uint timeout, out nuint result);
    [LibraryImport(User32)] private static partial int PostMessageW(nint window, uint message, nuint wparam, nint lparam);
    [LibraryImport(User32)] private static unsafe partial uint SendInput(uint count, MouseInputEvent* input, int size);
    [LibraryImport(User32)] private static partial void keybd_event(byte key, byte scan, uint flags, nuint extra);
}
