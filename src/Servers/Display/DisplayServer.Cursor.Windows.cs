using System.ComponentModel;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    // SDL's pinned Windows system-cursor table has no Help entry. Copy the OS artwork
    // into an SDL-owned cursor so normal focus, capture and custom-cursor lifetime still apply.
    private static unsafe nint CreateWindowsHelpCursor()
    {
        var shared = CursorLoad(0, 32651);
        if (shared == 0 || CursorInfo(shared, out var info) == 0) throw CursorWindowsFailure();
        nint dc = 0, bitmap = 0, previous = 0;
        try
        {
            if (CursorBitmapInfo(info.Color != 0 ? info.Color : info.Mask, Marshal.SizeOf<CursorBitmap>(), out var source) == 0)
                throw CursorWindowsFailure();
            var width = source.Width; var height = info.Color != 0 ? source.Height : source.Height / 2;
            if (width is <= 0 or > 256 || height is <= 0 or > 256) throw new InvalidOperationException("Invalid Windows help cursor dimensions.");
            var header = new CursorDIB { Size = 40, Width = width, Height = -height, Planes = 1, Bits = 32 };
            dc = CursorCreateDC(0);
            if (dc == 0) throw CursorWindowsFailure();
            bitmap = CursorCreateDIB(dc, ref header, 0, out var bits, 0, 0);
            if (bitmap == 0) throw CursorWindowsFailure();
            previous = CursorSelect(dc, bitmap);
            if (previous == 0 || previous == -1) throw CursorWindowsFailure();
            var pixels = new Span<uint>((void*)bits, checked(width * height));
            pixels.Clear();
            if (CursorDraw(dc, 0, 0, shared, width, height, 0, 0, 3) == 0) throw CursorWindowsFailure();
            _ = CursorFlush();
            var black = pixels.ToArray();
            pixels.Fill(0xffffffff);
            if (CursorDraw(dc, 0, 0, shared, width, height, 0, 0, 3) == 0) throw CursorWindowsFailure();
            _ = CursorFlush();
            // Two backgrounds recover transparency for both alpha and legacy masked artwork.
            for (var i = 0; i < pixels.Length; i++)
            {
                var alpha = 255 - (int)((pixels[i] >> 16) & 255) + (int)((black[i] >> 16) & 255);
                alpha = Math.Clamp(alpha, 0, 255);
                uint Channel(int shift) => alpha == 0 ? 0 : (uint)Math.Min(255, ((black[i] >> shift) & 255) * 255 / alpha);
                pixels[i] = (uint)alpha << 24 | Channel(16) << 16 | Channel(8) << 8 | Channel(0);
            }
            var surface = SDL.CreateSurfaceFrom(width, height, SDL.PixelFormat.ARGB8888, bits, width * 4);
            if (surface == 0) throw SDLFailure("copy the Windows help cursor");
            try { return SDL.CreateColorCursor(surface, (int)info.X, (int)info.Y); }
            finally { SDL.DestroySurface(surface); }
        }
        finally
        {
            if (dc != 0 && previous != 0 && previous != -1) _ = CursorSelect(dc, previous);
            if (bitmap != 0) _ = CursorDeleteObject(bitmap);
            if (dc != 0) _ = CursorDeleteDC(dc);
            if (info.Color != 0) _ = CursorDeleteObject(info.Color);
            if (info.Mask != 0) _ = CursorDeleteObject(info.Mask);
        }
    }

    private static Win32Exception CursorWindowsFailure() => new(Marshal.GetLastPInvokeError(), "Cannot copy the Windows help cursor.");
    [StructLayout(LayoutKind.Sequential)]
    private struct CursorIconInfo { internal int Icon; internal uint X, Y; internal nint Mask, Color; }
    [StructLayout(LayoutKind.Sequential)]
    private struct CursorBitmap { internal int Type, Width, Height, WidthBytes; internal ushort Planes, Bits; internal nint Data; }
    [StructLayout(LayoutKind.Sequential)]
    private struct CursorDIB { internal uint Size; internal int Width, Height; internal ushort Planes, Bits; internal uint Compression, ImageSize; internal int XPixels, YPixels; internal uint Used, Important; }
    [LibraryImport("user32.dll", EntryPoint = "LoadCursorW", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint CursorLoad(nint instance, nint name);
    [LibraryImport("user32.dll", EntryPoint = "GetIconInfo", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int CursorInfo(nint cursor, out CursorIconInfo info);
    [LibraryImport("user32.dll", EntryPoint = "DrawIconEx", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int CursorDraw(nint dc, int x, int y, nint icon, int width, int height, uint step, nint brush, uint flags);
    [LibraryImport("gdi32.dll", EntryPoint = "GetObjectW", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int CursorBitmapInfo(nint bitmap, int size, out CursorBitmap info);
    [LibraryImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint CursorCreateDC(nint dc);
    [LibraryImport("gdi32.dll", EntryPoint = "CreateDIBSection", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint CursorCreateDIB(nint dc, ref CursorDIB header, uint usage, out nint bits, nint section, uint offset);
    [LibraryImport("gdi32.dll", EntryPoint = "SelectObject", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint CursorSelect(nint dc, nint bitmap);
    [LibraryImport("gdi32.dll", EntryPoint = "DeleteObject"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int CursorDeleteObject(nint value);
    [LibraryImport("gdi32.dll", EntryPoint = "DeleteDC"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int CursorDeleteDC(nint dc);
    [LibraryImport("gdi32.dll", EntryPoint = "GdiFlush"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int CursorFlush();
}
