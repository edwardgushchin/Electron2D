using System;
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

        var ch = events.Events.Input;

        for (var i = 0; i < numKeys; i++)
        {
            var now = state[i];
            _keys[i] = now;

            var was = _prev[i];
            if (now == was) continue;

            if (!ch.TryPublish(new InputEvent(now ? InputEventType.KeyDown : InputEventType.KeyUp, code: i)))
            {
                // Минимальная измеримость: если input-канал забит, события теряются.
                // (Если хотите — можно прокинуть счётчик наружу через ProfilerSystem позже.)
            }
        }
    }

    public bool IsKeyDown(int scancode)
        => IsValidScancode(scancode) && _keys[scancode];

    public bool IsKeyPressed(int scancode)
        => IsValidScancode(scancode) && _keys[scancode] && !_prev[scancode];

    public bool IsKeyUp(int scancode)
        => IsValidScancode(scancode) && !_keys[scancode] && _prev[scancode];

    private bool IsValidScancode(int scancode)
        => (uint)scancode < (uint)_keys.Length;
}
