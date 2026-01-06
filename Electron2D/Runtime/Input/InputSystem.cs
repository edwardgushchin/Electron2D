using System.Numerics;
using SDL3;

namespace Electron2D;

#region InputSystem

/// <summary>
/// Система состояния клавиатуры (опрос через SDL_GetKeyboardState) с хранением текущего и предыдущего кадров.
/// + Unity-style мышь: MousePosition, MousePresent, MouseScrollDelta, GetMouseButton*.
/// </summary>
internal sealed class InputSystem
{
    #region Instance fields

    private bool[] _currentKeys = [];
    private bool[] _previousKeys = [];

    private MouseButton _currentMouseButtons;
    private MouseButton _previousMouseButtons;

    private Vector2 _currentMouse;
    private Vector2 _previousMouse;
    
    private Vector2 _mouseDelta;
    private Vector2 _mouseScrollDelta;
    
    private bool _mousePresent;

    #endregion

    #region public API (mouse)

    /// <summary>Текущее положение мыши в пикселях, относительно top-left фокусного окна.</summary>
    public Vector2 MousePosition => _currentMouse;
    
    /// <summary>Относительное движение мыши за кадр (dx/dy), накопленное SDL с прошлого вызова.</summary>
    public Vector2 MouseDelta => _mouseDelta;

    /// <summary>Есть ли подключенная мышь.</summary>
    public bool MousePresent => _mousePresent; // SDL_HasMouse :contentReference[oaicite:3]{index=3}

    /// <summary>Дельта колеса за кадр (accumulated). Сбрасывается каждый BeginFrame().</summary>
    public Vector2 MouseScrollDelta => _mouseScrollDelta;

    /// <summary>Кнопка удерживается (down).</summary>
    public bool GetMouseButton(MouseButton button)
        => button != MouseButton.None && (_currentMouseButtons & button) != 0;

    /// <summary>Кнопка нажата в этом кадре (edge: up -> down).</summary>
    public bool GetMouseButtonDown(MouseButton button)
        => button != MouseButton.None
           && (_currentMouseButtons & button) != 0
           && (_previousMouseButtons & button) == 0;

    /// <summary>Кнопка отпущена в этом кадре (edge: down -> up).</summary>
    public bool GetMouseButtonUp(MouseButton button)
        => button != MouseButton.None
           && (_currentMouseButtons & button) == 0
           && (_previousMouseButtons & button) != 0;

    // Перегрузки “как в Unity” (0=Left, 1=Right, 2=Middle; доп.: 3=X1, 4=X2)
    public bool GetMouseButton(int button) => GetMouseButton(ToMouseButton(button));
    public bool GetMouseButtonDown(int button) => GetMouseButtonDown(ToMouseButton(button));
    public bool GetMouseButtonUp(int button) => GetMouseButtonUp(ToMouseButton(button));

    #endregion

    #region Keyboard public API

    public void Initialize() { }
    public void Shutdown() { }

    public void BeginFrame(EventSystem eventSystem)
    {
        ArgumentNullException.ThrowIfNull(eventSystem);

        // SDL_GetKeyboardState требует PumpEvents; он уже сделан в EventSystem.BeginFrame().
        var keyboardState = SDL.GetKeyboardState(out var numKeys);

        EnsureKeyBuffers(numKeys);

        for (var scancode = 0; scancode < numKeys; scancode++)
        {
            _currentKeys[scancode] = keyboardState[scancode];
        }

        // Mouse: переносим current -> previous
        _previousMouseButtons = _currentMouseButtons;
        _previousMouse = _currentMouse;

        // MousePresent (SDL_HasMouse) :contentReference[oaicite:4]{index=4}
        _mousePresent = SDL.HasMouse();

        // Mouse wheel (только из событий): сбрасываем и берём accumulated из EventSystem.
        _mouseScrollDelta = eventSystem.MouseWheelDelta;

        // Mouse buttons + position (cached state after PumpEvents) :contentReference[oaicite:5]{index=5}
        var sdlButtons = SDL.GetMouseState(out var mouseX, out var mouseY);
        SDL.GetRelativeMouseState(out var dx, out var dy);
        _mouseDelta = new Vector2(dx, dy);
        _currentMouseButtons = (MouseButton)(uint)sdlButtons;
        _currentMouse = new(mouseX, mouseY);
    }

    /// <summary>Клавиша сейчас удерживается (down).</summary>
    public bool IsKeyDown(int scancode) => IsValidScancode(scancode) && _currentKeys[scancode];

    /// <summary>Клавиша нажата в этом кадре (edge: up -> down).</summary>
    public bool IsKeyPressed(int scancode)
        => IsValidScancode(scancode) && _currentKeys[scancode] && !_previousKeys[scancode];

    /// <summary>Клавиша отпущена в этом кадре (edge: down -> up).</summary>
    public bool IsKeyUp(int scancode)
        => IsValidScancode(scancode) && !_currentKeys[scancode] && _previousKeys[scancode];

    #endregion

    #region Private helpers

    private void EnsureKeyBuffers(int numKeys)
    {
        if (_currentKeys.Length != numKeys)
        {
            _currentKeys = new bool[numKeys];
            _previousKeys = new bool[numKeys];
            return;
        }

        // Swap buffers: previous <- current, current <- previous.
        (_currentKeys, _previousKeys) = (_previousKeys, _currentKeys);
    }

    private bool IsValidScancode(int scancode) => (uint)scancode < (uint)_currentKeys.Length;

    private static MouseButton ToMouseButton(int button) => button switch
    {
        0 => MouseButton.Left,
        1 => MouseButton.Right,
        2 => MouseButton.Middle,
        3 => MouseButton.X1,
        4 => MouseButton.X2,
        _ => MouseButton.None,
    };

    #endregion
}

#endregion
