using Electron2D;
using SDL3;

internal static class DisplayServerThemePortalNativeTests
{
    public static void Run()
    {
        if (Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") != "wayland")
            throw new InvalidOperationException("The portal theme test requires Wayland.");
        var version = int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_TEST_THEME_VERSION")!);
        var scheme = int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_TEST_THEME_SCHEME")!);
        var signalTest = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_THEME_SIGNALS") == "1";

        using var display = DisplayServer.Open("Electron2D hidden theme probe", new Vector2i(160, 120), hidden: true);
        Check(DisplayServer.IsDarkModeSupported() == (version >= 1), "Theme support follows the portal interface version.");
        Check(SDL.GetSystemTheme() == NativeTheme(scheme), "SDL read the portal's initial color scheme.");
        Check(DisplayServer.IsDarkMode() == (version >= 1 && scheme == 1),
            "The dark query requires both portal support and a dark preference.");
        DisplayServer.ProcessEvents();
        if (!signalTest)
        {
            Console.WriteLine($"PROBE_OK:{version}:{scheme}");
            return;
        }

        var delivered = 0;
        var expectedScheme = scheme;
        DisplayServer.SystemThemeChanged += () =>
        {
            Check(SDL.GetSystemTheme() == NativeTheme(expectedScheme), "Theme state changes before callback delivery.");
            Check(DisplayServer.IsDarkMode() == (expectedScheme == 1), "Callback sees the current dark preference.");
            delivered++;
        };
        foreach (var next in new[] { 2, 0, 1 })
        {
            Console.WriteLine($"READY:{next}");
            Console.Out.Flush();
            Check(Console.ReadLine() == $"GO:{next}", "The portal orchestrator must acknowledge each signal.");
            expectedScheme = next;
            var expectedCount = delivered + 1;
            var deadline = DateTime.UtcNow.AddSeconds(3);
            while (delivered < expectedCount && DateTime.UtcNow < deadline)
            {
                DisplayServer.ProcessEvents();
                Thread.Sleep(10);
            }
            Check(delivered == expectedCount, "One native theme notification must reach the typed event.");
            Check(SDL.GetSystemTheme() == NativeTheme(next), "SDL tracks the signaled portal theme.");
            Console.WriteLine($"DONE:{next}");
        }
    }

    private static SDL.SystemTheme NativeTheme(int scheme) => scheme switch
    {
        0 => SDL.SystemTheme.Unknown,
        1 => SDL.SystemTheme.Dark,
        2 => SDL.SystemTheme.Light,
        _ => throw new ArgumentOutOfRangeException(nameof(scheme))
    };

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
