using Electron2D;
using SDL3;

internal static class DisplayServerDialogNativeTests
{
    public static void Run()
    {
        using var display = DisplayServer.Open("Electron2D native dialog test", new Vector2I(400, 240));
        if (display.GetName() != "Wayland")
            throw new InvalidOperationException("The native dialog test requires Wayland.");

        var windows = SDL.GetWindows(out var count);
        if (count != 1 || windows is not { Length: 1 })
            throw new InvalidOperationException("The native dialog test requires one window.");
        var renderer = SDL.CreateRenderer(windows[0], "software");
        if (renderer == 0)
            throw new InvalidOperationException($"Cannot present the dialog test window: {SDL.GetError()}");
        try
        {
            if (!SDL.SetRenderDrawColor(renderer, 30, 90, 200, 255) ||
                !SDL.RenderClear(renderer) || !SDL.RenderPresent(renderer))
                throw new InvalidOperationException($"Cannot present the dialog test window: {SDL.GetError()}");

            var owner = Environment.CurrentManagedThreadId;
            var selected = int.MinValue;
            Console.Error.WriteLine("DIALOG_SELECT: Click Choose in the first dialog.");
            display.DialogShow("Choose button", "Click Choose", ["Cancel", "Choose"], index =>
            {
                if (Environment.CurrentManagedThreadId != owner)
                    throw new InvalidOperationException("The message dialog callback ran off the opening thread.");
                selected = index;
            });
            if (selected != 1)
                throw new InvalidOperationException($"The second dialog button returned {selected}, expected 1.");

            selected = int.MinValue;
            Console.Error.WriteLine("DIALOG_CLOSE: Close the second dialog with its window close button.");
            display.DialogShow("Close dialog", "Close this dialog without choosing", ["Only button"], index =>
            {
                if (Environment.CurrentManagedThreadId != owner)
                    throw new InvalidOperationException("The message dialog callback ran off the opening thread.");
                selected = index;
            });
            if (selected is not (0 or -1))
                throw new InvalidOperationException($"Closing the dialog returned invalid button index {selected}.");

            Console.WriteLine($"Native Wayland message dialog button, close, and callback timing passed (dismissal: {selected}).");
        }
        finally
        {
            SDL.DestroyRenderer(renderer);
        }
    }
}
