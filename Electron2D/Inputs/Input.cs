using SDL3;

namespace Electron2D.Inputs;

public static class Input
{
    private static bool[] _prevState = new bool[(int)SDL.Scancode.Count];
    private static bool[] _currentState = new bool[(int)SDL.Scancode.Count];
    
    private static SDL.MouseButtonFlags _currentButtons;
    private static SDL.MouseButtonFlags _prevButtons;
    
    private static Vector2 _mousePosition;

    // Вызывайте в начале каждого кадра
    internal static void UpdateState()
    {
        var currentStateSpan = SDL.GetKeyboardState(out var numkeys);
        
        _prevButtons = _currentButtons;
        _currentButtons = SDL.GetMouseState(out var mouseX, out var mouseY);
        
        _mousePosition.X = mouseX;
        _mousePosition.Y = mouseY;
        
        // Проверяем размер массива ДО копирования
        if (_currentState.Length != numkeys)
        {
            _currentState = new bool[numkeys];
            _prevState = new bool[numkeys];
        }

        // Сохраняем прошлое состояние
        Array.Copy(_currentState, _prevState, numkeys);

        // Копируем текущее состояние
        currentStateSpan.CopyTo(_currentState);
    }

    public static bool GetKeyDown(Scancode keycode)
    {
        var currentState = SDL.GetKeyboardState(out _);
        return currentState[(int)keycode];
    }

    public static bool GetKeyUp(Scancode keycode)
    {
        var currentState = SDL.GetKeyboardState(out _);
        return !currentState[(int)keycode] && _prevState[(int)keycode];
    }

    // Перегрузки для string (например, по имени клавиши)
    public static bool GetKeyDown(string keyName)
    {
        return TryGetScancode(keyName, out var sc) && GetKeyDown(sc);
    }

    public static bool GetKeyUp(string keyName)
    {
        return TryGetScancode(keyName, out var sc) && GetKeyUp(sc);
    }
    
    /// <summary>
    /// Возвращает true, если клавиша нажата в этом кадре
    /// (переход из отпущено -> нажато)
    /// </summary>
    public static bool IsKeyPressed(Scancode keycode)
    {
        return _currentState[(int)keycode] && !_prevState[(int)keycode];
    }

    /// <summary>
    /// Возвращает true, если клавиша отпущена в этом кадре
    /// (переход из нажато -> отпущено)
    /// </summary>
    public static bool IsKeyReleased(Scancode keycode)
    {
        return !_currentState[(int)keycode] && _prevState[(int)keycode];
    }

    private static bool TryGetScancode(string keyName, out Scancode scancode)
    {
        return Enum.TryParse(keyName, true, out scancode);
    }
    
    public static bool GetMouseButton(MouseButtonFlags button)
    {
        return (_currentButtons & (SDL.MouseButtonFlags)button) != 0;
    }

    public static bool GetMouseButtonDown(MouseButtonFlags button)
    {
        var b = (SDL.MouseButtonFlags)button;
        return (_currentButtons & b) != 0 && (_prevButtons & b) == 0;
    }

    public static bool GetMouseButtonUp(MouseButtonFlags button)
    {
        var b = (SDL.MouseButtonFlags)button;
        return (_currentButtons & b) == 0 && (_prevButtons & b) != 0;
    }
    
    public static Vector2 GetMousePosition() =>  _mousePosition;
}