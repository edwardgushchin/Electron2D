using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;
using Path = System.IO.Path;

internal static partial class NativeWindowIconTests
{
    internal static void Run(string profile)
    {
        if (profile == "unsupported")
        {
            using var display = DisplayServer.Open("Unsupported native icon", new(96, 96), hidden: true);
            Check(!DisplayServer.HasFeature(DisplayServer.Feature.NativeIcon), "Unsupported driver reports its capability.");
            Reject<NotSupportedException>(() => DisplayServer.SetNativeIcon("unread.ico"));
            Console.WriteLine("PASS unsupported native icon guard.");
            return;
        }

        var folder = Directory.CreateTempSubdirectory("electron2d-native-icon-");
        try
        {
            var first = Path.Combine(folder.FullName, "native.ico");
            var second = Path.Combine(folder.FullName, "replacement.ico");
            var bad = Path.Combine(folder.FullName, "bad.ico");
            var badPayload = Path.Combine(folder.FullName, "bad-image.ico");
            File.WriteAllBytes(first, MakeICO(0x00ff0000, 0x0000ff00));
            File.WriteAllBytes(second, MakeICO(0x000000ff, 0x00ffff00));
            var malformed = MakeICO(0, 0);
            File.WriteAllBytes(bad, malformed[..^1]);
            Array.Clear(malformed, 38, malformed.Length - 38);
            File.WriteAllBytes(badPayload, malformed);
            ProjectSettings.Set(ProjectSettings.WindowsNativeIcon, bad);
            ProjectSettings.SetFeatureOverride(ProjectSettings.WindowsNativeIcon, "windows", first);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            using var window = new Window { Title = "Native icon contract", Size = new(300, 160), Visible = false };
            var child = new Window { Title = "Native child", Size = new(100, 100) };
            window.AddChild(child);
            var scaleObserved = false;
            window.Ready += _ =>
            {
                Check(DisplayServer.HasFeature(DisplayServer.Feature.NativeIcon), "Win32 display supports native icons.");
                var rootHandle = GetWindow(window);
                var childHandle = GetWindow(child);
                Verify(rootHandle, 0x0000ff, 0x00ff00);
                Verify(childHandle, 0x0000ff, 0x00ff00);
                var original = GetIcons(rootHandle);
                Reject<ArgumentNullException>(() => DisplayServer.SetNativeIcon(null!));
                Reject<FileNotFoundException>(() => DisplayServer.SetNativeIcon(Path.Combine(folder.FullName, "missing.ico")));
                Reject<InvalidDataException>(() => DisplayServer.SetNativeIcon(bad));
                Reject<InvalidDataException>(() => DisplayServer.SetNativeIcon(badPayload));
                Task.Run(() => Reject<InvalidOperationException>(() => DisplayServer.SetNativeIcon(second))).GetAwaiter().GetResult();
                Check(GetIcons(rootHandle) == original, "Bad input preserves the committed window icons.");
                using var explicitIcon = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
                explicitIcon.Fill(Colors.Magenta);
                DisplayServer.WindowSetIcon(explicitIcon, child.GetWindowID());
                var childOverride = GetIcons(childHandle);
                using (var pack = new PCKPacker())
                {
                    var archive = Path.Combine(folder.FullName, "icons.pck");
                    pack.PCKStart(archive);
                    pack.AddFile("res://native-window-icon/replacement.ico", second);
                    pack.Flush();
                    Check(ProjectSettings.LoadResourcePack(archive), "Mounted native ICO resource.");
                    DisplayServer.SetNativeIcon("res://native-window-icon/replacement.ico");
                }
                Verify(rootHandle, 0xff0000, 0x00ffff);
                Check(GetIcons(childHandle) == childOverride, "Native default preserves the explicit per-window image.");
                var late = new Window { Title = "Late native child", Size = new(100, 100) };
                window.AddChild(late);
                Verify(GetWindow(late), 0xff0000, 0x00ffff);
                DisplayServer.SetIcon(explicitIcon);
                var imageDefault = GetIcons(rootHandle);
                Check(imageDefault.Small != 0 && imageDefault.Small == imageDefault.Large && GetIcons(childHandle) == childOverride,
                    "Switching to an image replaces the native default and preserves the explicit child.");
                DisplayServer.SetNativeIcon(first);
                DisplayServer.WindowSetMode(WindowMode.Fullscreen);
                DisplayServer.WindowSetMode(WindowMode.Windowed);
                Verify(rootHandle, 0x0000ff, 0x00ff00);
                var current = GetIcons(rootHandle);
                var windows = SDL3.SDL.GetWindows(out int _);
                var nativeWindow = windows!.Single(candidate => SDL3.SDL.GetPointerProperty(
                    SDL3.SDL.GetWindowProperties(candidate), SDL3.SDL.Props.WindowWin32HWNDPointer, 0) == rootHandle);
                var windowID = SDL3.SDL.GetWindowID(nativeWindow);
                Check(windowID != 0, "Find the SDL identity of the native root.");
                var scaleEvent = new SDL3.SDL.Event { Window = new() { Type = SDL3.SDL.EventType.WindowDisplayScaleChanged, WindowID = windowID } };
                window.DpiChanged += () =>
                {
                    Check(GetIcons(rootHandle) != current, "A scale notification reselects and replaces owned icons before delivery.");
                    Verify(rootHandle, 0x0000ff, 0x00ff00);
                    scaleObserved = true;
                };
                Check(SDL3.SDL.PushEvent(ref scaleEvent), "Queue the scale notification for the live window.");
            };
            Check(Engine.Run((_, frame) =>
            {
                Check(frame < 120, "The owner pump delivers the queued scale notification.");
                if (scaleObserved) window.Tree!.Quit();
            }, window) == 0 && scaleObserved && !DisplayServer.IsAvailable, "Display retirement completed.");
            Console.WriteLine("PASS native ICO startup, Win32 pixels, mounted replacement, window overrides and mode transitions.");
        }
        finally { folder.Delete(recursive: true); }
    }

    private static nint GetWindow(Window window) =>
        DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, window.GetWindowID());

    private static (nint Small, nint Large) GetIcons(nint window) =>
        (SendMessageW(window, 0x007f, 0, 0), SendMessageW(window, 0x007f, 1, 0));

    private static void Verify(nint window, uint smallColor, uint largeColor)
    {
        var icons = GetIcons(window);
        Check(icons.Small != 0 && icons.Large != 0 && icons.Small != icons.Large, "Win32 stores two independent icon sizes.");
        Check(ReadIconPixel(icons.Small) == smallColor && ReadIconPixel(icons.Large) == largeColor, "Each Win32 icon draws the selected source image.");
    }

    private static uint ReadIconPixel(nint icon)
    {
        var dc = CreateCompatibleDC(0);
        var display = GetDC(0);
        var bitmap = CreateCompatibleBitmap(display, 32, 32);
        ReleaseDC(0, display);
        var previous = SelectObject(dc, bitmap);
        try
        {
            Check(DrawIconEx(dc, 0, 0, icon, 32, 32, 0, 0, 3) != 0, "GDI draws the stored native icon.");
            return GetPixel(dc, 16, 16);
        }
        finally
        {
            SelectObject(dc, previous);
            DeleteObject(bitmap);
            DeleteDC(dc);
        }
    }

    private static byte[] MakeICO(uint small, uint large)
    {
        const int directorySize = 38;
        static int ImageSize(int size) => 40 + size * size * 4 + size * 4;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(0x00010000);
        writer.Write((ushort)2);
        var offset = directorySize;
        foreach (var size in new[] { 16, 32 })
        {
            writer.Write((byte)size); writer.Write((byte)size); writer.Write((ushort)0);
            writer.Write((ushort)1); writer.Write((ushort)32);
            writer.Write(ImageSize(size)); writer.Write(offset);
            offset += ImageSize(size);
        }
        WriteImage(16, small);
        WriteImage(32, large);
        return stream.ToArray();

        void WriteImage(int size, uint color)
        {
            writer.Write(40); writer.Write(size); writer.Write(size * 2);
            writer.Write((ushort)1); writer.Write((ushort)32);
            writer.Write(new byte[24]);
            for (var i = 0; i < size * size; i++) writer.Write(0xff000000 | color);
            writer.Write(new byte[size * 4]);
        }
    }

    [LibraryImport("user32.dll")] private static partial nint SendMessageW(nint window, uint message, nuint wParam, nint lParam);
    [LibraryImport("user32.dll")] private static partial nint GetDC(nint window);
    [LibraryImport("user32.dll")] private static partial int ReleaseDC(nint window, nint dc);
    [LibraryImport("user32.dll")] private static partial int DrawIconEx(nint dc, int x, int y, nint icon, int width, int height, uint step, nint brush, uint flags);
    [LibraryImport("gdi32.dll")] private static partial nint CreateCompatibleDC(nint dc);
    [LibraryImport("gdi32.dll")] private static partial nint CreateCompatibleBitmap(nint dc, int width, int height);
    [LibraryImport("gdi32.dll")] private static partial nint SelectObject(nint dc, nint obj);
    [LibraryImport("gdi32.dll")] private static partial int DeleteObject(nint obj);
    [LibraryImport("gdi32.dll")] private static partial int DeleteDC(nint dc);
    [LibraryImport("gdi32.dll")] private static partial uint GetPixel(nint dc, int x, int y);
}
