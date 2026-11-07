using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class WaylandInput
{
    private uint _captureSourceGlobal, _captureManagerGlobal, _captureShmGlobal, _captureShmVersion;
    private nint _captureSourceManager, _captureManager, _captureShm;

    internal bool CanCaptureOutput
    {
        get
        {
            DispatchPending();
            return _captureSourceGlobal != 0 && _captureManagerGlobal != 0 && _captureShmGlobal != 0;
        }
    }

    private void ObserveCaptureGlobal(uint id, string? name, uint version)
    {
        switch (name)
        {
            case "ext_output_image_capture_source_manager_v1" when _captureSourceGlobal == 0:
                _captureSourceGlobal = id;
                break;
            case "ext_image_copy_capture_manager_v1" when _captureManagerGlobal == 0:
                _captureManagerGlobal = id;
                break;
            case "wl_shm" when _captureShmGlobal == 0:
                _captureShmGlobal = id;
                _captureShmVersion = Math.Min(version, 2u);
                break;
        }
    }

    private void RetireCaptureGlobal(uint id)
    {
        if (id == _captureSourceGlobal) _captureSourceGlobal = 0;
        else if (id == _captureManagerGlobal) _captureManagerGlobal = 0;
        else if (id == _captureShmGlobal) _captureShmGlobal = 0;
        else return;
        DisposeCapture();
    }

    private void DisposeCapture()
    {
        Retire(ref _captureShm, 1, 2);
        Retire(ref _captureManager, 2);
        Retire(ref _captureSourceManager, 1);
    }

    private static void Retire(ref nint proxy, uint opcode, uint minimumVersion = 1)
    {
        if (proxy == 0) return;
        ReleaseProxy(proxy, opcode, minimumVersion);
        proxy = 0;
    }

    internal void CaptureOutput(nint output, Vector2i size, Rect2i crop, Span<byte> destination, int destinationStride)
    {
        if (output == 0)
            throw new IOException("The Wayland output is unavailable.");
        if (!CanCaptureOutput)
            throw new NotSupportedException("The compositor does not expose Wayland output capture.");
        if (_captureShm == 0)
        {
            try
            {
                CaptureProtocol.Link(_library);
                _captureSourceManager = CaptureHandle(BindGlobal(_captureSourceGlobal, CaptureProtocol.SourceManager.Handle, 1));
                _captureManager = CaptureHandle(BindGlobal(_captureManagerGlobal, CaptureProtocol.Manager.Handle, 1));
                _captureShm = CaptureHandle(BindGlobal(_captureShmGlobal, NativeLibrary.GetExport(_library, "wl_shm_interface"), _captureShmVersion));
                Listen(_captureShm, &IgnoreCaptureEvent, default);
            }
            catch
            {
                DisposeCapture();
                throw;
            }
        }

        using var capture = new OutputCapture(this);
        capture.Copy(output, size, crop, destination, destinationStride);
    }

    private static nint CaptureHandle(nint handle) =>
        handle != 0 ? handle : throw new IOException("Could not create a Wayland capture object.");

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int IgnoreCaptureEvent(nint context, nint proxy, uint opcode, nint message, Argument* arguments) => 0;

    private sealed class OutputCapture(WaylandInput owner) : IDisposable
    {
        private const uint ARGB8888 = 0, XRGB8888 = 1, ABGR8888 = 0x34324241, XBGR8888 = 0x34324258;
        private const int TimeoutMilliseconds = 2000;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private nint _source, _session, _frame, _buffer;
        private nint _pixels = -1;
        private int _length;
        private GCHandle _listener;
        private uint _pendingWidth, _pendingHeight, _width, _height, _transform;
        private uint? _pendingFormat, _format, _failure;
        private bool _configured, _stopped, _ready;

        internal void Copy(nint output, Vector2i size, Rect2i crop, Span<byte> destination, int destinationStride)
        {
            Argument* arguments = stackalloc Argument[4];
            arguments[0] = default;
            arguments[1].Object = output;
            _source = CaptureHandle(MarshalRequest(owner._captureSourceManager, 0, CaptureProtocol.Source.Handle, 1, 0, arguments));
            arguments[1].Object = _source;
            arguments[2] = default;
            _session = CaptureHandle(MarshalRequest(owner._captureManager, 0, CaptureProtocol.Session.Handle, 1, 0, arguments));

            _listener = GCHandle.Alloc(this);
            Listen(_session, &SessionEvent, _listener);
            WaitFor(configuration: true);
            if (_format is not { } format)
                throw new NotSupportedException("The compositor offered no supported Wayland shared-memory capture format.");

            if (_width is 0 or > int.MaxValue || _height is 0 or > int.MaxValue)
                throw new IOException("The compositor returned invalid Wayland capture dimensions.");
            var width = (int)_width;
            var height = (int)_height;
            CreateBuffer(width, height, format);
            _frame = CaptureHandle(MarshalRequest(_session, 0, CaptureProtocol.Frame.Handle, 1, 0, arguments));
            Listen(_frame, &FrameEvent, _listener);

            arguments[0].Object = _buffer;
            MarshalRequest(_frame, 1, 0, 1, 0, arguments);
            arguments[0] = default;
            arguments[1] = default;
            arguments[2].Int = width;
            arguments[3].Int = height;
            MarshalRequest(_frame, 2, 0, 1, 0, arguments);
            MarshalRequest(_frame, 3, 0, 1, 0, null);
            WaitFor(configuration: false);

            var orientedSize = (_transform & 1) == 0 ? new Vector2i(width, height) : new Vector2i(height, width);
            if (_transform > 7 || orientedSize != size)
                throw new IOException("The Wayland output changed dimensions or returned an invalid transform.");

            CopyPixels(width, height, format, crop, destination, destinationStride);
        }

        private void CreateBuffer(int width, int height, uint format)
        {
            if (owner._captureShm == 0)
                throw new IOException("The Wayland shared-memory global was withdrawn.");
            _length = Image.GetRequiredDataSize(width, height, Image.Format.Rgba8, mipmaps: false);
            var fd = CreateMemFD("electron2d-screen", 1); // MFD_CLOEXEC
            if (fd < 0)
                throw new IOException("Could not create Wayland capture shared memory.");
            nint pool = 0;
            try
            {
                if (Truncate(fd, _length) < 0)
                    throw new IOException("Could not size Wayland capture shared memory.");
                _pixels = MapMemory(0, (nuint)_length, 1, 1, fd, 0); // PROT_READ, MAP_SHARED
                if (_pixels == -1)
                    throw new IOException("Could not map Wayland capture shared memory.");

                Argument* arguments = stackalloc Argument[6];
                arguments[0] = default;
                arguments[1].FD = fd;
                arguments[2].Int = _length;
                var poolInterface = NativeLibrary.GetExport(owner._library, "wl_shm_pool_interface");
                pool = CaptureHandle(MarshalRequest(owner._captureShm, 0, poolInterface, 1, 0, arguments));

                arguments[1] = default;
                arguments[2].Int = width;
                arguments[3].Int = height;
                arguments[4].Int = width * 4;
                arguments[5].UInt = format;
                var bufferInterface = NativeLibrary.GetExport(owner._library, "wl_buffer_interface");
                _buffer = CaptureHandle(MarshalRequest(pool, 0, bufferInterface, 1, 0, arguments));
                Listen(_buffer, &IgnoreCaptureEvent, default);
            }
            finally
            {
                Retire(ref pool, 1);
                CloseFD(fd);
            }
        }

        private void WaitFor(bool configuration)
        {
            while (true)
            {
                owner.DispatchPending();
                if (_failure is { } failure)
                    throw new IOException($"The compositor failed to capture the Wayland output (reason {failure}).");
                if (_ready) return;
                if (_stopped)
                    throw new IOException("The compositor stopped the Wayland capture session.");
                if (configuration && _configured) return;
                if (RemainingTime() <= 0)
                    throw new IOException("Timed out waiting for the Wayland compositor to capture the output.");
                if (PrepareReadQueue(owner._display, owner._queue) == 0)
                    ReadEvents();
            }
        }

        private int RemainingTime() => TimeoutMilliseconds - (int)_clock.ElapsedMilliseconds;

        private void ReadEvents()
        {
            const short readable = 1, writable = 4;
            const int interrupted = 4;
            var read = false;
            try
            {
                var poll = new PollDescriptor { FD = DisplayFD(owner._display) };
                var flushed = false;
                for (int remaining; (remaining = RemainingTime()) > 0;)
                {
                    if (!flushed)
                    {
                        flushed = Flush(owner._display) >= 0;
                        if (!flushed && Marshal.GetLastPInvokeError() != TryAgain)
                        {
                            if (Marshal.GetLastPInvokeError() == interrupted) continue;
                            throw new IOException("Could not submit Wayland capture requests.");
                        }
                    }

                    poll.Events = flushed ? readable : (short)(readable | writable);
                    var result = Poll(&poll, 1, remaining);
                    if (result < 0 && Marshal.GetLastPInvokeError() == interrupted) continue;
                    if (result < 0 || (poll.ReturnedEvents & ~(readable | writable)) != 0)
                        throw new IOException("The Wayland capture connection failed.");
                    if (result == 0)
                        break;
                    if ((poll.ReturnedEvents & readable) == 0)
                        continue;

                    read = true;
                    if (ReadDisplayEvents(owner._display) < 0)
                        throw new IOException("Could not read the Wayland capture events.");
                    return;
                }
                throw new IOException("Timed out waiting for the Wayland compositor to capture the output.");
            }
            finally { if (!read) CancelRead(owner._display); }
        }

        private void CopyPixels(int width, int height, uint format, Rect2i crop, Span<byte> destination, int stride)
        {
            var bgr = format is ABGR8888 or XBGR8888;
            var pixels = (uint*)_pixels;
            for (var y = 0; y < crop.Size.Y; y++)
            {
                var target = destination.Slice(y * stride, crop.Size.X * 4);
                for (var x = 0; x < crop.Size.X; x++)
                {
                    var atX = crop.Position.X + x;
                    var atY = crop.Position.Y + y;
                    var (rawX, rawY) = _transform switch
                    {
                        1 => (atY, height - 1 - atX),
                        2 => (width - 1 - atX, height - 1 - atY),
                        3 => (width - 1 - atY, atX),
                        4 => (width - 1 - atX, atY),
                        5 => (atY, atX),
                        6 => (atX, height - 1 - atY),
                        7 => (width - 1 - atY, height - 1 - atX),
                        _ => (atX, atY),
                    };
                    var pixel = pixels[rawY * width + rawX];
                    var index = x * 4;
                    target[index] = (byte)(pixel >> (bgr ? 0 : 16));
                    target[index + 1] = (byte)(pixel >> 8);
                    target[index + 2] = (byte)(pixel >> (bgr ? 16 : 0));
                    target[index + 3] = 255;
                }
            }
        }

        public void Dispose()
        {
            Retire(ref _frame, 0);
            Retire(ref _buffer, 0);
            Retire(ref _session, 1);
            Retire(ref _source, 0);
            if (_listener.IsAllocated) _listener.Free();
            if (_pixels != -1) UnmapMemory(_pixels, (nuint)_length);
            Flush(owner._display);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int SessionEvent(nint context, nint proxy, uint opcode, nint message, Argument* arguments)
        {
            var capture = (OutputCapture)GCHandle.FromIntPtr(context).Target!;
            switch (opcode)
            {
                case 0: // buffer_size
                    capture._pendingWidth = arguments[0].UInt;
                    capture._pendingHeight = arguments[1].UInt;
                    break;
                case 1: // shm_format
                    var format = arguments[0].UInt;
                    if (format is XRGB8888 or ARGB8888 or XBGR8888 or ABGR8888)
                        capture._pendingFormat ??= format;
                    break;
                case 4: // done
                    capture._configured = true;
                    capture._width = capture._pendingWidth;
                    capture._height = capture._pendingHeight;
                    capture._format = capture._pendingFormat;
                    capture._pendingFormat = null;
                    break;
                case 5: // stopped
                    capture._stopped = true;
                    break;
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int FrameEvent(nint context, nint proxy, uint opcode, nint message, Argument* arguments)
        {
            var capture = (OutputCapture)GCHandle.FromIntPtr(context).Target!;
            switch (opcode)
            {
                case 0: capture._transform = arguments[0].UInt; break;
                case 3: capture._ready = true; break;
                case 4: capture._failure = arguments[0].UInt; break;
            }
            return 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PollDescriptor
    {
        internal int FD;
        internal short Events, ReturnedEvents;
    }

    [LibraryImport(ClientLibrary, EntryPoint = "wl_display_get_fd")]
    private static partial int DisplayFD(nint display);
    [LibraryImport(ClientLibrary, EntryPoint = "wl_display_prepare_read_queue")]
    private static partial int PrepareReadQueue(nint display, nint queue);
    [LibraryImport(ClientLibrary, EntryPoint = "wl_display_read_events")]
    private static partial int ReadDisplayEvents(nint display);
    [LibraryImport(ClientLibrary, EntryPoint = "wl_display_cancel_read")]
    private static partial void CancelRead(nint display);
    [LibraryImport("libc", EntryPoint = "poll", SetLastError = true)]
    private static partial int Poll(PollDescriptor* descriptors, nuint count, int timeout);
    [LibraryImport("libc", EntryPoint = "memfd_create", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int CreateMemFD(string name, uint flags);
    [LibraryImport("libc", EntryPoint = "ftruncate")]
    private static partial int Truncate(int fd, long length);
    [LibraryImport("libc", EntryPoint = "mmap")]
    private static partial nint MapMemory(nint address, nuint length, int protection, int flags, int fd, long offset);
    [LibraryImport("libc", EntryPoint = "munmap")]
    private static partial int UnmapMemory(nint address, nuint length);
    [LibraryImport("libc", EntryPoint = "close")]
    private static partial int CloseFD(int fd);
}
