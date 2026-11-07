using System.Diagnostics;
using System.Runtime.InteropServices;
using Electron2D;

internal static partial class DesktopScreenCaptureTests
{
    internal static void Run(string profile, string[] args)
    {
        Reject<InvalidOperationException>(() => DisplayServer.ScreenGetImage());
        if (profile == "unavailable")
        {
            using var display = DisplayServer.Open("Unavailable screen capture", new(64, 64), hidden: true);
            Check(!DisplayServer.HasFeature(DisplayServer.Feature.ScreenCapture), "The backend does not expose desktop capture.");
            Reject<NotSupportedException>(() => DisplayServer.ScreenGetImage());
            Reject<NotSupportedException>(() => DisplayServer.ScreenGetImageRect(new(0, 0, 1, 1)));
            Reject<NotSupportedException>(() => DisplayServer.ScreenGetPixel(Vector2i.Zero));
            Console.WriteLine("Unsupported desktop capture passed.");
            return;
        }

        var windows = profile == "windows";
        nint actor = 0;
        if (windows)
            Check(OperatingSystem.IsWindows() && args.Length == 1, "Provide the independent WGC monitor reader on the Windows desktop.");
        else
        {
            var fixture = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_X11_PRIVATE_DISPLAY");
            Check(profile == "x11" && !string.IsNullOrEmpty(fixture) && fixture == Environment.GetEnvironmentVariable("DISPLAY") &&
                Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "x11", "Use a private X11 test display.");
            actor = XOpenDisplay(0);
            Check(actor != 0, "An independent Xlib reader connects to the fixture.");
        }

        try { Scene(windows, windows ? args[0] : null, actor); }
        finally { if (actor != 0) XCloseDisplay(actor); }
    }

    private static void Scene(bool windows, string? monitor, nint actor)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 30;
        var window = new Window { Size = new(480, 320), Position = new(360, 260), Title = "Electron2D desktop capture", Borderless = true, AlwaysOnTop = true };
        window.AddChild(new ColorRect { Size = new(480, 320), Color = Colors.Blue });
        window.AddChild(new ColorRect { Position = new(68, 66), Size = new(104, 60), Color = Colors.Red });
        window.AddChild(new ColorRect { Position = new(208, 66), Size = new(104, 60), Color = Colors.Green });
        var pulse = new ColorRect { Position = new(440, 280), Size = new(10, 10), Color = Colors.Black };
        window.AddChild(pulse);
        var overlay = new Window { Size = new(84, 56), Borderless = true, AlwaysOnTop = true, Visible = false };
        overlay.AddChild(new ColorRect { Size = new(84, 56), Color = Colors.Yellow });
        window.AddChild(overlay);

        Process? oracle = null;
        Task<string>? oracleOutput = null;
        Image? retained = null;
        var frames = 0;
        var phase = 0;
        var deadline = Stopwatch.StartNew();
        var origin = Vector2i.Zero;
        var red = new Vector2i(120, 96);
        var green = new Vector2i(260, 96);
        var blue = new Vector2i(120, 205);
        window.Ready += _ => RenderingServer.FramePostDraw += Drawn;

        void Drawn()
        {
            Check(deadline.Elapsed < TimeSpan.FromSeconds(20), $"Desktop capture did not reach the expected pixels in phase {phase}.");
            frames++;
            pulse.Color = (frames & 2) == 0 ? Colors.White : Colors.Black;
            if (frames < 10)
                return;
            Check(DisplayServer.HasFeature(DisplayServer.Feature.ScreenCapture), "The display reports native screen capture.");
            if (phase == 0)
            {
                origin = DisplayServer.WindowGetPosition();
                if (monitor is not null)
                {
                    if (oracle is null)
                    {
                        var handle = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
                        var start = new ProcessStartInfo(monitor) { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                        start.ArgumentList.Add("0x" + handle.ToString("X"));
                        oracle = Process.Start(start) ?? throw new InvalidOperationException("Could not launch the WGC monitor reader.");
                        oracleOutput = oracle.StandardOutput.ReadToEndAsync();
                        return;
                    }
                    if (!oracle.HasExited || oracleOutput?.IsCompletedSuccessfully != true)
                        return;
                    Console.Write(oracleOutput.Result);
                    Check(oracle.ExitCode == 0, "The independent monitor frame contains the rendered fixture.");
                }
                else if (X11Pixel(actor, origin + red) != Colors.Red)
                    return;

                Check(DisplayServer.ScreenGetPixel(origin + red) == Colors.Red &&
                      DisplayServer.ScreenGetPixel(origin + green) == Colors.Green &&
                      DisplayServer.ScreenGetPixel(origin + blue) == Colors.Blue, "Native desktop pixel channels match the rendered markers.");
                foreach (var selector in new[] { 0, DisplayServer.ScreenPrimary, DisplayServer.ScreenOfMainWindow,
                    DisplayServer.ScreenWithMouseFocus, DisplayServer.ScreenWithKeyboardFocus })
                {
                    using var screen = DisplayServer.ScreenGetImage(selector);
                    var screenPosition = DisplayServer.ScreenGetPosition(selector);
                    Check(screen.Size == DisplayServer.ScreenGetSize(selector) && screen.PixelFormat == Image.Format.Rgba8 && !screen.HasMIPMaps,
                        "Full screen capture matches the selected output.");
                    var local = origin + red - screenPosition;
                    if ((uint)local.X < (uint)screen.Size.X && (uint)local.Y < (uint)screen.Size.Y)
                        Check(screen.GetPixel(local.X, local.Y) == Colors.Red, "Full screen contains the expected desktop pixel.");
                }

                var crop = new Rect2i(origin + new Vector2i(60, 58), new(121, 77));
                retained = DisplayServer.ScreenGetImageRect(crop);
                Check(retained.Size == crop.Size, "The rectangle keeps its requested dimensions.");
                var data = retained.GetData();
                for (var y = 0; y < crop.Size.Y; y++)
                {
                    for (var x = 0; x < crop.Size.X; x++)
                    {
                        var marker = x >= 8 && x < 112 && y >= 8 && y < 68;
                        var i = (y * crop.Size.X + x) * 4;
                        Check(data[i] == (marker ? 255 : 0) && data[i + 1] == 0 && data[i + 2] == (marker ? 0 : 255) && data[i + 3] == 255,
                            "The captured rectangle preserves scanline pitch, orientation, channels and opaque alpha.");
                    }
                }
                Reject<ArgumentOutOfRangeException>(() => DisplayServer.ScreenGetImage(int.MaxValue));
                Reject<ArgumentOutOfRangeException>(() => DisplayServer.ScreenGetImageRect(default));
                Reject<ArgumentOutOfRangeException>(() => DisplayServer.ScreenGetImageRect(new(int.MaxValue, 0, 8, 8)));
                Reject<ArgumentOutOfRangeException>(() => DisplayServer.ScreenGetPixel(new(int.MinValue, int.MinValue)));
                Reject<InvalidOperationException>(() => Task.Run(() => DisplayServer.ScreenGetPixel(origin + red)).GetAwaiter().GetResult());
                var objects = windows ? GetGuiResources(GetCurrentProcess(), 0) : 0;
                for (var i = 0; i < 8; i++)
                {
                    using var next = DisplayServer.ScreenGetImageRect(crop);
                    Check(next.GetPixel(0, 0) == Colors.Blue, "Repeated captures preserve fresh desktop pixels.");
                }
                if (windows)
                    Check(objects > 0 && GetGuiResources(GetCurrentProcess(), 0) == objects, "Each capture releases its GDI objects.");

                retained.SetPixel(0, 0, Colors.Yellow);
                Check(DisplayServer.ScreenGetPixel(crop.Position) == Colors.Blue, "Caller edits cannot affect the desktop.");
                overlay.Position = origin + new Vector2i(80, 78);
                overlay.Show();
                phase++;
                return;
            }

            var pixel = DisplayServer.ScreenGetPixel(origin + red);
            if (phase == 1 && pixel == Colors.Yellow)
            {
                using var captured = DisplayServer.ScreenGetImageRect(new(origin + red, Vector2i.One));
                Check(captured.GetPixel(0, 0) == Colors.Yellow, "Desktop rectangle sees the native overlay over the rendered window.");
                if (windows)
                {
                    overlay.ExcludeFromCapture = true;
                    phase++;
                    return;
                }
                overlay.Hide();
                phase = 3;
            }
            else if (phase == 2 && pixel == Colors.Red)
            {
                using var captured = DisplayServer.ScreenGetImageRect(new(origin + red, Vector2i.One));
                Check(captured.GetPixel(0, 0) == Colors.Red, "The desktop compositor omits the excluded overlay.");
                overlay.ExcludeFromCapture = false;
                phase = 4;
            }
            else if (phase == 4 && pixel == Colors.Yellow)
            {
                overlay.Hide();
                phase = 3;
            }
            else if (phase == 3 && pixel == Colors.Red)
            {
                RenderingServer.FramePostDraw -= Drawn;
                window.Tree!.Quit();
                phase = 5;
            }
        }

        try
        {
            Check(Engine.Run(window) == 0 && phase == 5, "The desktop capture scene retires after seeing the restored output.");
            Check(retained?.GetPixel(0, 0) == Colors.Yellow && retained.GetPixel(8, 8) == Colors.Red,
                "Caller-owned capture remains readable after display shutdown.");
            Reject<InvalidOperationException>(() => DisplayServer.ScreenGetPixel(Vector2i.Zero));
            Console.WriteLine($"Desktop screen, rectangle and pixel capture passed ({(windows ? "Windows" : "X11")}).");
        }
        finally
        {
            if (oracle is { HasExited: false }) oracle.Kill();
            oracle?.Dispose();
            retained?.Dispose();
        }
    }

    private static Color X11Pixel(nint display, Vector2i position)
    {
        var root = XDefaultRootWindow(display);
        var image = XGetImage(display, root, position.X, position.Y, 1, 1, nuint.MaxValue, 2);
        Check(image != 0, "The independent Xlib client reads the fixture.");
        try
        {
            var color = new XColor { Pixel = XGetPixel(image, 0, 0) };
            Check(XQueryColor(display, XDefaultColormap(display, XDefaultScreen(display)), ref color) != 0, "The X server resolves the pixel's color.");
            return new Color(color.Red / 65535f, color.Green / 65535f, color.Blue / 65535f);
        }
        finally { XDestroyImage(image); }
    }

    private static void Check(bool passed, string message)
    {
        if (!passed) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XColor
    {
        internal nuint Pixel;
        internal ushort Red;
        internal ushort Green;
        internal ushort Blue;
        internal byte Flags;
        internal byte Pad;
    }

    [LibraryImport("user32.dll")] private static partial uint GetGuiResources(nint process, uint flags);
    [LibraryImport("kernel32.dll")] private static partial nint GetCurrentProcess();
    [LibraryImport("libX11.so.6")] private static partial nint XOpenDisplay(nint name);
    [LibraryImport("libX11.so.6")] private static partial int XCloseDisplay(nint display);
    [LibraryImport("libX11.so.6")] private static partial int XDefaultScreen(nint display);
    [LibraryImport("libX11.so.6")] private static partial nuint XDefaultRootWindow(nint display);
    [LibraryImport("libX11.so.6")] private static partial nuint XDefaultColormap(nint display, int screen);
    [LibraryImport("libX11.so.6")]
    private static partial nint XGetImage(nint display, nuint window, int x, int y, uint width, uint height, nuint planeMask, int format);
    [LibraryImport("libX11.so.6")] private static partial nuint XGetPixel(nint image, int x, int y);
    [LibraryImport("libX11.so.6")] private static partial int XDestroyImage(nint image);
    [LibraryImport("libX11.so.6")] private static partial int XQueryColor(nint display, nuint colormap, ref XColor color);
}
