using Electron2D;
using SDL3;

internal static class DisplayServerTaskbarProgressNativeTests
{
    public static void Run(DisplayServer display)
    {
        if (DisplayServer.GetName() != "Wayland")
            return;

        var windows = SDL.GetWindows(out var count);
        if (count != 1 || windows is not { Length: 1 })
            throw new InvalidOperationException("The taskbar test requires one native window.");
        var window = windows[0];
        var initialState = SDL.GetWindowProgressState(window);
        var initialValue = SDL.GetWindowProgressValue(window);

        Expect<NotSupportedException>(() => DisplayServer.WindowSetTaskbarProgressState(DisplayServer.ProgressState.Normal));
        Expect<NotSupportedException>(() => DisplayServer.WindowSetTaskbarProgressValue(0.5f));
        Expect<ArgumentOutOfRangeException>(() => DisplayServer.WindowSetTaskbarProgressState((DisplayServer.ProgressState)99));
        Expect<ArgumentOutOfRangeException>(() => DisplayServer.WindowSetTaskbarProgressValue(float.NaN));
        Expect<ArgumentOutOfRangeException>(() => DisplayServer.WindowSetTaskbarProgressState(DisplayServer.ProgressState.Normal, 1));
        Expect<ArgumentOutOfRangeException>(() => DisplayServer.WindowSetTaskbarProgressValue(0.5f, 1));

        if (SDL.GetWindowProgressState(window) != initialState ||
            BitConverter.SingleToInt32Bits(SDL.GetWindowProgressValue(window)) != BitConverter.SingleToInt32Bits(initialValue))
            throw new InvalidOperationException("Unavailable Wayland taskbar calls mutated SDL progress state.");
        Console.WriteLine("Wayland taskbar progress rejected without a verified desktop integration.");
    }

    private static void Expect<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name} from the taskbar operation.");
    }
}
