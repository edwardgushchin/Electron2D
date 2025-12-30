using System;

namespace Electron2D;

public static class Input
{
    private static InputSystem? _system;

    internal static void Bind(InputSystem system) => _system = system;
    internal static void Unbind() => _system = null;

    public static bool IsKeyDown(KeyCode keyCode) => _system is not null && _system.IsKeyDown((int)keyCode);

    public static bool IsKeyPressed(KeyCode keyCode) => _system is not null && _system.IsKeyPressed((int)keyCode);

    public static bool IsKeyUp(KeyCode keyCode) => _system is not null && _system.IsKeyUp((int)keyCode);
}
