using System.Runtime.InteropServices;

namespace Electron2D;

internal static unsafe partial class WindowsScreenCapture
{
    private const string User32 = "user32.dll";
    private const string GDI32 = "gdi32.dll";
    private const uint CopyDesktop = 0x40cc0020; // SRCCOPY | CAPTUREBLT

    internal static Image Capture(Rect2i rectangle)
    {
        Validate(rectangle, nameof(rectangle));
        var width = rectangle.Size.X;
        var height = rectangle.Size.Y;
        var bytes = GC.AllocateUninitializedArray<byte>(Image.GetRequiredDataSize(width, height, Image.Format.Rgba8, mipmaps: false));
        Read(rectangle, bytes);
        return Image.FromImportedState(new Image.State(width, height, Image.Format.Rgba8, false, bytes));
    }

    internal static Color Pixel(Vector2i position)
    {
        var rectangle = new Rect2i(position, Vector2i.One);
        Validate(rectangle, nameof(position));
        Span<byte> bytes = stackalloc byte[4];
        Read(rectangle, bytes);
        return new Color(bytes[0] / 255f, bytes[1] / 255f, bytes[2] / 255f);
    }

    private static void Validate(Rect2i rectangle, string argument)
    {
        const int VirtualLeft = 76, VirtualTop = 77, VirtualWidth = 78, VirtualHeight = 79;
        var left = GetSystemMetrics(VirtualLeft);
        var top = GetSystemMetrics(VirtualTop);
        var width = GetSystemMetrics(VirtualWidth);
        var height = GetSystemMetrics(VirtualHeight);
        if (width <= 0 || height <= 0)
            throw new IOException("Windows did not report desktop bounds.");
        if (rectangle.Size.X <= 0 || rectangle.Size.Y <= 0 || rectangle.Position.X < left || rectangle.Position.Y < top ||
            (long)rectangle.Position.X + rectangle.Size.X > (long)left + width ||
            (long)rectangle.Position.Y + rectangle.Size.Y > (long)top + height)
            throw new ArgumentOutOfRangeException(argument, "The capture rectangle must fit inside the Windows desktop.");
    }

    private static void Read(Rect2i rectangle, Span<byte> target)
    {
        var screen = GetDC(0);
        if (screen == 0)
            throw new IOException("Windows could not acquire the desktop device context.");
        nint memory = 0, bitmap = 0, previous = 0;
        try
        {
            memory = CreateCompatibleDC(screen);
            if (memory == 0)
                throw new IOException("Windows could not allocate a capture device context.");

            var info = new BitmapHeader
            {
                Size = (uint)sizeof(BitmapHeader),
                Width = rectangle.Size.X,
                Height = -rectangle.Size.Y,
                Planes = 1,
                BitCount = 32,
            };
            bitmap = CreateDIBSection(screen, in info, 0, out var pixels, 0, 0);
            if (bitmap == 0 || pixels == null)
                throw new IOException("Windows could not allocate a desktop capture bitmap.");
            previous = SelectObject(memory, bitmap);
            if (previous == 0 || previous == -1)
                throw new IOException("Windows could not select the desktop capture bitmap.");
            if (BitBlt(memory, 0, 0, rectangle.Size.X, rectangle.Size.Y, screen,
                rectangle.Position.X, rectangle.Position.Y, CopyDesktop) == 0 || GdiFlush() == 0)
                throw new IOException("Windows could not copy the desktop pixels.");

            for (var i = 0; i < target.Length; i += 4)
            {
                target[i] = pixels[i + 2];
                target[i + 1] = pixels[i + 1];
                target[i + 2] = pixels[i];
                target[i + 3] = 255;
            }
        }
        finally
        {
            if (previous != 0 && previous != -1) SelectObject(memory, previous);
            if (memory != 0) DeleteDC(memory);
            if (bitmap != 0) DeleteObject(bitmap);
            ReleaseDC(0, screen);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapHeader
    {
        internal uint Size;
        internal int Width;
        internal int Height;
        internal ushort Planes;
        internal ushort BitCount;
        internal uint Compression;
        internal uint ImageSize;
        internal int XPixelsPerMeter;
        internal int YPixelsPerMeter;
        internal uint ColorsUsed;
        internal uint ColorsImportant;
    }

    [LibraryImport(User32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint GetDC(nint window);
    [LibraryImport(User32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int ReleaseDC(nint window, nint dc);
    [LibraryImport(User32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int GetSystemMetrics(int metric);
    [LibraryImport(GDI32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint CreateCompatibleDC(nint dc);
    [LibraryImport(GDI32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint CreateDIBSection(nint dc, in BitmapHeader info, uint usage, out byte* bits, nint section, uint offset);
    [LibraryImport(GDI32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint SelectObject(nint dc, nint value);
    [LibraryImport(GDI32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int BitBlt(nint target, int x, int y, int width, int height,
        nint source, int sourceX, int sourceY, uint operation);
    [LibraryImport(GDI32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int GdiFlush();
    [LibraryImport(GDI32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int DeleteDC(nint dc);
    [LibraryImport(GDI32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int DeleteObject(nint value);
}
