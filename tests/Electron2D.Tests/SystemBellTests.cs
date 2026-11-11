using Electron2D;

internal static class SystemBellTests
{
    internal static void Run(string profile)
    {
        var expectedDriver = profile switch
        {
            "windows" => "Windows",
            "cocoa" => "macOS",
            "x11" => "X11",
            "wayland" or "wayland-unavailable" => "Wayland",
            "unavailable" => "headless",
            _ => throw new ArgumentException("Unknown system bell test profile.", nameof(profile)),
        };

        Reject(DisplayServer.Beep);
        using (DisplayServer.Open("System bell", new(64, 64), hidden: true))
        {
            if (DisplayServer.GetName() != expectedDriver)
                throw new InvalidOperationException($"The system bell test requires {expectedDriver}.");
            Reject(() => Task.Run(DisplayServer.Beep).GetAwaiter().GetResult());
            DisplayServer.Beep();
            if (profile is "x11" or "wayland")
                DisplayServer.Beep();
        }
        Reject(DisplayServer.Beep);

        if (profile is "x11" or "wayland")
        {
            using var reopened = DisplayServer.Open("Reopened system bell", new(64, 64), hidden: true);
            DisplayServer.Beep();
        }
        Console.WriteLine($"System bell dispatch and ownership passed ({profile}).");
    }

    private static void Reject(Action action)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("The system bell requires an active display on its opening thread.");
    }
}
