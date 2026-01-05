namespace Electron2D;

/// <summary>
/// Глобальный фасад для чтения состояния ввода.
/// </summary>
/// <remarks>
/// Жизненный цикл привязан к <see cref="Engine"/>: <c>Bind</c> вызывается при старте, <c>Unbind</c> — при завершении.
/// Пока система не привязана, методы возвращают <see langword="false"/>.
/// </remarks>
public static class Input
{
    private static InputSystem? _system;

    internal static void Bind(InputSystem system) => _system = system;

    internal static void Unbind() => _system = null;

    /// <summary>
    /// Возвращает <see langword="true"/>, если клавиша удерживается на текущем кадре.
    /// </summary>
    public static bool IsKeyDown(KeyCode keyCode) => _system is not null && _system.IsKeyDown((int)keyCode);

    /// <summary>
    /// Возвращает <see langword="true"/>, если клавиша была нажата на текущем кадре (edge-triggered).
    /// </summary>
    public static bool IsKeyPressed(KeyCode keyCode) => _system is not null && _system.IsKeyPressed((int)keyCode);

    /// <summary>
    /// Возвращает <see langword="true"/>, если клавиша отпущена на текущем кадре.
    /// </summary>
    public static bool IsKeyUp(KeyCode keyCode) => _system is not null && _system.IsKeyUp((int)keyCode);
    
    public static MouseButton GetMouseButton(out float x, out float y) => _system!.GetMouseButton(out x, out y);
}