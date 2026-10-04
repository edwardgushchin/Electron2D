using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private bool _windowIconOverridden;
    private bool _nativeWaylandIconAvailable;

    internal void WindowSetIconCore(Image image, int windowId = MainWindowId)
    {
        EnsureOwner();
        ArgumentNullException.ThrowIfNull(image);
        var window = GetWindow(windowId);
        ValidateIcon(image);
        WithImageSurface(image, surface =>
        {
            if (!SDL.SetWindowIcon(window, surface))
                throw SDLFailure("set window icon");
        });
        _nativeWaylandIconAvailable = true;
        _windowIconOverridden = true;
    }

    internal void SetIconCore(Image image)
    {
        EnsureOwner();
        ValidateIcon(image);
        if (_windowIconOverridden)
            return;
        WithImageSurface(image, surface =>
        {
            if (!SDL.SetWindowIcon(GetWindow(MainWindowId), surface))
                throw SDLFailure("set default window icon");
        });
        _nativeWaylandIconAvailable = true;
    }

    private static void ValidateIcon(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.IsEmpty)
            throw new ArgumentException("An icon image must not be empty.", nameof(image));
        if (SDL.GetCurrentVideoDriver() == "wayland" && image.Width != image.Height)
            throw new ArgumentException("A Wayland icon image must be square.", nameof(image));
    }

    private static void WithImageSurface(Image image, Action<nint> action)
    {
        ArgumentNullException.ThrowIfNull(image);
        using var copy = new Image();
        copy.CopyFrom(image);
        if (copy.IsEmpty)
            throw new ArgumentException("A native image must not be empty.", nameof(image));
        copy.Convert(Image.Format.Rgba8);
        var pixels = copy.GetData();
        var pin = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            var format = BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888;
            var native = SDL.CreateSurfaceFrom(copy.Width, copy.Height, format,
                pin.AddrOfPinnedObject(), checked(copy.Width * 4));
            if (native == 0)
                throw SDLFailure("create an image surface");
            using var surface = new SdlSurfaceHandle(native);
            GC.SuppressFinalize(surface);
            action(surface.DangerousGetHandle());
        }
        finally
        {
            pin.Free();
        }
    }

    private sealed class SdlSurfaceHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SdlSurfaceHandle(nint handle) : base(ownsHandle: true) => SetHandle(handle);

        protected override bool ReleaseHandle()
        {
            SDL.DestroySurface(handle);
            return true;
        }
    }
}
