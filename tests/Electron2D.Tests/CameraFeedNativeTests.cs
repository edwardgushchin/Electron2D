using System.Reflection;
using System.Runtime.InteropServices;
using Electron2D;
using SDL3;

internal static class CameraFeedNativeTests
{
    private static nint _surface, _name;
    private static int _released, _closed;
    private static bool _available;
    private static SDL.CameraPermissionState _permission;
    private static SDL.CameraSpec _spec;
    private static nint Devices(out int count) { count = 1; var p = SDL.Malloc(8); Marshal.WriteInt32(p, 42); Marshal.WriteInt32(p + 4, 0); return p; }
    private static nint Name(uint id) => _name;
    private static SDL.CameraPosition Position(uint id) => SDL.CameraPosition.FrontFacing;
    private static nint Formats(uint id, out int count)
    {
        count = 1; var p = SDL.Malloc((nuint)(nint.Size * 2 + Marshal.SizeOf<SDL.CameraSpec>()));
        var item = p + nint.Size * 2; Marshal.WriteIntPtr(p, item); Marshal.WriteIntPtr(p + nint.Size, 0); Marshal.StructureToPtr(_spec, item, false); return p;
    }
    private static nint Open(uint id, in SDL.CameraSpec spec) => 123;
    private static nint OpenDefault(uint id, nint spec) => 123;
    private static SDL.CameraPermissionState Permission(nint camera) => _permission;
    private static nint Acquire(nint camera, out ulong timestamp) { timestamp = 1; if (!_available) return 0; _available = false; return _surface; }
    private static void Release(nint camera, nint surface) => _released++;
    private static void Close(nint camera) => _closed++;
    private static bool ConvertFailure(int width, int height, SDL.PixelFormat srcFormat, SDL.Colorspace srcColorspace, uint srcProps, nint src, int srcPitch, SDL.PixelFormat dstFormat, SDL.Colorspace dstColorspace, uint dstProps, nint dst, int dstPitch)
    { Marshal.WriteByte(dst, 99); return false; }
    private sealed class Hook : IDisposable
    {
        private readonly FieldInfo _field;
        private readonly object? _old;
        internal Hook(string field, string method)
        {
            _field = typeof(SDL).GetField(field, BindingFlags.NonPublic | BindingFlags.Static)!;
            _old = _field.GetValue(null);
            _field.SetValue(null, typeof(CameraFeedNativeTests).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!.CreateDelegate(_field.FieldType));
        }
        public void Dispose() => _field.SetValue(null, _old);
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    internal static unsafe void Run()
    {
        var previous = Environment.GetEnvironmentVariable("SDL_CAMERA_DRIVER");
        Environment.SetEnvironmentVariable("SDL_CAMERA_DRIVER", "dummy");
        _name = Marshal.StringToCoTaskMemUTF8("Synthetic camera"); _released = _closed = 0; _permission = SDL.CameraPermissionState.Approved;
        _spec = new() { Width = 4, Height = 2, PixelFormat = SDL.PixelFormat.YUY2, Colorspace = SDL.Colorspace.BT601Limited, FramerateNumerator = 30, FramerateDenominator = 1 };
        _surface = SDL.CreateSurface(4, 2, SDL.PixelFormat.YUY2);
        if (_surface == 0) throw new IOException(SDL.GetError());
        SDL.SetSurfaceColorspace(_surface, SDL.Colorspace.BT601Limited);
        var surface = (SDL.Surface*)_surface;
        for (var y = 0; y < 2; y++) new byte[] { 16, 128, 235, 128, 81, 90, 81, 240 }.CopyTo(new Span<byte>((void*)(surface->Pixels + y * surface->Pitch), 8));
        using var devices = new Hook("GetCamerasNativeFunction", nameof(Devices));
        using var name = new Hook("GetCameraNameNativeFunction", nameof(Name));
        using var position = new Hook("GetCameraPositionNativeFunction", nameof(Position));
        using var formats = new Hook("GetCameraSupportedFormatsNativeFunction", nameof(Formats));
        using var open = new Hook("OpenCameraWithSpecNativeFunction", nameof(Open));
        using var openDefault = new Hook("OpenCameraWithPointerNativeFunction", nameof(OpenDefault));
        using var permission = new Hook("GetCameraPermissionStateNativeFunction", nameof(Permission));
        using var acquire = new Hook("AcquireCameraFrameNativeFunction", nameof(Acquire));
        using var release = new Hook("ReleaseCameraFrameNativeFunction", nameof(Release));
        using var close = new Hook("CloseCameraNativeFunction", nameof(Close));
        try
        {
            CameraServer.MonitoringFeeds = true; CameraServer.Process();
            var feed = CameraServer.GetFeed(0); var id = feed.GetID();
            Check(feed.GetName() == "Synthetic camera" && feed.GetPosition() == CameraFeed.FeedPosition.Front && feed.Formats.Single() == new CameraFeedFormat(4, 2, "SDL_PIXELFORMAT_YUY2", 30, 1), "Native pointer-table format metadata and source discovery");
            using var view = new CameraTexture { CameraFeedID = id };
            Check(!feed.FeedIsActive, "Native discovery never opens a camera");
            foreach (var output in Enum.GetValues<CameraFeedOutput>())
            {
                Check(feed.SetFormat(0, output), "Native YUYV output accepts " + output); feed.FeedIsActive = true; _available = true; CameraServer.Process();
                using var image = view.GetImage(); Check(image is not null && image.Width == 4 && image.Height == 2, "Native full frame geometry");
                var data = image!.GetData();
                if (output == CameraFeedOutput.RGB) Check(image.GetPixel(0, 0).R < .01f && image.GetPixel(1, 0).R > .99f && image.GetPixel(2, 0).R > .9f && image.GetPixel(2, 0).G < .03f, "Real native colorspace conversion preserves limited-range black, white and red");
                if (output == CameraFeedOutput.Grayscale) Check(data[0] == 16 && data[4] == 235 && data[1] == 16 && data[3] == 255, "Native grayscale uses luminance and opaque alpha");
                if (output == CameraFeedOutput.Copy) Check(data.AsSpan(0, 8).SequenceEqual(new byte[] { 16, 128, 235, 128, 81, 90, 81, 240 }), "Native packed copy preserves every byte");
                if (output == CameraFeedOutput.Separate)
                {
                    Check(data.AsSpan(0, 4).SequenceEqual(new byte[] { 16, 235, 81, 81 }), "Native Y plane"); view.WhichFeed = CameraServer.FeedImage.CBCRImage;
                    using var chroma = view.GetImage(); Check(chroma!.Width == 2 && chroma.GetData().AsSpan(0, 4).SequenceEqual(new byte[] { 128, 128, 90, 240 }), "Native 4:2:2 chroma plane"); view.WhichFeed = CameraServer.FeedImage.RGBAImage;
                }
                feed.FeedIsActive = false;
            }
            Check(_released == 4 && _closed == 4, "Each acquired surface and activated handle retires exactly once");
            feed.SetFormat(0); feed.FeedIsActive = true; _available = true; CameraServer.Process();
            using var before = view.GetImage(); var bytes = before!.GetData(); var frames = 0; feed.FrameChanged += () => frames++;
            using (var fail = new Hook("ConvertPixelsAndColorspaceSpanNativeFunction", nameof(ConvertFailure))) { _available = true; Reject<AggregateException>(CameraServer.Process); }
            using (var after = view.GetImage()) Check(after!.GetData().SequenceEqual(bytes) && frames == 0 && _released == 6, "Partial conversion failure preserves old pixels and still releases the surface");
            for (var i = 0; i < 64; i++) { _available = true; CameraServer.Process(); }
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 256; i++) { _available = true; CameraServer.Process(); }
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Check(allocated == 0, "Prepared native camera conversion/publication allocated " + allocated + " managed bytes");
            Action stopOnFrame = () => feed.FeedIsActive = false;
            feed.FrameChanged += stopOnFrame; _available = true; CameraServer.Process();
            Check(!feed.FeedIsActive, "Frame callback can stop capture after the native surface is released");
            feed.FrameChanged -= stopOnFrame; feed.FeedIsActive = true;
            _permission = SDL.CameraPermissionState.Pending; _available = true; var released = _released; CameraServer.Process(); Check(_released == released && feed.FeedIsActive, "Pending permission acquires no surface");
            _permission = SDL.CameraPermissionState.Denied; Reject<AggregateException>(CameraServer.Process); Check(!feed.FeedIsActive, "Denied permission closes active native state");
            CameraServer.MonitoringFeeds = false; Check(feed.IsDisposed && view.GetSize() == Vector2.Zero && CameraServer.GetFeedCount() == 0, "Monitoring shutdown retires native feeds and detaches views");
            Console.WriteLine("Camera native boundary: discovery/format pointer tables, actual SDL YUYV conversion, four outputs, permission/failure cleanup, source retirement and 256 prepared frames with zero managed bytes passed.");
        }
        finally
        {
            CameraServer.MonitoringFeeds = false;
            SDL.DestroySurface(_surface); Marshal.FreeCoTaskMem(_name); _surface = _name = 0;
            Environment.SetEnvironmentVariable("SDL_CAMERA_DRIVER", previous);
        }
    }
}
