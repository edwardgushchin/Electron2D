using Electron2D;
using SDL3;

internal static class DisplayServerPointerNativeTests
{
    public static void Run(DisplayServer display)
    {
        if (SDL.GetCurrentVideoDriver() != "wayland")
            return;

        var windows = SDL.GetWindows(out var count);
        if (count != 1 || windows is not [var window])
            throw new InvalidOperationException("The pointer test requires one native window.");

        var modes = new[]
        {
            DisplayServer.MouseMode.Hidden,
            DisplayServer.MouseMode.Captured,
            DisplayServer.MouseMode.Confined,
            DisplayServer.MouseMode.ConfinedHidden,
            DisplayServer.MouseMode.Visible,
        };
        try
        {
            foreach (var mode in modes)
            {
                display.MouseSetMode(mode);
                display.MouseSetMode(mode);
                var flags = SDL.GetWindowFlags(window);
                var expectedGrab = mode is DisplayServer.MouseMode.Confined or DisplayServer.MouseMode.ConfinedHidden;
                Check(display.MouseGetMode() == mode &&
                      SDL.GetWindowRelativeMouseMode(window) == (mode == DisplayServer.MouseMode.Captured) &&
                      ((flags & SDL.WindowFlags.MouseGrabbed) != 0) == expectedGrab &&
                      SDL.CursorVisible() == (mode is DisplayServer.MouseMode.Visible or DisplayServer.MouseMode.Confined),
                    $"Mouse mode {mode} has the corresponding native relative, grab, and cursor state.");
            }

            var nativeButtons = SDL.GetMouseState(out _, out _);
            var expectedButtons = MouseButtonMask.None;
            if ((nativeButtons & SDL.MouseButtonFlags.Left) != 0) expectedButtons |= MouseButtonMask.Left;
            if ((nativeButtons & SDL.MouseButtonFlags.Right) != 0) expectedButtons |= MouseButtonMask.Right;
            if ((nativeButtons & SDL.MouseButtonFlags.Middle) != 0) expectedButtons |= MouseButtonMask.Middle;
            if ((nativeButtons & SDL.MouseButtonFlags.X1) != 0) expectedButtons |= MouseButtonMask.XButton1;
            if ((nativeButtons & SDL.MouseButtonFlags.X2) != 0) expectedButtons |= MouseButtonMask.XButton2;
            Check(display.MouseGetButtonState() == expectedButtons,
                "The public held-button mask follows native Wayland pointer state.");
            Check(!display.HasFeature(DisplayServer.Feature.MouseWarp),
                "Wayland does not advertise a pointer warp capability without a protocol-aware backend.");
            var beforeWarp = display.MouseGetPosition();
            var warpRejected = false;
            try
            {
                display.WarpMouse(new Vector2I(20, 20));
            }
            catch (NotSupportedException)
            {
                warpRejected = true;
            }
            Check(warpRejected && display.MouseGetPosition() == beforeWarp,
                "Wayland warp rejects the unavailable capability without synthetic pointer movement.");
            Console.WriteLine("Wayland pointer modes passed; unavailable pointer warp rejected.");
        }
        finally
        {
            display.MouseSetMode(DisplayServer.MouseMode.Visible);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
