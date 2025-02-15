using SDL3;

namespace Electron2D.Input;

public static class Mouse
{
    private static Vector2 _mousePosition = new(0, 0);
    private static MouseButtonFlags _currentButtonFlags = MouseButtonFlags.None;
    private static MouseButtonFlags _prevMouseButtonFlags = MouseButtonFlags.None;
    
    internal static void Update()
    {
        _prevMouseButtonFlags = _currentButtonFlags;
        _currentButtonFlags = (MouseButtonFlags)SDL.GetMouseState(out _mousePosition.X, out _mousePosition.Y);
    }

    public static bool GetButtonDown(MouseButton button)
    {
        return _currentButtonFlags.HasFlag((MouseButtonFlags)button);
    }

    public static bool GetButtonUp(MouseButton button)
    {
        return (!_currentButtonFlags.HasFlag((MouseButtonFlags)button) && _prevMouseButtonFlags.HasFlag((MouseButtonFlags)button));
    }
    
    public static Vector2 Position => _mousePosition;
}