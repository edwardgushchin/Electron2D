using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private bool _windowIconOverridden;
    private bool _nativeWaylandIconAvailable;

    /// <summary>Sets an icon specifically for the main window.</summary>
    /// <param name="image">A live nonempty source image.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>The image is copied and converted to eight-bit RGBA before native submission; the caller retains it. A successful call overrides subsequent <see cref="SetIcon"/> changes for this window. Wayland requires a square image and a compositor that supports window icons.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="image"/> is empty or is not square on Wayland.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowId"/> is not the main-window ID.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread or the native icon request fails.</exception>
    /// <exception cref="ObjectDisposedException">The display server or image has been disposed.</exception>
    public void WindowSetIcon(Image image, int windowId = MainWindowId)
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

    /// <summary>Sets the default icon for the engine-owned main window unless it has an explicit icon.</summary>
    /// <param name="image">A live nonempty source image.</param>
    /// <remarks>The image is copied before native submission. A prior successful <see cref="WindowSetIcon"/> call keeps its window-specific icon. The current host owns only one window and does not install a desktop-launcher icon. Wayland requires a square image and a compositor that supports window icons.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="image"/> is empty or is not square on Wayland.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread or the native icon request fails.</exception>
    /// <exception cref="ObjectDisposedException">The display server or image has been disposed.</exception>
    public void SetIcon(Image image)
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
