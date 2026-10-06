using Microsoft.Win32.SafeHandles;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private MouseMode _mouseMode;
    private CursorShape _cursorShape;
    private SdlCursorHandle? _cursor;
    private readonly SdlCursorHandle?[] _customCursors = new SdlCursorHandle?[(int)CursorShape.Max];

    internal MouseMode MouseGetModeCore()
    {
        EnsureOwner();
        return _mouseMode;
    }

    internal void MouseSetModeCore(MouseMode mode)
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

    internal Vector2i MouseGetPositionCore()
    {
        EnsureOwner();
        if (_pixelWindowCoordinates)
        {
            SDL.GetMouseState(out var windowX, out var windowY);
            var scale = GetMousePixelScale();
            return new Vector2i((int)(windowX * scale), (int)(windowY * scale));
        }
        SDL.GetGlobalMouseState(out var x, out var y);
        return new Vector2i((int)Mathf.Round(x), (int)Mathf.Round(y));
    }

    internal MouseButtonMask MouseGetButtonStateCore()
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

    internal void WarpMouseCore(Vector2i position)
    {
        EnsureOwner();
        if (!HasFeatureCore(Feature.MouseWarp))
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
        if (!_pixelWindowCoordinates)
            return 1f;
        var scale = SDL.GetWindowPixelDensity(_window.DangerousGetHandle());
        if (!float.IsFinite(scale) || scale <= 0f)
            throw SDLFailure("read the window pixel density");
        return scale;
    }

    internal CursorShape CursorGetShapeCore()
    {
        EnsureOwner();
        return _cursorShape;
    }

    internal void CursorSetShapeCore(CursorShape shape)
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

    internal void CursorSetCustomImageCore(Resource? image, CursorShape shape = CursorShape.Arrow,
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
