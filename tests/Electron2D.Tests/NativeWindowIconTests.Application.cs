using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;
using Path = System.IO.Path;

internal static partial class NativeWindowIconTests
{
    private static void RunApplicationIcon(string profile)
    {
        Check(ProjectSettings.Get(ProjectSettings.ApplicationIcon) == "" &&
            ProjectSettings.IsBasic(ProjectSettings.ApplicationIcon), "The built-in image icon setting defaults to empty.");
        var folder = Directory.CreateTempSubdirectory("electron2d-application-icon-");
        try
        {
            var packPath = Path.Combine(folder.FullName, "application.pck");
            using (var icon = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8))
            using (var pack = new PCKPacker())
            {
                icon.Fill(new Color(0, 1, 1, 1));
                pack.PCKStart(packPath);
                pack.AddFileFromBuffer("res://application-icon/icon.png", icon.SavePNGToBuffer());
                pack.Flush();
            }
            Check(ProjectSettings.LoadResourcePack(packPath), "Mount a pack with a startup image.");
            var requested = profile switch { "image" => "windows", "x11-image" => "x11", "wayland-image" => "wayland", _ => "dummy" };
            ProjectSettings.Set(ProjectSettings.ApplicationIcon, "res://application-icon/bad.png");
            ProjectSettings.SetFeatureOverride(ProjectSettings.ApplicationIcon, "template", "res://application-icon/icon.png");
            ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            var supported = false;
            RunWindow((window, driver) =>
            {
                Check(driver == requested, "The requested video driver is active.");
                supported = DisplayServer.HasFeature(DisplayServer.Feature.Icon);
                if (driver is "windows" or "x11")
                {
                    Check(supported, "The desktop image icon is supported.");
                    VerifyImage(window, driver);
                    var child = new Window { Title = "Application icon child", Size = new(100, 100) };
                    window.AddChild(child);
                    VerifyImage(child, driver);
                }
                else if (driver == "dummy")
                    Check(!supported, "The dummy backend does not advertise image icons.");
            });
            Check(ProjectSettings.ClearFeatureOverride(ProjectSettings.ApplicationIcon, "template"), "Clear the effective image path.");
            RunWindow((window, driver) =>
            {
                if (driver == "windows")
                    Check(GetIcons(GetWindow(window)) == default, "A missing image does not reuse icons from the previous display.");
                if (driver == "x11")
                    Check(ReadX11Icon(window).Length == 0, "A missing image leaves the fresh window icon unset.");
            });
            Console.WriteLine($"PASS application image icon startup on {requested}, capability={supported}, fresh display and missing image.");
        }
        finally { folder.Delete(recursive: true); }
    }

    private static void RunWindow(Action<Window, string> ready)
    {
        using var window = new Window { Title = "Application icon contract", Size = new(160, 100), Visible = false };
        var completed = false;
        window.Ready += _ =>
        {
            ready(window, SDL3.SDL.GetCurrentVideoDriver()!);
            completed = true;
            window.Tree!.Quit();
        };
        Check(Engine.Run(window) == 0 && completed && !DisplayServer.IsAvailable, "Startup and display retirement completed.");
    }

    private static void VerifyImage(Window window, string driver)
    {
        if (driver == "windows")
        {
            var icon = GetIcons(GetWindow(window));
            var pixel = icon.Small == 0 ? uint.MaxValue : ReadIconPixel(icon.Small, 16);
            var cyan = (pixel & 0xff) <= 3 && ((pixel >> 8) & 0xff) >= 252 && ((pixel >> 16) & 0xff) >= 252;
            Check(icon.Small != 0 && icon.Small == icon.Large && cyan,
                $"The configured PNG reaches Win32 as cyan pixels (small={icon.Small}, large={icon.Large}, pixel={pixel:x8}).");
            return;
        }
        var data = ReadX11Icon(window);
        Check(data.Length == 258 && data[0] == 16 && data[1] == 16 && unchecked((uint)data[2]) == 0xff00ffff,
            $"The configured PNG reaches X11 as a 16-pixel cyan ARGB icon ({data.Length} words, {string.Join(", ", data.Take(3).Select(value => value.ToString("x")))}).");
    }

    private static unsafe nuint[] ReadX11Icon(Window window)
    {
        var display = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.DisplayHandle);
        var property = XInternAtom(display, "_NET_WM_ICON", 0);
        XSync(display, 0);
        var status = XGetWindowProperty(display, (nuint)GetWindow(window), property, 0, 4096, 0, 6,
            out _, out var format, out var count, out var remaining, out var data);
        try
        {
            Check(status == 0 && remaining == 0 && (count == 0 || format == 32), "Read the X11 icon property.");
            return new ReadOnlySpan<nuint>((void*)data, checked((int)count)).ToArray();
        }
        finally { if (data != 0) XFree(data); }
    }

    [LibraryImport("libX11.so.6", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nuint XInternAtom(nint display, string name, int onlyIfExists);
    [LibraryImport("libX11.so.6")]
    private static partial int XSync(nint display, int discard);
    [LibraryImport("libX11.so.6")]
    private static partial int XGetWindowProperty(nint display, nuint window, nuint property, nint offset, nint length,
        int delete, nuint type, out nuint actualType, out int format, out nuint count, out nuint remaining, out nint data);
    [LibraryImport("libX11.so.6")]
    private static partial int XFree(nint data);
}
