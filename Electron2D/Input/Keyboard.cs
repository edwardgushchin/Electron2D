using SDL3;

namespace Electron2D.Input;

public static class Keyboard
{
    private static readonly bool[] PrevState = new bool[(int)SDL.Scancode.Count];
    private static readonly bool[] CurrentState = new bool[(int)SDL.Scancode.Count];

    internal static void Update()
    {
        CurrentState.CopyTo(PrevState, 0);
        SDL.GetKeyboardState(out _).CopyTo(CurrentState, 0);
    }

    public static bool GetKeyDown(Keycode keycode)
    {
        return CurrentState[(int)keycode];
    }

    public static bool GetKeyUp(Keycode keycode)
    {
        return !CurrentState[(int)keycode] && PrevState[(int)keycode];
    }
}