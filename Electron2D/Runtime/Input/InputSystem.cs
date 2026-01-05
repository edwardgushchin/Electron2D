using SDL3;

namespace Electron2D;

#region InputSystem

/// <summary>
/// Система состояния клавиатуры (опрос через SDL_GetKeyboardState) с хранением текущего и предыдущего кадров.
/// </summary>
internal sealed class InputSystem
{
    #region Instance fields

    private bool[] _currentKeys = [];
    private bool[] _previousKeys = [];

    private MouseButton _currentState;
    private MouseButton _previousState;
    
    private float _currentMouseX;
    private float _currentMouseY;
    private float _previousMouseX;
    private float _previousMouseY;

    #endregion

    #region Public API

    public void Initialize()
    {
    }

    public void Shutdown()
    {
    }

    public void BeginFrame(EventSystem eventSystem)
    {
        ArgumentNullException.ThrowIfNull(eventSystem);

        // SDL_GetKeyboardState требует PumpEvents; он уже сделан в EventSystem.BeginFrame().
        var keyboardState = SDL.GetKeyboardState(out var numKeys);
        var mouseState = SDL.GetMouseState(out var mouseX,  out var mouseY);

        EnsureKeyBuffers(numKeys);

        for (var scancode = 0; scancode < numKeys; scancode++)
        {
            var isDownNow = keyboardState[scancode];
            _currentKeys[scancode] = isDownNow;
        }
        
        
    }

    /// <summary>Клавиша сейчас удерживается (down).</summary>
    public bool IsKeyDown(int scancode) => IsValidScancode(scancode) && _currentKeys[scancode];

    /// <summary>Клавиша нажата в этом кадре (edge: up -> down).</summary>
    public bool IsKeyPressed(int scancode)
        => IsValidScancode(scancode) && _currentKeys[scancode] && !_previousKeys[scancode];

    /// <summary>Клавиша отпущена в этом кадре (edge: down -> up).</summary>
    public bool IsKeyUp(int scancode)
        => IsValidScancode(scancode) && !_currentKeys[scancode] && _previousKeys[scancode];

    public MouseButton GetMouseButton(out float x, out float y)
    {
        x = 0;
        y = 0;
        return MouseButton.None;
    }
        //=> IsValidScancode(scancode) && !_currentKeys[scancode] && _previousKeys[scancode];

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

    #endregion
}

#endregion
