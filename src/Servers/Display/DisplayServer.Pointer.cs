using Microsoft.Win32.SafeHandles;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private MouseMode _mouseMode;
    private DisplayServer? _mouseModeWindow;
    private Vector2 _lastClientMousePosition;
    private CursorShape _cursorShape;
    private readonly SdlCursorHandle?[] _systemCursors = new SdlCursorHandle?[(int)CursorShape.Max];
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
            var context = NativeContext(SDL.GetMouseFocus()) ?? this;
            var scale = context.GetMousePixelScale();
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
        (NativeContext(SDL.GetMouseFocus()) ?? FocusedWindow() ?? this).WarpClientMouse(position);
    }

    internal void WarpClientMouse(Vector2i position)
    {
        EnsureOwner();
        if (!HasFeatureCore(Feature.MouseWarp))
            throw new NotSupportedException("Pointer warping is unavailable on the current display backend.");
        var scale = GetMousePixelScale();
        SDL.WarpMouseInWindow(_window.DangerousGetHandle(), position.X / scale, position.Y / scale);
    }

    internal Vector2 GetClientMousePosition()
    {
        EnsureOwner();
        if (SDL.GetMouseFocus() != _window.DangerousGetHandle()) return _lastClientMousePosition;
        SDL.GetMouseState(out var x, out var y);
        return _lastClientMousePosition = new Vector2(x, y) * GetMousePixelScale();
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
        _ = MapCursorShape(shape);
        if (_customCursors[(int)shape] is { } custom)
        {
            if (!SDL.SetCursor(custom.DangerousGetHandle()))
                throw SDLFailure("set the custom cursor");
            _cursorShape = shape;
            return;
        }
        var replacement = GetSystemCursor(shape);
        if (!SDL.SetCursor(replacement.DangerousGetHandle()))
            throw SDLFailure("set the native cursor");
        _cursorShape = shape;
    }

    private SdlCursorHandle GetSystemCursor(CursorShape shape)
    {
        if (_systemCursors[(int)shape] is { } cached) return cached;
        var handle = OperatingSystem.IsWindows() && shape == CursorShape.Help
            ? CreateWindowsHelpCursor() : SDL.CreateSystemCursor(MapCursorShape(shape));
        if (handle == 0) throw SDLFailure("create a native cursor");
        var cursor = new SdlCursorHandle(handle);
        GC.SuppressFinalize(cursor);
        return _systemCursors[(int)shape] = cursor;
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
                var system = GetSystemCursor(shape);
                if (!SDL.SetCursor(system.DangerousGetHandle()))
                    throw SDLFailure("restore the system cursor");
            }
            else if (!SDL.SetCursor(replacement.DangerousGetHandle()))
            {
                replacement.Dispose();
                throw SDLFailure("set the custom cursor");
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
        CursorShape.Drag => OperatingSystem.IsWindows() ? SDL.SystemCursor.Move : SDL.SystemCursor.Grabbing,
        CursorShape.CanDrop => OperatingSystem.IsWindows() ? SDL.SystemCursor.Default : SDL.SystemCursor.Copy,
        CursorShape.Forbidden => OperatingSystem.IsWindows() ? SDL.SystemCursor.NotAllowed : SDL.SystemCursor.NoDrop,
        CursorShape.VSize => SDL.SystemCursor.NSResize,
        CursorShape.HSize => SDL.SystemCursor.EWResize,
        CursorShape.BDiagSize => SDL.SystemCursor.NESWResize,
        CursorShape.FDiagSize => SDL.SystemCursor.NWSEResize,
        CursorShape.Move => SDL.SystemCursor.Move,
        CursorShape.VSplit => OperatingSystem.IsWindows() ? SDL.SystemCursor.NSResize : SDL.SystemCursor.RowResize,
        CursorShape.HSplit => OperatingSystem.IsWindows() ? SDL.SystemCursor.EWResize : SDL.SystemCursor.ColResize,
        CursorShape.Help => SDL.SystemCursor.Help,
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unknown cursor shape."),
    };

    private void ApplyMouseMode(MouseMode mode)
    {
        var context = FocusedWindow() ?? this;
        if (_mouseModeWindow is { IsDisposed: false } previous && !ReferenceEquals(previous, context))
        {
            if (!SDL.SetWindowRelativeMouseMode(previous._window.DangerousGetHandle(), false) ||
                !SDL.SetWindowMouseGrab(previous._window.DangerousGetHandle(), false))
                throw SDLFailure("release previous pointer capture");
        }
        var window = context._window.DangerousGetHandle();
        if (!SDL.SetWindowRelativeMouseMode(window, mode == MouseMode.Captured) ||
            !SDL.SetWindowMouseGrab(window, mode is MouseMode.Confined or MouseMode.ConfinedHidden))
            throw SDLFailure("change pointer capture");
        var success = mode is MouseMode.Visible or MouseMode.Confined ? SDL.ShowCursor() : SDL.HideCursor();
        if (!success)
            throw SDLFailure("change pointer visibility");
        _mouseModeWindow = context;
    }

    private void ReleasePointer()
    {
        var context = _mouseModeWindow is { IsDisposed: false } captured ? captured : this;
        _ = SDL.SetWindowRelativeMouseMode(context._window.DangerousGetHandle(), false);
        _ = SDL.SetWindowMouseGrab(context._window.DangerousGetHandle(), false);
        _mouseModeWindow = null;
        _ = SDL.ShowCursor();
        _ = SDL.SetCursor(SDL.GetDefaultCursor());
        foreach (var cursor in _systemCursors)
            cursor?.Dispose();
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
