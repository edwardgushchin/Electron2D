using Microsoft.Win32.SafeHandles;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private MouseMode _mouseMode;
    private CursorShape _cursorShape;
    private SdlCursorHandle? _cursor;
    private readonly SdlCursorHandle?[] _customCursors = new SdlCursorHandle?[(int)CursorShape.Max];

    /// <summary>Gets the current mouse mode.</summary>
    /// <returns>The mode owned by this display server.</returns>
    public MouseMode MouseGetMode()
    {
        EnsureOwner();
        return _mouseMode;
    }

    /// <summary>Requests cursor visibility, capture, and confinement as one mode.</summary>
    /// <param name="mode">The mode to apply to the main window.</param>
    /// <remarks>
    /// Repeating the current mode does not resubmit native operations. Window managers may release grabs while the
    /// window lacks focus. If any native change fails, the prior mode is restored on a best-effort basis and the
    /// original native error is reported.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is unknown.</exception>
    /// <exception cref="InvalidOperationException">A native pointer-mode operation fails.</exception>
    /// <exception cref="AggregateException">Both a native pointer-mode operation and its rollback fail.</exception>
    public void MouseSetMode(MouseMode mode)
    {
        EnsureOwner();
        if ((uint)mode >= (uint)MouseMode.Max)
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown mouse mode.");
        if (mode == _mouseMode)
            return;
        var previous = _mouseMode;
        try
        {
            ApplyMouseMode(mode);
            _mouseMode = mode;
        }
        catch (Exception error)
        {
            try
            {
                ApplyMouseMode(previous);
            }
            catch (Exception rollbackError)
            {
                throw new AggregateException("Pointer mode change and rollback both failed.", error, rollbackError);
            }
            throw;
        }
    }

    /// <summary>Gets the last reported mouse cursor position.</summary>
    /// <returns>Desktop coordinates where available; on Wayland, the last position in physical pixels relative to the main window.</returns>
    /// <remarks>Wayland does not expose global pointer coordinates. Its window-relative SDL position is scaled to the physical client pixels used by <see cref="WindowGetSize(int)"/> and truncated toward zero.</remarks>
    /// <exception cref="InvalidOperationException">The native Wayland window pixel density cannot be read.</exception>
    public Vector2i MouseGetPosition()
    {
        EnsureOwner();
        if (_waylandWindowPosition)
        {
            SDL.GetMouseState(out var windowX, out var windowY);
            var scale = GetMousePixelScale();
            return new Vector2i((int)(windowX * scale), (int)(windowY * scale));
        }
        SDL.GetGlobalMouseState(out var x, out var y);
        return new Vector2i((int)Mathf.Round(x), (int)Mathf.Round(y));
    }

    /// <summary>Gets the mouse buttons currently reported as held by SDL.</summary>
    /// <returns>A mask of non-wheel buttons.</returns>
    public MouseButtonMask MouseGetButtonState()
    {
        EnsureOwner();
        var flags = SDL.GetMouseState(out _, out _);
        var result = MouseButtonMask.None;
        if ((flags & SDL.MouseButtonFlags.Left) != 0) result |= MouseButtonMask.Left;
        if ((flags & SDL.MouseButtonFlags.Right) != 0) result |= MouseButtonMask.Right;
        if ((flags & SDL.MouseButtonFlags.Middle) != 0) result |= MouseButtonMask.Middle;
        if ((flags & SDL.MouseButtonFlags.X1) != 0) result |= MouseButtonMask.XButton1;
        if ((flags & SDL.MouseButtonFlags.X2) != 0) result |= MouseButtonMask.XButton2;
        return result;
    }

    /// <summary>Requests a pointer move within the main window's client area when the backend supports warping.</summary>
    /// <param name="position">Target coordinates relative to the client area's upper-left corner in native client units.</param>
    /// <remarks>
    /// The request is available only when <see cref="HasFeature(Feature)"/> reports <see cref="Feature.MouseWarp"/>.
    /// On an advertised backend, the platform may still ignore movement under its input or remote-desktop policy.
    /// </remarks>
    /// <exception cref="NotSupportedException">Pointer warping is unavailable on the current backend.</exception>
    public void WarpMouse(Vector2i position)
    {
        EnsureOwner();
        if (!HasFeature(Feature.MouseWarp))
            throw new NotSupportedException("Pointer warping is unavailable on the current display backend.");
        var scale = GetMousePixelScale();
        SDL.WarpMouseInWindow(_window.DangerousGetHandle(), position.X / scale, position.Y / scale);
    }

    internal Vector2 GetClientMousePosition()
    {
        EnsureOwner(); SDL.GetMouseState(out var x, out var y);
        return new Vector2(x, y) * GetMousePixelScale();
    }

    private float GetMousePixelScale()
    {
        if (!_waylandWindowPosition)
            return 1f;
        var scale = SDL.GetWindowPixelDensity(_window.DangerousGetHandle());
        if (!float.IsFinite(scale) || scale <= 0f)
            throw SDLFailure("read the window pixel density");
        return scale;
    }

    /// <summary>Gets the last successfully selected standard pointer shape.</summary>
    /// <returns>The current server-owned shape.</returns>
    public CursorShape CursorGetShape()
    {
        EnsureOwner();
        return _cursorShape;
    }

    /// <summary>Selects a standard pointer shape from the native cursor theme.</summary>
    /// <param name="shape">Shape to apply.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="shape"/> is unknown.</exception>
    public void CursorSetShape(CursorShape shape)
    {
        EnsureOwner();
        var native = MapCursorShape(shape);
        if (_customCursors[(int)shape] is { } custom)
        {
            if (!SDL.SetCursor(custom.DangerousGetHandle()))
                throw SDLFailure("set the custom cursor");
            _cursorShape = shape;
            _cursor?.Dispose();
            _cursor = null;
            return;
        }
        var handle = SDL.CreateSystemCursor(native);
        if (handle == 0)
            throw SDLFailure("create a native cursor");
        var replacement = new SdlCursorHandle(handle);
        GC.SuppressFinalize(replacement);
        if (!SDL.SetCursor(handle))
        {
            replacement.Dispose();
            throw SDLFailure("set the native cursor");
        }
        var previous = _cursor;
        _cursor = replacement;
        _cursorShape = shape;
        previous?.Dispose();
    }

    /// <summary>Sets or clears the image used for one pointer shape.</summary>
    /// <param name="image">A live Image or readable Texture to copy into a native cursor, or <see langword="null"/> to restore the system shape.</param>
    /// <param name="shape">The pointer shape slot to customize.</param>
    /// <param name="hotspot">The active point relative to the image's upper-left corner, truncated to a pixel on native submission.</param>
    /// <remarks>Pixels are copied before this method returns; the resource remains caller-owned. Texture.GetImage
    /// supplies a temporary owned image, which is disposed after conversion. Cursor dimensions and hotspots use
    /// this image's pixels, independently of the texture's logical size override. Images must be at most 256 by 256
    /// pixels. The default hotspot is the top-left pixel. Other cursor slots are unaffected. Later resource changes
    /// require another call to refresh the cursor.</remarks>
    /// <exception cref="ArgumentException">The resource is neither Image nor Texture, has no readable nonempty image, or exceeds the cursor size limit.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The shape or hotspot is invalid.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread or the native cursor operation fails.</exception>
    /// <exception cref="ObjectDisposedException">The display server, source resource or returned image is disposing or disposed.</exception>
    /// <exception cref="NotSupportedException">The image requires an unavailable pixel conversion, including decompression.</exception>
    public void CursorSetCustomImage(Resource? image, CursorShape shape = CursorShape.Arrow,
        Vector2 hotspot = default)
    {
        EnsureOwner();
        if ((uint)shape >= (uint)CursorShape.Max)
            throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unknown cursor shape.");
        if (image is { IsDisposed: true }) throw new ObjectDisposedException(nameof(image));
        using var textureImage = image is Texture texture
            ? texture.GetImage() ?? throw new ArgumentException("The cursor texture has no readable image.", nameof(image))
            : null;
        // Custom texture capture can run user code that closes the display.
        EnsureOwner();
        var pixels = image switch
        {
            null => null,
            Image source => source,
            Texture => textureImage,
            _ => throw new ArgumentException("A cursor resource must be an Image or Texture.", nameof(image)),
        };
        SdlCursorHandle? replacement = null;
        if (pixels is not null)
        {
            if (pixels.IsEmpty)
                throw new ArgumentException("A cursor image must not be empty.", nameof(image));
            if (pixels.Width > 256 || pixels.Height > 256)
                throw new ArgumentException("A cursor image must not exceed 256 by 256 pixels.", nameof(image));
            if (!float.IsFinite(hotspot.X) || !float.IsFinite(hotspot.Y) ||
                hotspot.X < 0 || hotspot.Y < 0 || hotspot.X >= pixels.Width || hotspot.Y >= pixels.Height)
                throw new ArgumentOutOfRangeException(nameof(hotspot), hotspot, "Hotspot must lie inside the image.");
            WithImageSurface(pixels, surface =>
            {
                var handle = SDL.CreateColorCursor(surface, (int)hotspot.X, (int)hotspot.Y);
                if (handle == 0)
                    throw SDLFailure("create a custom cursor");
                replacement = new SdlCursorHandle(handle);
                GC.SuppressFinalize(replacement);
            });
        }

        if (_cursorShape == shape)
        {
            if (replacement is null)
            {
                var system = SDL.CreateSystemCursor(MapCursorShape(shape));
                if (system == 0)
                    throw SDLFailure("create a native cursor");
                replacement = new SdlCursorHandle(system);
                GC.SuppressFinalize(replacement);
                if (!SDL.SetCursor(system))
                {
                    replacement.Dispose();
                    throw SDLFailure("restore the system cursor");
                }
                var oldSystem = _cursor;
                _cursor = replacement;
                oldSystem?.Dispose();
                replacement = null;
            }
            else if (!SDL.SetCursor(replacement.DangerousGetHandle()))
            {
                replacement.Dispose();
                throw SDLFailure("set the custom cursor");
            }
            else
            {
                _cursor?.Dispose();
                _cursor = null;
            }
        }

        var previous = _customCursors[(int)shape];
        _customCursors[(int)shape] = replacement;
        previous?.Dispose();
    }

    private static SDL.SystemCursor MapCursorShape(CursorShape shape) => shape switch
    {
        CursorShape.Arrow => SDL.SystemCursor.Default,
        CursorShape.IBeam => SDL.SystemCursor.Text,
        CursorShape.PointingHand => SDL.SystemCursor.Pointer,
        CursorShape.Cross => SDL.SystemCursor.Crosshair,
        CursorShape.Wait => SDL.SystemCursor.Progress,
        CursorShape.Busy => SDL.SystemCursor.Wait,
        CursorShape.Drag => SDL.SystemCursor.Grabbing,
        CursorShape.CanDrop => SDL.SystemCursor.Copy,
        CursorShape.Forbidden => SDL.SystemCursor.NoDrop,
        CursorShape.VSize => SDL.SystemCursor.NSResize,
        CursorShape.HSize => SDL.SystemCursor.EWResize,
        CursorShape.BDiagSize => SDL.SystemCursor.NESWResize,
        CursorShape.FDiagSize => SDL.SystemCursor.NWSEResize,
        CursorShape.Move => SDL.SystemCursor.Move,
        CursorShape.VSplit => SDL.SystemCursor.RowResize,
        CursorShape.HSplit => SDL.SystemCursor.ColResize,
        CursorShape.Help => SDL.SystemCursor.Help,
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unknown cursor shape."),
    };

    private void ApplyMouseMode(MouseMode mode)
    {
        var window = _window.DangerousGetHandle();
        if (!SDL.SetWindowRelativeMouseMode(window, mode == MouseMode.Captured) ||
            !SDL.SetWindowMouseGrab(window, mode is MouseMode.Confined or MouseMode.ConfinedHidden))
            throw SDLFailure("change pointer capture");
        var success = mode is MouseMode.Visible or MouseMode.Confined ? SDL.ShowCursor() : SDL.HideCursor();
        if (!success)
            throw SDLFailure("change pointer visibility");
    }

    private void ReleasePointer()
    {
        _ = SDL.SetWindowRelativeMouseMode(_window.DangerousGetHandle(), false);
        _ = SDL.SetWindowMouseGrab(_window.DangerousGetHandle(), false);
        _ = SDL.ShowCursor();
        _ = SDL.SetCursor(SDL.GetDefaultCursor());
        _cursor?.Dispose();
        _cursor = null;
        foreach (var cursor in _customCursors)
            cursor?.Dispose();
    }

    private sealed class SdlCursorHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SdlCursorHandle(nint handle) : base(ownsHandle: true) => SetHandle(handle);

        protected override bool ReleaseHandle()
        {
            SDL.DestroyCursor(handle);
            return true;
        }
    }
}
