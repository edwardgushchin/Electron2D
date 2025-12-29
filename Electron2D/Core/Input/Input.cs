using System;

namespace Electron2D;

public static class Input
{
    private static InputSystem? _sys;

    internal static void Bind(InputSystem sys) => _sys = sys;
    internal static void Unbind() => _sys = null;

    public static bool IsKeyDown(Key key)  => _sys is not null && _sys.IsKeyDown((int)key);
    public static bool IsKeyPressed(Key key) => _sys is not null && _sys.IsKeyPressed((int)key);
    public static bool IsKeyUp(Key key)    => _sys is not null && _sys.IsKeyUp((int)key);

    [Obsolete("Use IsKeyPressed(...) instead.")]
    public static bool IsKeyPress(Key key) => IsKeyPressed(key);
}
