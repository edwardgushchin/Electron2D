using SDL3;

namespace Electron2D;

internal sealed class InputSystem
{
    private bool[] _keys = [];
    private bool[] _prev = [];

    public void Initialize() { }

    public void Shutdown() { }

    public void BeginFrame(EventSystem events)
    {
        // SDL_GetKeyboardState требует PumpEvents; он уже сделан в EventSystem.BeginFrame().
        var state = SDL.GetKeyboardState(out var numKeys);

        if (_keys.Length != numKeys)
        {
            _keys = new bool[numKeys];
            _prev = new bool[numKeys];
        }
        else
        {
            // swap buffers: prev <- keys, keys <- prev
            (_keys, _prev) = (_prev, _keys);
        }

        state.CopyTo(_keys);
    }

    public bool IsKeyDown(int scancode)
        => (uint)scancode < (uint)_keys.Length && _keys[scancode];

    public bool IsKeyPress(int scancode)
        => (uint)scancode < (uint)_keys.Length && _keys[scancode] && !_prev[scancode];

    public bool IsKeyUp(int scancode)
        => (uint)scancode < (uint)_keys.Length && !_keys[scancode] && _prev[scancode];
}