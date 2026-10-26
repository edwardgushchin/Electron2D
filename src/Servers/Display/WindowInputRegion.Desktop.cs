using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed partial class WindowInputRegion
{
    [StructLayout(LayoutKind.Sequential)] private struct XPoint { internal short X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct Point { internal int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { internal int Left, Top, Right, Bottom; }
    private nint _cocoaSelector;
    private void CheckXShape()
    {
        if (XShapeQueryExtension(_display, out _, out _) == 0 || XShapeQueryVersion(_display, out var major, out var minor) == 0 || major < 1 || major == 1 && minor < 1)
            throw new NotSupportedException("The X11 server has no Shape input-region extension.");
    }
    private unsafe void ApplyX11(bool passthrough, ReadOnlySpan<Vector2> polygon, float scale)
    {
        foreach (var point in polygon)
        {
            var x = Math.Truncate((double)point.X / scale); var y = Math.Truncate((double)point.Y / scale);
            if (x < short.MinValue || x > short.MaxValue || y < short.MinValue || y > short.MaxValue) throw new ArgumentOutOfRangeException(nameof(polygon), "X11 input-region vertices must fit signed 16-bit native coordinates.");
        }
        if (!passthrough && polygon.IsEmpty) { XShapeCombineMask(_display, (nuint)_native, 2, 0, 0, 0, 0); XFlush(_display); return; }
        XPoint[]? rented = null; nint region = 0;
        try
        {
            if (passthrough || polygon.Length < 3) region = XCreateRegion();
            else
            {
                Span<XPoint> points = polygon.Length <= 256 ? stackalloc XPoint[polygon.Length] : (rented = System.Buffers.ArrayPool<XPoint>.Shared.Rent(polygon.Length)).AsSpan(0, polygon.Length);
                for (var i = 0; i < polygon.Length; i++)
                {
                    var x = Math.Truncate((double)polygon[i].X / scale); var y = Math.Truncate((double)polygon[i].Y / scale);
                    points[i] = new() { X = (short)x, Y = (short)y };
                }
                fixed (XPoint* data = points) region = XPolygonRegion(data, points.Length, 0);
            }
            if (region == 0) throw new OutOfMemoryException("Cannot create an X11 mouse input region.");
            XShapeCombineRegion(_display, (nuint)_native, 2, 0, 0, region, 0); XFlush(_display);
        }
        finally { if (region != 0) XDestroyRegion(region); if (rented is not null) System.Buffers.ArrayPool<XPoint>.Shared.Return(rented); }
    }
    private unsafe void ApplyWindows(ReadOnlySpan<Vector2> polygon, float scale)
    {
        Point[]? rented = null; nint region = 0;
        try
        {
            if (!polygon.IsEmpty)
            {
                Span<Point> points = polygon.Length <= 256 ? stackalloc Point[polygon.Length] : (rented = System.Buffers.ArrayPool<Point>.Shared.Rent(polygon.Length)).AsSpan(0, polygon.Length);
                var offset = new Point();
                if (ClientToScreen(_native, ref offset) == 0 || GetWindowRect(_native, out var outer) == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
                offset.X -= outer.Left; offset.Y -= outer.Top;
                for (var i = 0; i < polygon.Length; i++)
                {
                    var x = Math.Truncate((double)polygon[i].X / scale) + offset.X; var y = Math.Truncate((double)polygon[i].Y / scale) + offset.Y;
                    if (x < -67108864 || x > 67108863 || y < -67108864 || y > 67108863) throw new ArgumentOutOfRangeException(nameof(polygon), "Windows input-region vertices must fit signed 27-bit native region coordinates.");
                    points[i] = new() { X = (int)x, Y = (int)y };
                }
                fixed (Point* data = points) region = CreatePolygonRgn(data, points.Length, 1);
                if (region == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
            if (SetWindowRgn(_native, region, 1) == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
            region = 0;
        }
        finally { if (region != 0) DeleteObject(region); if (rented is not null) System.Buffers.ArrayPool<Point>.Shared.Return(rented); }
    }
    private nint _subclassHandle;
    private unsafe void PrepareWindows()
    {
        var handle = GCHandle.Alloc(this); _subclassHandle = GCHandle.ToIntPtr(handle);
        if (SetWindowSubclass(_native, (nint)(delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nuint, nuint, nint>)&WindowsMessage, (nuint)_subclassHandle, (nuint)_subclassHandle) != 0) return;
        handle.Free(); _subclassHandle = 0; throw new InvalidOperationException("Cannot install the native mouse input policy.");
    }
    private unsafe void ReleaseWindowsSubclass()
    {
        _passthrough = false;
        if (_subclassHandle == 0) return;
        if (RemoveWindowSubclass(_native, (nint)(delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nuint, nuint, nint>)&WindowsMessage, (nuint)_subclassHandle) != 0) FreeWindowsHandle();
        // A callback still installed after failed removal releases its owner at native destruction.
    }
    private void FreeWindowsHandle()
    {
        var handle = _subclassHandle; _subclassHandle = 0;
        if (handle != 0) GCHandle.FromIntPtr(handle).Free();
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint WindowsMessage(nint window, uint message, nuint wParam, nint lParam, nuint id, nuint data)
    {
        var owner = (WindowInputRegion)GCHandle.FromIntPtr((nint)data).Target!;
        var result = message == 0x84 && owner._passthrough ? -1 : DefSubclassProc(window, message, wParam, lParam);
        if (message == 0x82) owner.FreeWindowsHandle();
        return result;
    }
    private void PrepareCocoa() { _cocoaSelector = Selector("setIgnoresMouseEvents:"); _ignores = CocoaBool(_native, Selector("ignoresMouseEvents")) != 0; }
    private void SetCocoaIgnores(bool value) => CocoaSetBool(_native, _cocoaSelector, value ? (byte)1 : (byte)0);
    [LibraryImport("libXext.so.6")] private static partial int XShapeQueryExtension(nint display, out int eventBase, out int errorBase);
    [LibraryImport("libXext.so.6")] private static partial int XShapeQueryVersion(nint display, out int major, out int minor);
    [LibraryImport("libXext.so.6")] private static partial void XShapeCombineRegion(nint display, nuint window, int kind, int x, int y, nint region, int operation);
    [LibraryImport("libXext.so.6")] private static partial void XShapeCombineMask(nint display, nuint window, int kind, int x, int y, nuint mask, int operation);
    [LibraryImport("libX11.so.6")] private static partial nint XCreateRegion();
    [LibraryImport("libX11.so.6")] private static unsafe partial nint XPolygonRegion(XPoint* points, int count, int rule);
    [LibraryImport("libX11.so.6")] private static partial int XDestroyRegion(nint region);
    [LibraryImport("libX11.so.6")] private static partial int XFlush(nint display);
    [LibraryImport("gdi32.dll", SetLastError = true)] private static unsafe partial nint CreatePolygonRgn(Point* points, int count, int rule);
    [LibraryImport("user32.dll", SetLastError = true)] private static partial int SetWindowRgn(nint window, nint region, int redraw);
    [LibraryImport("user32.dll", SetLastError = true)] private static partial int ClientToScreen(nint window, ref Point point);
    [LibraryImport("user32.dll", SetLastError = true)] private static partial int GetWindowRect(nint window, out NativeRect rect);
    [LibraryImport("comctl32.dll")] private static partial int SetWindowSubclass(nint window, nint callback, nuint id, nuint data);
    [LibraryImport("comctl32.dll")] private static partial int RemoveWindowSubclass(nint window, nint callback, nuint id);
    [LibraryImport("comctl32.dll")] private static partial nint DefSubclassProc(nint window, uint message, nuint wParam, nint lParam);
    [LibraryImport("gdi32.dll", SetLastError = true)] private static partial int DeleteObject(nint region);
    [LibraryImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)] private static partial nint Selector(string name);
    [LibraryImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")] private static partial byte CocoaBool(nint instance, nint selector);
    [LibraryImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")] private static partial void CocoaSetBool(nint instance, nint selector, byte value);
}
