using System.Runtime.InteropServices;
using Electron2D;
using Mutex = System.Threading.Mutex;

internal static partial class DesktopAccessibilityTests
{
    internal static void Run(string profile)
    {
        Func<int>[] queries =
        [
            DisplayServer.AccessibilityScreenReaderActive, DisplayServer.AccessibilityShouldIncreaseContrast,
            DisplayServer.AccessibilityShouldReduceAnimation, DisplayServer.AccessibilityShouldReduceTransparency,
        ];
        CheckInactive(queries);
        using (DisplayServer.Open("Desktop accessibility", new(64, 64), hidden: true))
        {
            foreach (var query in queries)
                Reject(() => Task.Run(query).GetAwaiter().GetResult());

            switch (profile)
            {
                case "windows":
                    Windows(queries);
                    break;
                case "linux":
                    var expectedDriver = Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "x11" ? "X11" : "Wayland";
                    Check(DisplayServer.GetName() == expectedDriver, $"Use the native {expectedDriver} display driver.");
                    while (Console.ReadLine() is { } line)
                    {
                        var expected = line.Split(' ').Select(int.Parse).ToArray();
                        Check(queries.Select(query => query()).SequenceEqual(expected), $"Unexpected Linux preferences for {line}.");
                        Console.WriteLine("Preferences matched.");
                    }
                    break;
                case "unavailable":
                    Check(queries.All(query => query() == -1), "Dummy display must report unknown values.");
                    break;
                default:
                    throw new ArgumentException("Unknown desktop accessibility test profile.", nameof(profile));
            }
        }

        CheckInactive(queries);
        Console.WriteLine($"Desktop accessibility preferences passed ({profile}).");
    }

    private static void Windows(Func<int>[] queries)
    {
        Check(DisplayServer.GetName() == "Windows", "Use the native Windows display driver.");
        var contrast = new HighContrast { Size = (uint)Marshal.SizeOf<HighContrast>() };
        Check(ReadContrast(0x0042, contrast.Size, ref contrast, 0) != 0, "Win32 high contrast probe failed.");
        Check(ReadFlag(0x1042, 0, out var animationEnabled, 0) != 0, "Win32 client animation probe failed.");
        Check(ReadFlag(0x1040, 0, out var overlappingDisabled, 0) != 0, "Win32 overlapped content probe failed.");
        Check(ReadFlag(0x0046, 0, out var readerEnabled, 0) != 0, "Win32 screen-reader probe failed.");
        for (var read = 0; read < 8; read++)
            Check(queries[1]() == (int)(contrast.Flags & 1), "Repeated high contrast reads must preserve the native state.");
        Check(queries[2]() == (animationEnabled == 0 ? 1 : 0), "Reduced animation must invert the native enable flag.");
        Check(queries[3]() == (overlappingDisabled != 0 ? 1 : 0), "Reduced transparency differs from Win32.");

        var actualNarrator = Mutex.TryOpenExisting("NarratorRunning", out var existing);
        existing?.Dispose();
        var expectedReader = readerEnabled != 0 || actualNarrator ? 1 : 0;
        Check(queries[0]() == expectedReader, "Screen-reader activity differs from the native signals.");
        if (expectedReader == 0)
        {
            using (var narratorMarker = new Mutex(false, "NarratorRunning", out var created))
            {
                Check(created && queries[0]() == 1, "A new Narrator session marker is observed immediately.");
            }

            Check(queries[0]() == 0, "Reader activity clears when the marker retires and no reader is registered.");
            Console.WriteLine("Observed Narrator marker appear and retire.");
        }

        Console.WriteLine($"Win32 preferences (reader, contrast, reduced animation, reduced transparency): {string.Join(", ", queries.Select(query => query()))}.");
    }

    private static void CheckInactive(Func<int>[] queries)
    {
        foreach (var query in queries)
            Reject(() => query());
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Reject(Action call)
    {
        try { call(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("The query must require a live display on its opening thread.");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HighContrast
    {
        internal uint Size, Flags;
        internal nint Scheme;
    }

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    private static partial int ReadContrast(uint action, uint parameter, ref HighContrast value, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    private static partial int ReadFlag(uint action, uint parameter, out int value, uint flags);
}
