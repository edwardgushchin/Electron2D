using Electron2D;

internal static class DisplayServerScreenSaverNativeTests
{
    public static void Run()
    {
        using var display = DisplayServer.Open("Electron2D screensaver native test", new Vector2i(160, 120));
        DisplayServer.ScreenSetKeepOn(false);
        Check(!DisplayServer.ScreenIsKeptOn(), "The native backend accepts restoring normal screen blanking.");
        Thread.Sleep(300);
        DisplayServer.ScreenSetKeepOn(true);
        Check(DisplayServer.ScreenIsKeptOn(), "The native backend accepts inhibiting screen blanking.");
        Thread.Sleep(300);
        DisplayServer.ScreenSetKeepOn(false);
        Check(!DisplayServer.ScreenIsKeptOn(), "The native backend accepts releasing the screen blanking inhibition.");
        Console.WriteLine("Native screen blanking requests passed.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
