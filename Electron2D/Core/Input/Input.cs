namespace Electron2D;

public static class Input
{
    private static InputSystem? _system;

    internal static void Bind(InputSystem system) => _system = system;
    internal static void Unbind() => _system = null;

    public static bool IsKeyDown(Key key) => _system is not null && _system.IsKeyDown((int)key);

    public static bool IsKeyPressed(Key key) => _system is not null && _system.IsKeyPressed((int)key);

    [Obsolete("Use IsKeyPressed instead.")]
    public static bool IsKeyPress(Key key) => IsKeyPressed(key);

    public static bool IsKeyUp(Key key) => _system is not null && _system.IsKeyUp((int)key);
}
