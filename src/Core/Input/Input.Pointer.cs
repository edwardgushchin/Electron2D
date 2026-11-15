namespace Electron2D;

public sealed partial class Input
{
    private struct PointerVelocity
    {
        internal Vector2 Local, Screen;
        private long _tick;
        private Vector2 _localAccum, _screenAccum;
        private float _time;

        public PointerVelocity() { this = default; _tick = System.Diagnostics.Stopwatch.GetTimestamp(); }

        internal void Update(Vector2 relative, Vector2 screenRelative)
        {
            var tick = System.Diagnostics.Stopwatch.GetTimestamp();
            var elapsed = (float)System.Diagnostics.Stopwatch.GetElapsedTime(_tick, tick).TotalSeconds;
            _tick = tick;
            if (elapsed > 3f)
            {
                Local = Screen = Vector2.Zero;
                _localAccum = relative;
                _screenAccum = screenRelative;
                _time = 0;
                return;
            }
            _localAccum += relative;
            _screenAccum += screenRelative;
            _time += elapsed;
            if (_time < 0.1f) return;
            Local = _localAccum / _time;
            Screen = _screenAccum / _time;
            _localAccum = _screenAccum = Vector2.Zero;
            _time = 0;
        }
    }

    private InputEventScreenDrag? CalibrateTouchMotion(InputEvent source)
    {
        if (source is not InputEventScreenDrag drag) return null;
        var copy = (InputEventScreenDrag)drag.Duplicate();
        try
        {
            lock (_gate)
            {
                var key = (drag.Device, drag.Index);
                if (!_touchVelocities.TryGetValue(key, out var velocity)) velocity = new PointerVelocity();
                velocity.Update(drag.Relative, drag.ScreenRelative);
                _touchVelocities[key] = velocity;
                copy.Velocity = velocity.Local;
                copy.ScreenVelocity = velocity.Screen;
            }
            return copy;
        }
        catch { copy.Dispose(); throw; }
    }

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
        lock (_gate) _mouseVelocity = new PointerVelocity();
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
            _mouseVelocity = new PointerVelocity();
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
