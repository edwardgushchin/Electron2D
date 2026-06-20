using System.Numerics;

namespace Electron2D;

/// <summary>
/// Глобальный фасад для чтения состояния ввода.
/// </summary>
/// <remarks>
/// Жизненный цикл привязан к <see cref="Engine"/>: <c>Bind</c> вызывается при старте, <c>Unbind</c> — при завершении.
/// Пока система не привязана, методы возвращают <see langword="false"/> / default.
/// </remarks>
public static class Input
{
    private static InputSystem? _system;

    internal static void Bind(InputSystem system) => _system = system;
    internal static void Unbind() => _system = null;

    #region Keyboard

    /// <summary>Клавиша удерживается (down/held).</summary>
    public static bool IsKeyDown(KeyCode keyCode)
        => _system is not null && _system.IsKeyDown((int)keyCode);

    /// <summary>Клавиша нажата в этом кадре (edge: up → down).</summary>
    public static bool IsKeyPressed(KeyCode keyCode)
        => _system is not null && _system.IsKeyPressed((int)keyCode);

    /// <summary>Клавиша отпущена в этом кадре (edge: down → up).</summary>
    public static bool IsKeyUp(KeyCode keyCode)
        => _system is not null && _system.IsKeyUp((int)keyCode);

    #endregion

    #region Mouse

    public static Vector2 MousePosition => _system?.MousePosition ?? default;
    
    public static Vector2 MouseWorldPosition => _system?.MouseWorldPosition ?? default;
    
    public static bool MousePresent => _system?.MousePresent ?? false;
    public static Vector2 MouseScrollDelta => _system?.MouseScrollDelta ?? default;
    
    public static Vector2 MouseDelta => _system?.MouseDelta ?? default;

    /// <summary>Кнопка мыши удерживается (down/held).</summary>
    public static bool IsMouseButtonDown(MouseButton button)
        => _system is not null && _system.GetMouseButton(button);

    /// <summary>Кнопка мыши нажата в этом кадре (edge: up → down).</summary>
    public static bool IsMouseButtonPressed(MouseButton button)
        => _system is not null && _system.GetMouseButtonDown(button);

    /// <summary>Кнопка мыши отпущена в этом кадре (edge: down → up).</summary>
    public static bool IsMouseButtonUp(MouseButton button)
        => _system is not null && _system.GetMouseButtonUp(button);

    #endregion
}
