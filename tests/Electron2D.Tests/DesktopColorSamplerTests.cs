using System.Diagnostics;
using System.Runtime.InteropServices;
using Electron2D;

internal static partial class DesktopColorSamplerTests
{
    internal static void Run()
    {
        var fixture = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_X11_PRIVATE_DISPLAY");
        Check(!string.IsNullOrEmpty(fixture) && fixture == Environment.GetEnvironmentVariable("DISPLAY") &&
            Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "x11", "Use a private X11 test display.");

        var client = XOpenDisplay(0);
        Check(client != 0, "The independent X11 client connects.");
        try { Scene(client); }
        finally { XCloseDisplay(client); }
    }

    private static void Scene(nint client)
    {
        const int buttonEvents = (1 << 2) | (1 << 3);
        var desktop = XDefaultRootWindow(client);
        var map = XDefaultColormap(client, XDefaultScreen(client));
        nuint Pixel(string name)
        {
            Check(XAllocNamedColor(client, map, name, out var color, out _) != 0, $"Allocate X11 fixture color {name}.");
            return color.Pixel;
        }

        var green = Pixel("green");
        var blue = Pixel("blue");
        var red = Pixel("red");
        var external = XCreateSimpleWindow(client, desktop, 780, 100, 210, 180, 0, 0, green);
        XSelectInput(client, external, buttonEvents);
        XMapRaised(client, external);
        XSync(client, 0);

        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 30;
        var root = new Window { Title = "Desktop sampler test", Position = new(32, 42), Size = new(640, 640), Borderless = true };
        var button = new ColorPickerButton { Position = new(95, 65), Size = new(90, 34), Color = Colors.Red };
        root.AddChild(button);
        var picker = button.GetPicker();
        var changes = 0;
        var pickControl = (Control)ColorPickerTests.Find(picker, "Pick");
        button.ColorChanged += _ => changes++;
        var deadline = Stopwatch.StartNew();
        var stage = 0;
        var activeAt = new Vector2i(846, 153);

        void Motion(Vector2i point) =>
            Check(XTestFakeMotionEvent(client, -1, point.X, point.Y, 0) != 0, "XTEST moves the pointer.");
        void Click(Vector2i point, uint index = 1)
        {
            Motion(point);
            Check(XTestFakeButtonEvent(client, index, 1, 0) != 0 && XTestFakeButtonEvent(client, index, 0, 0) != 0,
                "XTEST sends a complete click.");
            XSync(client, 0);
        }
        Vector2i Center(Control control)
        {
            var window = control.GetWindow()!;
            var xid = (nuint)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, window.GetWindowID());
            var point = (Vector2i)(control.GetGlobalTransformWithCanvas() * (control.Size / 2));
            Check(XTranslateCoordinates(client, xid, desktop, point.X, point.Y, out var x, out var y, out _) != 0, "Translate the fixture click through the native client.");
            return new(x, y);
        }
        void Paint(nuint color)
        {
            XSetWindowBackground(client, external, color);
            XClearWindow(client, external);
            XSync(client, 0);
        }
        unsafe void CheckReleased()
        {
            var input = stackalloc nint[24];
            Check(XCheckWindowEvent(client, external, buttonEvents, (nint)input) == 0, "No sampling click reaches the external window.");
            Check(XGrabPointer(client, desktop, 0, buttonEvents, 1, 1, 0, 0, 0) == 0, "The modal pointer grab was released.");
            XUngrabPointer(client, 0);
            XSync(client, 0);
        }

        root.Ready += _ => RenderingServer.FramePostDraw += Drawn;
        void Drawn()
        {
            Check(deadline.Elapsed < TimeSpan.FromSeconds(18), $"Desktop color sampler stalled at stage {stage}, color {picker.Color}, pointer {DisplayServer.MouseGetPosition()}, error {(picker.GetNodeOrNull("_color_error") as AcceptDialog)?.DialogText}.");
            switch (stage)
            {
                case 0 when DisplayServer.ScreenGetPixel(activeAt) == Colors.Green:
                    Click(Center(button));
                    break;
                case 1 when button.GetPopup().Visible:
                    Check(pickControl.TooltipText == "Pick a color from the desktop.", "The picker advertises the active desktop route.");
                    Click(Center(pickControl));
                    break;
                case 2 when picker.GetNodeOrNull("_desktop_color_sampler") is Window { Visible: true }:
                    Motion(activeAt);
                    XSync(client, 0);
                    break;
                case 3 when picker.Color == Colors.Green:
                    Check(button.Color == Colors.Red && changes == 0, $"A live preview does not commit the button color or publish. Button {button.Color}, events {changes}.");
                    Paint(blue);
                    break;
                case 4 when picker.Color == Colors.Blue:
                    Click(activeAt + new Vector2i(12, 12));
                    break;
                case 5 when changes == 1:
                    Check(button.Color == Colors.Blue && picker.Color == Colors.Blue && picker.GetRecentPresets().Contains(Colors.Blue),
                        "Left release commits the live desktop color and recent preset once.");
                    CheckReleased();
                    Paint(red);
                    Click(Center(pickControl));
                    break;
                case 6 when picker.GetNodeOrNull("_desktop_color_sampler") is Window { Visible: true }:
                    Motion(activeAt);
                    XSync(client, 0);
                    break;
                case 7 when picker.Color == Colors.Red:
                    Click(activeAt, 3);
                    break;
                case 8 when picker.GetNodeOrNull("_desktop_color_sampler") is Window { Visible: false }:
                    Check(button.Color == Colors.Blue && picker.Color == Colors.Blue && changes == 1, "Right cancellation restores without publishing.");
                    CheckReleased();
                    Click(Center(pickControl));
                    break;
                case 9 when picker.GetNodeOrNull("_desktop_color_sampler") is Window { Visible: true }:
                    Motion(activeAt);
                    XSync(client, 0);
                    break;
                case 10 when picker.Color == Colors.Red:
                    var escape = XKeysymToKeycode(client, 0xFF1B);
                    Check(escape != 0 && XTestFakeKeyEvent(client, escape, 1, 0) != 0 && XTestFakeKeyEvent(client, escape, 0, 0) != 0,
                        "XTEST sends Escape.");
                    XSync(client, 0);
                    break;
                case 11 when picker.GetNodeOrNull("_desktop_color_sampler") is Window { Visible: false }:
                    Check(button.Color == Colors.Blue && picker.Color == Colors.Blue && changes == 1, "Escape restores without publishing.");
                    CheckReleased();
                    Click(Center(pickControl));
                    break;
                case 12 when picker.GetNodeOrNull("_desktop_color_sampler") is Window { Visible: true }:
                    Motion(activeAt);
                    XSync(client, 0);
                    break;
                case 13 when picker.Color == Colors.Red:
                    root.Tree!.Defer(button.GetPopup().Hide);
                    break;
                case 14 when !button.GetPopup().Visible && picker.GetNodeOrNull("_desktop_color_sampler") is Window { Visible: false }:
                    Check(picker.Color == Colors.Blue && changes == 1, "Closing the picker cancels its desktop preview.");
                    CheckReleased();
                    Click(Center(button));
                    break;
                case 15 when button.GetPopup().Visible:
                    Click(Center(pickControl));
                    break;
                case 16 when picker.GetNodeOrNull("_desktop_color_sampler") is Window { Visible: true }:
                    Motion(activeAt);
                    XSync(client, 0);
                    break;
                case 17 when picker.Color == Colors.Red:
                    RenderingServer.FramePostDraw -= Drawn;
                    root.Tree!.Quit();
                    break;
                default:
                    return;
            }
            stage++;
        }

        Check(Engine.Run(root) == 0 && stage == 18, "The live X11 color sampler scene completed.");
        CheckReleased();
        Console.WriteLine("X11 desktop picker sampled live external pixels, committed once and released capture after confirmation, cancellation, owner closure and teardown.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private const string X11 = "libX11.so.6", XTest = "libXtst.so.6";

    [StructLayout(LayoutKind.Sequential)]
    private struct XColor
    {
        internal nuint Pixel;
        internal ushort Red, Green, Blue;
        internal byte Flags, Pad;
    }

    [LibraryImport(X11)] private static partial nint XOpenDisplay(nint name);
    [LibraryImport(X11)] private static partial int XCloseDisplay(nint client);
    [LibraryImport(X11)] private static partial nuint XDefaultRootWindow(nint client);
    [LibraryImport(X11)] private static partial int XDefaultScreen(nint client);
    [LibraryImport(X11)] private static partial nuint XDefaultColormap(nint client, int screen);
    [LibraryImport(X11, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int XAllocNamedColor(nint client, nuint map, string name, out XColor actual, out XColor exact);
    [LibraryImport(X11)]
    private static partial nuint XCreateSimpleWindow(nint client, nuint parent, int x, int y, uint width, uint height,
        uint borderWidth, nuint border, nuint background);
    [LibraryImport(X11)] private static partial int XSelectInput(nint client, nuint window, nint events);
    [LibraryImport(X11)] private static partial int XMapRaised(nint client, nuint window);
    [LibraryImport(X11)] private static partial int XSetWindowBackground(nint client, nuint window, nuint pixel);
    [LibraryImport(X11)] private static partial int XClearWindow(nint client, nuint window);
    [LibraryImport(X11)] private static partial int XSync(nint client, int discard);
    [LibraryImport(X11)] private static partial int XCheckWindowEvent(nint client, nuint window, nint eventMask, nint input);
    [LibraryImport(X11)] private static partial int XTranslateCoordinates(nint client, nuint source, nuint target, int x, int y, out int tx, out int ty, out nuint child);
    [LibraryImport(X11)]
    private static partial int XGrabPointer(nint client, nuint window, int ownerEvents, uint eventMask,
        int pointerMode, int keyboardMode, nuint confineTo, nuint cursor, nuint time);
    [LibraryImport(X11)] private static partial int XUngrabPointer(nint client, nuint time);
    [LibraryImport(X11)] private static partial byte XKeysymToKeycode(nint client, nuint symbol);
    [LibraryImport(XTest)] private static partial int XTestFakeMotionEvent(nint client, int screen, int x, int y, nuint delay);
    [LibraryImport(XTest)] private static partial int XTestFakeButtonEvent(nint client, uint button, int press, nuint delay);
    [LibraryImport(XTest)] private static partial int XTestFakeKeyEvent(nint client, uint key, int press, nuint delay);
}
