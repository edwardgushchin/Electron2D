using System.Numerics;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class X11ScreenCapture
{
    private const string Xlib = "libX11.so.6";
    private const string XCB = "libxcb.so.1";
    private const byte ZPixmap = 2;
    private const int TrueColor = 4;
    private readonly nint _display;
    private readonly nint _connection;
    private readonly uint _root;
    private readonly int _depth;
    private readonly PixelLayout _layout;

    private X11ScreenCapture(nint display, nint connection, uint root, int depth, PixelLayout layout)
    {
        _display = display;
        _connection = connection;
        _root = root;
        _depth = depth;
        _layout = layout;
    }

    internal static X11ScreenCapture? Open(nint display)
    {
        try
        {
            // Independent X screens have separate origins. SDL's public desktop rectangles cannot identify their roots.
            if (XScreenCount(display) != 1 || XQueryExtension(display, "XWAYLAND", out _, out _, out _) != 0)
                return null;

            var connection = XGetXCBConnection(display);
            var screen = XDefaultScreen(display);
            var visual = XDefaultVisual(display, screen);
            var root = XDefaultRootWindow(display);
            var depth = XDefaultDepth(display, screen);
            if (connection == 0 || root == 0 || visual == null || visual->Class != TrueColor)
                return null;

            var formats = XListPixmapFormats(display, out var count);
            try
            {
                for (var i = 0; formats != null && i < count; i++)
                {
                    var format = formats[i];
                    if (format.Depth != depth || format.BitsPerPixel is not (8 or 16 or 24 or 32) ||
                        format.ScanlinePad is not (8 or 16 or 32))
                        continue;
                    var red = (uint)visual->RedMask;
                    var green = (uint)visual->GreenMask;
                    var blue = (uint)visual->BlueMask;
                    if (red == 0 || green == 0 || blue == 0)
                        return null;
                    var layout = new PixelLayout(format.BitsPerPixel / 8, format.ScanlinePad / 8,
                        XImageByteOrder(display) == 0, new Channel(red), new Channel(green), new Channel(blue));
                    return new X11ScreenCapture(display, connection, checked((uint)root), depth, layout);
                }
                return null;
            }
            finally
            {
                if (formats != null) XFree(formats);
            }
        }
        catch (DllNotFoundException) { return null; }
        catch (EntryPointNotFoundException) { return null; }
    }

    internal X11ColorSampler OpenColorSampler() => X11ColorSampler.Open(XDisplayString(_display), _root);

    internal Image Capture(Rect2i rectangle)
    {
        var width = rectangle.Size.X;
        var height = rectangle.Size.Y;
        var size = Image.GetRequiredDataSize(width, height, Image.Format.Rgba8, mipmaps: false);
        var reply = Read(rectangle, nameof(rectangle), out var stride);
        try
        {
            var source = (byte*)reply + 32;
            var data = GC.AllocateUninitializedArray<byte>(size);
            var index = 0;
            for (var y = 0; y < height; y++)
            {
                var row = source + (y * stride);
                for (var x = 0; x < width; x++)
                {
                    var pixel = _layout.Read(row + (x * _layout.BytesPerPixel));
                    data[index++] = _layout.Red.ToByte(pixel);
                    data[index++] = _layout.Green.ToByte(pixel);
                    data[index++] = _layout.Blue.ToByte(pixel);
                    data[index++] = 255;
                }
            }
            return Image.FromImportedState(new Image.State(width, height, Image.Format.Rgba8, false, data));
        }
        finally { NativeMemory.Free(reply); }
    }

    internal Color Pixel(Vector2i position)
    {
        var reply = Read(new Rect2i(position, Vector2i.One), nameof(position), out _);
        try
        {
            var pixel = _layout.Read((byte*)reply + 32);
            return new Color(_layout.Red.ToFloat(pixel), _layout.Green.ToFloat(pixel), _layout.Blue.ToFloat(pixel));
        }
        finally { NativeMemory.Free(reply); }
    }

    private Reply* Read(Rect2i rectangle, string argument, out int stride)
    {
        var x = rectangle.Position.X;
        var y = rectangle.Position.Y;
        var width = rectangle.Size.X;
        var height = rectangle.Size.Y;
        if (x is < 0 or > short.MaxValue || y is < 0 or > short.MaxValue ||
            width is <= 0 or > ushort.MaxValue || height is <= 0 or > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(argument, "The capture rectangle cannot be represented by X11.");

        XFlush(_display);
        var geometry = GetGeometryReply(_connection, GetGeometry(_connection, _root), out var geometryError);
        try
        {
            CheckReply(geometry, geometryError, "query the desktop size");
            if (x + width > geometry->Width || y + height > geometry->Height)
                throw new ArgumentOutOfRangeException(argument, "The capture rectangle must fit inside the X11 desktop.");
        }
        finally
        {
            NativeMemory.Free(geometryError);
            NativeMemory.Free(geometry);
        }

        var cookie = GetImage(_connection, ZPixmap, _root, (short)x, (short)y, (ushort)width, (ushort)height, uint.MaxValue);
        var reply = GetImageReply(_connection, cookie, out var error);
        try
        {
            CheckReply(reply, error, "capture desktop pixels");
            var rowBytes = width * _layout.BytesPerPixel;
            stride = ((rowBytes + _layout.ScanlinePad - 1) / _layout.ScanlinePad) * _layout.ScanlinePad;
            if (reply->Depth != _depth || (long)reply->Length * 4 < (long)stride * height)
                throw new IOException("The X11 server returned an incompatible desktop pixel buffer.");
            return reply;
        }
        catch
        {
            NativeMemory.Free(reply);
            throw;
        }
        finally { NativeMemory.Free(error); }
    }

    private static void CheckReply(void* reply, byte* error, string operation)
    {
        if (error != null)
            throw new IOException($"X11 could not {operation} (protocol error {error[1]}).");
        if (reply == null)
            throw new IOException($"X11 did not return a reply while trying to {operation}.");
    }

    private readonly struct Channel(uint mask)
    {
        private readonly int _shift = BitOperations.TrailingZeroCount(mask);
        private uint Maximum => mask >> _shift;
        private uint Component(uint pixel) => (pixel & mask) >> _shift;
        internal byte ToByte(uint pixel) => (byte)(((ulong)Component(pixel) * 255 + Maximum / 2) / Maximum);
        internal float ToFloat(uint pixel) => (float)Component(pixel) / Maximum;
    }

    private readonly record struct PixelLayout(int BytesPerPixel, int ScanlinePad, bool LittleEndian, Channel Red, Channel Green, Channel Blue)
    {
        internal uint Read(byte* source)
        {
            uint value = 0;
            for (var i = 0; i < BytesPerPixel; i++)
            {
                var offset = LittleEndian ? BytesPerPixel - 1 - i : i;
                value = (value << 8) | source[offset];
            }
            return value;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Visual
    {
        internal nint ExtensionData;
        internal nuint ID;
        internal int Class;
        internal nuint RedMask;
        internal nuint GreenMask;
        internal nuint BlueMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PixmapFormat
    {
        internal int Depth;
        internal int BitsPerPixel;
        internal int ScanlinePad;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct GeometryReply
    {
        [FieldOffset(16)] internal ushort Width;
        [FieldOffset(18)] internal ushort Height;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct Reply
    {
        [FieldOffset(1)] internal byte Depth;
        [FieldOffset(4)] internal uint Length;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Cookie
    {
        internal uint Sequence;
    }

    [LibraryImport(Xlib)] private static partial int XScreenCount(nint display);
    [LibraryImport(Xlib)] private static partial int XDefaultScreen(nint display);
    [LibraryImport(Xlib)] private static partial nuint XDefaultRootWindow(nint display);
    [LibraryImport(Xlib)] private static partial Visual* XDefaultVisual(nint display, int screen);
    [LibraryImport(Xlib)] private static partial int XDefaultDepth(nint display, int screen);
    [LibraryImport(Xlib)] private static partial PixmapFormat* XListPixmapFormats(nint display, out int count);
    [LibraryImport(Xlib)] private static partial int XImageByteOrder(nint display);
    [LibraryImport(Xlib)] private static partial int XFlush(nint display);
    [LibraryImport(Xlib)] private static partial int XFree(void* pointer);
    [LibraryImport(Xlib)] private static partial nint XDisplayString(nint display);
    [LibraryImport(Xlib, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int XQueryExtension(nint display, string name, out int opcode, out int firstEvent, out int firstError);
    [LibraryImport("libX11-xcb.so.1")] private static partial nint XGetXCBConnection(nint display);

    [LibraryImport(XCB, EntryPoint = "xcb_get_geometry")]
    private static partial Cookie GetGeometry(nint connection, uint drawable);
    [LibraryImport(XCB, EntryPoint = "xcb_get_geometry_reply")]
    private static partial GeometryReply* GetGeometryReply(nint connection, Cookie cookie, out byte* error);
    [LibraryImport(XCB, EntryPoint = "xcb_get_image")]
    private static partial Cookie GetImage(nint connection, byte format, uint drawable, short x, short y,
        ushort width, ushort height, uint planeMask);
    [LibraryImport(XCB, EntryPoint = "xcb_get_image_reply")]
    private static partial Reply* GetImageReply(nint connection, Cookie cookie, out byte* error);
}
