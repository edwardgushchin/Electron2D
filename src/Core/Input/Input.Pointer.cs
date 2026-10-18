namespace Electron2D;

public sealed partial class Input
{
    private int _defaultCursorShape;

    internal CursorShape DefaultCursorShape => (CursorShape)Volatile.Read(ref _defaultCursorShape);

    internal Vector2 GetLastMousePositionCore() { lock (_gate) return _lastMousePosition; }

    internal MouseMode MouseModeCore
    {
        get => RequireDisplay().MouseGetModeCore();
        set => RequireDisplay().MouseSetModeCore(value);
    }

    internal CursorShape GetCurrentCursorShapeCore() => RequireDisplay().CursorGetShapeCore();

    internal void SetDefaultCursorShapeCore(CursorShape shape = CursorShape.Arrow)
    {
        var display = RequireDisplay();
        if (shape < CursorShape.Arrow || shape >= CursorShape.Max) throw new ArgumentOutOfRangeException(nameof(shape));
        _ = display.CursorGetShapeCore();
        if (DefaultCursorShape == shape) return;
        display.CursorSetShapeCore(shape);
        Volatile.Write(ref _defaultCursorShape, (int)shape);
        lock (_gate) _lastMouseVelocity = _lastMouseScreenVelocity = Vector2.Zero;
        if (Engine.MainLoop is SceneTree { Root: Window window } tree)
        {
            tree.RefreshGUICursor();
            tree.RequestMouseCursorRefresh(window, globalInput: true);
        }

    }

    internal InputEventMouseMotion CreateCursorRefreshEvent()
    {
        lock (_gate)
        {
            _lastMouseVelocity = _lastMouseScreenVelocity = Vector2.Zero;
            return new InputEventMouseMotion
            {
                Device = InputEvent.DeviceIdInternal,
                WindowID = _lastMouseWindowID,
                Position = _lastMousePosition,
                GlobalPosition = _lastMousePosition
            };
        }
    }

    internal void SetCustomMouseCursorCore(Resource? image, CursorShape shape = CursorShape.Arrow, Vector2 hotspot = default) =>
    RequireDisplay().CursorSetCustomImageCore(image, shape, hotspot);

    internal void WarpMouseCore(Vector2 position)
    {
        if (!position.IsFinite() || (double)position.X < int.MinValue || (double)position.X > int.MaxValue ||
            (double)position.Y < int.MinValue || (double)position.Y > int.MaxValue)
            throw new ArgumentException("Pointer coordinates must be finite and within native integer limits.", nameof(position));
        RequireDisplay().WarpMouseCore(new Vector2i((int)position.X, (int)position.Y));
    }

    private static DisplayServer RequireDisplay() => DisplayServer.Service ??
        throw new InvalidOperationException("Pointer control requires an active display server.");
}
