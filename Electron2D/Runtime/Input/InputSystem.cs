using SDL3;

namespace Electron2D;

internal sealed class InputSystem
{
    private bool[] _currentKeys = [];
    private bool[] _previousKeys = [];

    public void Initialize() { }

    public void Shutdown() { }

    public void BeginFrame(EventSystem events)
    {
        // SDL_GetKeyboardState требует PumpEvents; он уже сделан в EventSystem.BeginFrame().
        var state = SDL.GetKeyboardState(out var numKeys);

        if (_currentKeys.Length != numKeys)
        {
            _currentKeys = new bool[numKeys];
            _previousKeys = new bool[numKeys];
        }
        else
        {
            // swap buffers: prev <- keys, keys <- prev
            (_currentKeys, _previousKeys) = (_previousKeys, _currentKeys);
        }

        state.CopyTo(_currentKeys);

        // Генерация событий без SDL-типов наружу.
        // Важно: публикуем ДО EventSystem.EndFrame(), чтобы оно попало в read-буфер этого кадра.
        var ch = events.Events.Input;

        for (var i = 0; i < numKeys; i++)
        {
            var now = _currentKeys[i];
            var was = _previousKeys[i];
            if (now == was) continue;

            ch.TryPublish(new InputEvent(now ? InputEventType.KeyDown : InputEventType.KeyUp, code: i));
        }
    }

    public bool IsKeyDown(int scancode)
        => (uint)scancode < (uint)_currentKeys.Length && _currentKeys[scancode];

    public bool IsKeyPressed(int scancode)
        => (uint)scancode < (uint)_currentKeys.Length && _currentKeys[scancode] && !_previousKeys[scancode];

    public bool IsKeyUp(int scancode)
        => (uint)scancode < (uint)_currentKeys.Length && !_currentKeys[scancode] && _previousKeys[scancode];

    public bool IsKeyPress(int scancode)
        => IsKeyPressed(scancode);
}
