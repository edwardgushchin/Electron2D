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

    private float _currentMouseX;
    private float _currentMouseY;
    private float _previousMouseX;
    private float _previousMouseY;

    private bool _mousePresent;
    private Vector2 _mouseScrollDelta;

    #endregion

    #region Unity-style public API (mouse)

    /// <summary>Текущее положение мыши в пикселях, относительно top-left фокусного окна.</summary>
    public Vector2 MousePosition => new(_currentMouseX, _currentMouseY);

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
        _previousMouseX = _currentMouseX;
        _previousMouseY = _currentMouseY;

        // MousePresent (SDL_HasMouse) :contentReference[oaicite:4]{index=4}
        _mousePresent = SDL.HasMouse();

        // Mouse wheel (только из событий): сбрасываем и берём accumulated из EventSystem.
        _mouseScrollDelta = eventSystem.MouseWheelDelta;

        // Mouse buttons + position (cached state after PumpEvents) :contentReference[oaicite:5]{index=5}
        var sdlButtons = SDL.GetMouseState(out var mouseX, out var mouseY);
        _currentMouseButtons = (MouseButton)(uint)sdlButtons;
        _currentMouseX = mouseX;
        _currentMouseY = mouseY;
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
